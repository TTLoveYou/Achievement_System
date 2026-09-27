using MySql.Data.MySqlClient;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Text;
using 成就系统.Utilities;

namespace 成就系统.Services
{
    public class UserService : IDisposable
    {
        private readonly DatabaseHelper db = new();
        private readonly AdbDeviceManager _deviceManager;
        private readonly LocalCameraManager _localCameraManager;
        private bool _disposed;

        public UserService()
        {
            _deviceManager = new AdbDeviceManager();
            _localCameraManager = new LocalCameraManager();
        }

        //登录
        public async Task<bool> ValidateLoginAsync(string username, string password)
        {
            try
            {
                // 先获取存储的哈希和盐（hash 加盐）
                string usersTable = DatabaseTableManager.GetUsersTable();
                string sql = $"SELECT password, password_salt FROM {usersTable} WHERE account = @username";
                var parameter = new MySqlParameter("@username", username);

                using var dt = await db.ExecuteQuery(sql, parameter);
                if (dt.Rows.Count == 0) return false;

                DataRow row = dt.Rows[0];
                //正确转化字节数组
                byte[] hashBytes = (byte[])row["password"];
                byte[] saltBytes = (byte[])row["password_salt"];

                //转换为Base64字符串
                string storedHash = Convert.ToBase64String(hashBytes);
                string storedSalt = Convert.ToBase64String(saltBytes);

                // 调试输出验证
                //Debug.WriteLine($"Hash长度: {storedHash.Length}, Salt长度: {storedSalt.Length}");

                return PasswordHasher.VerifyHash(password, storedHash, storedSalt);
            }
            catch (Exception ex)
            {
                // 记录异常信息
                Debug.WriteLine($"登录验证异常: {ex.Message}\n{ex.StackTrace}");
                return false;
            }
        }

        //注册
        // 检查账户是否存在
        public async Task<bool> AccountExistsAsync(string username)
        {
            try
            {
                string usersTable = DatabaseTableManager.GetUsersTable();
                string sql = $"SELECT COUNT(1) FROM {usersTable} WHERE account = @username";
                var parameter = new MySqlParameter("@username", username);
                object result = await db.ExecuteScalarAsync(sql, parameter);
                return result != null && result != DBNull.Value && Convert.ToInt32(result) > 0;
            }
            catch (Exception ex)
            {
                // 建议记录日志
                Console.WriteLine($"账户检查异常：{ex.Message}");
                return true; // 发生异常时默认认为账户存在
            }
        }

        // 注册新用户
        public async Task<bool> RegisterAsync(string username, string password, byte[]? faceData = null)
        {
            try
            {
                byte[]? faceEmbeddings = null;
                if (faceData != null)
                {
                    using var faceService = new FaceRecognitionService();
                    faceEmbeddings = faceService.GetFaceEmbeddings(faceData)
                        ?.SelectMany(BitConverter.GetBytes)     // 浮点数组→字节流转换
                        .ToArray();                             // 生成最终字节序列
                }


                // 生成哈希密码和盐
                var (hashBase64, saltBase64) = PasswordHasher.CreateHash(password);

                // 将 Base64 字符串解码为字节数组
                byte[] hashBytes = Convert.FromBase64String(hashBase64);
                byte[] saltBytes = Convert.FromBase64String(saltBase64);

                // 修改SQL语句包含face_data字段
                string usersTable = DatabaseTableManager.GetUsersTable();
                string sql = $@"INSERT INTO {usersTable} (account, password, password_salt, face_data, face_embeddings) 
                         VALUES (@username, @password, @salt, @faceData, @faceEmbeddings)";

                var parameters = new[]
                {
                new MySqlParameter("@username", username),
                new MySqlParameter("@password", hashBytes),         // 传递字节数组
                new MySqlParameter("@salt",saltBytes),               // 传递字节数组
                new MySqlParameter("@faceData", faceData ?? (object)DBNull.Value),   // 处理空值
                new MySqlParameter("@faceEmbeddings", faceEmbeddings ?? (object)DBNull.Value)
            };

                int affectedRows = await db.ExecuteNonQueryAsync(sql, parameters);
                return affectedRows > 0;
            }
            catch (MySqlException ex)
            {
                // 处理唯一约束冲突（当账户已存在时）
                if (ex.Number == 1062)
                {
                    return false;
                }
                // 其他数据库异常处理
                return false;
            }
            catch
            {
                return false;
            }
        }

        //人脸识别 - ADB相机
        public async Task<string> FaceLoginAsync(bool shouldManageCamera = true)
        {
            string? imagePath = null;
            try
            {
                // 检查ADB是否可用
                if (!_deviceManager.IsAdbAvailable())
                {
                    throw new FaceRecognitionException("ADB未找到，请在设置中配置ADB路径");
                }

                // 传入skipCameraManagement参数
                imagePath = await _deviceManager.CaptureFaceImageAsync(skipCameraManagement: !shouldManageCamera);
                var currentImage = File.ReadAllBytes(imagePath);

                // 2. 提取特征
                float[] currentEmbeddings;
                using (var faceService = new FaceRecognitionService())
                {
                    currentEmbeddings = faceService.GetFaceEmbeddings(currentImage);
                    if (currentEmbeddings == null)
                        throw new FaceRecognitionException("未检测到人脸");
                }

                // 3. 数据库比对
                string usersTable = DatabaseTableManager.GetUsersTable();
                string sql = $"SELECT account, face_embeddings FROM {usersTable} WHERE face_embeddings IS NOT NULL";
                using var dt = await db.ExecuteQuery(sql);

                foreach (DataRow row in dt.Rows)
                {
                    // 字节流→浮点数组转换
                    var embeddingsBytes = (byte[])row["face_embeddings"];
                    var storedEmbeddings = new float[embeddingsBytes.Length / 4];
                    Buffer.BlockCopy(embeddingsBytes, 0, storedEmbeddings, 0, embeddingsBytes.Length);

                    using var faceService = new FaceRecognitionService();
                    if (FaceRecognitionService.CompareFaces(storedEmbeddings, currentEmbeddings))
                    {
                        return row["account"].ToString();
                    }
                }

                throw new FaceRecognitionException("未找到匹配的用户");
            }
            catch (DeviceException ex)
            {
                throw new FaceRecognitionException("设备操作失败", ex);
            }
            finally
            {
                // 无论成功与否都删除图片
                DeleteImageIfExists(imagePath);
            }
        }

        //人脸识别 - 本地相机
        public async Task<string> LocalFaceLoginAsync()
        {
            string? imagePath = null;
            try
            {
                // 捕获人脸图像
                imagePath = await _localCameraManager.CaptureFaceImageAsync();
                var currentImage = File.ReadAllBytes(imagePath);

                // 提取特征
                float[] currentEmbeddings;
                using (var faceService = new FaceRecognitionService())
                {
                    currentEmbeddings = faceService.GetFaceEmbeddings(currentImage);
                    if (currentEmbeddings == null)
                        throw new FaceRecognitionException("未检测到人脸");
                }

                // 数据库比对
                string usersTable = DatabaseTableManager.GetUsersTable();
                string sql = $"SELECT account, face_embeddings FROM {usersTable} WHERE face_embeddings IS NOT NULL";
                using var dt = await db.ExecuteQuery(sql);

                foreach (DataRow row in dt.Rows)
                {
                    // 字节流→浮点数组转换
                    var embeddingsBytes = (byte[])row["face_embeddings"];
                    var storedEmbeddings = new float[embeddingsBytes.Length / 4];
                    Buffer.BlockCopy(embeddingsBytes, 0, storedEmbeddings, 0, embeddingsBytes.Length);

                    using var faceService = new FaceRecognitionService();
                    if (FaceRecognitionService.CompareFaces(storedEmbeddings, currentEmbeddings))
                    {
                        return row["account"].ToString();
                    }
                }

                throw new FaceRecognitionException("未找到匹配的用户");
            }
            catch (DeviceException ex)
            {
                throw new FaceRecognitionException("设备操作失败", ex);
            }
            finally
            {
                // 无论成功与否都删除图片
                DeleteImageIfExists(imagePath);
            }
        }

        //本地相机人脸验证
        public async Task<bool> LocalVerifyFaceAsync(string username)
        {
            return await _localCameraManager.VerifyFaceAsync(username);
        }

        //本地相机录入人脸数据
        public async Task<bool> LocalEnrollFaceDataAsync(string userName)
        {
            try
            {
                // 捕获人脸图像
                string imagePath = await _localCameraManager.CaptureFaceImageAsync();
                var faceData = File.ReadAllBytes(imagePath);

                // 提取特征
                byte[]? faceEmbeddings = null;
                if (faceData != null)
                {
                    using var faceService = new FaceRecognitionService();
                    faceEmbeddings = faceService.GetFaceEmbeddings(faceData)
                        ?.SelectMany(BitConverter.GetBytes)
                        .ToArray();
                }

                // 保存到数据库
                string usersTable = DatabaseTableManager.GetUsersTable();
                string sql = $"UPDATE {usersTable} SET face_data = @faceData, face_embeddings = @faceEmbeddings WHERE account = @userName";
                var parameters = new[]
                {
                    new MySqlParameter("@faceData", faceData ?? (object)DBNull.Value),
                    new MySqlParameter("@faceEmbeddings", faceEmbeddings ?? (object)DBNull.Value),
                    new MySqlParameter("@userName", userName)
                };

                int affectedRows = await db.ExecuteNonQueryAsync(sql, parameters);
                return affectedRows > 0;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"录入人脸数据异常: {ex.Message}");
                return false;
            }
        }

        //重置密码（带人脸验证）
        public async Task<bool> ResetPasswordAsync(string username, string newPassword, byte[] faceData)
        {
            try
            {
                if (faceData == null)
                {
                    // 直接更新密码，不进行人脸验证
                    return await UpdatePasswordAsync(username, newPassword);
                }

                // 1. 提取当前人脸特征
                float[] currentEmbeddings;
                using (var faceService = new FaceRecognitionService())
                {
                    currentEmbeddings = faceService.GetFaceEmbeddings(faceData);
                    if (currentEmbeddings == null)
                        throw new FaceRecognitionException("未检测到人脸");
                }

                // 2. 获取用户存储的人脸特征
                string usersTable = DatabaseTableManager.GetUsersTable();
                string sql = $"SELECT face_embeddings FROM {usersTable} WHERE account = @username";
                var parameter = new MySqlParameter("@username", username);

                using var dt = await db.ExecuteQuery(sql, parameter);
                if (dt.Rows.Count == 0)
                    return false;

                // 3. 比对特征
                byte[] embeddingsBytes = (byte[])dt.Rows[0]["face_embeddings"];
                var storedEmbeddings = new float[embeddingsBytes.Length / 4];
                Buffer.BlockCopy(embeddingsBytes, 0, storedEmbeddings, 0, embeddingsBytes.Length);

                using (var faceService = new FaceRecognitionService())
                {
                    if (!FaceRecognitionService.CompareFaces(storedEmbeddings, currentEmbeddings))
                        return false;
                }

                // 4. 生成新密码的哈希
                var (hashBase64, saltBase64) = PasswordHasher.CreateHash(newPassword);
                byte[] hashBytes = Convert.FromBase64String(hashBase64);
                byte[] saltBytes = Convert.FromBase64String(saltBase64);

                // 5. 更新数据库
                sql = $@"UPDATE {usersTable} SET password = @password, password_salt = @salt 
                WHERE account = @username";

                var parameters = new[]
                {
                    new MySqlParameter("@password", hashBytes),
                    new MySqlParameter("@salt", saltBytes),
                    new MySqlParameter("@username", username)
                };

                int affectedRows = await db.ExecuteNonQueryAsync(sql, parameters);
                return affectedRows > 0;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"密码重置异常: {ex.Message}");
                return false;
            }
        }

        //直接更新密码（无需人脸验证）
        public async Task<bool> UpdatePasswordAsync(string username, string newPassword)
        {
            try
            {
                // 生成新密码的哈希
                var (hashBase64, saltBase64) = PasswordHasher.CreateHash(newPassword);
                byte[] hashBytes = Convert.FromBase64String(hashBase64);
                byte[] saltBytes = Convert.FromBase64String(saltBase64);

                // 更新数据库
                string usersTable = DatabaseTableManager.GetUsersTable();
                string sql = $"UPDATE {usersTable} SET password = @password, password_salt = @salt WHERE account = @username";

                var parameters = new[]
                {
                    new MySqlParameter("@password", hashBytes),
                    new MySqlParameter("@salt", saltBytes),
                    new MySqlParameter("@username", username)
                };

                int affectedRows = await db.ExecuteNonQueryAsync(sql, parameters);
                return affectedRows > 0;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"密码更新异常: {ex.Message}");
                return false;
            }
        }

        //删除临时文件
        private static void DeleteImageIfExists(string? imagePath)
        {
            try
            {
                if (!string.IsNullOrEmpty(imagePath) && File.Exists(imagePath))
                {
                    File.Delete(imagePath);
                    Debug.WriteLine($"已删除临时图片: {imagePath}");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"删除临时图片失败: {ex.Message}");
            }
        }

        // 更新账户名
        public async Task<bool> UpdateUserNameAsync(string oldUserName, string newUserName)
        {
            try
            {
                // 检查新账户名是否已存在（使用现有的 AccountExistsAsync 方法）
                bool accountExists = await AccountExistsAsync(newUserName);
                if (accountExists)
                {
                    return false;
                }

                // 更新账户名
                string usersTable = DatabaseTableManager.GetUsersTable();
                string updateSql = $"UPDATE {usersTable} SET account = @newUserName WHERE account = @oldUserName";
                var parameters = new[]
                {
                    new MySqlParameter("@newUserName", newUserName),
                    new MySqlParameter("@oldUserName", oldUserName)
                };

                int affectedRows = await db.ExecuteNonQueryAsync(updateSql, parameters);
                return affectedRows > 0;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"更新账户名异常: {ex.Message}");
                return false;
            }
        }

        // 通过当前密码更新新密码
        public async Task<bool> UpdatePasswordAsync(string userName, string currentPassword, string newPassword)
        {
            try
            {
                // 先验证当前密码
                string usersTable = DatabaseTableManager.GetUsersTable();
                string checkSql = $"SELECT password, password_salt FROM {usersTable} WHERE account = @userName";
                var checkParam = new MySqlParameter("@userName", userName);

                using var dt = await db.ExecuteQuery(checkSql, checkParam);
                if (dt.Rows.Count == 0)
                {
                    return false;
                }

                DataRow row = dt.Rows[0];
                byte[] hashBytes = (byte[])row["password"];
                byte[] saltBytes = (byte[])row["password_salt"];

                string storedHash = Convert.ToBase64String(hashBytes);
                string storedSalt = Convert.ToBase64String(saltBytes);

                if (!PasswordHasher.VerifyHash(currentPassword, storedHash, storedSalt))
                {
                    return false;
                }

                // 生成新密码的哈希和盐
                var (newHashBase64, newSaltBase64) = PasswordHasher.CreateHash(newPassword);
                byte[] newHashBytes = Convert.FromBase64String(newHashBase64);
                byte[] newSaltBytes = Convert.FromBase64String(newSaltBase64);

                // 更新密码
                string updateSql = $"UPDATE {usersTable} SET password = @password, password_salt = @salt WHERE account = @userName";
                var parameters = new[]
                {
                    new MySqlParameter("@password", newHashBytes),
                    new MySqlParameter("@salt", newSaltBytes),
                    new MySqlParameter("@userName", userName)
                };

                int affectedRows = await db.ExecuteNonQueryAsync(updateSql, parameters);
                return affectedRows > 0;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"更新密码异常: {ex.Message}");
                return false;
            }
        }

        // 检查人脸数据状态
        public async Task<bool> CheckFaceDataStatusAsync(string userName)
        {
            try
            {
                string usersTable = DatabaseTableManager.GetUsersTable();
                string sql = $"SELECT face_data, face_embeddings FROM {usersTable} WHERE account = @userName";
                var parameter = new MySqlParameter("@userName", userName);

                using var dt = await db.ExecuteQuery(sql, parameter);
                if (dt.Rows.Count == 0)
                    return false;

                DataRow row = dt.Rows[0];
                return !(row["face_data"] is DBNull || row["face_embeddings"] is DBNull);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"检查人脸数据状态异常: {ex.Message}");
                return false;
            }
        }

        // 录入人脸数据
        public async Task<bool> EnrollFaceDataAsync(string userName, byte[] faceData)
        {
            try
            {
                byte[]? faceEmbeddings = null;
                if (faceData != null)
                {
                    using var faceService = new FaceRecognitionService();
                    faceEmbeddings = faceService.GetFaceEmbeddings(faceData)
                        ?.SelectMany(BitConverter.GetBytes)
                        .ToArray();
                }

                string usersTable = DatabaseTableManager.GetUsersTable();
                string sql = $"UPDATE {usersTable} SET face_data = @faceData, face_embeddings = @faceEmbeddings WHERE account = @userName";
                var parameters = new[]
                {
                    new MySqlParameter("@faceData", faceData ?? (object)DBNull.Value),
                    new MySqlParameter("@faceEmbeddings", faceEmbeddings ?? (object)DBNull.Value),
                    new MySqlParameter("@userName", userName)
                };

                int affectedRows = await db.ExecuteNonQueryAsync(sql, parameters);
                return affectedRows > 0;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"录入人脸数据异常: {ex.Message}");
                return false;
            }
        }

        // 注销账户
        public async Task<bool> DeleteAccountAsync(string userName)
        {
            try
            {
                // 删除用户的所有成就数据
                string achievementsTable = DatabaseTableManager.GetAchievementsTable();
                string usersTable = DatabaseTableManager.GetUsersTable();

                // 先获取用户ID
                string userIdSql = $"SELECT user_id FROM {usersTable} WHERE account = @userName";
                var userIdParam = new MySqlParameter("@userName", userName);
                var userIdResult = await db.ExecuteScalarAsync(userIdSql, userIdParam);

                if (userIdResult != null)
                {
                    int userId = Convert.ToInt32(userIdResult);
                    // 删除用户的所有成就
                    string deleteAchievementsSql = $"DELETE FROM {achievementsTable} WHERE user_id = @userId";
                    var deleteAchievementsParam = new MySqlParameter("@userId", userId);
                    await db.ExecuteNonQueryAsync(deleteAchievementsSql, deleteAchievementsParam);
                }

                // 删除用户账户
                string deleteUserSql = $"DELETE FROM {usersTable} WHERE account = @userName";
                var deleteUserParam = new MySqlParameter("@userName", userName);
                int affectedRows = await db.ExecuteNonQueryAsync(deleteUserSql, deleteUserParam);

                return affectedRows > 0;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"注销账户异常: {ex.Message}");
                return false;
            }
        }

        //资源释放
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed) return;

            if (disposing)
            {
                _deviceManager?.Dispose();  // 级联释放设备管理资源（继承自之前ADB管理模块）
                _localCameraManager?.Dispose();  // 释放本地相机管理资源
            }

            _disposed = true;
        }

    }

    //生物识别专用异常
    public class FaceRecognitionException : Exception
    {
        public FaceErrorType ErrorType { get; }

        public FaceRecognitionException(string message,
                                       FaceErrorType type = FaceErrorType.RecognitionFailed)
            : base(message)
        {
            ErrorType = type;
        }

        public FaceRecognitionException(string message, Exception inner)
            : base(message, inner)
        {
            ErrorType = FaceErrorType.Unknown;
        }
    }

    //人脸识别错误类型
    public enum FaceErrorType
    {
        NoFaceDetected,     // 未检测到人脸
        MultipleFaces,      // 检测到多张人脸
        RecognitionFailed,  // 识别失败
        DatabaseError,      // 数据库错误
        DeviceError,        // 设备错误
        Unknown             // 未知错误
    }
}
