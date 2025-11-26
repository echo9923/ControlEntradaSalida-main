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
        private const string SnapshotUrl = "GET /ISAPI/Streaming/channels/101/picture";
        private const string EnrollmentDeviceName = "人脸录入仪";
        private const string DefaultFaceLibType = "blackFD";
        private const string DefaultFaceLibId = "1";
        private const string UserVerifyModeFace = "face";
        private const int MaxFaceImageBytes = 200 * 1024;

        private readonly DeviceConnectionManager deviceManager;
        private readonly Common commonHelper;
        private readonly object refreshLock = new object();
        private readonly object personSyncLock = new object();

        public PermissionRefreshManager()
        {
            deviceManager = DeviceConnectionManager.Instance;
            commonHelper = new Common();
        }

        /// <summary>
        /// 判断设备是否为人脸录入仪设备。
        /// 人脸录入仪仅用于连接和人脸录入功能，不参与人员信息、权限等数据的下发。
        /// </summary>
        /// <param name="device">设备连接信息</param>
        /// <returns>如果是人脸录入仪则返回 true</returns>
        private static bool IsEnrollmentDevice(DeviceConnectionInfo device)
        {
            if (device == null)
            {
                return false;
            }

            return string.Equals(device.Name, EnrollmentDeviceName, StringComparison.OrdinalIgnoreCase);
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

                List<UserPermissionRecord> users = LoadUserPermissions(out List<string> newlyCreatedRecords);
                summary.TotalUsers = users.Count;

                if (newlyCreatedRecords.Count > 0)
                {
                    try
                    {
                        InsertDefaultPermissionRecords(newlyCreatedRecords);
                    }
                    catch (Exception ex)
                    {
                        summary.Errors.Add(string.Format(CultureInfo.InvariantCulture,
                            "初始化权限记录时发生错误：{0}", ex.Message));
                        summary.UsersFailed = summary.TotalUsers;
                        return summary;
                    }
                }

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
                        bool updated = UpdateSyncedLevel(user.EmployeeId, user.PermissionLevel);
                        if (!updated)
                        {
                            summary.UsersFailed++;
                            summary.Errors.Add(string.Format(CultureInfo.InvariantCulture,
                                "更新用户 {0} 的同步状态失败。", user.EmployeeId));
                            continue;
                        }

                        summary.UsersUpdated++;
                    }
                    else
                    {
                        summary.UsersFailed++;
                        summary.Errors.AddRange(result.Errors);
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
                        summary.Errors.Add(string.Format(CultureInfo.InvariantCulture,
                            "用户 {0} 的权限级别 {1} 无效，应为 0-2。",
                            update.EmployeeId, update.PermissionCode));
                        continue;
                    }

                    bool permissionStored = UpdatePermissionLevel(update.EmployeeId, update.PermissionCode);
                    if (!permissionStored)
                    {
                        summary.UsersFailed++;
                        summary.Errors.Add(string.Format(CultureInfo.InvariantCulture,
                            "更新员工 {0} 在数据库中的权限失败，可能不存在该员工。", update.EmployeeId));
                        continue;
                    }

                    UserPermissionRecord userRecord = LoadUserPermission(update.EmployeeId);
                    if (userRecord == null)
                    {
                        summary.UsersFailed++;
                        summary.Errors.Add(string.Format(CultureInfo.InvariantCulture,
                            "未找到员工 {0} 的详细信息。", update.EmployeeId));
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
                        bool synced = UpdateSyncedLevel(userRecord.EmployeeId, update.PermissionCode);
                        if (!synced)
                        {
                            summary.UsersFailed++;
                            summary.Errors.Add(string.Format(CultureInfo.InvariantCulture,
                                "更新用户 {0} 的同步状态失败。", userRecord.EmployeeId));
                            continue;
                        }

                        summary.UsersUpdated++;
                    }
                    else
                    {
                        summary.UsersFailed++;
                        summary.Errors.AddRange(result.Errors);
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
                    ServiceLogger.Warn("检测到缺少 employee_id 的人员记录，已跳过。");
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
                        "有 {0} 条记录因缺少 employee_id 被跳过。", skippedWithoutId));
                }

                summary.Errors.Add("未提供任何需要同步的人员。");
                return summary;
            }

            lock (personSyncLock)
            {
                EnsureDevicesLoaded();

                // 排除人脸录入仪设备，该设备仅用于连接和人脸录入功能
                List<DeviceConnectionInfo> onlineDevices = deviceManager.GetAllDevices()
                    .Where(d => d.IsEnabled && d.IsConnected && d.UserID >= 0 && !IsEnrollmentDevice(d))
                    .ToList();

                summary.TargetDevices = onlineDevices.Count;

                if (onlineDevices.Count == 0)
                {
                    summary.Errors.Add("当前没有在线的门禁设备，无法下发人员信息。");
                    summary.FailedPersons = summary.TotalPersons;
                    return summary;
                }

                if (skippedWithoutId > 0)
                {
                    summary.Errors.Add(string.Format(CultureInfo.InvariantCulture,
                        "有 {0} 条记录因缺少 employee_id 被跳过。", skippedWithoutId));
                }

                foreach (PersonSyncRequest request in requests)
                {
                    bool personSucceeded = true;

                    foreach (DeviceConnectionInfo device in onlineDevices)
                    {
                        DeviceUpdateResult result = UpsertPersonOnDevice(device, request);
                        if (!result.Success)
                        {
                            personSucceeded = false;
                            summary.Errors.Add(result.ErrorMessage);
                        }
                        else if (request.HasFace)
                        {
                            summary.FacesUploaded++;
                        }
                    }

                    if (personSucceeded)
                    {
                        summary.SuccessfulPersons++;
                    }
                    else
                    {
                        summary.FailedPersons++;
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

        private List<DeviceAreaInfo> LoadActiveDevices()
        {
            List<DeviceAreaInfo> devices = new List<DeviceAreaInfo>();

            string connStr = commonHelper.obtenerCadenaConexion();
            SqlServerDatabase db = new SqlServerDatabase(commonHelper.obtenerTiempoEsperaComando());
            db.Connect(connStr);
            if (db.Connection == null)
            {
                return devices;
            }

            try
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
            finally
            {
                db.Disconnect();
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

        private List<UserPermissionRecord> LoadUserPermissions(out List<string> missingRecords)
        {
            List<UserPermissionRecord> users = new List<UserPermissionRecord>();
            missingRecords = new List<string>();

            string connStr = commonHelper.obtenerCadenaConexion();
            SqlServerDatabase db = new SqlServerDatabase(commonHelper.obtenerTiempoEsperaComando());
            db.Connect(connStr);
            if (db.Connection == null)
            {
                return users;
            }

            try
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
            finally
            {
                db.Disconnect();
            }

            return users;
        }

        private UserPermissionRecord LoadUserPermission(string employeeId)
        {
            string connStr = commonHelper.obtenerCadenaConexion();
            SqlServerDatabase db = new SqlServerDatabase(commonHelper.obtenerTiempoEsperaComando());
            db.Connect(connStr);
            if (db.Connection == null)
            {
                return null;
            }

            try
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
            finally
            {
                db.Disconnect();
            }

            return null;
        }

        private bool UpdatePermissionLevel(string employeeId, int permissionLevel)
        {
            string connStr = commonHelper.obtenerCadenaConexion();
            SqlServerDatabase db = new SqlServerDatabase(commonHelper.obtenerTiempoEsperaComando());
            db.Connect(connStr);
            if (db.Connection == null)
            {
                return false;
            }

            try
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
            finally
            {
                db.Disconnect();
            }
        }

        private void InsertDefaultPermissionRecords(IEnumerable<string> employeeIds)
        {
            // system_users 表中的 access_permission 已提供默认值，无需额外初始化
            // 保留该方法仅为兼容历史流程
            return;
        }

        private RefreshResult ApplyPermissionToDevices(UserPermissionRecord user, List<DeviceAreaInfo> devices)
        {
            RefreshResult result = new RefreshResult();

            foreach (DeviceAreaInfo device in devices)
            {
                bool shouldEnable = ShouldEnable(device.Area, user.PermissionLevel);

                DeviceUpdateResult updateResult = UpdateDeviceAccess(device, user, shouldEnable);
                if (!updateResult.Success)
                {
                    result.Success = false;
                    result.Errors.Add(updateResult.ErrorMessage);
                }
            }

            if (result.Errors.Count == 0)
            {
                result.Success = true;
            }

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
                    // 最高权限可通行所有已识别区域，包括未明确标注的其他区域
                    return area == DeviceArea.Office
                        || area == DeviceArea.Production
                        || area == DeviceArea.Other;
                default:
                    return false;
            }
        }

        private DeviceUpdateResult UpdateDeviceAccess(DeviceAreaInfo device, UserPermissionRecord user, bool enable)
        {
            DeviceUpdateResult failure(string message)
            {
                return DeviceUpdateResult.Fail(message);
            }

            DeviceConnectionInfo connection = device.Connection;
            if (connection == null)
            {
                return failure(string.Format(CultureInfo.InvariantCulture,
                    "设备 {0} 未在系统中加载，无法更新用户 {1} 的权限。",
                    device.DeviceName, user.EmployeeId));
            }

            if (!connection.IsConnected || connection.UserID < 0)
            {
                bool connected = deviceManager.ConnectToDevice(connection);
                if (!connected || connection.UserID < 0)
                {
                    return failure(string.Format(CultureInfo.InvariantCulture,
                        "无法连接设备 {0}，刷新用户 {1} 权限失败。",
                        device.DeviceName, user.EmployeeId));
                }
            }

            string payload = BuildUserInfoPayload(user, connection, enable);
            bool queryResult = commonHelper.ISAPIQuery(connection.UserID,
                "PUT /ISAPI/AccessControl/UserInfo/Modify?format=json",
                payload,
                out string outputResult,
                out string outputStatus);

            if (!queryResult)
            {
                string errorMessage = ParseErrorMessage(outputStatus ?? outputResult);
                return failure(string.Format(CultureInfo.InvariantCulture,
                    "设备 {0} 同步用户 {1} 权限失败：{2}",
                    device.DeviceName, user.EmployeeId, errorMessage));
            }

            if (!IsResponseOk(outputResult))
            {
                string errorMessage = ParseErrorMessage(outputResult);
                return failure(string.Format(CultureInfo.InvariantCulture,
                    "设备 {0} 返回错误，用户 {1} 权限未更新：{2}",
                    device.DeviceName, user.EmployeeId, errorMessage));
            }

            return DeviceUpdateResult.SuccessResult;
        }

        private DeviceUpdateResult UpsertPersonOnDevice(DeviceConnectionInfo device, PersonSyncRequest person)
        {
            if (device == null)
            {
                return DeviceUpdateResult.Fail(string.Format(CultureInfo.InvariantCulture,
                    "无法定位同步人员 {0} 的目标设备。", person.EmployeeId));
            }

            if (!device.IsConnected || device.UserID < 0)
            {
                bool connected = deviceManager.ConnectToDevice(device);
                if (!connected || device.UserID < 0)
                {
                    return DeviceUpdateResult.Fail(string.Format(CultureInfo.InvariantCulture,
                        "无法连接设备 {0}，同步人员 {1} 失败。", device.Name, person.EmployeeId));
                }
            }

            string payload = BuildPersonUserInfoPayload(person, device);
            bool queryResult = commonHelper.ISAPIQuery(device.UserID,
                UserInfoSetupUrl,
                payload,
                out string outputResult,
                out string outputStatus);

            if (!queryResult)
            {
                string errorMessage = ParseErrorMessage(outputStatus ?? outputResult);
                return DeviceUpdateResult.Fail(string.Format(CultureInfo.InvariantCulture,
                    "设备 {0} 下发人员 {1} 信息失败：{2}",
                    device.Name,
                    person.EmployeeId,
                    errorMessage));
            }

            if (!IsResponseOk(outputResult))
            {
                string errorMessage = ParseErrorMessage(outputResult);
                return DeviceUpdateResult.Fail(string.Format(CultureInfo.InvariantCulture,
                    "设备 {0} 返回错误，人员 {1} 信息未更新：{2}",
                    device.Name,
                    person.EmployeeId,
                    errorMessage));
            }

            if (!person.HasFace)
            {
                return DeviceUpdateResult.SuccessResult;
            }

            return UploadFaceToDevice(device, person);
        }

        private string BuildPersonUserInfoPayload(PersonSyncRequest person, DeviceConnectionInfo connection)
        {
            int doorCount = connection.Capabilities?.MaxDoorCount ?? 1;
            if (doorCount <= 0)
            {
                doorCount = 1;
            }

            bool enable = person.Enabled;
            string doorRightValue = enable
                ? string.Join(",", Enumerable.Range(1, doorCount)
                    .Select(doorNo => doorNo.ToString(CultureInfo.InvariantCulture)))
                : string.Empty;

            var rightPlans = enable && doorCount > 0
                ? Enumerable.Range(1, doorCount)
                    .Select(doorNo => new
                    {
                        doorNo,
                        planTemplateNo = "1"
                    })
                    .ToArray()
                : Array.Empty<object>();

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
            int doorCount = connection.Capabilities?.MaxDoorCount ?? 1;
            if (doorCount <= 0)
            {
                doorCount = 1;
            }

            string doorRightValue = string.Empty;
            if (enable && doorCount > 0)
            {
                doorRightValue = string.Join(",", Enumerable.Range(1, doorCount)
                    .Select(doorNo => doorNo.ToString(CultureInfo.InvariantCulture)));
            }

            var rightPlans = enable && doorCount > 0
                ? Enumerable.Range(1, doorCount)
                    .Select(doorNo => new
                    {
                        doorNo,
                        planTemplateNo = "1"
                    })
                    .ToArray()
                : Array.Empty<object>();

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

        private DeviceUpdateResult UploadFaceToDevice(DeviceConnectionInfo device, PersonSyncRequest person)
        {
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
                    return DeviceUpdateResult.Fail(string.Format(CultureInfo.InvariantCulture,
                        "设备 {0} 启动人脸同步失败，错误码 {1}。",
                        device.Name,
                        errorCode));
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

                return DeviceUpdateResult.Fail(string.Format(CultureInfo.InvariantCulture,
                    "设备 {0} 同步人员 {1} 的人脸失败：{2}",
                    device.Name,
                    person.EmployeeId,
                    errorMessage));
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

            if (!device.IsConnected || device.UserID < 0)
            {
                bool connected = deviceManager.ConnectToDevice(device);
                if (!connected || device.UserID < 0)
                {
                    return FaceCaptureResult.Fail(string.Format(CultureInfo.InvariantCulture,
                        "无法连接设备 {0}。", device.Name));
                }
            }

            // 根据技术规范2.4.6，使用NET_DVR_CAPTURE_FACE_INFO (2510)命令码进行人脸采集
            // 此接口适用于DS-K1F600U-D6E-F等专用人脸录入仪
            int handle = -1;
            IntPtr condPtr = IntPtr.Zero;
            try
            {
                // 1. 准备采集条件结构体
                HCNetSDK_Facial.NET_DVR_CAPTURE_FACE_COND cond = new HCNetSDK_Facial.NET_DVR_CAPTURE_FACE_COND();
                cond.init();
                cond.dwSize = Marshal.SizeOf(cond);

                condPtr = Marshal.AllocHGlobal(Marshal.SizeOf(cond));
                Marshal.StructureToPtr(cond, condPtr, false);

                // 2. 开启远程配置
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
                        "启动人脸采集失败，错误码：{0}。", errorCode));
                }

                // 3. 循环获取采集结果（等待用户将人脸对准设备）
                int maxAttempts = 100; // 最大尝试次数，约10秒
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
                        // 检查采集进度：0-未采集到人脸，100-采集到人脸
                        if (faceCfg.byCaptureProgress == 100)
                        {
                            // 采集成功，提取人脸图片
                            if (faceCfg.dwFacePicSize > 0 && faceCfg.pFacePicBuffer != IntPtr.Zero)
                            {
                                byte[] faceData = new byte[faceCfg.dwFacePicSize];
                                Marshal.Copy(faceCfg.pFacePicBuffer, faceData, 0, faceCfg.dwFacePicSize);

                                if (faceData.Length > MaxFaceImageBytes)
                                {
                                    return FaceCaptureResult.Fail(string.Format(CultureInfo.InvariantCulture,
                                        "采集图片大小 {0} 字节超过 200KB。", faceData.Length));
                                }

                                string base64 = Convert.ToBase64String(faceData);
                                return FaceCaptureResult.Ok(base64, "jpg");
                            }
                            else
                            {
                                return FaceCaptureResult.Fail("采集成功但未获取到人脸图片数据。");
                            }
                        }
                        // 进度为0，继续等待
                    }
                    else if (status == HCNetSDK_Facial.NET_SDK_GET_NEXT_STATUS_NEED_WAIT)
                    {
                        // 需要等待，休眠一段时间后继续
                        System.Threading.Thread.Sleep(100);
                    }
                    else if (status == HCNetSDK_Facial.NET_SDK_GET_NEXT_STATUS_FINISH)
                    {
                        // 采集完成但未获取到人脸
                        return FaceCaptureResult.Fail("人脸采集完成但未检测到有效人脸。");
                    }
                    else if (status == HCNetSDK_Facial.NET_SDK_GET_NEXT_STATUS_FAILED)
                    {
                        uint errorCode = HCNetSDK.NET_DVR_GetLastError();
                        return FaceCaptureResult.Fail(string.Format(CultureInfo.InvariantCulture,
                            "人脸采集失败，错误码：{0}。", errorCode));
                    }
                }

                return FaceCaptureResult.Fail("人脸采集超时，请确保人脸正对设备摄像头。");
            }
            finally
            {
                // 4. 关闭远程配置
                if (handle >= 0)
                {
                    HCNetSDK_Facial.NET_DVR_StopRemoteConfig(handle);
                }
                if (condPtr != IntPtr.Zero)
                {
                    Marshal.FreeHGlobal(condPtr);
                }
            }
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
                summary.Errors.Add("未提供需要删除人脸的员工编号。");
                return summary;
            }

            lock (personSyncLock)
            {
                EnsureDevicesLoaded();
                // 排除人脸录入仪设备，该设备仅用于连接和人脸录入功能
                List<DeviceConnectionInfo> onlineDevices = deviceManager.GetAllDevices()
                    .Where(d => d.IsEnabled && d.IsConnected && d.UserID >= 0 && !IsEnrollmentDevice(d))
                    .ToList();

                summary.TargetDevices = onlineDevices.Count;
                if (onlineDevices.Count == 0)
                {
                    summary.Errors.Add("当前没有在线的门禁设备，无法删除人脸。");
                    summary.Failed = summary.Total;
                    return summary;
                }

                foreach (string id in ids)
                {
                    FaceOperationItem item = new FaceOperationItem
                    {
                        EmployeeId = id
                    };

                    foreach (DeviceConnectionInfo device in onlineDevices)
                    {
                        DeviceUpdateResult result = DeleteFaceOnDevice(device, id);
                        if (result.Success)
                        {
                            item.Success = true;
                            break;
                        }

                        item.Error = result.ErrorMessage;
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
                    }

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
                summary.Errors.Add("未提供需要删除的员工编号。");
                return summary;
            }

            lock (personSyncLock)
            {
                EnsureDevicesLoaded();

                // 排除人脸录入仪设备，该设备仅用于连接和人脸录入功能
                List<DeviceConnectionInfo> onlineDevices = deviceManager.GetAllDevices()
                    .Where(d => d.IsEnabled && d.IsConnected && d.UserID >= 0 && !IsEnrollmentDevice(d))
                    .ToList();

                summary.TargetDevices = onlineDevices.Count;

                if (onlineDevices.Count == 0)
                {
                    summary.Errors.Add("当前没有在线的门禁设备，无法删除人员。");
                    summary.Failed = summary.Total;
                    return summary;
                }

                foreach (string id in ids)
                {
                    PersonDeleteItem item = new PersonDeleteItem
                    {
                        EmployeeId = id
                    };

                    int successCount = 0;
                    int failCount = 0;

                    foreach (DeviceConnectionInfo device in onlineDevices)
                    {
                        DeviceUpdateResult result = DeletePersonOnDevice(device, id);
                        if (result.Success)
                        {
                            successCount++;
                        }
                        else
                        {
                            failCount++;
                            item.DeviceErrors.Add(string.Format(CultureInfo.InvariantCulture,
                                "[{0}] {1}", device.Name, result.ErrorMessage));
                        }
                    }

                    item.SuccessDevices = successCount;
                    item.FailedDevices = failCount;

                    // 只要有一个设备成功删除就视为部分成功
                    if (successCount > 0)
                    {
                        item.Success = true;
                        summary.Succeeded++;
                    }
                    else
                    {
                        item.Success = false;
                        summary.Failed++;
                        if (item.DeviceErrors.Count > 0)
                        {
                            summary.Errors.Add(string.Format(CultureInfo.InvariantCulture,
                                "员工 {0} 在所有设备上删除失败：{1}",
                                id,
                                string.Join("; ", item.DeviceErrors.Take(3))));
                        }
                    }

                    summary.Items.Add(item);
                }
            }

            return summary;
        }

        /// <summary>
        /// 在指定设备上删除单个人员信息。
        /// </summary>
        /// <param name="device">目标设备</param>
        /// <param name="employeeId">员工编号</param>
        /// <returns>删除结果</returns>
        private DeviceUpdateResult DeletePersonOnDevice(DeviceConnectionInfo device, string employeeId)
        {
            if (device == null)
            {
                return DeviceUpdateResult.Fail("未找到可用的设备。");
            }

            if (!device.IsConnected || device.UserID < 0)
            {
                bool connected = deviceManager.ConnectToDevice(device);
                if (!connected || device.UserID < 0)
                {
                    return DeviceUpdateResult.Fail(string.Format(CultureInfo.InvariantCulture,
                        "无法连接设备 {0}，删除人员 {1} 失败。", device.Name, employeeId));
                }
            }

            // 根据海康威视 ISAPI 规范，使用 UserInfo/Delete 接口删除人员
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

            bool result = commonHelper.ISAPIQuery(device.UserID,
                UserInfoDeleteUrl,
                JsonConvert.SerializeObject(payload),
                out string outputResult,
                out string outputStatus);

            if (!result)
            {
                string errorMessage = ParseErrorMessage(outputStatus ?? outputResult);
                return DeviceUpdateResult.Fail(string.Format(CultureInfo.InvariantCulture,
                    "设备 {0} 删除人员 {1} 失败：{2}",
                    device.Name,
                    employeeId,
                    errorMessage));
            }

            if (!IsResponseOk(outputResult))
            {
                string errorMessage = ParseErrorMessage(outputResult);
                return DeviceUpdateResult.Fail(string.Format(CultureInfo.InvariantCulture,
                    "设备 {0} 返回错误，人员 {1} 未删除：{2}",
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
                EnsureDevicesLoaded();
                // 排除人脸录入仪设备，该设备仅用于连接和人脸录入功能
                List<DeviceConnectionInfo> onlineDevices = deviceManager.GetAllDevices()
                    .Where(d => d.IsEnabled && d.IsConnected && d.UserID >= 0 && !IsEnrollmentDevice(d))
                    .ToList();

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

            if (!device.IsConnected || device.UserID < 0)
            {
                bool connected = deviceManager.ConnectToDevice(device);
                if (!connected || device.UserID < 0)
                {
                    return DeviceUpdateResult.Fail(string.Format(CultureInfo.InvariantCulture,
                        "无法连接设备 {0}，删除人员 {1} 的人脸失败。", device.Name, employeeId));
                }
            }

            // 根据技术规范2.4.3.3，FPID为数组格式，支持批量删除
            // FDID和faceLibType已在URL参数中指定
            var payload = new
            {
                FPID = new[]
                {
                    new { value = employeeId }
                }
            };

            bool result = commonHelper.ISAPIQuery(device.UserID,
                FaceDeleteUrl,
                JsonConvert.SerializeObject(payload),
                out string outputResult,
                out string outputStatus);

            if (!result)
            {
                string errorMessage = ParseErrorMessage(outputStatus ?? outputResult);
                return DeviceUpdateResult.Fail(string.Format(CultureInfo.InvariantCulture,
                    "设备 {0} 删除人员 {1} 的人脸失败：{2}",
                    device.Name,
                    employeeId,
                    errorMessage));
            }

            if (!IsResponseOk(outputResult))
            {
                string errorMessage = ParseErrorMessage(outputResult);
                return DeviceUpdateResult.Fail(string.Format(CultureInfo.InvariantCulture,
                    "设备 {0} 返回错误，人员 {1} 人脸未删除：{2}",
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

            if (!device.IsConnected || device.UserID < 0)
            {
                bool connected = deviceManager.ConnectToDevice(device);
                if (!connected || device.UserID < 0)
                {
                    result.ErrorMessage = string.Format(CultureInfo.InvariantCulture,
                        "无法连接设备 {0}，查询人员 {1} 人脸失败。", device.Name, employeeId);
                    return result;
                }
            }

            // 根据技术规范2.4.1.4，faceLibType为必填字段
            var payload = new
            {
                searchResultPosition = 0,
                maxResults = 1,
                faceLibType = DefaultFaceLibType,
                FDID = DefaultFaceLibId,
                FPID = employeeId
            };

            bool ok = commonHelper.ISAPIQuery(device.UserID,
                FaceSearchUrl,
                JsonConvert.SerializeObject(payload),
                out string outputResult,
                out string outputStatus);

            result.RawResponse = string.IsNullOrWhiteSpace(outputResult) ? outputStatus : outputResult;

            if (!ok)
            {
                result.ErrorMessage = ParseErrorMessage(outputStatus ?? outputResult);
                return result;
            }

            // 尝试从 multipart 响应中提取 JSON 部分
            string jsonContent = ExtractJsonFromMultipart(outputResult);

            // 检查响应是否成功
            if (!IsResponseOkFromContent(jsonContent))
            {
                result.ErrorMessage = ParseErrorMessage(jsonContent);
                return result;
            }

            try
            {
                JToken root = JToken.Parse(jsonContent);

                // 尝试从 MatchList 或 FaceDataRecord 中获取数据
                JToken dataList = root["MatchList"] ?? root["FaceDataRecord"];
                if (dataList is JArray arr && arr.Count > 0)
                {
                    JToken first = arr[0];

                    // 尝试多种可能的字段名称
                    string face = first.Value<string>("facePicBinary") ??
                                  first.Value<string>("FacePicBinary") ??
                                  first.Value<string>("facePic") ??
                                  first.Value<string>("FacePic") ??
                                  first.Value<string>("modelData");  // 某些设备返回模型数据而非图片

                    result.FaceImageBase64 = face;
                }

                // 检查是否有匹配结果
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
                // 解析失败，但如果原始响应包含成功标记，仍视为成功
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
                string statusCode = data.Value<string>("statusCode");
                string statusString = data.Value<string>("statusString");
                string subStatusCode = data.Value<string>("subStatusCode");

                // statusCode 为 "1" 或 1 都表示成功
                bool codeOk = statusCode == "1" || data.Value<int?>("statusCode") == 1;
                bool stringOk = string.Equals(statusString, "OK", StringComparison.OrdinalIgnoreCase);
                bool subCodeOk = string.Equals(subStatusCode, "ok", StringComparison.OrdinalIgnoreCase);

                return codeOk && stringOk && subCodeOk;
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
            if (string.IsNullOrWhiteSpace(response))
            {
                return false;
            }

            try
            {
                JObject data = JObject.Parse(response);
                string statusCode = data.Value<string>("statusCode");
                string statusString = data.Value<string>("statusString");
                string subStatusCode = data.Value<string>("subStatusCode");

                return statusCode == "1"
                    && string.Equals(statusString, "OK", StringComparison.OrdinalIgnoreCase)
                    && string.Equals(subStatusCode, "ok", StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                return false;
            }
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

        private bool UpdateSyncedLevel(string employeeId, int permissionLevel)
        {
            string connStr = commonHelper.obtenerCadenaConexion();
            SqlServerDatabase db = new SqlServerDatabase(commonHelper.obtenerTiempoEsperaComando());
            db.Connect(connStr);
            if (db.Connection == null)
            {
                return false;
            }

            try
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
            finally
            {
                db.Disconnect();
            }
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

            public List<string> Errors { get; } = new List<string>();
        }

        private class DeviceUpdateResult
        {
            public bool Success { get; private set; }

            public string ErrorMessage { get; private set; }

            public static DeviceUpdateResult SuccessResult { get; } = new DeviceUpdateResult { Success = true };

            public static DeviceUpdateResult Fail(string message)
            {
                return new DeviceUpdateResult
                {
                    Success = false,
                    ErrorMessage = message
                };
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

        public List<string> Errors { get; } = new List<string>();
    }


    /// <summary>
    /// 统计人员删除操作的结果摘要。
    /// </summary>
    public class PersonDeleteSummary
    {
        /// <summary>
        /// 请求删除的人员总数。
        /// </summary>
        public int Total { get; set; }

        /// <summary>
        /// 成功删除的人员数量（至少在一个设备上成功）。
        /// </summary>
        public int Succeeded { get; set; }

        /// <summary>
        /// 删除失败的人员数量（在所有设备上都失败）。
        /// </summary>
        public int Failed { get; set; }

        /// <summary>
        /// 目标设备数量。
        /// </summary>
        public int TargetDevices { get; set; }

        /// <summary>
        /// 错误消息列表。
        /// </summary>
        public List<string> Errors { get; } = new List<string>();

        /// <summary>
        /// 每个人员的详细删除结果。
        /// </summary>
        public List<PersonDeleteItem> Items { get; } = new List<PersonDeleteItem>();
    }

    /// <summary>
    /// 单个人员的删除操作结果。
    /// </summary>
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
