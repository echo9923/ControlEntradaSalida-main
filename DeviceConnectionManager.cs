using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using MySql.Data.MySqlClient;
using System.Runtime.InteropServices;

namespace ControlEntradaSalida
{
    #region Namespace
    // 设备连接信息类
    public class DeviceConnectionInfo
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string IpAddress { get; set; }
        public string Port { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public int UserID { get; set; } = -1;
        public bool IsConnected { get; set; } = false;
        public DateTime LastChecked { get; set; }
        public DateTime LastUsed { get; set; }
        public string StatusMessage { get; set; } = "";
        public DeviceStatus Status { get; set; } = DeviceStatus.Offline;
        public bool IsEnabled { get; set; } = true;
        
        // 重连相关属性
        public int ReconnectAttempts { get; set; } = 0;
        public DateTime LastReconnectTime { get; set; } = DateTime.MinValue;
        public TimeSpan ReconnectDelay { get; set; } = TimeSpan.Zero;
        public bool IsReconnecting { get; set; } = false;
        public bool IsPermanentFailure { get; set; } = false;
        public DateTime NextReconnectTime { get; set; } = DateTime.MinValue;
        
        // 状态历史记录
        public DeviceStatus PreviousStatus { get; set; } = DeviceStatus.Unknown;
        public DateTime StatusChangeTime { get; set; } = DateTime.Now;
        
        // 连接质量指标
        public int ConnectionFailureCount { get; set; } = 0;
        public int SuccessfulConnectionCount { get; set; } = 0;
        public double ConnectionSuccessRate
        {
            get
            {
                int total = ConnectionFailureCount + SuccessfulConnectionCount;
                return total > 0 ? (double)SuccessfulConnectionCount / total * 100 : 0;
            }
        }
        
        // 设备能力信息
        public DeviceCapabilities Capabilities { get; set; }
        
        // 最后一次错误信息
        public uint LastErrorCode { get; set; } = 0;
        public string LastErrorMessage { get; set; } = "";
        
        // 线程安全锁对象
        [System.ComponentModel.Browsable(false)]
        public object LockObject { get; } = new object();
        
        /// <summary>
        /// 更新设备状态
        /// </summary>
        /// <param name="newStatus">新状态</param>
        /// <param name="message">状态消息</param>
        public void UpdateStatus(DeviceStatus newStatus, string message = "")
        {
            lock (LockObject)
            {
                if (Status != newStatus)
                {
                    PreviousStatus = Status;
                    Status = newStatus;
                    StatusChangeTime = DateTime.Now;
                }
                
                if (!string.IsNullOrEmpty(message))
                {
                    StatusMessage = message;
                }
                
                LastChecked = DateTime.Now;
            }
        }
        
        /// <summary>
        /// 重置重连状态
        /// </summary>
        public void ResetReconnectState()
        {
            lock (LockObject)
            {
                ReconnectAttempts = 0;
                IsReconnecting = false;
                IsPermanentFailure = false;
                NextReconnectTime = DateTime.MinValue;
                ReconnectDelay = TimeSpan.Zero;
            }
        }
        
        /// <summary>
        /// 记录连接成功
        /// </summary>
        public void RecordConnectionSuccess()
        {
            lock (LockObject)
            {
                SuccessfulConnectionCount++;
                ResetReconnectState();
                LastUsed = DateTime.Now;
            }
        }
        
        /// <summary>
        /// 记录连接失败
        /// </summary>
        /// <param name="errorCode">错误码</param>
        /// <param name="errorMessage">错误消息</param>
        public void RecordConnectionFailure(uint errorCode = 0, string errorMessage = "")
        {
            lock (LockObject)
            {
                ConnectionFailureCount++;
                LastErrorCode = errorCode;
                LastErrorMessage = errorMessage;
            }
        }
    }
    
    // 设备状态枚举
    public enum DeviceStatus
    {
        Online,      // 在线
        Offline,     // 离线
        AlwaysOpen,  // 常开
        AlwaysClose, // 常闭
        Unknown      // 未知
    }

    // 设备连接管理器
    #region DeviceConnectionManager
    public class DeviceConnectionManager : IDisposable
    {
        #region 私有成员
        
        private static DeviceConnectionManager _instance;
        private static readonly object _lock = new object();
        
        private readonly ConcurrentBag<DeviceConnectionInfo> _devices;
        private readonly ConcurrentDictionary<int, SemaphoreSlim> _connectionSemaphores;
        private System.Timers.Timer _statusCheckTimer;
        private readonly ReconnectManager _reconnectManager;
        private readonly DeviceStatusEngine _statusEngine;
        private readonly SafeUIUpdater _uiUpdater;
        
        private const int STATUS_CHECK_INTERVAL = 30000; // 30秒检查一次设备状态
        private const int CONNECTION_TIMEOUT = 5000; // 5秒连接超时
        private const int MAX_CONCURRENT_CONNECTIONS = 10; // 最大并发连接数
        
        private volatile bool _disposed = false;
        
        #endregion

        #region 事件
        
        /// <summary>
        /// 设备连接状态改变时触发
        /// </summary>
        public event EventHandler<DeviceStatusChangedEventArgs> DeviceStatusChanged;
        
        /// <summary>
        /// 设备连接事件
        /// </summary>
        public event EventHandler<DeviceConnectionEventArgs> DeviceConnectionStateChanged;
        
        /// <summary>
        /// 设备重连事件
        /// </summary>
        public event EventHandler<DeviceReconnectEventArgs> DeviceReconnectAttempt;
        
        /// <summary>
        /// 设备错误事件
        /// </summary>
        public event EventHandler<DeviceErrorEventArgs> DeviceError;
        
        #endregion

        #region 构造函数
        
        private DeviceConnectionManager()
        {
            _devices = new ConcurrentBag<DeviceConnectionInfo>();
            _connectionSemaphores = new ConcurrentDictionary<int, SemaphoreSlim>();
            _statusEngine = new DeviceStatusEngine();
            _uiUpdater = new SafeUIUpdater();
            
            // 初始化重连管理器
            _reconnectManager = new ReconnectManager();
            _reconnectManager.ProcessPendingReconnects = ProcessPendingReconnects;
            
            // 订阅重连事件
            _reconnectManager.ReconnectAttemptStarted += OnReconnectAttemptStarted;
            _reconnectManager.ReconnectSucceeded += OnReconnectSucceeded;
            _reconnectManager.ReconnectFailed += OnReconnectFailed;
            _reconnectManager.PermanentFailure += OnPermanentFailure;
            
            InitializeTimer();
        }
        
        #endregion

        #region 单例属性
        
        public static DeviceConnectionManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        if (_instance == null)
                        {
                            _instance = new DeviceConnectionManager();
                        }
                    }
                }
                return _instance;
            }
        }
        
        #endregion

        #region 初始化方法
        
        /// <summary>
        /// 初始化定时器
        /// </summary>
        private void InitializeTimer()
        {
            _statusCheckTimer = new System.Timers.Timer(STATUS_CHECK_INTERVAL);
            _statusCheckTimer.Elapsed += async (sender, e) => await CheckAllDeviceStatusAsync();
            _statusCheckTimer.AutoReset = true;
            _statusCheckTimer.Start();
        }
        
        #endregion

        #region 设备管理方法
        
        /// <summary>
        /// 加载所有设备信息
        /// </summary>
        public void LoadAllDevices()
        {
            // 清空现有设备列表
            while (_devices.TryTake(out _)) { }
            
            Common cmn = new Common();
            string connstr = cmn.obtenerCadenaConexion();
            BaseDatosMySQL bd = new BaseDatosMySQL();
            bd.conectarMySQL(connstr);
            
            if (bd.conn != null)
            {
                // 修改SQL查询，加载所有设备并获取其启用状态
                string sql = "SELECT device_id, device_name, ip_address, port, username, password, status, last_used_time FROM devices";
                try
                {
                    MySqlCommand cmd = new MySqlCommand(sql, bd.conn);
                    MySqlDataReader rdr = cmd.ExecuteReader();
                    
                    while (rdr.Read())
                    {
                        DeviceConnectionInfo device = new DeviceConnectionInfo
                        {
                            Id = Convert.ToInt32(rdr["device_id"]),
                            Name = rdr["device_name"].ToString(),
                            IpAddress = rdr["ip_address"].ToString(),
                            Port = rdr["port"].ToString(),
                            Username = rdr["username"].ToString(),
                            Password = rdr["password"].ToString(),
                            // 设置设备启用状态（状态为1表示启用，其他值表示禁用）
                            IsEnabled = Convert.ToInt32(rdr["status"]) == 1,
                            LastUsed = rdr["last_used_time"] != DBNull.Value ? Convert.ToDateTime(rdr["last_used_time"]) : DateTime.MinValue
                        };
                        
                        // 初始化设备信号量
                        _connectionSemaphores.TryAdd(device.Id, new SemaphoreSlim(1, 1));
                        
                        _devices.Add(device);
                    }
                    
                    rdr.Close();
                }
                catch (Exception ex)
                {
                    // 记录错误日志
                    Console.WriteLine($"加载设备信息时出错: {ex.Message}");
                    OnDeviceError(new DeviceErrorEventArgs(null, 0, $"加载设备信息失败: {ex.Message}", ex, "Database"));
                }
                finally
                {
                    bd.desconectarMySQL();
                }
            }
        }

        /// <summary>
        /// 获取所有设备
        /// </summary>
        /// <returns>设备列表</returns>
        public List<DeviceConnectionInfo> GetAllDevices()
        {
            return _devices.ToList();
        }

        /// <summary>
        /// 根据ID获取设备
        /// </summary>
        /// <param name="id">设备ID</param>
        /// <returns>设备信息</returns>
        public DeviceConnectionInfo GetDeviceById(int id)
        {
            return _devices.FirstOrDefault(d => d.Id == id);
        }

        /// <summary>
        /// 根据IP和端口获取设备
        /// </summary>
        /// <param name="ip">IP地址</param>
        /// <param name="port"端口</param>
        /// <returns>设备信息</returns>
        public DeviceConnectionInfo GetDeviceByAddress(string ip, string port)
        {
            return _devices.FirstOrDefault(d => d.IpAddress == ip && d.Port == port);
        }

        #endregion
        
        #region 连接管理方法
        
        /// <summary>
        /// 连接指定设备（异步版本）
        /// </summary>
        /// <param name="device">设备信息</param>
        /// <returns>连接是否成功</returns>
        public async Task<bool> ConnectToDeviceAsync(DeviceConnectionInfo device)
        {
            if (device == null) return false;
            if (!device.IsEnabled) return false;
            if (_disposed) return false;

            // 获取设备的连接信号量，防止并发连接冲突
            var semaphore = _connectionSemaphores.GetOrAdd(device.Id, _ => new SemaphoreSlim(1, 1));
            
            try
            {
                // 等待连接许可
                await semaphore.WaitAsync(CONNECTION_TIMEOUT);
                
                try
                {
                    return await Task.Run(() => ConnectToDeviceInternal(device));
                }
                finally
                {
                    semaphore.Release();
                }
            }
            catch (TimeoutException)
            {
                var errorMsg = "连接超时，设备可能忙碌";
                device.RecordConnectionFailure(0, errorMsg);
                device.UpdateStatus(DeviceStatus.Offline, errorMsg);
                OnDeviceError(new DeviceErrorEventArgs(device, 0, errorMsg, null, "Timeout"));
                return false;
            }
            catch (Exception ex)
            {
                var errorMsg = $"连接异常: {ex.Message}";
                device.RecordConnectionFailure(0, errorMsg);
                device.UpdateStatus(DeviceStatus.Offline, errorMsg);
                OnDeviceError(new DeviceErrorEventArgs(device, 0, errorMsg, ex, "Exception"));
                return false;
            }
        }
        
        /// <summary>
        /// 连接指定设备（同步版本）
        /// </summary>
        /// <param name="device">设备信息</param>
        /// <returns>连接是否成功</returns>
        public bool ConnectToDevice(DeviceConnectionInfo device)
        {
            return ConnectToDeviceAsync(device).GetAwaiter().GetResult();
        }
        
        /// <summary>
        /// 内部连接方法
        /// </summary>
        /// <param name="device">设备信息</param>
        /// <returns>连接是否成功</returns>
        /// <summary>
        /// 内部连接方法
        /// </summary>
        /// <param name="device">设备信息</param>
        /// <returns>连接是否成功</returns>
        private bool ConnectToDeviceInternal(DeviceConnectionInfo device)
        {
            if (device == null) return false;

            try
            {
                lock (device.LockObject)
                {
                    // 设置连接状态
                    device.IsReconnecting = true;
                }
                
                HCNetSDK.NET_DVR_USER_LOGIN_INFO struLoginInfo = new HCNetSDK.NET_DVR_USER_LOGIN_INFO();
                HCNetSDK.NET_DVR_DEVICEINFO_V40 struDeviceInfoV40 = new HCNetSDK.NET_DVR_DEVICEINFO_V40();
                struDeviceInfoV40.struDeviceV30.sSerialNumber = new byte[HCNetSDK.SERIALNO_LEN];

                struLoginInfo.sDeviceAddress = device.IpAddress;
                struLoginInfo.sUserName = device.Username;
                struLoginInfo.sPassword = device.Password;
                ushort.TryParse(device.Port, out struLoginInfo.wPort);

                int lUserID = HCNetSDK.NET_DVR_Login_V40(ref struLoginInfo, ref struDeviceInfoV40);
                
                if (lUserID >= 0)
                {
                    lock (device.LockObject)
                    {
                        device.UserID = lUserID;
                        device.IsConnected = true;
                        device.IsReconnecting = false;
                        device.RecordConnectionSuccess();
                        
                        // 获取设备能力信息
                        device.Capabilities = _statusEngine.GetDeviceCapabilities(lUserID);
                        
                        // 获取设备真实状态
                        var workStatus = _statusEngine.GetDeviceWorkStatus(lUserID);
                        device.UpdateStatus(workStatus.Status, workStatus.StatusMessage);
                    }
                    
                    // 更新数据库中的最后使用时间
                    UpdateDeviceLastUsed(device.Id);
                    
                    // 重置重连状态
                    _reconnectManager.ResetReconnectState(device.Id);
                    
                    // 触发连接成功事件
                    OnDeviceConnectionStateChanged(new DeviceConnectionEventArgs(device, true, "连接成功"));
                    OnDeviceStatusChanged(new DeviceStatusChangedEventArgs(device, true, "连接成功", "Connection"));
                    
                    return true;
                }
                else
                {
                    uint nErr = HCNetSDK.NET_DVR_GetLastError();
                    var errorMsg = $"连接失败，错误代码: {nErr}";
                    
                    lock (device.LockObject)
                    {
                        device.UserID = -1;
                        device.IsConnected = false;
                        device.IsReconnecting = false;
                        device.RecordConnectionFailure(nErr, errorMsg);
                        device.UpdateStatus(DeviceStatus.Offline, errorMsg);
                    }
                    
                    // 安排重连
                    _reconnectManager.ScheduleReconnect(device.Id, errorMsg);
                    
                    // 触发事件
                    OnDeviceConnectionStateChanged(new DeviceConnectionEventArgs(device, false, errorMsg, nErr));
                    OnDeviceStatusChanged(new DeviceStatusChangedEventArgs(device, false, errorMsg, "Connection"));
                    
                    return false;
                }
            }
            catch (Exception ex)
            {
                var errorMsg = $"连接异常: {ex.Message}";
                
                lock (device.LockObject)
                {
                    device.UserID = -1;
                    device.IsConnected = false;
                    device.IsReconnecting = false;
                    device.RecordConnectionFailure(0, errorMsg);
                    device.UpdateStatus(DeviceStatus.Offline, errorMsg);
                }
                
                // 安排重连
                _reconnectManager.ScheduleReconnect(device.Id, errorMsg);
                
                // 触发事件
                OnDeviceError(new DeviceErrorEventArgs(device, 0, errorMsg, ex, "ConnectionException"));
                OnDeviceStatusChanged(new DeviceStatusChangedEventArgs(device, false, errorMsg, "Exception"));
                
                return false;
            }
        }

        /// <summary>
        /// 断开设备连接
        /// </summary>
        /// <param name="device">设备信息</param>
        public void DisconnectDevice(DeviceConnectionInfo device)
        {
            if (device == null) return;
            
            var semaphore = _connectionSemaphores.GetOrAdd(device.Id, _ => new SemaphoreSlim(1, 1));
            
            try
            {
                semaphore.Wait(CONNECTION_TIMEOUT);
                
                try
                {
                    lock (device.LockObject)
                    {
                        if (device.UserID >= 0)
                        {
                            HCNetSDK.NET_DVR_Logout_V30(device.UserID);
                        }
                        
                        device.UserID = -1;
                        device.IsConnected = false;
                        device.IsReconnecting = false;
                        device.UpdateStatus(DeviceStatus.Offline, "已断开连接");
                    }
                    
                    // 触发事件
                    OnDeviceConnectionStateChanged(new DeviceConnectionEventArgs(device, false, "手动断开连接"));
                    OnDeviceStatusChanged(new DeviceStatusChangedEventArgs(device, false, "手动断开连接", "Manual"));
                }
                finally
                {
                    semaphore.Release();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"断开设备连接时发生异常: {ex.Message}");
                OnDeviceError(new DeviceErrorEventArgs(device, 0, $"断开连接异常: {ex.Message}", ex, "DisconnectException"));
            }
        }

        /// <summary>
        /// 断开所有设备连接
        /// </summary>
        public void DisconnectAllDevices()
        {
            var devices = GetAllDevices();
            Parallel.ForEach(devices, device =>
            {
                if (device.UserID >= 0)
                {
                    DisconnectDevice(device);
                }
            });
        }

        #endregion
        
        #region 状态检测方法
        
        /// <summary>
        /// 检查单个设备状态
        /// </summary>
        /// <param name="device">设备信息</param>
        /// <returns>状态检查结果</returns>
        public bool CheckDeviceStatus(DeviceConnectionInfo device)
        {
            if (device == null || !device.IsEnabled) return false;
            if (_disposed) return false;

            try
            {
                bool previousConnectionState;
                DeviceStatus previousStatus;
                
                lock (device.LockObject)
                {
                    previousConnectionState = device.IsConnected;
                    previousStatus = device.Status;
                }
                
                // 如果设备未连接，尝试连接
                if (device.UserID < 0)
                {
                    return ConnectToDevice(device);
                }
                
                // 使用设备状态引擎检测状态
                var workStatus = _statusEngine.GetDeviceWorkStatus(device.UserID);
                
                lock (device.LockObject)
                {
                    if (workStatus.IsOnline)
                    {
                        device.IsConnected = true;
                        device.UpdateStatus(workStatus.Status, workStatus.StatusMessage);
                    }
                    else
                    {
                        device.IsConnected = false;
                        device.UpdateStatus(DeviceStatus.Offline, workStatus.StatusMessage);
                        
                        // 如果之前是连接状态，现在断开，则需要清理UserID
                        if (previousConnectionState)
                        {
                            device.UserID = -1;
                            device.RecordConnectionFailure(workStatus.LastErrorCode, workStatus.ErrorMessage);
                            
                            // 安排重连
                            _reconnectManager.ScheduleReconnect(device.Id, "设备状态检查失败");
                        }
                    }
                }
                
                // 如果连接状态或设备状态发生改变，触发事件
                if (previousConnectionState != device.IsConnected || previousStatus != device.Status)
                {
                    OnDeviceStatusChanged(new DeviceStatusChangedEventArgs(device, device.IsConnected, 
                        device.StatusMessage, "StatusCheck"));
                }
                
                return workStatus.IsOnline;
            }
            catch (Exception ex)
            {
                bool previousConnectionState;
                DeviceStatus previousStatus;
                
                lock (device.LockObject)
                {
                    previousConnectionState = device.IsConnected;
                    previousStatus = device.Status;
                    
                    device.IsConnected = false;
                    device.UpdateStatus(DeviceStatus.Unknown, $"状态检查异常: {ex.Message}");
                    device.UserID = -1; // 出现异常时重置连接ID
                    device.RecordConnectionFailure(0, ex.Message);
                }
                
                // 安排重连
                _reconnectManager.ScheduleReconnect(device.Id, $"状态检查异常: {ex.Message}");
                
                // 如果状态发生改变，触发事件
                if (previousConnectionState != device.IsConnected || previousStatus != device.Status)
                {
                    OnDeviceError(new DeviceErrorEventArgs(device, 0, ex.Message, ex, "StatusCheckException"));
                    OnDeviceStatusChanged(new DeviceStatusChangedEventArgs(device, false, ex.Message, "Exception"));
                }
                
                return false;
            }
        }
        
        // 获取设备详细状态（实际实现）
        // 通过设备API获取真实状态
        private DeviceStatus GetDeviceDetailedStatus(DeviceConnectionInfo device)
        {
            try
            {
                // 使用NET_DVR_GetDVRConfig获取门禁设备工作状态
                HCNetSDK.NET_DVR_ACS_WORK_STATUS_V50 statusInfo = new HCNetSDK.NET_DVR_ACS_WORK_STATUS_V50();
                statusInfo.Init();
                
                uint dwReturned = 0;
                uint dwCommand = (uint)HCNetSDK.NET_DVR_GET_ACS_WORK_STATUS_V50;
                int nSize = Marshal.SizeOf(statusInfo);
                IntPtr ptrStatusInfo = Marshal.AllocHGlobal(nSize);
                Marshal.StructureToPtr(statusInfo, ptrStatusInfo, false);
                
                bool bRet = HCNetSDK.NET_DVR_GetDVRConfig(device.UserID, dwCommand, -1, ptrStatusInfo, (uint)nSize, ref dwReturned);
                
                if (bRet)
                {
                    // 成功获取状态信息
                    statusInfo = (HCNetSDK.NET_DVR_ACS_WORK_STATUS_V50)Marshal.PtrToStructure(ptrStatusInfo, typeof(HCNetSDK.NET_DVR_ACS_WORK_STATUS_V50));
                    
                    // 根据门状态判断设备状态
                    // 通常门状态：1-休眠状态, 2-常开状态, 3-常闭状态, 4-普通状态
                    if (statusInfo.byDoorStatus != null && statusInfo.byDoorStatus.Length > 0)
                    {
                        byte doorStatus = statusInfo.byDoorStatus[0];
                        switch (doorStatus)
                        {
                            case 2: // 常开状态
                                return DeviceStatus.AlwaysOpen;
                            case 3: // 常闭状态
                                return DeviceStatus.AlwaysClose;
                            case 1: // 休眠状态
                            case 4: // 普通状态
                            default:
                                return DeviceStatus.Online;
                        }
                    }
                    else
                    {
                        // 如果没有门状态信息，默认返回在线
                        return DeviceStatus.Online;
                    }
                }
                else
                {
                    // 获取状态失败，返回离线
                    return DeviceStatus.Offline;
                }
            }
            catch (Exception ex)
            {
                // 出现异常，返回离线
                Console.WriteLine($"获取设备详细状态时出错: {ex.Message}");
                return DeviceStatus.Offline;
            }
        }
        
        // 根据设备状态获取状态消息
        private string GetStatusMessage(DeviceStatus status)
        {
            switch (status)
            {
                case DeviceStatus.Online:
                    return "在线";
                case DeviceStatus.Offline:
                    return "离线";
                case DeviceStatus.AlwaysOpen:
                    return "常开";
                case DeviceStatus.AlwaysClose:
                    return "常闭";
                default:
                    return "未知";
            }
        }

        /// <summary>
        /// 异步检查所有设备状态
        /// </summary>
        private async Task CheckAllDeviceStatusAsync()
        {
            if (_disposed) return;
            
            try
            {
                var devices = GetAllDevices().Where(d => d.IsEnabled).ToList();
                
                if (devices.Count == 0) return;
                
                // 使用并行处理提高检查速度，但限制最大并发数
                var options = new ParallelOptions
                {
                    MaxDegreeOfParallelism = Math.Min(Environment.ProcessorCount, MAX_CONCURRENT_CONNECTIONS)
                };
                
                await Task.Run(() =>
                {
                    Parallel.ForEach(devices, options, device =>
                    {
                        try
                        {
                            CheckDeviceStatus(device);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"检查设备 {device.Name} 状态时发生异常: {ex.Message}");
                        }
                    });
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"检查所有设备状态时发生异常: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 处理待重连的设备
        /// </summary>
        /// <param name="deviceIds">设备ID列表</param>
        private void ProcessPendingReconnects(List<int> deviceIds)
        {
            if (deviceIds == null || deviceIds.Count == 0 || _disposed) return;
            
            try
            {
                Parallel.ForEach(deviceIds, deviceId =>
                {
                    try
                    {
                        var device = GetDeviceById(deviceId);
                        if (device != null && device.IsEnabled && !device.IsConnected)
                        {
                            // 检查是否在冷却期
                            if (_reconnectManager.IsInCooldown(deviceId))
                            {
                                return;
                            }
                            
                            // 尝试重连
                            bool success = ConnectToDevice(device);
                            
                            if (!success)
                            {
                                // 重连失败，更新重连状态
                                var state = _reconnectManager.GetReconnectState(deviceId);
                                if (state != null)
                                {
                                    OnDeviceReconnectAttempt(new DeviceReconnectEventArgs(
                                        deviceId, state.Attempts, state.CurrentDelay, 
                                        state.Attempts >= 10, "重连失败"));
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"处理设备 {deviceId} 重连时发生异常: {ex.Message}");
                    }
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"处理待重连设备时发生异常: {ex.Message}");
            }
        }
        
        #endregion
        
        #region 数据库操作方法
        
        /// <summary>
        /// 更新设备最后使用时间
        /// </summary>
        /// <param name="deviceId">设备ID</param>
        private void UpdateDeviceLastUsed(int deviceId)
        {
            try
            {
                Task.Run(() =>
                {
                    Common cmn = new Common();
                    string connstr = cmn.obtenerCadenaConexion();
                    BaseDatosMySQL bd = new BaseDatosMySQL();
                    bd.conectarMySQL(connstr);
                    
                    if (bd.conn != null)
                    {
                        string sql = "UPDATE devices SET last_used_time = @last_used_time WHERE device_id = @device_id";
                        MySqlCommand cmd = new MySqlCommand(sql, bd.conn);
                        cmd.Parameters.AddWithValue("@last_used_time", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                        cmd.Parameters.AddWithValue("@device_id", deviceId);
                        cmd.ExecuteNonQuery();
                        
                        bd.desconectarMySQL();
                    }
                });
            }
            catch (Exception ex)
            {
                // 记录错误但不中断流程
                Console.WriteLine($"更新设备最后使用时间时出错: {ex.Message}");
            }
        }
        
        #endregion
        
        #region 事件处理方法
        
        /// <summary>
        /// 触发设备状态改变事件
        /// </summary>
        protected virtual void OnDeviceStatusChanged(DeviceStatusChangedEventArgs e)
        {
            try
            {
                DeviceStatusChanged?.Invoke(this, e);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"触发设备状态改变事件时发生异常: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 触发设备连接状态改变事件
        /// </summary>
        protected virtual void OnDeviceConnectionStateChanged(DeviceConnectionEventArgs e)
        {
            try
            {
                DeviceConnectionStateChanged?.Invoke(this, e);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"触发设备连接状态改变事件时发生异常: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 触发设备重连事件
        /// </summary>
        protected virtual void OnDeviceReconnectAttempt(DeviceReconnectEventArgs e)
        {
            try
            {
                DeviceReconnectAttempt?.Invoke(this, e);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"触发设备重连事件时发生异常: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 触发设备错误事件
        /// </summary>
        protected virtual void OnDeviceError(DeviceErrorEventArgs e)
        {
            try
            {
                DeviceError?.Invoke(this, e);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"触发设备错误事件时发生异常: {ex.Message}");
            }
        }
        
        #endregion
        
        #region 重连事件处理
        
        private void OnReconnectAttemptStarted(object sender, DeviceReconnectEventArgs e)
        {
            Console.WriteLine($"设备 {e.DeviceId} 开始第 {e.Attempts} 次重连尝试，下次延迟: {e.NextDelay.TotalSeconds} 秒");
            OnDeviceReconnectAttempt(e);
        }
        
        private void OnReconnectSucceeded(object sender, DeviceReconnectEventArgs e)
        {
            Console.WriteLine($"设备 {e.DeviceId} 重连成功");
        }
        
        private void OnReconnectFailed(object sender, DeviceReconnectEventArgs e)
        {
            Console.WriteLine($"设备 {e.DeviceId} 第 {e.Attempts} 次重连失败: {e.Reason}");
        }
        
        private void OnPermanentFailure(object sender, DeviceReconnectEventArgs e)
        {
            Console.WriteLine($"设备 {e.DeviceId} 达到最大重连次数，进入冷却期");
            
            var device = GetDeviceById(e.DeviceId);
            if (device != null)
            {
                lock (device.LockObject)
                {
                    device.IsPermanentFailure = true;
                }
            }
        }
        
        #endregion
        
        #region IDisposable实现
        
        /// <summary>
        /// 清理资源
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed && disposing)
            {
                try
                {
                    _statusCheckTimer?.Stop();
                    _statusCheckTimer?.Dispose();
                    
                    DisconnectAllDevices();
                    
                    _reconnectManager?.Dispose();
                    
                    // 清理信号量
                    foreach (var semaphore in _connectionSemaphores.Values)
                    {
                        semaphore?.Dispose();
                    }
                    _connectionSemaphores.Clear();
                    
                    _disposed = true;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"清理资源时发生异常: {ex.Message}");
                }
            }
        }
        
        #endregion
        
        #region 兼容性方法（保留旧方法签名）
        

        
        #endregion
        
        #endregion
    }

    // 设备状态改变事件参数
    public class DeviceStatusChangedEventArgs : EventArgs
    {
        public DeviceConnectionInfo Device { get; }
        public bool IsConnected { get; }
        public DeviceStatus PreviousStatus { get; }
        public DeviceStatus CurrentStatus { get; }
        public DateTime Timestamp { get; }
        public string Message { get; }
        public string ChangeReason { get; }

        public DeviceStatusChangedEventArgs(DeviceConnectionInfo device, bool isConnected, 
            string message = "", string changeReason = "")
        {
            Device = device;
            IsConnected = isConnected;
            PreviousStatus = device.PreviousStatus;
            CurrentStatus = device.Status;
            Timestamp = DateTime.Now;
            Message = message ?? device.StatusMessage;
            ChangeReason = changeReason;
        }
    }
    
    // 设备连接事件参数
    public class DeviceConnectionEventArgs : EventArgs
    {
        public DeviceConnectionInfo Device { get; }
        public bool Success { get; }
        public string Message { get; }
        public uint ErrorCode { get; }
        public DateTime Timestamp { get; }
        
        public DeviceConnectionEventArgs(DeviceConnectionInfo device, bool success, 
            string message = "", uint errorCode = 0)
        {
            Device = device;
            Success = success;
            Message = message;
            ErrorCode = errorCode;
            Timestamp = DateTime.Now;
        }
    }
    
    // 设备错误事件参数
    public class DeviceErrorEventArgs : EventArgs
    {
        public DeviceConnectionInfo Device { get; }
        public uint ErrorCode { get; }
        public string ErrorMessage { get; }
        public Exception Exception { get; }
        public DateTime Timestamp { get; }
        public string ErrorType { get; }
        
        public DeviceErrorEventArgs(DeviceConnectionInfo device, uint errorCode, 
            string errorMessage, Exception exception = null, string errorType = "Unknown")
        {
            Device = device;
            ErrorCode = errorCode;
            ErrorMessage = errorMessage;
            Exception = exception;
            Timestamp = DateTime.Now;
            ErrorType = errorType;
        }
    }
    
    #endregion
}