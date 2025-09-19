using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;

namespace ControlEntradaSalida
{
    public sealed class AccessEventService : IDisposable
    {
        private static readonly Lazy<AccessEventService> lazyInstance = new Lazy<AccessEventService>(() => new AccessEventService());

        public static AccessEventService Instance => lazyInstance.Value;

        private readonly object componentLock = new object();
        private readonly object alarmLock = new object();
        private readonly Dictionary<int, int> alarmHandles = new Dictionary<int, int>();

        private AsyncEventQueue eventQueue;
        private EventDeduplicator eventDeduplicator;
        private AsyncDatabaseWriter asyncDatabaseWriter;
        private HCNetSDK.MSGCallBack alarmCallback;

        private bool componentsInitialized;
        private bool alarmCallbackRegistered;
        private bool writerRunning;
        private bool disposed;
        private long sequenceNumber;

        public event EventHandler<AccessEventReceivedEventArgs> AccessEventReceived;

        private AccessEventService()
        {
            DeviceConnectionManager.Instance.DeviceStatusChanged += OnDeviceStatusChanged;
        }

        public async Task EnsureStartedAsync()
        {
            if (disposed)
            {
                throw new ObjectDisposedException(nameof(AccessEventService));
            }

            EnsureComponentsInitialized();
            await StartDatabaseWriterAsync();
            DeployAlarms();
        }

        public void DeployAlarms()
        {
            if (disposed)
            {
                return;
            }

            lock (alarmLock)
            {
                if (!alarmCallbackRegistered)
                {
                    alarmCallback = new HCNetSDK.MSGCallBack(MsgCallback);
                    if (!HCNetSDK.NET_DVR_SetDVRMessageCallBack_V50(0, alarmCallback, IntPtr.Zero))
                    {
                        uint errorCode = HCNetSDK.NET_DVR_GetLastError();
                        Console.WriteLine($"[ERROR] 注册门禁事件回调失败，错误码: {errorCode}");
                        alarmCallback = null;
                        return;
                    }

                    alarmCallbackRegistered = true;
                }

                List<DeviceConnectionInfo> connectedDevices = DeviceConnectionManager.Instance.GetAllDevices()
                    .Where(d => d.IsConnected && d.UserID >= 0)
                    .ToList();

                if (connectedDevices.Count == 0)
                {
                    foreach (int handle in alarmHandles.Values.ToList())
                    {
                        HCNetSDK.NET_DVR_CloseAlarmChan_V30(handle);
                    }

                    alarmHandles.Clear();
                    return;
                }

                HashSet<int> activeDeviceIds = new HashSet<int>(connectedDevices.Select(d => d.Id));

                foreach (DeviceConnectionInfo device in connectedDevices)
                {
                    if (alarmHandles.ContainsKey(device.Id))
                    {
                        continue;
                    }

                    HCNetSDK.NET_DVR_SETUPALARM_PARAM setupParam = new HCNetSDK.NET_DVR_SETUPALARM_PARAM();
                    setupParam.dwSize = (uint)Marshal.SizeOf(setupParam);
                    setupParam.byLevel = 1;
                    setupParam.byAlarmInfoType = 1;
                    setupParam.byDeployType = 1;

                    int alarmHandle = HCNetSDK.NET_DVR_SetupAlarmChan_V41(device.UserID, ref setupParam);
                    if (alarmHandle < 0)
                    {
                        uint errorCode = HCNetSDK.NET_DVR_GetLastError();
                        Console.WriteLine($"[ERROR] 设备 {device.Name} 布防失败，错误码: {errorCode}");
                    }
                    else
                    {
                        alarmHandles[device.Id] = alarmHandle;
                    }
                }

                List<int> staleDeviceIds = alarmHandles.Keys.Where(id => !activeDeviceIds.Contains(id)).ToList();
                foreach (int deviceId in staleDeviceIds)
                {
                    if (alarmHandles.TryGetValue(deviceId, out int handle))
                    {
                        HCNetSDK.NET_DVR_CloseAlarmChan_V30(handle);
                    }

                    alarmHandles.Remove(deviceId);
                }
            }
        }

        public void AlignSequenceNumber(long value)
        {
            if (value <= 0)
            {
                return;
            }

            long current;
            do
            {
                current = Interlocked.Read(ref sequenceNumber);
                if (value <= current)
                {
                    return;
                }
            }
            while (Interlocked.CompareExchange(ref sequenceNumber, value, current) != current);
        }

        public string GetStatistics()
        {
            List<string> stats = new List<string>();

            if (eventQueue != null)
            {
                stats.Add(eventQueue.GetStatistics());
            }

            if (eventDeduplicator != null)
            {
                stats.Add(eventDeduplicator.GetStatistics());
            }

            if (asyncDatabaseWriter != null)
            {
                stats.Add(asyncDatabaseWriter.GetStatistics());
            }

            return string.Join(Environment.NewLine, stats.Where(s => !string.IsNullOrWhiteSpace(s)));
        }

        public async Task StopAsync(int timeoutMs = 30000)
        {
            if (disposed)
            {
                return;
            }

            AsyncDatabaseWriter writerSnapshot;
            EventDeduplicator deduplicatorSnapshot;
            AsyncEventQueue queueSnapshot;

            lock (componentLock)
            {
                if (disposed)
                {
                    return;
                }

                disposed = true;
                writerSnapshot = asyncDatabaseWriter;
                deduplicatorSnapshot = eventDeduplicator;
                queueSnapshot = eventQueue;

                asyncDatabaseWriter = null;
                eventDeduplicator = null;
                eventQueue = null;
                writerRunning = false;
            }

            if (writerSnapshot != null)
            {
                try
                {
                    await writerSnapshot.StopAsync(timeoutMs);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[WARNING] 停止事件写库线程失败: {ex.Message}");
                }

                writerSnapshot.Dispose();
            }

            deduplicatorSnapshot?.Dispose();
            queueSnapshot?.Dispose();

            lock (alarmLock)
            {
                foreach (int handle in alarmHandles.Values.ToList())
                {
                    HCNetSDK.NET_DVR_CloseAlarmChan_V30(handle);
                }

                alarmHandles.Clear();
                alarmCallbackRegistered = false;
                alarmCallback = null;
            }

            DeviceConnectionManager.Instance.DeviceStatusChanged -= OnDeviceStatusChanged;
        }

        public void Dispose()
        {
            StopAsync().GetAwaiter().GetResult();
        }

        private void EnsureComponentsInitialized()
        {
            if (componentsInitialized)
            {
                return;
            }

            lock (componentLock)
            {
                if (componentsInitialized)
                {
                    return;
                }

                try
                {
                    Common common = new Common();
                    string connectionString = common.obtenerCadenaConexion();
                    if (string.IsNullOrWhiteSpace(connectionString))
                    {
                        Console.WriteLine("[WARNING] 未能获取数据库连接字符串，事件服务初始化被忽略");
                        return;
                    }

                    eventQueue = new AsyncEventQueue(10000);
                    eventDeduplicator = new EventDeduplicator(10000, 60, 5);

                    BatchConfiguration batchConfig = new BatchConfiguration
                    {
                        BatchSize = 50,
                        BatchTimeoutMs = 5000,
                        MinBatchSize = 1,
                        MaxBatchSize = 200
                    };

                    RetryPolicy retryPolicy = new RetryPolicy
                    {
                        MaxRetryCount = 3,
                        InitialDelayMs = 1000,
                        BackoffMultiplier = 2.0,
                        MaxDelayMs = 30000
                    };

                    asyncDatabaseWriter = new AsyncDatabaseWriter(connectionString, eventQueue, eventDeduplicator, this, batchConfig, retryPolicy);
                    componentsInitialized = true;
                    Console.WriteLine("[INIT] 门禁事件服务组件初始化完成");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[ERROR] 初始化门禁事件服务组件失败: {ex.Message}");
                }
            }
        }

        private async Task StartDatabaseWriterAsync()
        {
            AsyncDatabaseWriter writerSnapshot;

            lock (componentLock)
            {
                if (!componentsInitialized || asyncDatabaseWriter == null)
                {
                    return;
                }

                if (writerRunning)
                {
                    return;
                }

                writerSnapshot = asyncDatabaseWriter;
                writerRunning = true;
            }

            try
            {
                await writerSnapshot.StartAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] 启动事件写库线程失败: {ex.Message}");
                lock (componentLock)
                {
                    writerRunning = false;
                }

                throw;
            }
        }

        private void MsgCallback(int command, ref HCNetSDK.NET_DVR_ALARMER alarmer, IntPtr alarmInfoPtr, uint bufferLength, IntPtr userData)
        {
            if (command != HCNetSDK.COMM_ALARM_ACS)
            {
                return;
            }

            ProcessAccessAlarm(ref alarmer, alarmInfoPtr, bufferLength);
        }

        private void ProcessAccessAlarm(ref HCNetSDK.NET_DVR_ALARMER alarmer, IntPtr alarmInfoPtr, uint bufferLength)
        {
            try
            {
                AccessLogEvent accessEvent = ParseAccessEvent(ref alarmer, alarmInfoPtr, bufferLength);
                if (accessEvent == null)
                {
                    return;
                }

                EnqueueAccessEvent(accessEvent);
                NotifyEventSubscribers(accessEvent);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] 处理门禁事件回调异常: {ex.Message}");
            }
        }

        private AccessLogEvent ParseAccessEvent(ref HCNetSDK.NET_DVR_ALARMER alarmer, IntPtr alarmInfoPtr, uint bufferLength)
        {
            HCNetSDK.NET_DVR_ACS_ALARM_INFO alarmInfo = (HCNetSDK.NET_DVR_ACS_ALARM_INFO)Marshal.PtrToStructure(alarmInfoPtr, typeof(HCNetSDK.NET_DVR_ACS_ALARM_INFO));

            HCNetSDK.NET_DVR_LOG_V30 logInfo = new HCNetSDK.NET_DVR_LOG_V30
            {
                dwMajorType = alarmInfo.dwMajor,
                dwMinorType = alarmInfo.dwMinor
            };

            char[] typeBuffer = new char[256];

            if (HCNetSDK.MAJOR_ALARM == logInfo.dwMajorType)
            {
                TypeMap.AlarmMinorTypeMap(logInfo, typeBuffer);
            }
            else if (HCNetSDK.MAJOR_OPERATION == logInfo.dwMajorType)
            {
                TypeMap.OperationMinorTypeMap(logInfo, typeBuffer);
            }
            else if (HCNetSDK.MAJOR_EXCEPTION == logInfo.dwMajorType)
            {
                TypeMap.ExceptionMinorTypeMap(logInfo, typeBuffer);
            }
            else if (HCNetSDK.MAJOR_EVENT == logInfo.dwMajorType)
            {
                TypeMap.EventMinorTypeMap(logInfo, typeBuffer);
            }

            string eventTypeCode = new string(typeBuffer).TrimEnd('\0');
            if (string.IsNullOrWhiteSpace(eventTypeCode))
            {
                eventTypeCode = "UNKNOWN";
            }

            if (!AccessEventFormatter.IsSupportedEventType(eventTypeCode))
            {
                return null;
            }

            DateTime eventTime;
            try
            {
                eventTime = new DateTime(
                    (int)alarmInfo.struTime.dwYear,
                    (int)alarmInfo.struTime.dwMonth,
                    (int)alarmInfo.struTime.dwDay,
                    (int)alarmInfo.struTime.dwHour,
                    (int)alarmInfo.struTime.dwMinute,
                    (int)alarmInfo.struTime.dwSecond);
            }
            catch
            {
                eventTime = DateTime.Now;
            }

            string employeeNumber = null;
            try
            {
                if (alarmInfo.struAcsEventInfo.dwEmployeeNo != 0)
                {
                    employeeNumber = alarmInfo.struAcsEventInfo.dwEmployeeNo.ToString();
                }
            }
            catch
            {
                employeeNumber = null;
            }

            string remoteHostAddress = ResolveRemoteHostAddress(ref alarmInfo, ref alarmer, null);

            int userId = alarmer.lUserID;
            DeviceConnectionInfo device = ResolveDeviceConnection(ref alarmer, remoteHostAddress, userId);

            if (device == null)
            {
                string remoteInfo = string.IsNullOrWhiteSpace(remoteHostAddress) ? "未知" : remoteHostAddress;
                Console.WriteLine($"[WARNING] 无法匹配报警来源到已登记设备，事件被忽略：UserID={userId}, RemoteIP={remoteInfo}");
                return null;
            }

            remoteHostAddress = ResolveRemoteHostAddress(ref alarmInfo, ref alarmer, device);

            long sequence = Interlocked.Increment(ref sequenceNumber);

            bool isPersonRelated = AccessEventFormatter.IsPersonRelatedEvent(eventTypeCode);
            string employeeName = string.Empty;

            if (isPersonRelated && !string.IsNullOrWhiteSpace(employeeNumber))
            {
                employeeName = FetchEmployeeName(employeeNumber);
            }

            AccessLogEvent accessEvent = new AccessLogEvent
            {
                SequenceNumber = sequence,
                EventTime = eventTime,
                EmployeeNumber = isPersonRelated ? (employeeNumber ?? string.Empty) : string.Empty,
                EmployeeName = isPersonRelated ? employeeName : string.Empty,
                DeviceNumber = device.Id,
                DeviceName = device.Name ?? string.Empty,
                EventType = eventTypeCode,
                EventTypeDisplay = AccessEventFormatter.TranslateEventType(eventTypeCode),
                RemoteHostAddress = remoteHostAddress,
                Priority = 2,
                CreateTime = DateTime.Now
            };

            return accessEvent;
        }

        private void EnqueueAccessEvent(AccessLogEvent accessEvent)
        {
            if (eventQueue == null || eventDeduplicator == null)
            {
                return;
            }

            if (eventDeduplicator.IsEventProcessed(accessEvent))
            {
                return;
            }

            eventDeduplicator.MarkEventProcessed(accessEvent);

            if (!eventQueue.TryEnqueue(accessEvent))
            {
                Console.WriteLine($"[WARNING] 事件入队失败，队列可能已满: {accessEvent.GetDeduplicationKey()}");
            }
        }

        private void NotifyEventSubscribers(AccessLogEvent accessEvent)
        {
            EventHandler<AccessEventReceivedEventArgs> handler = AccessEventReceived;
            if (handler == null)
            {
                return;
            }

            AccessEventReceivedEventArgs args = new AccessEventReceivedEventArgs(accessEvent);
            handler.Invoke(this, args);
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

        private DeviceConnectionInfo ResolveDeviceConnection(ref HCNetSDK.NET_DVR_ALARMER alarmer, string remoteHostAddress, int userId)
        {
            List<DeviceConnectionInfo> devices = DeviceConnectionManager.Instance.GetAllDevices();

            if (userId >= 0)
            {
                DeviceConnectionInfo matchedByUser = devices.FirstOrDefault(d => d.UserID == userId);
                if (matchedByUser != null)
                {
                    return matchedByUser;
                }
            }

            if (!string.IsNullOrWhiteSpace(remoteHostAddress))
            {
                DeviceConnectionInfo matchedByIp = devices.FirstOrDefault(d =>
                    string.Equals(d.IpAddress, remoteHostAddress, StringComparison.OrdinalIgnoreCase));
                if (matchedByIp != null)
                {
                    return matchedByIp;
                }
            }

            string alarmerIp = SanitizeNativeString(alarmer.sDeviceIP);
            if (string.IsNullOrWhiteSpace(alarmerIp))
            {
                alarmerIp = SanitizeNativeString(alarmer.sSocketIP);
            }

            if (!string.IsNullOrWhiteSpace(alarmerIp))
            {
                DeviceConnectionInfo matchedByAlarmerIp = devices.FirstOrDefault(d =>
                    string.Equals(d.IpAddress, alarmerIp, StringComparison.OrdinalIgnoreCase));
                if (matchedByAlarmerIp != null)
                {
                    return matchedByAlarmerIp;
                }
            }

            string linkPort = null;
            if (alarmer.byLinkPortValid == 1 && alarmer.wLinkPort > 0)
            {
                linkPort = alarmer.wLinkPort.ToString(System.Globalization.CultureInfo.InvariantCulture);
            }

            if (!string.IsNullOrWhiteSpace(linkPort))
            {
                if (!string.IsNullOrWhiteSpace(remoteHostAddress))
                {
                    DeviceConnectionInfo matchedByRemoteAddr = devices.FirstOrDefault(d =>
                        string.Equals(d.IpAddress, remoteHostAddress, StringComparison.OrdinalIgnoreCase) &&
                        string.Equals(d.Port, linkPort, StringComparison.OrdinalIgnoreCase));
                    if (matchedByRemoteAddr != null)
                    {
                        return matchedByRemoteAddr;
                    }
                }

                if (!string.IsNullOrWhiteSpace(alarmerIp))
                {
                    DeviceConnectionInfo matchedByIpAndPort = devices.FirstOrDefault(d =>
                        string.Equals(d.IpAddress, alarmerIp, StringComparison.OrdinalIgnoreCase) &&
                        string.Equals(d.Port, linkPort, StringComparison.OrdinalIgnoreCase));
                    if (matchedByIpAndPort != null)
                    {
                        return matchedByIpAndPort;
                    }
                }
            }

            return null;
        }

        private static string SanitizeNativeString(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return null;
            }

            string sanitized = value.TrimEnd('\0').Trim();
            return sanitized.Length == 0 ? null : sanitized;
        }

        private string FetchEmployeeName(string employeeNumber)
        {
            if (string.IsNullOrWhiteSpace(employeeNumber))
            {
                return string.Empty;
            }

            try
            {
                Common common = new Common();
                string connectionString = common.obtenerCadenaConexion();
                if (string.IsNullOrWhiteSpace(connectionString))
                {
                    return string.Empty;
                }

                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    connection.Open();
                    using (MySqlCommand command = new MySqlCommand("SELECT full_name FROM employees WHERE employee_id = @employee_id LIMIT 1", connection))
                    {
                        command.Parameters.AddWithValue("@employee_id", employeeNumber);
                        object result = command.ExecuteScalar();
                        return result == null ? string.Empty : result.ToString();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[WARNING] 查询员工姓名失败(工号: {employeeNumber}): {ex.Message}");
                return string.Empty;
            }
        }

        private void OnDeviceStatusChanged(object sender, DeviceStatusChangedEventArgs e)
        {
            try
            {
                DeployAlarms();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[WARNING] 设备状态变化时重新布防失败: {ex.Message}");
            }
        }
    }

    public class AccessEventReceivedEventArgs : EventArgs
    {
        public AccessEventReceivedEventArgs(AccessLogEvent accessEvent)
        {
            AccessEvent = accessEvent ?? throw new ArgumentNullException(nameof(accessEvent));
        }

        public AccessLogEvent AccessEvent { get; }
    }

    public static class AccessEventFormatter
    {
        public static bool IsSupportedEventType(string eventTypeCode)
        {
            switch (eventTypeCode)
            {
                case "MINOR_FACE_VERIFY_PASS":
                case "MINOR_FACE_VERIFY_FAIL":
                case "MINOR_LOCK_OPEN":
                case "MINOR_LOCK_CLOSE":
                case "MINOR_DOOR_OPEN_NORMAL":
                case "MINOR_DOOR_CLOSE_NORMAL":
                case "MINOR_DOOR_OPEN_ABNORMAL":
                case "MINOR_DOOR_OPEN_TIMEOUT":
                case "MINOR_DOOR_BUTTON_PRESS":
                case "MINOR_DOOR_BUTTON_RELEASE":
                case "MINOR_REMOTE_OPEN_DOOR":
                case "MINOR_REMOTE_CLOSE_DOOR":
                case "MINOR_ALWAYS_OPEN_BEGIN":
                case "MINOR_ALWAYS_OPEN_END":
                case "MINOR_ALWAYS_CLOSE_BEGIN":
                case "MINOR_ALWAYS_CLOSE_END":
                    return true;
                default:
                    return false;
            }
        }

        public static bool IsPersonRelatedEvent(string eventTypeCode)
        {
            switch (eventTypeCode)
            {
                case "MINOR_FACE_VERIFY_PASS":
                case "MINOR_FACE_VERIFY_FAIL":
                    return true;
                default:
                    return false;
            }
        }

        public static string TranslateEventType(string eventTypeCode)
        {
            if (string.IsNullOrWhiteSpace(eventTypeCode))
            {
                return "未知事件";
            }

            switch (eventTypeCode)
            {
                case "MINOR_FACE_VERIFY_PASS":
                    return "人脸验证通过";
                case "MINOR_FACE_VERIFY_FAIL":
                    return "人脸验证失败";
                case "MINOR_LOCK_OPEN":
                    return "门锁打开";
                case "MINOR_LOCK_CLOSE":
                    return "门锁关闭";
                case "MINOR_DOOR_OPEN_NORMAL":
                    return "门正常打开";
                case "MINOR_DOOR_CLOSE_NORMAL":
                    return "门正常关闭";
                case "MINOR_DOOR_OPEN_ABNORMAL":
                    return "门异常打开";
                case "MINOR_DOOR_OPEN_TIMEOUT":
                    return "门打开超时";
                case "MINOR_DOOR_BUTTON_PRESS":
                    return "门按钮按下";
                case "MINOR_DOOR_BUTTON_RELEASE":
                    return "门按钮释放";
                case "MINOR_REMOTE_OPEN_DOOR":
                    return "远程开门";
                case "MINOR_REMOTE_CLOSE_DOOR":
                    return "远程关门";
                case "MINOR_ALWAYS_OPEN_BEGIN":
                    return "常开模式开始";
                case "MINOR_ALWAYS_OPEN_END":
                    return "常开模式结束";
                case "MINOR_ALWAYS_CLOSE_BEGIN":
                    return "常闭模式开始";
                case "MINOR_ALWAYS_CLOSE_END":
                    return "常闭模式结束";
                default:
                    return eventTypeCode;
            }
        }
    }
}



