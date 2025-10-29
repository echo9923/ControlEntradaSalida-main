using MySql.Data.MySqlClient;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace ControlEntradaSalida
{
    public class PermissionRefreshManager
    {
        private static readonly string DefaultBeginTime = "2022-01-01T00:00:00";
        private static readonly string DefaultEndTime = "2035-12-31T23:59:59";

        private readonly DeviceConnectionManager deviceManager;
        private readonly Common commonHelper;
        private readonly object refreshLock = new object();

        public PermissionRefreshManager()
        {
            deviceManager = DeviceConnectionManager.Instance;
            commonHelper = new Common();
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
            BaseDatosMySQL bd = new BaseDatosMySQL();
            bd.conectarMySQL(connStr);
            if (bd.conn == null)
            {
                return devices;
            }

            try
            {
                string sql = "SELECT device_id, device_name, description, status FROM devices";
                MySqlCommand cmd = new MySqlCommand(sql, bd.conn);
                MySqlDataReader reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    bool isEnabled = Convert.ToInt32(reader["status"]) == 1;
                    if (!isEnabled)
                    {
                        continue;
                    }

                    int deviceId = Convert.ToInt32(reader["device_id"]);
                    string name = reader["device_name"].ToString();
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

                reader.Close();
            }
            finally
            {
                bd.desconectarMySQL();
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
            BaseDatosMySQL bd = new BaseDatosMySQL();
            bd.conectarMySQL(connStr);
            if (bd.conn == null)
            {
                return users;
            }

            try
            {
                // 直接从 employees 表查询，权限信息已合并到此表
                string sql = @"SELECT employee_id,
                                      full_name,
                                      permission_level,
                                      last_synced_level
                               FROM employees
                               ORDER BY employee_id";

                MySqlCommand cmd = new MySqlCommand(sql, bd.conn);
                MySqlDataReader reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    string employeeId = reader["employee_id"].ToString();
                    string fullName = reader["full_name"] == DBNull.Value ? string.Empty : reader["full_name"].ToString();

                    // 权限级别现在有默认值 0，不会是 NULL
                    int permissionLevel = reader["permission_level"] != DBNull.Value
                        ? Convert.ToInt32(reader["permission_level"])
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

                reader.Close();
            }
            finally
            {
                bd.desconectarMySQL();
            }

            return users;
        }

        private UserPermissionRecord LoadUserPermission(string employeeId)
        {
            string connStr = commonHelper.obtenerCadenaConexion();
            BaseDatosMySQL bd = new BaseDatosMySQL();
            bd.conectarMySQL(connStr);
            if (bd.conn == null)
            {
                return null;
            }

            try
            {
                string sql = @"SELECT employee_id,
                                      full_name,
                                      permission_level,
                                      last_synced_level
                               FROM employees
                               WHERE employee_id = @employee_id
                               LIMIT 1";

                MySqlCommand cmd = new MySqlCommand(sql, bd.conn);
                cmd.Parameters.AddWithValue("@employee_id", employeeId);

                MySqlDataReader reader = cmd.ExecuteReader();
                if (reader.Read())
                {
                    UserPermissionRecord record = new UserPermissionRecord
                    {
                        EmployeeId = reader["employee_id"].ToString(),
                        FullName = reader["full_name"] == DBNull.Value ? string.Empty : reader["full_name"].ToString(),
                        PermissionLevel = reader["permission_level"] != DBNull.Value
                            ? Convert.ToInt32(reader["permission_level"])
                            : 0,
                        LastSyncedLevel = reader["last_synced_level"] != DBNull.Value
                            ? Convert.ToInt32(reader["last_synced_level"])
                            : (int?)null
                    };

                    reader.Close();
                    return record;
                }

                reader.Close();
                return null;
            }
            finally
            {
                bd.desconectarMySQL();
            }
        }

        private bool UpdatePermissionLevel(string employeeId, int permissionLevel)
        {
            string connStr = commonHelper.obtenerCadenaConexion();
            BaseDatosMySQL bd = new BaseDatosMySQL();
            bd.conectarMySQL(connStr);
            if (bd.conn == null)
            {
                return false;
            }

            try
            {
                string sql = @"UPDATE employees
                               SET permission_level = @level
                               WHERE employee_id = @employee_id";

                MySqlCommand cmd = new MySqlCommand(sql, bd.conn);
                cmd.Parameters.AddWithValue("@level", permissionLevel);
                cmd.Parameters.AddWithValue("@employee_id", employeeId);

                int affected = cmd.ExecuteNonQuery();
                return affected > 0;
            }
            finally
            {
                bd.desconectarMySQL();
            }
        }

        private void InsertDefaultPermissionRecords(IEnumerable<string> employeeIds)
        {
            // 注意：权限信息已合并到 employees 表，且 permission_level 字段有默认值 0
            // 新增员工时会自动设置默认权限，此方法不再需要执行任何操作
            // 保留此方法以维持代码兼容性
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
                "PUT /ISAPI/AccessControl/UserInfo/SetUp?format=json",
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
            BaseDatosMySQL bd = new BaseDatosMySQL();
            bd.conectarMySQL(connStr);
            if (bd.conn == null)
            {
                return false;
            }

            try
            {
                // 直接更新 employees 表中的同步状态字段
                string sql = @"UPDATE employees
                               SET last_synced_level = @level,
                                   last_synced_at = CURRENT_TIMESTAMP
                               WHERE employee_id = @employee_id";

                MySqlCommand cmd = new MySqlCommand(sql, bd.conn);
                cmd.Parameters.AddWithValue("@level", permissionLevel);
                cmd.Parameters.AddWithValue("@employee_id", employeeId);

                int affected = cmd.ExecuteNonQuery();
                return affected > 0;
            }
            finally
            {
                bd.desconectarMySQL();
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
}
