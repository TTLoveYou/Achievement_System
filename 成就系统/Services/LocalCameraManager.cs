using MySql.Data.MySqlClient;
using System.Data;
using System.Diagnostics;
using System.IO;
using Windows.Media.Capture;
using 成就系统.Utilities;

namespace 成就系统.Services
{
    class LocalCameraManager : IDisposable
    {
        private MediaCapture? _mediaCapture;
        private bool _isInitialized = false;
        private bool _disposed;

        public LocalCameraManager()
        {
        }

        /// <summary>
        /// 初始化本地相机
        /// </summary>
        public async Task InitializeCameraAsync()
        {
            try
            {
                if (_mediaCapture == null)
                {
                    _mediaCapture = new MediaCapture();
                    await _mediaCapture.InitializeAsync();
                    _isInitialized = true;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"初始化相机失败: {ex.Message}");
                throw new DeviceException(DeviceErrorType.CameraLaunchFailed, "无法初始化本地相机");
            }
        }

        /// <summary>
        /// 捕获人脸图像
        /// </summary>
        public async Task<string> CaptureFaceImageAsync()
        {
            try
            {
                if (!_isInitialized)
                {
                    await InitializeCameraAsync();
                }

                return await CaptureAndSavePhoto();
            }
            finally
            {
                // 释放相机资源
                await CleanupCameraAsync();
            }
        }

        /// <summary>
        /// 捕获并保存照片
        /// </summary>
        private async Task<string> CaptureAndSavePhoto()
        {
            try
            {
                // 生成保存路径
                string savePath = GetPhotoSavePath();
                string directoryPath = Path.GetDirectoryName(savePath);
                if (!string.IsNullOrEmpty(directoryPath) && !Directory.Exists(directoryPath))
                {
                    Directory.CreateDirectory(directoryPath);
                }

                // 获取临时文件夹
                var tempFolder = Windows.Storage.ApplicationData.Current.TemporaryFolder;

                // 创建临时文件
                var tempFile = await tempFolder.CreateFileAsync($"temp_{Guid.NewGuid()}.jpg", Windows.Storage.CreationCollisionOption.ReplaceExisting);

                // 捕获照片
                var encodingProperties = Windows.Media.MediaProperties.ImageEncodingProperties.CreateJpeg();
                await _mediaCapture.CapturePhotoToStorageFileAsync(encodingProperties, tempFile);

                // 保存到本地文件
                using (var fileStream = await tempFile.OpenStreamForReadAsync())
                using (var outputStream = new FileStream(savePath, FileMode.Create))
                {
                    await fileStream.CopyToAsync(outputStream);
                }

                // 删除临时文件
                await tempFile.DeleteAsync();

                // 裁剪和调整图像大小
                try
                {
                    using var faceService = new FaceRecognitionService();
                    var croppedPath = FaceRecognitionService.CropAndResizeImage(savePath);

                    // 替换原图
                    File.Delete(savePath);
                    File.Move(croppedPath, savePath);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"图片裁剪失败: {ex.Message}");
                    // 裁剪失败时保留原图
                }

                return savePath;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"捕获照片失败: {ex.Message}");
                throw new DeviceException(DeviceErrorType.FaceCaptureFailed, "无法捕获人脸图像");
            }
        }

        /// <summary>
        /// 生成照片保存路径
        /// </summary>
        private static string GetPhotoSavePath()
        {
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string faceImageDir = Path.Combine(baseDir, "faceImage");
            Directory.CreateDirectory(faceImageDir);

            return Path.Combine(faceImageDir, $"local_{DateTime.Now:yyyyMMdd_HHmmss}.png");
        }

        /// <summary>
        /// 人脸验证
        /// </summary>
        public async Task<bool> VerifyFaceAsync(string username)
        {
            try
            {
                // 捕获当前人脸图像
                string imagePath = await CaptureFaceImageAsync();
                var currentImage = File.ReadAllBytes(imagePath);

                // 提取当前人脸特征
                float[] currentEmbeddings;
                using (var faceService = new FaceRecognitionService())
                {
                    currentEmbeddings = faceService.GetFaceEmbeddings(currentImage);
                    if (currentEmbeddings == null)
                        return false;
                }

                // 从数据库获取用户存储的人脸特征
                using var userService = new UserService();
                using var db = new DatabaseHelper();

                // 直接查询当前用户的人脸特征
                string usersTable = DatabaseTableManager.GetUsersTable();
                string sql = $"SELECT face_embeddings FROM {usersTable} WHERE account = @username";
                var parameter = new MySqlParameter("@username", username);

                using var dt = await db.ExecuteQuery(sql, parameter);
                if (dt.Rows.Count == 0)
                    return false;

                DataRow row = dt.Rows[0];
                if (row["face_embeddings"] is DBNull)
                    return false;

                // 字节流→浮点数组转换
                var embeddingsBytes = (byte[])row["face_embeddings"];
                var storedEmbeddings = new float[embeddingsBytes.Length / 4];
                System.Buffer.BlockCopy(embeddingsBytes, 0, storedEmbeddings, 0, embeddingsBytes.Length);

                // 比较人脸特征
                using var faceService2 = new FaceRecognitionService();
                bool isMatch = FaceRecognitionService.CompareFaces(storedEmbeddings, currentEmbeddings);

                return isMatch;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"人脸验证失败: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 清理相机资源
        /// </summary>
        private async Task CleanupCameraAsync()
        {
            try
            {
                if (_mediaCapture != null)
                {
                    _mediaCapture.Dispose();
                    _mediaCapture = null;
                    _isInitialized = false;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"清理相机资源失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            if (_disposed) return;

            CleanupCameraAsync().Wait();

            _disposed = true;
            GC.SuppressFinalize(this);
        }
    }
}
