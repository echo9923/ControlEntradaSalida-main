using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
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

        private const byte DefaultDirection = 1;
        private const byte DefaultProcessStatus = 0;
        private const long DefaultTenantId = 1;
        private const string DefaultDeleted = "0";

        private readonly BlockingCollection<FaceEventRecord> eventQueue;
        private readonly CancellationTokenSource cancellation = new CancellationTokenSource();
        private readonly List<Task> workers = new List<Task>();
        private readonly ConcurrentDictionary<int, int> remoteConfigHandles = new ConcurrentDictionary<int, int>();
        private readonly ConcurrentDictionary<string, string> nicknameCache = new ConcurrentDictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        private readonly string snapshotRootDirectory;

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

            string dataDirectory = ResolveDataDirectory();
            snapshotRootDirectory = string.IsNullOrWhiteSpace(dataDirectory)
                ? null
                : Path.Combine(dataDirectory, "snapshots");
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

            ServiceLogger.Info("人脸事件入库服务已启动（写入进出记录表 attendance_gate）。");
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
                string deviceSerialNumber = null;
                string deviceNameFallback = null;

                try
                {
                    if (alarmer.bySerialValid == 1 && alarmer.sSerialNumber != null && alarmer.sSerialNumber.Length > 0)
                    {
                        deviceSerialNumber = Encoding.ASCII.GetString(alarmer.sSerialNumber).TrimEnd('\0').Trim();
                    }

                    if (alarmer.byDeviceNameValid == 1 && !string.IsNullOrWhiteSpace(alarmer.sDeviceName))
                    {
                        deviceNameFallback = alarmer.sDeviceName.Trim();
                    }
                }
                catch
                {
                    // 忽略读取报警设备信息失败
                }

                FaceEventRecord record = BuildRecordFromAlarm(info, deviceIp, device);
                if (record == null)
                {
                    return;
                }

                if (string.IsNullOrWhiteSpace(record.DeviceSerialNumber) && !string.IsNullOrWhiteSpace(deviceSerialNumber))
                {
                    record.DeviceSerialNumber = deviceSerialNumber;
                }

                if (device == null && !string.IsNullOrWhiteSpace(deviceNameFallback))
                {
                    record.DeviceName = deviceNameFallback;
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
                DeviceId = device?.Id ?? 0,
                DeviceName = device != null ? device.Name : deviceIp,
                DeviceSerialNumber = device?.SerialNumber,
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
                    ServiceLogger.Error($"写入进出记录失败，准备重试（第 {attempts} 次）。", ex);
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
                            "IF NOT EXISTS (SELECT 1 FROM dbo.attendance_gate WHERE id=@Id) " +
                            "BEGIN INSERT INTO dbo.attendance_gate (" +
                            "id, username, nickname, record_datetime, record_date, record_time, direction, device_name, device_sn, snapshot_path, process_status, create_time, update_time, deleted, tenant_id" +
                            ") VALUES (" +
                            "@Id, @Username, @Nickname, @RecordDateTime, @RecordDate, @RecordTime, @Direction, @DeviceName, @DeviceSn, @SnapshotPath, @ProcessStatus, @CreateTime, @UpdateTime, @Deleted, @TenantId" +
                            "); END";

                        cmd.Parameters.Add("@Id", SqlDbType.BigInt);
                        cmd.Parameters.Add("@Username", SqlDbType.NVarChar, 30);
                        cmd.Parameters.Add("@Nickname", SqlDbType.NVarChar, 50);
                        cmd.Parameters.Add("@RecordDateTime", SqlDbType.DateTime2);
                        cmd.Parameters.Add("@RecordDate", SqlDbType.Date);
                        cmd.Parameters.Add("@RecordTime", SqlDbType.Time);
                        cmd.Parameters.Add("@Direction", SqlDbType.TinyInt);
                        cmd.Parameters.Add("@DeviceName", SqlDbType.NVarChar, 100);
                        cmd.Parameters.Add("@DeviceSn", SqlDbType.NVarChar, 100);
                        cmd.Parameters.Add("@SnapshotPath", SqlDbType.NVarChar, 255);
                        cmd.Parameters.Add("@ProcessStatus", SqlDbType.TinyInt);
                        cmd.Parameters.Add("@CreateTime", SqlDbType.DateTime2);
                        cmd.Parameters.Add("@UpdateTime", SqlDbType.DateTime2);
                        cmd.Parameters.Add("@Deleted", SqlDbType.VarChar, 1);
                        cmd.Parameters.Add("@TenantId", SqlDbType.BigInt);

                        DateTime now = DateTime.Now;

                        foreach (var item in batch)
                        {
                            string username = item.UserId?.Trim();
                            if (string.IsNullOrWhiteSpace(username))
                            {
                                ServiceLogger.Warn($"人脸事件缺少员工工号，已跳过写入。设备: {item.DeviceName ?? item.DeviceIP}");
                                continue;
                            }

                            string deviceName = !string.IsNullOrWhiteSpace(item.DeviceName)
                                ? item.DeviceName
                                : item.DeviceIP;

                            string deviceSn = !string.IsNullOrWhiteSpace(item.DeviceSerialNumber)
                                ? item.DeviceSerialNumber
                                : item.DeviceIP;

                            if (string.IsNullOrWhiteSpace(deviceSn))
                            {
                                ServiceLogger.Warn($"人脸事件缺少设备序列号，device_sn 将为空。设备: {deviceName}");
                            }

                            long id = BuildAttendanceGateId(item);
                            string nickname = ResolveNickname(conn, tran, username);
                            string snapshotPath = PersistSnapshot(id, item.EventTime, username, item.Snapshot);

                            cmd.Parameters["@Id"].Value = id;
                            cmd.Parameters["@Username"].Value = username;
                            cmd.Parameters["@Nickname"].Value = (object)nickname ?? DBNull.Value;
                            cmd.Parameters["@RecordDateTime"].Value = item.EventTime;
                            cmd.Parameters["@RecordDate"].Value = item.EventTime.Date;
                            cmd.Parameters["@RecordTime"].Value = item.EventTime.TimeOfDay;
                            cmd.Parameters["@Direction"].Value = DefaultDirection;
                            cmd.Parameters["@DeviceName"].Value = (object)deviceName ?? DBNull.Value;
                            cmd.Parameters["@DeviceSn"].Value = (object)deviceSn ?? DBNull.Value;
                            cmd.Parameters["@SnapshotPath"].Value = (object)snapshotPath ?? DBNull.Value;
                            cmd.Parameters["@ProcessStatus"].Value = DefaultProcessStatus;
                            cmd.Parameters["@CreateTime"].Value = now;
                            cmd.Parameters["@UpdateTime"].Value = now;
                            cmd.Parameters["@Deleted"].Value = DefaultDeleted;
                            cmd.Parameters["@TenantId"].Value = DefaultTenantId;

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
                            "MERGE dbo.face_event_checkpoint AS t " +
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

        private long BuildAttendanceGateId(FaceEventRecord record)
        {
            if (record == null)
            {
                return 0;
            }

            int deviceId = record.DeviceId > 0 ? record.DeviceId : 0;
            uint serialNo = record.SerialNo > 0 && record.SerialNo <= uint.MaxValue ? (uint)record.SerialNo : 0;
            if (deviceId > 0 && serialNo > 0)
            {
                return ((long)deviceId << 32) | serialNo;
            }

            return GenerateAttendanceGateHashId(record);
        }

        private static long GenerateAttendanceGateHashId(FaceEventRecord record)
        {
            string eventTime = record.EventTime.ToString("yyyy-MM-dd'T'HH:mm:ss");
            string key = $"{record.UserId}|{record.DeviceIP}|{record.DeviceSerialNumber}|{eventTime}|{(byte)record.EventType}|{record.VerifyMode}";
            ulong hash = Fnv1a64(key);
            long id = (long)(hash & 0x7FFFFFFFFFFFFFFFUL);
            return id == 0 ? 1 : id;
        }

        private static ulong Fnv1a64(string value)
        {
            const ulong offsetBasis = 14695981039346656037UL;
            const ulong prime = 1099511628211UL;

            ulong hash = offsetBasis;
            if (string.IsNullOrEmpty(value))
            {
                return hash;
            }

            byte[] data = Encoding.UTF8.GetBytes(value);
            for (int i = 0; i < data.Length; i++)
            {
                hash ^= data[i];
                hash *= prime;
            }

            return hash;
        }

        private string ResolveNickname(SqlConnection conn, SqlTransaction tran, string username)
        {
            if (string.IsNullOrWhiteSpace(username) || conn == null)
            {
                return null;
            }

            if (nicknameCache.TryGetValue(username, out string cached))
            {
                return string.IsNullOrWhiteSpace(cached) ? null : cached;
            }

            try
            {
                using (SqlCommand cmd = conn.CreateCommand())
                {
                    cmd.Transaction = tran;
                    cmd.CommandTimeout = commandTimeoutSeconds;
                    cmd.CommandText = "SELECT TOP 1 nickname FROM dbo.system_users WHERE username=@Username AND deleted=0";
                    cmd.Parameters.Add("@Username", SqlDbType.NVarChar, 30).Value = username;
                    object result = cmd.ExecuteScalar();
                    string nickname = result == null || result == DBNull.Value ? null : Convert.ToString(result);
                    nicknameCache[username] = nickname ?? string.Empty;
                    return string.IsNullOrWhiteSpace(nickname) ? null : nickname;
                }
            }
            catch (Exception ex)
            {
                ServiceLogger.Error($"查询人员姓名失败，username={username}", ex);
                nicknameCache.TryAdd(username, string.Empty);
                return null;
            }
        }

        private string PersistSnapshot(long attendanceGateId, DateTime eventTime, string username, byte[] snapshot)
        {
            if (snapshot == null || snapshot.Length == 0)
            {
                return null;
            }

            if (string.IsNullOrWhiteSpace(snapshotRootDirectory))
            {
                return null;
            }

            string dateFolder = eventTime.ToString("yyyy-MM-dd");
            string snapshotFolder = Path.Combine(snapshotRootDirectory, dateFolder);

            try
            {
                Directory.CreateDirectory(snapshotFolder);
            }
            catch (Exception ex)
            {
                ServiceLogger.Error($"创建抓拍目录失败: {snapshotFolder}", ex);
                return null;
            }

            string safeUsername = SanitizeFileNamePart(username);
            if (string.IsNullOrWhiteSpace(safeUsername))
            {
                safeUsername = "unknown";
            }

            long stableId = attendanceGateId > 0 ? attendanceGateId : 1;
            string timePart = eventTime.ToString("yyyy-MM-dd'T'HH-mm-ss");
            string fileName = $"{safeUsername}_{timePart}_{stableId}.jpg";
            string relativePath = Path.Combine("snapshots", dateFolder, fileName);
            string fullPath = Path.Combine(snapshotFolder, fileName);

            if (File.Exists(fullPath))
            {
                return relativePath;
            }

            string tempPath = fullPath + ".tmp";
            try
            {
                File.WriteAllBytes(tempPath, snapshot);

                try
                {
                    File.Move(tempPath, fullPath);
                }
                catch (IOException)
                {
                    // 并发或重试场景下文件已存在，视为成功
                    TryDeleteFile(tempPath);
                }

                return relativePath;
            }
            catch (Exception ex)
            {
                TryDeleteFile(tempPath);
                ServiceLogger.Error($"写入抓拍图片失败: {fullPath}", ex);
                return null;
            }
        }

        private static string ResolveDataDirectory()
        {
            if (!string.IsNullOrWhiteSpace(Common.datadir))
            {
                return Common.datadir;
            }

            try
            {
                Common.CrearDirectorioData();
            }
            catch
            {
                // ignore
            }

            if (!string.IsNullOrWhiteSpace(Common.datadir))
            {
                return Common.datadir;
            }

            string commonData = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            return Path.Combine(commonData, "Neapps", "ControlEntradaSalida", "data");
        }

        private static string SanitizeFileNamePart(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            string cleaned = value.Trim();
            foreach (char c in Path.GetInvalidFileNameChars())
            {
                cleaned = cleaned.Replace(c.ToString(), string.Empty);
            }

            return cleaned;
        }


        private static void TryDeleteFile(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return;
            }

            try
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }
            catch
            {
                // 忽略清理临时文件时的异常
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
                DeviceId = device?.Id ?? 0,
                DeviceName = device != null ? device.Name : device?.IpAddress,
                DeviceSerialNumber = device?.SerialNumber,
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
                    cmd.CommandText = "SELECT LastSerialNo, LastEventTime FROM dbo.face_event_checkpoint WHERE DeviceIP=@DeviceIP";
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
            public int DeviceId { get; set; }
            public string DeviceName { get; set; }
            public string DeviceSerialNumber { get; set; }
            public string DeviceIP { get; set; }
            public string VerifyMode { get; set; }
            public long SerialNo { get; set; }
            public byte[] Snapshot { get; set; }
        }
    }
}
