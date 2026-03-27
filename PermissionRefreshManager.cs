using System.Data.SqlClient;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace ControlEntradaSalida
{
    public class PermissionRefreshManager
    {
        private static readonly string DefaultBeginTime = "2022-01-01T00:00:00";
        private static readonly string DefaultEndTime = "2035-12-31T23:59:59";
        private const string UserInfoSetupUrl = "PUT /ISAPI/AccessControl/UserInfo/SetUp?format=json";
        private const string UserInfoDeleteUrl = "PUT /ISAPI/AccessControl/UserInfo/Delete?format=json";
        private const string FaceSetupUrl = "PUT /ISAPI/Intelligent/FDLib/FDSetUp?format=json";
        private const string FaceDeleteUrl = "PUT /ISAPI/Intelligent/FDLib/FDSearch/Delete?format=json&FDID=1&faceLibType=blackFD";
        private const string FaceSearchUrl = "POST /ISAPI/Intelligent/FDLib/FDSearch?format=json";
        private const string EnrollmentDeviceName = "人脸录入仪";
        private const string DefaultFaceLibType = "blackFD";
        private const string DefaultFaceLibId = "1";
        private const string UserVerifyModeFace = "face";
        private const int MaxFaceImageBytes = 200 * 1024;


        private readonly int deviceSdkLockTimeoutMs;
        private readonly DeviceConnectionManager deviceManager;
        private readonly Common commonHelper;
        private readonly DeviceOperationRetryStore retryStore;
        private readonly ServiceConfiguration.DeviceOperationRetryOptions retryOptions;
        private readonly object refreshLock = new object();
        private readonly object personSyncLock = new object();

        public PermissionRefreshManager(DeviceOperationRetryStore retryStore = null)
        {
            deviceManager = DeviceConnectionManager.Instance;
            commonHelper = new Common();
            this.retryStore = retryStore;
            retryOptions = ServiceConfiguration.Current.DeviceOperationRetry;

            try
            {
                deviceSdkLockTimeoutMs = ServiceConfiguration.Current.DeviceConnection?.DeviceSdkLockTimeoutMs ?? 30000;
            }
            catch
            {
                deviceSdkLockTimeoutMs = 30000;
            }
        }

        private T ExecuteWithDeviceSdkLock<T>(DeviceConnectionInfo device, string operationName, Func<T> action, Func<T> timeoutResult)
        {
            if (device == null)
            {
                return timeoutResult();
            }

            using (var sdkLock = device.TryAcquireDeviceSdkLock(deviceSdkLockTimeoutMs, operationName))
            {
                if (!sdkLock.IsAcquired)
                {
                    return timeoutResult();
                }

                return action();
            }
        }

        /// <summary>
        /// 判断设备是否为人脸录入仪设备。
        /// 人脸录入仪仅用于连接和人脸录入功能，不参与人员信息、权限等数据的下发。
        /// </summary>
        /// <param name="device">设备连接信息</param>
        /// <returns>如果是人脸录入仪则返回 true</returns>
        private static bool IsEnrollmentDevice(DeviceConnectionInfo device)
        {
            return IsEnrollmentDevice(device?.Name);
        }

        /// <summary>
        /// 判断设备名称是否为人脸录入仪设备。
        /// </summary>
        /// <param name="deviceName">设备名称</param>
        /// <returns>如果是人脸录入仪则返回 true</returns>
        private static bool IsEnrollmentDevice(string deviceName)
        {
            return string.Equals(deviceName, EnrollmentDeviceName, StringComparison.OrdinalIgnoreCase);
        }

        public PermissionRefreshSummary RefreshAllPermissions()
        {
            lock (refreshLock)
            {
                PermissionRefreshSummary summary = new PermissionRefreshSummary();

                EnsureDevicesLoaded();

                List<DeviceAreaInfo> devices = LoadActiveDevices();
                if (devices.Count == 0)
                {
                    summary.Errors.Add("未找到可用的门禁设备，无法刷新权限。");
                    return summary;
                }

                List<UserPermissionRecord> users = LoadUserPermissions();
                summary.TotalUsers = users.Count;

                foreach (UserPermissionRecord user in users)
                {
                    if (user.PermissionLevel < 0 || user.PermissionLevel > 2)
                    {
                        summary.UsersFailed++;
                        summary.Errors.Add(string.Format(CultureInfo.InvariantCulture,
                            "用户 {0} 的权限级别 {1} 无效，应为 0-2。", user.EmployeeId, user.PermissionLevel));
                        continue;
                    }

                    if (user.LastSyncedLevel.HasValue && user.LastSyncedLevel.Value == user.PermissionLevel)
                    {
                        summary.UsersSkipped++;
                        continue;
                    }

                    RefreshResult result = ApplyPermissionToDevices(user, devices);
                    if (result.Success)
                    {
                        if (result.HasQueued)
                        {
                            summary.QueuedCount++;
                            summary.QueuedDetails.AddRange(result.QueuedDetails);
                            continue;
                        }

                        bool updated = CompletePermissionSyncIfNoPending(user.EmployeeId, user.PermissionLevel);
                        if (!updated)
                        {
                            summary.UsersFailed++;
                            summary.Errors.Add(string.Format(CultureInfo.InvariantCulture,
                                "更新员工 {0} 的权限同步标记失败。", user.EmployeeId));
                            continue;
                        }

                        summary.UsersUpdated++;
                    }
                    else
                    {
                        summary.UsersFailed++;
                        summary.Errors.AddRange(result.Errors);
                        summary.ErrorDetails.AddRange(result.ErrorDetails);
                    }
                }

                return summary;
            }
        }

        public PermissionRefreshSummary RefreshPermissionsForEmployees(IEnumerable<PermissionUpdateInfo> updates)
        {
            if (updates == null)
            {
                throw new ArgumentNullException(nameof(updates));
            }

            List<PermissionUpdateInfo> distinctUpdates = updates
                .GroupBy(u => u.EmployeeId, StringComparer.OrdinalIgnoreCase)
                .Select(g => g.Last())
                .ToList();

            PermissionRefreshSummary summary = new PermissionRefreshSummary
            {
                TotalUsers = distinctUpdates.Count
            };

            if (distinctUpdates.Count == 0)
            {
                summary.Errors.Add("未提供任何需要更新的员工。");
                summary.UsersFailed = 0;
                return summary;
            }

            lock (refreshLock)
            {
                EnsureDevicesLoaded();

                List<DeviceAreaInfo> devices = LoadActiveDevices();
                if (devices.Count == 0)
                {
                    summary.Errors.Add("未找到可用的门禁设备，无法刷新权限。");
                    summary.UsersFailed = summary.TotalUsers;
                    return summary;
                }

                foreach (PermissionUpdateInfo update in distinctUpdates)
                {
                    if (update.PermissionCode < 0 || update.PermissionCode > 2)
                    {
                        summary.UsersFailed++;
                        string errorMessage = string.Format(CultureInfo.InvariantCulture,
                            "用户 {0} 的权限级别 {1} 无效，应为 0-2。",
                            update.EmployeeId, update.PermissionCode);
                        summary.Errors.Add(errorMessage);
                        summary.ErrorDetails.Add(new GrpcErrorDetail
                        {
                            EmployeeId = update.EmployeeId,
                            Code = GrpcErrorCodes.InvalidArgument,
                            Message = errorMessage
                        });
                        continue;
                    }

                    bool permissionStored = UpdatePermissionLevel(update.EmployeeId, update.PermissionCode);
                    if (!permissionStored)
                    {
                        summary.UsersFailed++;
                        string errorMessage = string.Format(CultureInfo.InvariantCulture,
                            "更新员工 {0} 在数据库中的权限失败，可能不存在该员工。", update.EmployeeId);
                        summary.Errors.Add(errorMessage);
                        summary.ErrorDetails.Add(new GrpcErrorDetail
                        {
                            EmployeeId = update.EmployeeId,
                            Code = GrpcErrorCodes.DbError,
                            Message = errorMessage
                        });
                        continue;
                    }

                    UserPermissionRecord userRecord = LoadUserPermission(update.EmployeeId);
                    if (userRecord == null)
                    {
                        summary.UsersFailed++;
                        string errorMessage = string.Format(CultureInfo.InvariantCulture,
                            "未找到员工 {0} 的详细信息。", update.EmployeeId);
                        summary.Errors.Add(errorMessage);
                        summary.ErrorDetails.Add(new GrpcErrorDetail
                        {
                            EmployeeId = update.EmployeeId,
                            Code = GrpcErrorCodes.DbError,
                            Message = errorMessage
                        });
                        continue;
                    }

                    userRecord.PermissionLevel = update.PermissionCode;

                    if (userRecord.LastSyncedLevel.HasValue &&
                        userRecord.LastSyncedLevel.Value == update.PermissionCode)
                    {
                        summary.UsersSkipped++;
                        continue;
                    }

                    RefreshResult result = ApplyPermissionToDevices(userRecord, devices);
                    if (result.Success)
                    {
                        if (result.HasQueued)
                        {
                            summary.QueuedCount++;
                            summary.QueuedDetails.AddRange(result.QueuedDetails);
                            continue;
                        }

                        bool synced = CompletePermissionSyncIfNoPending(userRecord.EmployeeId, update.PermissionCode);
                        if (!synced)
                        {
                            summary.UsersFailed++;
                            string errorMessage = string.Format(CultureInfo.InvariantCulture,
                                "更新员工 {0} 的权限同步标记失败。", userRecord.EmployeeId);
                            summary.Errors.Add(errorMessage);
                            summary.ErrorDetails.Add(new GrpcErrorDetail
                            {
                                EmployeeId = userRecord.EmployeeId,
                                Code = GrpcErrorCodes.DbError,
                                Message = errorMessage
                            });
                            continue;
                        }

                        summary.UsersUpdated++;
                    }
                    else
                    {
                        summary.UsersFailed++;
                        summary.Errors.AddRange(result.Errors);
                        summary.ErrorDetails.AddRange(result.ErrorDetails);
                    }
                }
            }

            return summary;
        }

        public PersonSyncSummary SyncPersonsToConnectedDevices(IEnumerable<PersonSyncRequest> persons)
        {
            if (persons == null)
            {
                throw new ArgumentNullException(nameof(persons));
            }

            List<PersonSyncRequest> sanitized = new List<PersonSyncRequest>();
            int skippedWithoutId = 0;
            foreach (PersonSyncRequest person in persons)
            {
                if (person == null)
                {
                    continue;
                }

                string employeeId = person.EmployeeId?.Trim();
                if (string.IsNullOrWhiteSpace(employeeId))
                {
                    ServiceLogger.Warn("人员下发请求缺少 employee_id，已跳过。");
                    skippedWithoutId++;
                    continue;
                }

                person.EmployeeId = employeeId;
                person.FullName = person.FullName?.Trim();
                person.Gender = NormalizeGender(person.Gender);
                sanitized.Add(person);
            }

            List<PersonSyncRequest> requests = sanitized
                .GroupBy(p => p.EmployeeId, StringComparer.OrdinalIgnoreCase)
                .Select(g => g.Last())
                .ToList();

            PersonSyncSummary summary = new PersonSyncSummary
            {
                TotalPersons = requests.Count
            };

            if (requests.Count == 0)
            {
                if (skippedWithoutId > 0)
                {
                    summary.Errors.Add(string.Format(CultureInfo.InvariantCulture,
                        "有 {0} 条人员记录缺少 employee_id，已跳过。",
                        skippedWithoutId));
                }

                summary.Errors.Add("未提供任何有效的人员信息。");
                return summary;
            }

            lock (personSyncLock)
            {
                List<DeviceConnectionInfo> devices = GetEnabledGateDevices();
                summary.TargetDevices = devices.Count;

                if (devices.Count == 0)
                {
                    summary.Errors.Add("未找到可用的门禁设备，无法下发人员信息。");
                    summary.FailedPersons = summary.TotalPersons;
                    return summary;
                }

                if (skippedWithoutId > 0)
                {
                    summary.Errors.Add(string.Format(CultureInfo.InvariantCulture,
                        "有 {0} 条人员记录缺少 employee_id，已跳过。",
                        skippedWithoutId));
                }

                foreach (PersonSyncRequest request in requests)
                {
                    bool hardFailed = false;
                    bool queued = false;

                    foreach (DeviceConnectionInfo device in devices)
                    {
                        DeviceUpdateResult result = UpsertPersonOnDevice(device, request);
                        if (result.Success)
                        {
                            ClearPersonRetryState(device.Id, request.EmployeeId, request.HasFace);
                            if (request.HasFace)
                            {
                                summary.FacesUploaded++;
                            }

                            continue;
                        }

                        if (result.IsRetryable && TryQueuePersonRetry(device, request, result.ErrorMessage, summary.QueuedDetails))
                        {
                            queued = true;
                            continue;
                        }

                        hardFailed = true;
                        summary.Errors.Add(result.ErrorMessage);
                        summary.ErrorDetails.Add(new GrpcErrorDetail
                        {
                            EmployeeId = request.EmployeeId,
                            DeviceId = device.Id,
                            DeviceName = device.Name,
                            DeviceIp = device.IpAddress,
                            Code = GrpcErrorCodes.DeviceError,
                            Message = result.ErrorMessage
                        });
                    }

                    if (hardFailed)
                    {
                        summary.FailedPersons++;
                    }
                    else if (queued)
                    {
                        summary.QueuedCount++;
                    }
                    else
                    {
                        summary.SuccessfulPersons++;
                    }
                }
            }

            return summary;
        }

        private void EnsureDevicesLoaded()
        {
            if (deviceManager.GetAllDevices().Count == 0)
            {
                deviceManager.LoadAllDevices();
            }
        }


        private bool TryOpenDatabase(out SqlServerDatabase db)
        {
            db = null;

            string connStr = commonHelper.obtenerCadenaConexion();
            db = new SqlServerDatabase(commonHelper.obtenerTiempoEsperaComando());
            db.Connect(connStr);

            if (db.Connection == null)
            {
                db.Dispose();
                db = null;
                return false;
            }

            return true;
        }


        private bool TryEnsureDeviceConnected(DeviceConnectionInfo device, bool allowReconnect)
        {
            if (device == null)
            {
                return false;
            }

            if (DeviceConnectionRetryPolicy.IsDeviceReady(device.IsConnected, device.UserID))
            {
                return true;
            }

            if (!DeviceConnectionRetryPolicy.ShouldAttemptReconnect(device.IsConnected,
                device.UserID,
                device.IsReconnecting,
                allowReconnect))
            {
                return false;
            }

            bool connected = deviceManager.ConnectToDevice(device);
            return connected && DeviceConnectionRetryPolicy.IsDeviceReady(device.IsConnected, device.UserID);
        }

        private List<DeviceConnectionInfo> GetEnabledGateDevices()
        {
            EnsureDevicesLoaded();

            return deviceManager.GetAllDevices()
                .Where(d => d.IsEnabled && !IsEnrollmentDevice(d))
                .ToList();
        }

        private List<DeviceConnectionInfo> GetOnlineGateDevices()
        {
            return GetEnabledGateDevices()
                .Where(d => d.IsConnected && d.UserID >= 0)
                .ToList();
        }

        private List<DeviceAreaInfo> LoadActiveDevices()
        {
            List<DeviceAreaInfo> devices = new List<DeviceAreaInfo>();

            if (!TryOpenDatabase(out SqlServerDatabase db))
            {
                return devices;
            }

            using (db)
            {
                const string sql = "SELECT device_id, device_name, description, status FROM devices";
                using (SqlCommand cmd = db.CreateCommand(sql))
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        bool isEnabled = Convert.ToInt32(reader["status"]) == 1;
                        if (!isEnabled)
                        {
                            continue;
                        }

                        int deviceId = Convert.ToInt32(reader["device_id"]);
                        string name = reader["device_name"].ToString();

                        // 跳过人脸录入仪设备，该设备仅用于连接和人脸录入功能
                        if (IsEnrollmentDevice(name))
                        {
                            continue;
                        }

                        string description = reader["description"] == DBNull.Value ? string.Empty : reader["description"].ToString();

                        DeviceConnectionInfo connection = deviceManager.GetDeviceById(deviceId);

                        devices.Add(new DeviceAreaInfo
                        {
                            DeviceId = deviceId,
                            DeviceName = name,
                            Area = ResolveArea(description),
                            Connection = connection
                        });
                    }
                }
            }

            return devices;
        }

        private DeviceArea ResolveArea(string description)
        {
            if (string.IsNullOrWhiteSpace(description))
            {
                return DeviceArea.Other;
            }

            string normalized = description.ToLowerInvariant();
            if (normalized.Contains("生产"))
            {
                return DeviceArea.Production;
            }

            if (normalized.Contains("办公"))
            {
                return DeviceArea.Office;
            }

            return DeviceArea.Other;
        }

        private List<UserPermissionRecord> LoadUserPermissions()
        {
            List<UserPermissionRecord> users = new List<UserPermissionRecord>();

            if (!TryOpenDatabase(out SqlServerDatabase db))
            {
                return users;
            }

            using (db)
            {
                // 从 system_users 表读取人员信息，username 字段即旧版 employee_id
                string sql = @"SELECT username,
                                      nickname,
                                      access_permission,
                                      last_synced_level
                               FROM system_users
                               WHERE deleted = 0
                                 AND status = 0
                               ORDER BY username";

                using (SqlCommand cmd = db.CreateCommand(sql))
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        string employeeId = reader["username"].ToString();
                        string fullName = reader["nickname"] == DBNull.Value ? string.Empty : reader["nickname"].ToString();

                        int permissionLevel = reader["access_permission"] != DBNull.Value
                            ? Convert.ToInt32(reader["access_permission"])
                            : 0;

                        int? lastSynced = reader["last_synced_level"] != DBNull.Value
                            ? Convert.ToInt32(reader["last_synced_level"])
                            : (int?)null;

                        users.Add(new UserPermissionRecord
                        {
                            EmployeeId = employeeId,
                            FullName = fullName,
                            PermissionLevel = permissionLevel,
                            LastSyncedLevel = lastSynced
                        });
                    }
                }
            }

            return users;
        }

        private UserPermissionRecord LoadUserPermission(string employeeId)
        {
            if (!TryOpenDatabase(out SqlServerDatabase db))
            {
                return null;
            }

            using (db)
            {
                const string sql = @"SELECT TOP (1) username,
                                             nickname,
                                             access_permission,
                                             last_synced_level
                                      FROM system_users
                                      WHERE username = @username
                                        AND deleted = 0";

                using (SqlCommand cmd = db.CreateCommand(sql))
                {
                    cmd.Parameters.AddWithValue("@username", employeeId);

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            return new UserPermissionRecord
                            {
                                EmployeeId = reader["username"].ToString(),
                                FullName = reader["nickname"] == DBNull.Value ? string.Empty : reader["nickname"].ToString(),
                                PermissionLevel = reader["access_permission"] != DBNull.Value
                                    ? Convert.ToInt32(reader["access_permission"])
                                    : 0,
                                LastSyncedLevel = reader["last_synced_level"] != DBNull.Value
                                    ? Convert.ToInt32(reader["last_synced_level"])
                                    : (int?)null
                            };
                        }
                    }
                }
            }

            return null;
        }

        private bool UpdatePermissionLevel(string employeeId, int permissionLevel)
        {
            if (!TryOpenDatabase(out SqlServerDatabase db))
            {
                return false;
            }

            using (db)
            {
                string sql = @"UPDATE system_users
                               SET access_permission = @level,
                                   permission_updated_at = SYSDATETIME()
                               WHERE username = @username
                                 AND deleted = 0";

                using (SqlCommand cmd = db.CreateCommand(sql))
                {
                    cmd.Parameters.AddWithValue("@level", permissionLevel);
                    cmd.Parameters.AddWithValue("@username", employeeId);

                    int affected = cmd.ExecuteNonQuery();
                    return affected > 0;
                }
            }
        }


        private RefreshResult ApplyPermissionToDevices(UserPermissionRecord user, List<DeviceAreaInfo> devices)
        {
            RefreshResult result = new RefreshResult();

            foreach (DeviceAreaInfo device in devices)
            {
                bool shouldEnable = ShouldEnable(device.Area, user.PermissionLevel);
                DeviceUpdateResult updateResult = UpdateDeviceAccess(device, user, shouldEnable);
                if (updateResult.Success)
                {
                    ClearPermissionRetryState(device.DeviceId, user.EmployeeId);
                    continue;
                }

                if (updateResult.IsRetryable && TryQueuePermissionRetry(device, user, updateResult.ErrorMessage, result))
                {
                    continue;
                }

                result.Errors.Add(updateResult.ErrorMessage);
                result.ErrorDetails.Add(new GrpcErrorDetail
                {
                    EmployeeId = user.EmployeeId,
                    DeviceId = device.DeviceId,
                    DeviceName = device.DeviceName,
                    DeviceIp = device.Connection?.IpAddress,
                    Code = GrpcErrorCodes.DeviceError,
                    Message = updateResult.ErrorMessage
                });
            }

            AllowPermissionSyncCompletion(user.EmployeeId, user.PermissionLevel, result);
            result.Success = result.Errors.Count == 0;
            result.CompletedImmediately = result.Success && !result.HasQueued;
            return result;
        }

        private bool ShouldEnable(DeviceArea area, int level)
        {
            switch (level)
            {
                case 0:
                    return false;
                case 1:
                    return area == DeviceArea.Office;
                case 2:
                    return area == DeviceArea.Office
                        || area == DeviceArea.Production
                        || area == DeviceArea.Other;
                default:
                    return false;
            }
        }

        private DeviceUpdateResult UpdateDeviceAccess(DeviceAreaInfo device, UserPermissionRecord user, bool enable)
        {
            DeviceConnectionInfo connection = device.Connection;
            if (connection == null)
            {
                return DeviceUpdateResult.Fail(string.Format(CultureInfo.InvariantCulture,
                    "未找到设备 {0}，无法同步员工 {1} 的权限。",
                    device.DeviceName,
                    user.EmployeeId));
            }

            if (!TryEnsureDeviceConnected(connection, allowReconnect: false))
            {
                return DeviceUpdateResult.RetryableFail(string.Format(CultureInfo.InvariantCulture,
                    "无法连接设备 {0}，同步员工 {1} 权限失败。",
                    device.DeviceName,
                    user.EmployeeId));
            }

            string payload = BuildUserInfoPayload(user, connection, enable);

            return ExecuteWithDeviceSdkLock(
                connection,
                $"PermissionSetUp-{connection.Id}-{user.EmployeeId}",
                () => UpdateDeviceAccessCore(device, user, payload),
                () => DeviceUpdateResult.RetryableFail(string.Format(CultureInfo.InvariantCulture,
                    "设备 {0} 获取设备 SDK 锁超时，员工 {1} 权限稍后重试。",
                    device.DeviceName,
                    user.EmployeeId)));
        }

        private DeviceUpdateResult UpdateDeviceAccessCore(DeviceAreaInfo device, UserPermissionRecord user, string payload)
        {
            DeviceConnectionInfo connection = device.Connection;
            bool queryResult = commonHelper.ISAPIQuery(connection.UserID,
                UserInfoSetupUrl,
                payload,
                out string outputResult,
                out string outputStatus);

            if (!queryResult)
            {
                string errorMessage = ParseErrorMessage(outputStatus ?? outputResult);
                return CreateDeviceCommunicationFailureResult(
                    string.Format(CultureInfo.InvariantCulture,
                        "设备 {0} 更新员工 {1} 权限失败：{2}",
                        device.DeviceName,
                        user.EmployeeId,
                        errorMessage),
                    outputResult,
                    outputStatus);
            }

            if (!IsResponseOk(outputResult))
            {
                string errorMessage = ParseErrorMessage(outputResult);
                return DeviceUpdateResult.Fail(string.Format(CultureInfo.InvariantCulture,
                    "设备 {0} 同步员工 {1} 权限失败：{2}",
                    device.DeviceName,
                    user.EmployeeId,
                    errorMessage));
            }

            return DeviceUpdateResult.SuccessResult;
        }

        private DeviceUpdateResult UpsertPersonOnDevice(DeviceConnectionInfo device, PersonSyncRequest person)
        {
            DeviceUpdateResult personResult = UpsertPersonInfoOnDevice(device, person);
            if (!personResult.Success || person == null || !person.HasFace)
            {
                return personResult;
            }

            return UploadFaceToDevice(device, person);
        }

        private DeviceUpdateResult UpsertPersonInfoOnDevice(DeviceConnectionInfo device, PersonSyncRequest person)
        {
            if (device == null)
            {
                return DeviceUpdateResult.Fail(string.Format(CultureInfo.InvariantCulture,
                    "未找到设备，无法下发员工 {0}。",
                    person?.EmployeeId));
            }

            if (!TryEnsureDeviceConnected(device, allowReconnect: false))
            {
                return DeviceUpdateResult.RetryableFail(string.Format(CultureInfo.InvariantCulture,
                    "无法连接设备 {0}，下发员工 {1} 失败。",
                    device.Name,
                    person?.EmployeeId));
            }

            string payload = BuildPersonUserInfoPayload(person, device);
            return ExecuteWithDeviceSdkLock(
                device,
                $"UpsertPerson-{device.Id}-{person?.EmployeeId}",
                () => UpsertPersonInfoOnDeviceCore(device, person, payload),
                () => DeviceUpdateResult.RetryableFail(string.Format(CultureInfo.InvariantCulture,
                    "设备 {0} 获取设备 SDK 锁超时，员工 {1} 下发稍后重试。",
                    device.Name,
                    person?.EmployeeId)));
        }

        private DeviceUpdateResult UpsertPersonInfoOnDeviceCore(DeviceConnectionInfo device, PersonSyncRequest person, string payload)
        {
            bool queryResult = commonHelper.ISAPIQuery(device.UserID,
                UserInfoSetupUrl,
                payload,
                out string outputResult,
                out string outputStatus);

            if (!queryResult)
            {
                string errorMessage = ParseErrorMessage(outputStatus ?? outputResult);
                return CreateDeviceCommunicationFailureResult(
                    string.Format(CultureInfo.InvariantCulture,
                        "设备 {0} 下发员工 {1} 信息失败：{2}",
                        device.Name,
                        person.EmployeeId,
                        errorMessage),
                    outputResult,
                    outputStatus);
            }

            if (!IsResponseOk(outputResult))
            {
                string errorMessage = ParseErrorMessage(outputResult);
                return DeviceUpdateResult.Fail(string.Format(CultureInfo.InvariantCulture,
                    "设备 {0} 下发员工 {1} 信息失败：{2}",
                    device.Name,
                    person.EmployeeId,
                    errorMessage));
            }

            return DeviceUpdateResult.SuccessResult;
        }

        private DeviceUpdateResult UploadFaceToDevice(DeviceConnectionInfo device, PersonSyncRequest person)
        {
            if (device == null)
            {
                return DeviceUpdateResult.Fail(string.Format(CultureInfo.InvariantCulture,
                    "未找到设备，无法下发员工 {0} 人脸。",
                    person?.EmployeeId));
            }

            if (!TryEnsureDeviceConnected(device, allowReconnect: false))
            {
                return DeviceUpdateResult.RetryableFail(string.Format(CultureInfo.InvariantCulture,
                    "无法连接设备 {0}，下发员工 {1} 人脸失败。",
                    device.Name,
                    person?.EmployeeId));
            }

            return ExecuteWithDeviceSdkLock(
                device,
                $"UploadFace-{device.Id}-{person?.EmployeeId}",
                () => UploadFaceToDeviceInternal(device, person),
                () => DeviceUpdateResult.RetryableFail(string.Format(CultureInfo.InvariantCulture,
                    "设备 {0} 获取设备 SDK 锁超时，员工 {1} 人脸稍后重试。",
                    device.Name,
                    person?.EmployeeId)));
        }

        private static void BuildDoorRights(DeviceConnectionInfo connection, bool enable, out string doorRightValue, out object[] rightPlans)
        {
            int doorCount = connection?.Capabilities?.MaxDoorCount ?? 1;
            if (doorCount <= 0)
            {
                doorCount = 1;
            }

            if (!enable)
            {
                doorRightValue = string.Empty;
                rightPlans = Array.Empty<object>();
                return;
            }

            doorRightValue = string.Join(",", Enumerable.Range(1, doorCount)
                .Select(doorNo => doorNo.ToString(CultureInfo.InvariantCulture)));

            rightPlans = Enumerable.Range(1, doorCount)
                .Select(doorNo => (object)new
                {
                    doorNo,
                    planTemplateNo = "1"
                })
                .ToArray();
        }

        private string BuildPersonUserInfoPayload(PersonSyncRequest person, DeviceConnectionInfo connection)
        {
            bool enable = person.Enabled;
            BuildDoorRights(connection, enable, out string doorRightValue, out object[] rightPlans);

            string beginTime = FormatDateTimeValue(person.ValidFrom) ?? DefaultBeginTime;
            string endTime = FormatDateTimeValue(person.ValidTo) ?? DefaultEndTime;

            var payload = new
            {
                UserInfo = new
                {
                    employeeNo = person.EmployeeId,
                    name = person.FullName ?? string.Empty,
                    userType = "normal",
                    gender = string.IsNullOrWhiteSpace(person.Gender) ? "unknown" : person.Gender,
                    userVerifyMode = UserVerifyModeFace,
                    Valid = new
                    {
                        enable,
                        beginTime,
                        endTime,
                        timeType = "local"
                    },
                    doorRight = doorRightValue,
                    RightPlan = rightPlans
                }
            };

            return JsonConvert.SerializeObject(payload);
        }

        private string BuildUserInfoPayload(UserPermissionRecord user, DeviceConnectionInfo connection, bool enable)
        {
            BuildDoorRights(connection, enable, out string doorRightValue, out object[] rightPlans);

            var payload = new
            {
                UserInfo = new
                {
                    employeeNo = user.EmployeeId,
                    name = user.FullName ?? string.Empty,
                    userType = "normal",
                    Valid = new
                    {
                        enable,
                        beginTime = DefaultBeginTime,
                        endTime = enable ? DefaultEndTime : DefaultBeginTime,
                        timeType = "local"
                    },
                    doorRight = doorRightValue,
                    RightPlan = rightPlans
                }
            };

            return JsonConvert.SerializeObject(payload);
        }

        private DeviceUpdateResult UploadFaceToDeviceInternal(DeviceConnectionInfo device, PersonSyncRequest person)
        {
            if (person == null)
            {
                return DeviceUpdateResult.Fail("未提供需要同步的人脸信息。");
            }

            if (!person.HasFace)
            {
                return DeviceUpdateResult.SuccessResult;
            }

            if (person.FaceImageBytes.Length > MaxFaceImageBytes)
            {
                return DeviceUpdateResult.Fail(string.Format(CultureInfo.InvariantCulture,
                    "人员 {0} 的人脸图片大小 {1} 字节超过 200KB 限制。",
                    person.EmployeeId,
                    person.FaceImageBytes.Length));
            }

            IntPtr urlPtr = IntPtr.Zero;
            IntPtr jsonPtr = IntPtr.Zero;
            IntPtr picturePtr = IntPtr.Zero;
            IntPtr configPtr = IntPtr.Zero;
            IntPtr responsePtr = IntPtr.Zero;
            int handle = -1;

            try
            {
                byte[] urlBytes = Encoding.UTF8.GetBytes(FaceSetupUrl);
                urlPtr = Marshal.AllocHGlobal(urlBytes.Length + 1);
                Marshal.Copy(urlBytes, 0, urlPtr, urlBytes.Length);
                Marshal.WriteByte(urlPtr, urlBytes.Length, 0);

                handle = HCNetSDK.NET_DVR_StartRemoteConfig(device.UserID,
                    (uint)HCNetSDK.NET_DVR_FACE_DATA_RECORD,
                    urlPtr,
                    urlBytes.Length,
                    null,
                    IntPtr.Zero);
                if (handle < 0)
                {
                    uint errorCode = HCNetSDK.NET_DVR_GetLastError();
                    return CreateDeviceSdkFailureResult(
                        string.Format(CultureInfo.InvariantCulture,
                            "设备 {0} 启动人脸同步失败，错误码 {1}。",
                            device.Name,
                            errorCode),
                        errorCode);
                }

                string jsonPayload = BuildFacePayload(person);
                byte[] jsonBytes = Encoding.UTF8.GetBytes(jsonPayload);
                jsonPtr = Marshal.AllocHGlobal(jsonBytes.Length);
                Marshal.Copy(jsonBytes, 0, jsonPtr, jsonBytes.Length);

                picturePtr = Marshal.AllocHGlobal(person.FaceImageBytes.Length);
                Marshal.Copy(person.FaceImageBytes, 0, picturePtr, person.FaceImageBytes.Length);

                HCNetSDK.NET_DVR_JSON_DATA_CFG config = new HCNetSDK.NET_DVR_JSON_DATA_CFG
                {
                    dwSize = (uint)Marshal.SizeOf(typeof(HCNetSDK.NET_DVR_JSON_DATA_CFG)),
                    lpJsonData = jsonPtr,
                    dwJsonDataSize = (uint)jsonBytes.Length,
                    lpPicData = picturePtr,
                    dwPicDataSize = (uint)person.FaceImageBytes.Length,
                    byRes = new byte[256]
                };

                int configSize = Marshal.SizeOf(config);
                configPtr = Marshal.AllocHGlobal(configSize);
                Marshal.StructureToPtr(config, configPtr, false);

                responsePtr = Marshal.AllocHGlobal(2048);
                uint responseSize = 0;

                int status = HCNetSDK.NET_DVR_SendWithRecvRemoteConfig(
                    handle,
                    configPtr,
                    (uint)configSize,
                    responsePtr,
                    2048,
                    ref responseSize);

                string response = ReadStringFromBuffer(responsePtr, responseSize);
                if (status == (int)HCNetSDK.NET_SDK_SENDWITHRECV_STATUS.NET_SDK_CONFIG_STATUS_SUCCESS ||
                    status == (int)HCNetSDK.NET_SDK_SENDWITHRECV_STATUS.NET_SDK_CONFIG_STATUS_FINISH)
                {
                    return DeviceUpdateResult.SuccessResult;
                }

                string errorMessage = string.IsNullOrWhiteSpace(response)
                    ? string.Format(CultureInfo.InvariantCulture, "状态 {0}", status)
                    : ParseErrorMessage(response);

                return CreateRemoteConfigFailureResult(
                    string.Format(CultureInfo.InvariantCulture,
                        "设备 {0} 同步人员 {1} 的人脸失败：{2}",
                        device.Name,
                        person.EmployeeId,
                        errorMessage),
                    status,
                    response);
            }
            finally
            {
                if (handle >= 0)
                {
                    HCNetSDK.NET_DVR_StopRemoteConfig(handle);
                }

                if (urlPtr != IntPtr.Zero)
                {
                    Marshal.FreeHGlobal(urlPtr);
                }

                if (jsonPtr != IntPtr.Zero)
                {
                    Marshal.FreeHGlobal(jsonPtr);
                }

                if (picturePtr != IntPtr.Zero)
                {
                    Marshal.FreeHGlobal(picturePtr);
                }

                if (configPtr != IntPtr.Zero)
                {
                    Marshal.FreeHGlobal(configPtr);
                }

                if (responsePtr != IntPtr.Zero)
                {
                    Marshal.FreeHGlobal(responsePtr);
                }
            }
        }

        private static string BuildFacePayload(PersonSyncRequest person)
        {
            var payload = new
            {
                faceLibType = DefaultFaceLibType,
                FDID = DefaultFaceLibId,
                FPID = person.EmployeeId
            };

            return JsonConvert.SerializeObject(payload);
        }

        /// <summary>
        /// 从“人脸录入仪”设备抓取一帧 JPEG 并返回 Base64。
        /// </summary>
        public FaceCaptureResult CaptureFaceFromEnrollmentDevice()
        {
            EnsureDevicesLoaded();
            DeviceConnectionInfo device = deviceManager.GetAllDevices()
                .FirstOrDefault(d => string.Equals(d.Name, EnrollmentDeviceName, StringComparison.OrdinalIgnoreCase));

            if (device == null)
            {
                return FaceCaptureResult.Fail("未找到名称为'人脸录入仪'的设备。");
            }

            if (!TryEnsureDeviceConnected(device, allowReconnect: true))
            {
                return FaceCaptureResult.Fail(string.Format(CultureInfo.InvariantCulture,
                    "无法连接设备 {0}。", device.Name), device.Id, device.Name, device.IpAddress);
            }

            
            int? deviceId = device.Id;
            string deviceName = device.Name;
            string deviceIp = device.IpAddress;

return ExecuteWithDeviceSdkLock(
                device,
                $"CaptureFace-{device.Id}",
                () =>
                {
                    int handle = -1;
                    IntPtr condPtr = IntPtr.Zero;

                    try
                    {
                        HCNetSDK_Facial.NET_DVR_CAPTURE_FACE_COND cond = new HCNetSDK_Facial.NET_DVR_CAPTURE_FACE_COND();
                        cond.init();
                        cond.dwSize = Marshal.SizeOf(cond);

                        condPtr = Marshal.AllocHGlobal(Marshal.SizeOf(cond));
                        Marshal.StructureToPtr(cond, condPtr, false);

                        handle = HCNetSDK_Facial.NET_DVR_StartRemoteConfig(
                            device.UserID,
                            HCNetSDK_Facial.NET_DVR_CAPTURE_FACE_INFO,
                            condPtr,
                            Marshal.SizeOf(cond),
                            null,
                            IntPtr.Zero);

                        if (handle < 0)
                        {
                            uint errorCode = HCNetSDK.NET_DVR_GetLastError();
                            return FaceCaptureResult.Fail(string.Format(CultureInfo.InvariantCulture,
                                "启动人脸采集失败，错误码：{0}。", errorCode), deviceId, deviceName, deviceIp);
                        }

                        int maxAttempts = 100;
                        for (int i = 0; i < maxAttempts; i++)
                        {
                            HCNetSDK_Facial.NET_DVR_CAPTURE_FACE_CFG faceCfg = new HCNetSDK_Facial.NET_DVR_CAPTURE_FACE_CFG();
                            faceCfg.init();
                            faceCfg.dwSize = Marshal.SizeOf(faceCfg);

                            int status = HCNetSDK_Facial.NET_DVR_GetNextRemoteConfig(
                                handle,
                                ref faceCfg,
                                Marshal.SizeOf(faceCfg));

                            if (status == HCNetSDK_Facial.NET_SDK_GET_NEXT_STATUS_SUCCESS)
                            {
                                if (faceCfg.byCaptureProgress == 100)
                                {
                                    if (faceCfg.dwFacePicSize > 0 && faceCfg.pFacePicBuffer != IntPtr.Zero)
                                    {
                                        byte[] faceData = new byte[faceCfg.dwFacePicSize];
                                        Marshal.Copy(faceCfg.pFacePicBuffer, faceData, 0, faceCfg.dwFacePicSize);

                                        if (faceData.Length > MaxFaceImageBytes)
                                        {
                                            return FaceCaptureResult.Fail(string.Format(CultureInfo.InvariantCulture,
                                                "采集图片大小 {0} 字节超过 200KB。", faceData.Length), deviceId, deviceName, deviceIp);
                                        }

                                        string base64 = Convert.ToBase64String(faceData);
                                        return FaceCaptureResult.Ok(base64, "jpg", deviceId, deviceName, deviceIp);
                                    }

                                    return FaceCaptureResult.Fail("采集成功但未获取到人脸图片数据。", deviceId, deviceName, deviceIp);
                                }
                            }
                            else if (status == HCNetSDK_Facial.NET_SDK_GET_NEXT_STATUS_NEED_WAIT)
                            {
                                System.Threading.Thread.Sleep(100);
                            }
                            else if (status == HCNetSDK_Facial.NET_SDK_GET_NEXT_STATUS_FINISH)
                            {
                                return FaceCaptureResult.Fail("人脸采集完成但未检测到有效人脸。", deviceId, deviceName, deviceIp);
                            }
                            else if (status == HCNetSDK_Facial.NET_SDK_GET_NEXT_STATUS_FAILED)
                            {
                                uint errorCode = HCNetSDK.NET_DVR_GetLastError();
                                return FaceCaptureResult.Fail(string.Format(CultureInfo.InvariantCulture,
                                    "人脸采集失败，错误码：{0}。", errorCode), deviceId, deviceName, deviceIp);
                            }
                        }

                        return FaceCaptureResult.Fail("人脸采集超时，请确保人脸正对设备摄像头。", deviceId, deviceName, deviceIp);
                    }
                    finally
                    {
                        if (handle >= 0)
                        {
                            HCNetSDK_Facial.NET_DVR_StopRemoteConfig(handle);
                        }
                        if (condPtr != IntPtr.Zero)
                        {
                            Marshal.FreeHGlobal(condPtr);
                        }
                    }
                },
                () => FaceCaptureResult.Fail(string.Format(CultureInfo.InvariantCulture,
                    "设备 {0} 忙碌，等待设备SDK锁超时，人脸采集启动失败。",
                    device.Name), deviceId, deviceName, deviceIp));
        }

        public FaceOperationSummary DeleteFacesOnDevices(IEnumerable<string> employeeIds)
        {
            if (employeeIds == null)
            {
                throw new ArgumentNullException(nameof(employeeIds));
            }

            List<string> ids = employeeIds
                .Where(id => !string.IsNullOrWhiteSpace(id))
                .Select(id => id.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            FaceOperationSummary summary = new FaceOperationSummary
            {
                Total = ids.Count
            };

            if (ids.Count == 0)
            {
                summary.Errors.Add("未提供任何需要删除人脸的员工。");
                return summary;
            }

            lock (personSyncLock)
            {
                List<DeviceConnectionInfo> devices = GetEnabledGateDevices();
                summary.TargetDevices = devices.Count;
                if (devices.Count == 0)
                {
                    summary.Errors.Add("未找到可用的门禁设备，无法删除人脸。");
                    summary.Failed = summary.Total;
                    return summary;
                }

                foreach (string id in ids)
                {
                    FaceOperationItem item = new FaceOperationItem
                    {
                        EmployeeId = id
                    };

                    int immediateSuccessCount = 0;
                    bool queued = false;
                    bool hardFailed = false;
                    List<string> deviceErrors = new List<string>();

                    foreach (DeviceConnectionInfo device in devices)
                    {
                        DeviceUpdateResult result = DeleteFaceOnDevice(device, id);
                        if (result.Success)
                        {
                            immediateSuccessCount++;
                            ClearDeleteFaceRetryState(device.Id, id);
                            continue;
                        }

                        if (result.IsRetryable && TryQueueDeleteFaceRetry(device, id, result.ErrorMessage, summary.QueuedDetails))
                        {
                            queued = true;
                            continue;
                        }

                        hardFailed = true;
                        if (!string.IsNullOrWhiteSpace(result.ErrorMessage))
                        {
                            deviceErrors.Add(result.ErrorMessage);
                        }

                        summary.ErrorDetails.Add(new GrpcErrorDetail
                        {
                            EmployeeId = id,
                            DeviceId = device.Id,
                            DeviceName = device.Name,
                            DeviceIp = device.IpAddress,
                            Code = GrpcErrorCodes.DeviceError,
                            Message = result.ErrorMessage
                        });
                    }

                    if (hardFailed)
                    {
                        item.Success = false;
                        item.Error = deviceErrors.Count == 0
                            ? $"员工 {id} 删除人脸失败。"
                            : string.Join("; ", deviceErrors.Take(3));
                        summary.Errors.Add(item.Error);
                        summary.Failed++;
                    }
                    else if (queued)
                    {
                        item.Success = false;
                        item.Error = $"员工 {id} 的人脸删除已加入重试队列。";
                        summary.QueuedCount++;
                    }
                    else
                    {
                        item.Success = true;
                        summary.Succeeded++;
                    }

                    item.RawResponse = immediateSuccessCount.ToString(CultureInfo.InvariantCulture);
                    summary.Items.Add(item);
                }
            }

            return summary;
        }

        /// <summary>
        /// 从所有在线门禁设备中彻底删除指定人员（包括人员信息和人脸数据）。
        /// 此操作会遍历所有设备，跳过人脸录入仪设备。
        /// </summary>
        /// <param name="employeeIds">要删除的人员工号列表</param>
        /// <returns>删除操作的摘要结果</returns>
        public PersonDeleteSummary DeletePersonsFromDevices(IEnumerable<string> employeeIds)
        {
            if (employeeIds == null)
            {
                throw new ArgumentNullException(nameof(employeeIds));
            }

            List<string> ids = employeeIds
                .Where(id => !string.IsNullOrWhiteSpace(id))
                .Select(id => id.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            PersonDeleteSummary summary = new PersonDeleteSummary
            {
                Total = ids.Count
            };

            if (ids.Count == 0)
            {
                summary.Errors.Add("未提供任何需要删除的员工。");
                return summary;
            }

            lock (personSyncLock)
            {
                List<DeviceConnectionInfo> devices = GetEnabledGateDevices();
                summary.TargetDevices = devices.Count;

                if (devices.Count == 0)
                {
                    summary.Errors.Add("未找到可用的门禁设备，无法删除人员。");
                    summary.Failed = summary.Total;
                    return summary;
                }

                foreach (string id in ids)
                {
                    PersonDeleteItem item = new PersonDeleteItem
                    {
                        EmployeeId = id
                    };

                    bool queued = false;
                    int successCount = 0;
                    int failCount = 0;

                    foreach (DeviceConnectionInfo device in devices)
                    {
                        DeviceUpdateResult result = DeletePersonAndFaceOnDevice(device, id);
                        if (result.Success)
                        {
                            successCount++;
                            ClearDeletePersonRetryState(device.Id, id);
                            continue;
                        }

                        if (result.IsRetryable && TryQueueDeletePersonRetry(device, id, result, summary.QueuedDetails))
                        {
                            queued = true;
                            continue;
                        }

                        failCount++;
                        item.DeviceErrors.Add(string.Format(CultureInfo.InvariantCulture,
                            "[{0}] {1}",
                            device.Name,
                            result.ErrorMessage));
                        summary.ErrorDetails.Add(new GrpcErrorDetail
                        {
                            EmployeeId = id,
                            DeviceId = device.Id,
                            DeviceName = device.Name,
                            DeviceIp = device.IpAddress,
                            Code = GrpcErrorCodes.DeviceError,
                            Message = result.ErrorMessage
                        });
                    }

                    item.SuccessDevices = successCount;
                    item.FailedDevices = failCount;

                    if (failCount > 0)
                    {
                        item.Success = false;
                        string errorMessage = item.DeviceErrors.Count > 0
                            ? string.Format(CultureInfo.InvariantCulture,
                                "员工 {0} 在 {1} 台设备上删除失败：{2}",
                                id,
                                failCount,
                                string.Join("; ", item.DeviceErrors.Take(3)))
                            : string.Format(CultureInfo.InvariantCulture,
                                "员工 {0} 在 {1} 台设备上删除失败。",
                                id,
                                failCount);
                        summary.Errors.Add(errorMessage);
                        summary.Failed++;
                    }
                    else if (queued)
                    {
                        item.Success = false;
                        summary.QueuedCount++;
                    }
                    else
                    {
                        item.Success = true;
                        summary.Succeeded++;
                    }

                    summary.Items.Add(item);
                }
            }

            return summary;
        }

        private DeviceUpdateResult DeletePersonAndFaceOnDevice(DeviceConnectionInfo device, string employeeId)
        {
            DeviceUpdateResult faceResult = DeleteFaceOnDevice(device, employeeId);
            if (!faceResult.Success)
            {
                return faceResult;
            }

            return DeletePersonOnDevice(device, employeeId)
                .MarkDeleteFaceApplied();
        }

        private DeviceUpdateResult DeletePersonOnDevice(DeviceConnectionInfo device, string employeeId)
        {
            if (device == null)
            {
                return DeviceUpdateResult.Fail("未找到可用的设备。");
            }

            if (!TryEnsureDeviceConnected(device, allowReconnect: false))
            {
                return DeviceUpdateResult.RetryableFail(string.Format(CultureInfo.InvariantCulture,
                    "无法连接设备 {0}，删除员工 {1} 失败。",
                    device.Name,
                    employeeId));
            }

            var payload = new
            {
                UserInfoDelCond = new
                {
                    EmployeeNoList = new[]
                    {
                        new { employeeNo = employeeId }
                    }
                }
            };

            return ExecuteWithDeviceSdkLock(
                device,
                $"DeletePerson-{device.Id}-{employeeId}",
                () => DeletePersonOnDeviceCore(device, employeeId, JsonConvert.SerializeObject(payload)),
                () => DeviceUpdateResult.RetryableFail(string.Format(CultureInfo.InvariantCulture,
                    "设备 {0} 获取设备 SDK 锁超时，删除员工 {1} 稍后重试。",
                    device.Name,
                    employeeId)));
        }

        private DeviceUpdateResult DeletePersonOnDeviceCore(DeviceConnectionInfo device, string employeeId, string payload)
        {
            bool result = commonHelper.ISAPIQuery(device.UserID,
                UserInfoDeleteUrl,
                payload,
                out string outputResult,
                out string outputStatus);

            if (!result)
            {
                string errorMessage = ParseErrorMessage(outputStatus ?? outputResult);
                return CreateDeviceCommunicationFailureResult(
                    string.Format(CultureInfo.InvariantCulture,
                        "设备 {0} 删除员工 {1} 失败：{2}",
                        device.Name,
                        employeeId,
                        errorMessage),
                    outputResult,
                    outputStatus);
            }

            if (!IsResponseOk(outputResult))
            {
                string errorMessage = ParseErrorMessage(outputResult);
                return DeviceUpdateResult.Fail(string.Format(CultureInfo.InvariantCulture,
                    "设备 {0} 删除员工 {1} 失败：{2}",
                    device.Name,
                    employeeId,
                    errorMessage));
            }

            return DeviceUpdateResult.SuccessResult;
        }

        public FaceOperationSummary GetFacesFromDevices(IEnumerable<string> employeeIds)
        {
            if (employeeIds == null)
            {
                throw new ArgumentNullException(nameof(employeeIds));
            }

            List<string> ids = employeeIds
                .Where(id => !string.IsNullOrWhiteSpace(id))
                .Select(id => id.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            FaceOperationSummary summary = new FaceOperationSummary
            {
                Total = ids.Count
            };

            if (ids.Count == 0)
            {
                summary.Errors.Add("未提供需要查询人脸的员工编号。");
                return summary;
            }

            lock (personSyncLock)
            {
                List<DeviceConnectionInfo> onlineDevices = GetOnlineGateDevices();

                summary.TargetDevices = onlineDevices.Count;
                if (onlineDevices.Count == 0)
                {
                    summary.Errors.Add("当前没有在线的门禁设备，无法查询人脸。");
                    summary.Failed = summary.Total;
                    return summary;
                }

                foreach (string id in ids)
                {
                    FaceOperationItem item = new FaceOperationItem
                    {
                        EmployeeId = id
                    };

                    List<GrpcErrorDetail> deviceDetails = new List<GrpcErrorDetail>();

                    foreach (DeviceConnectionInfo device in onlineDevices)
                    {
                        FaceQueryResult result = QueryFaceOnDevice(device, id);
                        if (result.Success)
                        {
                            item.Success = true;
                            item.FaceImageBase64 = result.FaceImageBase64;
                            item.RawResponse = result.RawResponse;
                            break;
                        }

                        item.Error = result.ErrorMessage;
                        item.RawResponse = result.RawResponse;
                        deviceDetails.Add(new GrpcErrorDetail
                        {
                            EmployeeId = id,
                            DeviceId = device.Id,
                            DeviceName = device.Name,
                            DeviceIp = device.IpAddress,
                            Code = GrpcErrorCodes.DeviceError,
                            Message = result.ErrorMessage,
                            RawResponse = result.RawResponse
                        });
                    }

                    if (item.Success)
                    {
                        summary.Succeeded++;
                    }
                    else
                    {
                        summary.Failed++;
                        if (!string.IsNullOrWhiteSpace(item.Error))
                        {
                            summary.Errors.Add(item.Error);
                        }
                        summary.ErrorDetails.AddRange(deviceDetails);
                    }

                    summary.Items.Add(item);
                }
            }

            return summary;
        }

        private DeviceUpdateResult DeleteFaceOnDevice(DeviceConnectionInfo device, string employeeId)
        {
            if (device == null)
            {
                return DeviceUpdateResult.Fail("未找到可用的设备。");
            }

            if (!TryEnsureDeviceConnected(device, allowReconnect: false))
            {
                return DeviceUpdateResult.RetryableFail(string.Format(CultureInfo.InvariantCulture,
                    "无法连接设备 {0}，删除员工 {1} 人脸失败。",
                    device.Name,
                    employeeId));
            }

            var payload = new
            {
                FPID = new[]
                {
                    new { value = employeeId }
                }
            };

            return ExecuteWithDeviceSdkLock(
                device,
                $"DeleteFace-{device.Id}-{employeeId}",
                () => DeleteFaceOnDeviceCore(device, employeeId, JsonConvert.SerializeObject(payload)),
                () => DeviceUpdateResult.RetryableFail(string.Format(CultureInfo.InvariantCulture,
                    "设备 {0} 获取设备 SDK 锁超时，删除员工 {1} 人脸稍后重试。",
                    device.Name,
                    employeeId)));
        }

        private DeviceUpdateResult DeleteFaceOnDeviceCore(DeviceConnectionInfo device, string employeeId, string payload)
        {
            bool result = commonHelper.ISAPIQuery(device.UserID,
                FaceDeleteUrl,
                payload,
                out string outputResult,
                out string outputStatus);

            string responseContent = string.IsNullOrWhiteSpace(outputResult) ? outputStatus : outputResult;
            if (!result)
            {
                string errorMessage = ParseErrorMessage(responseContent);
                return CreateDeviceCommunicationFailureResult(
                    string.Format(CultureInfo.InvariantCulture,
                        "设备 {0} 删除员工 {1} 人脸失败：{2}",
                        device.Name,
                        employeeId,
                        errorMessage),
                    outputResult,
                    outputStatus);
            }

            responseContent = ExtractJsonFromMultipart(responseContent);
            if (DeviceDeleteResponsePolicy.IsDeleteFaceAlreadyAbsent(responseContent))
            {
                return DeviceUpdateResult.SuccessResult;
            }

            if (!IsResponseOkFromContent(responseContent))
            {
                string errorMessage = ParseErrorMessage(responseContent);
                return DeviceUpdateResult.Fail(string.Format(CultureInfo.InvariantCulture,
                    "设备 {0} 删除员工 {1} 人脸失败：{2}",
                    device.Name,
                    employeeId,
                    errorMessage));
            }

            return DeviceUpdateResult.SuccessResult;
        }

        private FaceQueryResult QueryFaceOnDevice(DeviceConnectionInfo device, string employeeId)
        {
            FaceQueryResult result = new FaceQueryResult
            {
                Success = false,
                ErrorMessage = string.Empty
            };

            if (device == null)
            {
                result.ErrorMessage = "未找到可用的设备。";
                return result;
            }

            if (!TryEnsureDeviceConnected(device, allowReconnect: true))
            {
                result.ErrorMessage = string.Format(CultureInfo.InvariantCulture,
                    "无法连接设备 {0}，查询人员 {1} 人脸失败。", device.Name, employeeId);
                return result;
            }

            var payload = new
            {
                searchResultPosition = 0,
                maxResults = 1,
                faceLibType = DefaultFaceLibType,
                FDID = DefaultFaceLibId,
                FPID = employeeId
            };

            string outputResult = null;
            string outputStatus = null;

            bool ok = ExecuteWithDeviceSdkLock(
                device,
                $"QueryFace-{device.Id}-{employeeId}",
                () => commonHelper.ISAPIQuery(device.UserID,
                    FaceSearchUrl,
                    JsonConvert.SerializeObject(payload),
                    out outputResult,
                    out outputStatus),
                () => false);

            result.RawResponse = string.IsNullOrWhiteSpace(outputResult) ? outputStatus : outputResult;

            if (!ok)
            {
                if (outputResult == null && outputStatus == null)
                {
                    result.ErrorMessage = string.Format(CultureInfo.InvariantCulture,
                        "设备 {0} 忙碌，等待设备SDK锁超时，查询人员 {1} 人脸失败。",
                        device.Name,
                        employeeId);
                    return result;
                }

                result.ErrorMessage = ParseErrorMessage(outputStatus ?? outputResult);
                return result;
            }

            string jsonContent = ExtractJsonFromMultipart(outputResult);

            if (!IsResponseOkFromContent(jsonContent))
            {
                result.ErrorMessage = ParseErrorMessage(jsonContent);
                return result;
            }

            try
            {
                JToken root = JToken.Parse(jsonContent);

                JToken dataList = root["MatchList"] ?? root["FaceDataRecord"];
                if (dataList is JArray arr && arr.Count > 0)
                {
                    JToken first = arr[0];

                    string face = first.Value<string>("facePicBinary") ??
                                  first.Value<string>("FacePicBinary") ??
                                  first.Value<string>("facePic") ??
                                  first.Value<string>("FacePic") ??
                                  first.Value<string>("modelData");

                    result.FaceImageBase64 = face;
                }

                int numOfMatches = root.Value<int?>("numOfMatches") ?? 0;
                int totalMatches = root.Value<int?>("totalMatches") ?? 0;
                if (numOfMatches > 0 || totalMatches > 0)
                {
                    result.Success = true;
                }
                else
                {
                    result.Success = dataList != null && ((JArray)dataList).Count > 0;
                }
            }
            catch (JsonException)
            {
                if (jsonContent.Contains("\"statusCode\":\t1") || jsonContent.Contains("\"statusCode\": 1"))
                {
                    result.Success = true;
                }
            }

            return result;
        }

        /// <summary>
        /// 从 multipart/form-data 响应中提取 JSON 内容。
        /// 如果响应不是 multipart 格式，则原样返回。
        /// </summary>
        private static string ExtractJsonFromMultipart(string response)
        {
            if (string.IsNullOrWhiteSpace(response))
            {
                return response;
            }

            // 如果响应以 { 开头，说明是纯 JSON
            string trimmed = response.TrimStart();
            if (trimmed.StartsWith("{", StringComparison.Ordinal) || trimmed.StartsWith("[", StringComparison.Ordinal))
            {
                return response;
            }

            // 尝试从 multipart 响应中提取 JSON 部分
            // 格式：Content-Type:multipart/form-data;boundary=MIME_boundary\r\n--MIME_boundary\r\nContent-Type: application/json\r\n...\r\n\r\n{JSON内容}\r\n\r\n--MIME_boundary--
            int jsonStart = response.IndexOf("\r\n\r\n{", StringComparison.Ordinal);
            if (jsonStart < 0)
            {
                jsonStart = response.IndexOf("\n\n{", StringComparison.Ordinal);
            }

            if (jsonStart >= 0)
            {
                // 跳过空行前缀
                jsonStart = response.IndexOf('{', jsonStart);
                
                // 找到 JSON 结束位置（最后一个 } 在边界标记之前）
                int boundaryEnd = response.LastIndexOf("--", StringComparison.Ordinal);
                if (boundaryEnd > jsonStart)
                {
                    // 从 jsonStart 向 boundaryEnd 方向找最后一个 }
                    int jsonEnd = response.LastIndexOf('}', boundaryEnd, boundaryEnd - jsonStart);
                    if (jsonEnd > jsonStart)
                    {
                        return response.Substring(jsonStart, jsonEnd - jsonStart + 1);
                    }
                }

                // 回退：从 jsonStart 开始找到最后一个 }
                int lastBrace = response.LastIndexOf('}');
                if (lastBrace > jsonStart)
                {
                    return response.Substring(jsonStart, lastBrace - jsonStart + 1);
                }
            }

            return response;
        }

        /// <summary>
        /// 检查响应内容是否表示成功。
        /// </summary>
        private bool IsResponseOkFromContent(string content)
        {
            if (string.IsNullOrWhiteSpace(content))
            {
                return false;
            }

            try
            {
                JObject data = JObject.Parse(content);

                int? statusCodeInt = data.Value<int?>("statusCode");
                string statusCodeText = data.Value<string>("statusCode");

                // 技术规范：以 statusCode==1 作为成功主判断；其他字段用于兼容与诊断
                if (statusCodeInt == 1 || string.Equals(statusCodeText, "1", StringComparison.Ordinal))
                {
                    return true;
                }

                // 有明确 statusCode 且不为 1，则按失败处理
                if (statusCodeInt.HasValue || !string.IsNullOrWhiteSpace(statusCodeText))
                {
                    return false;
                }

                // 兼容：部分设备可能不返回 statusCode，仅返回 statusString/subStatusCode
                string statusString = data.Value<string>("statusString");
                string subStatusCode = data.Value<string>("subStatusCode");

                bool statusOk = string.Equals(statusString, "OK", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(statusString, "ok", StringComparison.OrdinalIgnoreCase);

                bool subOk = string.IsNullOrWhiteSpace(subStatusCode)
                    || string.Equals(subStatusCode, "ok", StringComparison.OrdinalIgnoreCase);

                return statusOk && subOk;
            }
            catch
            {
                return false;
            }
        }

        private static string NormalizeGender(string gender)
        {
            if (string.IsNullOrWhiteSpace(gender))
            {
                return "unknown";
            }

            string normalized = gender.Trim().ToLowerInvariant();
            switch (normalized)
            {
                case "male":
                case "m":
                case "man":
                case "boy":
                    return "male";
                case "female":
                case "f":
                case "woman":
                case "girl":
                    return "female";
                default:
                    return "unknown";
            }
        }

        private static string FormatDateTimeValue(DateTime? value)
        {
            if (!value.HasValue)
            {
                return null;
            }

            return value.Value.ToString("yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture);
        }

        private static string ReadStringFromBuffer(IntPtr buffer, uint length)
        {
            if (buffer == IntPtr.Zero)
            {
                return null;
            }

            if (length == 0)
            {
                int actualLength = 0;
                while (Marshal.ReadByte(buffer, actualLength) != 0)
                {
                    actualLength++;
                }

                length = (uint)actualLength;
            }

            if (length == 0)
            {
                return string.Empty;
            }

            byte[] data = new byte[length];
            Marshal.Copy(buffer, data, 0, (int)length);
            return Encoding.UTF8.GetString(data).TrimEnd('\0');
        }

        private bool IsResponseOk(string response)
        {
            return IsResponseOkFromContent(response);
        }

        private string ParseErrorMessage(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
            {
                return "未知错误";
            }

            try
            {
                JObject data = JObject.Parse(raw);
                List<string> messages = new List<string>();

                string statusString = data.Value<string>("statusString");
                string subStatusCode = data.Value<string>("subStatusCode");
                string errorMsg = data.Value<string>("errorMsg");

                if (!string.IsNullOrWhiteSpace(statusString))
                {
                    messages.Add(statusString);
                }

                if (!string.IsNullOrWhiteSpace(subStatusCode))
                {
                    messages.Add(subStatusCode);
                }

                if (!string.IsNullOrWhiteSpace(errorMsg))
                {
                    messages.Add(errorMsg);
                }

                return messages.Count > 0 ? string.Join(" / ", messages) : raw;
            }
            catch
            {
                return raw;
            }
        }

        private static DeviceUpdateResult CreateDeviceCommunicationFailureResult(string errorMessage, string outputResult, string outputStatus)
        {
            return DeviceOperationRetryFailurePolicy.IsRetryableTransportFailure(outputResult, outputStatus)
                ? DeviceUpdateResult.RetryableFail(errorMessage)
                : DeviceUpdateResult.Fail(errorMessage);
        }

        private static DeviceUpdateResult CreateDeviceSdkFailureResult(string errorMessage, uint errorCode)
        {
            return DeviceOperationRetryFailurePolicy.IsRetryableSdkError(errorCode)
                ? DeviceUpdateResult.RetryableFail(errorMessage)
                : DeviceUpdateResult.Fail(errorMessage);
        }

        private static DeviceUpdateResult CreateRemoteConfigFailureResult(string errorMessage, int status, string responseContent)
        {
            return DeviceOperationRetryFailurePolicy.IsRetryableRemoteConfigStatus(status, responseContent)
                ? DeviceUpdateResult.RetryableFail(errorMessage)
                : DeviceUpdateResult.Fail(errorMessage);
        }

        private bool UpdateSyncedLevel(string employeeId, int permissionLevel)
        {
            if (!TryOpenDatabase(out SqlServerDatabase db))
            {
                return false;
            }

            using (db)
            {
                // 更新 system_users 表中的同步字段
                string sql = @"UPDATE system_users
                               SET last_synced_level = @level,
                                   last_synced_at = SYSDATETIME()
                               WHERE username = @username
                                 AND deleted = 0";

                using (SqlCommand cmd = db.CreateCommand(sql))
                {
                    cmd.Parameters.AddWithValue("@level", permissionLevel);
                    cmd.Parameters.AddWithValue("@username", employeeId);

                    int affected = cmd.ExecuteNonQuery();
                    return affected > 0;
                }
            }
        }

        internal bool CompletePermissionSyncIfNoPending(string employeeId, int permissionLevel)
        {
            if (retryStore != null && retryOptions != null && retryOptions.Enabled && retryStore.HasPendingPermission(employeeId))
            {
                return true;
            }

            return UpdateSyncedLevel(employeeId, permissionLevel);
        }

        private void AllowPermissionSyncCompletion(string employeeId, int permissionLevel, RefreshResult result)
        {
            if (result == null || !result.HasQueued || result.Errors.Count > 0)
            {
                return;
            }

            if (retryStore == null || retryOptions == null || !retryOptions.Enabled || string.IsNullOrWhiteSpace(employeeId))
            {
                return;
            }

            try
            {
                retryStore.AllowPermissionSyncCompletion(employeeId, permissionLevel);
            }
            catch (Exception ex)
            {
                string errorMessage = string.Format(CultureInfo.InvariantCulture,
                    "\u5141\u8bb8\u5458\u5de5 {0} \u7684\u6743\u9650\u540c\u6b65\u6807\u8bb0\u843d\u5e93\u5931\u8d25\u3002",
                    employeeId);
                ServiceLogger.Error(errorMessage, ex);
                result.Errors.Add(errorMessage);
                result.ErrorDetails.Add(new GrpcErrorDetail
                {
                    EmployeeId = employeeId,
                    Code = GrpcErrorCodes.DbError,
                    Message = errorMessage
                });
            }
        }

        private void ClearPermissionRetryState(int deviceId, string employeeId)
        {
            if (retryStore == null || retryOptions == null || !retryOptions.Enabled)
            {
                return;
            }

            try
            {
                retryStore.MarkPermissionApplied(deviceId, employeeId);
            }
            catch (Exception ex)
            {
                ServiceLogger.Error($"清理设备 {deviceId} 员工 {employeeId} 的权限补偿状态失败。", ex);
            }
        }

        private void ClearPersonRetryState(int deviceId, string employeeId, bool clearFaceRetry)
        {
            if (retryStore == null || retryOptions == null || !retryOptions.Enabled)
            {
                return;
            }

            try
            {
                if (clearFaceRetry)
                {
                    retryStore.MarkPersonAndFaceApplied(deviceId, employeeId);
                }
                else
                {
                    retryStore.MarkPersonAppliedAndClearFaceRetry(deviceId, employeeId);
                }
            }
            catch (Exception ex)
            {
                ServiceLogger.Error($"清理设备 {deviceId} 员工 {employeeId} 的人员补偿状态失败。", ex);
            }
        }

        private void ClearDeleteFaceRetryState(int deviceId, string employeeId)
        {
            if (retryStore == null || retryOptions == null || !retryOptions.Enabled)
            {
                return;
            }

            try
            {
                retryStore.MarkDeleteFaceApplied(deviceId, employeeId, clearDeletePersonPending: true);
            }
            catch (Exception ex)
            {
                ServiceLogger.Error($"清理设备 {deviceId} 员工 {employeeId} 的删脸补偿状态失败。", ex);
            }
        }

        private void ClearDeletePersonRetryState(int deviceId, string employeeId)
        {
            if (retryStore == null || retryOptions == null || !retryOptions.Enabled)
            {
                return;
            }

            try
            {
                retryStore.MarkDeletePersonApplied(deviceId, employeeId);
            }
            catch (Exception ex)
            {
                ServiceLogger.Error($"清理设备 {deviceId} 员工 {employeeId} 的删人补偿状态失败。", ex);
            }
        }

        private bool TryQueuePermissionRetry(DeviceAreaInfo device, UserPermissionRecord user, string message, RefreshResult result)
        {
            if (retryStore == null || retryOptions == null || !retryOptions.Enabled)
            {
                return false;
            }

            try
            {
                retryStore.QueuePermissionRetry(device.DeviceId, user.EmployeeId, user.PermissionLevel, message);
                result.HasQueued = true;
                result.QueuedDetails.Add(CreateQueuedDetail(user.EmployeeId, device.DeviceId, device.DeviceName, device.Connection?.IpAddress, "PermissionSync", message));
                return true;
            }
            catch (Exception ex)
            {
                ServiceLogger.Error($"设备 {device.DeviceName} 员工 {user.EmployeeId} 写入权限补偿队列失败。", ex);
                return false;
            }
        }

        private bool TryQueuePersonRetry(DeviceConnectionInfo device, PersonSyncRequest person, string message, ICollection<QueuedOperationDetail> queuedDetails)
        {
            if (retryStore == null || retryOptions == null || !retryOptions.Enabled)
            {
                return false;
            }

            try
            {
                retryStore.QueuePersonRetry(device.Id, person, message);
                queuedDetails.Add(CreateQueuedDetail(person.EmployeeId,
                    device.Id,
                    device.Name,
                    device.IpAddress,
                    person.HasFace ? "PersonAndFaceSync" : "PersonSync",
                    message));
                return true;
            }
            catch (Exception ex)
            {
                ServiceLogger.Error($"设备 {device.Name} 员工 {person?.EmployeeId} 写入人员补偿队列失败。", ex);
                return false;
            }
        }

        private bool TryQueueDeleteFaceRetry(DeviceConnectionInfo device, string employeeId, string message, ICollection<QueuedOperationDetail> queuedDetails)
        {
            if (retryStore == null || retryOptions == null || !retryOptions.Enabled)
            {
                return false;
            }

            try
            {
                retryStore.QueueDeleteFaceRetry(device.Id, employeeId, message);
                queuedDetails.Add(CreateQueuedDetail(employeeId, device.Id, device.Name, device.IpAddress, "DeleteFace", message));
                return true;
            }
            catch (Exception ex)
            {
                ServiceLogger.Error($"设备 {device.Name} 员工 {employeeId} 写入删脸补偿队列失败。", ex);
                return false;
            }
        }

        private bool TryQueueDeletePersonRetry(DeviceConnectionInfo device, string employeeId, DeviceUpdateResult result, ICollection<QueuedOperationDetail> queuedDetails)
        {
            if (retryStore == null || retryOptions == null || !retryOptions.Enabled)
            {
                return false;
            }

            try
            {
                retryStore.QueueDeletePersonRetry(device.Id,
                    employeeId,
                    result?.ErrorMessage,
                    deleteFacePending: result == null || !result.DeleteFaceApplied);
                queuedDetails.Add(CreateQueuedDetail(employeeId,
                    device.Id,
                    device.Name,
                    device.IpAddress,
                    "DeletePerson",
                    result?.ErrorMessage));
                return true;
            }
            catch (Exception ex)
            {
                ServiceLogger.Error($"?? {device.Name} ?? {employeeId} ???????????", ex);
                return false;
            }
        }

        private static QueuedOperationDetail CreateQueuedDetail(string employeeId, int deviceId, string deviceName, string deviceIp, string operation, string message)
        {
            return new QueuedOperationDetail
            {
                EmployeeId = employeeId,
                DeviceId = deviceId,
                DeviceName = deviceName,
                DeviceIp = deviceIp,
                Operation = operation,
                Message = message
            };
        }

        private DeviceAreaInfo LoadActiveDevice(int deviceId)
        {
            return LoadActiveDevices().FirstOrDefault(d => d.DeviceId == deviceId);
        }

        public DeviceOperationRetryExecutionResult ProcessQueuedState(DeviceOperationRetryState state)
        {
            if (state == null || !state.HasPendingOperations)
            {
                return DeviceOperationRetryExecutionResult.Completed;
            }

            if (retryStore == null || retryOptions == null || !retryOptions.Enabled)
            {
                return DeviceOperationRetryExecutionResult.HardFailure("补偿管理器未启用。");
            }

            DeviceConnectionInfo device = deviceManager.GetDeviceById(state.DeviceId);
            if (device == null || !device.IsEnabled || IsEnrollmentDevice(device))
            {
                return DeviceOperationRetryExecutionResult.HardFailure(string.Format(CultureInfo.InvariantCulture,
                    "设备 {0} 不可用或不支持补偿。",
                    state.DeviceId));
            }

            if (!TryEnsureDeviceConnected(device, allowReconnect: true))
            {
                return DeviceOperationRetryExecutionResult.RetryableFailure(string.Format(CultureInfo.InvariantCulture,
                    "无法连接设备 {0}，补偿任务稍后重试。",
                    device.Name));
            }

            using (var sdkLock = device.TryAcquireDeviceSdkLock(deviceSdkLockTimeoutMs, $"Replay-{device.Id}-{state.EmployeeId}"))
            {
                if (!sdkLock.IsAcquired)
                {
                    return DeviceOperationRetryExecutionResult.RetryableFailure(string.Format(CultureInfo.InvariantCulture,
                        "设备 {0} 获取设备 SDK 锁超时，补偿任务稍后重试。",
                        device.Name));
                }

                DeviceOperationRetryState current = retryStore.GetState(state.DeviceId, state.EmployeeId);
                if (current == null || !current.HasPendingOperations)
                {
                    return DeviceOperationRetryExecutionResult.Completed;
                }

                if (current.DeleteFacePending)
                {
                    DeviceUpdateResult deleteFaceResult = DeleteFaceOnDeviceCore(device,
                        current.EmployeeId,
                        JsonConvert.SerializeObject(new
                        {
                            FPID = new[]
                            {
                                new { value = current.EmployeeId }
                            }
                        }));
                    if (!deleteFaceResult.Success)
                    {
                        return ToRetryExecutionResult(deleteFaceResult);
                    }

                    retryStore.MarkDeleteFaceApplied(current.DeviceId, current.EmployeeId);
                    current = retryStore.GetState(current.DeviceId, current.EmployeeId);
                    if (current == null || !current.HasPendingOperations)
                    {
                        return DeviceOperationRetryExecutionResult.Completed;
                    }
                }

                if (current.DeletePersonPending)
                {
                    DeviceUpdateResult deletePersonResult = DeletePersonOnDeviceCore(device,
                        current.EmployeeId,
                        JsonConvert.SerializeObject(new
                        {
                            UserInfoDelCond = new
                            {
                                EmployeeNoList = new[]
                                {
                                    new { employeeNo = current.EmployeeId }
                                }
                            }
                        }));
                    if (!deletePersonResult.Success)
                    {
                        return ToRetryExecutionResult(deletePersonResult);
                    }

                    retryStore.MarkDeletePersonApplied(current.DeviceId, current.EmployeeId);
                    current = retryStore.GetState(current.DeviceId, current.EmployeeId);
                    if (current == null || !current.HasPendingOperations)
                    {
                        return DeviceOperationRetryExecutionResult.Completed;
                    }
                }

                if (current.PersonPending)
                {
                    PersonSyncRequest personRequest = current.CreatePersonRequest();
                    if (personRequest == null || string.IsNullOrWhiteSpace(personRequest.EmployeeId))
                    {
                        return DeviceOperationRetryExecutionResult.HardFailure($"员工 {current.EmployeeId} 的人员补偿数据无效。");
                    }

                    DeviceUpdateResult personResult = UpsertPersonInfoOnDeviceCore(device,
                        personRequest,
                        BuildPersonUserInfoPayload(personRequest, device));
                    if (!personResult.Success)
                    {
                        return ToRetryExecutionResult(personResult);
                    }

                    retryStore.MarkPersonApplied(current.DeviceId, current.EmployeeId);
                    current = retryStore.GetState(current.DeviceId, current.EmployeeId);
                    if (current == null || !current.HasPendingOperations)
                    {
                        return DeviceOperationRetryExecutionResult.Completed;
                    }
                }

                if (current.FacePending)
                {
                    PersonSyncRequest faceRequest = current.CreateFaceRequest();
                    if (faceRequest == null || string.IsNullOrWhiteSpace(faceRequest.EmployeeId) || !faceRequest.HasFace)
                    {
                        return DeviceOperationRetryExecutionResult.HardFailure($"员工 {current.EmployeeId} 的人脸补偿数据无效。");
                    }

                    DeviceUpdateResult faceResult = UploadFaceToDeviceInternal(device, faceRequest);
                    if (!faceResult.Success)
                    {
                        return ToRetryExecutionResult(faceResult);
                    }

                    retryStore.MarkFaceApplied(current.DeviceId, current.EmployeeId);
                    current = retryStore.GetState(current.DeviceId, current.EmployeeId);
                    if (current == null || !current.HasPendingOperations)
                    {
                        return DeviceOperationRetryExecutionResult.Completed;
                    }
                }

                if (current.PermissionPending)
                {
                    if (!current.PermissionLevel.HasValue)
                    {
                        return DeviceOperationRetryExecutionResult.HardFailure($"员工 {current.EmployeeId} 的权限补偿数据无效。");
                    }

                    UserPermissionRecord userRecord = LoadUserPermission(current.EmployeeId);
                    if (userRecord == null)
                    {
                        return DeviceOperationRetryExecutionResult.HardFailure(string.Format(CultureInfo.InvariantCulture,
                            "未找到员工 {0} 的权限信息，无法执行补偿。",
                            current.EmployeeId));
                    }

                    DeviceAreaInfo deviceInfo = LoadActiveDevice(current.DeviceId);
                    if (deviceInfo == null)
                    {
                        return DeviceOperationRetryExecutionResult.HardFailure(string.Format(CultureInfo.InvariantCulture,
                            "未找到设备 {0} 的权限区域信息，无法执行补偿。",
                            current.DeviceId));
                    }

                    userRecord.PermissionLevel = current.PermissionLevel.Value;
                    deviceInfo.Connection = device;
                    bool shouldEnable = ShouldEnable(deviceInfo.Area, userRecord.PermissionLevel);
                    DeviceUpdateResult permissionResult = UpdateDeviceAccessCore(
                        deviceInfo,
                        userRecord,
                        BuildUserInfoPayload(userRecord, device, shouldEnable));
                    if (!permissionResult.Success)
                    {
                        return ToRetryExecutionResult(permissionResult);
                    }

                    PermissionRetryCommitResult commitResult = retryStore.CompletePermissionRetry(
                        current.DeviceId,
                        current.EmployeeId,
                        userRecord.PermissionLevel);
                    if (!commitResult.Success)
                    {
                        return DeviceOperationRetryExecutionResult.RetryableFailure(commitResult.ErrorMessage);
                    }
                }
            }

            return DeviceOperationRetryExecutionResult.Completed;
        }

        private static DeviceOperationRetryExecutionResult ToRetryExecutionResult(DeviceUpdateResult result)
        {
            if (result == null || result.Success)
            {
                return DeviceOperationRetryExecutionResult.Completed;
            }

            return result.IsRetryable
                ? DeviceOperationRetryExecutionResult.RetryableFailure(result.ErrorMessage)
                : DeviceOperationRetryExecutionResult.HardFailure(result.ErrorMessage);
        }

        private class UserPermissionRecord
        {
            public string EmployeeId { get; set; }

            public string FullName { get; set; }

            public int PermissionLevel { get; set; }

            public int? LastSyncedLevel { get; set; }
        }

        private class DeviceAreaInfo
        {
            public int DeviceId { get; set; }

            public string DeviceName { get; set; }

            public DeviceArea Area { get; set; }

            public DeviceConnectionInfo Connection { get; set; }
        }

        private class RefreshResult
        {
            public bool Success { get; set; }

            public bool HasQueued { get; set; }

            public bool CompletedImmediately { get; set; }

            public List<string> Errors { get; } = new List<string>();

            public List<GrpcErrorDetail> ErrorDetails { get; } = new List<GrpcErrorDetail>();

            public List<QueuedOperationDetail> QueuedDetails { get; } = new List<QueuedOperationDetail>();
        }

        private class DeviceUpdateResult
        {
            public bool Success { get; private set; }

            public bool IsRetryable { get; private set; }

            public string ErrorMessage { get; private set; }

            public bool DeleteFaceApplied { get; private set; }

            public static DeviceUpdateResult SuccessResult { get; } = new DeviceUpdateResult { Success = true };

            public static DeviceUpdateResult Fail(string message)
            {
                return new DeviceUpdateResult
                {
                    Success = false,
                    ErrorMessage = message,
                    IsRetryable = false
                };
            }

            public static DeviceUpdateResult RetryableFail(string message)
            {
                return new DeviceUpdateResult
                {
                    Success = false,
                    ErrorMessage = message,
                    IsRetryable = true
                };
            }

            public DeviceUpdateResult MarkDeleteFaceApplied()
            {
                DeleteFaceApplied = true;
                return this;
            }
        }
    }

    public enum DeviceArea
    {
        Production,
        Office,
        Other
    }

    public class PermissionRefreshSummary
    {
        public int TotalUsers { get; set; }

        public int UsersSkipped { get; set; }

        public int UsersUpdated { get; set; }

        public int UsersFailed { get; set; }

        public int QueuedCount { get; set; }

        public List<QueuedOperationDetail> QueuedDetails { get; } = new List<QueuedOperationDetail>();

        public List<string> Errors { get; } = new List<string>();

        public List<GrpcErrorDetail> ErrorDetails { get; } = new List<GrpcErrorDetail>();
    }

    public class PersonDeleteSummary
    {
        public int Total { get; set; }

        public int Succeeded { get; set; }

        public int Failed { get; set; }

        public int TargetDevices { get; set; }

        public int QueuedCount { get; set; }

        public List<QueuedOperationDetail> QueuedDetails { get; } = new List<QueuedOperationDetail>();

        public List<string> Errors { get; } = new List<string>();

        public List<GrpcErrorDetail> ErrorDetails { get; } = new List<GrpcErrorDetail>();

        public List<PersonDeleteItem> Items { get; } = new List<PersonDeleteItem>();
    }

    public class PersonDeleteItem
    {
        /// <summary>
        /// 员工编号。
        /// </summary>
        public string EmployeeId { get; set; }

        /// <summary>
        /// 是否成功删除（至少在一个设备上成功）。
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// 成功删除的设备数量。
        /// </summary>
        public int SuccessDevices { get; set; }

        /// <summary>
        /// 删除失败的设备数量。
        /// </summary>
        public int FailedDevices { get; set; }

        /// <summary>
        /// 各设备的错误消息。
        /// </summary>
        public List<string> DeviceErrors { get; } = new List<string>();
    }
}
