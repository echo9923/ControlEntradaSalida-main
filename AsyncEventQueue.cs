using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace ControlEntradaSalida
{
    /// <summary>
    /// 门禁事件数据模型 - 用于异步数据库处理
    /// 严格按照海康威视SDK编程指南设计，确保轻量级快速处理
    /// </summary>
    public class AccessLogEvent
    {
        /// <summary>
        /// 事件编号
        /// </summary>
        public string LogNumber { get; set; }
        
        /// <summary>
        /// 事件时间
        /// </summary>
        public DateTime EventTime { get; set; }
        
        /// <summary>
        /// 员工ID（卡号）
        /// </summary>
        public string EmployeeId { get; set; }
        
        /// <summary>
        /// 设备ID
        /// </summary>
        public int DeviceId { get; set; }
        
        /// <summary>
        /// 事件类型（如MINOR_FACE_VERIFY_PASS）
        /// </summary>
        public string EventType { get; set; }
        
        /// <summary>
        /// 员工姓名（用于UI显示）
        /// </summary>
        public string EmployeeName { get; set; }
        
        /// <summary>
        /// 事件优先级（1=高，2=中，3=低）
        /// </summary>
        public int Priority { get; set; } = 2;
        
        /// <summary>
        /// 创建时间（用于监控处理延迟）
        /// </summary>
        public DateTime CreateTime { get; set; } = DateTime.Now;
        
        /// <summary>
        /// 重试次数
        /// </summary>
        public int RetryCount { get; set; } = 0;
        
        /// <summary>
        /// 生成事件去重标识
        /// </summary>
        public string GetDeduplicationKey()
        {
            return $"{EmployeeId}_{DeviceId}_{EventTime:yyyy-MM-dd HH:mm:ss}_{EventType}";
        }
    }

    /// <summary>
    /// 线程安全的异步事件队列
    /// 基于海康威视SDK编程指南设计，实现轻量级事件缓冲机制
    /// </summary>
    public class AsyncEventQueue : IDisposable
    {
        private readonly ConcurrentQueue<AccessLogEvent> _queue;
        private readonly ManualResetEventSlim _dataAvailable;
        private readonly int _maxCapacity;
        private volatile bool _disposed = false;
        private long _enqueuedCount = 0;
        private long _dequeuedCount = 0;

        public AsyncEventQueue(int maxCapacity = 10000)
        {
            _queue = new ConcurrentQueue<AccessLogEvent>();
            _dataAvailable = new ManualResetEventSlim(false);
            _maxCapacity = maxCapacity;
        }

        /// <summary>
        /// 当前队列长度
        /// </summary>
        public int Count => _queue.Count;

        /// <summary>
        /// 队列是否为空
        /// </summary>
        public bool IsEmpty => _queue.IsEmpty;

        /// <summary>
        /// 入队事件总数
        /// </summary>
        public long EnqueuedCount => _enqueuedCount;

        /// <summary>
        /// 出队事件总数  
        /// </summary>
        public long DequeuedCount => _dequeuedCount;

        /// <summary>
        /// 异步入队事件（轻量级操作，确保SDK回调快速返回）
        /// </summary>
        /// <param name="eventData">事件数据</param>
        /// <returns>是否成功入队</returns>
        public bool TryEnqueue(AccessLogEvent eventData)
        {
            if (_disposed || eventData == null) return false;

            // 容量保护机制 - 防止内存溢出
            if (_queue.Count >= _maxCapacity)
            {
                Console.WriteLine($"[WARNING] 事件队列已满，当前容量: {_queue.Count}，丢弃事件: {eventData.GetDeduplicationKey()}");
                return false;
            }

            _queue.Enqueue(eventData);
            Interlocked.Increment(ref _enqueuedCount);
            
            // 通知等待的消费者线程有新数据
            _dataAvailable.Set();
            
            return true;
        }

        /// <summary>
        /// 尝试出队事件
        /// </summary>
        /// <param name="eventData">输出的事件数据</param>
        /// <returns>是否成功出队</returns>
        public bool TryDequeue(out AccessLogEvent eventData)
        {
            if (_queue.TryDequeue(out eventData))
            {
                Interlocked.Increment(ref _dequeuedCount);
                
                // 如果队列为空，重置信号
                if (_queue.IsEmpty)
                {
                    _dataAvailable.Reset();
                }
                
                return true;
            }

            return false;
        }

        /// <summary>
        /// 批量出队事件（用于批处理优化）
        /// </summary>
        /// <param name="maxCount">最大出队数量</param>
        /// <returns>出队的事件列表</returns>
        public AccessLogEvent[] DequeueBatch(int maxCount = 50)
        {
            if (maxCount <= 0) return new AccessLogEvent[0];

            var events = new AccessLogEvent[Math.Min(maxCount, _queue.Count)];
            int actualCount = 0;

            for (int i = 0; i < events.Length && TryDequeue(out AccessLogEvent evt); i++)
            {
                events[actualCount++] = evt;
            }

            // 调整数组大小
            if (actualCount < events.Length)
            {
                Array.Resize(ref events, actualCount);
            }

            return events;
        }

        /// <summary>
        /// 等待队列有数据（带超时机制）
        /// </summary>
        /// <param name="timeoutMs">超时时间（毫秒）</param>
        /// <returns>是否有数据可用</returns>
        public bool WaitForData(int timeoutMs = 5000)
        {
            if (_disposed) return false;
            
            // 如果队列不为空，立即返回
            if (!_queue.IsEmpty) return true;
            
            // 等待新数据或超时
            return _dataAvailable.Wait(timeoutMs);
        }

        /// <summary>
        /// 异步等待队列有数据
        /// </summary>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>等待任务</returns>
        public Task<bool> WaitForDataAsync(CancellationToken cancellationToken = default)
        {
            if (_disposed) return Task.FromResult(false);
            
            if (!_queue.IsEmpty) return Task.FromResult(true);
            
            return Task.Run(() => 
            {
                try
                {
                    _dataAvailable.Wait(cancellationToken);
                    return true;
                }
                catch (OperationCanceledException)
                {
                    return false;
                }
            }, cancellationToken);
        }

        /// <summary>
        /// 获取队列统计信息
        /// </summary>
        public string GetStatistics()
        {
            return $"队列状态: 当前长度={Count}, 总入队={EnqueuedCount}, 总出队={DequeuedCount}, 待处理={Count}";
        }

        /// <summary>
        /// 清空队列
        /// </summary>
        public void Clear()
        {
            while (_queue.TryDequeue(out _))
            {
                // 清空所有元素
            }
            _dataAvailable.Reset();
        }

        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;
                _dataAvailable?.Set(); // 唤醒等待的线程
                _dataAvailable?.Dispose();
            }
        }
    }
}