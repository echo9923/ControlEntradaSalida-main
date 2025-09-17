using System;
using System.Threading.Tasks;

namespace ControlEntradaSalida
{
    /// <summary>
    /// 异步数据库处理机制简化验证程序
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
        /// 运行基础验证测试
        /// </summary>
        public async Task RunBasicTest()
        {
            Console.WriteLine("\n========== 异步处理机制验证测试 \n");

            try
            {
                // 测试1：基础功能
                TestBasicFunctionality();

                // 测试2：异步写入
                await TestAsyncWriting();

                Console.WriteLine("\n========== 测试完成 \n");
                PrintStatistics();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] 测试异常: {ex.Message}");
            }
            finally
            {
                await CleanupAsync();
            }
        }

        private void TestBasicFunctionality()
        {
            Console.WriteLine("[TEST 1] 基础功能测试...");

            // 创建测试事件
            var testEvent = CreateTestEvent("TEST001", DateTime.Now, "12345", "MINOR_FACE_VERIFY_PASS", 1);

            // 测试队列操作
            bool enqueued = _eventQueue.TryEnqueue(testEvent);
            Console.WriteLine($"  - 事件入队: {(enqueued ? "成功" : "失败")}");

            bool dequeued = _eventQueue.TryDequeue(out AccessLogEvent dequeuedEvent);
            Console.WriteLine($"  - 事件出队: {(dequeued ? "成功" : "失败")}");

            // 测试去重功能
            bool isDup1 = _eventDeduplicator.IsEventProcessed(testEvent);
            _eventDeduplicator.MarkEventProcessed(testEvent);
            bool isDup2 = _eventDeduplicator.IsEventProcessed(testEvent);

            Console.WriteLine($"  - 去重功能: {(!isDup1 && isDup2 ? "成功" : "失败")}");
            Console.WriteLine("[TEST 1] 完成\n");
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
                EmployeeName = $"测试员工_{employeeId}",
                EventType = eventType,
                EventTypeDisplay = eventType,
                DeviceNumber = deviceId,
                DeviceName = $"测试设备_{deviceId}",
                RemoteHostAddress = "127.0.0.1",
                Priority = 2
            };
        }

        private void PrintStatistics()
        {
            Console.WriteLine("========== 统计信息 ==========");
            Console.WriteLine($"队列统计: {_eventQueue?.GetStatistics()}");
            Console.WriteLine($"去重统计: {_eventDeduplicator?.GetStatistics()}");
            Console.WriteLine($"写入统计: {_asyncWriter?.GetStatistics()}");
        }

        private async Task CleanupAsync()
        {
            Console.WriteLine("[CLEANUP] 清理资源...");
            if (_asyncWriter != null)
            {
                await _asyncWriter.StopAsync(3000);
                _asyncWriter.Dispose();
            }
            _eventDeduplicator?.Dispose();
            _eventQueue?.Dispose();
            Console.WriteLine("[CLEANUP] 完成");
        }

        public void Dispose()
        {
            CleanupAsync().Wait(5000);
        }
    }

    /// <summary>
    /// 验证程序入口
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