using System;
using System.Threading;
using System.Threading.Tasks;

namespace ControlEntradaSalida
{
    /// <summary>
    /// 设备连接管理模块功能演示程序
    /// </summary>
    public class DeviceConnectionDemo
    {
        private DeviceConnectionManager _manager;
        private bool _isRunning = false;

        public DeviceConnectionDemo()
        {
            _manager = DeviceConnectionManager.Instance;
            
            // 订阅事件以观察系统行为
            _manager.DeviceStatusChanged += OnDeviceStatusChanged;
            _manager.DeviceConnectionStateChanged += OnDeviceConnectionStateChanged;
            _manager.DeviceReconnectAttempt += OnDeviceReconnectAttempt;
            _manager.DeviceError += OnDeviceError;
        }

        /// <summary>
        /// 运行演示
        /// </summary>
        public async Task RunDemo()
        {
            Console.WriteLine("========== 设备连接管理模块演示程序 ==========");
            Console.WriteLine("按 'q' 退出程序\n");

            _isRunning = true;

            // 启动演示任务
            var demoTask = RunDemoTasks();
            
            // 监听用户输入
            await Task.Run(() =>
            {
                while (_isRunning)
                {
                    var key = Console.ReadKey(true);
                    if (key.KeyChar == 'q' || key.KeyChar == 'Q')
                    {
                        _isRunning = false;
                        break;
                    }
                }
            });

            await demoTask;
            
            Console.WriteLine("\n程序已退出");
        }

        /// <summary>
        /// 运行演示任务
        /// </summary>
        private async Task RunDemoTasks()
        {
            try
            {
                // 演示1：基本功能测试
                await DemoBasicFunctionality();
                
                if (!_isRunning) return;
                
                // 演示2：重连机制测试
                await DemoReconnectMechanism();
                
                if (!_isRunning) return;
                
                // 演示3：并发连接测试
                await DemoConcurrentConnections();
                
                if (!_isRunning) return;
                
                // 演示4：状态监控测试
                await DemoStatusMonitoring();
                
                // 演示5：运行单元测试
                if (_isRunning)
                {
                    Console.WriteLine("\n========== 运行单元测试 ==========");
                    DeviceConnectionTestRunner.RunTests();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"演示过程中发生异常: {ex.Message}");
            }
        }

        /// <summary>
        /// 演示基本功能
        /// </summary>
        private async Task DemoBasicFunctionality()
        {
            Console.WriteLine("========== 基本功能演示 ==========");
            
            // 加载设备信息
            Console.WriteLine("1. 加载设备信息...");
            _manager.LoadAllDevices();
            
            var devices = _manager.GetAllDevices();
            Console.WriteLine($"   已加载 {devices.Count} 个设备");
            
            foreach (var device in devices)
            {
                Console.WriteLine($"   - {device.Name} ({device.IpAddress}:{device.Port}) " +
                                $"[{(device.IsEnabled ? "启用" : "禁用")}]");
            }
            
            await Task.Delay(2000);
            
            // 测试设备连接
            if (devices.Count > 0)
            {
                Console.WriteLine("\n2. 测试设备连接...");
                var testDevice = devices[0];
                
                Console.WriteLine($"   尝试连接到 {testDevice.Name}...");
                bool connected = await _manager.ConnectToDeviceAsync(testDevice);
                
                Console.WriteLine($"   连接结果: {(connected ? "成功" : "失败")}");
                Console.WriteLine($"   设备状态: {testDevice.Status}");
                Console.WriteLine($"   状态消息: {testDevice.StatusMessage}");
                
                if (connected)
                {
                    await Task.Delay(1000);
                    Console.WriteLine("   断开连接...");
                    _manager.DisconnectDevice(testDevice);
                }
            }
            
            await Task.Delay(2000);
        }

        /// <summary>
        /// 演示重连机制
        /// </summary>
        private async Task DemoReconnectMechanism()
        {
            Console.WriteLine("\n========== 重连机制演示 ==========");
            
            // 创建一个测试设备（使用无效IP模拟连接失败）
            var testDevice = new DeviceConnectionInfo
            {
                Id = 9999,
                Name = "重连测试设备",
                IpAddress = "192.168.999.999", // 无效IP
                Port = "8000",
                Username = "admin",
                Password = "12345",
                IsEnabled = true
            };
            
            Console.WriteLine("1. 尝试连接到无效设备以触发重连机制...");
            
            // 尝试连接多次以观察重连行为
            for (int i = 1; i <= 3; i++)
            {
                if (!_isRunning) break;
                
                Console.WriteLine($"\n   第 {i} 次连接尝试:");
                bool connected = await _manager.ConnectToDeviceAsync(testDevice);
                Console.WriteLine($"   结果: {(connected ? "成功" : "失败")}");
                Console.WriteLine($"   重连次数: {testDevice.ReconnectAttempts}");
                Console.WriteLine($"   连接成功率: {testDevice.ConnectionSuccessRate:F1}%");
                
                await Task.Delay(2000);
            }
        }

        /// <summary>
        /// 演示并发连接
        /// </summary>
        private async Task DemoConcurrentConnections()
        {
            Console.WriteLine("\n========== 并发连接演示 ==========");
            
            var devices = _manager.GetAllDevices();
            if (devices.Count == 0)
            {
                Console.WriteLine("没有可用设备进行并发连接测试");
                return;
            }
            
            var testDevice = devices[0];
            Console.WriteLine($"1. 对设备 {testDevice.Name} 进行并发连接测试...");
            
            var tasks = new Task[5];
            var startTime = DateTime.Now;
            
            for (int i = 0; i < 5; i++)
            {
                int taskId = i + 1;
                tasks[i] = Task.Run(async () =>
                {
                    Console.WriteLine($"   任务 {taskId}: 开始连接...");
                    var taskStartTime = DateTime.Now;
                    
                    bool result = await _manager.ConnectToDeviceAsync(testDevice);
                    var duration = DateTime.Now - taskStartTime;
                    
                    Console.WriteLine($"   任务 {taskId}: {(result ? "成功" : "失败")} " +
                                    $"(耗时: {duration.TotalMilliseconds:F0}ms)");
                    
                    if (result)
                    {
                        await Task.Delay(500); // 保持连接一段时间
                        _manager.DisconnectDevice(testDevice);
                    }
                });
            }
            
            await Task.WhenAll(tasks);
            var totalDuration = DateTime.Now - startTime;
            
            Console.WriteLine($"   并发测试完成，总耗时: {totalDuration.TotalMilliseconds:F0}ms");
        }

        /// <summary>
        /// 演示状态监控
        /// </summary>
        private async Task DemoStatusMonitoring()
        {
            Console.WriteLine("\n========== 状态监控演示 ==========");
            
            var devices = _manager.GetAllDevices();
            Console.WriteLine("1. 当前设备状态概览:");
            
            foreach (var device in devices)
            {
                Console.WriteLine($"   {device.Name}:");
                Console.WriteLine($"     - 状态: {device.Status}");
                Console.WriteLine($"     - 连接: {(device.IsConnected ? "已连接" : "未连接")}");
                Console.WriteLine($"     - 成功率: {device.ConnectionSuccessRate:F1}%");
                Console.WriteLine($"     - 最后检查: {device.LastChecked:HH:mm:ss}");
            }
            
            Console.WriteLine("\n2. 监控状态变化 (10秒)...");
            Console.WriteLine("   (定时器会自动检查设备状态)");
            
            await Task.Delay(10000);
            
            Console.WriteLine("\n3. 更新后的设备状态:");
            foreach (var device in devices)
            {
                Console.WriteLine($"   {device.Name}: {device.Status} " +
                                $"(最后检查: {device.LastChecked:HH:mm:ss})");
            }
        }

        #region 事件处理方法

        private void OnDeviceStatusChanged(object sender, DeviceStatusChangedEventArgs e)
        {
            Console.WriteLine($"[事件] 设备状态变更: {e.Device.Name} " +
                            $"{e.PreviousStatus} -> {e.CurrentStatus} ({e.ChangeReason})");
        }

        private void OnDeviceConnectionStateChanged(object sender, DeviceConnectionEventArgs e)
        {
            Console.WriteLine($"[事件] 连接状态变更: {e.Device.Name} " +
                            $"{(e.Success ? "连接成功" : "连接失败")} - {e.Message}");
        }

        private void OnDeviceReconnectAttempt(object sender, DeviceReconnectEventArgs e)
        {
            Console.WriteLine($"[事件] 重连尝试: 设备{e.DeviceId} " +
                            $"第{e.Attempts}次 (下次延迟: {e.NextDelay.TotalSeconds:F1}秒)");
        }

        private void OnDeviceError(object sender, DeviceErrorEventArgs e)
        {
            Console.WriteLine($"[事件] 设备错误: {e.Device?.Name ?? "Unknown"} " +
                            $"- {e.ErrorMessage} (类型: {e.ErrorType})");
        }

        #endregion

        /// <summary>
        /// 清理资源
        /// </summary>
        public void Cleanup()
        {
            _isRunning = false;
            
            // 取消事件订阅
            _manager.DeviceStatusChanged -= OnDeviceStatusChanged;
            _manager.DeviceConnectionStateChanged -= OnDeviceConnectionStateChanged;
            _manager.DeviceReconnectAttempt -= OnDeviceReconnectAttempt;
            _manager.DeviceError -= OnDeviceError;
        }
    }

    /// <summary>
    /// 演示程序入口
    /// </summary>
    public class DemoProgram
    {
        public static async Task Main(string[] args)
        {
            var demo = new DeviceConnectionDemo();
            
            try
            {
                await demo.RunDemo();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"演示程序异常: {ex.Message}");
            }
            finally
            {
                demo.Cleanup();
                DeviceConnectionManager.Instance.Dispose();
            }
        }
    }
}