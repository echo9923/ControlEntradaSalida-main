using System;
using System.Diagnostics;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;

namespace ControlEntradaSalida
{
    /// <summary>
    /// 同步原语辅助类，提供安全的信号量操作封装。
    /// </summary>
    public static class SynchronizationHelper
    {
        /// <summary>
        /// 安全的信号量操作结果。
        /// </summary>
        public sealed class SemaphoreOperationResult : IDisposable
        {
            private readonly SemaphoreSlim semaphore;
            private readonly bool acquired;
            private readonly string operationId;
            private readonly int timeoutMs;
            private readonly int waitMs;
            private readonly int acquiredThreadId;
            private readonly long acquiredTick;
            private bool disposed;

            internal SemaphoreOperationResult(
                SemaphoreSlim semaphore,
                bool acquired,
                string operationId,
                int timeoutMs,
                int waitMs,
                int acquiredThreadId,
                long acquiredTick)
            {
                this.semaphore = semaphore;
                this.acquired = acquired;
                this.operationId = operationId;
                this.timeoutMs = timeoutMs;
                this.waitMs = waitMs;
                this.acquiredThreadId = acquiredThreadId;
                this.acquiredTick = acquiredTick;

                LogOperation($"信号量操作创建 - 获取状态: {acquired}, 超时: {timeoutMs}ms, 等待: {waitMs}ms");
            }

            /// <summary>
            /// 是否成功获取信号量。
            /// </summary>
            public bool IsAcquired => acquired;

            /// <summary>
            /// 释放信号量（如果已获取）。
            /// </summary>
            public void Dispose()
            {
                if (disposed || !acquired)
                {
                    return;
                }

                try
                {
                    double heldMs = acquiredTick <= 0 ? -1 : StopwatchTicksToMs(Stopwatch.GetTimestamp() - acquiredTick);
                    semaphore.Release();
                    LogOperation($"信号量已释放 - 持有: {FormatMs(heldMs)}");
                }
                catch (SemaphoreFullException ex)
                {
                    LogOperation($"信号量释放失败 - SemaphoreFullException: {ex.Message}");
                    Debug.WriteLine($"[SynchronizationHelper] 信号量过度释放检测: {operationId} - {ex.Message}");
                }
                catch (Exception ex)
                {
                    LogOperation($"信号量释放异常: {ex.Message}");
                    Debug.WriteLine($"[SynchronizationHelper] 信号量释放异常: {operationId} - {ex.Message}");
                }
                finally
                {
                    disposed = true;
                }
            }

            private void LogOperation(string message)
            {
                if (ServiceLogger.IsVerboseEnabled)
                {
                    ServiceLogger.Verbose($"[锁] {operationId}: {message} | acquiredTid={acquiredThreadId} currentTid={Thread.CurrentThread.ManagedThreadId} semCount={SafeGetCurrentCount(semaphore)}");
                }
                else
                {
                    Debug.WriteLine($"[SynchronizationHelper] {operationId}: {message} - 线程ID: {Thread.CurrentThread.ManagedThreadId}");
                }
            }
        }

        /// <summary>
        /// 安全的异步信号量等待操作。
        /// </summary>
        /// <param name="semaphore">信号量对象</param>
        /// <param name="timeout">超时时间（毫秒）</param>
        /// <param name="operationName">操作名称（用于日志）</param>
        /// <returns>信号量操作结果</returns>
        public static async Task<SemaphoreOperationResult> SafeWaitAsync(
            SemaphoreSlim semaphore,
            int timeout,
            string operationName = "Unknown")
        {
            if (semaphore == null)
            {
                throw new ArgumentNullException(nameof(semaphore));
            }

            string operationId = CreateOperationId(operationName);

            try
            {
                long startTick = Stopwatch.GetTimestamp();
                LogWaitStart(operationId, timeout, semaphore);

                bool acquired = await semaphore.WaitAsync(timeout);
                long acquiredTick = acquired ? Stopwatch.GetTimestamp() : 0;

                int waitMs = (int)Math.Max(0, StopwatchTicksToMs(Stopwatch.GetTimestamp() - startTick));
                LogWaitResult(operationId, acquired, waitMs, semaphore);

                return new SemaphoreOperationResult(
                    semaphore,
                    acquired,
                    operationId,
                    timeout,
                    waitMs,
                    Thread.CurrentThread.ManagedThreadId,
                    acquiredTick);
            }
            catch (Exception ex)
            {
                LogWaitException(operationId, ex, semaphore);
                return new SemaphoreOperationResult(
                    semaphore,
                    false,
                    operationId,
                    timeout,
                    waitMs: -1,
                    acquiredThreadId: Thread.CurrentThread.ManagedThreadId,
                    acquiredTick: 0);
            }
        }

        /// <summary>
        /// 安全的同步信号量等待操作。
        /// </summary>
        /// <param name="semaphore">信号量对象</param>
        /// <param name="timeout">超时时间（毫秒）</param>
        /// <param name="operationName">操作名称（用于日志）</param>
        /// <returns>信号量操作结果</returns>
        public static SemaphoreOperationResult SafeWait(
            SemaphoreSlim semaphore,
            int timeout,
            string operationName = "Unknown")
        {
            if (semaphore == null)
            {
                throw new ArgumentNullException(nameof(semaphore));
            }

            string operationId = CreateOperationId(operationName);

            try
            {
                long startTick = Stopwatch.GetTimestamp();
                LogWaitStart(operationId, timeout, semaphore);

                bool acquired = semaphore.Wait(timeout);
                long acquiredTick = acquired ? Stopwatch.GetTimestamp() : 0;

                int waitMs = (int)Math.Max(0, StopwatchTicksToMs(Stopwatch.GetTimestamp() - startTick));
                LogWaitResult(operationId, acquired, waitMs, semaphore);

                return new SemaphoreOperationResult(
                    semaphore,
                    acquired,
                    operationId,
                    timeout,
                    waitMs,
                    Thread.CurrentThread.ManagedThreadId,
                    acquiredTick);
            }
            catch (Exception ex)
            {
                LogWaitException(operationId, ex, semaphore);
                return new SemaphoreOperationResult(
                    semaphore,
                    false,
                    operationId,
                    timeout,
                    waitMs: -1,
                    acquiredThreadId: Thread.CurrentThread.ManagedThreadId,
                    acquiredTick: 0);
            }
        }

        /// <summary>
        /// 创建带调试信息的信号量。
        /// </summary>
        /// <param name="initialCount">初始计数</param>
        /// <param name="maxCount">最大计数</param>
        /// <param name="name">信号量名称（用于调试）</param>
        /// <returns>信号量对象</returns>
        public static SemaphoreSlim CreateSemaphore(int initialCount, int maxCount, string name = "Unknown")
        {
            if (ServiceLogger.IsVerboseEnabled)
            {
                ServiceLogger.Verbose($"[锁] 创建信号量: {name} - 初始计数: {initialCount}, 最大计数: {maxCount}");
            }
            else
            {
                Debug.WriteLine($"[SynchronizationHelper] 创建信号量: {name} - 初始计数: {initialCount}, 最大计数: {maxCount}");
            }
            return new SemaphoreSlim(initialCount, maxCount);
        }

        private static string CreateOperationId(string operationName)
        {
            return $"{operationName}-{DateTime.Now:HHmmss.fff}-T{Thread.CurrentThread.ManagedThreadId}";
        }

        private static void LogWaitStart(string operationId, int timeout, SemaphoreSlim semaphore)
        {
            if (ServiceLogger.IsVerboseEnabled)
            {
                ServiceLogger.Verbose($"[锁] {operationId}: 开始等待 - 超时: {timeout}ms - semCount={SafeGetCurrentCount(semaphore)}");
            }
            else
            {
                Debug.WriteLine($"[SynchronizationHelper] {operationId}: 开始等待信号量 - 超时: {timeout}ms");
            }
        }

        private static void LogWaitResult(string operationId, bool acquired, int waitMs, SemaphoreSlim semaphore)
        {
            if (ServiceLogger.IsVerboseEnabled)
            {
                ServiceLogger.Verbose($"[锁] {operationId}: 等待结果: {acquired} - waitMs={waitMs} - semCount={SafeGetCurrentCount(semaphore)}");
            }
            else
            {
                Debug.WriteLine($"[SynchronizationHelper] {operationId}: 信号量等待结果: {acquired}");
            }
        }

        private static void LogWaitException(string operationId, Exception ex, SemaphoreSlim semaphore)
        {
            if (ServiceLogger.IsVerboseEnabled)
            {
                ServiceLogger.Verbose($"[锁] {operationId}: 等待异常: {ex.Message} - semCount={SafeGetCurrentCount(semaphore)}");
            }
            else
            {
                Debug.WriteLine($"[SynchronizationHelper] {operationId}: 信号量等待异常: {ex.Message}");
            }
        }

        private static int SafeGetCurrentCount(SemaphoreSlim semaphore)
        {
            if (semaphore == null)
            {
                return -1;
            }

            try
            {
                return semaphore.CurrentCount;
            }
            catch
            {
                return -1;
            }
        }

        private static double StopwatchTicksToMs(long ticks)
        {
            if (ticks <= 0)
            {
                return 0;
            }

            return ticks * 1000.0 / Stopwatch.Frequency;
        }

        private static string FormatMs(double value)
        {
            if (value < 0)
            {
                return "-";
            }

            return value.ToString("0.###", CultureInfo.InvariantCulture) + "ms";
        }
    }
}
