using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ControlEntradaSalida.Application.Abstractions;
using ControlEntradaSalida.Application.Models;
using ControlEntradaSalida.Domain.Common;

namespace ControlEntradaSalida.Infrastructure.Hikvision
{
    public sealed class LegacyDeviceRegistryService : IDeviceRegistryService
    {
        private readonly global::ControlEntradaSalida.DeviceConnectionManager deviceManager;

        public LegacyDeviceRegistryService(global::ControlEntradaSalida.DeviceConnectionManager deviceManager)
        {
            this.deviceManager = deviceManager;
        }

        public Task<OperationResult> GetDeviceStatusAsync(DeviceStatusQuery query, RequestContext requestContext, CancellationToken cancellationToken)
        {
            List<global::ControlEntradaSalida.DeviceConnectionInfo> devices;
            var details = new List<OperationErrorDetail>();

            if (query.DeviceId.HasValue)
            {
                global::ControlEntradaSalida.DeviceConnectionInfo device = deviceManager.GetDeviceById(query.DeviceId.Value);
                if (device == null)
                {
                    return Task.FromResult(OperationResult.Failure(
                        global::ControlEntradaSalida.GrpcErrorCodes.NotFound,
                        string.Format(CultureInfo.InvariantCulture, "设备 {0} 不存在。", query.DeviceId.Value),
                        new[] { string.Format(CultureInfo.InvariantCulture, "设备 {0} 不存在。", query.DeviceId.Value) }));
                }

                devices = new List<global::ControlEntradaSalida.DeviceConnectionInfo> { device };
            }
            else if (query.DeviceIds != null && query.DeviceIds.Count > 0)
            {
                devices = new List<global::ControlEntradaSalida.DeviceConnectionInfo>();
                foreach (int id in query.DeviceIds)
                {
                    global::ControlEntradaSalida.DeviceConnectionInfo device = deviceManager.GetDeviceById(id);
                    if (device == null)
                    {
                        details.Add(new OperationErrorDetail
                        {
                            DeviceId = id,
                            Code = global::ControlEntradaSalida.GrpcErrorCodes.NotFound,
                            Message = "设备不存在。"
                        });
                        continue;
                    }

                    devices.Add(device);
                }

                if (devices.Count == 0)
                {
                    return Task.FromResult(OperationResult.Failure(
                        global::ControlEntradaSalida.GrpcErrorCodes.NotFound,
                        "请求的设备均不存在。",
                        new[] { "请求的设备均不存在。" },
                        details));
                }
            }
            else if (!string.IsNullOrWhiteSpace(query.IpAddress))
            {
                if (!deviceManager.TryGetDeviceByIp(query.IpAddress.Trim(), out global::ControlEntradaSalida.DeviceConnectionInfo device) || device == null)
                {
                    return Task.FromResult(OperationResult.Failure(
                        global::ControlEntradaSalida.GrpcErrorCodes.NotFound,
                        string.Format(CultureInfo.InvariantCulture, "设备 {0} 不存在。", query.IpAddress),
                        new[] { string.Format(CultureInfo.InvariantCulture, "设备 {0} 不存在。", query.IpAddress) }));
                }

                devices = new List<global::ControlEntradaSalida.DeviceConnectionInfo> { device };
            }
            else
            {
                devices = deviceManager.GetAllDevices();
            }

            if (!query.IncludeDisabled)
            {
                devices = devices.Where(device => device != null && device.IsEnabled).ToList();
            }

            if (query.Refresh)
            {
                foreach (global::ControlEntradaSalida.DeviceConnectionInfo device in devices)
                {
                    try
                    {
                        deviceManager.CheckDeviceStatus(device);
                    }
                    catch (Exception ex)
                    {
                        details.Add(new OperationErrorDetail
                        {
                            DeviceId = device.Id,
                            DeviceName = device.Name,
                            DeviceIp = device.IpAddress,
                            Code = global::ControlEntradaSalida.GrpcErrorCodes.InternalError,
                            Message = ex.Message
                        });
                    }
                }
            }

            bool success = details.Count == 0;
            return Task.FromResult(success
                ? OperationResult.Success(
                    global::ControlEntradaSalida.GrpcErrorCodes.Ok,
                    "查询成功。",
                    new
                    {
                        devices = devices.Where(device => device != null).Select(BuildDeviceStatusPayload).ToArray()
                    })
                : OperationResult.Failure(
                    global::ControlEntradaSalida.GrpcErrorCodes.PartialSuccess,
                    "查询完成，但存在部分异常或缺失。",
                    details.Select(detail => detail.Message).Distinct().ToArray(),
                    details,
                    new
                    {
                        devices = devices.Where(device => device != null).Select(BuildDeviceStatusPayload).ToArray()
                    }));
        }

        public Task<OperationResult> AddDeviceAsync(AddDeviceCommand command, RequestContext requestContext, CancellationToken cancellationToken)
        {
            var device = new global::ControlEntradaSalida.DeviceConnectionInfo
            {
                Id = command.DeviceId,
                Name = command.DeviceName?.Trim(),
                IpAddress = command.IpAddress?.Trim(),
                Port = string.IsNullOrWhiteSpace(command.Port) ? "8000" : command.Port.Trim(),
                Username = string.IsNullOrWhiteSpace(command.Username) ? "admin" : command.Username.Trim(),
                Password = command.Password,
                IsEnabled = command.Enabled,
                LastUsed = DateTime.MinValue
            };

            if (!deviceManager.TryAddDevice(device, command.Description, out string errorMessage))
            {
                return Task.FromResult(CreateFailureFromManagerError(errorMessage));
            }

            bool connected = false;
            string connectionMessage = string.Empty;
            if (command.ConnectNow)
            {
                try
                {
                    connected = deviceManager.ConnectToDevice(device);
                    connectionMessage = connected ? "连接成功。" : (device.StatusMessage ?? "连接失败。");
                }
                catch (Exception ex)
                {
                    connectionMessage = ex.Message;
                }
            }

            bool success = !command.ConnectNow || connected;
            return Task.FromResult(success
                ? OperationResult.Success(
                    global::ControlEntradaSalida.GrpcErrorCodes.Ok,
                    command.ConnectNow ? "新增并连接成功。" : "新增成功。",
                    new
                    {
                        device = BuildDeviceStatusPayload(device),
                        connectNow = command.ConnectNow,
                        connected,
                        connectionMessage
                    })
                : OperationResult.Failure(
                    global::ControlEntradaSalida.GrpcErrorCodes.PartialSuccess,
                    "新增成功，但连接失败。",
                    new[] { connectionMessage },
                    null,
                    new
                    {
                        device = BuildDeviceStatusPayload(device),
                        connectNow = command.ConnectNow,
                        connected,
                        connectionMessage
                    }));
        }

        public Task<OperationResult> DeleteDeviceAsync(DeleteDeviceCommand command, RequestContext requestContext, CancellationToken cancellationToken)
        {
            if (!deviceManager.TryDeleteDevice(command.DeviceId, command.DisconnectFirst, out string errorMessage))
            {
                return Task.FromResult(CreateFailureFromManagerError(errorMessage));
            }

            return Task.FromResult(OperationResult.Success(
                global::ControlEntradaSalida.GrpcErrorCodes.Ok,
                "删除成功。",
                new
                {
                    deleted = true,
                    deviceId = command.DeviceId
                }));
        }

        public Task<OperationResult> DisconnectDeviceAsync(DisconnectDeviceCommand command, RequestContext requestContext, CancellationToken cancellationToken)
        {
            global::ControlEntradaSalida.DeviceConnectionInfo device = deviceManager.GetDeviceById(command.DeviceId);
            if (device == null)
            {
                return Task.FromResult(OperationResult.Failure(
                    global::ControlEntradaSalida.GrpcErrorCodes.NotFound,
                    string.Format(CultureInfo.InvariantCulture, "设备 {0} 不存在。", command.DeviceId),
                    new[] { string.Format(CultureInfo.InvariantCulture, "设备 {0} 不存在。", command.DeviceId) }));
            }

            deviceManager.DisconnectDevice(device);
            var snapshot = BuildDeviceStatusPayload(device);
            bool isConnected = (bool)(snapshot.GetType().GetProperty("isConnected")?.GetValue(snapshot) ?? false);
            string status = (string)snapshot.GetType().GetProperty("status")?.GetValue(snapshot);
            string message = !isConnected ? "已断开连接。" : "断开请求已执行，但设备仍处于连接状态。";

            return Task.FromResult(!isConnected
                ? OperationResult.Success(
                    global::ControlEntradaSalida.GrpcErrorCodes.Ok,
                    message,
                    new
                    {
                        deviceId = command.DeviceId,
                        isConnected,
                        status,
                        message
                    })
                : OperationResult.Failure(
                    global::ControlEntradaSalida.GrpcErrorCodes.Failed,
                    message,
                    new[] { message },
                    null,
                    new
                    {
                        deviceId = command.DeviceId,
                        isConnected,
                        status,
                        message
                    }));
        }

        public Task<OperationResult> ReconnectDeviceAsync(ReconnectDeviceCommand command, RequestContext requestContext, CancellationToken cancellationToken)
        {
            if (!deviceManager.TryReconnectDevice(command.DeviceId, command.Force, out bool connected, out string message))
            {
                return Task.FromResult(OperationResult.Failure(
                    global::ControlEntradaSalida.GrpcErrorCodes.NotFound,
                    message ?? string.Format(CultureInfo.InvariantCulture, "设备 {0} 不存在。", command.DeviceId),
                    new[] { message ?? string.Format(CultureInfo.InvariantCulture, "设备 {0} 不存在。", command.DeviceId) }));
            }

            string payloadMessage = string.IsNullOrWhiteSpace(message)
                ? (connected ? "连接成功。" : "连接失败。")
                : message;

            return Task.FromResult(connected
                ? OperationResult.Success(
                    global::ControlEntradaSalida.GrpcErrorCodes.Ok,
                    payloadMessage,
                    new
                    {
                        deviceId = command.DeviceId,
                        connected,
                        message = payloadMessage
                    })
                : OperationResult.Failure(
                    global::ControlEntradaSalida.GrpcErrorCodes.Failed,
                    payloadMessage,
                    new[] { payloadMessage },
                    null,
                    new
                    {
                        deviceId = command.DeviceId,
                        connected,
                        message = payloadMessage
                    }));
        }

        private static object BuildDeviceStatusPayload(global::ControlEntradaSalida.DeviceConnectionInfo device)
        {
            lock (device.LockObject)
            {
                return new
                {
                    deviceId = device.Id,
                    deviceName = device.Name,
                    ipAddress = device.IpAddress,
                    port = device.Port,
                    enabled = device.IsEnabled,
                    isConnected = device.IsConnected,
                    status = device.Status.ToString(),
                    statusMessage = device.StatusMessage,
                    lastChecked = device.LastChecked == DateTime.MinValue ? (DateTime?)null : device.LastChecked,
                    lastUsed = device.LastUsed == DateTime.MinValue ? (DateTime?)null : device.LastUsed,
                    lastErrorCode = device.LastErrorCode == 0 ? (uint?)null : device.LastErrorCode,
                    lastErrorMessage = device.LastErrorMessage
                };
            }
        }

        private static OperationResult CreateFailureFromManagerError(string errorMessage)
        {
            string code = global::ControlEntradaSalida.GrpcErrorCodes.InternalError;
            string message = string.IsNullOrWhiteSpace(errorMessage) ? "操作失败。" : errorMessage;

            if (!string.IsNullOrWhiteSpace(errorMessage))
            {
                int index = errorMessage.IndexOf(':');
                if (index > 0)
                {
                    code = errorMessage.Substring(0, index).Trim();
                    message = errorMessage.Substring(index + 1).Trim();
                }
            }

            return OperationResult.Failure(code, message, new[] { message });
        }
    }
}
