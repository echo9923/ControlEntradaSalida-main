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
    /// 浜嬩欢鍘婚噸鍣?- 闃叉闂ㄧ浜嬩欢閲嶅澶勭悊
    /// 
    /// 瑙ｅ喅鐨勯棶棰橈細
    /// 1. 缃戠粶閲嶄紶锛氱綉缁滀笉绋冲畾瀵艰嚧SDK閲嶅鎺ユ敹鐩稿悓浜嬩欢
    /// 2. 璁惧鏁呴殰锛氳澶囧紓甯稿彲鑳介噸澶嶅彂閫佺浉鍚屾椂闂存埑鐨勪簨浠?
    /// 3. 绯荤粺閲嶅惎锛氱郴缁熼噸鍚悗鍙兘閲嶅澶勭悊鏈‘璁ょ殑浜嬩欢
    /// 
    /// 璁捐鍘熺悊锛?
    /// - 浣跨敤浜嬩欢鍞竴鏍囪瘑锛堝憳宸D + 璁惧ID + 鏃堕棿鎴?+ 浜嬩欢绫诲瀷锛夌敓鎴怣D5鍝堝笇
    /// - 鍩轰簬鏃堕棿绐楀彛鐨勭紦瀛樼鐞嗭紝淇濈暀鏈€杩?灏忔椂鐨勪簨浠惰褰?
    /// - 瀹氭湡娓呯悊杩囨湡璁板綍锛岄槻姝㈠唴瀛樻硠婕?
    /// - 绾跨▼瀹夊叏璁捐锛屾敮鎸侀珮骞跺彂璁块棶
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
        /// 鍒濆鍖栦簨浠跺幓閲嶅櫒
        /// </summary>
        /// <param name="maxCacheSize">鏈€澶х紦瀛樺ぇ灏忥紙榛樿10000鏉★級</param>
        /// <param name="cacheExpiryMinutes">缂撳瓨杩囨湡鏃堕棿锛堝垎閽燂紝榛樿60鍒嗛挓锛?/param>
        /// <param name="cleanupIntervalMinutes">娓呯悊闂撮殧鏃堕棿锛堝垎閽燂紝榛樿5鍒嗛挓锛?/param>
        public EventDeduplicator(int maxCacheSize = 10000, int cacheExpiryMinutes = 60, int cleanupIntervalMinutes = 5)
        {
            _processedEvents = new ConcurrentDictionary<string, DateTime>();
            _maxCacheSize = maxCacheSize;
            _cacheExpiry = TimeSpan.FromMinutes(cacheExpiryMinutes);

            // 鍒涘缓瀹氭湡娓呯悊浠诲姟
            _cleanupTimer = new Timer(CleanupExpiredEvents, null, 
                TimeSpan.FromMinutes(cleanupIntervalMinutes), 
                TimeSpan.FromMinutes(cleanupIntervalMinutes));
        }

        /// <summary>
        /// 妫€鏌ヤ簨浠舵槸鍚﹀凡缁忚澶勭悊杩?
        /// </summary>
        /// <param name="accessEvent">闂ㄧ浜嬩欢</param>
        /// <returns>true琛ㄧず宸插鐞嗚繃锛堝簲涓㈠純锛夛紝false琛ㄧず鏈鐞嗚繃锛堝簲缁х画澶勭悊锛?/returns>
        public bool IsEventProcessed(AccessLogEvent accessEvent)
        {
            if (_disposed || accessEvent == null) return true;

            Interlocked.Increment(ref _totalEventsChecked);

            string eventKey = GenerateEventKey(accessEvent);
            bool isProcessed = _processedEvents.ContainsKey(eventKey);

            if (isProcessed)
            {
                Interlocked.Increment(ref _duplicateEventsBlocked);
                Console.WriteLine($"[DUPLICATE] 妫€娴嬪埌閲嶅浜嬩欢骞朵涪寮? {accessEvent.GetDeduplicationKey()}");
            }

            return isProcessed;
        }

        /// <summary>
        /// 鏍囪浜嬩欢涓哄凡澶勭悊
        /// </summary>
        /// <param name="accessEvent">闂ㄧ浜嬩欢</param>
        public void MarkEventProcessed(AccessLogEvent accessEvent)
        {
            if (_disposed || accessEvent == null) return;

            string eventKey = GenerateEventKey(accessEvent);
            DateTime processTime = DateTime.Now;

            // 瀹归噺淇濇姢 - 濡傛灉缂撳瓨杩囧ぇ锛屽厛娓呯悊涓€娆?
            if (_processedEvents.Count >= _maxCacheSize)
            {
                CleanupExpiredEvents(null);
            }

            // 濡傛灉浠嶇劧瓒呰繃瀹归噺锛屽垹闄ゆ渶鏃х殑璁板綍
            if (_processedEvents.Count >= _maxCacheSize)
            {
                RemoveOldestEntries(_maxCacheSize / 10); // 鍒犻櫎10%鐨勬渶鏃ц褰?
            }

            _processedEvents.TryAdd(eventKey, processTime);
        }

        /// <summary>
        /// 鐢熸垚浜嬩欢鍞竴鏍囪瘑
        /// </summary>
        /// <param name="accessEvent">闂ㄧ浜嬩欢</param>
        /// <returns>浜嬩欢鍞竴鏍囪瘑锛圡D5鍝堝笇锛?/returns>
        private string GenerateEventKey(AccessLogEvent accessEvent)
        {
            // 鏋勯€犲敮涓€鏍囪瘑瀛楃涓?
            string uniqueString = $"{accessEvent.EmployeeNumber}_{accessEvent.DeviceNumber}_{accessEvent.EventTime:yyyy-MM-dd HH:mm:ss}_{accessEvent.EventType}";
            
            // 鐢熸垚MD5鍝堝笇
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
        /// 娓呯悊杩囨湡鐨勪簨浠惰褰曪Paper瀹氭椂浠诲姟锛?
        /// </summary>
        /// <param name="state">瀹氭椂鍣ㄧ姸鎬侊紙鏈娇鐢級</param>
        private void CleanupExpiredEvents(object state)
        {
            if (_disposed) return;

            try
            {
                DateTime cutoffTime = DateTime.Now - _cacheExpiry;
                int removedCount = 0;

                // 鏌ユ壘骞跺垹闄よ繃鏈熻褰?
                var keysToRemove = new List<string>();
                foreach (var kvp in _processedEvents)
                {
                    if (kvp.Value < cutoffTime)
                    {
                        keysToRemove.Add(kvp.Key);
                    }
                }

                // 鍒犻櫎杩囨湡璁板綍
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
        /// 鍒犻櫎鏈€鏃х殑璁板綍锛堝閲忎繚鎶わ級
        /// </summary>
        /// <param name="countToRemove">瑕佸垹闄ょ殑璁板綍鏁伴噺</param>
        private void RemoveOldestEntries(int countToRemove)
        {
            if (countToRemove <= 0) return;

            try
            {
                // 鎸夋椂闂存帓搴忥紝鍒犻櫎鏈€鏃х殑璁板綍
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
        /// 鑾峰彇鍘婚噸鐜?
        /// </summary>
        /// <returns>鍘婚噸鐜囷紙0-1涔嬮棿鐨勫€硷級</returns>
        public double GetDeduplicationRate()
        {
            long totalChecked = _totalEventsChecked;
            long duplicateBlocked = _duplicateEventsBlocked;
            
            return totalChecked > 0 ? (double)duplicateBlocked / totalChecked : 0.0;
        }

        /// <summary>
        /// 娓呯┖鎵€鏈夌紦瀛樿褰?
        /// </summary>
        public void Clear()
        {
            _processedEvents.Clear();
            _totalEventsChecked = 0;
            _duplicateEventsBlocked = 0;
            Console.WriteLine("[CLEAR] 宸叉竻绌哄幓閲嶅櫒缂撳瓨");
        }

        /// <summary>
        /// 鎵嬪姩瑙﹀彂娓呯悊杩囨湡璁板綍
        /// </summary>
        public void ForceCleanup()
        {
            CleanupExpiredEvents(null);
        }

        /// <summary>
        /// 妫€鏌ュ幓閲嶅櫒鍋ュ悍鐘舵€?
        /// </summary>
        /// <returns>鍋ュ悍鐘舵€佹姤鍛?/returns>
        public string GetHealthStatus()
        {
            int cacheSize = _processedEvents.Count;
            double memoryUsageRatio = (double)cacheSize / _maxCacheSize;
            
            string status;
            if (memoryUsageRatio < 0.7)
                status = "鍋ュ悍";
            else if (memoryUsageRatio < 0.9)
                status = "璀﹀憡";
            else
                status = "鍗遍櫓";

            return $"鍘婚噸鍣ㄥ仴搴风姸鎬? {status} (缂撳瓨浣跨敤鐜? {memoryUsageRatio:P1})";
        }

        /// <summary>
        /// 閲婃斁璧勬簮
        /// </summary>
        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;
                _cleanupTimer?.Dispose();
                _processedEvents?.Clear();
                Console.WriteLine("[DISPOSE] 浜嬩欢鍘婚噸鍣ㄥ凡閲婃斁璧勬簮");
            }
        }
    }
}
