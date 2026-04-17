using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ControlEntradaSalida.Application.Abstractions;
using ControlEntradaSalida.Application.Faces;
using ControlEntradaSalida.Application.People;
using ControlEntradaSalida.Application.Permissions;
using ControlEntradaSalida.Compatibility.Grpc.Parsing;
using ControlEntradaSalida.Domain.Common;
using Grpc.Core;
using Newtonsoft.Json;

namespace ControlEntradaSalida.Compatibility.Grpc
{
    public sealed class PermissionSyncCompatibilityService
    {
        private const string ServiceName = "permission.PermissionSyncService";

        private static readonly Marshaller<string> StringMarshaller = Marshallers.Create(
            Encoding.UTF8.GetBytes,
            bytes => Encoding.UTF8.GetString(bytes ?? Array.Empty<byte>()));

        private static readonly Method<string, string> SyncPermissionsMethod = new Method<string, string>(MethodType.Unary, ServiceName, "SyncPermissions", StringMarshaller, StringMarshaller);
        private static readonly Method<string, string> SyncPersonsMethod = new Method<string, string>(MethodType.Unary, ServiceName, "SyncPersons", StringMarshaller, StringMarshaller);
        private static readonly Method<string, string> DeleteFacesMethod = new Method<string, string>(MethodType.Unary, ServiceName, "DeleteFaces", StringMarshaller, StringMarshaller);
        private static readonly Method<string, string> DeletePersonsMethod = new Method<string, string>(MethodType.Unary, ServiceName, "DeletePersons", StringMarshaller, StringMarshaller);
        private static readonly Method<string, string> GetFacesMethod = new Method<string, string>(MethodType.Unary, ServiceName, "GetFaces", StringMarshaller, StringMarshaller);
        private static readonly Method<string, string> GetEnrollmentStatusMethod = new Method<string, string>(MethodType.Unary, ServiceName, "GetEnrollmentStatus", StringMarshaller, StringMarshaller);
        private static readonly Method<string, string> CaptureFaceStreamMethod = new Method<string, string>(MethodType.ServerStreaming, ServiceName, "CaptureFaceStream", StringMarshaller, StringMarshaller);

        private static readonly string[] RequestIdHeaders = { "x-request-id", "x-correlation-id", "x-trace-id" };

        private readonly SyncPermissionsCommandHandler syncPermissions;
        private readonly SyncPersonsCommandHandler syncPersons;
        private readonly DeleteFacesCommandHandler deleteFaces;
        private readonly DeletePersonsCommandHandler deletePersons;
        private readonly GetFacesQueryHandler getFaces;
        private readonly GetEnrollmentTaskStatusQueryHandler getEnrollmentStatus;
        private readonly CaptureEnrollmentFaceCommandHandler captureEnrollmentFace;
        private readonly ILoggerFacade logger;
        private readonly bool logPayloads;
        private readonly int payloadLogMaxChars;

        public PermissionSyncCompatibilityService(
            SyncPermissionsCommandHandler syncPermissions,
            SyncPersonsCommandHandler syncPersons,
            DeleteFacesCommandHandler deleteFaces,
            DeletePersonsCommandHandler deletePersons,
            GetFacesQueryHandler getFaces,
            GetEnrollmentTaskStatusQueryHandler getEnrollmentStatus,
            CaptureEnrollmentFaceCommandHandler captureEnrollmentFace,
            ILoggerFacade logger,
            bool logPayloads,
            int payloadLogMaxChars)
        {
            this.syncPermissions = syncPermissions;
            this.syncPersons = syncPersons;
            this.deleteFaces = deleteFaces;
            this.deletePersons = deletePersons;
            this.getFaces = getFaces;
            this.getEnrollmentStatus = getEnrollmentStatus;
            this.captureEnrollmentFace = captureEnrollmentFace;
            this.logger = logger;
            this.logPayloads = logPayloads;
            this.payloadLogMaxChars = payloadLogMaxChars;
        }

        public ServerServiceDefinition BuildServiceDefinition()
        {
            return ServerServiceDefinition.CreateBuilder()
                .AddMethod(SyncPermissionsMethod, HandleSyncPermissionsAsync)
                .AddMethod(SyncPersonsMethod, HandleSyncPersonsAsync)
                .AddMethod(DeleteFacesMethod, HandleDeleteFacesAsync)
                .AddMethod(DeletePersonsMethod, HandleDeletePersonsAsync)
                .AddMethod(GetFacesMethod, HandleGetFacesAsync)
                .AddMethod(GetEnrollmentStatusMethod, HandleGetEnrollmentStatusAsync)
                .AddMethod(CaptureFaceStreamMethod, HandleCaptureFaceStreamAsync)
                .Build();
        }

        public Task<string> ExecuteSyncPermissionsAsync(string request, Metadata headers = null)
        {
            return ExecuteUnaryAsync(
                request,
                headers,
                payload => syncPermissions.HandleAsync(SyncPermissionsRequestParser.Parse(payload), CreateContext(headers), CancellationToken.None));
        }

        public Task<string> ExecuteSyncPersonsAsync(string request, Metadata headers = null)
        {
            return ExecuteUnaryAsync(
                request,
                headers,
                payload => syncPersons.HandleAsync(SyncPersonsRequestParser.Parse(payload), CreateContext(headers), CancellationToken.None));
        }

        public Task<string> ExecuteDeleteFacesAsync(string request, Metadata headers = null)
        {
            return ExecuteUnaryAsync(
                request,
                headers,
                payload => deleteFaces.HandleAsync(BatchRequestParser.ParseEmployeeIds(payload), CreateContext(headers), CancellationToken.None));
        }

        public Task<string> ExecuteDeletePersonsAsync(string request, Metadata headers = null)
        {
            return ExecuteUnaryAsync(
                request,
                headers,
                payload => deletePersons.HandleAsync(BatchRequestParser.ParseEmployeeIds(payload), CreateContext(headers), CancellationToken.None));
        }

        public Task<string> ExecuteGetFacesAsync(string request, Metadata headers = null)
        {
            return ExecuteUnaryAsync(
                request,
                headers,
                payload => getFaces.HandleAsync(BatchRequestParser.ParseEmployeeIds(payload), CreateContext(headers), CancellationToken.None));
        }

        public Task<string> ExecuteGetEnrollmentStatusAsync(string request, Metadata headers = null)
        {
            return ExecuteUnaryAsync(
                request,
                headers,
                payload => getEnrollmentStatus.HandleAsync(BatchRequestParser.ParseEnrollmentStatus(payload), CreateContext(headers), CancellationToken.None));
        }

        public async Task<IReadOnlyList<string>> ExecuteCaptureFaceStreamAsync(string request, Metadata headers = null)
        {
            LogPayload("CaptureFaceStream", request);
            try
            {
                IReadOnlyList<OperationResult> frames = await captureEnrollmentFace.HandleAsync(
                    BatchRequestParser.ParseCaptureFace(request),
                    CreateContext(headers),
                    CancellationToken.None).ConfigureAwait(false);
                string requestId = ResolveRequestId(headers);
                return frames.Select(frame => GrpcEnvelopeFactory.Create(requestId, frame)).ToArray();
            }
            catch (Exception ex) when (ex is JsonException || ex is ArgumentException)
            {
                throw CreateRpcException(ResolveRequestId(headers), "invalid_argument", ex.Message);
            }
        }

        private Task<string> HandleSyncPermissionsAsync(string request, ServerCallContext context) => ExecuteSyncPermissionsAsync(request, context?.RequestHeaders);

        private Task<string> HandleSyncPersonsAsync(string request, ServerCallContext context) => ExecuteSyncPersonsAsync(request, context?.RequestHeaders);

        private Task<string> HandleDeleteFacesAsync(string request, ServerCallContext context) => ExecuteDeleteFacesAsync(request, context?.RequestHeaders);

        private Task<string> HandleDeletePersonsAsync(string request, ServerCallContext context) => ExecuteDeletePersonsAsync(request, context?.RequestHeaders);

        private Task<string> HandleGetFacesAsync(string request, ServerCallContext context) => ExecuteGetFacesAsync(request, context?.RequestHeaders);

        private Task<string> HandleGetEnrollmentStatusAsync(string request, ServerCallContext context) => ExecuteGetEnrollmentStatusAsync(request, context?.RequestHeaders);

        private async Task HandleCaptureFaceStreamAsync(string request, IServerStreamWriter<string> responseStream, ServerCallContext context)
        {
            foreach (string frame in await ExecuteCaptureFaceStreamAsync(request, context?.RequestHeaders).ConfigureAwait(false))
            {
                await responseStream.WriteAsync(frame).ConfigureAwait(false);
            }
        }

        private async Task<string> ExecuteUnaryAsync(string request, Metadata headers, Func<string, Task<OperationResult>> operation)
        {
            LogPayload("permission", request);
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

        private void LogPayload(string operation, string payload)
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

            logger.Debug($"[{operation}] {sanitized}");
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
