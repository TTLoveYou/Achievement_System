namespace 成就系统.Utilities
{
    /// <summary>
    /// 数据库表名管理器，用于获取配置的自定义表名
    /// </summary>
    public static class DatabaseTableManager
    {
        private static Services.ThemeSettings.DatabaseConfig? _dbConfig;
        private static bool _initialized;

        /// <summary>
        /// 初始化表名管理器
        /// </summary>
        private static void Initialize()
        {
            if (!_initialized)
            {
                _dbConfig = Services.ThemeSettings.LoadDatabaseConfig();
                _initialized = true;
            }
        }

        /// <summary>
        /// 获取用户表名
        /// </summary>
        /// <returns>用户表名</returns>
        public static string GetUsersTable()
        {
            Initialize();
            return _dbConfig.UsersTable ?? "users";
        }

        /// <summary>
        /// 获取成就分类表名
        /// </summary>
        /// <returns>成就分类表名</returns>
        public static string GetCategoriesTable()
        {
            Initialize();
            return _dbConfig.CategoriesTable ?? "achievement_categories";
        }

        /// <summary>
        /// 获取成就表名
        /// </summary>
        /// <returns>成就表名</returns>
        public static string GetAchievementsTable()
        {
            Initialize();
            return _dbConfig.AchievementsTable ?? "achievements";
        }

        /// <summary>
        /// 重置初始化状态，强制下次获取表名时重新加载配置
        /// </summary>
        public static void Reset()
        {
            _initialized = false;
        }
    }
}