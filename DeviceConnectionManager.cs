using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using MySql.Data.MySqlClient;
using System.Runtime.InteropServices;

namespace ControlEntradaSalida
{
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
        public DeviceStatus Status { get; set; } = DeviceStatus.Offline; // 添加设备状态属性
        public bool IsEnabled { get; set; } = true; // 添加设备启用状态属性
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
    public class DeviceConnectionManager
    {
        private static DeviceConnectionManager _instance;
        private static readonly object _lock = new object();
        
        private List<DeviceConnectionInfo> _devices;
        private Timer _statusCheckTimer;
        private const int STATUS_CHECK_INTERVAL = 30000; // 30秒检查一次设备状态

        // 事件：设备连接状态改变时触发
        public event EventHandler<DeviceStatusChangedEventArgs> DeviceStatusChanged;

        private DeviceConnectionManager()
        {
            _devices = new List<DeviceConnectionInfo>();
            InitializeTimer();
        }

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

        // 初始化定时器
        private void InitializeTimer()
        {
            _statusCheckTimer = new Timer(STATUS_CHECK_INTERVAL);
            _statusCheckTimer.Elapsed += async (sender, e) => await CheckAllDeviceStatusAsync();
            _statusCheckTimer.AutoReset = true;
            _statusCheckTimer.Start();
        }

        // 加载所有设备信息
        public void LoadAllDevices()
        {
            _devices.Clear();
            
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
                        
                        _devices.Add(device);
                    }
                    
                    rdr.Close();
                }
                catch (Exception ex)
                {
                    // 记录错误日志
                    Console.WriteLine($"加载设备信息时出错: {ex.Message}");
                }
                finally
                {
                    bd.desconectarMySQL();
                }
            }
        }

        // 获取所有设备
        public List<DeviceConnectionInfo> GetAllDevices()
        {
            return new List<DeviceConnectionInfo>(_devices);
        }

        // 根据ID获取设备
        public DeviceConnectionInfo GetDeviceById(int id)
        {
            return _devices.FirstOrDefault(d => d.Id == id);
        }

        // 根据IP和端口获取设备
        public DeviceConnectionInfo GetDeviceByAddress(string ip, string port)
        {
            return _devices.FirstOrDefault(d => d.IpAddress == ip && d.Port == port);
        }

        // 连接指定设备
        public bool ConnectToDevice(DeviceConnectionInfo device)
        {
            if (device == null)
                return false;

            try
            {
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
                    device.UserID = lUserID;
                    device.IsConnected = true;
                    device.StatusMessage = "连接成功";
                    device.LastChecked = DateTime.Now;
                    
                    // 更新数据库中的最后使用时间
                    UpdateDeviceLastUsed(device.Id);
                    
                    // 触发状态改变事件
                    OnDeviceStatusChanged(new DeviceStatusChangedEventArgs(device, true));
                    return true;
                }
                else
                {
                    uint nErr = HCNetSDK.NET_DVR_GetLastError();
                    device.UserID = -1;
                    device.IsConnected = false;
                    device.StatusMessage = $"连接失败，错误代码: {nErr}";
                    device.LastChecked = DateTime.Now;
                    
                    // 触发状态改变事件
                    OnDeviceStatusChanged(new DeviceStatusChangedEventArgs(device, false));
                    return false;
                }
            }
            catch (Exception ex)
            {
                device.UserID = -1;
                device.IsConnected = false;
                device.StatusMessage = $"连接异常: {ex.Message}";
                device.LastChecked = DateTime.Now;
                
                // 触发状态改变事件
                OnDeviceStatusChanged(new DeviceStatusChangedEventArgs(device, false));
                return false;
            }
        }

        // 断开设备连接
        public void DisconnectDevice(DeviceConnectionInfo device)
        {
            if (device != null && device.UserID >= 0)
            {
                HCNetSDK.NET_DVR_Logout_V30(device.UserID);
                device.UserID = -1;
                device.IsConnected = false;
                device.StatusMessage = "已断开连接";
                device.LastChecked = DateTime.Now;
                
                // 触发状态改变事件
                OnDeviceStatusChanged(new DeviceStatusChangedEventArgs(device, false));
            }
        }

        // 断开所有设备连接
        public void DisconnectAllDevices()
        {
            foreach (var device in _devices)
            {
                if (device.UserID >= 0)
                {
                    HCNetSDK.NET_DVR_Logout_V30(device.UserID);
                    device.UserID = -1;
                    device.IsConnected = false;
                    device.StatusMessage = "已断开连接";
                }
            }
        }

        // 检查单个设备状态
        public bool CheckDeviceStatus(DeviceConnectionInfo device)
        {
            if (device == null)
                return false;

            try
            {
                // 如果设备未连接，尝试连接
                if (device.UserID < 0)
                {
                    return ConnectToDevice(device);
                }
                
                // 使用设备能力查询来检测连接状态
                uint dwSize = 1024 * 10;
                IntPtr ptrOutBuf = Marshal.AllocHGlobal((int)dwSize);
                bool bRet = HCNetSDK.NET_DVR_GetDeviceAbility(device.UserID, HCNetSDK.ACS_ABILITY, IntPtr.Zero, 0, ptrOutBuf, dwSize);
                Marshal.FreeHGlobal(ptrOutBuf);
                
                bool previousStatus = device.IsConnected;
                DeviceStatus previousDeviceStatus = device.Status;
                
                if (bRet)
                {
                    device.IsConnected = true;
                    // 这里可以调用获取设备详细状态的方法
                    // 暂时使用模拟方法
                    device.Status = GetDeviceDetailedStatus(device);
                    device.StatusMessage = GetStatusMessage(device.Status);
                }
                else
                {
                    uint nErr = HCNetSDK.NET_DVR_GetLastError();
                    device.IsConnected = false;
                    device.Status = DeviceStatus.Offline;
                    device.StatusMessage = $"离线，错误代码: {nErr}";
                    
                    // 如果之前是连接状态，现在断开，则需要清理UserID
                    if (previousStatus)
                    {
                        device.UserID = -1;
                    }
                }
                
                device.LastChecked = DateTime.Now;
                
                // 如果连接状态或设备状态发生改变，触发事件
                if (previousStatus != device.IsConnected || previousDeviceStatus != device.Status)
                {
                    OnDeviceStatusChanged(new DeviceStatusChangedEventArgs(device, device.IsConnected));
                }
                
                return bRet;
            }
            catch (Exception ex)
            {
                bool previousStatus = device.IsConnected;
                DeviceStatus previousDeviceStatus = device.Status;
                
                device.IsConnected = false;
                device.Status = DeviceStatus.Offline;
                device.StatusMessage = $"状态检查异常: {ex.Message}";
                device.LastChecked = DateTime.Now;
                device.UserID = -1; // 出现异常时重置连接ID
                
                // 如果状态发生改变，触发事件
                if (previousStatus != device.IsConnected || previousDeviceStatus != device.Status)
                {
                    OnDeviceStatusChanged(new DeviceStatusChangedEventArgs(device, false));
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

        // 异步检查所有设备状态
        private async Task CheckAllDeviceStatusAsync()
        {
            await Task.Run(() =>
            {
                // 使用并行处理提高检查速度
                Parallel.ForEach(_devices, device =>
                {
                    CheckDeviceStatus(device);
                });
            });
        }

        // 更新设备最后使用时间
        private void UpdateDeviceLastUsed(int deviceId)
        {
            try
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
            }
            catch (Exception ex)
            {
                // 记录错误但不中断流程
                Console.WriteLine($"更新设备最后使用时间时出错: {ex.Message}");
            }
        }

        // 触发设备状态改变事件
        protected virtual void OnDeviceStatusChanged(DeviceStatusChangedEventArgs e)
        {
            DeviceStatusChanged?.Invoke(this, e);
        }

        // 清理资源
        public void Dispose()
        {
            _statusCheckTimer?.Stop();
            _statusCheckTimer?.Dispose();
            DisconnectAllDevices();
        }
    }

    // 设备状态改变事件参数
    public class DeviceStatusChangedEventArgs : EventArgs
    {
        public DeviceConnectionInfo Device { get; }
        public bool IsConnected { get; }

        public DeviceStatusChangedEventArgs(DeviceConnectionInfo device, bool isConnected)
        {
            Device = device;
            IsConnected = isConnected;
        }
    }
}