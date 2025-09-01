using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;

namespace ControlEntradaSalida
{
    /// <summary>
    /// 重试策略配置
    /// </summary>
    public class RetryPolicy
    {
        /// <summary>
        /// 最大重试次数
        /// </summary>
        public int MaxRetryCount { get; set; } = 3;
        
        /// <summary>
        /// 初始重试延迟（毫秒）
        /// </summary>
        public int InitialDelayMs { get; set; } = 1000;
        
        /// <summary>
        /// 指数退避因子
        /// </summary>
        public double BackoffMultiplier { get; set; } = 2.0;
        
        /// <summary>
        /// 最大重试延迟（毫秒）
        /// </summary>
        public int MaxDelayMs { get; set; } = 30000;
    }

    /// <summary>
    /// 批处理配置
    /// </summary>
    public class BatchConfiguration
    {
        /// <summary>
        /// 批处理大小（记录数）
        /// </summary>
        public int BatchSize { get; set; } = 50;
        
        /// <summary>
        /// 批处理超时时间（毫秒）
        /// </summary>
        public int BatchTimeoutMs { get; set; } = 5000;
        
        /// <summary>
        /// 最小批处理大小
        /// </summary>
        public int MinBatchSize { get; set; } = 1;
        
        /// <summary>
        /// 最大批处理大小
        /// </summary>
        public int MaxBatchSize { get; set; } = 200;
    }

    /// <summary>
    /// 异步数据库写入线程 - 门禁系统核心异步处理组件
    /// 
    /// 核心职责：
    /// 1. 从事件队列异步获取门禁事件
    /// 2. 批量聚合事件数据以提高写入效率
    /// 3. 调用批量处理器执行数据库写入
    /// 4. 实现智能重试机制处理临时故障
    /// 5. 提供性能监控和健康检查
    /// 
    /// 设计特点：
    /// - 严格遵循海康威视SDK编程指南
    /// - 与UI线程完全分离，确保界面响应性
    /// - 支持动态配置调整和优雅关闭
    /// - 内置错误恢复和降级策略
    /// </summary>
    public class AsyncDatabaseWriter : IDisposable
    {
        private readonly AsyncEventQueue _eventQueue;
        private readonly DatabaseBatchProcessor _batchProcessor;
        private readonly EventDeduplicator _deduplicator;
        private readonly BatchConfiguration _batchConfig;
        private readonly RetryPolicy _retryPolicy;
        private readonly string _connectionString;
        
        private Task _processingTask;
        private CancellationTokenSource _cancellationTokenSource;
        private volatile bool _isRunning = false;
        private volatile bool _disposed = false;
        
        // 统计信息
        private long _totalEventsProcessed = 0;
        private long _totalBatchesExecuted = 0;
        private long _totalRetries = 0;
        private long _totalErrors = 0;
        private DateTime _startTime;
        
        // 性能监控
        private readonly Queue<double> _recentThroughputs = new Queue<double>();
        private readonly object _statsLock = new object();

        public AsyncDatabaseWriter(string connectionString, 
                                  AsyncEventQueue eventQueue, 
                                  EventDeduplicator deduplicator,
                                  BatchConfiguration batchConfig = null, 
                                  RetryPolicy retryPolicy = null)
        {
            _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
            _eventQueue = eventQueue ?? throw new ArgumentNullException(nameof(eventQueue));
            _deduplicator = deduplicator ?? throw new ArgumentNullException(nameof(deduplicator));
            _batchConfig = batchConfig ?? new BatchConfiguration();
            _retryPolicy = retryPolicy ?? new RetryPolicy();
            
            _batchProcessor = new DatabaseBatchProcessor(connectionString);
            _cancellationTokenSource = new CancellationTokenSource();
            _startTime = DateTime.Now;
        }

        /// <summary>
        /// 启动异步数据库写入处理
        /// </summary>
        /// <returns>启动任务</returns>
        public async Task StartAsync()
        {
            if (_disposed) throw new ObjectDisposedException(nameof(AsyncDatabaseWriter));
            if (_isRunning) return;

            Console.WriteLine("[START] 启动异步数据库写入线程...");

            // 测试数据库连接
            bool connectionOk = await _batchProcessor.TestConnectionAsync();
            if (!connectionOk)
            {
                throw new InvalidOperationException("数据库连接测试失败，无法启动异步写入线程");
            }

            _isRunning = true;
            _startTime = DateTime.Now;
            
            _processingTask = Task.Run(ProcessEventQueueAsync, _cancellationTokenSource.Token);
            
            Console.WriteLine("[START] 异步数据库写入线程已启动");
        }

        /// <summary>
        /// 停止异步数据库写入处理
        /// </summary>
        /// <param name="timeoutMs">停止超时时间（毫秒）</param>
        /// <returns>停止任务</returns>
        public async Task StopAsync(int timeoutMs = 30000)
        {
            if (!_isRunning || _disposed) return;

            Console.WriteLine("[STOP] 正在停止异步数据库写入线程...");

            _cancellationTokenSource.Cancel();
            _isRunning = false;

            try
            {
                if (_processingTask != null)
                {
                    await Task.WhenAny(_processingTask, Task.Delay(timeoutMs));
                }
            }
            catch (TimeoutException)
            {
                Console.WriteLine("[WARNING] 异步写入线程停止超时，强制终止");
            }
            catch (OperationCanceledException)
            {
                // 正常取消，忽略
            }

            Console.WriteLine("[STOP] 异步数据库写入线程已停止");
        }

        /// <summary>
        /// 事件队列处理主循环
        /// </summary>
        private async Task ProcessEventQueueAsync()
        {
            Console.WriteLine("[PROCESSING] 事件队列处理循环已启动");
            
            while (!_cancellationTokenSource.Token.IsCancellationRequested)
            {
                try
                {
                    // 等待队列中有数据
                    bool hasData = await _eventQueue.WaitForDataAsync(_cancellationTokenSource.Token);
                    
                    if (!hasData || _cancellationTokenSource.Token.IsCancellationRequested)
                        continue;

                    // 收集批处理事件
                    var batchEvents = await CollectBatchEventsAsync();
                    
                    if (batchEvents.Length > 0)
                    {
                        await ProcessBatchWithRetryAsync(batchEvents);
                    }
                }
                catch (OperationCanceledException)
                {
                    break; // 正常退出
                }
                catch (Exception ex)
                {
                    Interlocked.Increment(ref _totalErrors);
                    Console.WriteLine($"[ERROR] 事件队列处理异常: {ex.Message}");
                    
                    // 异常后短暂等待，避免紧密循环
                    try
                    {
                        await Task.Delay(1000, _cancellationTokenSource.Token);
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                }
            }
            
            Console.WriteLine("[PROCESSING] 事件队列处理循环已退出");
        }

        /// <summary>
        /// 收集批处理事件
        /// </summary>
        /// <returns>收集到的事件数组</returns>
        private async Task<AccessLogEvent[]> CollectBatchEventsAsync()
        {
            var batchEvents = new List<AccessLogEvent>();
            var startTime = DateTime.Now;
            
            // 收集事件直到达到批处理条件
            while (batchEvents.Count < _batchConfig.BatchSize && 
                   (DateTime.Now - startTime).TotalMilliseconds < _batchConfig.BatchTimeoutMs)
            {
                if (_eventQueue.TryDequeue(out AccessLogEvent evt))
                {
                    batchEvents.Add(evt);
                }
                else
                {
                    // 队列暂时为空，短暂等待
                    try
                    {
                        await Task.Delay(10, _cancellationTokenSource.Token);
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                }
            }
            
            // 如果已达到超时时间但没有事件，再尝试批量出队
            if (batchEvents.Count == 0)
            {
                var additionalEvents = _eventQueue.DequeueBatch(_batchConfig.BatchSize);
                batchEvents.AddRange(additionalEvents);
            }
            
            return batchEvents.ToArray();
        }

        /// <summary>
        /// 带重试机制的批处理
        /// </summary>
        /// <param name="events">事件数组</param>
        private async Task ProcessBatchWithRetryAsync(AccessLogEvent[] events)
        {
            var currentEvents = events;
            int retryCount = 0;
            
            while (retryCount <= _retryPolicy.MaxRetryCount && currentEvents.Length > 0)
            {
                try
                {
                    var result = await _batchProcessor.ProcessBatchAsync(currentEvents);
                    
                    // 更新统计信息
                    Interlocked.Add(ref _totalEventsProcessed, result.ProcessedCount);
                    Interlocked.Increment(ref _totalBatchesExecuted);
                    
                    // 更新吞吐量统计
                    UpdateThroughputStats(result.ProcessedCount, result.ElapsedMilliseconds);
                    
                    if (result.Success)
                    {
                        Console.WriteLine($"[BATCH] 成功处理批次: {result.ProcessedCount}条事件, 耗时: {result.ElapsedMilliseconds}ms");
                        break; // 成功处理，退出重试循环
                    }
                    else if (result.FailedEvents.Count > 0)
                    {
                        // 部分失败，准备重试失败的事件
                        currentEvents = result.FailedEvents.ToArray();
                        retryCount++;
                        
                        if (retryCount <= _retryPolicy.MaxRetryCount)
                        {
                            int delay = CalculateRetryDelay(retryCount);
                            Console.WriteLine($"[RETRY] 批处理部分失败，{delay}ms后进行第{retryCount}次重试，剩余{currentEvents.Length}条事件");
                            
                            Interlocked.Increment(ref _totalRetries);
                            
                            try
                            {
                                await Task.Delay(delay, _cancellationTokenSource.Token);
                            }
                            catch (OperationCanceledException)
                            {
                                return;
                            }
                        }
                        else
                        {
                            // 超过重试次数，记录失败事件
                            Console.WriteLine($"[ERROR] 批处理最终失败，丢弃{currentEvents.Length}条事件");
                            Interlocked.Add(ref _totalErrors, currentEvents.Length);
                        }
                    }
                }
                catch (Exception ex)
                {
                    retryCount++;
                    Console.WriteLine($"[ERROR] 批处理异常: {ex.Message}");
                    
                    if (retryCount <= _retryPolicy.MaxRetryCount)
                    {
                        int delay = CalculateRetryDelay(retryCount);
                        Console.WriteLine($"[RETRY] {delay}ms后进行第{retryCount}次重试");
                        
                        Interlocked.Increment(ref _totalRetries);
                        
                        try
                        {
                            await Task.Delay(delay, _cancellationTokenSource.Token);
                        }
                        catch (OperationCanceledException)
                        {
                            return;
                        }
                    }
                    else
                    {
                        Console.WriteLine($"[ERROR] 重试次数超限，丢弃{currentEvents.Length}条事件");
                        Interlocked.Add(ref _totalErrors, currentEvents.Length);
                    }
                }
            }
        }

        /// <summary>
        /// 计算重试延迟时间
        /// </summary>
        /// <param name="retryCount">重试次数</param>
        /// <returns>延迟时间（毫秒）</returns>
        private int CalculateRetryDelay(int retryCount)
        {
            double delay = _retryPolicy.InitialDelayMs * Math.Pow(_retryPolicy.BackoffMultiplier, retryCount - 1);
            return Math.Min((int)delay, _retryPolicy.MaxDelayMs);
        }

        /// <summary>
        /// 更新吞吐量统计
        /// </summary>
        /// <param name="eventsProcessed">处理的事件数</param>
        /// <param name="elapsedMs">耗时（毫秒）</param>
        private void UpdateThroughputStats(int eventsProcessed, long elapsedMs)
        {
            if (elapsedMs <= 0) return;
            
            double throughput = (double)eventsProcessed / elapsedMs * 1000; // 事件/秒
            
            lock (_statsLock)
            {
                _recentThroughputs.Enqueue(throughput);
                
                // 保持最近20次的吞吐量记录
                while (_recentThroughputs.Count > 20)
                {
                    _recentThroughputs.Dequeue();
                }
            }
        }

        /// <summary>
        /// 获取平均吞吐量
        /// </summary>
        /// <returns>平均吞吐量（事件/秒）</returns>
        private double GetAverageThroughput()
        {
            lock (_statsLock)
            {
                return _recentThroughputs.Count > 0 ? _recentThroughputs.Average() : 0.0;
            }
        }

        /// <summary>
        /// 获取统计信息
        /// </summary>
        /// <returns>统计信息字符串</returns>
        public string GetStatistics()
        {
            var uptime = DateTime.Now - _startTime;
            double avgThroughput = GetAverageThroughput();
            
            return $"异步写入器统计: " +
                   $"运行时间={uptime:hh\\:mm\\:ss}, " +
                   $"处理事件={_totalEventsProcessed}, " +
                   $"执行批次={_totalBatchesExecuted}, " +
                   $"重试次数={_totalRetries}, " +
                   $"错误数量={_totalErrors}, " +
                   $"平均吞吐量={avgThroughput:F1}事件/秒, " +
                   $"队列长度={_eventQueue.Count}";
        }

        /// <summary>
        /// 获取健康状态
        /// </summary>
        /// <returns>健康状态信息</returns>
        public string GetHealthStatus()
        {
            bool isHealthy = _isRunning && 
                            !_disposed && 
                            _totalErrors < _totalEventsProcessed * 0.1; // 错误率低于10%

            string status = isHealthy ? "健康" : "异常";
            double errorRate = _totalEventsProcessed > 0 ? 
                (double)_totalErrors / _totalEventsProcessed * 100 : 0;

            return $"异步写入器状态: {status} (错误率: {errorRate:F2}%)";
        }

        /// <summary>
        /// 动态调整批处理配置
        /// </summary>
        /// <param name="batchSize">新的批处理大小</param>
        /// <param name="timeoutMs">新的批处理超时时间</param>
        public void UpdateBatchConfiguration(int batchSize, int timeoutMs)
        {
            _batchConfig.BatchSize = Math.Min(Math.Max(batchSize, _batchConfig.MinBatchSize), _batchConfig.MaxBatchSize);
            _batchConfig.BatchTimeoutMs = Math.Max(timeoutMs, 1000);
            
            Console.WriteLine($"[CONFIG] 批处理配置已更新: 批次大小={_batchConfig.BatchSize}, 超时={_batchConfig.BatchTimeoutMs}ms");
        }

        /// <summary>
        /// 等待所有待处理事件完成
        /// </summary>
        /// <param name="timeoutMs">等待超时时间</param>
        public async Task<bool> FlushAsync(int timeoutMs = 30000)
        {
            var startTime = DateTime.Now;
            
            while (_eventQueue.Count > 0 && (DateTime.Now - startTime).TotalMilliseconds < timeoutMs)
            {
                await Task.Delay(100);
            }
            
            return _eventQueue.Count == 0;
        }

        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;
                
                // 优雅停止处理线程
                StopAsync().Wait(10000);
                
                _cancellationTokenSource?.Dispose();
                _batchProcessor?.Dispose();
                
                Console.WriteLine("[DISPOSE] 异步数据库写入器已释放资源");
            }
        }
    }
}