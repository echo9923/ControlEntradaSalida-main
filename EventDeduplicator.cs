using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ControlEntradaSalida
{
    /// <summary>
    /// 事件去重器- 防止门禁事件重复处理
    /// 
    /// 解决的问题：
    /// 1. 网络重传：网络不稳定导致SDK重复接收相同事件
    /// 2. 设备故障：设备异常可能重复发送相同时间戳的事件
    /// 3. 系统重启：系统重启后可能重复处理未确认的事件
    /// 
    /// 设计原理：
    /// - 使用事件唯一标识（员工ID + 设备ID + 时间戳 + 事件类型）生成MD5哈希
    /// - 基于时间窗口的缓存管理，保留最近1小时的事件记录
    /// - 定期清理过期记录，防止内存泄漏
    /// - 线程安全设计，支持高并发访问
    /// </summary>
    public class EventDeduplicator : IDisposable
    {
        private readonly ConcurrentDictionary<string, DateTime> _processedEvents;
        private readonly Timer _cleanupTimer;
        private readonly int _maxCacheSize;
        private readonly TimeSpan _cacheExpiry;
        private volatile bool _disposed = false;
        private long _totalEventsChecked = 0;
        private long _duplicateEventsBlocked = 0;

        /// <summary>
        /// 初始化事件去重器
        /// </summary>
        /// <param name="maxCacheSize">最大缓存大小（默认10000条）</param>
        /// <param name="cacheExpiryMinutes">缓存过期时间（分钟，默认60分钟）</param>
        /// <param name="cleanupIntervalMinutes">清理间隔时间（分钟，默认5分钟）</param>
        public EventDeduplicator(int maxCacheSize = 10000, int cacheExpiryMinutes = 60, int cleanupIntervalMinutes = 5)
        {
            _processedEvents = new ConcurrentDictionary<string, DateTime>();
            _maxCacheSize = maxCacheSize;
            _cacheExpiry = TimeSpan.FromMinutes(cacheExpiryMinutes);

            // 创建定期清理任务
            _cleanupTimer = new Timer(CleanupExpiredEvents, null, 
                TimeSpan.FromMinutes(cleanupIntervalMinutes), 
                TimeSpan.FromMinutes(cleanupIntervalMinutes));
        }

        /// <summary>
        /// 检查事件是否已经被处理过
        /// </summary>
        /// <param name="accessEvent">门禁事件</param>
        /// <returns>true表示已处理过（应丢弃），false表示未处理过（应继续处理）</returns>
        public bool IsEventProcessed(AccessLogEvent accessEvent)
        {
            if (_disposed || accessEvent == null) return true;

            Interlocked.Increment(ref _totalEventsChecked);

            string eventKey = GenerateEventKey(accessEvent);
            bool isProcessed = _processedEvents.ContainsKey(eventKey);

            if (isProcessed)
            {
                Interlocked.Increment(ref _duplicateEventsBlocked);
                Console.WriteLine($"[DUPLICATE] 检测到重复事件并丢弃: {accessEvent.GetDeduplicationKey()}");
            }

            return isProcessed;
        }

        /// <summary>
        /// 标记事件为已处理
        /// </summary>
        /// <param name="accessEvent">门禁事件</param>
        public void MarkEventProcessed(AccessLogEvent accessEvent)
        {
            if (_disposed || accessEvent == null) return;

            string eventKey = GenerateEventKey(accessEvent);
            DateTime processTime = DateTime.Now;

            // 容量保护 - 如果缓存过大，先清理一次
            if (_processedEvents.Count >= _maxCacheSize)
            {
                CleanupExpiredEvents(null);
            }

            // 如果仍然超过容量，删除最旧的记录
            if (_processedEvents.Count >= _maxCacheSize)
            {
                RemoveOldestEntries(_maxCacheSize / 10); // 删除10%的最旧记录
            }

            _processedEvents.TryAdd(eventKey, processTime);
        }

        /// <summary>
        /// 生成事件唯一标识
        /// </summary>
        /// <param name="accessEvent">门禁事件</param>
        /// <returns>事件唯一标识（MD5哈希）</returns>
        private string GenerateEventKey(AccessLogEvent accessEvent)
        {
            // 构造唯一标识字符串
            string uniqueString = $"{accessEvent.EmployeeNumber}_{accessEvent.DeviceNumber}_{accessEvent.EventTime:yyyy-MM-dd HH:mm:ss}_{accessEvent.EventType}";
            
            // 生成MD5哈希
            using (MD5 md5 = MD5.Create())
            {
                byte[] hashBytes = md5.ComputeHash(Encoding.UTF8.GetBytes(uniqueString));
                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < hashBytes.Length; i++)
                {
                    sb.Append(hashBytes[i].ToString("X2"));
                }
                return sb.ToString();
            }
        }

        /// <summary>
        /// 清理过期的事件记录（定时任务）
        /// </summary>
        /// <param name="state">定时器状态（未使用）</param>
        private void CleanupExpiredEvents(object state)
        {
            if (_disposed) return;

            try
            {
                DateTime cutoffTime = DateTime.Now - _cacheExpiry;
                int removedCount = 0;

                // 查找并删除过期记录
                var keysToRemove = new List<string>();
                foreach (var kvp in _processedEvents)
                {
                    if (kvp.Value < cutoffTime)
                    {
                        keysToRemove.Add(kvp.Key);
                    }
                }

                // 删除过期记录
                foreach (string key in keysToRemove)
                {
                    if (_processedEvents.TryRemove(key, out _))
                    {
                        removedCount++;
                    }
                }

                if (removedCount > 0)
                {
                    Console.WriteLine($"[CLEANUP] 清理过期去重记录: {removedCount}条，剩余: {_processedEvents.Count}条");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] 去重器清理任务异常: {ex.Message}");
            }
        }

        /// <summary>
        /// 删除最旧的记录（容量保护）
        /// </summary>
        /// <param name="countToRemove">要删除的记录数量</param>
        private void RemoveOldestEntries(int countToRemove)
        {
            if (countToRemove <= 0) return;

            try
            {
                // 按时间排序，删除最旧的记录
                var sortedEntries = _processedEvents.OrderBy(kvp => kvp.Value).Take(countToRemove);
                int removedCount = 0;

                foreach (var entry in sortedEntries)
                {
                    if (_processedEvents.TryRemove(entry.Key, out _))
                    {
                        removedCount++;
                    }
                }

                Console.WriteLine($"[CAPACITY] 容量维护删除最旧记录: {removedCount}条，剩余: {_processedEvents.Count}条");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] 删除最旧记录时异常: {ex.Message}");
            }
        }

        /// <summary>
        /// 获取去重器统计信息
        /// </summary>
        /// <returns>统计信息字符串</returns>
        public string GetStatistics()
        {
            return $"去重器状态: 缓存记录={_processedEvents.Count}, 总检测数={_totalEventsChecked}, 拦截重复数={_duplicateEventsBlocked}, 去重率={GetDeduplicationRate():P2}";
        }

        /// <summary>
        /// 获取去重率
        /// </summary>
        /// <returns>去重率（0-1之间的值）</returns>
        public double GetDeduplicationRate()
        {
            long totalChecked = _totalEventsChecked;
            long duplicateBlocked = _duplicateEventsBlocked;
            
            return totalChecked > 0 ? (double)duplicateBlocked / totalChecked : 0.0;
        }

        /// <summary>
        /// 清空所有缓存记录
        /// </summary>
        public void Clear()
        {
            _processedEvents.Clear();
            _totalEventsChecked = 0;
            _duplicateEventsBlocked = 0;
            Console.WriteLine("[CLEAR] 已清空去重器缓存");
        }

        /// <summary>
        /// 手动触发清理过期记录
        /// </summary>
        public void ForceCleanup()
        {
            CleanupExpiredEvents(null);
        }

        /// <summary>
        /// 检查去重器健康状态
        /// </summary>
        /// <returns>健康状态报告</returns>
        public string GetHealthStatus()
        {
            int cacheSize = _processedEvents.Count;
            double memoryUsageRatio = (double)cacheSize / _maxCacheSize;
            
            string status;
            if (memoryUsageRatio < 0.7)
                status = "健康";
            else if (memoryUsageRatio < 0.9)
                status = "警告";
            else
                status = "危险";

            return $"去重器健康状态: {status} (缓存使用率: {memoryUsageRatio:P1})";
        }

        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;
                _cleanupTimer?.Dispose();
                _processedEvents?.Clear();
                Console.WriteLine("[DISPOSE] 事件去重器已释放资源");
            }
        }
    }
}