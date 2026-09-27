using MySql.Data.MySqlClient;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Text;
using 成就系统.Config;
using 成就系统.Utilities;

namespace 成就系统.Services
{
    public class AchievementService : IDisposable
    {
        // 图片路径缓存：键为成就名称（清理后），值为图片路径
        private static readonly Dictionary<string, string> _imagePathCache = [];
        // 缓存初始化标记
        private static bool _imageCacheInitialized = false;
        // 缓存线程安全锁
        private static readonly Lock _cacheLock = new();

        private readonly DatabaseHelper _db = new();

        //获取用户ID
        private async Task<int> GetUserIdByUsernameAsync(string? username)
        {
            if (string.IsNullOrWhiteSpace(username))
                return 0;

            string usersTable = DatabaseTableManager.GetUsersTable();
            string sql = $"SELECT user_id FROM {usersTable} WHERE account = @username";
            var parameter = new MySqlParameter("@username", username);
            var result = await _db.ExecuteScalarAsync(sql, parameter);
            return result != null ? Convert.ToInt32(result) : 0;
        }

        // 获取类别ID
        private async Task<int> GetCategoryIdAsync(string? bigCategory, string? smallCategory)
        {
            if (string.IsNullOrWhiteSpace(bigCategory) || string.IsNullOrWhiteSpace(smallCategory))
                return 0;

            string categoriesTable = DatabaseTableManager.GetCategoriesTable();
            string sql = $@"SELECT category_id 
                           FROM {categoriesTable} 
                           WHERE big_category = @bigCategory 
                           AND small_category = @smallCategory";

            var parameters = new[]
            {
                new MySqlParameter("@bigCategory", bigCategory),
                new MySqlParameter("@smallCategory", smallCategory)
            };

            var result = await _db.ExecuteScalarAsync(sql, parameters);
            return result != null ? Convert.ToInt32(result) : 0;
        }

        //获取大类、小类
        public async Task<(string bigCategory, string smallCategory)> GetCategoryNamesAsync(int categoryId)
        {
            try
            {
                string categoriesTable = DatabaseTableManager.GetCategoriesTable();
                string sql = $@"
                                SELECT big_category, small_category 
                                FROM {categoriesTable} 
                                WHERE category_id = @categoryId
                             ";

                var parameter = new MySqlParameter("@categoryId", categoryId);

                using var dt = await _db.ExecuteQuery(sql, parameter);
                if (dt.Rows.Count > 0)
                {
                    DataRow row = dt.Rows[0];
                    string bigCategory = row["big_category"]?.ToString() ?? "未知大类";
                    string smallCategory = row["small_category"]?.ToString() ?? "未知小类";
                    return (bigCategory, smallCategory);
                }
                else
                {
                    return ("未知大类", "未知小类");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"获取类别名称失败: {ex.Message}");
                return ("未知大类", "未知小类");
            }
        }

        // 获取生日成就
        public async Task<List<AchievementCard>> GetBirthdayAchievementsAsync(string username)
        {
            try
            {
                // 获取当前日期
                DateTime today = DateTime.Today;

                string achievementsTable = DatabaseTableManager.GetAchievementsTable();
                string usersTable = DatabaseTableManager.GetUsersTable();
                string sql = $@"
                                SELECT 
                                    a.achievement_id,
                                    a.category_id,
                                    a.achievement_name, 
                                    a.description, 
                                    a.achievement_date, 
                                    a.completion_degree, 
                                    a.score
                                FROM {achievementsTable} a
                                JOIN {usersTable} u ON a.user_id = u.user_id
                                WHERE u.account = @username 
                                    AND MONTH(a.achievement_date) = MONTH(@today)
                                    AND DAY(a.achievement_date) = DAY(@today)";

                var parameters = new[]
                {
                    new MySqlParameter("@username", username),
                    new MySqlParameter("@today", today)
                };

                using var dt = await _db.ExecuteQuery(sql, parameters);
                var cards = new List<AchievementCard>();

                foreach (DataRow row in dt.Rows)
                {
                    var card = new AchievementCard
                    {
                        AchievementId = Convert.ToInt32(row["achievement_id"]),
                        CategoryId = Convert.ToInt32(row["category_id"]),
                        AchievementName = row["achievement_name"] != DBNull.Value ? row["achievement_name"].ToString() : string.Empty,
                        Description = row["description"] != DBNull.Value ? row["description"].ToString() : string.Empty,
                        AchievementDate = Convert.ToDateTime(row["achievement_date"]),
                        CompletionDegree = row["completion_degree"] != DBNull.Value ? row["completion_degree"].ToString() : string.Empty,
                        Score = Convert.ToDecimal(row["score"])
                    };

                    card.ImagePath = GetAchievementImagePath(card.AchievementName);
                    cards.Add(card);
                }

                return cards;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"获取生日成就失败: {ex.Message}");
                return [];
            }
        }

        // 获取整5周年成就
        public async Task<List<AchievementCard>> GetAnniversaryAchievementsAsync(string username)
        {
            try
            {
                // 获取当前日期
                DateTime today = DateTime.Today;
                int currentYear = today.Year;

                string achievementsTable = DatabaseTableManager.GetAchievementsTable();
                string usersTable = DatabaseTableManager.GetUsersTable();
                string sql = $@"
                                SELECT 
                                    a.achievement_id,
                                    a.category_id,
                                    a.achievement_name, 
                                    a.description, 
                                    a.achievement_date, 
                                    a.completion_degree, 
                                    a.score,
                                    @currentYear - YEAR(a.achievement_date) AS AnniversaryYears
                                FROM {achievementsTable} a
                                JOIN {usersTable} u ON a.user_id = u.user_id
                                WHERE u.account = @username 
                                    AND (@currentYear - YEAR(a.achievement_date)) % 5 = 0
                                    AND (@currentYear - YEAR(a.achievement_date)) > 0";

                var parameters = new[]
                {
                    new MySqlParameter("@username", username),
                    new MySqlParameter("@currentYear", currentYear)
                };

                using var dt = await _db.ExecuteQuery(sql, parameters);
                var cards = new List<AchievementCard>();

                foreach (DataRow row in dt.Rows)
                {
                    var card = new AchievementCard
                    {
                        AchievementId = Convert.ToInt32(row["achievement_id"]),
                        CategoryId = Convert.ToInt32(row["category_id"]),
                        AchievementName = row["achievement_name"] != DBNull.Value ? row["achievement_name"].ToString() : string.Empty,
                        Description = row["description"] != DBNull.Value ? row["description"].ToString() : string.Empty,
                        AchievementDate = Convert.ToDateTime(row["achievement_date"]),
                        CompletionDegree = row["completion_degree"] != DBNull.Value ? row["completion_degree"].ToString() : string.Empty,
                        Score = Convert.ToDecimal(row["score"]),
                        AnniversaryYears = row["AnniversaryYears"] != DBNull.Value ? Convert.ToInt32(row["AnniversaryYears"]) : 0
                    };

                    card.ImagePath = GetAchievementImagePath(card.AchievementName);
                    cards.Add(card);
                }

                return cards;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"获取周年成就失败: {ex.Message}");
                return [];
            }
        }

        //插入成就字段
        private static string BuildInsertSql()
        {
            string achievementsTable = DatabaseTableManager.GetAchievementsTable();
            return $@"INSERT INTO {achievementsTable} 
                    (user_id, category_id, achievement_name, description, achievement_date, completion_degree, score)
                 VALUES 
                    (@userId, @categoryId, @achievementName, @description, @achievementDate, @completionDegree, @score)";
        }

        //更新成就字段
        private static string BuildUpdateSql()
        {
            string achievementsTable = DatabaseTableManager.GetAchievementsTable();
            return $@"UPDATE {achievementsTable} 
                         SET category_id = @categoryId,
                             achievement_name = @achievementName,
                             description = @description,
                             achievement_date = @achievementDate,
                             completion_degree = @completionDegree,
                             score = @score
                         WHERE achievement_id = @achievementId";
        }

        //保存成就
        public async Task<bool> SaveAchievementAsync(AchievementCard achievement, string? username, string? bigCategory, string? smallCategory)
        {
            try
            {
                // 1. 验证输入
                ArgumentNullException.ThrowIfNull(achievement);

                if (string.IsNullOrWhiteSpace(username))
                    throw new ArgumentException("用户名不能为空", nameof(username));

                if (string.IsNullOrWhiteSpace(bigCategory) || string.IsNullOrWhiteSpace(smallCategory))
                    throw new ArgumentException("必须选择成就分类", nameof(bigCategory));

                // 2. 获取用户ID
                int userId = await GetUserIdByUsernameAsync(username);
                if (userId <= 0) return false;

                // 获取类别ID
                int categoryId = await GetCategoryIdAsync(bigCategory, smallCategory);
                if (categoryId <= 0) return false;

                //移除成就名非法字符
                string cleanName = CleanFileName(achievement.AchievementName ?? string.Empty);
                if (string.IsNullOrWhiteSpace(cleanName))
                    throw new ArgumentException("成就名称不能为空", nameof(achievement));

                //处理图片
                if (!string.IsNullOrEmpty(achievement.ImagePath))
                {
                    string extension = Path.GetExtension(achievement.ImagePath);
                    string destPath = Path.Combine(AppConfig.ImagesDirectoryPath, cleanName + extension);

                    // 仅当需要时复制文件
                    if (File.Exists(achievement.ImagePath) &&
                       (achievement.AchievementId == 0 || !destPath.Equals(achievement.ImagePath)))
                    {
                        File.Copy(achievement.ImagePath, destPath, true);
                    }
                }

                // 4. 构建SQL
                string sql = achievement.AchievementId == 0
                    ? BuildInsertSql()
                    : BuildUpdateSql();

                // 5. 准备参数
                var parameters = new List<MySqlParameter>
                {
                    new("@userId", userId),
                    new("@categoryId", categoryId),
                    new("@achievementName", cleanName),
                    new("@description", achievement.Description ?? string.Empty),
                    new("@achievementDate", achievement.AchievementDate),
                    new("@completionDegree", achievement.CompletionDegree ?? string.Empty),
                    new("@score", achievement.Score),
                };

                // 如果是更新，添加achievementId参数
                if (achievement.AchievementId != 0)
                {
                    parameters.Add(new MySqlParameter("@achievementId", achievement.AchievementId));
                }

                // 6. 执行SQL
                int affectedRows = await _db.ExecuteNonQueryAsync(sql, [.. parameters]);
                return affectedRows > 0;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"保存成就失败: {ex.Message}");
                return false;
            }
        }

        //获取用户所有成就
        public async Task<List<AchievementCard>> GetAllAchievementCardsAsync(string username, string? sortField = null, bool isDescending = true)
        {
            try
            {
                // 构建排序子句
                string orderBy = BuildOrderByClause(sortField, isDescending);

                string achievementsTable = DatabaseTableManager.GetAchievementsTable();
                string usersTable = DatabaseTableManager.GetUsersTable();
                string sql = $@"
                                SELECT 
                                    a.achievement_id,
                                    a.category_id,
                                    a.achievement_name, 
                                    a.description, 
                                    a.achievement_date, 
                                    a.completion_degree, 
                                    a.score
                                FROM {achievementsTable} a
                                JOIN {usersTable} u ON a.user_id = u.user_id
                                WHERE u.account = @username
                                ORDER BY {orderBy}";

                var parameter = new MySqlParameter("@username", username);

                using var dt = await _db.ExecuteQuery(sql, parameter);
                var cards = new List<AchievementCard>();

                foreach (DataRow row in dt.Rows)
                {
                    var card = new AchievementCard
                    {
                        AchievementId = Convert.ToInt32(row["achievement_id"]),
                        CategoryId = Convert.ToInt32(row["category_id"]),
                        AchievementName = row["achievement_name"] != DBNull.Value ? row["achievement_name"].ToString() : string.Empty,
                        Description = row["description"] != DBNull.Value ? row["description"].ToString() : string.Empty,
                        AchievementDate = Convert.ToDateTime(row["achievement_date"]),
                        CompletionDegree = row["completion_degree"] != DBNull.Value ? row["completion_degree"].ToString() : string.Empty,
                        Score = Convert.ToDecimal(row["score"])
                    };

                    // 构建图片路径
                    card.ImagePath = GetAchievementImagePath(card.AchievementName);
                    cards.Add(card);
                }

                return cards;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"获取所有成就卡片失败: {ex.Message}");
                return [];
            }
        }

        // 根据用户名获取成就大类类别
        public async Task<List<string>> GetbigCategoriesAsync(string? username = null)
        {
            string sql;
            List<MySqlParameter> parameters = [];

            if (string.IsNullOrWhiteSpace(username))
            {
                // 全局查询 - 获取所有成就类别
                string categoriesTable = DatabaseTableManager.GetCategoriesTable();
                sql = $@"
                        SELECT DISTINCT big_category
                        FROM {categoriesTable}
                        ORDER BY big_category";
            }
            else
            {
                // SQL 查询获取用户的所有成就类别
                string usersTable = DatabaseTableManager.GetUsersTable();
                string achievementsTable = DatabaseTableManager.GetAchievementsTable();
                string categoriesTable = DatabaseTableManager.GetCategoriesTable();
                sql = $@"
                        SELECT DISTINCT ac.big_category
                        FROM {usersTable} u
                        JOIN {achievementsTable} a ON u.user_id = a.user_id
                        JOIN {categoriesTable} ac ON a.category_id = ac.category_id
                        WHERE u.account = @username
                        ORDER BY ac.big_category";

                parameters.Add(new MySqlParameter("@username", username));
            }

            using var dt = await _db.ExecuteQuery(sql, [.. parameters]);
            var categories = new List<string>();
            foreach (DataRow row in dt.Rows)
            {
                categories.Add(row["big_category"].ToString());
            }
            return categories;
        }

        //加载成就小类类别  
        public async Task<List<string>> GetSmallCategoriesAsync(string bigCategory, string? username = null)
        {
            string sql;
            List<MySqlParameter> parameters = [new MySqlParameter("@bigCategory", bigCategory)];

            if (string.IsNullOrWhiteSpace(username))
            {
                // 全局查询 - 获取指定大类下所有小类
                string categoriesTable = DatabaseTableManager.GetCategoriesTable();
                sql = $@"
                        SELECT DISTINCT small_category
                        FROM {categoriesTable}
                        WHERE big_category = @bigCategory
                        ORDER BY small_category";
            }
            else
            {
                // SQL 查询获取指定大类别下的小类别
                string usersTable = DatabaseTableManager.GetUsersTable();
                string achievementsTable = DatabaseTableManager.GetAchievementsTable();
                string categoriesTable = DatabaseTableManager.GetCategoriesTable();
                sql = $@"
                        SELECT DISTINCT ac.small_category
                        FROM {usersTable} u
                        JOIN {achievementsTable} a ON u.user_id = a.user_id
                        JOIN {categoriesTable} ac ON a.category_id = ac.category_id
                        WHERE u.account = @username AND ac.big_category = @bigCategory
                        ORDER BY ac.small_category";

                parameters.Add(new MySqlParameter("@username", username));
            }

            using var dt = await _db.ExecuteQuery(sql, [.. parameters]);
            var smallCategories = new List<string>();
            foreach (DataRow row in dt.Rows)
            {
                smallCategories.Add(row["small_category"].ToString());
            }
            return smallCategories;
        }

        // 构建排序子句
        private static string BuildOrderByClause(string? sortField, bool isDescending)
        {
            if (string.IsNullOrEmpty(sortField))
                return "a.score DESC"; // 默认排序

            string direction = isDescending ? "DESC" : "ASC";

            return sortField switch
            {
                "Name" => $"CONVERT(a.achievement_name USING gbk) {direction}",
                "Rating" => $"a.score {direction}",
                "Progress" => $"LENGTH(a.completion_degree) {direction}",// 根据完成度字符串长度排序
                "Date" => $"a.achievement_date {direction}",
                _ => "a.score DESC",
            };
        }

        //根据用户名和大类获取成就卡片数据
        public async Task<List<AchievementCard>> GetAchievementCardsByBigCategoryAsync(string username, string bigCategory, string? sortField = null, bool isDescending = true)
        {
            try
            {
                // 构建排序子句
                string orderBy = BuildOrderByClause(sortField, isDescending);

                string achievementsTable = DatabaseTableManager.GetAchievementsTable();
                string usersTable = DatabaseTableManager.GetUsersTable();
                string categoriesTable = DatabaseTableManager.GetCategoriesTable();
                string sql = $@"
                            SELECT 
                                a.achievement_id,
                                a.category_id,
                                a.achievement_name, 
                                a.description, 
                                a.achievement_date, 
                                a.completion_degree, 
                                a.score
                            FROM {achievementsTable} a
                            JOIN {usersTable} u ON a.user_id = u.user_id
                            JOIN {categoriesTable} ac ON a.category_id = ac.category_id
                            WHERE u.account = @username 
                            AND ac.big_category = @bigCategory
                            ORDER BY {orderBy}";

                var parameters = new[]
                {
                    new MySqlParameter("@username", username),
                    new MySqlParameter("@bigCategory", bigCategory)
                };

                using var dt = await _db.ExecuteQuery(sql, parameters);
                var cards = new List<AchievementCard>();

                foreach (DataRow row in dt.Rows)
                {
                    var card = new AchievementCard
                    {
                        AchievementId = Convert.ToInt32(row["achievement_id"]),
                        CategoryId = Convert.ToInt32(row["category_id"]),
                        AchievementName = row["achievement_name"] != DBNull.Value ? row["achievement_name"].ToString() : string.Empty,
                        Description = row["description"] != DBNull.Value ? row["description"].ToString() : string.Empty,
                        AchievementDate = Convert.ToDateTime(row["achievement_date"]),
                        CompletionDegree = row["completion_degree"] != DBNull.Value ? row["completion_degree"].ToString() : string.Empty,
                        Score = Convert.ToDecimal(row["score"])
                    };

                    // 构建图片路径
                    card.ImagePath = GetAchievementImagePath(card.AchievementName);
                    cards.Add(card);
                }

                return cards;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"根据大类获取成就卡片失败: {ex.Message}");
                return [];
            }
        }

        // 根据用户名和小类获取成就卡片数据
        public async Task<List<AchievementCard>> GetAchievementCardsAsync(string username, string smallCategory, string? sortField = null, bool isDescending = true)
        {
            try
            {
                // 构建排序子句
                string orderBy = BuildOrderByClause(sortField, isDescending);

                string achievementsTable = DatabaseTableManager.GetAchievementsTable();
                string usersTable = DatabaseTableManager.GetUsersTable();
                string categoriesTable = DatabaseTableManager.GetCategoriesTable();
                string sql = $@"
                SELECT 
                    a.achievement_id,
                    a.category_id,
                    a.achievement_name, 
                    a.description, 
                    a.achievement_date, 
                    a.completion_degree, 
                    a.score
                FROM {achievementsTable} a
                JOIN {usersTable} u ON a.user_id = u.user_id
                JOIN {categoriesTable} ac ON a.category_id = ac.category_id
                WHERE u.account = @username 
                AND ac.small_category = @smallCategory
                ORDER BY {orderBy}";

                var parameters = new[]
                {
                    new MySqlParameter("@username", username),
                    new MySqlParameter("@smallCategory", smallCategory)
                 };

                using var dt = await _db.ExecuteQuery(sql, parameters);
                var cards = new List<AchievementCard>();

                foreach (DataRow row in dt.Rows)
                {
                    var card = new AchievementCard
                    {
                        AchievementId = Convert.ToInt32(row["achievement_id"]),
                        CategoryId = Convert.ToInt32(row["category_id"]),
                        AchievementName = row["achievement_name"] != DBNull.Value ? row["achievement_name"].ToString() : string.Empty,
                        Description = row["description"] != DBNull.Value ? row["description"].ToString() : string.Empty,
                        AchievementDate = Convert.ToDateTime(row["achievement_date"]),
                        CompletionDegree = row["completion_degree"] != DBNull.Value ? row["completion_degree"].ToString() : string.Empty,
                        Score = Convert.ToDecimal(row["score"])
                    };

                    // 构建图片路径
                    card.ImagePath = GetAchievementImagePath(card.AchievementName);

                    cards.Add(card);
                }

                return cards;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"获取成就卡片失败: {ex.Message}");
                return [];
            }
        }

        // 初始化图片缓存
        private static void InitializeImageCache()
        {
            // 双重检查锁定，确保只初始化一次
            if (!_imageCacheInitialized)
            {
                lock (_cacheLock)
                {
                    if (!_imageCacheInitialized)
                    {
                        // 在后台线程中初始化缓存，避免阻塞主线程
                        Task.Run(() =>
                        {
                            try
                            {
                                string imagesDir = AppConfig.ImagesDirectoryPath;
                                if (Directory.Exists(imagesDir))
                                {
                                    // 枚举所有图片文件
                                    var imageFiles = Directory.EnumerateFiles(imagesDir);
                                    foreach (var file in imageFiles)
                                    {
                                        // 获取文件名（不含扩展名）作为缓存键
                                        string fileName = Path.GetFileNameWithoutExtension(file);
                                        lock (_cacheLock)
                                        {
                                            if (!_imagePathCache.ContainsKey(fileName))
                                            {
                                                _imagePathCache[fileName] = file;
                                            }
                                        }
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                Debug.WriteLine($"初始化图片缓存失败: {ex.Message}");
                            }
                            finally
                            {
                                _imageCacheInitialized = true;
                            }
                        });
                    }
                }
            }
        }

        // 获取成就图片路径
        private static string GetAchievementImagePath(string? achievementName)
        {
            // 确保缓存已初始化
            InitializeImageCache();

            // 清理成就名称（移除非法字符）
            string cleanName = CleanFileName(achievementName ?? string.Empty);

            // 如果成就名称为空，返回默认图片
            if (string.IsNullOrWhiteSpace(cleanName))
                return AppConfig.DefaultImagePath;

            // 先检查缓存
            lock (_cacheLock)
            {
                if (_imagePathCache.TryGetValue(cleanName, out string cachedPath))
                {
                    // 缓存命中，直接返回
                    return cachedPath;
                }
            }

            // 缓存未命中，查找图片
            string? imagePath = FindMatchingImage(cleanName);
            string finalPath = !string.IsNullOrWhiteSpace(imagePath)
                ? imagePath
                : AppConfig.DefaultImagePath;

            // 将结果存入缓存
            lock (_cacheLock)
            {
                if (!_imagePathCache.ContainsKey(cleanName))
                {
                    _imagePathCache[cleanName] = finalPath;
                }
            }

            return finalPath;
        }

        // 查找匹配的成就图片（支持多种格式）
        private static string? FindMatchingImage(string baseName)
        {
            try
            {
                string imagesDir = AppConfig.ImagesDirectoryPath;

                // 确保目录存在
                if (!Directory.Exists(imagesDir))
                    return null;

                // 获取目录中所有文件（包括一级目录和二级目录）
                var imageFiles = Directory.EnumerateFiles(imagesDir, "*.*", SearchOption.AllDirectories);

                // 查找匹配的文件（忽略大小写）
                foreach (var file in imageFiles)
                {
                    // 提取文件名（不含扩展名）
                    string fileName = Path.GetFileNameWithoutExtension(file);

                    // 检查文件名是否匹配（不区分大小写）
                    if (fileName.Equals(baseName, StringComparison.OrdinalIgnoreCase))
                    {
                        // 检查文件扩展名是否支持
                        string extension = Path.GetExtension(file).ToLowerInvariant();
                        if (AppConfig.SupportedImageExtensions.Contains(extension))
                        {
                            return file;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"查找成就图片时出错: {ex.Message}");
            }

            return null;
        }

        // 成就名非法字符串清理（优化处理）
        private static string CleanFileName(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
                return string.Empty;

            // 定义允许保留的特殊字符
            var allowedSpecialChars = new HashSet<char>
                {
                     '，', '·', '-', '|', '!', '：', '(', ')', '《', '》', '[', ']', '【', '】'
                };

            // 创建合法文件名
            _ = Path.GetInvalidFileNameChars();
            StringBuilder cleanName = new();

            foreach (char c in fileName)
            {
                // 检查是否允许的字符：汉字、字母、数字、下划线或指定特殊字符
                if (char.IsLetterOrDigit(c) || c == '_' || (c > 0x4E00 && c < 0x9FFF) || allowedSpecialChars.Contains(c))
                {
                    cleanName.Append(c);
                }
                // 空格替换为下划线
                else if (c == ' ')
                {
                    cleanName.Append('_');
                }
                // 其他字符转换为安全字符（Unicode替代）
                else
                {
                    cleanName.Append('$').Append(((int)c).ToString("X4"));
                }
            }

            return cleanName.ToString();
        }

        // 删除用户成就（需要用户名和成就ID双重验证）
        public async Task<bool> DeleteAchievementAsync(string username, int achievementId)
        {
            // 参数验证
            if (string.IsNullOrWhiteSpace(username))
                throw new ArgumentException("用户名不能为空", nameof(username));

            if (achievementId <= 0)
                throw new ArgumentException("无效的成就ID", nameof(achievementId));

            try
            {
                // 双重验证SQL：确保成就属于指定用户
                string achievementsTable = DatabaseTableManager.GetAchievementsTable();
                string usersTable = DatabaseTableManager.GetUsersTable();
                string sql = $@"
                            DELETE a
                            FROM {achievementsTable} a
                            JOIN {usersTable} u ON a.user_id = u.user_id
                            WHERE u.account = @username 
                            AND a.achievement_id = @achievementId";

                var parameters = new[]
                {
                    new MySqlParameter("@username", username),
                    new MySqlParameter("@achievementId", achievementId)
                };

                // 执行删除并返回操作结果
                int affectedRows = await _db.ExecuteNonQueryAsync(sql, parameters);
                return affectedRows > 0;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"删除成就失败: {ex.Message}");
                return false;
            }
        }

        //搜索成就功能
        public async Task<List<AchievementCard>> SearchAchievementsByNameAsync(string userName, string searchText, string? sortField = null, bool isDescending = true)
        {
            try
            {
                // 构建排序子句
                string orderBy = BuildOrderByClause(sortField, isDescending);

                // SQL 查询使用参数化查询防止SQL注入
                string achievementsTable = DatabaseTableManager.GetAchievementsTable();
                string usersTable = DatabaseTableManager.GetUsersTable();
                string sql = $@"
                                SELECT 
                                    a.achievement_id,
                                    a.category_id,
                                    a.achievement_name, 
                                    a.description, 
                                    a.achievement_date, 
                                    a.completion_degree, 
                                    a.score
                                FROM {achievementsTable} a
                                JOIN {usersTable} u ON a.user_id = u.user_id
                                WHERE u.account = @username 
                                AND a.achievement_name LIKE CONCAT('%', @searchText, '%')
                                ORDER BY {orderBy}";

                var parameters = new[]
                {
                    new MySqlParameter("@username", userName),
                    new MySqlParameter("@searchText", searchText)
                };

                using var dt = await _db.ExecuteQuery(sql, parameters);
                var cards = new List<AchievementCard>();

                foreach (DataRow row in dt.Rows)
                {
                    var card = new AchievementCard
                    {
                        AchievementId = Convert.ToInt32(row["achievement_id"]),
                        CategoryId = Convert.ToInt32(row["category_id"]),
                        AchievementName = row["achievement_name"].ToString(),
                        Description = row["description"].ToString(),
                        AchievementDate = Convert.ToDateTime(row["achievement_date"]),
                        CompletionDegree = row["completion_degree"].ToString(),
                        Score = Convert.ToDecimal(row["score"])
                    };

                    // 构建图片路径
                    card.ImagePath = GetAchievementImagePath(card.AchievementName);
                    cards.Add(card);
                }

                return cards;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"搜索成就失败: {ex.Message}");
                return [];
            }
        }

        //添加类别
        public async Task<int> AddCategoryAsync(string bigCategory, string smallCategory)
        {
            try
            {
                // 检查类别是否已存在
                string categoriesTable = DatabaseTableManager.GetCategoriesTable();
                string checkSql = $@"SELECT COUNT(*) 
                                       FROM {categoriesTable} 
                                       WHERE big_category = @bigCategory 
                                       AND small_category = @smallCategory";

                var checkParams = new[]
                {
                    new MySqlParameter("@bigCategory", bigCategory),
                    new MySqlParameter("@smallCategory", smallCategory)
                };

                int count = Convert.ToInt32(await _db.ExecuteScalarAsync(checkSql, checkParams));
                if (count > 0) return -1; // 类别已存在

                // 插入新类别
                string insertSql = $@"INSERT INTO {categoriesTable} (big_category, small_category) 
                                    VALUES (@bigCategory, @smallCategory)";

                int affectedRows = await _db.ExecuteNonQueryAsync(insertSql, checkParams);
                if (affectedRows > 0)
                {
                    insertSql = "SELECT LAST_INSERT_ID()";
                    object result = await _db.ExecuteScalarAsync(insertSql);
                    return result != null ? Convert.ToInt32(result) : 0;
                }

                return 0; // 插入失败
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"添加类别失败: {ex.Message}");
                return 0;
            }
        }

        //删除类别
        public async Task<bool> DeleteCategoryAsync(int categoryId)
        {
            try
            {
                string categoriesTable = DatabaseTableManager.GetCategoriesTable();
                string sql = $"DELETE FROM {categoriesTable} WHERE category_id = @categoryId";
                var param = new MySqlParameter("@categoryId", categoryId);
                return await _db.ExecuteNonQueryAsync(sql, param) > 0;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"删除类别失败: {ex.Message}");
                return false;
            }
        }

        //加载类别树
        public async Task<List<CategoryItem>> GetAllCategoriesAsync(string? username = null)
        {
            try
            {
                // 构建基本SQL查询
                string categoriesTable = DatabaseTableManager.GetCategoriesTable();
                string achievementsTable = DatabaseTableManager.GetAchievementsTable();
                var sql = new StringBuilder($@"
                                                SELECT 
                                                    ac.category_id,
                                                    ac.big_category,
                                                    ac.small_category,
                                                    CAST(COUNT(a.achievement_id) AS UNSIGNED) AS achievement_count
                                                FROM {categoriesTable} ac
                                                LEFT JOIN {achievementsTable} a ON ac.category_id = a.category_id
                                            ");

                var parameters = new List<MySqlParameter>();

                // 如果有用户名，添加用户过滤条件
                if (!string.IsNullOrWhiteSpace(username))
                {
                    string usersTable = DatabaseTableManager.GetUsersTable();
                    sql.Append($@"
                                    LEFT JOIN {usersTable} u ON a.user_id = u.user_id
                                    WHERE u.account = @username OR a.user_id IS NULL
                                    ");
                    parameters.Add(new MySqlParameter("@username", username));
                }

                sql.Append(@"
                                GROUP BY ac.category_id, ac.big_category, ac.small_category
                                ORDER BY ac.big_category, ac.small_category
                            ");

                using var dt = await _db.ExecuteQuery(sql.ToString(), [.. parameters]);
                // 按大类分组
                var bigCategoryGroups = dt.AsEnumerable()
                    .GroupBy(row => row["big_category"] != DBNull.Value ? row["big_category"].ToString() : string.Empty)
                    .ToList();

                var categoryList = new List<CategoryItem>();

                foreach (var bigGroup in bigCategoryGroups)
                {
                    // 创建大类节点
                    var bigCategoryItem = new CategoryItem
                    {
                        Id = -1, // 大类没有独立ID
                        BigCategory = bigGroup.Key,
                        Type = CategoryType.BigCategory,
                        AchievementCount = bigGroup.Sum(row => row["achievement_count"] != DBNull.Value ? Convert.ToInt32(row["achievement_count"]) : 0)
                    };

                    // 处理当前大类下的所有小类
                    foreach (var row in bigGroup)
                    {
                        var smallCategoryItem = new CategoryItem
                        {
                            Id = row["category_id"] != DBNull.Value ? Convert.ToInt32(row["category_id"]) : 0,
                            BigCategory = bigGroup.Key,
                            SmallCategory = row["small_category"] != DBNull.Value ? row["small_category"].ToString() : string.Empty,
                            Type = CategoryType.SmallCategory,
                            AchievementCount = row["achievement_count"] != DBNull.Value ? Convert.ToInt32(row["achievement_count"]) : 0
                        };

                        bigCategoryItem.Subcategories.Add(smallCategoryItem);
                    }

                    categoryList.Add(bigCategoryItem);
                }

                return categoryList;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"获取类别树失败: {ex.Message}");
                return [];
            }
        }


        //修改类别名称
        public async Task<bool> UpdateCategoryNameAsync(int categoryId, string newName, CategoryType categoryType, string oldName = "")
        {
            try
            {
                string categoriesTable = DatabaseTableManager.GetCategoriesTable();
                string fieldName = categoryType == CategoryType.BigCategory ? "big_category" : "small_category";
                string sql;
                MySqlParameter[] parameters;

                if (categoryType == CategoryType.BigCategory)
                {
                    // 修改大类名称时，更新该大类下所有小类的 big_category 字段
                    sql = $"UPDATE {categoriesTable} SET big_category = @newName WHERE big_category = @oldName";
                    parameters =
                    [
                        new MySqlParameter("@newName", newName),
                        new MySqlParameter("@oldName", oldName)
                    ];
                }
                else
                {
                    // 修改小类名称时，只更新单个类别的 small_category 字段
                    sql = $"UPDATE {categoriesTable} SET small_category = @newName WHERE category_id = @categoryId";
                    parameters =
                    [
                        new MySqlParameter("@newName", newName),
                        new MySqlParameter("@categoryId", categoryId)
                    ];
                }

                return await _db.ExecuteNonQueryAsync(sql, parameters) > 0;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"修改类别名称失败: {ex.Message}");
                return false;
            }
        }

        //移动大类下所有小类到目标大类
        public async Task<bool> MoveCategoriesToBigCategoryAsync(string sourceBigCategory, string targetBigCategory)
        {
            try
            {
                string categoriesTable = DatabaseTableManager.GetCategoriesTable();
                string sql = $"UPDATE {categoriesTable} SET big_category = @targetBigCategory WHERE big_category = @sourceBigCategory";

                var parameters = new[]
                {
                    new MySqlParameter("@targetBigCategory", targetBigCategory),
                    new MySqlParameter("@sourceBigCategory", sourceBigCategory)
                };

                return await _db.ExecuteNonQueryAsync(sql, parameters) > 0;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"移动类别失败: {ex.Message}");
                return false;
            }
        }

        //移动小类到目标大类
        public async Task<bool> MoveCategoryToBigCategoryAsync(int categoryId, string targetBigCategory)
        {
            try
            {
                string categoriesTable = DatabaseTableManager.GetCategoriesTable();
                string sql = $"UPDATE {categoriesTable} SET big_category = @targetBigCategory WHERE category_id = @categoryId";

                var parameters = new[]
                {
                    new MySqlParameter("@targetBigCategory", targetBigCategory),
                    new MySqlParameter("@categoryId", categoryId)
                };

                return await _db.ExecuteNonQueryAsync(sql, parameters) > 0;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"移动类别失败: {ex.Message}");
                return false;
            }
        }

        //移动大类下所有小类的数据到目标小类别
        public async Task<bool> MoveBigCategoryDataToSmallCategoryAsync(string sourceBigCategory, string targetBigCategory, string targetSmallCategory)
        {
            try
            {
                // 获取目标小类的ID
                int targetCategoryId = await GetCategoryIdAsync(targetBigCategory, targetSmallCategory);
                if (targetCategoryId <= 0) return false;

                // 获取源大类下所有小类的ID
                string categoriesTable = DatabaseTableManager.GetCategoriesTable();
                string sql = $"SELECT category_id FROM {categoriesTable} WHERE big_category = @sourceBigCategory";
                var parameter = new MySqlParameter("@sourceBigCategory", sourceBigCategory);

                using var dt = await _db.ExecuteQuery(sql, parameter);
                var sourceCategoryIds = new List<int>();
                foreach (DataRow row in dt.Rows)
                {
                    sourceCategoryIds.Add(Convert.ToInt32(row["category_id"]));
                }

                if (sourceCategoryIds.Count == 0) return false;

                // 更新成就的类别ID
                string achievementsTable = DatabaseTableManager.GetAchievementsTable();
                string updateSql = $"UPDATE {achievementsTable} SET category_id = @targetCategoryId WHERE category_id IN (@sourceCategoryIds)";

                // 构建IN参数
                var updateParameters = new List<MySqlParameter>
                {
                    new("@targetCategoryId", targetCategoryId)
                };

                // 对于IN子句，需要使用MySql的方式处理
                string inClause = string.Join(",", sourceCategoryIds);
                updateSql = updateSql.Replace("@sourceCategoryIds", inClause);

                return await _db.ExecuteNonQueryAsync(updateSql, [.. updateParameters]) > 0;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"移动类别数据失败: {ex.Message}");
                return false;
            }
        }

        //移动小类下的数据到目标小类别
        public async Task<bool> MoveSmallCategoryDataToSmallCategoryAsync(int sourceCategoryId, string targetBigCategory, string targetSmallCategory)
        {
            try
            {
                // 获取目标小类的ID
                int targetCategoryId = await GetCategoryIdAsync(targetBigCategory, targetSmallCategory);
                if (targetCategoryId <= 0) return false;

                // 更新成就的类别ID
                string achievementsTable = DatabaseTableManager.GetAchievementsTable();
                string sql = $"UPDATE {achievementsTable} SET category_id = @targetCategoryId WHERE category_id = @sourceCategoryId";

                var parameters = new[]
                {
                    new MySqlParameter("@targetCategoryId", targetCategoryId),
                    new MySqlParameter("@sourceCategoryId", sourceCategoryId)
                };

                return await _db.ExecuteNonQueryAsync(sql, parameters) > 0;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"移动类别数据失败: {ex.Message}");
                return false;
            }
        }

        //删除大类下所有小类的成就数据
        public async Task<bool> DeleteAchievementsByBigCategoryAsync(string bigCategory)
        {
            try
            {
                // 获取大类下所有小类的ID
                string categoriesTable = DatabaseTableManager.GetCategoriesTable();
                string sql = $"SELECT category_id FROM {categoriesTable} WHERE big_category = @bigCategory";
                var parameter = new MySqlParameter("@bigCategory", bigCategory);

                using var dt = await _db.ExecuteQuery(sql, parameter);
                var categoryIds = new List<int>();
                foreach (DataRow row in dt.Rows)
                {
                    categoryIds.Add(Convert.ToInt32(row["category_id"]));
                }

                if (categoryIds.Count == 0) return true;

                // 删除成就数据
                string achievementsTable = DatabaseTableManager.GetAchievementsTable();
                string deleteSql = $"DELETE FROM {achievementsTable} WHERE category_id IN (@categoryIds)";

                // 构建IN参数
                string inClause = string.Join(",", categoryIds);
                deleteSql = deleteSql.Replace("@categoryIds", inClause);

                return await _db.ExecuteNonQueryAsync(deleteSql) > 0;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"删除类别数据失败: {ex.Message}");
                return false;
            }
        }

        //删除小类下的成就数据
        public async Task<bool> DeleteAchievementsBySmallCategoryAsync(int categoryId)
        {
            try
            {
                string achievementsTable = DatabaseTableManager.GetAchievementsTable();
                string sql = $"DELETE FROM {achievementsTable} WHERE category_id = @categoryId";
                var parameter = new MySqlParameter("@categoryId", categoryId);

                return await _db.ExecuteNonQueryAsync(sql, parameter) > 0;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"删除类别数据失败: {ex.Message}");
                return false;
            }
        }

        //数据库资源清理
        public void Dispose()
        {
            _db.Dispose();
        }
    }
}
