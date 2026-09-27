using MySql.Data.MySqlClient;
using System.Configuration;
using System.Data;

namespace 成就系统.Utilities
{
    public class DatabaseHelper : IDisposable
    {
        // 数据库连接池：键为连接字符串，值为数据库连接
        private static readonly Dictionary<string, MySqlConnection> _connectionPool = [];
        // 连接池线程安全锁
        private static readonly Lock _poolLock = new();
        // 当前使用的连接
        private MySqlConnection? _currentConnection = null;
        // 连接字符串
        private readonly string _connectionString;

        public DatabaseHelper()
        {
            try
            {
                // 从主题设置中加载数据库配置
                var dbConfig = Services.ThemeSettings.LoadDatabaseConfig();
                _connectionString = dbConfig.GetConnectionString();
            }
            catch (Exception)
            {
                // 发生错误时，尝试从配置文件读取
                var conn = ConfigurationManager.ConnectionStrings["MySQLConnection"] ?? throw new ConfigurationErrorsException("未找到MySQL连接字符串配置");
                _connectionString = conn.ConnectionString;
            }
        }

        // 从连接池获取连接
        private MySqlConnection GetConnection()
        {
            lock (_poolLock)
            {
                // 尝试从连接池获取连接
                if (_connectionPool.TryGetValue(_connectionString, out MySqlConnection? pooledConn))
                {
                    // 检查连接状态
                    if (pooledConn != null && pooledConn.State == ConnectionState.Open)
                    {
                        // 从池中移除并返回
                        _connectionPool.Remove(_connectionString);
                        _currentConnection = pooledConn;
                        return pooledConn;
                    }
                    else
                    {
                        // 连接已关闭或为null，移除并创建新连接
                        _connectionPool.Remove(_connectionString);
                    }
                }
            }

            // 连接池无可用连接，创建新连接
            _currentConnection = new MySqlConnection(_connectionString);
            return _currentConnection;
        }

        // 归还连接到连接池
        private void ReturnConnection(MySqlConnection? connection)
        {
            if (connection == null) return;

            lock (_poolLock)
            {
                // 检查连接状态，如果是打开的则归还到池中
                if (connection.State == ConnectionState.Open)
                {
                    // 确保池中没有相同连接字符串的连接
                    if (_connectionPool.TryGetValue(_connectionString, out MySqlConnection? value))
                    {
                        // 关闭旧连接
                        try
                        {
                            value?.Close();
                        }
                        catch { }
                    }
                    // 归还新连接
                    _connectionPool[_connectionString] = connection;
                }
                else
                {
                    // 连接已关闭，直接销毁
                    connection.Dispose();
                }
            }
        }

        /// <summary>
        /// 执行查询返回DataTable
        /// </summary>
        public async Task<DataTable> ExecuteQuery(string sql, params MySqlParameter[] parameters)
        {
            ValidateSql(sql);
            MySqlConnection? conn = null;

            try
            {
                // 从连接池获取连接
                conn = GetConnection();

                // 确保连接是打开的
                if (conn.State != ConnectionState.Open)
                {
                    await conn.OpenAsync();
                }

                using var cmd = new MySqlCommand(sql, conn);
                if (parameters != null && parameters.Length > 0)
                {
                    cmd.Parameters.AddRange(parameters);
                }

                using var reader = await cmd.ExecuteReaderAsync();
                var dt = new DataTable();
                dt.Load(reader);
                return dt;
            }
            finally
            {
                // 归还连接到连接池
                if (conn != null)
                {
                    ReturnConnection(conn);
                }
            }
        }

        /// <summary>
        /// 执行非查询操作（增删改）
        /// </summary>
        public async Task<int> ExecuteNonQueryAsync(string sql, params MySqlParameter[] parameters)
        {
            ValidateSql(sql);
            MySqlConnection? conn = null;

            try
            {
                // 从连接池获取连接
                conn = GetConnection();

                // 确保连接是打开的
                if (conn.State != ConnectionState.Open)
                {
                    await conn.OpenAsync();
                }

                using var cmd = new MySqlCommand(sql, conn);
                if (parameters != null && parameters.Length > 0)
                {
                    cmd.Parameters.AddRange(parameters);
                }
                return await cmd.ExecuteNonQueryAsync();
            }
            finally
            {
                // 归还连接到连接池
                if (conn != null)
                {
                    ReturnConnection(conn);
                }
            }
        }

        /// <summary>
        /// 执行查询返回首行首列
        /// </summary>
        public async Task<object?> ExecuteScalarAsync(string? sql, params MySqlParameter[] parameters)
        {
            if (string.IsNullOrWhiteSpace(sql))
                throw new ArgumentException("SQL语句不能为空");
            ValidateSql(sql!);

            MySqlConnection? conn = null;

            try
            {
                // 从连接池获取连接
                conn = GetConnection();

                // 确保连接是打开的
                if (conn.State != ConnectionState.Open)
                {
                    await conn.OpenAsync();
                }

                using var cmd = new MySqlCommand(sql, conn);
                if (parameters != null && parameters.Length > 0)
                {
                    cmd.Parameters.AddRange(parameters);
                }
                return await cmd.ExecuteScalarAsync();
            }
            finally
            {
                // 归还连接到连接池
                if (conn != null)
                {
                    ReturnConnection(conn);
                }
            }
        }

        //数据库防注入检查
        private static void ValidateSql(string? sql)
        {
            if (sql != null && (sql.Contains(';') || sql.Contains("--") || sql.Contains("/*")))
            {
                throw new ArgumentException("检测到潜在的危险SQL语句");
            }
        }

        // 实现IDisposable接口
        public void Dispose()
        {
            // 清理当前使用的连接
            if (_currentConnection != null)
            {
                try
                {
                    // 检查连接状态
                    if (_currentConnection.State == ConnectionState.Open)
                    {
                        // 尝试归还到连接池
                        ReturnConnection(_currentConnection);
                    }
                    else
                    {
                        // 连接已关闭，直接销毁
                        _currentConnection.Dispose();
                    }
                }
                catch { }
                finally
                {
                    _currentConnection = null;
                }
            }
        }
    }
}
