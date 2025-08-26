using System;
using System.Runtime.InteropServices;

namespace ControlEntradaSalida
{
    /// <summary>
    /// 设备状态管理器
    /// 封装海康威视SDK的设备状态获取功能
    /// 基于设备网络SDK编程指南（明眸-以人为中心）
    /// </summary>
    public class DeviceStatusManager
    {
        private static readonly Lazy<DeviceStatusManager> _instance = 
            new Lazy<DeviceStatusManager>(() => new DeviceStatusManager());

        /// <summary>
        /// 单例实例
        /// </summary>
        public static DeviceStatusManager Instance => _instance.Value;

        /// <summary>
        /// 私有构造函数
        /// </summary>
        private DeviceStatusManager()
        {
        }

        /// <summary>
        /// 获取设备真实状态
        /// 基于海康威视SDK NET_DVR_GET_ACS_WORK_STATUS_V50 API
        /// </summary>
        /// <param name="device">设备连接信息</param>
        /// <returns>设备状态信息</returns>
        public DeviceStatusInfo GetRealDeviceStatus(DeviceConnectionInfo device)
        {
            if (device == null)
            {
                return CreateOfflineStatus(0, "设备信息为空");
            }

            if (device.UserID < 0 || !device.IsConnected)
            {
                return CreateOfflineStatus(device.Id, "设备未连接");
            }

            try
            {
                // 调用海康威视SDK获取设备工作状态
                var workStatus = GetDeviceWorkStatus(device.UserID);
                return ParseWorkStatus(workStatus, device);
            }
            catch (Exception ex)
            {
                LogError($"获取设备状态失败 - 设备ID: {device.Id}, 错误: {ex.Message}");
                return CreateOfflineStatus(device.Id, $"状态获取失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 使用海康威视SDK获取设备工作状态
        /// 基于NET_DVR_GET_ACS_WORK_STATUS_V50命令
        /// </summary>
        /// <param name="userId">NET_DVR_Login_V40返回的用户ID</param>
        /// <returns>设备工作状态结构体</returns>
        private HCNetSDK.NET_DVR_ACS_WORK_STATUS_V50 GetDeviceWorkStatus(int userId)
        {
            var statusInfo = new HCNetSDK.NET_DVR_ACS_WORK_STATUS_V50();
            statusInfo.Init();
            statusInfo.dwSize = (uint)Marshal.SizeOf(statusInfo);

            uint dwReturned = 0;
            // 使用SDK手册中定义的命令码 
            uint dwCommand = (uint)HCNetSDK.NET_DVR_GET_ACS_WORK_STATUS_V50;
            int nSize = Marshal.SizeOf(statusInfo);
            IntPtr ptrStatusInfo = Marshal.AllocHGlobal(nSize);

            try
            {
                Marshal.StructureToPtr(statusInfo, ptrStatusInfo, false);

                // 调用海康威视SDK API
                bool bRet = HCNetSDK.NET_DVR_GetDVRConfig(
                    userId,           // 登录句柄
                    dwCommand,        // 命令码
                    -1,              // 通道号，-1表示无效
                    ptrStatusInfo,    // 输出缓冲区
                    (uint)nSize,     // 缓冲区大小
                    ref dwReturned   // 实际返回数据长度
                );

                if (bRet && dwReturned > 0)
                {
                    // 成功获取状态信息
                    statusInfo = (HCNetSDK.NET_DVR_ACS_WORK_STATUS_V50)
                        Marshal.PtrToStructure(ptrStatusInfo, 
                        typeof(HCNetSDK.NET_DVR_ACS_WORK_STATUS_V50));

                    LogInfo($"成功获取设备状态 - UserID: {userId}, 返回数据长度: {dwReturned}");
                    return statusInfo;
                }
                else
                {
                    uint errorCode = HCNetSDK.NET_DVR_GetLastError();
                    string errorMessage = GetSDKErrorMessage(errorCode);
                    throw new Exception($"SDK调用失败，错误代码: {errorCode}, 错误信息: {errorMessage}");
                }
            }
            finally
            {
                Marshal.FreeHGlobal(ptrStatusInfo);
            }
        }

        /// <summary>
        /// 解析海康威视SDK返回的设备工作状态数据
        /// </summary>
        /// <param name="workStatus">SDK返回的工作状态结构体</param>
        /// <param name="device">设备连接信息</param>
        /// <returns>解析后的设备状态信息</returns>
        private DeviceStatusInfo ParseWorkStatus(
            HCNetSDK.NET_DVR_ACS_WORK_STATUS_V50 workStatus, 
            DeviceConnectionInfo device)
        {
            var statusInfo = new DeviceStatusInfo
            {
                IsOnline = true,
                DeviceId = device.Id,
                LastUpdated = DateTime.Now
            };

            // 解析门状态 - 基于SDK手册定义
            if (workStatus.byDoorStatus != null && workStatus.byDoorStatus.Length > 0)
            {
                byte doorStatus = workStatus.byDoorStatus[0];
                statusInfo.DoorStatus = (DoorStatus)doorStatus;
                statusInfo.RawDoorStatus = doorStatus;

                LogDebug($"门状态解析 - 原始值: {doorStatus}, 解析结果: {statusInfo.DoorStatus}");
            }

            // 解析门锁状态（继电器状态）- 基于SDK手册定义
            if (workStatus.byDoorLockStatus != null && workStatus.byDoorLockStatus.Length > 0)
            {
                byte lockStatus = workStatus.byDoorLockStatus[0];
                statusInfo.DoorLockStatus = (DoorLockStatus)lockStatus;
                statusInfo.RawLockStatus = lockStatus;

                LogDebug($"门锁状态解析 - 原始值: {lockStatus}, 解析结果: {statusInfo.DoorLockStatus}");
            }

            // 解析磁力锁状态 - 基于SDK手册定义
            if (workStatus.byMagneticStatus != null && workStatus.byMagneticStatus.Length > 0)
            {
                byte magneticStatus = workStatus.byMagneticStatus[0];
                statusInfo.MagneticLockStatus = (MagneticLockStatus)magneticStatus;
                statusInfo.RawMagneticStatus = magneticStatus;

                LogDebug($"磁力锁状态解析 - 原始值: {magneticStatus}, 解析结果: {statusInfo.MagneticLockStatus}");
            }

            // 解析读卡器状态
            ParseCardReaderStatus(workStatus, statusInfo);

            // 解析电源和电池状态
            ParsePowerStatus(workStatus, statusInfo);

            // 解析其他状态信息
            ParseOtherStatus(workStatus, statusInfo);

            // 设置综合状态
            statusInfo.OverallStatus = DetermineOverallStatus(statusInfo);
            statusInfo.StatusMessage = GenerateStatusMessage(statusInfo);

            // 验证状态数据的有效性
            var validation = DeviceStatusHelper.ValidateStatusData(statusInfo);
            if (!validation.IsValid)
            {
                LogError($"设备状态数据验证失败 - 设备ID: {device.Id}, 错误: {validation.ErrorMessage}");
                statusInfo.StatusMessage += $" [数据异常: {validation.ErrorMessage}]";
            }

            return statusInfo;
        }

        /// <summary>
        /// 解析读卡器相关状态
        /// </summary>
        private void ParseCardReaderStatus(HCNetSDK.NET_DVR_ACS_WORK_STATUS_V50 workStatus, DeviceStatusInfo statusInfo)
        {
            // 读卡器在线状态
            if (workStatus.byCardReaderOnlineStatus != null)
            {
                for (int i = 0; i < Math.Min(workStatus.byCardReaderOnlineStatus.Length, statusInfo.CardReaderOnlineStatus.Length); i++)
                {
                    statusInfo.CardReaderOnlineStatus[i] = workStatus.byCardReaderOnlineStatus[i] == 1;
                }
            }

            // 读卡器验证模式
            if (workStatus.byCardReaderVerifyMode != null)
            {
                for (int i = 0; i < Math.Min(workStatus.byCardReaderVerifyMode.Length, statusInfo.CardReaderVerifyModes.Length); i++)
                {
                    statusInfo.CardReaderVerifyModes[i] = (CardReaderVerifyMode)workStatus.byCardReaderVerifyMode[i];
                }
            }

            // 读卡器防拆状态
            if (workStatus.byCardReaderAntiDismantleStatus != null)
            {
                for (int i = 0; i < Math.Min(workStatus.byCardReaderAntiDismantleStatus.Length, statusInfo.CardReaderAntiDismantleStatus.Length); i++)
                {
                    statusInfo.CardReaderAntiDismantleStatus[i] = workStatus.byCardReaderAntiDismantleStatus[i] == 1;
                }
            }
        }

        /// <summary>
        /// 解析电源相关状态
        /// </summary>
        private void ParsePowerStatus(HCNetSDK.NET_DVR_ACS_WORK_STATUS_V50 workStatus, DeviceStatusInfo statusInfo)
        {
            // 电池电压（需要除以10得到实际电压值）
            statusInfo.BatteryVoltage = workStatus.wBatteryVoltage / 10.0f;

            // 低电压状态
            statusInfo.IsLowVoltage = workStatus.byBatteryLowVoltage == 1;

            // 电源供应状态
            statusInfo.PowerSupplyStatus = workStatus.byPowerSupplyStatus;
        }

        /// <summary>
        /// 解析其他状态信息
        /// </summary>
        private void ParseOtherStatus(HCNetSDK.NET_DVR_ACS_WORK_STATUS_V50 workStatus, DeviceStatusInfo statusInfo)
        {
            // 多门互锁状态
            statusInfo.MultiDoorInterlockStatus = workStatus.byMultiDoorInterlockStatus == 1;

            // 反潜回状态
            statusInfo.AntiSneakStatus = workStatus.byAntiSneakStatus == 1;

            // 主机防拆状态
            statusInfo.HostAntiDismantleStatus = workStatus.byHostAntiDismantleStatus == 1;

            // 指示灯状态
            statusInfo.IndicatorLightStatus = workStatus.byIndicatorLightStatus == 1;

            // 添加卡数量
            statusInfo.CardNum = workStatus.dwCardNum;

            // 火警报警状态
            statusInfo.FireAlarmStatus = workStatus.byFireAlarmStatus;

            // 机箱传感器状态
            if (workStatus.byCaseStatus != null)
            {
                for (int i = 0; i < Math.Min(workStatus.byCaseStatus.Length, statusInfo.CaseStatus.Length); i++)
                {
                    statusInfo.CaseStatus[i] = workStatus.byCaseStatus[i] == 1;
                }
            }

            // 报警相关状态
            ParseAlarmStatus(workStatus, statusInfo);
        }

        /// <summary>
        /// 解析报警相关状态
        /// </summary>
        private void ParseAlarmStatus(HCNetSDK.NET_DVR_ACS_WORK_STATUS_V50 workStatus, DeviceStatusInfo statusInfo)
        {
            // 报警输入设防状态
            if (workStatus.bySetupAlarmStatus != null)
            {
                for (int i = 0; i < Math.Min(workStatus.bySetupAlarmStatus.Length, statusInfo.SetupAlarmStatus.Length); i++)
                {
                    statusInfo.SetupAlarmStatus[i] = workStatus.bySetupAlarmStatus[i] == 1;
                }
            }

            // 报警输入状态
            if (workStatus.byAlarmInStatus != null)
            {
                for (int i = 0; i < Math.Min(workStatus.byAlarmInStatus.Length, statusInfo.AlarmInStatus.Length); i++)
                {
                    statusInfo.AlarmInStatus[i] = workStatus.byAlarmInStatus[i] == 1;
                }
            }

            // 报警输出状态
            if (workStatus.byAlarmOutStatus != null)
            {
                for (int i = 0; i < Math.Min(workStatus.byAlarmOutStatus.Length, statusInfo.AlarmOutStatus.Length); i++)
                {
                    statusInfo.AlarmOutStatus[i] = workStatus.byAlarmOutStatus[i] == 1;
                }
            }
        }

        /// <summary>
        /// 判断设备综合状态
        /// </summary>
        private DeviceOverallStatus DetermineOverallStatus(DeviceStatusInfo statusInfo)
        {
            return DeviceStatusHelper.DetermineOverallStatus(statusInfo);
        }

        /// <summary>
        /// 生成状态描述信息
        /// </summary>
        private string GenerateStatusMessage(DeviceStatusInfo statusInfo)
        {
            return DeviceStatusHelper.GenerateDetailedStatusMessage(statusInfo);
        }

        /// <summary>
        /// 创建离线状态信息
        /// </summary>
        private DeviceStatusInfo CreateOfflineStatus(int deviceId, string message)
        {
            return new DeviceStatusInfo
            {
                DeviceId = deviceId,
                IsOnline = false,
                OverallStatus = DeviceOverallStatus.Offline,
                StatusMessage = message,
                LastUpdated = DateTime.Now
            };
        }

        /// <summary>
        /// 根据SDK手册将错误代码转换为可读信息
        /// </summary>
        private string GetSDKErrorMessage(uint errorCode)
        {
            switch (errorCode)
            {
                case 1:
                    return "用户未登录";
                case 2:
                    return "网络连接超时";
                case 3:
                    return "版本不匹配";
                case 7:
                    return "命令不支持";
                case 17:
                    return "资源不足";
                case 23:
                    return "参数错误";
                case 24:
                    return "设备无响应";
                case 25:
                    return "设备用户名或密码错误";
                case 26:
                    return "设备用户权限不足";
                case 29:
                    return "多媒体字节数据错误";
                case 33:
                    return "设备端用户数超过最大值";
                case 34:
                    return "客户端用户数超过最大值";
                case 35:
                    return "数据保护密码错误";
                case 36:
                    return "文件不存在";
                case 37:
                case 38:
                    return "列表查询错误";
                case 39:
                    return "错误的文件名";
                case 40:
                    return "文件写入错误";
                case 41:
                    return "文件读取错误";
                case 42:
                    return "硬盘满";
                case 43:
                    return "设备没有格式化";
                case 44:
                    return "设备在格式化";
                case 45:
                    return "设备密码错误";
                case 46:
                    return "设备初始化错误";
                case 47:
                    return "设备语音播放中";
                default:
                    return $"未知错误 (ErrorCode: {errorCode})";
            }
        }

        #region 日志记录方法

        /// <summary>
        /// 记录信息日志
        /// </summary>
        private void LogInfo(string message)
        {
            // 可以根据项目需要实现具体的日志记录
            Console.WriteLine($"[INFO] {DateTime.Now:yyyy-MM-dd HH:mm:ss} - {message}");
        }

        /// <summary>
        /// 记录调试日志
        /// </summary>
        private void LogDebug(string message)
        {
            // 在调试模式下记录详细信息
            #if DEBUG
            Console.WriteLine($"[DEBUG] {DateTime.Now:yyyy-MM-dd HH:mm:ss} - {message}");
            #endif
        }

        /// <summary>
        /// 记录错误日志
        /// </summary>
        private void LogError(string message)
        {
            // 可以根据项目需要实现具体的错误日志记录
            Console.WriteLine($"[ERROR] {DateTime.Now:yyyy-MM-dd HH:mm:ss} - {message}");
        }

        #endregion
    }
}