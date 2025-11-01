using System;
using System.Diagnostics;
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
        private const int MaxLogLines = 2000;
        private static string logDirectory;
        private static string currentLogFile;
        private static DateTime currentLogDate = DateTime.MinValue;

        public static void Initialize(string directory)
        {
            if (string.IsNullOrWhiteSpace(directory))
            {
                directory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs");
            }

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
                EnforceLogSizeLimit();
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
            string fileName = $"ControlEntradaSalida_{currentLogDate:yyyyMMdd}.log";
            currentLogFile = Path.Combine(logDirectory, fileName);
        }

        private static void EnforceLogSizeLimit()
        {
            if (string.IsNullOrEmpty(currentLogFile) || !File.Exists(currentLogFile))
            {
                return;
            }

            int lineCount = 0;
            bool exceedsLimit = false;

            using (var stream = new FileStream(currentLogFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (var reader = new StreamReader(stream, Encoding.UTF8, true))
            {
                while (!reader.EndOfStream)
                {
                    reader.ReadLine();
                    lineCount++;

                    if (lineCount > MaxLogLines)
                    {
                        exceedsLimit = true;
                        break;
                    }
                }
            }

            if (!exceedsLimit)
            {
                return;
            }

            using (var stream = new FileStream(currentLogFile, FileMode.Open, FileAccess.Write, FileShare.Read))
            {
                stream.SetLength(0);
            }
        }
    }
}
