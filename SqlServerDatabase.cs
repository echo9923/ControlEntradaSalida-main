using System;
using System.Data;
using System.Data.SqlClient;
using System.Threading;

namespace ControlEntradaSalida
{
    /// <summary>
    /// SQL Server 数据库访问帮助类，提供连接、命令创建与资源释放。
    /// </summary>
    public sealed class SqlServerDatabase : IDisposable
    {
        public SqlConnection Connection { get; private set; }

        public string ErrorMessage { get; private set; }

        public string ErrorNumber { get; private set; }

        public int CommandTimeoutSeconds { get; }

        public SqlServerDatabase(int commandTimeoutSeconds)
        {
            CommandTimeoutSeconds = commandTimeoutSeconds > 0 ? commandTimeoutSeconds : 30;
        }

        public void Connect(string connectionString, int maxRetryCount = 10, int delayMilliseconds = 60000)
        {
            ErrorMessage = null;
            ErrorNumber = null;
            int totalAttempts = Math.Max(1, maxRetryCount);
            int retryDelay = delayMilliseconds < 0 ? 0 : delayMilliseconds;

            for (int attempt = 1; attempt <= totalAttempts; attempt++)
            {
                SqlConnection connection = null;

                try
                {
                    connection = new SqlConnection(connectionString);
                    connection.Open();
                    Connection = connection;

                    if (attempt > 1)
                    {
                        ServiceLogger.Info($"数据库连接在第 {attempt} 次尝试后成功。");
                    }

                    return;
                }
                catch (SqlException ex)
                {
                    ErrorMessage = ex.Message;
                    ErrorNumber = ex.Number.ToString();
                    LogConnectionFailure("SQL 异常", ex, attempt, totalAttempts);
                    DisposeConnection(connection);
                    Connection = null;
                }
                catch (Exception ex)
                {
                    ErrorMessage = ex.Message;
                    ErrorNumber = null;
                    LogConnectionFailure("非 SQL 异常", ex, attempt, totalAttempts);
                    DisposeConnection(connection);
                    Connection = null;
                }

                if (attempt < totalAttempts)
                {
                    if (retryDelay > 0)
                    {
                        ServiceLogger.Warn($"数据库连接失败，将在 {retryDelay} 毫秒后进行第 {attempt + 1} 次重试。");
                        Thread.Sleep(retryDelay);
                    }
                    else
                    {
                        ServiceLogger.Warn($"数据库连接失败，正在立即进行第 {attempt + 1} 次重试。");
                    }
                }
            }
        }

        private static void DisposeConnection(SqlConnection connection)
        {
            if (connection == null)
            {
                return;
            }

            try
            {
                connection.Dispose();
            }
            catch
            {
                // 忽略释放阶段的异常
            }
        }

        private static void LogConnectionFailure(string category, Exception ex, int attempt, int maxAttempts)
        {
            string attemptInfo = maxAttempts > 1 ? $"（第 {attempt}/{maxAttempts} 次尝试）" : string.Empty;
            string message = $"数据库连接失败{attemptInfo}（{category}）: {ex.Message}";
            ServiceLogger.Error(message, ex);
        }

        public SqlCommand CreateCommand(string sql)
        {
            if (Connection == null)
            {
                throw new InvalidOperationException("SQL Server 连接尚未建立，请先调用 Connect。");
            }

            SqlCommand command = Connection.CreateCommand();
            command.CommandText = sql;
            command.CommandTimeout = CommandTimeoutSeconds;
            return command;
        }

        public void Disconnect()
        {
            if (Connection == null)
            {
                return;
            }

            if (Connection.State != ConnectionState.Closed)
            {
                Connection.Close();
            }

            Connection.Dispose();
            Connection = null;
        }

        public void Dispose()
        {
            Disconnect();
        }
    }
}
