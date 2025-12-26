using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using System.Data.SqlClient;
using System.Runtime.InteropServices;
using System.Diagnostics;

namespace ControlEntradaSalida
{
    #region Namespace
    // 设备连接信息类
    public class DeviceConnectionInfo : IDisposable
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string SerialNumber { get; set; }
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

        // 人脸事件相关状态
        public int AlarmHandle { get; set; } = -1;
        public long LastSerialNo { get; set; } = 0;
        public DateTime LastFaceEventTime { get; set; } = DateTime.MinValue;
        
        // 最后一次错误信息
        public uint LastErrorCode { get; set; } = 0;
        public string LastErrorMessage { get; set; } = "";
        
        // 线程安全锁对象
        [System.ComponentModel.Browsable(false)]
        public object LockObject { get; } = new object();
        
        
        // 设备级 SDK/ISAPI/远程配置互斥锁：确保同一设备上的 HCNetSDK 调用串行化
        private readonly Lazy<SemaphoreSlim> deviceSdkLock;

        [System.ComponentModel.Browsable(false)]
        public SemaphoreSlim DeviceSdkLock => deviceSdkLock.Value;

        public DeviceConnectionInfo()
        {
            deviceSdkLock = new Lazy<SemaphoreSlim>(() =>
                SynchronizationHelper.CreateSemaphore(1, 1, $"Device-{Id}-SDK"));
        }

        public SynchronizationHelper.SemaphoreOperationResult TryAcquireDeviceSdkLock(int timeoutMs, string operationName)
        {
            return SynchronizationHelper.SafeWait(DeviceSdkLock, timeoutMs, operationName);
        }

        public Task<SynchronizationHelper.SemaphoreOperationResult> TryAcquireDeviceSdkLockAsync(int timeoutMs, string operationName)
        {
            return SynchronizationHelper.SafeWaitAsync(DeviceSdkLock, timeoutMs, operationName);
        }
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

        public void Dispose()
        {
            if (deviceSdkLock.IsValueCreated)
            {
                deviceSdkLock.Value.Dispose();
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
        
        private static volatile DeviceConnectionManager _instance;
        private static readonly object _lock = new object();
        
        // 以 ID 为主键（唯一）
        private readonly ConcurrentDictionary<int, DeviceConnectionInfo> _devicesById;
        // 以 IP 为索引（回调热点路径用）
        private readonly ConcurrentDictionary<string, int> _deviceIdByIp;
        private readonly ConcurrentDictionary<int, SemaphoreSlim> _connectionSemaphores;
        private System.Timers.Timer _statusCheckTimer;
        private readonly ReconnectManager _reconnectManager;
        private readonly DeviceStatusEngine _statusEngine;
        private int _statusCheckLoopRunning;
        
        private int statusCheckIntervalMs = 30000; // 30秒检查一次设备状态
        private int connectTimeoutMs = 5000; // 5秒连接等待超时
        private int disconnectTimeoutMs = 5000; // 5秒断开连接等待超时

        private int deviceSdkLockTimeoutMs = 30000; // 30秒设备级 SDK 锁等待（避免远程配置/ISAPI 并发）
        private int statusSdkLockTimeoutMs = 1000;  // 1秒状态检查锁等待（超时则跳过本次检查）
        private int maxConcurrentConnections = 10; // 最大并发连接数
        
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
            _devicesById = new ConcurrentDictionary<int, DeviceConnectionInfo>();
            _deviceIdByIp = new ConcurrentDictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            _connectionSemaphores = new ConcurrentDictionary<int, SemaphoreSlim>();
            _statusEngine = new DeviceStatusEngine();
            
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
            _statusCheckTimer = new System.Timers.Timer(statusCheckIntervalMs);
            _statusCheckTimer.AutoReset = true;
            _statusCheckTimer.Elapsed += (sender, e) =>
            {
                // 避免 async void 重入；任何异常都应被捕获并记录
                if (Interlocked.Exchange(ref _statusCheckLoopRunning, 1) == 1)
                {
                    return;
                }

                _ = Task.Run(async () =>
                {
                    try
                    {
                        await CheckAllDeviceStatusAsync().ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        ServiceLogger.Error("状态轮询任务异常。", ex);
                    }
                    finally
                    {
                        Interlocked.Exchange(ref _statusCheckLoopRunning, 0);
                    }
                });
            };
        }

        /// <summary>
        /// 应用服务配置内容，供设备加载使用。
        /// </summary>
        /// <param name="configuration">服务配置对象</param>
        public void ApplyConfiguration(ServiceConfiguration configuration)
        {
            if (configuration == null)
            {
                throw new ArgumentNullException(nameof(configuration));
            }

            if (_disposed)
            {
                return;
            }

            var deviceOptions = configuration.DeviceConnection;
            if (deviceOptions != null)
            {
                statusCheckIntervalMs = deviceOptions.StatusCheckIntervalMs;
                statusSdkLockTimeoutMs = deviceOptions.StatusCheckSdkLockTimeoutMs;
                deviceSdkLockTimeoutMs = deviceOptions.DeviceSdkLockTimeoutMs;
                connectTimeoutMs = deviceOptions.ConnectTimeoutMs;
                disconnectTimeoutMs = deviceOptions.DisconnectTimeoutMs;
                maxConcurrentConnections = deviceOptions.MaxConcurrentConnections;

                if (_statusCheckTimer != null)
                {
                    _statusCheckTimer.Interval = statusCheckIntervalMs;
                }
            }

            if (configuration.Reconnect != null)
            {
                _reconnectManager.ApplyConfiguration(configuration.Reconnect);
            }
        }
        
        #endregion

        #region 设备管理方法
        
        /// <summary>
        /// 加载所有设备信息（完全依赖数据库数据）
        /// </summary>
        public void LoadAllDevices()
        {
            if (_disposed)
            {
                return;
            }

            try
            {
                DisconnectAllDevices();

                foreach (var device in _devicesById.Values)
                {
                    device?.Dispose();
                }
            }
            catch (Exception ex)
            {
                ServiceLogger.Error("重新加载设备列表前清理旧设备资源失败。", ex);
            }

            _devicesById.Clear();
            _deviceIdByIp.Clear();

            LoadDevicesFromDatabase();

            Task.Run(async () => { await InitializeDeviceConnectionsAsync(); });
        }

        private int LoadDevicesFromDatabase()
        {
            int newDevices = 0;

            if (!TryOpenDatabase(out SqlServerDatabase db))
            {
                ServiceLogger.Warn("数据库连接失败，无法加载设备列表。");
                return newDevices;
            }

            using (db)
            {
                string sql = "SELECT device_id, device_name, ip_address, port, username, password, status, last_used_time FROM devices";

                try
                {
                    using (SqlCommand cmd = db.CreateCommand(sql))
                    using (SqlDataReader rdr = cmd.ExecuteReader())
                    {
                        while (rdr.Read())
                        {
                            int deviceId = Convert.ToInt32(rdr["device_id"]);

                            DeviceConnectionInfo device = new DeviceConnectionInfo
                            {
                                Id = deviceId,
                                Name = rdr["device_name"].ToString(),
                                IpAddress = rdr["ip_address"].ToString(),
                                Port = rdr["port"].ToString(),
                                Username = rdr["username"].ToString(),
                                Password = rdr["password"].ToString(),
                                IsEnabled = Convert.ToInt32(rdr["status"]) == 1,
                                LastUsed = rdr["last_used_time"] != DBNull.Value ? Convert.ToDateTime(rdr["last_used_time"]) : DateTime.MinValue
                            };

                            if (!_devicesById.TryAdd(device.Id, device))
                            {
                                continue;
                            }

                            _connectionSemaphores.TryAdd(device.Id,
                                SynchronizationHelper.CreateSemaphore(1, 1, $"Device-{device.Id}-Connection"));

                            string ipKey = device.IpAddress?.Trim();
                            if (!string.IsNullOrWhiteSpace(ipKey))
                            {
                                _deviceIdByIp.TryAdd(ipKey, device.Id);
                            }

                            newDevices++;
                        }
                    }

                    if (newDevices > 0)
                    {
                        ServiceLogger.Info($"数据库查询完成，共加载 {newDevices} 台设备。");
                    }
                    else
                    {
                        ServiceLogger.Warn("数据库查询完成，但未找到任何设备记录。");
                    }
                }
                catch (Exception ex)
                {
                    ServiceLogger.Error("加载设备信息失败。", ex);
                    OnDeviceError(new DeviceErrorEventArgs(null, 0, $"加载设备信息失败: {ex.Message}", ex, "Database"));
                }
            }

            return newDevices;
        }

        /// <summary>
        /// 初始化设备连接（新增方法）
        /// </summary>
        private async Task InitializeDeviceConnectionsAsync()
        {
            var enabledDevices = GetAllDevices().Where(d => d.IsEnabled).ToList();

            if (enabledDevices.Count == 0) return;

            ServiceLogger.Info($"开始初始化 {enabledDevices.Count} 个启用设备的连接状态。");

            // 并行检查所有启用设备的连接状态
            var options = new ParallelOptions
            {
                MaxDegreeOfParallelism = Math.Min(Environment.ProcessorCount, maxConcurrentConnections)
            };

            await Task.Run(() =>
            {
                Parallel.ForEach(enabledDevices, options, device =>
                {
                    try
                    {
                        ServiceLogger.Debug($"检查设备 {device.Id}({device.Name}) 的连接状态。");

                        // 尝试连接设备并更新状态
                        bool connected = ConnectToDevice(device);

                        if (connected)
                        {
                            ServiceLogger.Info($"设备 {device.Id}({device.Name}) 连接成功。");
                        }
                        else
                        {
                            ServiceLogger.Warn($"设备 {device.Id}({device.Name}) 连接失败: {device.StatusMessage}");
                        }
                    }
                    catch (Exception ex)
                    {
                        ServiceLogger.Error($"初始化设备 {device.Id}({device.Name}) 连接时发生异常。", ex);
                    }
                });
            });

            ServiceLogger.Info("设备连接初始化完成。");
        }

        /// <summary>
        /// 获取所有设备
        /// </summary>
        /// <returns>设备列表</returns>
        public List<DeviceConnectionInfo> GetAllDevices()
        {
            var devices = new List<DeviceConnectionInfo>(_devicesById.Count);
            foreach (var kvp in _devicesById)
            {
                devices.Add(kvp.Value);
            }
            return devices;
        }

        /// <summary>
        /// 根据ID获取设备
        /// </summary>
        /// <param name="id">设备ID</param>
        /// <returns>设备信息</returns>
        public DeviceConnectionInfo GetDeviceById(int id)
        {
            return _devicesById.TryGetValue(id, out DeviceConnectionInfo device) ? device : null;
        }

        /// <summary>
        /// 按设备 IP 快速查找设备（回调热点路径使用，避免线性扫描与频繁分配）。
        /// </summary>
        public bool TryGetDeviceByIp(string ip, out DeviceConnectionInfo device)
        {
            device = null;
            if (string.IsNullOrWhiteSpace(ip))
            {
                return false;
            }

            return _deviceIdByIp.TryGetValue(ip, out int id)
                && _devicesById.TryGetValue(id, out device);
        }

        /// <summary>
        /// 根据IP和端口获取设备
        /// </summary>
        /// <param name="ip">IP地址</param>
        /// <param name="port">端口</param>
        /// <returns>设备信息</returns>
        public DeviceConnectionInfo GetDeviceByAddress(string ip, string port)
        {
            if (string.IsNullOrWhiteSpace(ip) || string.IsNullOrWhiteSpace(port))
            {
                return null;
            }

            if (_deviceIdByIp.TryGetValue(ip, out int deviceId)
                && _devicesById.TryGetValue(deviceId, out DeviceConnectionInfo device)
                && string.Equals(device.IpAddress, ip, StringComparison.OrdinalIgnoreCase)
                && string.Equals(device.Port, port, StringComparison.OrdinalIgnoreCase))
            {
                return device;
            }

            foreach (var kvp in _devicesById)
            {
                DeviceConnectionInfo candidate = kvp.Value;
                if (string.Equals(candidate.IpAddress, ip, StringComparison.OrdinalIgnoreCase)
                    && string.Equals(candidate.Port, port, StringComparison.OrdinalIgnoreCase))
                {
                    return candidate;
                }
            }

            return null;
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
            var semaphore = _connectionSemaphores.GetOrAdd(device.Id, _ => 
                SynchronizationHelper.CreateSemaphore(1, 1, $"Device-{device.Id}-Connection"));
            
            // 使用安全的信号量操作
            using (var semaphoreResult = await SynchronizationHelper.SafeWaitAsync(
                semaphore, connectTimeoutMs, $"ConnectDevice-{device.Id}").ConfigureAwait(false))
            {
                if (!semaphoreResult.IsAcquired)
                {
                    var infoMsg = $"连接请求跳过：连接信号量获取超时（{connectTimeoutMs}ms），设备可能忙碌或本机并发受限。";
                    ServiceLogger.Warn($"设备 {device.Id}({device.Name}) {infoMsg}");
                    device.UpdateStatus(device.Status, infoMsg);
                    OnDeviceError(new DeviceErrorEventArgs(device, 0, infoMsg, null, "BusySkip"));
                    return false;
                }

                try
                {
                    Debug.WriteLine($"[DeviceConnectionManager] 开始连接设备 {device.Id} ({device.Name})");
                    var result = await Task.Run(() => ConnectToDeviceInternal(device)).ConfigureAwait(false);
                    Debug.WriteLine($"[DeviceConnectionManager] 设备 {device.Id} 连接结果: {result}");
                    return result;
                }
                catch (Exception ex)
                {
                    var errorMsg = $"连接异常: {ex.Message}";
                    device.RecordConnectionFailure(0, errorMsg);
                    device.UpdateStatus(DeviceStatus.Offline, errorMsg);
                    OnDeviceError(new DeviceErrorEventArgs(device, 0, errorMsg, ex, "Exception"));
                    Debug.WriteLine($"[DeviceConnectionManager] 设备 {device.Id} 连接异常: {ex.Message}");
                    return false;
                }
            } // using语句确保信号量正确释放
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

            string ipAddress;
            string username;
            string password;
            string port;

            lock (device.LockObject)
            {
                ipAddress = device.IpAddress;
                username = device.Username;
                password = device.Password;
                port = device.Port;
            }

            DeviceConnectionEventArgs connectionEventArgs = null;
            DeviceStatusChangedEventArgs statusEventArgs = null;
            DeviceErrorEventArgs errorEventArgs = null;
            bool result = false;

            using (var sdkLock = device.TryAcquireDeviceSdkLock(
                deviceSdkLockTimeoutMs,
                $"ConnectDeviceSdk-{device.Id}"))
            {
                if (!sdkLock.IsAcquired)
                {
                    var errorMsg = "设备忙，获取设备SDK锁超时，稍后重试。";

                    lock (device.LockObject)
                    {
                        device.UserID = -1;
                        device.IsConnected = false;
                        device.IsReconnecting = false;
                        device.RecordConnectionFailure(0, errorMsg);
                        device.UpdateStatus(DeviceStatus.Offline, errorMsg);
                    }

                    _reconnectManager.ScheduleReconnect(device.Id, errorMsg);

                    connectionEventArgs = new DeviceConnectionEventArgs(device, false, errorMsg);
                    statusEventArgs = new DeviceStatusChangedEventArgs(device, false, errorMsg, "Connection");
                    result = false;
                }
                else
                {
                    try
                    {
                        lock (device.LockObject)
                        {
                            device.IsReconnecting = true;
                        }

                        HCNetSDK.NET_DVR_USER_LOGIN_INFO struLoginInfo = new HCNetSDK.NET_DVR_USER_LOGIN_INFO();
                        HCNetSDK.NET_DVR_DEVICEINFO_V40 struDeviceInfoV40 = new HCNetSDK.NET_DVR_DEVICEINFO_V40();
                        struDeviceInfoV40.struDeviceV30.sSerialNumber = new byte[HCNetSDK.SERIALNO_LEN];
                        struDeviceInfoV40.byRes2 = new byte[246];
                        struLoginInfo.byRes3 = new byte[120];

                        struLoginInfo.sDeviceAddress = ipAddress;
                        struLoginInfo.sUserName = username;
                        struLoginInfo.sPassword = password;
                        ushort.TryParse(port, out struLoginInfo.wPort);

                        int lUserID = HCNetSDK.NET_DVR_Login_V40(ref struLoginInfo, ref struDeviceInfoV40);

                        if (lUserID >= 0)
                        {
                            string serialNumber = Encoding.ASCII.GetString(struDeviceInfoV40.struDeviceV30.sSerialNumber)
                                .TrimEnd('\0')
                                .Trim();

                            DeviceCapabilities capabilities = _statusEngine.GetDeviceCapabilities(lUserID);
                            DeviceWorkStatus workStatus = _statusEngine.GetDeviceWorkStatus(lUserID);

                            lock (device.LockObject)
                            {
                                device.UserID = lUserID;
                                device.IsConnected = true;
                                device.IsReconnecting = false;
                                device.RecordConnectionSuccess();

                                if (!string.IsNullOrWhiteSpace(serialNumber))
                                {
                                    device.SerialNumber = serialNumber;
                                }

                                device.Capabilities = capabilities;
                                device.UpdateStatus(workStatus.Status, workStatus.StatusMessage);
                            }

                            UpdateDeviceLastUsed(device.Id);
                            _reconnectManager.ResetReconnectState(device.Id);

                            connectionEventArgs = new DeviceConnectionEventArgs(device, true, "连接成功");
                            statusEventArgs = new DeviceStatusChangedEventArgs(device, true, "连接成功", "Connection");
                            result = true;
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

                            _reconnectManager.ScheduleReconnect(device.Id, errorMsg);

                            connectionEventArgs = new DeviceConnectionEventArgs(device, false, errorMsg, nErr);
                            statusEventArgs = new DeviceStatusChangedEventArgs(device, false, errorMsg, "Connection");
                            result = false;
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

                        _reconnectManager.ScheduleReconnect(device.Id, errorMsg);

                        errorEventArgs = new DeviceErrorEventArgs(device, 0, errorMsg, ex, "ConnectionException");
                        statusEventArgs = new DeviceStatusChangedEventArgs(device, false, errorMsg, "Exception");
                        result = false;
                    }
                }
            }

            // 注意：不要在持有 device.DeviceSdkLock 时触发事件，
            // 否则订阅方（如 FaceEventService）在回调里再获取设备 SDK 锁会导致超时/假死。
            if (connectionEventArgs != null)
            {
                OnDeviceConnectionStateChanged(connectionEventArgs);
            }

            if (statusEventArgs != null)
            {
                OnDeviceStatusChanged(statusEventArgs);
            }

            if (errorEventArgs != null)
            {
                OnDeviceError(errorEventArgs);
            }

            return result;
        }

        /// <summary>
        /// 断开设备连接
        /// </summary>
        /// <param name="device">设备信息</param>
        public void DisconnectDevice(DeviceConnectionInfo device)
        {
            if (device == null) return;

            var semaphore = _connectionSemaphores.GetOrAdd(device.Id, _ =>
                SynchronizationHelper.CreateSemaphore(1, 1, $"Device-{device.Id}-Disconnect"));

            using (var semaphoreResult = SynchronizationHelper.SafeWait(
                semaphore, disconnectTimeoutMs, $"DisconnectDevice-{device.Id}"))
            {
                if (!semaphoreResult.IsAcquired)
                {
                    var errorMsg = "断开设备连接等待信号量超时，设备可能忙碌";
                    Debug.WriteLine($"[DeviceConnectionManager] {errorMsg} - 设备ID: {device.Id}");
                    OnDeviceError(new DeviceErrorEventArgs(device, 0, errorMsg, null, "Timeout"));
                    return;
                }

                try
                {
                    Debug.WriteLine($"[DeviceConnectionManager] 开始断开设备 {device.Id} ({device.Name})");

                    using (var sdkLock = device.TryAcquireDeviceSdkLock(
                        deviceSdkLockTimeoutMs,
                        $"DisconnectDeviceSdk-{device.Id}"))
                    {
                        if (!sdkLock.IsAcquired)
                        {
                            var errorMsg = "断开连接获取设备SDK锁超时，设备可能忙碌";
                            Debug.WriteLine($"[DeviceConnectionManager] {errorMsg} - 设备ID: {device.Id}");
                            OnDeviceError(new DeviceErrorEventArgs(device, 0, errorMsg, null, "Timeout"));
                            return;
                        }

                        int userIdToLogout;
                        lock (device.LockObject)
                        {
                            userIdToLogout = device.UserID;
                        }

                        if (userIdToLogout >= 0)
                        {
                            HCNetSDK.NET_DVR_Logout_V30(userIdToLogout);
                            Debug.WriteLine($"[DeviceConnectionManager] 设备 {device.Id} SDK登出完成");
                        }

                        lock (device.LockObject)
                        {
                            device.UserID = -1;
                            device.IsConnected = false;
                            device.IsReconnecting = false;
                            device.UpdateStatus(DeviceStatus.Offline, "已断开连接");
                        }
                    }

                    OnDeviceConnectionStateChanged(new DeviceConnectionEventArgs(device, false, "手动断开连接"));
                    OnDeviceStatusChanged(new DeviceStatusChangedEventArgs(device, false, "手动断开连接", "Manual"));

                    Debug.WriteLine($"[DeviceConnectionManager] 设备 {device.Id} 断开连接完成");
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[DeviceConnectionManager] 断开设备 {device.Id} 连接时发生异常: {ex.Message}");
                    OnDeviceError(new DeviceErrorEventArgs(device, 0, $"断开连接异常: {ex.Message}", ex, "DisconnectException"));
                }
            }
        }

        /// <summary>
        /// 断开所有设备连接
        /// </summary>
        public void DisconnectAllDevices()
        {
            var devices = GetAllDevices();
            var options = new ParallelOptions
            {
                MaxDegreeOfParallelism = Math.Max(1, maxConcurrentConnections)
            };

            Parallel.ForEach(devices, options, device =>
            {
                if (device.UserID >= 0)
                {
                    DisconnectDevice(device);
                }
            });
        }

        /// <summary>
        /// 暂停设备状态监控。
        /// </summary>
        public void SuspendMonitoring()
        {
            if (_statusCheckTimer != null)
            {
                _statusCheckTimer.Stop();
            }
        }

        /// <summary>
        /// 恢复设备状态监控。
        /// </summary>
        public void ResumeMonitoring()
        {
            if (_statusCheckTimer == null)
            {
                InitializeTimer();
            }

            if (_statusCheckTimer != null && !_statusCheckTimer.Enabled)
            {
                _statusCheckTimer.Start();
            }
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
                DateTime lastConnectionSuccess;
                int currentUserId;

                lock (device.LockObject)
                {
                    previousConnectionState = device.IsConnected;
                    previousStatus = device.Status;
                    lastConnectionSuccess = device.LastUsed;
                    currentUserId = device.UserID;
                }

                if (currentUserId < 0)
                {
                    return ConnectToDevice(device);
                }

                bool isRecentlyConnected = (DateTime.Now - lastConnectionSuccess).TotalSeconds < 30;

                DeviceWorkStatus workStatus;
                using (var sdkLock = device.TryAcquireDeviceSdkLock(
                    statusSdkLockTimeoutMs,
                    $"StatusCheckSdk-{device.Id}"))
                {
                    if (!sdkLock.IsAcquired)
                    {
                        ServiceLogger.Debug($"[状态检查] 设备 {device.Id}({device.Name}) 忙碌，跳过本次状态检查。");
                        return previousConnectionState;
                    }

                    workStatus = _statusEngine.GetDeviceWorkStatus(currentUserId);
                }

                bool connectionStateChanged = false;
                bool statusChanged = false;

                lock (device.LockObject)
                {
                    if (workStatus.IsOnline)
                    {
                        device.IsConnected = true;
                        device.UpdateStatus(workStatus.Status, workStatus.StatusMessage);

                        if (!previousConnectionState)
                        {
                            _reconnectManager.ResetReconnectState(device.Id);
                            connectionStateChanged = true;
                        }

                        statusChanged = previousStatus != device.Status;
                    }
                    else
                    {
                        if (isRecentlyConnected && previousConnectionState)
                        {
                            ServiceLogger.Debug($"[容错机制] 设备 {device.Id}({device.Name}) 最近连接成功，忽略此次状态检查失败。");
                            return true;
                        }

                        device.IsConnected = false;
                        device.UpdateStatus(DeviceStatus.Offline, workStatus.StatusMessage);

                        if (previousConnectionState)
                        {
                            device.UserID = -1;
                            device.RecordConnectionFailure(workStatus.LastErrorCode, workStatus.ErrorMessage);

                            var reconnectState = _reconnectManager.GetReconnectState(device.Id);
                            if (reconnectState == null || reconnectState.NextRetry == DateTime.MinValue)
                            {
                                ServiceLogger.Warn($"[状态检查] 设备 {device.Id}({device.Name}) 连接丢失，安排重连。");
                                _reconnectManager.ScheduleReconnect(device.Id, "设备状态检查失败");
                            }
                            else
                            {
                                ServiceLogger.Debug($"[状态检查] 设备 {device.Id}({device.Name}) 已在重连队列中，跳过重复调度。");
                            }

                            connectionStateChanged = true;
                        }

                        statusChanged = previousStatus != device.Status;
                    }
                }

                if (connectionStateChanged || statusChanged)
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
                    device.UserID = -1;
                    device.RecordConnectionFailure(0, ex.Message);
                }

                var reconnectState = _reconnectManager.GetReconnectState(device.Id);
                if (reconnectState == null || reconnectState.NextRetry == DateTime.MinValue)
                {
                    ServiceLogger.Error($"[异常处理] 设备 {device.Id}({device.Name}) 状态检查异常，安排重连。", ex);
                    _reconnectManager.ScheduleReconnect(device.Id, $"状态检查异常: {ex.Message}");
                }
                else
                {
                    ServiceLogger.Debug($"[异常处理] 设备 {device.Id}({device.Name}) 已在重连队列中，跳过重复调度。");
                }

                if (previousConnectionState != device.IsConnected || previousStatus != device.Status)
                {
                    OnDeviceError(new DeviceErrorEventArgs(device, 0, ex.Message, ex, "StatusCheckException"));
                    OnDeviceStatusChanged(new DeviceStatusChangedEventArgs(device, false, ex.Message, "Exception"));
                }

                return false;
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
                    MaxDegreeOfParallelism = Math.Min(Environment.ProcessorCount, maxConcurrentConnections)
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
                            ServiceLogger.Error($"检查设备 {device.Name} 状态时发生异常。", ex);
                        }
                    });
                });
            }
            catch (Exception ex)
            {
                ServiceLogger.Error("检查所有设备状态时发生异常。", ex);
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
                var options = new ParallelOptions
                {
                    MaxDegreeOfParallelism = Math.Max(1, maxConcurrentConnections)
                };

                Parallel.ForEach(deviceIds, options, deviceId =>
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
                                        state.Attempts >= _reconnectManager.MaxReconnectAttempts, "重连失败"));
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        ServiceLogger.Error($"处理设备 {deviceId} 重连时发生异常。", ex);
                    }
                });
            }
            catch (Exception ex)
            {
                ServiceLogger.Error("处理待重连设备时发生异常。", ex);
            }
        }
        
        #endregion
        
        #region 数据库操作方法
        
        /// <summary>
        /// 更新设备最后使用时间
        /// </summary>
        /// <param name="deviceId">设备ID</param>
        private bool TryOpenDatabase(out SqlServerDatabase db)
        {
            db = null;

            Common common = new Common();
            string connStr = common.obtenerCadenaConexion();
            db = new SqlServerDatabase(common.obtenerTiempoEsperaComando());
            db.Connect(connStr);

            if (db.Connection == null)
            {
                db.Dispose();
                db = null;
                return false;
            }

            return true;
        }

        private void UpdateDeviceLastUsed(int deviceId)
        {
            Task.Run(() =>
            {
                try
                {
                    if (!TryOpenDatabase(out SqlServerDatabase db))
                    {
                        return;
                    }

                    using (db)
                    {
                        const string sql = "UPDATE devices SET last_used_time = SYSDATETIME() WHERE device_id = @device_id";
                        using (SqlCommand cmd = db.CreateCommand(sql))
                        {
                            cmd.Parameters.AddWithValue("@device_id", deviceId);
                            cmd.ExecuteNonQuery();
                        }
                    }
                }
                catch (Exception ex)
                {
                    // 记录错误但不中断流程
                    ServiceLogger.Error("更新设备最后使用时间时出错。", ex);
                }
            });
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
                ServiceLogger.Error("触发设备状态改变事件时发生异常。", ex);
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
                ServiceLogger.Error("触发设备连接状态改变事件时发生异常。", ex);
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
                ServiceLogger.Error("触发设备重连事件时发生异常。", ex);
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
                ServiceLogger.Error("触发设备错误事件时发生异常。", ex);
            }
        }
        
        #endregion
        
        #region 重连事件处理
        
        private void OnReconnectAttemptStarted(object sender, DeviceReconnectEventArgs e)
        {
            ServiceLogger.Info($"设备 {e.DeviceId} 开始第 {e.Attempts} 次重连尝试，下次延迟: {e.NextDelay.TotalSeconds} 秒。");
            OnDeviceReconnectAttempt(e);
        }
        
        private void OnReconnectSucceeded(object sender, DeviceReconnectEventArgs e)
        {
            ServiceLogger.Info($"设备 {e.DeviceId} 重连成功。");
        }
        
        private void OnReconnectFailed(object sender, DeviceReconnectEventArgs e)
        {
            ServiceLogger.Warn($"设备 {e.DeviceId} 第 {e.Attempts} 次重连失败: {e.Reason}");
        }
        
        private void OnPermanentFailure(object sender, DeviceReconnectEventArgs e)
        {
            ServiceLogger.Warn($"设备 {e.DeviceId} 达到最大重连次数，进入冷却期。");
            
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
                    _statusCheckTimer = null;
                    
                    DisconnectAllDevices();
                    
                    _reconnectManager?.Dispose();

                    foreach (var device in _devicesById.Values)
                    {
                        device?.Dispose();
                    }
                    _devicesById.Clear();
                    _deviceIdByIp.Clear();
                    
                    // 清理信号量
                    foreach (var semaphore in _connectionSemaphores.Values)
                    {
                        semaphore?.Dispose();
                    }
                    _connectionSemaphores.Clear();
                    
                    _disposed = true;
                    lock (_lock)
                    {
                        _instance = null;
                    }
                }
                catch (Exception ex)
                {
                    ServiceLogger.Error("清理资源时发生异常。", ex);
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
