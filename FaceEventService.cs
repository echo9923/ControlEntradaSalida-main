using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
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

        private const byte DefaultDirection = 1;
        private const byte DefaultProcessStatus = 0;
        private const long DefaultTenantId = 1;
        private const string DefaultDeleted = "0";

        private static readonly int AcsAlarmInfoSize = Marshal.SizeOf(typeof(HCNetSDK.NET_DVR_ACS_ALARM_INFO));
        private static readonly int AcsEventCondSize = Marshal.SizeOf(typeof(HCNetSDK.NET_DVR_ACS_EVENT_COND));
        private static readonly int AcsEventCfgSize = Marshal.SizeOf(typeof(HCNetSDK.NET_DVR_ACS_EVENT_CFG));
        private static readonly int AcsEventDetailSize = Marshal.SizeOf(typeof(HCNetSDK.NET_DVR_ACS_EVENT_DETAIL));
        private static int acsInteropLayoutLogged;

        private readonly int deviceSdkLockTimeoutMs;
        private readonly BlockingCollection<FaceEventRecord> eventQueue;
        private readonly CancellationTokenSource cancellation = new CancellationTokenSource();

        private readonly CancellationTokenSource persistCancellation = new CancellationTokenSource();
        private readonly List<Task> workers = new List<Task>();
        private readonly ConcurrentDictionary<int, int> remoteConfigHandles = new ConcurrentDictionary<int, int>();
        
        private readonly ConcurrentDictionary<int, CancellationTokenSource> compensationTokens = new ConcurrentDictionary<int, CancellationTokenSource>();
        private readonly ConcurrentDictionary<string, string> nicknameCache = new ConcurrentDictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        private readonly string snapshotRootDirectory;

        private HCNetSDK.MSGCallBack alarmCallback;
        private bool callbackRegistered;
        private bool disposed;


        private int started;
        private int stopping;

        public FaceEventService(ServiceConfiguration configuration)
        {
            if (configuration == null)
            {
                throw new ArgumentNullException(nameof(configuration));
            }

            deviceSdkLockTimeoutMs = configuration.DeviceConnection?.DeviceSdkLockTimeoutMs ?? 30000;

            options = configuration.FaceEvent ?? new ServiceConfiguration.FaceEventOptions
            {
                Enabled = false,
                QueueCapacity = 2000,
                BatchSize = 20,
                RetryIntervalSeconds = 5,
                ShutdownFlushTimeoutSeconds = 30,
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

            if (disposed)
            {
                throw new ObjectDisposedException(nameof(FaceEventService));
            }

            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new InvalidOperationException("未提供数据库连接字符串，无法启动人脸事件入库功能。");
            }

            if (Interlocked.CompareExchange(ref started, 1, 0) != 0)
            {
                ServiceLogger.Warn("人脸事件入库服务已启动，重复调用 Start 将被忽略。");
                return;
            }

            Volatile.Write(ref stopping, 0);

            try
            {
                LogAcsInteropLayoutOnce();

                // 注册报警回调，SDK 要求回调委托在托管侧保持存活
                alarmCallback = AlarmMessageCallback;
                callbackRegistered = HCNetSDK.NET_DVR_SetDVRMessageCallBack_V50(0, alarmCallback, IntPtr.Zero);
                if (!callbackRegistered)
                {
                    uint err = HCNetSDK.NET_DVR_GetLastError();
                    throw new InvalidOperationException($"注册报警回调失败，错误码: {err}");
                }

                DeviceConnectionManager.Instance.DeviceConnectionStateChanged += OnDeviceConnectionStateChanged;

                // 启动后台消费者（使用 CompleteAdding + GetConsumingEnumerable 的标准模型优雅停机）
                workers.Add(Task.Run(() => ProcessQueue()));

                // 已连接设备立即补充订阅与补偿
                foreach (var device in DeviceConnectionManager.Instance.GetAllDevices().Where(d => d.IsConnected))
                {
                    if (IsExcludedDevice(device))
                    {
                        ServiceLogger.Info($"设备 {device.Name} 已配置为跳过人脸事件订阅/补偿，忽略布防。");
                        continue;
                    }

                    SetupAlarm(device);
                    Task.Run(() => FetchHistory(device, cancellation.Token), cancellation.Token);
                }

                ServiceLogger.Info("人脸事件入库服务已启动（写入进出记录表 attendance_gate）。");
            }
            catch
            {
                Interlocked.Exchange(ref started, 0);
                throw;
            }
        }

        private void OnDeviceConnectionStateChanged(object sender, DeviceConnectionEventArgs e)
        {
            if (!options.Enabled || e.Device == null || Volatile.Read(ref stopping) == 1)
            {
                return;
            }

            try
            {
                if (e.Success)
                {
                    if (IsExcludedDevice(e.Device))
                    {
                        CloseAlarm(e.Device);

                        if (compensationTokens.TryRemove(e.Device.Id, out CancellationTokenSource excludedPreviousCts))
                        {
                            try
                            {
                                excludedPreviousCts.Cancel();
                            }
                            catch
                            {
                            }
                            finally
                            {
                                excludedPreviousCts.Dispose();
                            }
                        }

                        ServiceLogger.Info($"设备 {e.Device.Name} 已配置为跳过人脸事件订阅/补偿，忽略布防。");
                        return;
                    }

                    SetupAlarm(e.Device);

                    if (compensationTokens.TryRemove(e.Device.Id, out CancellationTokenSource previousCts))
                    {
                        try
                        {
                            previousCts.Cancel();
                        }
                        catch
                        {
                        }
                        finally
                        {
                            previousCts.Dispose();
                        }
                    }

                    CancellationTokenSource deviceCts = new CancellationTokenSource();
                    compensationTokens[e.Device.Id] = deviceCts;

                    CancellationTokenSource linkedCts = CancellationTokenSource.CreateLinkedTokenSource(
                        cancellation.Token,
                        deviceCts.Token);

                    Task task = Task.Run(() => FetchHistory(e.Device, linkedCts.Token), linkedCts.Token);
                    _ = task.ContinueWith(
                        _ => linkedCts.Dispose(),
                        CancellationToken.None,
                        TaskContinuationOptions.ExecuteSynchronously,
                        TaskScheduler.Default);
                }
                else
                {
                    if (compensationTokens.TryRemove(e.Device.Id, out CancellationTokenSource deviceCts))
                    {
                        try
                        {
                            deviceCts.Cancel();
                        }
                        catch
                        {
                        }
                        finally
                        {
                            deviceCts.Dispose();
                        }
                    }

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
            if (!options.Enabled || disposed || Volatile.Read(ref stopping) == 1)
            {
                return;
            }

            if (command != HCNetSDK.COMM_ALARM_ACS || alarmInfo == IntPtr.Zero)
            {
                return;
            }

            try
            {
                string deviceIp = SafeTrim(alarmer.sDeviceIP);

                DeviceConnectionInfo device;
                DeviceConnectionManager.Instance.TryGetDeviceByIp(deviceIp, out device);

                FaceEventRecord record;
                if (bufferLength >= (uint)AcsAlarmInfoSize)
                {
                    var info = Marshal.PtrToStructure<HCNetSDK.NET_DVR_ACS_ALARM_INFO>(alarmInfo);
                    if (!IsFaceVerifyMinor(info.dwMinor))
                    {
                        return;
                    }

                    record = BuildRecordFromAlarm(info, deviceIp, device);
                }
                else
                {
                    var info = Marshal.PtrToStructure<HCNetSDK.NET_DVR_ACS_ALARM_INFO_V1>(alarmInfo);
                    if (!IsFaceVerifyMinor(info.dwMinor))
                    {
                        return;
                    }

                    record = BuildRecordFromAlarm(info, deviceIp, device);
                }

                if (record == null)
                {
                    return;
                }

                if (string.IsNullOrWhiteSpace(record.DeviceSerialNumber)
                    && alarmer.bySerialValid == 1
                    && alarmer.sSerialNumber != null
                    && alarmer.sSerialNumber.Length > 0)
                {
                    try
                    {
                        record.DeviceSerialNumber = Encoding.ASCII.GetString(alarmer.sSerialNumber).TrimEnd('\0').Trim();
                    }
                    catch
                    {
                        // 忽略读取报警设备信息失败
                    }
                }

                if (device == null
                    && alarmer.byDeviceNameValid == 1
                    && !string.IsNullOrWhiteSpace(alarmer.sDeviceName))
                {
                    try
                    {
                        record.DeviceName = alarmer.sDeviceName.Trim();
                    }
                    catch
                    {
                        // 忽略读取报警设备信息失败
                    }
                }

                Enqueue(record);

                if (device != null)
                {
                    lock (device.LockObject)
                    {
                        if (record.SerialNo > 0)
                        {
                            device.LastSerialNo = record.SerialNo;
                        }

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
            if (info.byTimeType == 1)
            {
                eventTime = DateTime.SpecifyKind(eventTime, DateTimeKind.Utc).ToLocalTime();
            }

            var acs = info.struAcsEventInfo;

            FaceEventType eventType = info.dwMinor == HCNetSDK.MINOR_FACE_VERIFY_FAIL
                ? FaceEventType.Fail
                : FaceEventType.Pass;

            string employeeNo = TryGetEmployeeNoFromExtend(info);
            if (string.IsNullOrWhiteSpace(employeeNo) && acs.dwEmployeeNo != 0)
            {
                employeeNo = acs.dwEmployeeNo.ToString();
            }

            byte[] snapshot = null;
            string snapshotUrl = null;

            if (info.dwPicDataLen > 0 && info.pPicData != IntPtr.Zero)
            {
                if (info.byPicTransType == 1)
                {
                    snapshotUrl = ReadStringFromPtr(info.pPicData, info.dwPicDataLen);
                }
                else
                {
                    snapshot = new byte[info.dwPicDataLen];
                    Marshal.Copy(info.pPicData, snapshot, 0, (int)info.dwPicDataLen);

                    if (TryParseSnapshotUrl(snapshot, out string url))
                    {
                        snapshotUrl = url;
                        snapshot = null;
                    }
                }
            }

            return new FaceEventRecord
            {
                EventType = eventType,
                EventTime = eventTime,
                UserId = employeeNo ?? string.Empty,
                DeviceId = device?.Id ?? 0,
                DeviceName = device != null ? device.Name : deviceIp,
                DeviceSerialNumber = device?.SerialNumber,
                DeviceIP = deviceIp,
                VerifyMode = $"0x{info.dwMinor:X}",
                SerialNo = acs.dwSerialNo,
                Snapshot = snapshot,
                SnapshotUrl = snapshotUrl
            };
        }


        private FaceEventRecord BuildRecordFromAlarm(HCNetSDK.NET_DVR_ACS_ALARM_INFO_V1 info, string deviceIp, DeviceConnectionInfo device)
        {
            DateTime eventTime = ConvertToDateTime(info.struTime);
            var acs = info.struAcsEventInfo;

            FaceEventType eventType = info.dwMinor == HCNetSDK.MINOR_FACE_VERIFY_FAIL
                ? FaceEventType.Fail
                : FaceEventType.Pass;

            byte[] snapshot = null;
            string snapshotUrl = null;

            if (info.dwPicDataLen > 0 && info.pPicData != IntPtr.Zero)
            {
                snapshot = new byte[info.dwPicDataLen];
                Marshal.Copy(info.pPicData, snapshot, 0, (int)info.dwPicDataLen);

                if (TryParseSnapshotUrl(snapshot, out string url))
                {
                    snapshotUrl = url;
                    snapshot = null;
                }
            }

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
                SerialNo = 0,
                Snapshot = snapshot,
                SnapshotUrl = snapshotUrl
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

            if (Volatile.Read(ref stopping) == 1 || eventQueue.IsAddingCompleted)
            {
                return;
            }

            if (!eventQueue.TryAdd(record))
            {
                if (Volatile.Read(ref stopping) == 1 || eventQueue.IsAddingCompleted)
                {
                    return;
                }

                ServiceLogger.Warn("人脸事件队列已满，事件被丢弃。请调大 QueueCapacity 或加快落库速度。");
            }
        }

        private void SetupAlarm(DeviceConnectionInfo device)
        {
            if (device == null || device.UserID < 0)
            {
                return;
            }

            if (IsExcludedDevice(device))
            {
                CloseAlarm(device);
                ServiceLogger.Info($"设备 {device.Name} 已配置为跳过人脸事件订阅/补偿，忽略布防。");
                return;
            }

            using (var sdkLock = device.TryAcquireDeviceSdkLock(
                deviceSdkLockTimeoutMs,
                $"SetupAlarm-{device.Id}"))
            {
                if (!sdkLock.IsAcquired)
                {
                    ServiceLogger.Warn($"设备 {device.Name} 获取设备SDK锁超时，跳过布防。");
                    return;
                }

                int previousAlarmHandle;
                lock (device.LockObject)
                {
                    previousAlarmHandle = device.AlarmHandle;
                    device.AlarmHandle = -1;
                }

                if (previousAlarmHandle >= 0)
                {
                    HCNetSDK.NET_DVR_CloseAlarmChan_V30(previousAlarmHandle);
                }

                HCNetSDK.NET_DVR_SETUPALARM_PARAM param = new HCNetSDK.NET_DVR_SETUPALARM_PARAM();
                param.Init();
                param.byLevel = 1;

                byte deployType = options.AlarmDeployType;
                if (deployType != 0 && deployType != 1)
                {
                    deployType = 0;
                }

                param.SetDeployType(deployType);

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

                ServiceLogger.Info($"设备 {device.Name} 已开启人脸事件订阅（byDeployType={deployType}）。");
            }
        }

        private bool IsExcludedDevice(DeviceConnectionInfo device)
        {
            if (device == null)
            {
                return true;
            }

            if (options?.ExcludedDeviceIds != null && options.ExcludedDeviceIds.Count > 0)
            {
                if (options.ExcludedDeviceIds.Contains(device.Id))
                {
                    return true;
                }
            }

            string ip = device.IpAddress?.Trim();
            if (!string.IsNullOrWhiteSpace(ip)
                && options?.ExcludedDeviceIps != null
                && options.ExcludedDeviceIps.Count > 0)
            {
                foreach (string excludedIp in options.ExcludedDeviceIps)
                {
                    if (string.Equals(excludedIp, ip, StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private void CloseAlarm(DeviceConnectionInfo device)
        {
            if (device == null)
            {
                return;
            }

            using (var sdkLock = device.TryAcquireDeviceSdkLock(
                deviceSdkLockTimeoutMs,
                $"CloseAlarm-{device.Id}"))
            {
                if (!sdkLock.IsAcquired)
                {
                    ServiceLogger.Warn($"设备 {device.Name} 获取设备SDK锁超时，跳过撤防关闭。");
                    return;
                }

                int alarmHandle;
                lock (device.LockObject)
                {
                    alarmHandle = device.AlarmHandle;
                    device.AlarmHandle = -1;
                }

                if (alarmHandle < 0)
                {
                    return;
                }

                HCNetSDK.NET_DVR_CloseAlarmChan_V30(alarmHandle);
            }
        }

        private void ProcessQueue()
        {
            List<FaceEventRecord> buffer = new List<FaceEventRecord>(options.BatchSize);

            try
            {
                // 使用 BlockingCollection 的标准消费模型：CompleteAdding + GetConsumingEnumerable。
                // 停机时不会因为 CancellationToken 被取消而“跳过尾部写入”。
                foreach (var item in eventQueue.GetConsumingEnumerable())
                {
                    buffer.Add(item);

                    while (buffer.Count < options.BatchSize && eventQueue.TryTake(out var next))
                    {
                        buffer.Add(next);
                    }

                    PersistBatchWithRetry(buffer, persistCancellation.Token);
                    buffer.Clear();
                }

                if (buffer.Count > 0)
                {
                    PersistBatchWithRetry(buffer, persistCancellation.Token);
                }
            }
            catch (Exception ex)
            {
                ServiceLogger.Error("人脸事件入库消费者线程异常退出。", ex);
            }
        }

        private void PersistBatchWithRetry(List<FaceEventRecord> batch, CancellationToken token)
        {
            if (batch == null || batch.Count == 0)
            {
                return;
            }

            int attempts = 0;
            while (true)
            {
                attempts++;

                try
                {
                    PersistBatch(batch);
                    return;
                }
                catch (Exception ex)
                {
                    if (token.IsCancellationRequested)
                    {
                        ServiceLogger.Error($"写入进出记录失败，已取消重试并结束本批次（第 {attempts} 次）。", ex);
                        return;
                    }

                    ServiceLogger.Error($"写入进出记录失败，准备重试（第 {attempts} 次）。", ex);

                    int delaySeconds = ComputePersistBackoffSeconds(attempts);
                    token.WaitHandle.WaitOne(TimeSpan.FromSeconds(delaySeconds));

                    if (token.IsCancellationRequested)
                    {
                        return;
                    }
                }
            }
        }


        private int ComputePersistBackoffSeconds(int attempts)
        {
            const int maxDelaySeconds = 60;

            int baseDelaySeconds = Math.Max(1, options.RetryIntervalSeconds);
            int exponent = Math.Min(Math.Max(0, attempts - 1), 30);
            long delay = (long)baseDelaySeconds * (1L << exponent);

            if (delay > maxDelaySeconds)
            {
                return maxDelaySeconds;
            }

            return (int)delay;
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
                            string snapshotPath = !string.IsNullOrWhiteSpace(item.SnapshotUrl)
                                ? item.SnapshotUrl
                                : PersistSnapshot(id, item.EventTime, username, item.Snapshot);

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
            string legacyKey = $"{record.UserId}|{record.DeviceIP}|{record.DeviceSerialNumber}|{eventTime}|{(byte)record.EventType}|{record.VerifyMode}";

            string snapshotFingerprint = string.Empty;
            if (!string.IsNullOrWhiteSpace(record.SnapshotUrl))
            {
                snapshotFingerprint = record.SnapshotUrl;
            }
            else if (record.Snapshot != null && record.Snapshot.Length > 0)
            {
                snapshotFingerprint = Fnv1a64(record.Snapshot).ToString();
            }

            string key = legacyKey;
            if (record.SerialNo > 0 || !string.IsNullOrEmpty(snapshotFingerprint))
            {
                key = $"{legacyKey}|{record.SerialNo}|{snapshotFingerprint}";
            }

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

        private static ulong Fnv1a64(byte[] data)
        {
            const ulong offsetBasis = 14695981039346656037UL;
            const ulong prime = 1099511628211UL;

            ulong hash = offsetBasis;
            if (data == null || data.Length == 0)
            {
                return hash;
            }

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
                LogAcsInteropLayoutOnce();

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
                cond.dwSize = (uint)AcsEventCondSize;
                cond.dwMajor = HCNetSDK.MAJOR_EVENT;
                cond.dwMinor = 0;
                cond.struStartTime = ToDvrTime(startTime);
                cond.struEndTime = ToDvrTime(DateTime.Now);
                cond.dwBeginSerialNo = beginSerial;
                cond.byPicEnable = 1;
                cond.byTimeType = 0;
                // 技术规范：bySearchType=0 为保留值；按事件源搜索建议使用 1，并提供有效的 IOT 通道号（1..N）。
                cond.bySearchType = 1;
                cond.dwIOTChannelNo = 1;

                int size = AcsEventCondSize;

                string startLog =
                    $"设备 {device.Name} 准备启动历史事件补偿 | " +
                    $"Id={device.Id}, IP={device.IpAddress}, UserID={device.UserID}, " +
                    $"Start={startTime:yyyy-MM-dd HH:mm:ss.fff}, BeginSerial={beginSerial}, " +
                    $"Major={cond.dwMajor}, Minor={cond.dwMinor}, PicEnable={cond.byPicEnable}, TimeType={cond.byTimeType}, " +
                    $"SearchType={cond.bySearchType}, IOTChannelNo={cond.dwIOTChannelNo}, EventAttr={cond.byEventAttribute}, " +
                    $"CondSize={size}, CfgSize={AcsEventCfgSize}, IntPtrSize={IntPtr.Size}";

                if (ServiceLogger.IsVerboseEnabled)
                {
                    ServiceLogger.Verbose(startLog);
                }
                else
                {
                    ServiceLogger.Debug(startLog);
                }

                using (var sdkLock = device.TryAcquireDeviceSdkLock(
                    deviceSdkLockTimeoutMs,
                    $"FetchHistory-{device.Id}"))
                {
                    if (!sdkLock.IsAcquired)
                    {
                        ServiceLogger.Warn($"设备 {device.Name} 获取设备SDK锁超时，跳过历史事件补偿。");
                        return;
                    }

                    IntPtr ptrCond = IntPtr.Zero;
                    IntPtr outPtr = IntPtr.Zero;
                    int handle = -1;
                    int receivedCount = 0;
                    int filteredCount = 0;
                    int enqueuedCount = 0;
                    int nullRecordCount = 0;
                    int needWaitCount = 0;
                    int failedCount = 0;
                    int exceptionStatusCount = 0;
                    int unknownStatusCount = 0;
                    Stopwatch stopwatch = Stopwatch.StartNew();

                    try
                    {
                        ptrCond = Marshal.AllocHGlobal(size);
                        Marshal.StructureToPtr(cond, ptrCond, false);

                        string startMode = "Primary";
                        handle = HCNetSDK.NET_DVR_StartRemoteConfig(device.UserID, HCNetSDK.NET_DVR_GET_ACS_EVENT, ptrCond, size, null, IntPtr.Zero);
                        if (handle < 0)
                        {
                            uint err = HCNetSDK.NET_DVR_GetLastError();
                            string errMsg = GetSdkErrorMessage(err);

                            ServiceLogger.Warn(
                                $"设备 {device.Name} 历史事件补偿启动失败，将尝试降级重试 | " +
                                $"Err={err}{(string.IsNullOrWhiteSpace(errMsg) ? string.Empty : $"({errMsg})")}, " +
                                $"Major={cond.dwMajor}, PicEnable={cond.byPicEnable}, TimeType={cond.byTimeType}, SearchType={cond.bySearchType}, IOTChannelNo={cond.dwIOTChannelNo}, BeginSerial={cond.dwBeginSerialNo}");

                            // 兼容策略：部分设备固件不接受 BeginSerialNo（或仅支持按时间搜索）。
                            // 先将 BeginSerialNo 置 0 重试；仍失败再逐步降级图片/主类型；最后退回 SearchType=0（保留/兼容）尝试。
                            int retryHandle = -1;

                            if (cond.dwBeginSerialNo != 0)
                            {
                                cond.dwBeginSerialNo = 0;
                                ServiceLogger.Debug(
                                    $"设备 {device.Name} 历史事件补偿启动重试 | " +
                                    $"Id={device.Id}, Mode=BeginSerial=0, {BuildAcsEventCondSummary(cond)}");
                                Marshal.StructureToPtr(cond, ptrCond, false);
                                retryHandle = HCNetSDK.NET_DVR_StartRemoteConfig(device.UserID, HCNetSDK.NET_DVR_GET_ACS_EVENT, ptrCond, size, null, IntPtr.Zero);
                                if (retryHandle >= 0)
                                {
                                    handle = retryHandle;
                                    startMode = "BeginSerial=0";
                                }
                                else
                                {
                                    uint retryErr = HCNetSDK.NET_DVR_GetLastError();
                                    string retryErrMsg = GetSdkErrorMessage(retryErr);
                                    ServiceLogger.Debug(
                                        $"设备 {device.Name} 历史事件补偿启动重试失败 | " +
                                        $"Id={device.Id}, Mode=BeginSerial=0, Err={retryErr}{(string.IsNullOrWhiteSpace(retryErrMsg) ? string.Empty : $"({retryErrMsg})")}, {BuildAcsEventCondSummary(cond)}");
                                }
                            }

                            if (handle < 0)
                            {
                                // 兼容策略：部分设备不支持补偿带图，尝试关闭图片开关。
                                if (cond.byPicEnable != 0)
                                {
                                    cond.byPicEnable = 0;
                                    ServiceLogger.Debug(
                                        $"设备 {device.Name} 历史事件补偿启动重试 | " +
                                        $"Id={device.Id}, Mode=PicEnable=0, {BuildAcsEventCondSummary(cond)}");
                                    Marshal.StructureToPtr(cond, ptrCond, false);
                                    retryHandle = HCNetSDK.NET_DVR_StartRemoteConfig(device.UserID, HCNetSDK.NET_DVR_GET_ACS_EVENT, ptrCond, size, null, IntPtr.Zero);
                                    if (retryHandle >= 0)
                                    {
                                        handle = retryHandle;
                                        startMode = "PicEnable=0";
                                    }
                                    else
                                    {
                                        uint retryErr = HCNetSDK.NET_DVR_GetLastError();
                                        string retryErrMsg = GetSdkErrorMessage(retryErr);
                                        ServiceLogger.Debug(
                                            $"设备 {device.Name} 历史事件补偿启动重试失败 | " +
                                            $"Id={device.Id}, Mode=PicEnable=0, Err={retryErr}{(string.IsNullOrWhiteSpace(retryErrMsg) ? string.Empty : $"({retryErrMsg})")}, {BuildAcsEventCondSummary(cond)}");
                                    }
                                }
                            }

                            if (handle < 0)
                            {
                                // 兼容策略：部分设备对 dwMajor 的校验更严格（或仅支持 0-全部）。
                                if (cond.dwMajor != 0)
                                {
                                    cond.dwMajor = 0;
                                    ServiceLogger.Debug(
                                        $"设备 {device.Name} 历史事件补偿启动重试 | " +
                                        $"Id={device.Id}, Mode=Major=0, {BuildAcsEventCondSummary(cond)}");
                                    Marshal.StructureToPtr(cond, ptrCond, false);
                                    retryHandle = HCNetSDK.NET_DVR_StartRemoteConfig(device.UserID, HCNetSDK.NET_DVR_GET_ACS_EVENT, ptrCond, size, null, IntPtr.Zero);
                                    if (retryHandle >= 0)
                                    {
                                        handle = retryHandle;
                                        startMode = "Major=0";
                                    }
                                    else
                                    {
                                        uint retryErr = HCNetSDK.NET_DVR_GetLastError();
                                        string retryErrMsg = GetSdkErrorMessage(retryErr);
                                        ServiceLogger.Debug(
                                            $"设备 {device.Name} 历史事件补偿启动重试失败 | " +
                                            $"Id={device.Id}, Mode=Major=0, Err={retryErr}{(string.IsNullOrWhiteSpace(retryErrMsg) ? string.Empty : $"({retryErrMsg})")}, {BuildAcsEventCondSummary(cond)}");
                                    }
                                }
                            }

                            if (handle < 0)
                            {
                                // 退回保留值 SearchType=0（尽量兼容旧固件/差异固件）。
                                cond.bySearchType = 0;
                                cond.dwIOTChannelNo = 0;
                                ServiceLogger.Debug(
                                    $"设备 {device.Name} 历史事件补偿启动重试 | " +
                                    $"Id={device.Id}, Mode=SearchType=0, {BuildAcsEventCondSummary(cond)}");
                                Marshal.StructureToPtr(cond, ptrCond, false);
                                retryHandle = HCNetSDK.NET_DVR_StartRemoteConfig(device.UserID, HCNetSDK.NET_DVR_GET_ACS_EVENT, ptrCond, size, null, IntPtr.Zero);
                                if (retryHandle >= 0)
                                {
                                    handle = retryHandle;
                                    startMode = "SearchType=0";
                                }
                                else
                                {
                                    uint retryErr = HCNetSDK.NET_DVR_GetLastError();
                                    string retryErrMsg = GetSdkErrorMessage(retryErr);
                                    ServiceLogger.Debug(
                                        $"设备 {device.Name} 历史事件补偿启动重试失败 | " +
                                        $"Id={device.Id}, Mode=SearchType=0, Err={retryErr}{(string.IsNullOrWhiteSpace(retryErrMsg) ? string.Empty : $"({retryErrMsg})")}, {BuildAcsEventCondSummary(cond)}");
                                }
                            }

                            if (handle < 0)
                            {
                                // 全部尝试失败，输出最终失败日志（包含关键入参）。
                                err = HCNetSDK.NET_DVR_GetLastError();
                                errMsg = GetSdkErrorMessage(err);
                                DateTime startLocal = ConvertToDateTime(cond.struStartTime);
                                DateTime endLocal = ConvertToDateTime(cond.struEndTime);
                                ServiceLogger.Error(
                                    $"设备 {device.Name} 历史事件补偿启动失败，错误码: {err}{(string.IsNullOrWhiteSpace(errMsg) ? string.Empty : $"({errMsg})")} | " +
                                    $"Id={device.Id}, IP={device.IpAddress}, UserID={device.UserID}, " +
                                    $"Major={cond.dwMajor}, Minor={cond.dwMinor}, BeginSerial={cond.dwBeginSerialNo}, EndSerial={cond.dwEndSerialNo}, " +
                                    $"PicEnable={cond.byPicEnable}, TimeType={cond.byTimeType}, SearchType={cond.bySearchType}, IOTChannelNo={cond.dwIOTChannelNo}, EventAttr={cond.byEventAttribute}, " +
                                    $"Start={startLocal:yyyy-MM-dd HH:mm:ss.fff}, End={endLocal:yyyy-MM-dd HH:mm:ss.fff}, InSize={size}, StructSize={cond.dwSize}, IntPtrSize={IntPtr.Size}");
                                return;
                            }
                        }

                        remoteConfigHandles[device.Id] = handle;
                        string startedLog =
                            $"设备 {device.Name} 历史事件补偿通道已启动 | " +
                            $"Id={device.Id}, Handle={handle}, Mode={startMode}, {BuildAcsEventCondSummary(cond)}, InSize={size}, OutSize={AcsEventCfgSize}";
                        if (ServiceLogger.IsVerboseEnabled)
                        {
                            ServiceLogger.Verbose(startedLog);
                        }
                        else
                        {
                            ServiceLogger.Info(startedLog);
                        }

                        int cfgSize = AcsEventCfgSize;
                        outPtr = Marshal.AllocHGlobal(cfgSize);

                        while (!token.IsCancellationRequested)
                        {
                            int status = HCNetSDK.NET_DVR_GetNextRemoteConfig(handle, outPtr, (uint)cfgSize);
                            if (status == (int)HCNetSDK.NET_SDK_SENDWITHRECV_STATUS.NET_SDK_CONFIG_STATUS_SUCCESS)
                            {
                                var cfg = Marshal.PtrToStructure<HCNetSDK.NET_DVR_ACS_EVENT_CFG>(outPtr);
                                receivedCount++;
                                LogAcsEventCfgSummary(device, handle, cfg);
                                if (!IsFaceVerifyMinor(cfg.dwMinor))
                                {
                                    filteredCount++;
                                    continue;
                                }

                                FaceEventRecord record = BuildRecordFromConfig(cfg, device);
                                if (record != null)
                                {
                                    Enqueue(record);
                                    enqueuedCount++;
                                }
                                else
                                {
                                    nullRecordCount++;
                                }
                            }
                            else if (status == (int)HCNetSDK.NET_SDK_SENDWITHRECV_STATUS.NET_SDK_CONFIG_STATUS_FINISH)
                            {
                                break;
                            }
                            else if (status == (int)HCNetSDK.NET_SDK_SENDWITHRECV_STATUS.NET_SDK_CONFIG_STATUS_NEEDWAIT)
                            {
                                needWaitCount++;
                                if (ServiceLogger.IsVerboseEnabled && needWaitCount % 20 == 1)
                                {
                                    ServiceLogger.Verbose(
                                        $"设备 {device.Name} 补偿通道需要等待 | " +
                                        $"Id={device.Id}, Handle={handle}, NeedWaitCount={needWaitCount}, Received={receivedCount}, Enqueued={enqueuedCount}");
                                }
                                Thread.Sleep(200);
                            }
                            else if (status == (int)HCNetSDK.NET_SDK_SENDWITHRECV_STATUS.NET_SDK_CONFIG_STATUS_FAILED)
                            {
                                failedCount++;
                                uint err = HCNetSDK.NET_DVR_GetLastError();
                                string errMsg = GetSdkErrorMessage(err);
                                ServiceLogger.Warn(
                                    $"设备 {device.Name} 补偿通道获取失败，将重试 | " +
                                    $"Id={device.Id}, Handle={handle}, Status={status}, FailedCount={failedCount}, Err={err}{(string.IsNullOrWhiteSpace(errMsg) ? string.Empty : $"({errMsg})")}");
                                Thread.Sleep(200);
                            }
                            else if (status == (int)HCNetSDK.NET_SDK_SENDWITHRECV_STATUS.NET_SDK_CONFIG_STATUS_EXCEPTION)
                            {
                                exceptionStatusCount++;
                                uint err = HCNetSDK.NET_DVR_GetLastError();
                                string errMsg = GetSdkErrorMessage(err);
                                ServiceLogger.Warn(
                                    $"设备 {device.Name} 补偿通道异常，终止补偿 | " +
                                    $"Id={device.Id}, Handle={handle}, Status={status}, ExceptionCount={exceptionStatusCount}, Err={err}{(string.IsNullOrWhiteSpace(errMsg) ? string.Empty : $"({errMsg})")}");
                                break;
                            }
                            else
                            {
                                unknownStatusCount++;
                                uint err = HCNetSDK.NET_DVR_GetLastError();
                                string errMsg = GetSdkErrorMessage(err);
                                ServiceLogger.Warn(
                                    $"设备 {device.Name} 补偿通道返回未知状态，终止补偿 | " +
                                    $"Id={device.Id}, Handle={handle}, Status={status}, UnknownCount={unknownStatusCount}, Err={err}{(string.IsNullOrWhiteSpace(errMsg) ? string.Empty : $"({errMsg})")}");
                                break;
                            }
                        }
                    }
                    finally
                    {
                        stopwatch.Stop();
                        string finishedLog =
                            $"设备 {device.Name} 历史事件补偿结束 | " +
                            $"Id={device.Id}, Handle={handle}, " +
                            $"Received={receivedCount}, Filtered={filteredCount}, Enqueued={enqueuedCount}, NullRecord={nullRecordCount}, " +
                            $"NeedWait={needWaitCount}, Failed={failedCount}, Exception={exceptionStatusCount}, Unknown={unknownStatusCount}, " +
                            $"ElapsedMs={stopwatch.ElapsedMilliseconds}";
                        if (ServiceLogger.IsVerboseEnabled)
                        {
                            ServiceLogger.Verbose(finishedLog);
                        }
                        else
                        {
                            ServiceLogger.Debug(finishedLog);
                        }

                        if (ptrCond != IntPtr.Zero)
                        {
                            Marshal.FreeHGlobal(ptrCond);
                        }

                        if (outPtr != IntPtr.Zero)
                        {
                            Marshal.FreeHGlobal(outPtr);
                        }

                        if (handle >= 0)
                        {
                            remoteConfigHandles.TryRemove(device.Id, out _);
                            HCNetSDK.NET_DVR_StopRemoteConfig(handle);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ServiceLogger.Error($"设备 {device?.Name} 补偿流程异常。", ex);
            }
        }

        private static void LogAcsInteropLayoutOnce()
        {
            if (!ServiceLogger.IsVerboseEnabled)
            {
                return;
            }

            if (Interlocked.Exchange(ref acsInteropLayoutLogged, 1) != 0)
            {
                return;
            }

            try
            {
                ServiceLogger.Verbose(
                    $"[Interop] ACS结构体布局 | " +
                    $"Proc64={Environment.Is64BitProcess}, IntPtrSize={IntPtr.Size}, " +
                    $"AcsAlarmInfoSize={AcsAlarmInfoSize}, AcsEventCondSize={AcsEventCondSize}, AcsEventDetailSize={AcsEventDetailSize}, AcsEventCfgSize={AcsEventCfgSize}");

                LogStructLayout(
                    typeof(HCNetSDK.NET_DVR_ACS_EVENT_COND),
                    "NET_DVR_ACS_EVENT_COND",
                    "dwSize",
                    "dwMajor",
                    "dwMinor",
                    "struStartTime",
                    "struEndTime",
                    "byPicEnable",
                    "byTimeType",
                    "dwBeginSerialNo",
                    "dwEndSerialNo",
                    "bySearchType",
                    "byEventAttribute",
                    "byEmployeeNo");

                LogStructLayout(
                    typeof(HCNetSDK.NET_DVR_ACS_EVENT_DETAIL),
                    "NET_DVR_ACS_EVENT_DETAIL",
                    "dwSize",
                    "byCardNo",
                    "dwCardReaderNo",
                    "dwDoorNo",
                    "dwEmployeeNo",
                    "dwSerialNo",
                    "dwRecordChannelNum",
                    "pRecordChannelData",
                    "byEmployeeNo");

                LogStructLayout(
                    typeof(HCNetSDK.NET_DVR_ACS_EVENT_CFG),
                    "NET_DVR_ACS_EVENT_CFG",
                    "dwSize",
                    "dwMajor",
                    "dwMinor",
                    "struTime",
                    "struRemoteHostAddr",
                    "struAcsEventInfo",
                    "dwPicDataLen",
                    "pPicData",
                    "wInductiveEventType",
                    "byTimeType",
                    "dwQCodeInfoLen",
                    "pQRCodeInfo");
            }
            catch (Exception ex)
            {
                ServiceLogger.Verbose($"[Interop] ACS结构体布局输出失败: {ex}");
            }
        }

        private static void LogStructLayout(Type type, string typeName, params string[] fieldNames)
        {
            if (type == null)
            {
                return;
            }

            int size = -1;
            try
            {
                size = Marshal.SizeOf(type);
            }
            catch
            {
            }

            ServiceLogger.Verbose($"[Interop] {typeName} | Size={size}");

            if (fieldNames == null || fieldNames.Length == 0)
            {
                return;
            }

            foreach (string fieldName in fieldNames)
            {
                if (string.IsNullOrWhiteSpace(fieldName))
                {
                    continue;
                }

                try
                {
                    int offset = (int)Marshal.OffsetOf(type, fieldName);
                    ServiceLogger.Verbose($"[Interop] {typeName}.{fieldName} | Offset={offset}");
                }
                catch (Exception ex)
                {
                    ServiceLogger.Verbose($"[Interop] {typeName}.{fieldName} | Offset读取失败: {ex.Message}");
                }
            }
        }

        private static string BuildAcsEventCondSummary(HCNetSDK.NET_DVR_ACS_EVENT_COND cond)
        {
            return
                $"Major={cond.dwMajor}, Minor={cond.dwMinor}, BeginSerial={cond.dwBeginSerialNo}, EndSerial={cond.dwEndSerialNo}, " +
                $"PicEnable={cond.byPicEnable}, TimeType={cond.byTimeType}, SearchType={cond.bySearchType}, IOTChannelNo={cond.dwIOTChannelNo}, " +
                $"InductiveType={cond.wInductiveEventType}, EventAttr={cond.byEventAttribute}";
        }

        private static void LogAcsEventCfgSummary(DeviceConnectionInfo device, int handle, HCNetSDK.NET_DVR_ACS_EVENT_CFG cfg)
        {
            if (!ServiceLogger.IsVerboseEnabled)
            {
                return;
            }

            try
            {
                var detail = cfg.struAcsEventInfo;

                DateTime eventTime = ConvertToDateTime(cfg.struTime);
                if (cfg.byTimeType == 1)
                {
                    eventTime = DateTime.SpecifyKind(eventTime, DateTimeKind.Utc).ToLocalTime();
                }

                string employeeNo = DecodeFixedAsciiString(detail.byEmployeeNo);
                string cardNo = DecodeFixedAsciiString(detail.byCardNo);

                ServiceLogger.Verbose(
                    $"设备 {device?.Name} 获取历史事件 | " +
                    $"Id={device?.Id ?? 0}, Handle={handle}, Major=0x{cfg.dwMajor:X}, Minor=0x{cfg.dwMinor:X}, " +
                    $"Time={eventTime:yyyy-MM-dd HH:mm:ss.fff}, SerialNo={detail.dwSerialNo}, " +
                    $"DoorNo={detail.dwDoorNo}, ReaderNo={detail.dwCardReaderNo}, VerifyNo={detail.dwVerifyNo}, " +
                    $"EmployeeNo={employeeNo}, EmployeeNo(dw)={detail.dwEmployeeNo}, CardNo={cardNo}, CardType={detail.byCardType}, " +
                    $"PicLen={cfg.dwPicDataLen}, PicPtr=0x{cfg.pPicData.ToInt64():X}, " +
                    $"RecordChannelNum={detail.dwRecordChannelNum}, RecordChannelPtr=0x{detail.pRecordChannelData.ToInt64():X}");

                if (cfg.dwPicDataLen > 0 && cfg.pPicData == IntPtr.Zero)
                {
                    ServiceLogger.Warn(
                        $"设备 {device?.Name} 事件图片长度不为0但图片指针为空，可能存在结构体不匹配 | " +
                        $"Id={device?.Id ?? 0}, Handle={handle}, PicLen={cfg.dwPicDataLen}");
                }

                if (detail.dwRecordChannelNum > 0 && detail.pRecordChannelData == IntPtr.Zero)
                {
                    ServiceLogger.Warn(
                        $"设备 {device?.Name} 录像通道数不为0但录像通道指针为空，可能存在结构体不匹配 | " +
                        $"Id={device?.Id ?? 0}, Handle={handle}, RecordChannelNum={detail.dwRecordChannelNum}");
                }
            }
            catch (Exception ex)
            {
                ServiceLogger.Verbose($"设备 {device?.Name} 输出历史事件摘要失败: {ex.Message}");
            }
        }

        private static string GetSdkErrorMessage(uint errorCode)
        {
            try
            {
                int err = unchecked((int)errorCode);
                IntPtr msgPtr = HCNetSDK.NET_DVR_GetErrorMsg(ref err);
                if (msgPtr == IntPtr.Zero)
                {
                    return null;
                }

                string msg = Marshal.PtrToStringAnsi(msgPtr);
                return string.IsNullOrWhiteSpace(msg) ? null : msg.Trim();
            }
            catch
            {
                return null;
            }
        }

        private FaceEventRecord BuildRecordFromConfig(HCNetSDK.NET_DVR_ACS_EVENT_CFG cfg, DeviceConnectionInfo device)
        {
            var detail = cfg.struAcsEventInfo;

            FaceEventType eventType = cfg.dwMinor == HCNetSDK.MINOR_FACE_VERIFY_FAIL
                ? FaceEventType.Fail
                : FaceEventType.Pass;

            DateTime eventTime = ConvertToDateTime(cfg.struTime);
            if (cfg.byTimeType == 1)
            {
                eventTime = DateTime.SpecifyKind(eventTime, DateTimeKind.Utc).ToLocalTime();
            }

            string rawEmployeeNo = DecodeFixedAsciiString(detail.byEmployeeNo);
            string employeeNo = rawEmployeeNo;
            bool employeeFromDw = false;
            if (string.IsNullOrWhiteSpace(employeeNo) && detail.dwEmployeeNo != 0)
            {
                employeeNo = detail.dwEmployeeNo.ToString();
                employeeFromDw = true;
            }

            if (ServiceLogger.IsVerboseEnabled)
            {
                ServiceLogger.Verbose(
                    $"设备 {device?.Name} 解析历史事件字段 | " +
                    $"Id={device?.Id ?? 0}, Minor=0x{cfg.dwMinor:X}, EventTime={eventTime:yyyy-MM-dd HH:mm:ss.fff}, " +
                    $"EmployeeRaw={rawEmployeeNo}, EmployeeFinal={employeeNo}, EmployeeFromDw={employeeFromDw}, " +
                    $"SerialNo={detail.dwSerialNo}, PicLen={cfg.dwPicDataLen}");
            }

            byte[] snapshot = null;
            string snapshotUrl = null;

            if (cfg.dwPicDataLen > 0 && cfg.pPicData != IntPtr.Zero)
            {
                byte[] data = new byte[cfg.dwPicDataLen];
                Marshal.Copy(cfg.pPicData, data, 0, (int)cfg.dwPicDataLen);

                if (TryParseSnapshotUrl(data, out string url))
                {
                    snapshotUrl = url;
                    if (ServiceLogger.IsVerboseEnabled)
                    {
                        ServiceLogger.Verbose(
                            $"设备 {device?.Name} 快照解析为URL | " +
                            $"Id={device?.Id ?? 0}, SerialNo={detail.dwSerialNo}, Url={snapshotUrl}");
                    }
                }
                else
                {
                    snapshot = data;
                    if (ServiceLogger.IsVerboseEnabled)
                    {
                        ServiceLogger.Verbose(
                            $"设备 {device?.Name} 快照解析为二进制 | " +
                            $"Id={device?.Id ?? 0}, SerialNo={detail.dwSerialNo}, Bytes={snapshot.Length}");
                    }
                }
            }

            if (ServiceLogger.IsVerboseEnabled)
            {
                ServiceLogger.Verbose(
                    $"设备 {device?.Name} 构建入库事件记录 | " +
                    $"Id={device?.Id ?? 0}, UserId={employeeNo}, EventType={eventType}, EventTime={eventTime:yyyy-MM-dd HH:mm:ss.fff}, " +
                    $"SerialNo={detail.dwSerialNo}, SnapshotUrl={(string.IsNullOrWhiteSpace(snapshotUrl) ? "-" : snapshotUrl)}, SnapshotBytes={(snapshot == null ? 0 : snapshot.Length)}");
            }

            return new FaceEventRecord
            {
                EventType = eventType,
                EventTime = eventTime,
                UserId = employeeNo ?? string.Empty,
                DeviceId = device?.Id ?? 0,
                DeviceName = device != null ? device.Name : device?.IpAddress,
                DeviceSerialNumber = device?.SerialNumber,
                DeviceIP = device?.IpAddress,
                VerifyMode = $"0x{cfg.dwMinor:X}",
                SerialNo = detail.dwSerialNo,
                Snapshot = snapshot,
                SnapshotUrl = snapshotUrl
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

            using (var sdkLock = device.TryAcquireDeviceSdkLock(
                deviceSdkLockTimeoutMs,
                $"StopRemoteConfig-{device.Id}"))
            {
                if (!sdkLock.IsAcquired)
                {
                    ServiceLogger.Warn($"设备 {device.Name} 获取设备SDK锁超时，跳过停止远程配置。" );
                    return;
                }

                if (remoteConfigHandles.TryRemove(device.Id, out int handle))
                {
                    HCNetSDK.NET_DVR_StopRemoteConfig(handle);
                }
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

        private static string TryGetEmployeeNoFromExtend(HCNetSDK.NET_DVR_ACS_ALARM_INFO info)
        {
            if (info.byAcsEventInfoExtend != 1 || info.pAcsEventInfoExtend == IntPtr.Zero)
            {
                return null;
            }

            try
            {
                var extend = Marshal.PtrToStructure<HCNetSDK.NET_DVR_ACS_EVENT_INFO_EXTEND>(info.pAcsEventInfoExtend);

                string employeeNo = DecodeFixedAsciiString(extend.byEmployeeNo);
                return string.IsNullOrWhiteSpace(employeeNo) ? null : employeeNo;
            }
            catch
            {
                return null;
            }
        }

        private static string DecodeFixedAsciiString(byte[] data)
        {
            if (data == null || data.Length == 0)
            {
                return string.Empty;
            }

            return Encoding.ASCII.GetString(data).TrimEnd('\0').Trim();
        }

        private static string ReadStringFromPtr(IntPtr buffer, uint length)
        {
            if (buffer == IntPtr.Zero)
            {
                return null;
            }

            uint actualLength = length;

            if (actualLength == 0)
            {
                // 最多读取 4096 字节，避免异常指针导致的无限循环
                const int maxScan = 4096;
                int i = 0;
                for (; i < maxScan; i++)
                {
                    if (Marshal.ReadByte(buffer, i) == 0)
                    {
                        break;
                    }
                }

                actualLength = (uint)i;
            }

            if (actualLength == 0)
            {
                return string.Empty;
            }

            byte[] data = new byte[actualLength];
            Marshal.Copy(buffer, data, 0, (int)actualLength);
            return Encoding.UTF8.GetString(data).TrimEnd('\0').Trim();
        }

        private static bool TryParseSnapshotUrl(byte[] data, out string url)
        {
            url = null;

            if (data == null || data.Length == 0)
            {
                return false;
            }

            // URL 一般较短；若数据过大，基本可以认为是二进制图片
            if (data.Length > 4096)
            {
                return false;
            }

            string text = Encoding.UTF8.GetString(data).TrimEnd('\0').Trim();
            if (string.IsNullOrWhiteSpace(text))
            {
                return false;
            }

            if (text.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
                || text.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                url = text;
                return true;
            }

            return false;
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
            Interlocked.Exchange(ref stopping, 1);

            try
            {
                DeviceConnectionManager.Instance.DeviceConnectionStateChanged -= OnDeviceConnectionStateChanged;
            }
            catch
            {
            }

            // 停止设备侧生产者（补偿任务/远程配置轮询等）
            cancellation.Cancel();

            foreach (var kvp in compensationTokens)
            {
                try
                {
                    kvp.Value.Cancel();
                }
                catch
                {
                }
                finally
                {
                    kvp.Value.Dispose();
                }
            }

            compensationTokens.Clear();

            foreach (var device in DeviceConnectionManager.Instance.GetAllDevices())
            {
                CloseAlarm(device);
                StopRemoteConfig(device);
            }

            // 完成队列：触发消费者自然 drain
            try
            {
                eventQueue.CompleteAdding();
            }
            catch
            {
            }

            int flushTimeoutSeconds = options?.ShutdownFlushTimeoutSeconds > 0
                ? options.ShutdownFlushTimeoutSeconds
                : 30;
            flushTimeoutSeconds = Math.Min(Math.Max(1, flushTimeoutSeconds), 600);
            TimeSpan flushTimeout = TimeSpan.FromSeconds(flushTimeoutSeconds);

            try
            {
                // 超时后取消写入重试，确保停机可控
                persistCancellation.CancelAfter(flushTimeout);
            }
            catch
            {
            }

            try
            {
                int waitSeconds = Math.Min(600, flushTimeoutSeconds + commandTimeoutSeconds + 5);
                Task.WaitAll(workers.ToArray(), TimeSpan.FromSeconds(waitSeconds));
            }
            catch
            {
            }

            try
            {
                persistCancellation.Cancel();
            }
            catch
            {
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
            public string SnapshotUrl { get; set; }
        }
    }
}
