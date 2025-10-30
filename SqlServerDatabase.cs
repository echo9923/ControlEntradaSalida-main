using System;
using System.Data;
using System.Data.SqlClient;

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

        public void Connect(string connectionString)
        {
            Connection = new SqlConnection(connectionString);
            try
            {
                Connection.Open();
            }
            catch (SqlException ex)
            {
                ErrorMessage = ex.Message;
                ErrorNumber = ex.Number.ToString();
                Connection = null;
            }
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
