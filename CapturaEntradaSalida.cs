using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ControlEntradaSalida
{
    //实时接收、显示和记录出入事件 - 异步优化版本
    //严格按照海康威视SDK编程指南实现轻量级事件处理机制
    public partial class CapturaEntradaSalida : Form
    {
        private HCNetSDK.MSGCallBack m_falarmData = null;
        private long m_lLogNum = 0;
        private Dictionary<int, int> lAlarmHandles = new Dictionary<int, int>(); // 设备ID到报警句柄的映射
        private readonly object _alarmSyncRoot = new object(); // 控制布防/撤防临界区，避免线程竞态
        private bool _alarmCallbackRegistered = false; // 标记报警回调是否已经注册
        private bool _hasShownNoDeviceMessage = false; // 控制无设备提示只弹一次

        // 异步数据库处理组件 - 核心异步处理机制

        private AsyncEventQueue _eventQueue;
        private EventDeduplicator _eventDeduplicator;
        private AsyncDatabaseWriter _asyncDatabaseWriter;
        private readonly object _asyncComponentsLock = new object();
        private bool _asyncComponentsInitialized = false;

        //初始化窗体和异步组件
        public CapturaEntradaSalida()
        {
            InitializeComponent();
            InitializeAsyncComponents();
        }

        /// <summary>
        /// 初始化异步数据库处理组件
        /// 严格遵循海康威视SDK编程指南，实现轻量级事件处理
        /// </summary>
        private void InitializeAsyncComponents()
        {
            try
            {
                // 获取数据库连接字符串
                Common cmn = new Common();
                string connectionString = cmn.obtenerCadenaConexion();
                
                // 初始化线程安全的事件队列（容量10000条）
                _eventQueue = new AsyncEventQueue(maxCapacity: 10000);
                
                // 初始化事件去重器（缓存时间60分钟）
                _eventDeduplicator = new EventDeduplicator(
                    maxCacheSize: 10000, 
                    cacheExpiryMinutes: 60, 
                    cleanupIntervalMinutes: 5);
                
                // 配置批处理参数（按照设计文档推荐值）
                var batchConfig = new BatchConfiguration
                {
                    BatchSize = 50,        // 批处理大小
                    BatchTimeoutMs = 5000, // 批处理超时
                    MinBatchSize = 1,      // 最小批大小
                    MaxBatchSize = 200     // 最大批大小
                };
                
                // 配置重试策略（指数退避）
                var retryPolicy = new RetryPolicy
                {
                    MaxRetryCount = 3,      // 最大重试3次
                    InitialDelayMs = 1000,  // 初始延时1秒
                    BackoffMultiplier = 2.0, // 指数退避因子
                    MaxDelayMs = 30000      // 最大延时30秒
                };
                
                // 初始化异步数据库写入器
                _asyncDatabaseWriter = new AsyncDatabaseWriter(
                    connectionString, 
                    _eventQueue, 
                    _eventDeduplicator,
                    batchConfig, 
                    retryPolicy);
                    
                _asyncComponentsInitialized = true;
                Console.WriteLine("[INIT] 异步数据库处理组件初始化成功");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"初始化异步处理组件失败: {ex.Message}", "初始化错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Console.WriteLine($"[ERROR] 异步组件初始化失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 异步入队门禁事件（轻量级快速处理）
        /// 替代原有的同步 InsertAccessLog 方法
        /// 严格按照SDK编程指南：回调函数只做数据解析 + 快速入队
        /// </summary>
        /// <param name="logNumber">事件编号</param>
        /// <param name="eventTime">事件时间</param>
        /// <param name="employeeId">员工ID（卡号）</param>
        /// <param name="eventType">事件类型</param>
        /// <param name="deviceId">设备ID</param>
        /// <param name="employeeName">员工姓名</param>
        /// <returns>是否成功入队</returns>
        private bool EnqueueAccessLogAsync(EventDataQuick eventData)
        {
            if (eventData == null)
            {
                return false;
            }

            // 检查异步组件是否已初始化
            if (!_asyncComponentsInitialized || _eventQueue == null || _eventDeduplicator == null)
            {
                Console.WriteLine("[WARNING] 异步组件未初始化，事件将被丢弃");
                return false;
            }

            try
            {
                var accessEvent = new AccessLogEvent
                {
                    SequenceNumber = eventData.SequenceNumber,
                    EventTime = eventData.EventTime,
                    EmployeeNumber = eventData.EmployeeNumber ?? string.Empty,
                    EmployeeName = eventData.EmployeeName,
                    DeviceNumber = eventData.DeviceNumber,
                    DeviceName = eventData.DeviceName,
                    EventType = eventData.EventTypeCode,
                    EventTypeDisplay = eventData.EventTypeDisplay,
                    RemoteHostAddress = eventData.RemoteHostAddress,
                    Priority = 2,
                    CreateTime = DateTime.Now
                };

                if (_eventDeduplicator.IsEventProcessed(accessEvent))
                {
                    return true;
                }

                _eventDeduplicator.MarkEventProcessed(accessEvent);

                bool enqueued = _eventQueue.TryEnqueue(accessEvent);

                if (!enqueued)
                {
                    Console.WriteLine($"[WARNING] 事件入队失败，队列可能已满: {accessEvent.GetDeduplicationKey()}");
                }

                return enqueued;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] 事件入队异常: {ex.Message}");
                return false;
            }
        }

        //登录设备,设置报警监听参数,实现与设备建立"监听"连接
        private void Deploy()
        {
            lock (_alarmSyncRoot)
            {
                // 仅为已经在线的设备维持布防状态，避免重复撤防
                var connectedDevices = DeviceConnectionManager.Instance.GetAllDevices()
                    .Where(d => d.IsConnected && d.UserID >= 0).ToList();

                if (connectedDevices.Count == 0)
                {
                    if (lAlarmHandles.Count > 0)
                    {
                        foreach (var handle in lAlarmHandles.Values.ToList())
                        {
                            HCNetSDK.NET_DVR_CloseAlarmChan_V30(handle);
                        }
                        lAlarmHandles.Clear();
                    }
                    if (!_hasShownNoDeviceMessage)
                    {
                        _hasShownNoDeviceMessage = true;
                        MessageBox.Show("暂无在线设备", "设备状态", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    return;
                }

                _hasShownNoDeviceMessage = false;

                if (!_alarmCallbackRegistered)
                {
                    m_falarmData = new HCNetSDK.MSGCallBack(MsgCallback); // 注册报警回调，避免重复注册
                    if (!HCNetSDK.NET_DVR_SetDVRMessageCallBack_V50(0, m_falarmData, IntPtr.Zero))
                    {
                        MessageBox.Show("NET_DVR_SetDVRMessageCallBack_V50 fail", "operation fail", MessageBoxButtons.OK);
                        return;
                    }
                    _alarmCallbackRegistered = true;
                }
                var activeDeviceIds = new HashSet<int>(connectedDevices.Select(d => d.Id));
                foreach (var device in connectedDevices)
                {
                    if (lAlarmHandles.ContainsKey(device.Id))
                    {
                        continue; // 设备已经布防，无需重复
                    }
                    HCNetSDK.NET_DVR_SETUPALARM_PARAM struSetupAlarmParam = new HCNetSDK.NET_DVR_SETUPALARM_PARAM();
                    struSetupAlarmParam.dwSize = (uint)Marshal.SizeOf(struSetupAlarmParam);
                    struSetupAlarmParam.byLevel = 1;
                    struSetupAlarmParam.byAlarmInfoType = 1;
                    struSetupAlarmParam.byDeployType = (byte)1;

                    int alarmHandle = HCNetSDK.NET_DVR_SetupAlarmChan_V41(device.UserID, ref struSetupAlarmParam);
                    if (alarmHandle < 0)
                    {
                        MessageBox.Show($"设备 {device.Name} 布防失败，错误码: " + HCNetSDK.NET_DVR_GetLastError(), "Setup alarm chan failed");
                    }
                    else
                    {
                        lAlarmHandles[device.Id] = alarmHandle;
                    }
                }
                var staleDeviceIds = lAlarmHandles.Keys.Where(id => !activeDeviceIds.Contains(id)).ToList();
                foreach (var deviceId in staleDeviceIds)
                {
                    if (lAlarmHandles.TryGetValue(deviceId, out var handle))
                    {
                        HCNetSDK.NET_DVR_CloseAlarmChan_V30(handle);
                    }
                    lAlarmHandles.Remove(deviceId);
                }
            }
        }

        // 断开所有设备的报警通道
        private void UnDeploy()
        {
            lock (_alarmSyncRoot)
            {
                foreach (var handle in lAlarmHandles.Values.ToList())
                {
                    HCNetSDK.NET_DVR_CloseAlarmChan_V30(handle);
                }
                lAlarmHandles.Clear();
                _alarmCallbackRegistered = false;
                _hasShownNoDeviceMessage = false;
            }
        }

        private void MsgCallback(int lCommand, ref HCNetSDK.NET_DVR_ALARMER pAlarmer, IntPtr pAlarmInfo, uint dwBufLen, IntPtr pUser)
        {
            switch (lCommand)
            {
                case HCNetSDK.COMM_ALARM_ACS://只处理门禁事件
                    ProcessCommAlarmACS(ref pAlarmer, pAlarmInfo, dwBufLen, pUser);//分发到 ProcessCommAlarmACS()
                    break;
                default:
                    break;
            }
        }
        /// <summary>
        /// 处理具体门禁事件信息 - 异步优化版本
        /// 严格按照海康威视SDK编程指南：
        /// 1. 回调函数必须是轻量级的，不能执行耗时操作
        /// 2. 禁止在回调中阻塞，不能执行数据库操作、网络请求等
        /// 3. 线程安全限制，回调函数可能在SDK内部线程池中执行
        /// 4. 错误处理要求，回调函数中的异常不能抛出到SDK层
        /// </summary>
        private void ProcessCommAlarmACS(ref HCNetSDK.NET_DVR_ALARMER pAlarmer, IntPtr pAlarmInfo, uint dwBufLen, IntPtr pUser)
        {
            try
            {
                // 步骤 1：解析事件数据，保持回调轻量化
                var eventData = ParseEventDataFast(ref pAlarmer, pAlarmInfo, dwBufLen);

                if (eventData == null)
                {
                    return; // 解析失败或非目标事件，直接返回
                }

                // 步骤 2：异步入队等待数据库写入
                EnqueueAccessLogAsync(eventData);

                // 步骤 3：实时刷新 UI 展示
                UpdateUIImmediately(eventData);

                // SDK 回调需尽可能快速返回，整段处理保持毫秒级
            }
            catch (Exception ex)
            {
                // 捕获所有异常，避免影响 SDK 内部线程
                Console.WriteLine($"[ERROR] ProcessCommAlarmACS 异常: {ex.Message}");
            }
        }
        
        
        /// <summary>
        /// 快速解析事件数据（轻量级操作，不进行数据库查询）
        /// </summary>
        private EventDataQuick ParseEventDataFast(ref HCNetSDK.NET_DVR_ALARMER pAlarmer, IntPtr pAlarmInfo, uint dwBufLen)
        {
            try
            {
                HCNetSDK.NET_DVR_ACS_ALARM_INFO struAcsAlarmInfo =
                    (HCNetSDK.NET_DVR_ACS_ALARM_INFO)Marshal.PtrToStructure(pAlarmInfo, typeof(HCNetSDK.NET_DVR_ACS_ALARM_INFO));

                HCNetSDK.NET_DVR_LOG_V30 struFileInfo = new HCNetSDK.NET_DVR_LOG_V30
                {
                    dwMajorType = struAcsAlarmInfo.dwMajor,
                    dwMinorType = struAcsAlarmInfo.dwMinor
                };

                char[] csTmp = new char[256];

                if (HCNetSDK.MAJOR_ALARM == struFileInfo.dwMajorType)
                    TypeMap.AlarmMinorTypeMap(struFileInfo, csTmp);
                else if (HCNetSDK.MAJOR_OPERATION == struFileInfo.dwMajorType)
                    TypeMap.OperationMinorTypeMap(struFileInfo, csTmp);
                else if (HCNetSDK.MAJOR_EXCEPTION == struFileInfo.dwMajorType)
                    TypeMap.ExceptionMinorTypeMap(struFileInfo, csTmp);
                else if (HCNetSDK.MAJOR_EVENT == struFileInfo.dwMajorType)
                    TypeMap.EventMinorTypeMap(struFileInfo, csTmp);

                string eventTypeCode = new string(csTmp).TrimEnd('\0');
                if (string.IsNullOrWhiteSpace(eventTypeCode))
                {
                    eventTypeCode = "UNKNOWN";
                }

                if (eventTypeCode != "MINOR_FACE_VERIFY_PASS")
                {
                    return null;
                }

                var eventTime = new DateTime(
                    (int)struAcsAlarmInfo.struTime.dwYear,
                    (int)struAcsAlarmInfo.struTime.dwMonth,
                    (int)struAcsAlarmInfo.struTime.dwDay,
                    (int)struAcsAlarmInfo.struTime.dwHour,
                    (int)struAcsAlarmInfo.struTime.dwMinute,
                    (int)struAcsAlarmInfo.struTime.dwSecond);

                string employeeNumber = null;
                try
                {
                    if (struAcsAlarmInfo.struAcsEventInfo.dwEmployeeNo != 0)
                    {
                        employeeNumber = struAcsAlarmInfo.struAcsEventInfo.dwEmployeeNo.ToString();
                    }
                }
                catch
                {
                    // 结构体兼容保护
                }

                int userId = pAlarmer.lUserID;
                var device = DeviceConnectionManager.Instance.GetAllDevices().FirstOrDefault(d => d.UserID == userId);
                int deviceId = device?.Id ?? 0;
                string deviceName = device?.Name ?? string.Empty;

                string remoteHostAddress = ResolveRemoteHostAddress(ref struAcsAlarmInfo, ref pAlarmer, device);

                long sequenceNumber = System.Threading.Interlocked.Increment(ref m_lLogNum);

                return new EventDataQuick
                {
                    SequenceNumber = sequenceNumber,
                    EmployeeNumber = employeeNumber ?? string.Empty,
                    EmployeeName = null,
                    DeviceNumber = deviceId,
                    DeviceName = deviceName,
                    EventTypeCode = eventTypeCode,
                    EventTypeDisplay = TranslateEventType(eventTypeCode),
                    EventTime = eventTime,
                    RemoteHostAddress = remoteHostAddress
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] 解析事件数据异常: {ex.Message}");
                return null;
            }
        }

        private string ResolveRemoteHostAddress(ref HCNetSDK.NET_DVR_ACS_ALARM_INFO alarmInfo, ref HCNetSDK.NET_DVR_ALARMER alarmer, DeviceConnectionInfo device)
        {
            string remoteHost = alarmInfo.struRemoteHostAddr.sIpV4;

            if (string.IsNullOrWhiteSpace(remoteHost))
            {
                remoteHost = TryParseIpString(alarmInfo.struRemoteHostAddr.byIPv6);
            }

            if (string.IsNullOrWhiteSpace(remoteHost))
            {
                remoteHost = alarmer.sDeviceIP;
            }

            if (string.IsNullOrWhiteSpace(remoteHost) && device != null)
            {
                remoteHost = device.IpAddress;
            }

            return string.IsNullOrWhiteSpace(remoteHost) ? string.Empty : remoteHost;
        }

        private string TryParseIpString(byte[] rawBytes)
        {
            if (rawBytes == null || rawBytes.Length == 0)
            {
                return null;
            }

            string candidate = Encoding.ASCII.GetString(rawBytes).Trim('\0');
            return string.IsNullOrWhiteSpace(candidate) ? null : candidate;
        }

        private string TranslateEventType(string eventTypeCode)
        {
            if (string.IsNullOrWhiteSpace(eventTypeCode))
            {
                return "\u672a\u77e5\u4e8b\u4ef6";
            }

            switch (eventTypeCode)
            {
                case "MINOR_FACE_VERIFY_PASS":
                    return "\u4eba\u8138\u9a8c\u8bc1\u901a\u8fc7";
                case "MINOR_FACE_VERIFY_FAIL":
                    return "\u4eba\u8138\u9a8c\u8bc1\u5931\u8d25";
                case "MINOR_REMOTE_OPEN_DOOR":
                    return "\u8fdc\u7a0b\u5f00\u95e8";
                case "MINOR_REMOTE_CLOSE_DOOR":
                    return "\u8fdc\u7a0b\u5173\u95e8";
                case "MINOR_REMOTE_ALWAYS_OPEN":
                    return "\u8fdc\u7a0b\u5e38\u5f00";
                case "MINOR_REMOTE_ALWAYS_CLOSE":
                    return "\u8fdc\u7a0b\u5e38\u95ed";
                default:
                    return eventTypeCode;
            }
        }

        /// <summary>
        /// 即时更新UI显示（线程安全）
        /// </summary>
        private void UpdateUIImmediately(EventDataQuick eventData)
        {
            try
            {
                SafeUIUpdater.UpdateUI(this.listViewEventos, () =>
                {
                    string employeeName = string.IsNullOrWhiteSpace(eventData.EmployeeName)
                        ? "\u67e5\u8be2\u4e2d..."
                        : eventData.EmployeeName;

                    string eventTypeDisplay = string.IsNullOrWhiteSpace(eventData.EventTypeDisplay)
                        ? TranslateEventType(eventData.EventTypeCode)
                        : eventData.EventTypeDisplay;

                    ListViewItem item = new ListViewItem(eventData.SequenceNumber.ToString());
                    item.SubItems.Add(eventData.EmployeeNumber ?? string.Empty);
                    item.SubItems.Add(employeeName);
                    item.SubItems.Add(eventData.DeviceNumber.ToString());
                    item.SubItems.Add(eventData.DeviceName ?? string.Empty);
                    item.SubItems.Add(eventTypeDisplay);
                    item.SubItems.Add(eventData.EventTime.ToString("yyyy-MM-dd HH:mm:ss"));
                    item.SubItems.Add(eventData.RemoteHostAddress ?? string.Empty);

                    this.listViewEventos.Items.Add(item);

                    if (this.listViewEventos.Items.Count > 0)
                    {
                        this.listViewEventos.EnsureVisible(this.listViewEventos.Items.Count - 1);
                    }
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] UI 更新异常: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 轻量级事件数据结构（用于快速解析）
        /// </summary>
        private class EventDataQuick
        {
            public long SequenceNumber { get; set; }
            public string EmployeeNumber { get; set; }
            public string EmployeeName { get; set; }
            public int DeviceNumber { get; set; }
            public string DeviceName { get; set; }
            public string EventTypeCode { get; set; }
            public string EventTypeDisplay { get; set; }
            public DateTime EventTime { get; set; }
            public string RemoteHostAddress { get; set; }
        }
        //根据卡号（文档号）从 employees 表中查找员工姓名；用于显示在事件列表中。
        private string GetEmployeeName(string employeeId)
        {
            string retval = null;

           
            Common cmn = new Common();
            string connstr = cmn.obtenerCadenaConexion();
            BaseDatosMySQL bd = new BaseDatosMySQL();
            bd.conectarMySQL(connstr);

            if (bd.conn != null)
            {
                string sql = "SELECT * FROM employees WHERE employee_id = @employee_id";
                try
                {
                    MySqlCommand cmd = new MySqlCommand(sql, bd.conn);
                    cmd.Parameters.AddWithValue("@employee_id", employeeId);                    
                    MySqlDataReader rdr = cmd.ExecuteReader();
                    if (rdr.HasRows)
                    {
                        
                        while (rdr.Read())
                        {
                            retval = rdr["first_name"].ToString() + " " + rdr["last_name"].ToString();
                        }
                    }
                    rdr.Close();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
            }
            else
            {
                MessageBox.Show(bd.errormsg);
            }

            return retval;
        }
        //窗体加载时调用 Deploy() 开始监听，并启动异步数据库写入器
        private async void GestionEventos_Load(object sender, EventArgs e)
        {
            try
            {
                // 订阅设备状态变化事件
                DeviceConnectionManager.Instance.DeviceStatusChanged += OnDeviceStatusChanged;
                
                // 启动异步数据库写入器
                await StartAsyncDatabaseWriter();
                
                Deploy();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"窗体初始化异常: {ex.Message}", "初始化错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        
        /// <summary>
        /// 启动异步数据库写入器
        /// </summary>
        private async Task StartAsyncDatabaseWriter()
        {
            if (!_asyncComponentsInitialized || _asyncDatabaseWriter == null)
            {
                Console.WriteLine("[WARNING] 异步组件未初始化，无法启动数据库写入器");
                return;
            }
            
            try
            {
                await _asyncDatabaseWriter.StartAsync();
                Console.WriteLine("[SUCCESS] 异步数据库写入器启动成功");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] 启动异步数据库写入器失败: {ex.Message}");
                throw;
            }
        }
        
        /// <summary>
        /// 获取异步处理统计信息
        /// </summary>
        public string GetAsyncProcessingStats()
        {
            if (!_asyncComponentsInitialized)
            {
                return "异步组件未初始化";
            }
            
            var stats = new List<string>();
            
            if (_eventQueue != null)
                stats.Add(_eventQueue.GetStatistics());
                
            if (_eventDeduplicator != null)
                stats.Add(_eventDeduplicator.GetStatistics());
                
            if (_asyncDatabaseWriter != null)
                stats.Add(_asyncDatabaseWriter.GetStatistics());
            
            return string.Join("\n", stats);
        }
        
        //设备状态变化事件处理
        private void OnDeviceStatusChanged(object sender, DeviceStatusChangedEventArgs e)
        {
            // 当设备状态发生变化时，重新部署监听
            this.Invoke(new Action(() =>
            {
                Deploy();
            }));
        }
        
        //窗体关闭前关闭报警通道 NET_DVR_CloseAlarmChan() 并停止异步处理器
        private async void GestionEventos_FormClosing(object sender, FormClosingEventArgs e)
        {
            try
            {
                // 取消订阅设备状态变化事件
                DeviceConnectionManager.Instance.DeviceStatusChanged -= OnDeviceStatusChanged;
                
                // 停止异步数据库处理器
                await StopAsyncDatabaseWriter();
                
                UnDeploy();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] 窗体关闭异常: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 停止异步数据库写入器
        /// </summary>
        private async Task StopAsyncDatabaseWriter()
        {
            if (_asyncDatabaseWriter != null)
            {
                try
                {
                    await _asyncDatabaseWriter.StopAsync();
                    Console.WriteLine("[SUCCESS] 异步数据库写入器已停止");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[ERROR] 停止异步数据库写入器失败: {ex.Message}");
                }
            }
            
            // 释放资源
            _asyncDatabaseWriter?.Dispose();
            _eventDeduplicator?.Dispose();
            _eventQueue?.Dispose();
        }
        
        //
        private void listViewEventos_SelectedIndexChanged(object sender, EventArgs e)
        {

        }
    }
        
    
}

