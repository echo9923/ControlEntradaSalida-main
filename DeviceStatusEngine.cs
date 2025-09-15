using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace ControlEntradaSalida
{
    /// <summary>
    /// 设备能力信息
    /// </summary>
    public class DeviceCapabilities
    {
        public bool SupportsFaceRecognition { get; set; }
        public bool SupportsCardAccess { get; set; }
        public bool SupportsFingerprint { get; set; }
        public bool SupportsRemoteControl { get; set; }
        public int MaxDoorCount { get; set; }
        public string DeviceModel { get; set; }
        public string FirmwareVersion { get; set; }
    }

    /// <summary>
    /// 设备工作状态详情
    /// </summary>
    public class DeviceWorkStatus
    {
        public DeviceStatus Status { get; set; }
        public DateTime CheckTime { get; set; }
        public string StatusMessage { get; set; }
        public byte[] DoorStatuses { get; set; }
        public bool IsOnline { get; set; }
        public uint LastErrorCode { get; set; }
        public string ErrorMessage { get; set; }
    }

    /// <summary>
    /// 设备状态引擎 - 基于海康威视SDK技术规范实现真实设备状态检测
    /// </summary>
    public class DeviceStatusEngine
    {
        #region 私有成员
        
        private readonly object _lockObject = new object();
        
        #endregion

        #region 公共方法
        
        /// <summary>
        /// 验证设备连接有效性
        /// </summary>
        /// <param name="userID">用户登录ID</param>
        /// <returns>连接是否有效</returns>
        public bool ValidateConnection(int userID)
        {
            if (userID < 0) return false;

            try
            {
                // 使用设备能力查询来验证连接有效性
                // 根据海康威视SDK编程指南，NET_DVR_GetDeviceAbility是检测连接状态的标准方法
                uint dwSize = 1024 * 10;
                IntPtr ptrOutBuf = IntPtr.Zero;
                
                try
                {
                    ptrOutBuf = Marshal.AllocHGlobal((int)dwSize);
                    bool result = HCNetSDK.NET_DVR_GetDeviceAbility(
                        userID, 
                        HCNetSDK.ACS_ABILITY, 
                        IntPtr.Zero, 
                        0, 
                        ptrOutBuf, 
                        dwSize);
                    
                    return result;
                }
                finally
                {
                    if (ptrOutBuf != IntPtr.Zero)
                    {
                        Marshal.FreeHGlobal(ptrOutBuf);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"验证连接时发生异常: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 获取设备工作状态
        /// </summary>
        /// <param name="userID">用户登录ID</param>
        /// <param name="channelNo">通道号，默认为-1（所有通道）</param>
        /// <returns>设备工作状态</returns>
        public DeviceWorkStatus GetDeviceWorkStatus(int userID, int channelNo = -1)
        {
            var workStatus = new DeviceWorkStatus
            {
                CheckTime = DateTime.Now,
                IsOnline = false,
                Status = DeviceStatus.Unknown
            };

            if (userID < 0)
            {
                workStatus.StatusMessage = "无效的用户ID";
                workStatus.Status = DeviceStatus.Offline;
                return workStatus;
            }

            try
            {
                lock (_lockObject)
                {
                    // 首先验证连接有效性
                    if (!TestConnectivity(userID))
                    {
                        uint errorCode = HCNetSDK.NET_DVR_GetLastError();
                        workStatus.StatusMessage = $"连接验证失败，错误码: {errorCode}";
                        workStatus.Status = DeviceStatus.Offline;
                        workStatus.LastErrorCode = errorCode;
                        workStatus.ErrorMessage = GetErrorMessage(errorCode);
                        return workStatus;
                    }

                    // 获取门禁设备工作状态
                    // 根据SDK编程指南，使用NET_DVR_GET_ACS_WORK_STATUS_V50命令获取设备状态
                    HCNetSDK.NET_DVR_ACS_WORK_STATUS_V50 statusInfo = new HCNetSDK.NET_DVR_ACS_WORK_STATUS_V50();
                    statusInfo.Init();
                    
                    uint dwReturned = 0;
                    uint dwCommand = HCNetSDK.NET_DVR_GET_ACS_WORK_STATUS_V50;
                    int nSize = Marshal.SizeOf(statusInfo);
                    IntPtr ptrStatusInfo = IntPtr.Zero;

                    try
                    {
                        ptrStatusInfo = Marshal.AllocHGlobal(nSize);
                        Marshal.StructureToPtr(statusInfo, ptrStatusInfo, false);
                        
                        bool result = HCNetSDK.NET_DVR_GetDVRConfig(
                            userID, 
                            dwCommand, 
                            channelNo, 
                            ptrStatusInfo, 
                            (uint)nSize, 
                            ref dwReturned);

                        if (result)
                        {
                            // 成功获取状态信息
                            statusInfo = (HCNetSDK.NET_DVR_ACS_WORK_STATUS_V50)Marshal.PtrToStructure(
                                ptrStatusInfo, typeof(HCNetSDK.NET_DVR_ACS_WORK_STATUS_V50));
                            
                            workStatus.IsOnline = true;
                            workStatus.DoorStatuses = statusInfo.byDoorStatus;
                            
                            // 解析设备状态
                            var deviceStatus = ParseDeviceStatus(statusInfo);
                            workStatus.Status = deviceStatus.Status;
                            workStatus.StatusMessage = deviceStatus.Message;
                        }
                        else
                        {
                            // 获取状态失败
                            uint errorCode = HCNetSDK.NET_DVR_GetLastError();
                            workStatus.IsOnline = true;
                            workStatus.StatusMessage = $"在线，但读取状态失败(错误码: {errorCode})";
                            workStatus.Status = DeviceStatus.Online;
                            workStatus.LastErrorCode = errorCode;
                            workStatus.ErrorMessage = GetErrorMessage(errorCode);
                        }
                    }
                    finally
                    {
                        if (ptrStatusInfo != IntPtr.Zero)
                        {
                            Marshal.FreeHGlobal(ptrStatusInfo);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                workStatus.StatusMessage = $"获取设备状态时发生异常: {ex.Message}";
                workStatus.Status = DeviceStatus.Unknown;
                Console.WriteLine($"获取设备工作状态时发生异常: {ex.Message}");
            }

            return workStatus;
        }

        /// <summary>
        /// 异步获取设备工作状态
        /// </summary>
        /// <param name="userID">用户登录ID</param>
        /// <param name="channelNo">通道号</param>
        /// <returns>设备工作状态</returns>
        public async Task<DeviceWorkStatus> GetDeviceWorkStatusAsync(int userID, int channelNo = -1)
        {
            return await Task.Run(() => GetDeviceWorkStatus(userID, channelNo));
        }

        /// <summary>
        /// 获取设备能力信息
        /// </summary>
        /// <param name="userID">用户登录ID</param>
        /// <returns>设备能力信息</returns>
        public DeviceCapabilities GetDeviceCapabilities(int userID)
        {
            var capabilities = new DeviceCapabilities();

            if (userID < 0) return capabilities;

            try
            {
                // 获取设备能力
                uint dwSize = 1024 * 10;
                IntPtr ptrOutBuf = IntPtr.Zero;
                
                try
                {
                    ptrOutBuf = Marshal.AllocHGlobal((int)dwSize);
                    bool result = HCNetSDK.NET_DVR_GetDeviceAbility(
                        userID, 
                        HCNetSDK.ACS_ABILITY, 
                        IntPtr.Zero, 
                        0, 
                        ptrOutBuf, 
                        dwSize);
                    
                    if (result)
                    {
                        // 解析设备能力信息
                        // 注意：这里需要根据实际的设备能力结构体来解析
                        // 当前使用基本的能力检测
                        capabilities.SupportsRemoteControl = true;
                        capabilities.SupportsFaceRecognition = true;
                        capabilities.SupportsCardAccess = true;
                        capabilities.MaxDoorCount = 1; // 默认1个门
                        
                        // 可以进一步解析ptrOutBuf中的数据来获取详细能力
                    }
                }
                finally
                {
                    if (ptrOutBuf != IntPtr.Zero)
                    {
                        Marshal.FreeHGlobal(ptrOutBuf);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"获取设备能力时发生异常: {ex.Message}");
            }

            return capabilities;
        }

        /// <summary>
        /// 测试设备连通性
        /// </summary>
        /// <param name="userID">用户登录ID</param>
        /// <returns>连通性测试结果</returns>
        public bool TestConnectivity(int userID)
        {
            if (userID < 0) return false;

            try
            {
                // 简单的连通性测试：尝试获取设备时间
                HCNetSDK.NET_DVR_TIME timeInfo = new HCNetSDK.NET_DVR_TIME();
                uint dwReturned = 0;
                
                IntPtr ptrTimeInfo = Marshal.AllocHGlobal(Marshal.SizeOf(timeInfo));
                try
                {
                    Marshal.StructureToPtr(timeInfo, ptrTimeInfo, false);
                    bool result = HCNetSDK.NET_DVR_GetDVRConfig(
                        userID,
                        HCNetSDK.NET_DVR_GET_TIMECFG,
                        -1,
                        ptrTimeInfo,
                        (uint)Marshal.SizeOf(timeInfo),
                        ref dwReturned);
                    
                    if (result)
                    {
                        timeInfo = (HCNetSDK.NET_DVR_TIME)Marshal.PtrToStructure(ptrTimeInfo, typeof(HCNetSDK.NET_DVR_TIME));
                    }
                    
                    return result;
                 }
                 finally
                 {
                     Marshal.FreeHGlobal(ptrTimeInfo);
                 }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"测试设备连通性时发生异常: {ex.Message}");
                return false;
            }
        }

        #endregion

        #region 私有方法
        
        /// <summary>
        /// 解析设备状态
        /// </summary>
        /// <param name="statusInfo">设备状态信息</param>
        /// <returns>解析后的状态</returns>
        private (DeviceStatus Status, string Message) ParseDeviceStatus(HCNetSDK.NET_DVR_ACS_WORK_STATUS_V50 statusInfo)
        {
            try
            {
                // 检查门状态
                if (statusInfo.byDoorStatus != null && statusInfo.byDoorStatus.Length > 0)
                {
                    byte doorStatus = statusInfo.byDoorStatus[0];
                    
                    switch (doorStatus)
                    {
                        case 1: // 休眠状态/正常状态
                            return (DeviceStatus.Online, "设备在线，门状态正常");
                            
                        case 2: // 常开状态
                            return (DeviceStatus.AlwaysOpen, "门处于常开状态");
                            
                        case 3: // 常闭状态  
                            return (DeviceStatus.AlwaysClose, "门处于常闭状态");
                            
                        case 4: // 普通状态
                            return (DeviceStatus.Online, "设备在线，门状态正常");
                            
                        default:
                            return (DeviceStatus.Online, $"设备在线，门状态未知({doorStatus})");
                    }
                }
                else
                {
                    // 没有门状态信息，但设备响应正常
                    return (DeviceStatus.Online, "设备在线，无门状态信息");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"解析设备状态时发生异常: {ex.Message}");
                return (DeviceStatus.Unknown, $"状态解析异常: {ex.Message}");
            }
        }

        /// <summary>
        /// 获取错误消息
        /// </summary>
        /// <param name="errorCode">错误码</param>
        /// <returns>错误消息</returns>
        private string GetErrorMessage(uint errorCode)
        {
            switch (errorCode)
            {
                case 1: return "用户未登录";
                case 2: return "密码错误";
                case 3: return "权限不足";
                case 4: return "通道号错误";
                case 5: return "连接到设备的客户端个数超过最大值";
                case 6: return "版本不匹配";
                case 7: return "连接设备失败";
                case 8: return "向设备发送失败";
                case 9: return "从设备接收数据失败";
                case 10: return "等待超时";
                case 11: return "缓冲区太小";
                case 12: return "创建SOCKET出错";
                case 13: return "分配资源失败";
                case 14: return "调用顺序错误";
                case 15: return "设备命令执行超时";
                case 16: return "串口号错误";
                case 17: return "报警端口错误";
                case 18: return "参数错误";
                case 19: return "服务器启动失败";
                case 20: return "设备忙";
                default: return $"未知错误(错误码: {errorCode})";
            }
        }

        #endregion
    }
}
