using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace ControlEntradaSalida
{
    /// <summary>
    /// 闂ㄧ浜嬩欢鏁版嵁妯″瀷 - 鐢ㄤ簬寮傛鏁版嵁搴撳鐞?
    /// 涓ユ牸鎸夌収娴峰悍濞佽SDK缂栫▼鎸囧崡璁捐锛岀‘淇濊交閲忕骇蹇€熷鐞?
    /// </summary>
    public class AccessLogEvent
    {
        /// <summary>
        /// 搴忓彿锛堜笌鏁版嵁搴?sequence_number 瀵瑰簲锛?
        /// </summary>
        public long SequenceNumber { get; set; }

        /// <summary>
        /// 浜嬩欢鍙戠敓鏃堕棿
        /// </summary>
        public DateTime EventTime { get; set; }

        /// <summary>
        /// 鍛樺伐宸ュ彿
        /// </summary>
        public string EmployeeNumber { get; set; }

        /// <summary>
        /// 鍛樺伐濮撳悕
        /// </summary>
        public string EmployeeName { get; set; }

        /// <summary>
        /// 璁惧缂栧彿
        /// </summary>
        public int DeviceNumber { get; set; }

        /// <summary>
        /// 璁惧鍚嶇О
        /// </summary>
        public string DeviceName { get; set; }

        /// <summary>
        /// 浜嬩欢绫诲瀷锛堝師濮嬫灇涓惧€硷級
        /// </summary>
        public string EventType { get; set; }

        /// <summary>
        /// 浜嬩欢绫诲瀷涓枃鏄剧ず鍊?
        /// </summary>
        public string EventTypeDisplay { get; set; }

        /// <summary>
        /// 杩滅▼涓绘満鍦板潃
        /// </summary>
        public string RemoteHostAddress { get; set; }

        /// <summary>
        /// 浜嬩欢浼樺厛绾э紙1=楂橈紝2=涓紝3=浣庯級
        /// </summary>
        public int Priority { get; set; } = 2;

        /// <summary>
        /// 鍒涘缓鏃堕棿锛堢敤浜庣洃鎺у鐞嗗欢杩燂級
        /// </summary>
        public DateTime CreateTime { get; set; } = DateTime.Now;

        /// <summary>
        /// 閲嶈瘯娆℃暟
        /// </summary>
        public int RetryCount { get; set; } = 0;

        /// <summary>
        /// 鐢熸垚浜嬩欢鍘婚噸鏍囪瘑
        /// </summary>
        public string GetDeduplicationKey()
        {
            return $"{EmployeeNumber}_{DeviceNumber}_{EventTime:yyyy-MM-dd HH:mm:ss}_{EventType}";
        }
    }

    /// <summary>
    /// 绾跨▼瀹夊叏鐨勫紓姝ヤ簨浠堕槦鍒?
    /// 鍩轰簬娴峰悍濞佽SDK缂栫▼鎸囧崡璁捐锛屽疄鐜拌交閲忕骇浜嬩欢缂撳啿鏈哄埗
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
        /// 褰撳墠闃熷垪闀垮害
        /// </summary>
        public int Count => _queue.Count;

        /// <summary>
        /// 闃熷垪鏄惁涓虹┖
        /// </summary>
        public bool IsEmpty => _queue.IsEmpty;

        /// <summary>
        /// 鍏ラ槦浜嬩欢鎬绘暟
        /// </summary>
        public long EnqueuedCount => _enqueuedCount;

        /// <summary>
        /// 鍑洪槦浜嬩欢鎬绘暟  
        /// </summary>
        public long DequeuedCount => _dequeuedCount;

        /// <summary>
        /// 寮傛鍏ラ槦浜嬩欢锛堣交閲忕骇鎿嶄綔锛岀‘淇漇DK鍥炶皟蹇€熻繑鍥烇級
        /// </summary>
        /// <param name="eventData">浜嬩欢鏁版嵁</param>
        /// <returns>鏄惁鎴愬姛鍏ラ槦</returns>
        public bool TryEnqueue(AccessLogEvent eventData)
        {
            if (_disposed || eventData == null) return false;

            // 瀹归噺淇濇姢鏈哄埗 - 闃叉鍐呭瓨婧㈠嚭
            if (_queue.Count >= _maxCapacity)
            {
                Console.WriteLine($"[WARNING] 浜嬩欢闃熷垪宸叉弧锛屽綋鍓嶅閲? {_queue.Count}锛屼涪寮冧簨浠? {eventData.GetDeduplicationKey()}");
                return false;
            }

            _queue.Enqueue(eventData);
            Interlocked.Increment(ref _enqueuedCount);
            
            // 閫氱煡绛夊緟鐨勬秷璐硅€呯嚎绋嬫湁鏂版暟鎹?
            _dataAvailable.Set();
            
            return true;
        }

        /// <summary>
        /// 灏濊瘯鍑洪槦浜嬩欢
        /// </summary>
        /// <param name="eventData">杈撳嚭鐨勪簨浠舵暟鎹?/param>
        /// <returns>鏄惁鎴愬姛鍑洪槦</returns>
        public bool TryDequeue(out AccessLogEvent eventData)
        {
            if (_queue.TryDequeue(out eventData))
            {
                Interlocked.Increment(ref _dequeuedCount);
                
                // 濡傛灉闃熷垪涓虹┖锛岄噸缃俊鍙?
                if (_queue.IsEmpty)
                {
                    _dataAvailable.Reset();
                }
                
                return true;
            }

            return false;
        }

        /// <summary>
        /// 鎵归噺鍑洪槦浜嬩欢锛堢敤浜庢壒澶勭悊浼樺寲锛?
        /// </summary>
        /// <param name="maxCount">鏈€澶у嚭闃熸暟閲?/param>
        /// <returns>鍑洪槦鐨勪簨浠跺垪琛?/returns>
        public AccessLogEvent[] DequeueBatch(int maxCount = 50)
        {
            if (maxCount <= 0) return new AccessLogEvent[0];

            var events = new AccessLogEvent[Math.Min(maxCount, _queue.Count)];
            int actualCount = 0;

            for (int i = 0; i < events.Length && TryDequeue(out AccessLogEvent evt); i++)
            {
                events[actualCount++] = evt;
            }

            // 璋冩暣鏁扮粍澶у皬
            if (actualCount < events.Length)
            {
                Array.Resize(ref events, actualCount);
            }

            return events;
        }

        /// <summary>
        /// 绛夊緟闃熷垪鏈夋暟鎹紙甯﹁秴鏃舵満鍒讹級
        /// </summary>
        /// <param name="timeoutMs">瓒呮椂鏃堕棿锛堟绉掞級</param>
        /// <returns>鏄惁鏈夋暟鎹彲鐢?/returns>
        public bool WaitForData(int timeoutMs = 5000)
        {
            if (_disposed) return false;
            
            // 濡傛灉闃熷垪涓嶄负绌猴紝绔嬪嵆杩斿洖
            if (!_queue.IsEmpty) return true;
            
            // 绛夊緟鏂版暟鎹垨瓒呮椂
            return _dataAvailable.Wait(timeoutMs);
        }

        /// <summary>
        /// 寮傛绛夊緟闃熷垪鏈夋暟鎹?
        /// </summary>
        /// <param name="cancellationToken">鍙栨秷浠ょ墝</param>
        /// <returns>绛夊緟浠诲姟</returns>
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
        /// 鑾峰彇闃熷垪缁熻淇℃伅
        /// </summary>
        public string GetStatistics()
        {
            return $"闃熷垪鐘舵€? 褰撳墠闀垮害={Count}, 鎬诲叆闃?{EnqueuedCount}, 鎬诲嚭闃?{DequeuedCount}, 寰呭鐞?{Count}";
        }

        /// <summary>
        /// 娓呯┖闃熷垪
        /// </summary>
        public void Clear()
        {
            while (_queue.TryDequeue(out _))
            {
                // 娓呯┖鎵€鏈夊厓绱?
            }
            _dataAvailable.Reset();
        }

        /// <summary>
        /// 閲婃斁璧勬簮
        /// </summary>
        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;
                _dataAvailable?.Set(); // 鍞ら啋绛夊緟鐨勭嚎绋?
                _dataAvailable?.Dispose();
            }
        }
    }
}
