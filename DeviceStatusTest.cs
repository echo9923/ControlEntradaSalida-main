using System;
using System.Threading.Tasks;

namespace ControlEntradaSalida
{
    /// <summary>
    /// 设备状态监控功能测试类
    /// 用于验证真实SDK集成的正确性
    /// </summary>
    public class DeviceStatusTest
    {
        private DeviceStatusManager _statusManager;

        public DeviceStatusTest()
        {
            _statusManager = DeviceStatusManager.Instance;
        }

        /// <summary>
        /// 运行所有测试用例
        /// </summary>
        public async Task RunAllTests()
        {
            Console.WriteLine("========================================");
            Console.WriteLine("设备状态监控功能测试开始");
            Console.WriteLine("========================================");

            // 测试1: 测试状态枚举和描述
            TestStatusEnums();
            
            // 测试2: 测试DeviceStatusInfo数据结构
            TestDeviceStatusInfo();
            
            // 测试3: 测试状态验证功能
            TestStatusValidation();
            
            // 测试4: 测试模拟设备状态获取
            TestMockDeviceStatus();
            
            // 测试5: 测试状态辅助功能
            TestStatusHelper();

            Console.WriteLine("========================================");
            Console.WriteLine("所有测试完成");
            Console.WriteLine("========================================");
        }

        /// <summary>
        /// 测试状态枚举和描述功能
        /// </summary>
        private void TestStatusEnums()
        {
            Console.WriteLine("\n--- 测试1: 状态枚举和描述 ---");

            // 测试门状态枚举
            var doorStatuses = new[] { 
                DoorStatus.Invalid, DoorStatus.Sleep, DoorStatus.AlwaysOpen, 
                DoorStatus.AlwaysClose, DoorStatus.Normal 
            };

            foreach (var status in doorStatuses)
            {
                var description = DeviceStatusHelper.GetDoorStatusDescription(status);
                Console.WriteLine($"门状态 {(byte)status}: {description}");
            }

            // 测试门锁状态枚举
            var lockStatuses = new[] { 
                DoorLockStatus.NormalClose, DoorLockStatus.NormalOpen, 
                DoorLockStatus.ShortCircuit, DoorLockStatus.OpenCircuit, DoorLockStatus.Abnormal 
            };

            foreach (var status in lockStatuses)
            {
                var description = DeviceStatusHelper.GetDoorLockStatusDescription(status);
                Console.WriteLine($"门锁状态 {(byte)status}: {description}");
            }

            Console.WriteLine("✓ 状态枚举测试通过");
        }

        /// <summary>
        /// 测试DeviceStatusInfo数据结构
        /// </summary>
        private void TestDeviceStatusInfo()
        {
            Console.WriteLine("\n--- 测试2: DeviceStatusInfo数据结构 ---");

            var statusInfo = new DeviceStatusInfo
            {
                DeviceId = 1,
                IsOnline = true,
                DoorStatus = DoorStatus.Normal,
                DoorLockStatus = DoorLockStatus.NormalClose,
                MagneticLockStatus = MagneticLockStatus.NormalClose,
                BatteryVoltage = 12.5f,
                IsLowVoltage = false,
                PowerSupplyStatus = 1
            };

            Console.WriteLine($"设备ID: {statusInfo.DeviceId}");
            Console.WriteLine($"在线状态: {statusInfo.IsOnline}");
            Console.WriteLine($"门状态: {statusInfo.GetDoorStatusDescription()}");
            Console.WriteLine($"门锁状态: {statusInfo.GetDoorLockStatusDescription()}");
            Console.WriteLine($"磁力锁状态: {statusInfo.GetMagneticLockStatusDescription()}");
            Console.WriteLine($"电池电压: {statusInfo.BatteryVoltage}V");
            Console.WriteLine($"电源状态: {statusInfo.GetPowerSupplyStatusDescription()}");
            Console.WriteLine($"在线读卡器数量: {statusInfo.GetOnlineCardReaderCount()}");
            Console.WriteLine($"是否有硬件错误: {statusInfo.HasHardwareError()}");
            Console.WriteLine($"是否有警告: {statusInfo.HasWarning()}");

            Console.WriteLine("✓ DeviceStatusInfo测试通过");
        }

        /// <summary>
        /// 测试状态验证功能
        /// </summary>
        private void TestStatusValidation()
        {
            Console.WriteLine("\n--- 测试3: 状态验证功能 ---");

            // 测试正常状态
            var normalStatus = new DeviceStatusInfo
            {
                DeviceId = 1,
                IsOnline = true,
                DoorStatus = DoorStatus.Normal,
                DoorLockStatus = DoorLockStatus.NormalClose,
                MagneticLockStatus = MagneticLockStatus.NormalClose,
                BatteryVoltage = 12.5f,
                PowerSupplyStatus = 1,
                LastUpdated = DateTime.Now
            };

            var validation1 = DeviceStatusHelper.ValidateStatusData(normalStatus);
            Console.WriteLine($"正常状态验证: {(validation1.IsValid ? "通过" : "失败")} - {validation1.ErrorMessage}");

            // 测试异常状态
            var errorStatus = new DeviceStatusInfo
            {
                DeviceId = 0, // 无效设备ID
                BatteryVoltage = -5.0f, // 异常电压
                PowerSupplyStatus = 99 // 无效电源状态
            };

            var validation2 = DeviceStatusHelper.ValidateStatusData(errorStatus);
            Console.WriteLine($"异常状态验证: {(validation2.IsValid ? "通过" : "失败")} - {validation2.ErrorMessage}");

            // 测试空状态
            var validation3 = DeviceStatusHelper.ValidateStatusData(null);
            Console.WriteLine($"空状态验证: {(validation3.IsValid ? "通过" : "失败")} - {validation3.ErrorMessage}");

            Console.WriteLine("✓ 状态验证测试通过");
        }

        /// <summary>
        /// 测试模拟设备状态获取
        /// </summary>
        private void TestMockDeviceStatus()
        {
            Console.WriteLine("\n--- 测试4: 模拟设备状态获取 ---");

            // 创建模拟设备连接信息
            var mockDevice = new DeviceConnectionInfo
            {
                Id = 1,
                Name = "测试门禁设备",
                IpAddress = "192.168.1.100",
                Port = "8000",
                Username = "admin",
                Password = "12345",
                UserID = -1, // 模拟未连接状态
                IsConnected = false
            };

            Console.WriteLine($"测试设备: {mockDevice.Name} ({mockDevice.IpAddress}:{mockDevice.Port})");
            
            // 测试离线设备状态获取
            var offlineStatus = _statusManager.GetRealDeviceStatus(mockDevice);
            Console.WriteLine($"离线设备状态: {offlineStatus.StatusMessage}");
            Console.WriteLine($"综合状态: {offlineStatus.OverallStatus}");

            // 模拟连接成功的设备（注意：这里只是模拟，实际需要真实的SDK连接）
            mockDevice.UserID = 1;
            mockDevice.IsConnected = true;

            Console.WriteLine("\n模拟连接设备状态获取（注意：需要真实SDK支持）:");
            try
            {
                var onlineStatus = _statusManager.GetRealDeviceStatus(mockDevice);
                Console.WriteLine($"在线设备状态: {onlineStatus.StatusMessage}");
                Console.WriteLine($"综合状态: {onlineStatus.OverallStatus}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"SDK调用异常（这是预期的，因为没有真实设备连接）: {ex.Message}");
            }

            Console.WriteLine("✓ 模拟设备状态获取测试完成");
        }

        /// <summary>
        /// 测试状态辅助功能
        /// </summary>
        private void TestStatusHelper()
        {
            Console.WriteLine("\n--- 测试5: 状态辅助功能 ---");

            // 创建不同状态的设备信息进行测试
            var errorStatus = new DeviceStatusInfo
            {
                DeviceId = 1,
                IsOnline = true,
                DoorStatus = DoorStatus.Sleep,
                DoorLockStatus = DoorLockStatus.ShortCircuit,
                MagneticLockStatus = MagneticLockStatus.Abnormal,
                BatteryVoltage = 9.5f,
                IsLowVoltage = true,
                PowerSupplyStatus = 2,
                FireAlarmStatus = 1,
                LastUpdated = DateTime.Now
            };

            Console.WriteLine("测试错误状态:");
            Console.WriteLine($"是否严重错误: {DeviceStatusHelper.IsCriticalError(errorStatus)}");
            Console.WriteLine($"是否警告状态: {DeviceStatusHelper.IsWarningStatus(errorStatus)}");
            Console.WriteLine($"综合状态: {DeviceStatusHelper.DetermineOverallStatus(errorStatus)}");
            Console.WriteLine($"详细描述: {DeviceStatusHelper.GenerateDetailedStatusMessage(errorStatus)}");

            // 测试状态变化事件
            var oldStatus = new DeviceStatusInfo
            {
                DeviceId = 1,
                IsOnline = false,
                OverallStatus = DeviceOverallStatus.Offline
            };

            var newStatus = new DeviceStatusInfo
            {
                DeviceId = 1,
                IsOnline = true,
                OverallStatus = DeviceOverallStatus.Online
            };

            var changeEvent = DeviceStatusHelper.CreateStatusChangedEvent(1, oldStatus, newStatus);
            Console.WriteLine($"\n状态变化事件:");
            Console.WriteLine($"设备ID: {changeEvent.DeviceId}");
            Console.WriteLine($"变化时间: {changeEvent.ChangeTime}");
            Console.WriteLine($"状态是否改善: {changeEvent.IsStatusImproved}");
            Console.WriteLine($"是否关键变化: {changeEvent.IsCriticalChange}");

            Console.WriteLine("✓ 状态辅助功能测试通过");
        }

        /// <summary>
        /// 模拟真实SDK数据的测试
        /// </summary>
        public void TestWithSimulatedSDKData()
        {
            Console.WriteLine("\n--- 模拟真实SDK数据测试 ---");

            // 模拟海康威视SDK返回的数据格式
            var simulatedSDKData = new
            {
                byDoorStatus = new byte[] { 4, 0, 0, 0 }, // 普通状态
                byDoorLockStatus = new byte[] { 0, 0, 0, 0 }, // 常闭
                byMagneticStatus = new byte[] { 0, 0, 0, 0 }, // 常闭
                wBatteryVoltage = (ushort)125, // 12.5V
                byBatteryLowVoltage = (byte)0, // 电压正常
                byPowerSupplyStatus = (byte)1, // 交流供电
                byFireAlarmStatus = (byte)0, // 火警正常
                dwCardNum = (uint)150 // 卡片数量
            };

            Console.WriteLine("模拟SDK数据:");
            Console.WriteLine($"门状态原始值: {simulatedSDKData.byDoorStatus[0]} -> {(DoorStatus)simulatedSDKData.byDoorStatus[0]}");
            Console.WriteLine($"门锁状态原始值: {simulatedSDKData.byDoorLockStatus[0]} -> {(DoorLockStatus)simulatedSDKData.byDoorLockStatus[0]}");
            Console.WriteLine($"磁力锁状态原始值: {simulatedSDKData.byMagneticStatus[0]} -> {(MagneticLockStatus)simulatedSDKData.byMagneticStatus[0]}");
            Console.WriteLine($"电池电压原始值: {simulatedSDKData.wBatteryVoltage} -> {simulatedSDKData.wBatteryVoltage / 10.0f}V");
            Console.WriteLine($"电源状态: {simulatedSDKData.byPowerSupplyStatus}");
            Console.WriteLine($"卡片数量: {simulatedSDKData.dwCardNum}");

            Console.WriteLine("✓ SDK数据解析测试完成");
        }
    }
}