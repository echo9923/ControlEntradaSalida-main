using System;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;

namespace ControlEntradaSalida
{
    /// <summary>
    /// 同步原语辅助类，提供安全的信号量操作封装
    /// </summary>
    public static class SynchronizationHelper
{
    /// <summary>
    /// 安全的信号量操作结果
    /// </summary>
    public class SemaphoreOperationResult : IDisposable
    {
        private readonly SemaphoreSlim _semaphore;
        private readonly bool _acquired;
        private readonly string _operationId;
        private bool _disposed = false;

        internal SemaphoreOperationResult(SemaphoreSlim semaphore, bool acquired, string operationId)
        {
            _semaphore = semaphore;
            _acquired = acquired;
            _operationId = operationId;
            
            LogOperation($"信号量操作创建 - 获取状态: {acquired}");
        }

        /// <summary>
        /// 是否成功获取信号量
        /// </summary>
        public bool IsAcquired => _acquired;

        /// <summary>
        /// 释放信号量（如果已获取）
        /// </summary>
        public void Dispose()
        {
            if (!_disposed && _acquired)
            {
                try
                {
                    _semaphore.Release();
                    LogOperation("信号量已释放");
                }
                catch (SemaphoreFullException ex)
                {
                    LogOperation($"信号量释放失败 - SemaphoreFullException: {ex.Message}");
                    // 记录详细错误但不重新抛出，避免程序崩溃
                    Debug.WriteLine($"[SynchronizationHelper] 信号量过度释放检测: {_operationId} - {ex.Message}");
                }
                catch (Exception ex)
                {
                    LogOperation($"信号量释放异常: {ex.Message}");
                    Debug.WriteLine($"[SynchronizationHelper] 信号量释放异常: {_operationId} - {ex.Message}");
                }
                finally
                {
                    _disposed = true;
                }
            }
        }

        private void LogOperation(string message)
        {
            Debug.WriteLine($"[SynchronizationHelper] {_operationId}: {message} - 线程ID: {Thread.CurrentThread.ManagedThreadId}");
        }
    }

    /// <summary>
    /// 安全的异步信号量等待操作
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
            throw new ArgumentNullException(nameof(semaphore));

        var operationId = $"{operationName}-{DateTime.Now:HHmmss.fff}";
        
        try
        {
            Debug.WriteLine($"[SynchronizationHelper] {operationId}: 开始等待信号量 - 超时: {timeout}ms");
            
            bool acquired = await semaphore.WaitAsync(timeout);
            
            Debug.WriteLine($"[SynchronizationHelper] {operationId}: 信号量等待结果: {acquired}");
            
            return new SemaphoreOperationResult(semaphore, acquired, operationId);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[SynchronizationHelper] {operationId}: 信号量等待异常: {ex.Message}");
            return new SemaphoreOperationResult(semaphore, false, operationId);
        }
    }

    /// <summary>
    /// 安全的同步信号量等待操作
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
            throw new ArgumentNullException(nameof(semaphore));

        var operationId = $"{operationName}-{DateTime.Now:HHmmss.fff}";
        
        try
        {
            Debug.WriteLine($"[SynchronizationHelper] {operationId}: 开始等待信号量 - 超时: {timeout}ms");
            
            bool acquired = semaphore.Wait(timeout);
            
            Debug.WriteLine($"[SynchronizationHelper] {operationId}: 信号量等待结果: {acquired}");
            
            return new SemaphoreOperationResult(semaphore, acquired, operationId);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[SynchronizationHelper] {operationId}: 信号量等待异常: {ex.Message}");
            return new SemaphoreOperationResult(semaphore, false, operationId);
        }
    }

    /// <summary>
    /// 创建带调试信息的信号量
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
}
}