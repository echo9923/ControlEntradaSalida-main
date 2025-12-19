using System;
using System.Diagnostics;
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
            private bool disposed;

            internal SemaphoreOperationResult(SemaphoreSlim semaphore, bool acquired, string operationId)
            {
                this.semaphore = semaphore;
                this.acquired = acquired;
                this.operationId = operationId;

                LogOperation($"信号量操作创建 - 获取状态: {acquired}");
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
                    semaphore.Release();
                    LogOperation("信号量已释放");
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
                Debug.WriteLine($"[SynchronizationHelper] {operationId}: {message} - 线程ID: {Thread.CurrentThread.ManagedThreadId}");
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
                LogWaitStart(operationId, timeout);

                bool acquired = await semaphore.WaitAsync(timeout);

                LogWaitResult(operationId, acquired);

                return new SemaphoreOperationResult(semaphore, acquired, operationId);
            }
            catch (Exception ex)
            {
                LogWaitException(operationId, ex);
                return new SemaphoreOperationResult(semaphore, false, operationId);
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
                LogWaitStart(operationId, timeout);

                bool acquired = semaphore.Wait(timeout);

                LogWaitResult(operationId, acquired);

                return new SemaphoreOperationResult(semaphore, acquired, operationId);
            }
            catch (Exception ex)
            {
                LogWaitException(operationId, ex);
                return new SemaphoreOperationResult(semaphore, false, operationId);
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
            Debug.WriteLine($"[SynchronizationHelper] 创建信号量: {name} - 初始计数: {initialCount}, 最大计数: {maxCount}");
            return new SemaphoreSlim(initialCount, maxCount);
        }

        private static string CreateOperationId(string operationName)
        {
            return $"{operationName}-{DateTime.Now:HHmmss.fff}";
        }

        private static void LogWaitStart(string operationId, int timeout)
        {
            Debug.WriteLine($"[SynchronizationHelper] {operationId}: 开始等待信号量 - 超时: {timeout}ms");
        }

        private static void LogWaitResult(string operationId, bool acquired)
        {
            Debug.WriteLine($"[SynchronizationHelper] {operationId}: 信号量等待结果: {acquired}");
        }

        private static void LogWaitException(string operationId, Exception ex)
        {
            Debug.WriteLine($"[SynchronizationHelper] {operationId}: 信号量等待异常: {ex.Message}");
        }
    }
}
