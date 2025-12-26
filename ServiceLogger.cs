using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;

namespace ControlEntradaSalida
{
    /// <summary>
    /// 异步文件日志记录器：业务线程只入队，后台线程批量写盘，避免日志 IO 阻塞业务。
    /// </summary>
    public static class ServiceLogger
    {
        private const int DefaultLogRetentionDays = 90;
        private const int DefaultQueueCapacity = 10000;
        private const int DefaultFlushIntervalMs = 250;
        private const int DefaultBatchSize = 256;

        private static readonly object InitLock = new object();
        private static readonly Stopwatch ProcessUptime = Stopwatch.StartNew();

        private static string logDirectory;
        private static string currentLogFile;
        private static DateTime currentLogDate = DateTime.MinValue;
        private static int logRetentionDays = DefaultLogRetentionDays;

        private static BlockingCollection<LogEntry> logQueue;
        private static Thread writerThread;
        private static CancellationTokenSource writerCancellation;

        private static volatile bool initialized;
        private static volatile bool shutdownStarted;
        private static bool exitHandlersRegistered;
        private static volatile bool verboseEnabled;

        private static long sequence;
        private static long droppedCount;
        private static DateTime lastDropReportUtc = DateTime.MinValue;

        private static int processId;
        private static string processName;

        private sealed class LogEntry
        {
            public DateTime Timestamp { get; set; }
            public string Level { get; set; }
            public string Message { get; set; }
            public int ManagedThreadId { get; set; }
            public string ThreadName { get; set; }
            public long Sequence { get; set; }
            public string CallerMemberName { get; set; }
            public string CallerFilePath { get; set; }
            public int CallerLineNumber { get; set; }
        }

        public static void Initialize(string directory, int retentionDays = DefaultLogRetentionDays)
        {
            Initialize(directory, retentionDays, verboseLogging: false);
        }

        public static void Initialize(string directory, int retentionDays, bool verboseLogging)
        {
            lock (InitLock)
            {
                if (shutdownStarted)
                {
                    return;
                }

                if (string.IsNullOrWhiteSpace(directory))
                {
                    directory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs");
                }

                logRetentionDays = retentionDays > 0 ? retentionDays : DefaultLogRetentionDays;
                logDirectory = directory.Trim();
                Directory.CreateDirectory(logDirectory);
                verboseEnabled = verboseLogging;

                processId = Process.GetCurrentProcess().Id;
                processName = Process.GetCurrentProcess().ProcessName;

                UpdateLogFilePath();

                if (!initialized)
                {
                    logQueue = new BlockingCollection<LogEntry>(new ConcurrentQueue<LogEntry>(), DefaultQueueCapacity);
                    writerCancellation = new CancellationTokenSource();

                    writerThread = new Thread(WriterLoop)
                    {
                        IsBackground = true,
                        Name = "ServiceLoggerWriter"
                    };
                    writerThread.Start();

                    initialized = true;
                }

                RegisterExitHandlersIfNeeded();
            }
        }

        public static bool IsVerboseEnabled => verboseEnabled;

        public static void Verbose(
            string message,
            [CallerMemberName] string callerMemberName = null,
            [CallerFilePath] string callerFilePath = null,
            [CallerLineNumber] int callerLineNumber = 0)
        {
            if (!verboseEnabled)
            {
                return;
            }

            Write("TRACE", message, callerMemberName, callerFilePath, callerLineNumber);
        }

        public static void Info(
            string message,
            [CallerMemberName] string callerMemberName = null,
            [CallerFilePath] string callerFilePath = null,
            [CallerLineNumber] int callerLineNumber = 0)
        {
            Write("INFO", message, callerMemberName, callerFilePath, callerLineNumber);
        }

        public static void Warn(
            string message,
            [CallerMemberName] string callerMemberName = null,
            [CallerFilePath] string callerFilePath = null,
            [CallerLineNumber] int callerLineNumber = 0)
        {
            Write("WARN", message, callerMemberName, callerFilePath, callerLineNumber);
        }

        public static void Error(
            string message,
            Exception ex = null,
            [CallerMemberName] string callerMemberName = null,
            [CallerFilePath] string callerFilePath = null,
            [CallerLineNumber] int callerLineNumber = 0)
        {
            var builder = new StringBuilder(message ?? string.Empty);
            if (ex != null)
            {
                builder.Append(" | 异常: ").Append(ex);
            }

            Write("ERROR", builder.ToString(), callerMemberName, callerFilePath, callerLineNumber);
        }

        public static void Debug(
            string message,
            [CallerMemberName] string callerMemberName = null,
            [CallerFilePath] string callerFilePath = null,
            [CallerLineNumber] int callerLineNumber = 0)
        {
            Write("DEBUG", message, callerMemberName, callerFilePath, callerLineNumber);
        }

        public static void Flush(int timeoutMs = 2000)
        {
            if (!initialized || logQueue == null)
            {
                return;
            }

            DateTime deadline = DateTime.UtcNow.AddMilliseconds(Math.Max(0, timeoutMs));
            while (DateTime.UtcNow < deadline)
            {
                if (logQueue.Count == 0)
                {
                    return;
                }

                Thread.Sleep(20);
            }
        }

        public static void Shutdown(int flushTimeoutMs = 2000)
        {
            lock (InitLock)
            {
                if (shutdownStarted)
                {
                    return;
                }

                shutdownStarted = true;
            }

            try
            {
                Flush(flushTimeoutMs);
            }
            catch
            {
            }

            try
            {
                writerCancellation?.Cancel();
            }
            catch
            {
            }

            try
            {
                logQueue?.CompleteAdding();
            }
            catch
            {
            }

            try
            {
                if (writerThread != null && writerThread.IsAlive)
                {
                    writerThread.Join(Math.Max(0, flushTimeoutMs));
                }
            }
            catch
            {
            }
        }

        private static void Write(string level, string message, string callerMemberName, string callerFilePath, int callerLineNumber)
        {
            if (!initialized)
            {
                Initialize(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs"));
            }

            if (shutdownStarted)
            {
                return;
            }

            var entry = new LogEntry
            {
                Timestamp = DateTime.Now,
                Level = level,
                Message = message ?? string.Empty,
                ManagedThreadId = Thread.CurrentThread.ManagedThreadId,
                ThreadName = Thread.CurrentThread.Name,
                Sequence = Interlocked.Increment(ref sequence),
                CallerMemberName = callerMemberName,
                CallerFilePath = callerFilePath,
                CallerLineNumber = callerLineNumber
            };

            if (!TryEnqueue(entry))
            {
                ReportDropIfNeeded();
            }
        }

        private static bool TryEnqueue(LogEntry entry)
        {
            if (logQueue == null)
            {
                return false;
            }

            try
            {
                return logQueue.TryAdd(entry, 0);
            }
            catch
            {
                return false;
            }
        }

        private static void ReportDropIfNeeded()
        {
            Interlocked.Increment(ref droppedCount);

            DateTime nowUtc = DateTime.UtcNow;
            DateTime last = lastDropReportUtc;
            if (last != DateTime.MinValue && (nowUtc - last).TotalSeconds < 5)
            {
                return;
            }

            lastDropReportUtc = nowUtc;
            long dropped = Interlocked.Read(ref droppedCount);
            Trace.TraceWarning($"[ServiceLogger] 日志队列已满，已丢弃 {dropped} 条日志。建议降低日志量或增大队列容量。");
        }

        private static void WriterLoop()
        {
            StreamWriter writer = null;
            DateTime writerLogDate = DateTime.MinValue;
            string writerLogFile = null;

            try
            {
                while (true)
                {
                    if (writerCancellation != null && writerCancellation.IsCancellationRequested)
                    {
                        DrainQueue(writer);
                        return;
                    }

                    if (!initialized || logQueue == null)
                    {
                        Thread.Sleep(DefaultFlushIntervalMs);
                        continue;
                    }

                    EnsureLogFileForToday();
                    if (writerLogDate != currentLogDate || !string.Equals(writerLogFile, currentLogFile, StringComparison.OrdinalIgnoreCase))
                    {
                        try
                        {
                            writer?.Flush();
                            writer?.Dispose();
                        }
                        catch
                        {
                        }

                        writerLogDate = currentLogDate;
                        writerLogFile = currentLogFile;
                        writer = OpenWriter(writerLogFile);
                    }

                    int written = 0;
                    var batchBuilder = new StringBuilder(8192);
                    while (written < DefaultBatchSize && logQueue.TryTake(out LogEntry entry, DefaultFlushIntervalMs))
                    {
                        batchBuilder.AppendLine(FormatEntry(entry));
                        written++;
                    }

                    if (written > 0 && writer != null)
                    {
                        writer.Write(batchBuilder.ToString());
                        writer.Flush();
                    }
                }
            }
            catch (Exception ex)
            {
                Trace.TraceError($"[ServiceLogger] 后台写日志线程异常退出: {ex}");
            }
            finally
            {
                try
                {
                    DrainQueue(writer);
                }
                catch
                {
                }

                try
                {
                    writer?.Flush();
                    writer?.Dispose();
                }
                catch
                {
                }
            }
        }

        private static void DrainQueue(StreamWriter writer)
        {
            if (logQueue == null || writer == null)
            {
                return;
            }

            int safety = 0;
            while (safety < 200000 && logQueue.TryTake(out LogEntry entry))
            {
                writer.WriteLine(FormatEntry(entry));
                safety++;
            }

            writer.Flush();
        }

        private static StreamWriter OpenWriter(string logFilePath)
        {
            if (string.IsNullOrWhiteSpace(logFilePath))
            {
                return null;
            }

            try
            {
                var stream = new FileStream(
                    logFilePath,
                    FileMode.Append,
                    FileAccess.Write,
                    FileShare.ReadWrite);
                return new StreamWriter(stream, Encoding.UTF8) { AutoFlush = false };
            }
            catch (Exception ex)
            {
                Trace.TraceError($"[ServiceLogger] 打开日志文件失败: {logFilePath} | {ex}");
                return null;
            }
        }

        private static string FormatEntry(LogEntry entry)
        {
            string callerFile = TryGetFileName(entry.CallerFilePath);
            string threadName = string.IsNullOrWhiteSpace(entry.ThreadName) ? "-" : entry.ThreadName;
            string uptime = ProcessUptime.Elapsed.ToString("c", CultureInfo.InvariantCulture);

            return string.Format(
                CultureInfo.InvariantCulture,
                "{0:yyyy-MM-dd HH:mm:ss.fff} [{1}] [pid:{2}] [proc:{3}] [tid:{4}] [tname:{5}] [seq:{6}] [up:{7}] {8} | at {9}:{10} {11}",
                entry.Timestamp,
                entry.Level,
                processId,
                processName,
                entry.ManagedThreadId,
                threadName,
                entry.Sequence,
                uptime,
                entry.Message,
                callerFile,
                entry.CallerLineNumber,
                entry.CallerMemberName ?? "-");
        }

        private static string TryGetFileName(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                return "-";
            }

            try
            {
                return Path.GetFileName(filePath);
            }
            catch
            {
                return filePath;
            }
        }

        private static void EnsureLogFileForToday()
        {
            lock (InitLock)
            {
                if (currentLogDate != DateTime.Today || string.IsNullOrEmpty(currentLogFile))
                {
                    UpdateLogFilePath();
                }
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

        private static void RegisterExitHandlersIfNeeded()
        {
            if (exitHandlersRegistered)
            {
                return;
            }

            exitHandlersRegistered = true;

            AppDomain.CurrentDomain.ProcessExit += (_, __) =>
            {
                try
                {
                    Shutdown(2000);
                }
                catch
                {
                }
            };

            AppDomain.CurrentDomain.DomainUnload += (_, __) =>
            {
                try
                {
                    Shutdown(2000);
                }
                catch
                {
                }
            };

            AppDomain.CurrentDomain.UnhandledException += (_, __) =>
            {
                try
                {
                    Shutdown(2000);
                }
                catch
                {
                }
            };
        }
    }
}
