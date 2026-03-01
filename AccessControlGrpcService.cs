using Grpc.Core;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ControlEntradaSalida
{
    /// <summary>
    /// 门禁设备管理 gRPC 服务实现。
    /// 说明：沿用 string + JSON 载荷模式，便于与现有 Permission gRPC 接口保持一致。
    /// </summary>
    public sealed class AccessControlGrpcService
    {
        private const string ServiceName = "device.AccessControlService";

        private const string GetDeviceStatusMethodName = "GetDeviceStatus";
        private const string AddDeviceMethodName = "AddDevice";
        private const string DeleteDeviceMethodName = "DeleteDevice";
        private const string DisconnectDeviceMethodName = "DisconnectDevice";
        private const string ReconnectDeviceMethodName = "ReconnectDevice";

        private const string GrpcLogPrefix = "[门禁管理GRPC]";

        private static readonly string[] RequestIdHeaderCandidates = new[]
        {
            "x-request-id",
            "x-correlation-id",
            "x-trace-id"
        };

        private static readonly Marshaller<string> StringMarshaller = Marshallers.Create(
            Encoding.UTF8.GetBytes,
            bytes => Encoding.UTF8.GetString(bytes ?? Array.Empty<byte>()));

        private static readonly Method<string, string> GetDeviceStatusMethod = new Method<string, string>(
            MethodType.Unary,
            ServiceName,
            GetDeviceStatusMethodName,
            StringMarshaller,
            StringMarshaller);

        private static readonly Method<string, string> AddDeviceMethod = new Method<string, string>(
            MethodType.Unary,
            ServiceName,
            AddDeviceMethodName,
            StringMarshaller,
            StringMarshaller);

        private static readonly Method<string, string> DeleteDeviceMethod = new Method<string, string>(
            MethodType.Unary,
            ServiceName,
            DeleteDeviceMethodName,
            StringMarshaller,
            StringMarshaller);

        private static readonly Method<string, string> DisconnectDeviceMethod = new Method<string, string>(
            MethodType.Unary,
            ServiceName,
            DisconnectDeviceMethodName,
            StringMarshaller,
            StringMarshaller);

        private static readonly Method<string, string> ReconnectDeviceMethod = new Method<string, string>(
            MethodType.Unary,
            ServiceName,
            ReconnectDeviceMethodName,
            StringMarshaller,
            StringMarshaller);

        private readonly DeviceConnectionManager deviceManager;
        private readonly bool logPayloads;
        private readonly int payloadLogMaxChars;
        private readonly string requiredApiKey;
        private readonly bool enforceApiKey;
        private int warnedMissingApiKey;

        public AccessControlGrpcService(DeviceConnectionManager deviceManager,
            bool logPayloads,
            int payloadLogMaxChars,
            string apiKey)
        {
            this.deviceManager = deviceManager ?? throw new ArgumentNullException(nameof(deviceManager));
            this.logPayloads = logPayloads;
            this.payloadLogMaxChars = payloadLogMaxChars > 0 ? payloadLogMaxChars : 0;

            if (string.IsNullOrWhiteSpace(apiKey))
            {
                enforceApiKey = false;
                requiredApiKey = null;
                warnedMissingApiKey = 0;
            }
            else
            {
                enforceApiKey = true;
                requiredApiKey = apiKey.Trim();
                warnedMissingApiKey = 1;
            }
        }

        public ServerServiceDefinition BuildServiceDefinition()
        {
            WarnIfApiKeyNotConfigured();

            return ServerServiceDefinition.CreateBuilder()
                .AddMethod(GetDeviceStatusMethod, HandleGetDeviceStatusAsync)
                .AddMethod(AddDeviceMethod, HandleAddDeviceAsync)
                .AddMethod(DeleteDeviceMethod, HandleDeleteDeviceAsync)
                .AddMethod(DisconnectDeviceMethod, HandleDisconnectDeviceAsync)
                .AddMethod(ReconnectDeviceMethod, HandleReconnectDeviceAsync)
                .Build();
        }

        private Task<string> HandleGetDeviceStatusAsync(string request, ServerCallContext context)
        {
            string requestId = ResolveRequestId(context);
            LogGrpcSummary(requestId, GetDeviceStatusMethodName, request);

            EnsureAuthorized(context, requestId);

            // 空请求等价于 "{}"，用于“查询全部设备”。
            request = string.IsNullOrWhiteSpace(request) ? "{}" : request;

            JObject root;
            try
            {
                root = JObject.Parse(request);
            }
            catch (JsonException ex)
            {
                string errorMessage = $"JSON格式错误：{ex.Message}";
                throw BuildRpcException(StatusCode.InvalidArgument, requestId, GrpcErrorCodes.InvalidArgument, errorMessage);
            }

            bool includeDisabled = root.Value<bool?>("includeDisabled") ?? true;
            bool refresh = root.Value<bool?>("refresh") ?? false;

            int? singleDeviceId = ReadInt(root, "deviceId", "device_id");
            List<int> deviceIds = ReadIntList(root, "deviceIds", "device_ids");
            string ipAddress = ReadString(root, "ipAddress", "ip_address");

            List<DeviceConnectionInfo> targetDevices = new List<DeviceConnectionInfo>();
            List<GrpcErrorDetail> details = new List<GrpcErrorDetail>();

            if (singleDeviceId.HasValue)
            {
                DeviceConnectionInfo device = deviceManager.GetDeviceById(singleDeviceId.Value);
                if (device == null)
                {
                    throw BuildRpcException(StatusCode.NotFound, requestId, GrpcErrorCodes.NotFound,
                        $"设备 {singleDeviceId.Value} 不存在。",
                        null,
                        new[]
                        {
                            new GrpcErrorDetail
                            {
                                DeviceId = singleDeviceId.Value,
                                Code = GrpcErrorCodes.NotFound,
                                Message = "设备不存在。"
                            }
                        });
                }

                targetDevices.Add(device);
            }
            else if (deviceIds.Count > 0)
            {
                foreach (int deviceId in deviceIds.Distinct())
                {
                    DeviceConnectionInfo device = deviceManager.GetDeviceById(deviceId);
                    if (device == null)
                    {
                        details.Add(new GrpcErrorDetail
                        {
                            DeviceId = deviceId,
                            Code = GrpcErrorCodes.NotFound,
                            Message = "设备不存在。"
                        });
                        continue;
                    }

                    targetDevices.Add(device);
                }

                if (targetDevices.Count == 0)
                {
                    throw BuildRpcException(StatusCode.NotFound, requestId, GrpcErrorCodes.NotFound,
                        "请求的设备均不存在。",
                        new[] { "请求的设备均不存在。" },
                        details);
                }
            }
            else if (!string.IsNullOrWhiteSpace(ipAddress))
            {
                string ipKey = ipAddress.Trim();
                if (!deviceManager.TryGetDeviceByIp(ipKey, out DeviceConnectionInfo device) || device == null)
                {
                    throw BuildRpcException(StatusCode.NotFound, requestId, GrpcErrorCodes.NotFound,
                        $"设备 {ipKey} 不存在。",
                        null,
                        new[]
                        {
                            new GrpcErrorDetail
                            {
                                DeviceIp = ipKey,
                                Code = GrpcErrorCodes.NotFound,
                                Message = "设备不存在。"
                            }
                        });
                }

                targetDevices.Add(device);
            }
            else
            {
                targetDevices = deviceManager.GetAllDevices();
            }

            if (!includeDisabled)
            {
                targetDevices = targetDevices.Where(d => d != null && d.IsEnabled).ToList();
            }

            if (refresh)
            {
                foreach (DeviceConnectionInfo device in targetDevices)
                {
                    if (device == null)
                    {
                        continue;
                    }

                    try
                    {
                        deviceManager.CheckDeviceStatus(device);
                    }
                    catch (Exception ex)
                    {
                        details.Add(new GrpcErrorDetail
                        {
                            DeviceId = device.Id,
                            DeviceName = device.Name,
                            DeviceIp = device.IpAddress,
                            Code = GrpcErrorCodes.InternalError,
                            Message = $"刷新状态异常: {ex.Message}"
                        });
                    }
                }
            }

            var devicesPayload = targetDevices
                .Where(d => d != null)
                .Select(BuildDeviceStatusPayload)
                .ToList();

            bool success = details.Count == 0;
            string code = success ? GrpcErrorCodes.Ok : GrpcErrorCodes.PartialSuccess;
            string message = success ? "查询成功。" : "查询完成，但存在部分异常或缺失。";

            var payload = new
            {
                devices = devicesPayload
            };

            string responsePayload = BuildStandardPayload(
                requestId,
                success,
                code,
                message,
                details.Select(d => d.Message).Where(m => !string.IsNullOrWhiteSpace(m)).Distinct().ToList(),
                details,
                payload);

            return Task.FromResult(responsePayload);
        }

        private Task<string> HandleAddDeviceAsync(string request, ServerCallContext context)
        {
            string requestId = ResolveRequestId(context);
            LogGrpcSummary(requestId, AddDeviceMethodName, request);

            EnsureAuthorized(context, requestId);

            if (string.IsNullOrWhiteSpace(request))
            {
                throw BuildRpcException(StatusCode.InvalidArgument, requestId, GrpcErrorCodes.InvalidArgument, "请求体不能为空。",
                    new[] { "请求体不能为空。" });
            }

            JObject root;
            try
            {
                root = JObject.Parse(request);
            }
            catch (JsonException ex)
            {
                throw BuildRpcException(StatusCode.InvalidArgument, requestId, GrpcErrorCodes.InvalidArgument,
                    $"JSON格式错误：{ex.Message}");
            }

            int? deviceId = ReadInt(root, "deviceId", "device_id");
            if (!deviceId.HasValue || deviceId.Value <= 0)
            {
                throw BuildRpcException(StatusCode.InvalidArgument, requestId, GrpcErrorCodes.InvalidArgument, "缺少或非法的 deviceId。",
                    new[] { "缺少或非法的 deviceId。" });
            }

            string deviceName = ReadString(root, "deviceName", "device_name");
            if (string.IsNullOrWhiteSpace(deviceName))
            {
                throw BuildRpcException(StatusCode.InvalidArgument, requestId, GrpcErrorCodes.InvalidArgument, "缺少 deviceName。",
                    new[] { "缺少 deviceName。" });
            }

            string ipAddress = ReadString(root, "ipAddress", "ip_address");
            if (string.IsNullOrWhiteSpace(ipAddress))
            {
                throw BuildRpcException(StatusCode.InvalidArgument, requestId, GrpcErrorCodes.InvalidArgument, "缺少 ipAddress。",
                    new[] { "缺少 ipAddress。" });
            }

            string port = ReadString(root, "port") ?? "8000";
            if (!ushort.TryParse(port, out ushort parsedPort) || parsedPort <= 0)
            {
                throw BuildRpcException(StatusCode.InvalidArgument, requestId, GrpcErrorCodes.InvalidArgument, "port 必须为 1-65535 的数字字符串。",
                    new[] { "port 必须为 1-65535 的数字字符串。" });
            }

            string username = ReadString(root, "username") ?? "admin";
            string password = ReadString(root, "password");
            if (string.IsNullOrWhiteSpace(password))
            {
                throw BuildRpcException(StatusCode.InvalidArgument, requestId, GrpcErrorCodes.InvalidArgument, "缺少 password。",
                    new[] { "缺少 password。" });
            }

            string description = ReadString(root, "description");
            bool enabled = root.Value<bool?>("enabled") ?? true;
            bool connectNow = root.Value<bool?>("connectNow") ?? false;

            var device = new DeviceConnectionInfo
            {
                Id = deviceId.Value,
                Name = deviceName.Trim(),
                IpAddress = ipAddress.Trim(),
                Port = port.Trim(),
                Username = username?.Trim() ?? "admin",
                Password = password,
                IsEnabled = enabled,
                LastUsed = DateTime.MinValue
            };

            if (!deviceManager.TryAddDevice(device, description, out string errorMessage))
            {
                ThrowFromManagerError(requestId, errorMessage);
            }

            bool connected = false;
            string connectionMessage = string.Empty;

            if (connectNow)
            {
                try
                {
                    connected = deviceManager.ConnectToDevice(device);
                    connectionMessage = connected
                        ? "连接成功。"
                        : (device.StatusMessage ?? "连接失败。");
                }
                catch (Exception ex)
                {
                    connected = false;
                    connectionMessage = $"连接异常: {ex.Message}";
                }
            }

            bool success = !connectNow || connected;
            string code = success ? GrpcErrorCodes.Ok : GrpcErrorCodes.PartialSuccess;
            string message = success
                ? (connectNow ? "新增并连接成功。" : "新增成功。")
                : "新增成功，但连接失败。";

            var payload = new
            {
                device = BuildDeviceStatusPayload(device),
                connectNow,
                connected,
                connectionMessage
            };

            var errors = new List<string>();
            if (!success && !string.IsNullOrWhiteSpace(connectionMessage))
            {
                errors.Add(connectionMessage);
            }

            string responsePayload = BuildStandardPayload(
                requestId,
                success,
                code,
                message,
                errors,
                Array.Empty<GrpcErrorDetail>(),
                payload);

            return Task.FromResult(responsePayload);
        }

        private Task<string> HandleDeleteDeviceAsync(string request, ServerCallContext context)
        {
            string requestId = ResolveRequestId(context);
            LogGrpcSummary(requestId, DeleteDeviceMethodName, request);

            EnsureAuthorized(context, requestId);

            if (string.IsNullOrWhiteSpace(request))
            {
                throw BuildRpcException(StatusCode.InvalidArgument, requestId, GrpcErrorCodes.InvalidArgument, "请求体不能为空。",
                    new[] { "请求体不能为空。" });
            }

            JObject root;
            try
            {
                root = JObject.Parse(request);
            }
            catch (JsonException ex)
            {
                throw BuildRpcException(StatusCode.InvalidArgument, requestId, GrpcErrorCodes.InvalidArgument,
                    $"JSON格式错误：{ex.Message}");
            }

            int? deviceId = ReadInt(root, "deviceId", "device_id");
            if (!deviceId.HasValue || deviceId.Value <= 0)
            {
                throw BuildRpcException(StatusCode.InvalidArgument, requestId, GrpcErrorCodes.InvalidArgument, "缺少或非法的 deviceId。",
                    new[] { "缺少或非法的 deviceId。" });
            }

            bool disconnectFirst = root.Value<bool?>("disconnectFirst") ?? true;

            if (!deviceManager.TryDeleteDevice(deviceId.Value, disconnectFirst, out string errorMessage))
            {
                ThrowFromManagerError(requestId, errorMessage);
            }

            var payload = new
            {
                deleted = true,
                deviceId = deviceId.Value
            };

            string responsePayload = BuildStandardPayload(
                requestId,
                true,
                GrpcErrorCodes.Ok,
                "删除成功。",
                Array.Empty<string>(),
                Array.Empty<GrpcErrorDetail>(),
                payload);

            return Task.FromResult(responsePayload);
        }

        private Task<string> HandleDisconnectDeviceAsync(string request, ServerCallContext context)
        {
            string requestId = ResolveRequestId(context);
            LogGrpcSummary(requestId, DisconnectDeviceMethodName, request);

            EnsureAuthorized(context, requestId);

            if (string.IsNullOrWhiteSpace(request))
            {
                throw BuildRpcException(StatusCode.InvalidArgument, requestId, GrpcErrorCodes.InvalidArgument, "请求体不能为空。",
                    new[] { "请求体不能为空。" });
            }

            JObject root;
            try
            {
                root = JObject.Parse(request);
            }
            catch (JsonException ex)
            {
                throw BuildRpcException(StatusCode.InvalidArgument, requestId, GrpcErrorCodes.InvalidArgument,
                    $"JSON格式错误：{ex.Message}");
            }

            int? deviceId = ReadInt(root, "deviceId", "device_id");
            if (!deviceId.HasValue || deviceId.Value <= 0)
            {
                throw BuildRpcException(StatusCode.InvalidArgument, requestId, GrpcErrorCodes.InvalidArgument, "缺少或非法的 deviceId。",
                    new[] { "缺少或非法的 deviceId。" });
            }

            DeviceConnectionInfo device = deviceManager.GetDeviceById(deviceId.Value);
            if (device == null)
            {
                throw BuildRpcException(StatusCode.NotFound, requestId, GrpcErrorCodes.NotFound, $"设备 {deviceId.Value} 不存在。",
                    null,
                    new[] { new GrpcErrorDetail { DeviceId = deviceId.Value, Code = GrpcErrorCodes.NotFound, Message = "设备不存在。" } });
            }

            deviceManager.DisconnectDevice(device);

            JObject snapshot = BuildDeviceStatusPayload(device);
            bool isConnected = snapshot?.Value<bool?>("isConnected") ?? false;

            bool success = !isConnected;
            string code = success ? GrpcErrorCodes.Ok : GrpcErrorCodes.Failed;
            string message = success ? "已断开连接。" : "断开请求已执行，但设备仍处于连接状态（可能设备忙碌）。";

            var payload = new
            {
                deviceId = deviceId.Value,
                isConnected,
                status = snapshot?.Value<string>("status"),
                message
            };

            string responsePayload = BuildStandardPayload(
                requestId,
                success,
                code,
                message,
                success ? Array.Empty<string>() : new[] { message },
                Array.Empty<GrpcErrorDetail>(),
                payload);

            return Task.FromResult(responsePayload);
        }

        private Task<string> HandleReconnectDeviceAsync(string request, ServerCallContext context)
        {
            string requestId = ResolveRequestId(context);
            LogGrpcSummary(requestId, ReconnectDeviceMethodName, request);

            EnsureAuthorized(context, requestId);

            if (string.IsNullOrWhiteSpace(request))
            {
                throw BuildRpcException(StatusCode.InvalidArgument, requestId, GrpcErrorCodes.InvalidArgument, "请求体不能为空。",
                    new[] { "请求体不能为空。" });
            }

            JObject root;
            try
            {
                root = JObject.Parse(request);
            }
            catch (JsonException ex)
            {
                throw BuildRpcException(StatusCode.InvalidArgument, requestId, GrpcErrorCodes.InvalidArgument,
                    $"JSON格式错误：{ex.Message}");
            }

            int? deviceId = ReadInt(root, "deviceId", "device_id");
            if (!deviceId.HasValue || deviceId.Value <= 0)
            {
                throw BuildRpcException(StatusCode.InvalidArgument, requestId, GrpcErrorCodes.InvalidArgument, "缺少或非法的 deviceId。",
                    new[] { "缺少或非法的 deviceId。" });
            }

            bool force = root.Value<bool?>("force") ?? false;

            if (!deviceManager.TryReconnectDevice(deviceId.Value, force, out bool connected, out string message))
            {
                throw BuildRpcException(StatusCode.NotFound, requestId, GrpcErrorCodes.NotFound, message ?? "设备不存在。",
                    null,
                    new[] { new GrpcErrorDetail { DeviceId = deviceId.Value, Code = GrpcErrorCodes.NotFound, Message = message ?? "设备不存在。" } });
            }

            bool success = connected;
            string code = success ? GrpcErrorCodes.Ok : GrpcErrorCodes.Failed;
            string payloadMessage = string.IsNullOrWhiteSpace(message)
                ? (connected ? "连接成功。" : "连接失败。")
                : message;

            var payload = new
            {
                deviceId = deviceId.Value,
                connected,
                message = payloadMessage
            };

            string responsePayload = BuildStandardPayload(
                requestId,
                success,
                code,
                payloadMessage,
                success ? Array.Empty<string>() : new[] { payloadMessage },
                Array.Empty<GrpcErrorDetail>(),
                payload);

            return Task.FromResult(responsePayload);
        }

        private void WarnIfApiKeyNotConfigured()
        {
            if (enforceApiKey)
            {
                return;
            }

            if (Interlocked.Exchange(ref warnedMissingApiKey, 1) == 1)
            {
                return;
            }

            ServiceLogger.Warn($"{GrpcLogPrefix} 未配置 Service.GrpcManagementApiKey，门禁管理接口将不强制鉴权，请确保仅暴露在受控网络环境。");
        }

        private void EnsureAuthorized(ServerCallContext context, string requestId)
        {
            WarnIfApiKeyNotConfigured();

            if (!enforceApiKey)
            {
                return;
            }

            string provided = null;
            if (context?.RequestHeaders != null)
            {
                Metadata.Entry entry = context.RequestHeaders.FirstOrDefault(h =>
                    string.Equals(h.Key, "x-api-key", StringComparison.OrdinalIgnoreCase));
                if (entry != null && !string.IsNullOrWhiteSpace(entry.Value))
                {
                    provided = entry.Value;
                }
            }

            if (!string.Equals(provided, requiredApiKey, StringComparison.Ordinal))
            {
                throw BuildRpcException(StatusCode.Unauthenticated, requestId, GrpcErrorCodes.Unauthenticated,
                    "未授权：缺少或错误的 x-api-key。",
                    new[] { "未授权：缺少或错误的 x-api-key。" });
            }
        }

        private static JObject BuildDeviceStatusPayload(DeviceConnectionInfo device)
        {
            if (device == null)
            {
                return null;
            }

            int id;
            string name;
            string ip;
            string port;
            bool enabled;
            bool isConnected;
            DeviceStatus status;
            string statusMessage;
            DateTime lastChecked;
            DateTime lastUsed;
            uint lastErrorCode;
            string lastErrorMessage;

            lock (device.LockObject)
            {
                id = device.Id;
                name = device.Name;
                ip = device.IpAddress;
                port = device.Port;
                enabled = device.IsEnabled;
                isConnected = device.IsConnected;
                status = device.Status;
                statusMessage = device.StatusMessage;
                lastChecked = device.LastChecked;
                lastUsed = device.LastUsed;
                lastErrorCode = device.LastErrorCode;
                lastErrorMessage = device.LastErrorMessage;
            }

            return JObject.FromObject(new
            {
                deviceId = id,
                deviceName = name,
                ipAddress = ip,
                port,
                enabled,
                isConnected,
                status = status.ToString(),
                statusMessage,
                lastChecked = lastChecked == DateTime.MinValue ? (DateTime?)null : lastChecked,
                lastUsed = lastUsed == DateTime.MinValue ? (DateTime?)null : lastUsed,
                lastErrorCode = lastErrorCode == 0 ? (uint?)null : lastErrorCode,
                lastErrorMessage
            });
        }

        private static int? ReadInt(JObject root, string camelName, string snakeName = null)
        {
            if (root == null)
            {
                return null;
            }

            JToken token = root[camelName];
            if (token == null && !string.IsNullOrWhiteSpace(snakeName))
            {
                token = root[snakeName];
            }

            if (token == null || token.Type == JTokenType.Null)
            {
                return null;
            }

            if (token.Type == JTokenType.Integer)
            {
                return token.Value<int>();
            }

            if (token.Type == JTokenType.String && int.TryParse(token.Value<string>(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsed))
            {
                return parsed;
            }

            return null;
        }

        private static List<int> ReadIntList(JObject root, string camelName, string snakeName = null)
        {
            var result = new List<int>();
            if (root == null)
            {
                return result;
            }

            JToken token = root[camelName];
            if (token == null && !string.IsNullOrWhiteSpace(snakeName))
            {
                token = root[snakeName];
            }

            if (token == null || token.Type == JTokenType.Null)
            {
                return result;
            }

            if (token.Type == JTokenType.Array)
            {
                foreach (JToken item in token)
                {
                    if (item == null || item.Type == JTokenType.Null)
                    {
                        continue;
                    }

                    if (item.Type == JTokenType.Integer)
                    {
                        int value = item.Value<int>();
                        if (value > 0)
                        {
                            result.Add(value);
                        }
                        continue;
                    }

                    if (item.Type == JTokenType.String
                        && int.TryParse(item.Value<string>(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsed)
                        && parsed > 0)
                    {
                        result.Add(parsed);
                    }
                }
            }

            return result;
        }

        private static string ReadString(JObject root, string camelName, string snakeName = null)
        {
            if (root == null)
            {
                return null;
            }

            string value = root.Value<string>(camelName);
            if (string.IsNullOrWhiteSpace(value) && !string.IsNullOrWhiteSpace(snakeName))
            {
                value = root.Value<string>(snakeName);
            }

            return string.IsNullOrWhiteSpace(value) ? null : value;
        }

        private void ThrowFromManagerError(string requestId, string errorMessage)
        {
            string code = GrpcErrorCodes.InternalError;
            string message = string.IsNullOrWhiteSpace(errorMessage) ? "操作失败。" : errorMessage;

            if (!string.IsNullOrWhiteSpace(errorMessage))
            {
                int idx = errorMessage.IndexOf(':');
                if (idx > 0)
                {
                    string candidate = errorMessage.Substring(0, idx).Trim();
                    string remainder = errorMessage.Substring(idx + 1).Trim();

                    if (string.Equals(candidate, GrpcErrorCodes.InvalidArgument, StringComparison.OrdinalIgnoreCase)
                        || string.Equals(candidate, GrpcErrorCodes.NotFound, StringComparison.OrdinalIgnoreCase)
                        || string.Equals(candidate, GrpcErrorCodes.DbError, StringComparison.OrdinalIgnoreCase)
                        || string.Equals(candidate, GrpcErrorCodes.DeviceError, StringComparison.OrdinalIgnoreCase)
                        || string.Equals(candidate, GrpcErrorCodes.SdkError, StringComparison.OrdinalIgnoreCase))
                    {
                        code = candidate;
                        message = string.IsNullOrWhiteSpace(remainder) ? message : remainder;
                    }
                }
            }

            StatusCode statusCode;
            if (string.Equals(code, GrpcErrorCodes.InvalidArgument, StringComparison.OrdinalIgnoreCase))
            {
                statusCode = StatusCode.InvalidArgument;
            }
            else if (string.Equals(code, GrpcErrorCodes.NotFound, StringComparison.OrdinalIgnoreCase))
            {
                statusCode = StatusCode.NotFound;
            }
            else if (string.Equals(code, GrpcErrorCodes.Unauthenticated, StringComparison.OrdinalIgnoreCase))
            {
                statusCode = StatusCode.Unauthenticated;
            }
            else
            {
                statusCode = StatusCode.Internal;
            }

            throw BuildRpcException(statusCode, requestId, code, message, new[] { message });
        }

        private void LogGrpcSummary(string requestId, string methodName, string request)
        {
            int payloadLength = string.IsNullOrEmpty(request) ? 0 : Encoding.UTF8.GetByteCount(request);
            ServiceLogger.Info($"{GrpcLogPrefix} 请求 {requestId} ({methodName}) 载荷长度 {payloadLength} 字节。 ");

            if (!logPayloads)
            {
                return;
            }

            string formatted = FormatPayloadForLog(request);
            ServiceLogger.Info($"{GrpcLogPrefix} 请求 {requestId} ({methodName}) 载荷: {formatted}");
        }

        private string FormatPayloadForLog(string payload)
        {
            if (string.IsNullOrWhiteSpace(payload))
            {
                return string.Empty;
            }

            string formatted = payload;
            try
            {
                JToken token = JToken.Parse(payload);
                MaskSensitiveFields(token);
                formatted = token.ToString(Formatting.None);
            }
            catch
            {
                // 忽略格式化失败，直接输出原始载荷（但仍会被截断）。
            }

            if (payloadLogMaxChars > 0 && formatted.Length > payloadLogMaxChars)
            {
                formatted = formatted.Substring(0, payloadLogMaxChars) + "...(truncated)";
            }

            return formatted;
        }

        private static void MaskSensitiveFields(JToken token)
        {
            if (token == null)
            {
                return;
            }

            if (token.Type == JTokenType.Object)
            {
                var obj = (JObject)token;
                foreach (JProperty property in obj.Properties())
                {
                    if (string.Equals(property.Name, "password", StringComparison.OrdinalIgnoreCase))
                    {
                        property.Value = "***";
                        continue;
                    }

                    MaskSensitiveFields(property.Value);
                }

                return;
            }

            if (token.Type == JTokenType.Array)
            {
                foreach (JToken item in (JArray)token)
                {
                    MaskSensitiveFields(item);
                }
            }
        }

        private static string ResolveRequestId(ServerCallContext context)
        {
            if (context?.RequestHeaders != null)
            {
                foreach (string headerName in RequestIdHeaderCandidates)
                {
                    Metadata.Entry entry = context.RequestHeaders.FirstOrDefault(
                        h => string.Equals(h.Key, headerName, StringComparison.OrdinalIgnoreCase));
                    if (entry != null && !string.IsNullOrWhiteSpace(entry.Value))
                    {
                        return entry.Value;
                    }
                }
            }

            return Guid.NewGuid().ToString("N");
        }

        private static string BuildStandardPayload(string requestId,
            bool success,
            string code,
            string message,
            IEnumerable<string> errors,
            IEnumerable<GrpcErrorDetail> details,
            object businessPayload)
        {
            JObject payload = businessPayload == null ? new JObject() : JObject.FromObject(businessPayload);
            payload["requestId"] = requestId;
            payload["success"] = success;
            payload["code"] = code ?? GrpcErrorCodes.InternalError;
            payload["message"] = message ?? string.Empty;
            payload["errors"] = errors == null ? new JArray() : JArray.FromObject(errors);
            payload["errorDetails"] = details == null ? new JArray() : JArray.FromObject(details);
            return payload.ToString(Formatting.None);
        }

        private static RpcException BuildRpcException(StatusCode statusCode,
            string requestId,
            string code,
            string message,
            IEnumerable<string> errors = null,
            IEnumerable<GrpcErrorDetail> details = null)
        {
            List<string> errorList = errors == null ? new List<string>() : errors.ToList();
            if (errorList.Count == 0 && !string.IsNullOrWhiteSpace(message))
            {
                errorList.Add(message);
            }

            string payload = BuildStandardPayload(requestId, false, code, message, errorList, details, null);
            return new RpcException(new Status(statusCode, payload));
        }
    }
}