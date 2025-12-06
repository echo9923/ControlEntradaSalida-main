using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ControlEntradaSalida.Configuration;

namespace ControlEntradaSalida
{
    /// <summary>
    /// 人脸认证事件订阅、落库与补偿服务。
    /// </summary>
    public sealed class FaceEventService : IDisposable
    {
        private readonly ServiceConfiguration.FaceEventOptions options;
        private readonly string connectionString;
        private readonly int commandTimeoutSeconds;
        private static readonly DateTime UnixEpoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        private readonly BlockingCollection<FaceEventRecord> eventQueue;
        private readonly CancellationTokenSource cancellation = new CancellationTokenSource();
        private readonly List<Task> workers = new List<Task>();
        private readonly ConcurrentDictionary<int, int> remoteConfigHandles = new ConcurrentDictionary<int, int>();

        private HCNetSDK.MSGCallBack alarmCallback;
        private bool callbackRegistered;
        private bool disposed;

        public FaceEventService(ServiceConfiguration configuration)
        {
            if (configuration == null)
            {
                throw new ArgumentNullException(nameof(configuration));
            }

            options = configuration.FaceEvent ?? new ServiceConfiguration.FaceEventOptions
            {
                Enabled = false,
                QueueCapacity = 2000,
                BatchSize = 20,
                RetryIntervalSeconds = 5,
                CompensationLookbackMinutes = 60
            };

            eventQueue = new BlockingCollection<FaceEventRecord>(Math.Max(100, options.QueueCapacity));

            var dbSection = ExternalConfiguration.Current.Database;
            connectionString = dbSection.ConnectionString?.Trim() ?? string.Empty;
            commandTimeoutSeconds = dbSection.CommandTimeoutSeconds.HasValue && dbSection.CommandTimeoutSeconds.Value > 0
                ? dbSection.CommandTimeoutSeconds.Value
                : 30;
        }

        public void Start()
        {
            if (!options.Enabled)
            {
                ServiceLogger.Info("人脸事件入库功能未启用，跳过事件订阅。");
                return;
            }

            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new InvalidOperationException("未提供数据库连接字符串，无法启动人脸事件入库功能。");
            }

            // 注册报警回调，SDK 要求回调委托在托管侧保持存活
            alarmCallback = AlarmMessageCallback;
            callbackRegistered = HCNetSDK.NET_DVR_SetDVRMessageCallBack_V50(0, alarmCallback, IntPtr.Zero);
            if (!callbackRegistered)
            {
                uint err = HCNetSDK.NET_DVR_GetLastError();
                throw new InvalidOperationException($"注册报警回调失败，错误码: {err}");
            }

            DeviceConnectionManager.Instance.DeviceConnectionStateChanged += OnDeviceConnectionStateChanged;

            // 启动后台消费者
            workers.Add(Task.Run(() => ProcessQueue(cancellation.Token), cancellation.Token));

            // 已连接设备立即补充订阅与补偿
            foreach (var device in DeviceConnectionManager.Instance.GetAllDevices().Where(d => d.IsConnected))
            {
                SetupAlarm(device);
                Task.Run(() => FetchHistory(device, cancellation.Token), cancellation.Token);
            }

            ServiceLogger.Info("人脸事件入库服务已启动。");
        }

        private void OnDeviceConnectionStateChanged(object sender, DeviceConnectionEventArgs e)
        {
            if (!options.Enabled || e.Device == null)
            {
                return;
            }

            try
            {
                if (e.Success)
                {
                    SetupAlarm(e.Device);
                    Task.Run(() => FetchHistory(e.Device, cancellation.Token), cancellation.Token);
                }
                else
                {
                    CloseAlarm(e.Device);
                    StopRemoteConfig(e.Device);
                }
            }
            catch (Exception ex)
            {
                ServiceLogger.Error($"处理设备 {e.Device?.Name} 人脸事件订阅状态时发生异常。", ex);
            }
        }

        /// <summary>
        /// SDK 报警回调，仅处理门禁人脸事件。
        /// </summary>
        private void AlarmMessageCallback(int command, ref HCNetSDK.NET_DVR_ALARMER alarmer, IntPtr alarmInfo, uint bufferLength, IntPtr user)
        {
            if (!options.Enabled || disposed)
            {
                return;
            }

            if (command != HCNetSDK.COMM_ALARM_ACS || alarmInfo == IntPtr.Zero)
            {
                return;
            }

            try
            {
                var info = (HCNetSDK.NET_DVR_ACS_ALARM_INFO)Marshal.PtrToStructure(alarmInfo, typeof(HCNetSDK.NET_DVR_ACS_ALARM_INFO));
                if (!IsFaceVerifyMinor(info.dwMinor))
                {
                    return;
                }

                if (info.dwPicDataLen == 0 || info.pPicData == IntPtr.Zero)
                {
                    ServiceLogger.Warn("收到人脸事件但未携带抓拍图片，已跳过。");
                    return;
                }

                string deviceIp = SafeTrim(alarmer.sDeviceIP);
                DeviceConnectionInfo device = DeviceConnectionManager.Instance.GetAllDevices()
                    .FirstOrDefault(d => string.Equals(d.IpAddress, deviceIp, StringComparison.OrdinalIgnoreCase));

                FaceEventRecord record = BuildRecordFromAlarm(info, deviceIp, device);
                if (record == null)
                {
                    return;
                }

                Enqueue(record);

                if (device != null)
                {
                    lock (device.LockObject)
                    {
                        device.LastSerialNo = record.SerialNo;
                        device.LastFaceEventTime = record.EventTime;
                    }
                }
            }
            catch (Exception ex)
            {
                ServiceLogger.Error("处理人脸事件回调时发生异常。", ex);
            }
        }

        private FaceEventRecord BuildRecordFromAlarm(HCNetSDK.NET_DVR_ACS_ALARM_INFO info, string deviceIp, DeviceConnectionInfo device)
        {
            DateTime eventTime = ConvertToDateTime(info.struTime);
            var acs = info.struAcsEventInfo;

            FaceEventType eventType = info.dwMinor == HCNetSDK.MINOR_FACE_VERIFY_FAIL
                ? FaceEventType.Fail
                : FaceEventType.Pass;

            byte[] snapshot = new byte[info.dwPicDataLen];
            Marshal.Copy(info.pPicData, snapshot, 0, (int)info.dwPicDataLen);

            return new FaceEventRecord
            {
                EventType = eventType,
                EventTime = eventTime,
                UserId = acs.dwEmployeeNo != 0 ? acs.dwEmployeeNo.ToString() : string.Empty,
                CardNo = ByteArrayToString(acs.byCardNo),
                DeviceName = device != null ? device.Name : deviceIp,
                DeviceIP = deviceIp,
                VerifyMode = $"0x{info.dwMinor:X}",
                SerialNo = GenerateSerial(eventTime, deviceIp),
                Snapshot = snapshot
            };
        }

        private static bool IsFaceVerifyMinor(uint minor)
        {
            if (minor == HCNetSDK.MINOR_FACE_VERIFY_PASS || minor == HCNetSDK.MINOR_FACE_VERIFY_FAIL)
            {
                return true;
            }

            // 组合验证 0x3C~0x44 视为通过类事件
            return minor >= 0x3C && minor <= 0x44;
        }

        private void Enqueue(FaceEventRecord record)
        {
            if (record == null)
            {
                return;
            }

            if (!eventQueue.TryAdd(record))
            {
                ServiceLogger.Warn("人脸事件队列已满，事件被丢弃。请调大 QueueCapacity 或加快落库速度。");
            }
        }

        private void SetupAlarm(DeviceConnectionInfo device)
        {
            if (device == null || device.UserID < 0)
            {
                return;
            }

            lock (device.LockObject)
            {
                if (device.AlarmHandle >= 0)
                {
                    HCNetSDK.NET_DVR_CloseAlarmChan_V30(device.AlarmHandle);
                    device.AlarmHandle = -1;
                }
            }

            HCNetSDK.NET_DVR_SETUPALARM_PARAM param = new HCNetSDK.NET_DVR_SETUPALARM_PARAM();
            param.Init();
            param.byLevel = 1; // 0-一级,1-二级，使用默认

            int handle = HCNetSDK.NET_DVR_SetupAlarmChan_V41(device.UserID, ref param);
            if (handle < 0)
            {
                uint err = HCNetSDK.NET_DVR_GetLastError();
                ServiceLogger.Error($"设备 {device.Name} 报警布防失败，错误码: {err}");
                return;
            }

            lock (device.LockObject)
            {
                device.AlarmHandle = handle;
            }

            ServiceLogger.Info($"设备 {device.Name} 已开启人脸事件订阅。");
        }

        private void CloseAlarm(DeviceConnectionInfo device)
        {
            if (device == null)
            {
                return;
            }

            lock (device.LockObject)
            {
                if (device.AlarmHandle >= 0)
                {
                    HCNetSDK.NET_DVR_CloseAlarmChan_V30(device.AlarmHandle);
                    device.AlarmHandle = -1;
                }
            }
        }

        private void ProcessQueue(CancellationToken token)
        {
            List<FaceEventRecord> buffer = new List<FaceEventRecord>(options.BatchSize);

            while (!token.IsCancellationRequested)
            {
                FaceEventRecord item;
                try
                {
                    item = eventQueue.Take(token);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (InvalidOperationException)
                {
                    break;
                }

                buffer.Add(item);

                while (buffer.Count < options.BatchSize && eventQueue.TryTake(out var next))
                {
                    buffer.Add(next);
                }

                PersistBatchWithRetry(buffer, token);
                buffer.Clear();
            }

            // 尝试清空剩余事件
            while (eventQueue.TryTake(out var tail))
            {
                buffer.Add(tail);
                if (buffer.Count >= options.BatchSize)
                {
                    PersistBatchWithRetry(buffer, token);
                    buffer.Clear();
                }
            }

            if (buffer.Count > 0)
            {
                PersistBatchWithRetry(buffer, token);
            }
        }

        private void PersistBatchWithRetry(List<FaceEventRecord> batch, CancellationToken token)
        {
            if (batch == null || batch.Count == 0)
            {
                return;
            }

            int attempts = 0;
            while (!token.IsCancellationRequested)
            {
                attempts++;
                try
                {
                    PersistBatch(batch);
                    return;
                }
                catch (Exception ex)
                {
                    ServiceLogger.Error($"写入人脸事件失败，准备重试（第 {attempts} 次）。", ex);
                    int delay = Math.Min(options.RetryIntervalSeconds * (int)Math.Pow(2, attempts - 1), 60);
                    Thread.Sleep(TimeSpan.FromSeconds(delay));
                }
            }
        }

        private void PersistBatch(List<FaceEventRecord> batch)
        {
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                using (SqlTransaction tran = conn.BeginTransaction())
                {
                    using (SqlCommand cmd = conn.CreateCommand())
                    {
                        cmd.Transaction = tran;
                        cmd.CommandTimeout = commandTimeoutSeconds;
                        cmd.CommandText =
                            "IF NOT EXISTS (SELECT 1 FROM face_event_log WHERE DeviceIP=@DeviceIP AND SerialNo=@SerialNo) " +
                            "BEGIN INSERT INTO face_event_log (EventType, EventTime, UserId, CardNo, DeviceName, DeviceIP, VerifyMode, SerialNo, Snapshot) " +
                            "VALUES (@EventType, @EventTime, @UserId, @CardNo, @DeviceName, @DeviceIP, @VerifyMode, @SerialNo, @Snapshot); END";

                        cmd.Parameters.Add("@EventType", SqlDbType.TinyInt);
                        cmd.Parameters.Add("@EventTime", SqlDbType.DateTime2);
                        cmd.Parameters.Add("@UserId", SqlDbType.NVarChar, 64);
                        cmd.Parameters.Add("@CardNo", SqlDbType.NVarChar, 32);
                        cmd.Parameters.Add("@DeviceName", SqlDbType.NVarChar, 128);
                        cmd.Parameters.Add("@DeviceIP", SqlDbType.NVarChar, 45);
                        cmd.Parameters.Add("@VerifyMode", SqlDbType.NVarChar, 32);
                        cmd.Parameters.Add("@SerialNo", SqlDbType.BigInt);
                        cmd.Parameters.Add("@Snapshot", SqlDbType.VarBinary, -1);

                        foreach (var item in batch)
                        {
                            cmd.Parameters["@EventType"].Value = (byte)item.EventType;
                            cmd.Parameters["@EventTime"].Value = item.EventTime;
                            cmd.Parameters["@UserId"].Value = (object)item.UserId ?? DBNull.Value;
                            cmd.Parameters["@CardNo"].Value = (object)item.CardNo ?? DBNull.Value;
                            cmd.Parameters["@DeviceName"].Value = (object)item.DeviceName ?? DBNull.Value;
                            cmd.Parameters["@DeviceIP"].Value = item.DeviceIP ?? string.Empty;
                            cmd.Parameters["@VerifyMode"].Value = (object)item.VerifyMode ?? DBNull.Value;
                            cmd.Parameters["@SerialNo"].Value = item.SerialNo;
                            cmd.Parameters["@Snapshot"].Value = item.Snapshot ?? (object)DBNull.Value;

                            cmd.ExecuteNonQuery();
                        }
                    }

                    // 同步补偿检查点，按设备取最大序列号和时间
                    var checkpoints = batch
                        .GroupBy(b => b.DeviceIP ?? string.Empty)
                        .Select(g => new
                        {
                            DeviceIP = g.Key,
                            LastSerial = g.Max(x => x.SerialNo),
                            LastEventTime = g.Max(x => x.EventTime)
                        })
                        .ToList();

                    using (SqlCommand ckCmd = conn.CreateCommand())
                    {
                        ckCmd.Transaction = tran;
                        ckCmd.CommandTimeout = commandTimeoutSeconds;
                        ckCmd.CommandText =
                            "MERGE face_event_checkpoint AS t " +
                            "USING (VALUES (@DeviceIP, @LastSerialNo, @LastEventTime)) AS s(DeviceIP, LastSerialNo, LastEventTime) " +
                            "ON t.DeviceIP = s.DeviceIP " +
                            "WHEN MATCHED THEN UPDATE SET LastSerialNo = CASE WHEN s.LastSerialNo > t.LastSerialNo THEN s.LastSerialNo ELSE t.LastSerialNo END, " +
                            "LastEventTime = CASE WHEN s.LastEventTime > t.LastEventTime THEN s.LastEventTime ELSE t.LastEventTime END, " +
                            "UpdatedAt = SYSUTCDATETIME() " +
                            "WHEN NOT MATCHED THEN INSERT (DeviceIP, LastSerialNo, LastEventTime, UpdatedAt) VALUES (s.DeviceIP, s.LastSerialNo, s.LastEventTime, SYSUTCDATETIME());";

                        ckCmd.Parameters.Add("@DeviceIP", SqlDbType.NVarChar, 45);
                        ckCmd.Parameters.Add("@LastSerialNo", SqlDbType.BigInt);
                        ckCmd.Parameters.Add("@LastEventTime", SqlDbType.DateTime2);

                        foreach (var item in checkpoints)
                        {
                            ckCmd.Parameters["@DeviceIP"].Value = item.DeviceIP;
                            ckCmd.Parameters["@LastSerialNo"].Value = item.LastSerial;
                            ckCmd.Parameters["@LastEventTime"].Value = item.LastEventTime;
                            ckCmd.ExecuteNonQuery();
                        }
                    }

                    tran.Commit();
                }
            }
        }

        private void FetchHistory(DeviceConnectionInfo device, CancellationToken token)
        {
            if (device == null || device.UserID < 0 || !options.Enabled)
            {
                return;
            }

            try
            {
                var checkpoint = GetCheckpoint(device.IpAddress);
                DateTime startTime = checkpoint.HasValue
                    ? checkpoint.Value.LastEventTime.AddMilliseconds(1)
                    : DateTime.Now.AddMinutes(-options.CompensationLookbackMinutes);

                uint beginSerial = 0;
                if (checkpoint.HasValue && checkpoint.Value.LastSerialNo > 0 && checkpoint.Value.LastSerialNo < uint.MaxValue)
                {
                    beginSerial = (uint)Math.Min(checkpoint.Value.LastSerialNo + 1, uint.MaxValue);
                }

                HCNetSDK.NET_DVR_ACS_EVENT_COND cond = new HCNetSDK.NET_DVR_ACS_EVENT_COND();
                cond.Init();
                cond.dwSize = (uint)Marshal.SizeOf(typeof(HCNetSDK.NET_DVR_ACS_EVENT_COND));
                cond.dwMajor = HCNetSDK.MAJOR_EVENT;
                cond.dwMinor = 0; // 拉取全部再本地过滤
                cond.struStartTime = ToDvrTime(startTime);
                cond.struEndTime = ToDvrTime(DateTime.Now);
                cond.dwBeginSerialNo = beginSerial;
                cond.byPicEnable = 1;

                int size = Marshal.SizeOf(cond);
                IntPtr ptrCond = Marshal.AllocHGlobal(size);
                Marshal.StructureToPtr(cond, ptrCond, false);

                int handle = HCNetSDK.NET_DVR_StartRemoteConfig(device.UserID, HCNetSDK.NET_DVR_GET_ACS_EVENT, ptrCond, size, null, IntPtr.Zero);
                Marshal.FreeHGlobal(ptrCond);

                if (handle < 0)
                {
                    uint err = HCNetSDK.NET_DVR_GetLastError();
                    ServiceLogger.Error($"设备 {device.Name} 历史事件补偿启动失败，错误码: {err}");
                    return;
                }

                remoteConfigHandles[device.Id] = handle;

                IntPtr outPtr = IntPtr.Zero;
                try
                {
                    int cfgSize = Marshal.SizeOf(typeof(HCNetSDK.NET_DVR_ACS_EVENT_CFG));
                    outPtr = Marshal.AllocHGlobal(cfgSize);

                    while (!token.IsCancellationRequested)
                    {
                        int status = HCNetSDK.NET_DVR_GetNextRemoteConfig(handle, outPtr, (uint)cfgSize);
                        if (status == (int)HCNetSDK.NET_SDK_SENDWITHRECV_STATUS.NET_SDK_CONFIG_STATUS_SUCCESS)
                        {
                            var cfg = (HCNetSDK.NET_DVR_ACS_EVENT_CFG)Marshal.PtrToStructure(outPtr, typeof(HCNetSDK.NET_DVR_ACS_EVENT_CFG));
                            if (!IsFaceVerifyMinor(cfg.dwMinor))
                            {
                                continue;
                            }

                            if (cfg.dwPicDataLen == 0 || cfg.pPicData == IntPtr.Zero)
                            {
                                continue;
                            }

                            FaceEventRecord record = BuildRecordFromConfig(cfg, device);
                            Enqueue(record);
                        }
                        else if (status == (int)HCNetSDK.NET_SDK_SENDWITHRECV_STATUS.NET_SDK_CONFIG_STATUS_FINISH)
                        {
                            break;
                        }
                        else if (status == (int)HCNetSDK.NET_SDK_SENDWITHRECV_STATUS.NET_SDK_CONFIG_STATUS_NEEDWAIT)
                        {
                            Thread.Sleep(200);
                        }
                        else
                        {
                            ServiceLogger.Warn($"设备 {device.Name} 补偿通道返回状态 {status}，终止补偿。");
                            break;
                        }
                    }
                }
                finally
                {
                    if (outPtr != IntPtr.Zero)
                    {
                        Marshal.FreeHGlobal(outPtr);
                    }

                    StopRemoteConfig(device);
                }
            }
            catch (Exception ex)
            {
                ServiceLogger.Error($"设备 {device?.Name} 补偿流程异常。", ex);
            }
        }

        private FaceEventRecord BuildRecordFromConfig(HCNetSDK.NET_DVR_ACS_EVENT_CFG cfg, DeviceConnectionInfo device)
        {
            var detail = cfg.struAcsEventInfo;
            byte[] snapshot = new byte[cfg.dwPicDataLen];
            Marshal.Copy(cfg.pPicData, snapshot, 0, (int)cfg.dwPicDataLen);

            FaceEventType eventType = cfg.dwMinor == HCNetSDK.MINOR_FACE_VERIFY_FAIL ? FaceEventType.Fail : FaceEventType.Pass;

            return new FaceEventRecord
            {
                EventType = eventType,
                EventTime = ConvertToDateTime(cfg.struTime),
                UserId = detail.dwEmployeeNo != 0 ? detail.dwEmployeeNo.ToString() : string.Empty,
                CardNo = ByteArrayToString(detail.byCardNo),
                DeviceName = device != null ? device.Name : device?.IpAddress,
                DeviceIP = device?.IpAddress,
                VerifyMode = $"0x{cfg.dwMinor:X}",
                SerialNo = detail.dwSerialNo,
                Snapshot = snapshot
            };
        }

        private (long LastSerialNo, DateTime LastEventTime)? GetCheckpoint(string deviceIp)
        {
            if (string.IsNullOrWhiteSpace(deviceIp))
            {
                return null;
            }

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                using (SqlCommand cmd = conn.CreateCommand())
                {
                    cmd.CommandTimeout = commandTimeoutSeconds;
                    cmd.CommandText = "SELECT LastSerialNo, LastEventTime FROM face_event_checkpoint WHERE DeviceIP=@DeviceIP";
                    cmd.Parameters.Add("@DeviceIP", SqlDbType.NVarChar, 45).Value = deviceIp;

                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            long serial = reader.GetInt64(0);
                            DateTime time = reader.GetDateTime(1);
                            return (serial, time);
                        }
                    }
                }
            }

            return null;
        }

        private void StopRemoteConfig(DeviceConnectionInfo device)
        {
            if (device == null)
            {
                return;
            }

            if (remoteConfigHandles.TryRemove(device.Id, out int handle))
            {
                HCNetSDK.NET_DVR_StopRemoteConfig(handle);
            }
        }

        private static DateTime ConvertToDateTime(HCNetSDK.NET_DVR_TIME time)
        {
            try
            {
                return new DateTime(time.dwYear, time.dwMonth, time.dwDay, time.dwHour, time.dwMinute, time.dwSecond);
            }
            catch
            {
                return DateTime.Now;
            }
        }

        private static HCNetSDK.NET_DVR_TIME ToDvrTime(DateTime time)
        {
            return new HCNetSDK.NET_DVR_TIME
            {
                dwYear = time.Year,
                dwMonth = time.Month,
                dwDay = time.Day,
                dwHour = time.Hour,
                dwMinute = time.Minute,
                dwSecond = time.Second
            };
        }

        private static string ByteArrayToString(byte[] data)
        {
            if (data == null || data.Length == 0)
            {
                return string.Empty;
            }

            int length = Array.IndexOf<byte>(data, 0);
            if (length < 0)
            {
                length = data.Length;
            }

            return length == 0 ? string.Empty : Encoding.UTF8.GetString(data, 0, length).Trim();
        }

        private static string SafeTrim(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }

            int zeroIndex = value.IndexOf('\0');
            return zeroIndex >= 0 ? value.Substring(0, zeroIndex) : value.Trim();
        }

        private static long GenerateSerial(DateTime eventTime, string deviceIp)
        {
            long millis = (long)(eventTime.ToUniversalTime() - UnixEpoch).TotalMilliseconds;
            int hash = string.IsNullOrEmpty(deviceIp) ? 0 : (deviceIp.GetHashCode() & 0xFFFF);
            return (millis << 16) | (uint)hash;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (disposed || !disposing)
            {
                return;
            }

            disposed = true;

            try
            {
                DeviceConnectionManager.Instance.DeviceConnectionStateChanged -= OnDeviceConnectionStateChanged;
            }
            catch
            {
                // ignore
            }

            cancellation.Cancel();
            eventQueue.CompleteAdding();

            try
            {
                Task.WaitAll(workers.ToArray(), TimeSpan.FromSeconds(5));
            }
            catch
            {
                // ignore
            }

            foreach (var device in DeviceConnectionManager.Instance.GetAllDevices())
            {
                CloseAlarm(device);
                StopRemoteConfig(device);
            }
        }

        private enum FaceEventType : byte
        {
            Fail = 0,
            Pass = 1
        }

        private sealed class FaceEventRecord
        {
            public FaceEventType EventType { get; set; }
            public DateTime EventTime { get; set; }
            public string UserId { get; set; }
            public string CardNo { get; set; }
            public string DeviceName { get; set; }
            public string DeviceIP { get; set; }
            public string VerifyMode { get; set; }
            public long SerialNo { get; set; }
            public byte[] Snapshot { get; set; }
        }
    }
}
