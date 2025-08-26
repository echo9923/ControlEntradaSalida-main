using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;

namespace ControlEntradaSalida.Tests
{
    /// <summary>
    /// 设备连接管理器单元测试
    /// </summary>
    public class DeviceConnectionManagerTests
    {
        private DeviceConnectionManager _manager;
        private List<DeviceConnectionInfo> _testDevices;
        private List<string> _eventLog;

        public DeviceConnectionManagerTests()
        {
            InitializeTests();
        }

        /// <summary>
        /// 初始化测试环境
        /// </summary>
        private void InitializeTests()
        {
            _manager = DeviceConnectionManager.Instance;
            _eventLog = new List<string>();
            
            // 订阅事件以进行测试验证
            _manager.DeviceStatusChanged += OnDeviceStatusChanged;
            _manager.DeviceConnectionStateChanged += OnDeviceConnectionStateChanged;
            _manager.DeviceReconnectAttempt += OnDeviceReconnectAttempt;
            _manager.DeviceError += OnDeviceError;

            // 创建测试设备
            _testDevices = new List<DeviceConnectionInfo>
            {
                new DeviceConnectionInfo
                {
                    Id = 1001,
                    Name = "测试设备1",
                    IpAddress = "192.168.1.100",
                    Port = "8000",
                    Username = "admin",
                    Password = "12345",
                    IsEnabled = true
                },
                new DeviceConnectionInfo
                {
                    Id = 1002,
                    Name = "测试设备2",
                    IpAddress = "192.168.1.101",
                    Port = "8000", 
                    Username = "admin",
                    Password = "12345",
                    IsEnabled = true
                },
                new DeviceConnectionInfo
                {
                    Id = 1003,
                    Name = "测试设备3-禁用",
                    IpAddress = "192.168.1.102",
                    Port = "8000",
                    Username = "admin", 
                    Password = "12345",
                    IsEnabled = false
                }
            };
        }

        /// <summary>
        /// 运行所有测试
        /// </summary>
        public void RunAllTests()
        {
            Console.WriteLine("========== 开始设备连接管理器测试 ==========");
            
            try
            {
                TestReconnectManager();
                TestDeviceStatusEngine();
                TestConcurrentConnections();
                TestEventSystem();
                TestDeviceConnectionInfo();
                
                Console.WriteLine("\n========== 所有测试通过 ==========");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\n测试失败: {ex.Message}");
                Console.WriteLine($"堆栈跟踪: {ex.StackTrace}");
            }
        }

        /// <summary>
        /// 测试重连管理器
        /// </summary>
        public void TestReconnectManager()
        {
            Console.WriteLine("\n--- 测试重连管理器 ---");
            
            var reconnectManager = new ReconnectManager();
            int reconnectEventCount = 0;
            
            // 订阅重连事件
            reconnectManager.ReconnectAttemptStarted += (sender, e) => 
            {
                reconnectEventCount++;
                Console.WriteLine($"重连尝试开始: 设备{e.DeviceId}, 第{e.Attempts}次, 延迟{e.NextDelay.TotalSeconds}秒");
            };
            
            // 测试指数退避算法
            Console.WriteLine("测试指数退避算法:");
            for (int i = 0; i < 5; i++)
            {
                var delay = reconnectManager.GetNextRetryDelay(i);
                Console.WriteLine($"第{i+1}次重连延迟: {delay.TotalSeconds:F2}秒");
                AssertTrue(delay.TotalSeconds >= 1, $"重连延迟应该至少1秒，实际: {delay.TotalSeconds}");
            }
            
            // 测试重连状态管理
            Console.WriteLine("\n测试重连状态管理:");
            reconnectManager.ScheduleReconnect(1001, "测试重连");
            
            var state = reconnectManager.GetReconnectState(1001);
            AssertNotNull(state, "重连状态不应为空");
            AssertEqual(1, state.Attempts, "重连次数应为1");
            
            // 测试重连成功后状态重置
            reconnectManager.ResetReconnectState(1001);
            state = reconnectManager.GetReconnectState(1001);
            AssertEqual(0, state.Attempts, "重连状态重置后次数应为0");
            
            // 测试最大重连次数限制
            Console.WriteLine("\n测试最大重连次数限制:");
            for (int i = 0; i < 12; i++)
            {
                reconnectManager.ScheduleReconnect(1002, $"测试重连第{i+1}次");
            }
            
            state = reconnectManager.GetReconnectState(1002);
            AssertTrue(state.IsPermanentFailure, "达到最大重连次数后应标记为永久失败");
            
            reconnectManager.Dispose();
            Console.WriteLine("重连管理器测试完成");
        }

        /// <summary>
        /// 测试设备状态引擎
        /// </summary>
        public void TestDeviceStatusEngine()
        {
            Console.WriteLine("\n--- 测试设备状态引擎 ---");
            
            var statusEngine = new DeviceStatusEngine();
            
            // 测试无效连接验证
            Console.WriteLine("测试无效连接验证:");
            bool isValid = statusEngine.ValidateConnection(-1);
            AssertFalse(isValid, "无效UserID应该返回false");
            
            // 测试设备工作状态获取
            Console.WriteLine("测试设备工作状态获取:");
            var workStatus = statusEngine.GetDeviceWorkStatus(-1);
            AssertNotNull(workStatus, "工作状态不应为空");
            AssertEqual(DeviceStatus.Unknown, workStatus.Status, "无效UserID应返回未知状态");
            
            // 测试设备能力获取
            Console.WriteLine("测试设备能力获取:");
            var capabilities = statusEngine.GetDeviceCapabilities(-1);
            AssertNotNull(capabilities, "设备能力信息不应为空");
            
            // 测试连通性测试
            Console.WriteLine("测试设备连通性:");
            bool connectivity = statusEngine.TestConnectivity(-1);
            AssertFalse(connectivity, "无效UserID连通性测试应返回false");
            
            Console.WriteLine("设备状态引擎测试完成");
        }

        /// <summary>
        /// 测试并发连接控制
        /// </summary>
        public void TestConcurrentConnections()
        {
            Console.WriteLine("\n--- 测试并发连接控制 ---");
            
            var device = _testDevices[0];
            var tasks = new List<Task<bool>>();
            var results = new List<bool>();
            var connectionTimes = new List<TimeSpan>();
            
            // 创建多个并发连接任务
            Console.WriteLine("创建10个并发连接任务:");
            for (int i = 0; i < 10; i++)
            {
                int taskId = i;
                tasks.Add(Task.Run(async () =>
                {
                    var stopwatch = Stopwatch.StartNew();
                    try
                    {
                        var result = await _manager.ConnectToDeviceAsync(device);
                        stopwatch.Stop();
                        lock (results)
                        {
                            results.Add(result);
                            connectionTimes.Add(stopwatch.Elapsed);
                        }
                        Console.WriteLine($"任务{taskId}: 连接{(result ? "成功" : "失败")}, 耗时: {stopwatch.ElapsedMilliseconds}ms");
                        return result;
                    }
                    catch (Exception ex)
                    {
                        stopwatch.Stop();
                        Console.WriteLine($"任务{taskId}: 异常 - {ex.Message}");
                        return false;
                    }
                }));
            }
            
            // 等待所有任务完成
            Task.WaitAll(tasks.ToArray(), TimeSpan.FromSeconds(30));
            
            Console.WriteLine($"并发连接测试完成: {results.Count}个任务完成");
            Console.WriteLine($"平均连接时间: {connectionTimes.Average(t => t.TotalMilliseconds):F2}ms");
            Console.WriteLine($"最大连接时间: {connectionTimes.Max(t => t.TotalMilliseconds):F2}ms");
            
            // 验证并发控制是否正常工作（没有异常或死锁）
            AssertEqual(10, results.Count, "所有并发任务都应完成");
        }

        /// <summary>
        /// 测试事件系统
        /// </summary>
        public void TestEventSystem()
        {
            Console.WriteLine("\n--- 测试事件系统 ---");
            
            _eventLog.Clear();
            
            var device = _testDevices[0];
            
            // 测试设备状态更新
            Console.WriteLine("测试设备状态更新:");
            device.UpdateStatus(DeviceStatus.Online, "测试状态更新");
            
            AssertEqual(DeviceStatus.Online, device.Status, "设备状态应已更新");
            AssertEqual("测试状态更新", device.StatusMessage, "状态消息应已更新");
            
            // 测试连接成功记录
            Console.WriteLine("测试连接成功记录:");
            int previousSuccessCount = device.SuccessfulConnectionCount;
            device.RecordConnectionSuccess();
            AssertEqual(previousSuccessCount + 1, device.SuccessfulConnectionCount, "成功连接计数应增加");
            
            // 测试连接失败记录
            Console.WriteLine("测试连接失败记录:");
            int previousFailureCount = device.ConnectionFailureCount;
            device.RecordConnectionFailure(123, "测试错误");
            AssertEqual(previousFailureCount + 1, device.ConnectionFailureCount, "失败连接计数应增加");
            AssertEqual((uint)123, device.LastErrorCode, "错误码应已记录");
            
            Console.WriteLine($"记录的事件数量: {_eventLog.Count}");
            Console.WriteLine("事件系统测试完成");
        }

        /// <summary>
        /// 测试DeviceConnectionInfo扩展功能
        /// </summary>
        public void TestDeviceConnectionInfo()
        {
            Console.WriteLine("\n--- 测试DeviceConnectionInfo扩展功能 ---");
            
            var device = _testDevices[0];
            
            // 测试线程安全锁
            Console.WriteLine("测试线程安全锁:");
            AssertNotNull(device.LockObject, "锁对象不应为空");
            
            // 测试连接成功率计算
            Console.WriteLine("测试连接成功率计算:");
            device.SuccessfulConnectionCount = 8;
            device.ConnectionFailureCount = 2;
            AssertEqual(80.0, device.ConnectionSuccessRate, "连接成功率应为80%");
            
            // 测试重连状态重置
            Console.WriteLine("测试重连状态重置:");
            device.ReconnectAttempts = 5;
            device.IsReconnecting = true;
            device.ResetReconnectState();
            
            AssertEqual(0, device.ReconnectAttempts, "重连次数应重置为0");
            AssertFalse(device.IsReconnecting, "重连状态应重置为false");
            
            Console.WriteLine("DeviceConnectionInfo测试完成");
        }

        #region 事件处理方法

        private void OnDeviceStatusChanged(object sender, DeviceStatusChangedEventArgs e)
        {
            _eventLog.Add($"状态改变: {e.Device.Name} - {e.PreviousStatus} -> {e.CurrentStatus}");
        }

        private void OnDeviceConnectionStateChanged(object sender, DeviceConnectionEventArgs e)
        {
            _eventLog.Add($"连接状态改变: {e.Device.Name} - {(e.Success ? "成功" : "失败")}");
        }

        private void OnDeviceReconnectAttempt(object sender, DeviceReconnectEventArgs e)
        {
            _eventLog.Add($"重连尝试: 设备{e.DeviceId} - 第{e.Attempts}次");
        }

        private void OnDeviceError(object sender, DeviceErrorEventArgs e)
        {
            _eventLog.Add($"设备错误: {e.Device?.Name ?? "Unknown"} - {e.ErrorMessage}");
        }

        #endregion

        #region 断言方法

        private void AssertTrue(bool condition, string message)
        {
            if (!condition)
            {
                throw new Exception($"断言失败: {message}");
            }
        }

        private void AssertFalse(bool condition, string message)
        {
            if (condition)
            {
                throw new Exception($"断言失败: {message}");
            }
        }

        private void AssertEqual<T>(T expected, T actual, string message)
        {
            if (!EqualityComparer<T>.Default.Equals(expected, actual))
            {
                throw new Exception($"断言失败: {message}. 期望: {expected}, 实际: {actual}");
            }
        }

        private void AssertNotNull(object obj, string message)
        {
            if (obj == null)
            {
                throw new Exception($"断言失败: {message}");
            }
        }

        #endregion
    }

    /// <summary>
    /// 测试运行器
    /// </summary>
    public static class DeviceConnectionTestRunner
    {
        public static void RunTests()
        {
            try
            {
                var tests = new DeviceConnectionManagerTests();
                tests.RunAllTests();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"测试运行器异常: {ex.Message}");
            }
        }
    }
}