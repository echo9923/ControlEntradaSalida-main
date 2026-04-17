using System;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ControlEntradaSalida.Application.Abstractions;
using ControlEntradaSalida.Application.Devices;
using ControlEntradaSalida.Compatibility.Grpc.Parsing;
using ControlEntradaSalida.Domain.Common;
using Grpc.Core;
using Newtonsoft.Json;

namespace ControlEntradaSalida.Compatibility.Grpc
{
    public sealed class DeviceManagementCompatibilityService
    {
        private const string ServiceName = "device.AccessControlService";

        private static readonly Marshaller<string> StringMarshaller = Marshallers.Create(
            Encoding.UTF8.GetBytes,
            bytes => Encoding.UTF8.GetString(bytes ?? Array.Empty<byte>()));

        private static readonly Method<string, string> GetDeviceStatusMethod = new Method<string, string>(MethodType.Unary, ServiceName, "GetDeviceStatus", StringMarshaller, StringMarshaller);
        private static readonly Method<string, string> AddDeviceMethod = new Method<string, string>(MethodType.Unary, ServiceName, "AddDevice", StringMarshaller, StringMarshaller);
        private static readonly Method<string, string> DeleteDeviceMethod = new Method<string, string>(MethodType.Unary, ServiceName, "DeleteDevice", StringMarshaller, StringMarshaller);
        private static readonly Method<string, string> DisconnectDeviceMethod = new Method<string, string>(MethodType.Unary, ServiceName, "DisconnectDevice", StringMarshaller, StringMarshaller);
        private static readonly Method<string, string> ReconnectDeviceMethod = new Method<string, string>(MethodType.Unary, ServiceName, "ReconnectDevice", StringMarshaller, StringMarshaller);

        private static readonly string[] RequestIdHeaders = { "x-request-id", "x-correlation-id", "x-trace-id" };

        private readonly GetDeviceStatusQueryHandler getDeviceStatus;
        private readonly AddDeviceCommandHandler addDevice;
        private readonly DeleteDeviceCommandHandler deleteDevice;
        private readonly DisconnectDeviceCommandHandler disconnectDevice;
        private readonly ReconnectDeviceCommandHandler reconnectDevice;
        private readonly ILoggerFacade logger;
        private readonly bool logPayloads;
        private readonly int payloadLogMaxChars;
        private readonly string apiKey;

        public DeviceManagementCompatibilityService(
            GetDeviceStatusQueryHandler getDeviceStatus,
            AddDeviceCommandHandler addDevice,
            DeleteDeviceCommandHandler deleteDevice,
            DisconnectDeviceCommandHandler disconnectDevice,
            ReconnectDeviceCommandHandler reconnectDevice,
            ILoggerFacade logger,
            bool logPayloads,
            int payloadLogMaxChars,
            string apiKey)
        {
            this.getDeviceStatus = getDeviceStatus;
            this.addDevice = addDevice;
            this.deleteDevice = deleteDevice;
            this.disconnectDevice = disconnectDevice;
            this.reconnectDevice = reconnectDevice;
            this.logger = logger;
            this.logPayloads = logPayloads;
            this.payloadLogMaxChars = payloadLogMaxChars;
            this.apiKey = apiKey;
        }

        public ServerServiceDefinition BuildServiceDefinition()
        {
            return ServerServiceDefinition.CreateBuilder()
                .AddMethod(GetDeviceStatusMethod, HandleGetDeviceStatusAsync)
                .AddMethod(AddDeviceMethod, HandleAddDeviceAsync)
                .AddMethod(DeleteDeviceMethod, HandleDeleteDeviceAsync)
                .AddMethod(DisconnectDeviceMethod, HandleDisconnectDeviceAsync)
                .AddMethod(ReconnectDeviceMethod, HandleReconnectDeviceAsync)
                .Build();
        }

        public Task<string> ExecuteGetDeviceStatusAsync(string request, Metadata headers = null)
        {
            return ExecuteUnaryAsync(
                request,
                headers,
                payload => getDeviceStatus.HandleAsync(BatchRequestParser.ParseDeviceStatusQuery(payload), CreateContext(headers), CancellationToken.None));
        }

        public Task<string> ExecuteAddDeviceAsync(string request, Metadata headers = null)
        {
            return ExecuteUnaryAsync(
                request,
                headers,
                payload => addDevice.HandleAsync(BatchRequestParser.ParseAddDevice(payload), CreateContext(headers), CancellationToken.None));
        }

        public Task<string> ExecuteDeleteDeviceAsync(string request, Metadata headers = null)
        {
            return ExecuteUnaryAsync(
                request,
                headers,
                payload => deleteDevice.HandleAsync(BatchRequestParser.ParseDeleteDevice(payload), CreateContext(headers), CancellationToken.None));
        }

        public Task<string> ExecuteDisconnectDeviceAsync(string request, Metadata headers = null)
        {
            return ExecuteUnaryAsync(
                request,
                headers,
                payload => disconnectDevice.HandleAsync(BatchRequestParser.ParseDisconnectDevice(payload), CreateContext(headers), CancellationToken.None));
        }

        public Task<string> ExecuteReconnectDeviceAsync(string request, Metadata headers = null)
        {
            return ExecuteUnaryAsync(
                request,
                headers,
                payload => reconnectDevice.HandleAsync(BatchRequestParser.ParseReconnectDevice(payload), CreateContext(headers), CancellationToken.None));
        }

        private Task<string> HandleGetDeviceStatusAsync(string request, ServerCallContext context) => ExecuteGetDeviceStatusAsync(request, context?.RequestHeaders);

        private Task<string> HandleAddDeviceAsync(string request, ServerCallContext context) => ExecuteAddDeviceAsync(request, context?.RequestHeaders);

        private Task<string> HandleDeleteDeviceAsync(string request, ServerCallContext context) => ExecuteDeleteDeviceAsync(request, context?.RequestHeaders);

        private Task<string> HandleDisconnectDeviceAsync(string request, ServerCallContext context) => ExecuteDisconnectDeviceAsync(request, context?.RequestHeaders);

        private Task<string> HandleReconnectDeviceAsync(string request, ServerCallContext context) => ExecuteReconnectDeviceAsync(request, context?.RequestHeaders);

        private async Task<string> ExecuteUnaryAsync(string request, Metadata headers, Func<string, Task<OperationResult>> operation)
        {
            EnsureAuthorized(headers);
            LogPayload(request);
            string requestId = ResolveRequestId(headers);
            try
            {
                OperationResult result = await operation(request).ConfigureAwait(false);
                return GrpcEnvelopeFactory.Create(requestId, result);
            }
            catch (Exception ex) when (ex is JsonException || ex is ArgumentException)
            {
                throw CreateRpcException(requestId, "invalid_argument", ex.Message);
            }
        }

        private void EnsureAuthorized(Metadata headers)
        {
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                return;
            }

            string provided = headers?.FirstOrDefault(item => string.Equals(item.Key, "x-api-key", StringComparison.OrdinalIgnoreCase))?.Value;
            if (!string.Equals(provided, apiKey, StringComparison.Ordinal))
            {
                throw CreateRpcException(ResolveRequestId(headers), "unauthenticated", "缺少或错误的 x-api-key。");
            }
        }

        private void LogPayload(string payload)
        {
            if (!logPayloads || logger == null)
            {
                return;
            }

            string sanitized = PayloadMasker.Mask(payload ?? string.Empty);
            if (payloadLogMaxChars > 0 && sanitized.Length > payloadLogMaxChars)
            {
                sanitized = sanitized.Substring(0, payloadLogMaxChars) + "...";
            }

            logger.Debug("[device]" + sanitized);
        }

        private RequestContext CreateContext(Metadata headers)
        {
            return new RequestContext(ResolveRequestId(headers), headers: headers?.ToDictionary(h => h.Key, h => h.Value));
        }

        private static string ResolveRequestId(Metadata headers)
        {
            if (headers != null)
            {
                foreach (string headerName in RequestIdHeaders)
                {
                    Metadata.Entry entry = headers.FirstOrDefault(item => string.Equals(item.Key, headerName, StringComparison.OrdinalIgnoreCase));
                    if (entry != null && !string.IsNullOrWhiteSpace(entry.Value))
                    {
                        return entry.Value;
                    }
                }
            }

            return Guid.NewGuid().ToString("N");
        }

        private static RpcException CreateRpcException(string requestId, string code, string message)
        {
            OperationResult result = OperationResult.Failure(code, message, new[] { message });
            return new RpcException(new Status(GrpcErrorMapper.MapToStatusCode(code), GrpcEnvelopeFactory.Create(requestId, result)));
        }
    }
}
