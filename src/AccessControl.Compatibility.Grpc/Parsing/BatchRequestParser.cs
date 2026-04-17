using System;
using System.Collections.Generic;
using System.Globalization;
using ControlEntradaSalida.Application.Models;
using Newtonsoft.Json.Linq;

namespace ControlEntradaSalida.Compatibility.Grpc.Parsing
{
    public static class BatchRequestParser
    {
        public static IReadOnlyList<string> ParseEmployeeIds(string payload)
        {
            if (string.IsNullOrWhiteSpace(payload))
            {
                throw new ArgumentException("请求体不能为空。");
            }

            JToken root = JToken.Parse(payload);
            var ids = new List<string>();

            void Add(JToken token)
            {
                if (token == null || token.Type == JTokenType.Null)
                {
                    return;
                }

                if (token.Type == JTokenType.String || token.Type == JTokenType.Integer)
                {
                    ids.Add(token.ToString());
                    return;
                }

                if (token.Type == JTokenType.Object)
                {
                    string value = token.Value<string>("employee_id")
                        ?? token.Value<string>("employeeId")
                        ?? token.Value<string>("employee_no")
                        ?? token.Value<string>("employeeNo");
                    if (!string.IsNullOrWhiteSpace(value))
                    {
                        ids.Add(value);
                    }
                }
            }

            if (root.Type == JTokenType.Array)
            {
                foreach (JToken item in root)
                {
                    Add(item);
                }
            }
            else if (root.Type == JTokenType.Object)
            {
                if (root["items"] is JArray items)
                {
                    foreach (JToken item in items)
                    {
                        Add(item);
                    }
                }
                else if (root["records"] is JArray records)
                {
                    foreach (JToken item in records)
                    {
                        Add(item);
                    }
                }
                else
                {
                    Add(root);
                }
            }
            else
            {
                Add(root);
            }

            return ids;
        }

        public static EnrollmentStatusQuery ParseEnrollmentStatus(string payload)
        {
            JToken root = JToken.Parse(payload);
            string taskId = root.Value<string>("taskId") ?? root.Value<string>("task_id");
            if (string.IsNullOrWhiteSpace(taskId))
            {
                throw new ArgumentException("缺少 taskId。");
            }

            return new EnrollmentStatusQuery(taskId);
        }

        public static CaptureFaceStreamCommand ParseCaptureFace(string payload)
        {
            JToken root = JToken.Parse(payload);
            string employeeId = root.Value<string>("employee_id")
                ?? root.Value<string>("employeeId")
                ?? root.Value<string>("employee_no")
                ?? root.Value<string>("employeeNo");
            if (string.IsNullOrWhiteSpace(employeeId))
            {
                throw new ArgumentException("缺少 employee_id。");
            }

            return new CaptureFaceStreamCommand(employeeId);
        }

        public static DeviceStatusQuery ParseDeviceStatusQuery(string payload)
        {
            JObject root = string.IsNullOrWhiteSpace(payload) ? new JObject() : JObject.Parse(payload);
            return new DeviceStatusQuery(
                includeDisabled: root.Value<bool?>("includeDisabled") ?? true,
                refresh: root.Value<bool?>("refresh") ?? false,
                deviceId: ReadInt(root, "deviceId", "device_id"),
                deviceIds: ReadIntList(root, "deviceIds", "device_ids"),
                ipAddress: root.Value<string>("ipAddress") ?? root.Value<string>("ip_address"));
        }

        public static AddDeviceCommand ParseAddDevice(string payload)
        {
            JObject root = JObject.Parse(payload);
            return new AddDeviceCommand
            {
                DeviceId = ReadInt(root, "deviceId", "device_id") ?? 0,
                DeviceName = root.Value<string>("deviceName") ?? root.Value<string>("device_name"),
                IpAddress = root.Value<string>("ipAddress") ?? root.Value<string>("ip_address"),
                Port = root.Value<string>("port") ?? "8000",
                Username = root.Value<string>("username") ?? "admin",
                Password = root.Value<string>("password"),
                Description = root.Value<string>("description"),
                Enabled = root.Value<bool?>("enabled") ?? true,
                ConnectNow = root.Value<bool?>("connectNow") ?? false
            };
        }

        public static DeleteDeviceCommand ParseDeleteDevice(string payload)
        {
            JObject root = JObject.Parse(payload);
            return new DeleteDeviceCommand
            {
                DeviceId = ReadInt(root, "deviceId", "device_id") ?? 0,
                DisconnectFirst = root.Value<bool?>("disconnectFirst") ?? true
            };
        }

        public static DisconnectDeviceCommand ParseDisconnectDevice(string payload)
        {
            JObject root = JObject.Parse(payload);
            return new DisconnectDeviceCommand
            {
                DeviceId = ReadInt(root, "deviceId", "device_id") ?? 0
            };
        }

        public static ReconnectDeviceCommand ParseReconnectDevice(string payload)
        {
            JObject root = JObject.Parse(payload);
            return new ReconnectDeviceCommand
            {
                DeviceId = ReadInt(root, "deviceId", "device_id") ?? 0,
                Force = root.Value<bool?>("force") ?? false
            };
        }

        private static int? ReadInt(JObject root, string camelName, string snakeName)
        {
            JToken token = root[camelName] ?? root[snakeName];
            if (token == null || token.Type == JTokenType.Null)
            {
                return null;
            }

            if (token.Type == JTokenType.Integer)
            {
                return token.Value<int>();
            }

            if (int.TryParse(token.ToString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int value))
            {
                return value;
            }

            return null;
        }

        private static IReadOnlyList<int> ReadIntList(JObject root, string camelName, string snakeName)
        {
            JToken token = root[camelName] ?? root[snakeName];
            var values = new List<int>();
            if (token is JArray array)
            {
                foreach (JToken item in array)
                {
                    if (int.TryParse(item.ToString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int value))
                    {
                        values.Add(value);
                    }
                }
            }

            return values;
        }
    }
}
