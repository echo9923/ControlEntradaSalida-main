using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;

namespace ControlEntradaSalida
{
    /// <summary>
    /// 简单的线程安全文件日志记录器，用于替代原有的控制台输出。
    /// </summary>
    public static class ServiceLogger
    {
        private static readonly ReaderWriterLockSlim LogLock = new ReaderWriterLockSlim();
        private const int DefaultLogRetentionDays = 90;
        private static string logDirectory;
        private static string currentLogFile;
        private static DateTime currentLogDate = DateTime.MinValue;
        private static int logRetentionDays = DefaultLogRetentionDays;

        public static void Initialize(string directory, int retentionDays = DefaultLogRetentionDays)
        {
            if (string.IsNullOrWhiteSpace(directory))
            {
                directory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs");
            }

            logRetentionDays = retentionDays > 0 ? retentionDays : DefaultLogRetentionDays;
            logDirectory = directory;
            Directory.CreateDirectory(logDirectory);
            UpdateLogFilePath();
        }

        public static void Info(string message)
        {
            Write("INFO", message);
        }

        public static void Warn(string message)
        {
            Write("WARN", message);
        }

        public static void Error(string message, Exception ex = null)
        {
            var builder = new StringBuilder(message);
            if (ex != null)
            {
                builder.Append(" | 异常: ").Append(ex);
            }
            Write("ERROR", builder.ToString());
        }

        public static void Debug(string message)
        {
            Write("DEBUG", message);
        }

        private static void Write(string level, string message)
        {
            if (string.IsNullOrEmpty(currentLogFile))
            {
                Initialize(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs"));
            }

            EnsureLogFileForToday();

            string line = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} [{level}] {message}";

            try
            {
                LogLock.EnterWriteLock();
                File.AppendAllText(currentLogFile, line + Environment.NewLine, Encoding.UTF8);
            }
            catch (Exception ex)
            {
                Trace.TraceError($"写入日志失败: {ex}");
            }
            finally
            {
                if (LogLock.IsWriteLockHeld)
                {
                    LogLock.ExitWriteLock();
                }
            }
        }

        private static void EnsureLogFileForToday()
        {
            if (currentLogDate != DateTime.Today || string.IsNullOrEmpty(currentLogFile))
            {
                UpdateLogFilePath();
            }
        }

        private static void UpdateLogFilePath()
        {
            currentLogDate = DateTime.Today;
            string fileName = $"{currentLogDate:yyyy-MM-dd}.log";
            currentLogFile = Path.Combine(logDirectory, fileName);
            CleanupOldLogs();
            EnsureLogFileExists();
        }

        private static void EnsureLogFileExists()
        {
            if (string.IsNullOrEmpty(currentLogFile))
            {
                return;
            }

            try
            {
                if (!File.Exists(currentLogFile))
                {
                    using (File.Create(currentLogFile))
                    {
                    }
                }
            }
            catch (Exception ex)
            {
                Trace.TraceError($"创建日志文件失败: {ex}");
            }
        }

        private static void CleanupOldLogs()
        {
            if (logRetentionDays <= 0)
            {
                return;
            }

            if (string.IsNullOrEmpty(logDirectory) || !Directory.Exists(logDirectory))
            {
                return;
            }

            DateTime cutoffDate = DateTime.Today.AddDays(-logRetentionDays);

            try
            {
                foreach (string file in Directory.GetFiles(logDirectory, "*.log", SearchOption.TopDirectoryOnly))
                {
                    if (!TryParseLogDate(file, out DateTime fileDate))
                    {
                        continue;
                    }

                    if (fileDate < cutoffDate)
                    {
                        try
                        {
                            File.Delete(file);
                        }
                        catch (Exception ex)
                        {
                            Trace.TraceError($"删除过期日志失败: {file} | {ex}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Trace.TraceError($"执行日志清理时失败: {ex}");
            }
        }

        private static bool TryParseLogDate(string filePath, out DateTime date)
        {
            string fileName = Path.GetFileNameWithoutExtension(filePath);

            if (DateTime.TryParseExact(
                    fileName,
                    "yyyy-MM-dd",
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out date))
            {
                return true;
            }

            const string legacyPrefix = "ControlEntradaSalida_";
            if (fileName.StartsWith(legacyPrefix, StringComparison.OrdinalIgnoreCase))
            {
                string legacyDatePart = fileName.Substring(legacyPrefix.Length);
                if (DateTime.TryParseExact(
                        legacyDatePart,
                        "yyyyMMdd",
                        CultureInfo.InvariantCulture,
                        DateTimeStyles.None,
                        out date))
                {
                    return true;
                }
            }

            date = default;
            return false;
        }
    }
}
