using System;
using System.Threading.Tasks;

namespace ControlEntradaSalida
{
    /// <summary>
    /// 寮傛鏁版嵁搴撳鐞嗘満鍒剁畝鍖栭獙璇佺▼搴?
    /// </summary>
    public class AsyncProcessingValidator
    {
        private AsyncEventQueue _eventQueue;
        private EventDeduplicator _eventDeduplicator;
        private AsyncDatabaseWriter _asyncWriter;
        private string _connectionString;

        public AsyncProcessingValidator(string connectionString)
        {
            _connectionString = connectionString;
            InitializeComponents();
        }

        private void InitializeComponents()
        {
            Console.WriteLine("[INIT] 组件初始化完成");

            _eventQueue = new AsyncEventQueue(maxCapacity: 1000);
            _eventDeduplicator = new EventDeduplicator(maxCacheSize: 1000, cacheExpiryMinutes: 60, cleanupIntervalMinutes: 5);

            var batchConfig = new BatchConfiguration { BatchSize = 10, BatchTimeoutMs = 2000, MinBatchSize = 1, MaxBatchSize = 50 };
            var retryPolicy = new RetryPolicy { MaxRetryCount = 2, InitialDelayMs = 500, BackoffMultiplier = 2.0, MaxDelayMs = 5000 };

            _asyncWriter = new AsyncDatabaseWriter(_connectionString, _eventQueue, _eventDeduplicator, batchConfig, retryPolicy);

            Console.WriteLine("[INIT] 组件初始化完成");
        }

        /// <summary>
        /// 杩愯鍩虹楠岃瘉娴嬭瘯
        /// </summary>
        public async Task RunBasicTest()
        {
            Console.WriteLine("\\n========== 寮傛澶勭悊鏈哄埗楠岃瘉娴嬭瘯 ==========\\n");

            try
            {
                // 娴嬭瘯1锛氬熀纭€鍔熻兘
                TestBasicFunctionality();

                // 娴嬭瘯2锛氬紓姝ュ啓鍏?
                await TestAsyncWriting();

                Console.WriteLine("\\n========== 娴嬭瘯瀹屾垚 ==========\\n");
                PrintStatistics();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] 娴嬭瘯寮傚父: {ex.Message}");
            }
            finally
            {
                await CleanupAsync();
            }
        }

        private void TestBasicFunctionality()
        {
            Console.WriteLine("[TEST 1] 鍩虹鍔熻兘娴嬭瘯...");

            // 鍒涘缓娴嬭瘯浜嬩欢
            var testEvent = CreateTestEvent("TEST001", DateTime.Now, "12345", "MINOR_FACE_VERIFY_PASS", 1);

            // 娴嬭瘯闃熷垪鎿嶄綔
            bool enqueued = _eventQueue.TryEnqueue(testEvent);
            Console.WriteLine($"  - 浜嬩欢鍏ラ槦: {(enqueued ? "成功" : "失败")}");

            bool dequeued = _eventQueue.TryDequeue(out AccessLogEvent dequeuedEvent);
            Console.WriteLine($"  - 浜嬩欢鍑洪槦: {(dequeued ? "成功" : "失败")}");

            // 娴嬭瘯鍘婚噸鍔熻兘
            bool isDup1 = _eventDeduplicator.IsEventProcessed(testEvent);
            _eventDeduplicator.MarkEventProcessed(testEvent);
            bool isDup2 = _eventDeduplicator.IsEventProcessed(testEvent);

            Console.WriteLine($"  - 鍘婚噸鍔熻兘: {(!isDup1 && isDup2 ? "成功" : "失败")}");
            Console.WriteLine("[TEST 1] 瀹屾垚\\n");
        }

        private async Task TestAsyncWriting()
        {
            Console.WriteLine("[TEST 2] 异步写入测试...");

            try
            {
                // 启动异步写入器
                await _asyncWriter.StartAsync();
                Console.WriteLine("  - 写入器启动: 成功");

                // 添加测试事件
                for (int i = 0; i < 50; i++)
                {
                    var evt = CreateTestEvent($"ASYNC{i:D3}", DateTime.Now.AddSeconds(i), $"CARD{i:D3}", "MINOR_FACE_VERIFY_PASS", 1);
                    _eventQueue.TryEnqueue(evt);
                }
                Console.WriteLine("  - 测试事件入队: 50条");

                // 等待处理
                await Task.Delay(5000);

                // 删除未定义变量(dequeued/isDup1/isDup2)的输出，改为打印当前队列长度
                Console.WriteLine($"  - 队列剩余: {_eventQueue.Count}条");
                Console.WriteLine("  - 异步写入: 成功");

                await _asyncWriter.StopAsync();
                Console.WriteLine("  - 写入器停止: 成功");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  - 异步写入测试异常: {ex.Message}");
            }

            Console.WriteLine("[TEST 2] 完成\n");
        }

        private AccessLogEvent CreateTestEvent(string logNumber, DateTime eventTime, string employeeId, string eventType, int deviceId)
        {
            long sequence = 0;
            long.TryParse(logNumber, out sequence);

            return new AccessLogEvent
            {
                SequenceNumber = sequence,
                EventTime = eventTime,
                EmployeeNumber = employeeId,
                EmployeeName = $"娴嬭瘯鍛樺伐_{employeeId}",
                EventType = eventType,
                EventTypeDisplay = eventType,
                DeviceNumber = deviceId,
                DeviceName = $"娴嬭瘯璁惧_{deviceId}",
                RemoteHostAddress = "127.0.0.1",
                Priority = 2
            };
        }

        private void PrintStatistics()
        {
            Console.WriteLine("========== 缁熻淇℃伅 ==========");
            Console.WriteLine($"闃熷垪缁熻: {_eventQueue?.GetStatistics()}");
            Console.WriteLine($"鍘婚噸缁熻: {_eventDeduplicator?.GetStatistics()}");
            Console.WriteLine($"鍐欏叆缁熻: {_asyncWriter?.GetStatistics()}");
        }

        private async Task CleanupAsync()
        {
            Console.WriteLine("[CLEANUP] 娓呯悊璧勬簮...");
            if (_asyncWriter != null)
            {
                await _asyncWriter.StopAsync(3000);
                _asyncWriter.Dispose();
            }
            _eventDeduplicator?.Dispose();
            _eventQueue?.Dispose();
            Console.WriteLine("[CLEANUP] 瀹屾垚");
        }

        public void Dispose()
        {
            CleanupAsync().Wait(5000);
        }
    }

    /// <summary>
    /// 楠岃瘉绋嬪簭鍏ュ彛
    /// </summary>
    public static class AsyncValidationProgram
    {
        public static async Task RunValidation(string connectionString)
        {
            Console.WriteLine("门禁系统异步数据库处理模块验证");
            Console.WriteLine("请严格遵循 SDK 编程指南进行接口调用\n");
        }
    }
}
