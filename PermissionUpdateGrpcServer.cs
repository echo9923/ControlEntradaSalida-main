using Grpc.Core;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Collections.Concurrent;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ControlEntradaSalida
{
    /// <summary>
    /// 负责承载权限更新的GRPC监听服务。
    /// 接收JSON负载并转发到权限刷新管理器。
    /// </summary>
    public sealed class PermissionUpdateGrpcServer : IDisposable
    {
        private const int DefaultPort = 5001;
        private const int MaxBatchSize = 500;
        private const string ServiceName = "permission.PermissionSyncService";
        private const string MethodName = "SyncPermissions";
        private const string PersonMethodName = "SyncPersons";
        private const string DeleteFaceMethodName = "DeleteFaces";
        private const string DeletePersonMethodName = "DeletePersons";
        private const string GetFaceMethodName = "GetFaces";
        private const string CaptureFaceMethodName = "CaptureFaceStream";
        private const string StatusMethodName = "GetEnrollmentStatus";

        private const string GrpcLogPrefix = "[权限GRPC]";

        private static readonly string[] RequestIdHeaderCandidates = new[]
        {
            "x-request-id",
            "x-correlation-id",
            "x-trace-id"
        };

        private static readonly Marshaller<string> StringMarshaller = Marshallers.Create(
            Encoding.UTF8.GetBytes,
            bytes => Encoding.UTF8.GetString(bytes ?? Array.Empty<byte>()));

        private static readonly Method<string, string> UpdatePermissionMethod = new Method<string, string>(
            MethodType.Unary,
            ServiceName,
            MethodName,
            StringMarshaller,
            StringMarshaller);

        private static readonly Method<string, string> SyncPersonsMethod = new Method<string, string>(
            MethodType.Unary,
            ServiceName,
            PersonMethodName,
            StringMarshaller,
            StringMarshaller);

        private static readonly Method<string, string> DeleteFacesMethod = new Method<string, string>(
            MethodType.Unary,
            ServiceName,
            DeleteFaceMethodName,
            StringMarshaller,
            StringMarshaller);

        private static readonly Method<string, string> DeletePersonsMethod = new Method<string, string>(
            MethodType.Unary,
            ServiceName,
            DeletePersonMethodName,
            StringMarshaller,
            StringMarshaller);

        private static readonly Method<string, string> GetFacesMethod = new Method<string, string>(
            MethodType.Unary,
            ServiceName,
            GetFaceMethodName,
            StringMarshaller,
            StringMarshaller);

        private static readonly Method<string, string> GetStatusMethod = new Method<string, string>(
            MethodType.Unary,
            ServiceName,
            StatusMethodName,
            StringMarshaller,
            StringMarshaller);

        private static readonly Method<string, string> CaptureFaceStreamMethod = new Method<string, string>(
            MethodType.ServerStreaming,
            ServiceName,
            CaptureFaceMethodName,
            StringMarshaller,
            StringMarshaller);

        private readonly PermissionRefreshManager refreshManager;
        private readonly object lifecycleLock = new object();
        private readonly bool logPayloads;
        private readonly int payloadLogMaxChars;
        private readonly AccessControlGrpcService accessControlGrpcService;

        private CancellationTokenSource shutdownTokenSource;
        private Server grpcServer;
        private Thread listenerThread;
        private int listenPort = DefaultPort;

        public PermissionUpdateGrpcServer(PermissionRefreshManager refreshManager, bool logPayloads, int payloadLogMaxChars)
            : this(refreshManager, logPayloads, payloadLogMaxChars, null)
        {
        }

        public PermissionUpdateGrpcServer(PermissionRefreshManager refreshManager,
            bool logPayloads,
            int payloadLogMaxChars,
            AccessControlGrpcService accessControlGrpcService)
        {
            this.refreshManager = refreshManager ?? throw new ArgumentNullException(nameof(refreshManager));
            this.logPayloads = logPayloads;
            this.payloadLogMaxChars = payloadLogMaxChars > 0 ? payloadLogMaxChars : 0;
            this.accessControlGrpcService = accessControlGrpcService;
        }

        public void Start(int port = DefaultPort)
        {
            lock (lifecycleLock)
            {
                // 既要防止重复启动，也要覆盖“线程已启动但 grpcServer 尚未赋值”的启动中状态。
                if (grpcServer != null || listenerThread != null || shutdownTokenSource != null)
                {
                    return;
                }

                listenPort = port;
                shutdownTokenSource = new CancellationTokenSource();

                listenerThread = new Thread(ServerThreadEntry)
                {
                    IsBackground = true,
                    Name = "PermissionUpdateGrpcListener"
                };

                listenerThread.Start();
            }
        }

        public Task StopAsync()
        {
            Server serverToShutdown = null;
            CancellationTokenSource tokenSource = null;
            Thread threadToJoin = null;

            lock (lifecycleLock)
            {
                tokenSource = shutdownTokenSource;
                serverToShutdown = grpcServer;
                threadToJoin = listenerThread;

                // 覆盖“grpcServer 尚未创建”的启动中场景：只要线程/Token 仍存在，就需要执行 Stop。
                if (tokenSource == null && serverToShutdown == null && threadToJoin == null)
                {
                    return Task.CompletedTask;
                }

                grpcServer = null;
                listenerThread = null;
                shutdownTokenSource = null;
            }

            tokenSource?.Cancel();

            async Task ShutdownCoreAsync()
            {
                try
                {
                    if (serverToShutdown != null)
                    {
                        await serverToShutdown.ShutdownAsync().ConfigureAwait(false);
                    }
                }
                catch (InvalidOperationException)
                {
                    // 忽略无效状态异常
                }
                catch (Exception ex)
                {
                    ServiceLogger.Error("停止权限GRPC服务时发生异常。", ex);
                }

                if (threadToJoin != null && threadToJoin.IsAlive)
                {
                    threadToJoin.Join(TimeSpan.FromSeconds(3));
                }

                tokenSource?.Dispose();
            }

            return ShutdownCoreAsync();
        }

        private void ServerThreadEntry()
        {
            CancellationTokenSource tokenSource;
            int port;

            lock (lifecycleLock)
            {
                tokenSource = shutdownTokenSource;
                port = listenPort;
            }

            if (tokenSource == null)
            {
                return;
            }

            ServerServiceDefinition permissionServiceDefinition = ServerServiceDefinition.CreateBuilder()
                .AddMethod(UpdatePermissionMethod, HandlePermissionUpdateAsync)
                .AddMethod(SyncPersonsMethod, HandlePersonSyncAsync)
                .AddMethod(DeleteFacesMethod, HandleFaceDeleteAsync)
                .AddMethod(DeletePersonsMethod, HandlePersonDeleteAsync)
                .AddMethod(GetFacesMethod, HandleFaceGetAsync)
                .AddMethod(GetStatusMethod, HandleStatusGetAsync)
                .AddMethod(CaptureFaceStreamMethod, HandleCaptureStreamAsync)
                .Build();

            var localServer = new Server();
            localServer.Services.Add(permissionServiceDefinition);

            if (accessControlGrpcService != null)
            {
                localServer.Services.Add(accessControlGrpcService.BuildServiceDefinition());
            }

            localServer.Ports.Add(new ServerPort("0.0.0.0", port, ServerCredentials.Insecure));

            try
            {
                // 关键点：在生命周期锁内完成“发布 grpcServer + Start”，避免 StopAsync 在 Start 之前抢跑。
                lock (lifecycleLock)
                {
                    if (shutdownTokenSource != tokenSource || tokenSource.IsCancellationRequested)
                    {
                        return;
                    }

                    grpcServer = localServer;
                    grpcServer.Start();
                }

                ServiceLogger.Info(string.Format(CultureInfo.InvariantCulture,
                    "权限GRPC服务已启动，端口：{0}。", port));

                CancellationToken token = tokenSource.Token;
                token.WaitHandle.WaitOne();
            }
            catch (IOException ex)
            {
                ServiceLogger.Error(
                    string.Format(CultureInfo.InvariantCulture,
                        "权限GRPC服务启动失败，可能端口被占用：{0}", ex.Message), ex);
            }
            catch (Exception ex)
            {
                ServiceLogger.Error("权限GRPC服务运行异常。", ex);
            }
        }

        private Task<string> HandlePermissionUpdateAsync(string request, ServerCallContext context)
        {
            string peer = DescribePeer(context);
            string requestId = ResolveRequestId(context);
            int payloadLength = string.IsNullOrEmpty(request) ? 0 : Encoding.UTF8.GetByteCount(request);
            Stopwatch stopwatch = Stopwatch.StartNew();

            ServiceLogger.Info($"{GrpcLogPrefix} 请求 {requestId} 来自 {peer}，载荷长度 {payloadLength} 字节。");
            LogPayloadIfEnabled("入站", requestId, request, payloadLength);

            try
            {
                List<PermissionUpdateInfo> updates = ParseUpdates(request);
                string employeePreview = updates.Count == 0
                    ? "无"
                    : string.Join(",", updates.Take(5).Select(u => u.EmployeeId));
                ServiceLogger.Debug($"{GrpcLogPrefix} 请求 {requestId} 已解析 {updates.Count} 条权限指令，示例员工：{employeePreview}。");

                if (updates.Count == 0)
                {
                    ServiceLogger.Warn($"{GrpcLogPrefix} 请求 {requestId} 未解析出任何有效的权限更新。");
                    PermissionRefreshSummary summary = new PermissionRefreshSummary();
                    summary.Errors.Add("未解析到有效的权限更新数据。");
                    summary.UsersFailed = 0;
                    stopwatch.Stop();
                    LogGrpcSummary(peer, requestId, summary, stopwatch.Elapsed);
                    string responsePayload = BuildResponse(requestId, summary);
                    LogPayloadIfEnabled("出站", requestId, responsePayload, Encoding.UTF8.GetByteCount(responsePayload));
                    return Task.FromResult(responsePayload);
                }

                PermissionRefreshSummary result = refreshManager.RefreshPermissionsForEmployees(updates);
                stopwatch.Stop();
                LogGrpcSummary(peer, requestId, result, stopwatch.Elapsed);
                string successPayload = BuildResponse(requestId, result);
                LogPayloadIfEnabled("出站", requestId, successPayload, Encoding.UTF8.GetByteCount(successPayload));
                return Task.FromResult(successPayload);
            }
            catch (GrpcValidationException ex)
            {
                stopwatch.Stop();
                ServiceLogger.Warn($"{GrpcLogPrefix} 请求 {requestId} 参数非法：{ex.Message}");
                throw BuildRpcException(StatusCode.InvalidArgument, requestId, ex.ErrorCode ?? GrpcErrorCodes.InvalidArgument, ex.Message);
            }
            catch (JsonException ex)
            {
                stopwatch.Stop();
                string errorMessage = string.Format(CultureInfo.InvariantCulture, "JSON格式错误：{0}", ex.Message);
                ServiceLogger.Warn($"{GrpcLogPrefix} 请求 {requestId} {errorMessage}");
                throw BuildRpcException(StatusCode.InvalidArgument, requestId, GrpcErrorCodes.InvalidArgument, errorMessage);
            }
            catch (ArgumentException ex)
            {
                stopwatch.Stop();
                ServiceLogger.Warn($"{GrpcLogPrefix} 请求 {requestId} 参数非法：{ex.Message}");
                throw BuildRpcException(StatusCode.InvalidArgument, requestId, GrpcErrorCodes.InvalidArgument, ex.Message);
            }
            catch (RpcException)
            {
                stopwatch.Stop();
                throw;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                ServiceLogger.Error($"{GrpcLogPrefix} 请求 {requestId} 处理权限更新时发生异常。", ex);
                throw BuildRpcException(StatusCode.Internal, requestId, GrpcErrorCodes.InternalError, "处理权限更新时发生未知错误。");
            }
        }

        private Task<string> HandlePersonSyncAsync(string request, ServerCallContext context)
        {
            string peer = DescribePeer(context);
            string requestId = ResolveRequestId(context);
            int payloadLength = string.IsNullOrEmpty(request) ? 0 : Encoding.UTF8.GetByteCount(request);
            Stopwatch stopwatch = Stopwatch.StartNew();

            ServiceLogger.Info($"{GrpcLogPrefix} 请求 {requestId} 来自 {peer}，人员载荷长度 {payloadLength} 字节。");
            LogPayloadIfEnabled("入站", requestId, request, payloadLength);

            try
            {
                List<PersonSyncRequest> persons = ParsePersonSyncRequests(request);
                PersonSyncSummary summary = refreshManager.SyncPersonsToConnectedDevices(persons);
                stopwatch.Stop();

                LogPersonSummary(peer, requestId, summary, stopwatch.Elapsed);
                string responsePayload = BuildPersonSyncResponse(requestId, summary);
                LogPayloadIfEnabled("出站", requestId, responsePayload, Encoding.UTF8.GetByteCount(responsePayload));
                return Task.FromResult(responsePayload);
            }
            catch (GrpcValidationException ex)
            {
                stopwatch.Stop();
                ServiceLogger.Warn($"{GrpcLogPrefix} 请求 {requestId} 参数非法：{ex.Message}");
                throw BuildRpcException(StatusCode.InvalidArgument, requestId, ex.ErrorCode ?? GrpcErrorCodes.InvalidArgument, ex.Message);
            }
            catch (JsonException ex)
            {
                stopwatch.Stop();
                string errorMessage = string.Format(CultureInfo.InvariantCulture, "JSON格式错误：{0}", ex.Message);
                ServiceLogger.Warn($"{GrpcLogPrefix} 请求 {requestId} {errorMessage}");
                throw BuildRpcException(StatusCode.InvalidArgument, requestId, GrpcErrorCodes.InvalidArgument, errorMessage);
            }
            catch (ArgumentException ex)
            {
                stopwatch.Stop();
                ServiceLogger.Warn($"{GrpcLogPrefix} 请求 {requestId} 参数非法：{ex.Message}");
                throw BuildRpcException(StatusCode.InvalidArgument, requestId, GrpcErrorCodes.InvalidArgument, ex.Message);
            }
            catch (RpcException)
            {
                stopwatch.Stop();
                throw;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                ServiceLogger.Error($"{GrpcLogPrefix} 请求 {requestId} 下发人员信息时发生异常。", ex);
                throw BuildRpcException(StatusCode.Internal, requestId, GrpcErrorCodes.InternalError, "处理人员下发时发生未知错误。");
            }
        }

        private Task<string> HandleFaceDeleteAsync(string request, ServerCallContext context)
        {
            string peer = DescribePeer(context);
            string requestId = ResolveRequestId(context);
            int payloadLength = string.IsNullOrEmpty(request) ? 0 : Encoding.UTF8.GetByteCount(request);
            Stopwatch stopwatch = Stopwatch.StartNew();

            ServiceLogger.Info($"{GrpcLogPrefix} 请求 {requestId} (DeleteFaces) 来自 {peer}，载荷长度 {payloadLength} 字节。");
            LogPayloadIfEnabled("入站", requestId, request, payloadLength);

            try
            {
                List<string> ids = ParseEmployeeIdList(request);
                FaceOperationSummary summary = refreshManager.DeleteFacesOnDevices(ids);
                stopwatch.Stop();

                string responsePayload = BuildFaceOperationResponse(
                    requestId,
                    summary,
                    "人脸删除完成。",
                    "人脸删除部分失败。",
                    "人脸删除失败。");
                LogPayloadIfEnabled("出站", requestId, responsePayload, Encoding.UTF8.GetByteCount(responsePayload));
                return Task.FromResult(responsePayload);
            }
            catch (GrpcValidationException ex)
            {
                stopwatch.Stop();
                ServiceLogger.Warn($"{GrpcLogPrefix} 请求 {requestId} 参数非法：{ex.Message}");
                throw BuildRpcException(StatusCode.InvalidArgument, requestId, ex.ErrorCode ?? GrpcErrorCodes.InvalidArgument, ex.Message);
            }
            catch (JsonException ex)
            {
                stopwatch.Stop();
                string errorMessage = $"JSON格式错误：{ex.Message}";
                ServiceLogger.Warn($"{GrpcLogPrefix} 请求 {requestId} {errorMessage}");
                throw BuildRpcException(StatusCode.InvalidArgument, requestId, GrpcErrorCodes.InvalidArgument, errorMessage);
            }
            catch (ArgumentException ex)
            {
                stopwatch.Stop();
                ServiceLogger.Warn($"{GrpcLogPrefix} 请求 {requestId} 参数非法：{ex.Message}");
                throw BuildRpcException(StatusCode.InvalidArgument, requestId, GrpcErrorCodes.InvalidArgument, ex.Message);
            }
            catch (RpcException)
            {
                stopwatch.Stop();
                throw;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                ServiceLogger.Error($"{GrpcLogPrefix} 请求 {requestId} 删除人脸时发生异常。", ex);
                throw BuildRpcException(StatusCode.Internal, requestId, GrpcErrorCodes.InternalError, "处理人脸删除时发生未知错误。");
            }
        }

        private Task<string> HandlePersonDeleteAsync(string request, ServerCallContext context)
        {
            string peer = DescribePeer(context);
            string requestId = ResolveRequestId(context);
            int payloadLength = string.IsNullOrEmpty(request) ? 0 : Encoding.UTF8.GetByteCount(request);
            Stopwatch stopwatch = Stopwatch.StartNew();

            ServiceLogger.Info($"{GrpcLogPrefix} 请求 {requestId} (DeletePersons) 来自 {peer}，载荷长度 {payloadLength} 字节。");
            LogPayloadIfEnabled("入站", requestId, request, payloadLength);

            try
            {
                List<string> ids = ParseEmployeeIdList(request);
                PersonDeleteSummary summary = refreshManager.DeletePersonsFromDevices(ids);
                stopwatch.Stop();

                string responsePayload = BuildPersonDeleteResponse(requestId, summary);
                LogPayloadIfEnabled("出站", requestId, responsePayload, Encoding.UTF8.GetByteCount(responsePayload));
                return Task.FromResult(responsePayload);
            }
            catch (GrpcValidationException ex)
            {
                stopwatch.Stop();
                ServiceLogger.Warn($"{GrpcLogPrefix} 请求 {requestId} 参数非法：{ex.Message}");
                throw BuildRpcException(StatusCode.InvalidArgument, requestId, ex.ErrorCode ?? GrpcErrorCodes.InvalidArgument, ex.Message);
            }
            catch (JsonException ex)
            {
                stopwatch.Stop();
                string errorMessage = $"JSON格式错误：{ex.Message}";
                ServiceLogger.Warn($"{GrpcLogPrefix} 请求 {requestId} {errorMessage}");
                throw BuildRpcException(StatusCode.InvalidArgument, requestId, GrpcErrorCodes.InvalidArgument, errorMessage);
            }
            catch (ArgumentException ex)
            {
                stopwatch.Stop();
                ServiceLogger.Warn($"{GrpcLogPrefix} 请求 {requestId} 参数非法：{ex.Message}");
                throw BuildRpcException(StatusCode.InvalidArgument, requestId, GrpcErrorCodes.InvalidArgument, ex.Message);
            }
            catch (RpcException)
            {
                stopwatch.Stop();
                throw;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                ServiceLogger.Error($"{GrpcLogPrefix} 请求 {requestId} 删除人员时发生异常。", ex);
                throw BuildRpcException(StatusCode.Internal, requestId, GrpcErrorCodes.InternalError, "处理人员删除时发生未知错误。");
            }
        }

        private Task<string> HandleFaceGetAsync(string request, ServerCallContext context)
        {
            string peer = DescribePeer(context);
            string requestId = ResolveRequestId(context);
            int payloadLength = string.IsNullOrEmpty(request) ? 0 : Encoding.UTF8.GetByteCount(request);
            Stopwatch stopwatch = Stopwatch.StartNew();

            ServiceLogger.Info($"{GrpcLogPrefix} 请求 {requestId} (GetFaces) 来自 {peer}，载荷长度 {payloadLength} 字节。");
            LogPayloadIfEnabled("入站", requestId, request, payloadLength);

            try
            {
                List<string> ids = ParseEmployeeIdList(request);
                FaceOperationSummary summary = refreshManager.GetFacesFromDevices(ids);
                stopwatch.Stop();

                string responsePayload = BuildFaceOperationResponse(
                    requestId,
                    summary,
                    "人脸查询完成。",
                    "人脸查询部分失败。",
                    "人脸查询失败。");
                LogPayloadIfEnabled("出站", requestId, responsePayload, Encoding.UTF8.GetByteCount(responsePayload));
                return Task.FromResult(responsePayload);
            }
            catch (GrpcValidationException ex)
            {
                stopwatch.Stop();
                ServiceLogger.Warn($"{GrpcLogPrefix} 请求 {requestId} 参数非法：{ex.Message}");
                throw BuildRpcException(StatusCode.InvalidArgument, requestId, ex.ErrorCode ?? GrpcErrorCodes.InvalidArgument, ex.Message);
            }
            catch (JsonException ex)
            {
                stopwatch.Stop();
                string errorMessage = $"JSON格式错误：{ex.Message}";
                ServiceLogger.Warn($"{GrpcLogPrefix} 请求 {requestId} {errorMessage}");
                throw BuildRpcException(StatusCode.InvalidArgument, requestId, GrpcErrorCodes.InvalidArgument, errorMessage);
            }
            catch (ArgumentException ex)
            {
                stopwatch.Stop();
                ServiceLogger.Warn($"{GrpcLogPrefix} 请求 {requestId} 参数非法：{ex.Message}");
                throw BuildRpcException(StatusCode.InvalidArgument, requestId, GrpcErrorCodes.InvalidArgument, ex.Message);
            }
            catch (RpcException)
            {
                stopwatch.Stop();
                throw;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                ServiceLogger.Error($"{GrpcLogPrefix} 请求 {requestId} 查询人脸时发生异常。", ex);
                throw BuildRpcException(StatusCode.Internal, requestId, GrpcErrorCodes.InternalError, "处理人脸查询时发生未知错误。");
            }
        }

        private Task<string> HandleStatusGetAsync(string request, ServerCallContext context)
        {
            string requestId = ResolveRequestId(context);
            int payloadLength = string.IsNullOrEmpty(request) ? 0 : Encoding.UTF8.GetByteCount(request);
            ServiceLogger.Info($"{GrpcLogPrefix} 请求 {requestId} (GetEnrollmentStatus) 载荷长度 {payloadLength} 字节。");

            if (string.IsNullOrWhiteSpace(request))
            {
                throw BuildRpcException(StatusCode.InvalidArgument, requestId, GrpcErrorCodes.InvalidArgument, "请求体不能为空。");
            }

            string taskId;
            try
            {
                JToken root = JToken.Parse(request);
                taskId = root.Value<string>("taskId") ?? root.Value<string>("task_id");
            }
            catch (JsonException ex)
            {
                string errorMessage = $"JSON格式错误：{ex.Message}";
                throw BuildRpcException(StatusCode.InvalidArgument, requestId, GrpcErrorCodes.InvalidArgument, errorMessage);
            }

            if (string.IsNullOrWhiteSpace(taskId))
            {
                throw BuildRpcException(StatusCode.InvalidArgument, requestId, GrpcErrorCodes.InvalidArgument, "缺少 taskId。");
            }

            EnrollmentTaskStatus status = EnrollmentTaskStore.Get(taskId);
            if (status == null)
            {
                throw BuildRpcException(StatusCode.NotFound, requestId, GrpcErrorCodes.NotFound,
                    $"任务 {taskId} 不存在或已过期。");
            }

            var payload = new
            {
                taskId = status.TaskId,
                employeeId = status.EmployeeId,
                action = status.Action,
                status = status.Status,
                message = status.Message,
                errorCode = status.ErrorCode
            };

            string responsePayload = BuildStandardPayload(
                requestId,
                true,
                GrpcErrorCodes.Ok,
                "查询成功。",
                Array.Empty<string>(),
                Array.Empty<GrpcErrorDetail>(),
                payload);

            return Task.FromResult(responsePayload);
        }

        private async Task HandleCaptureStreamAsync(string request, IServerStreamWriter<string> responseStream, ServerCallContext context)
        {
            string peer = DescribePeer(context);
            string requestId = ResolveRequestId(context);
            int payloadLength = string.IsNullOrEmpty(request) ? 0 : Encoding.UTF8.GetByteCount(request);
            ServiceLogger.Info($"{GrpcLogPrefix} 请求 {requestId} (CaptureFaceStream) 来自 {peer}，载荷长度 {payloadLength} 字节。");

            string employeeId = null;
            string taskId;

            try
            {
                JToken root = JToken.Parse(request);
                employeeId = root.Value<string>("employee_id") ??
                             root.Value<string>("employeeId") ??
                             root.Value<string>("employee_no") ??
                             root.Value<string>("employeeNo");
            }
            catch (JsonException ex)
            {
                string errorMessage = $"JSON格式错误：{ex.Message}";
                throw BuildRpcException(StatusCode.InvalidArgument, requestId, GrpcErrorCodes.InvalidArgument, errorMessage);
            }

            if (string.IsNullOrWhiteSpace(employeeId))
            {
                throw BuildRpcException(StatusCode.InvalidArgument, requestId, GrpcErrorCodes.InvalidArgument, "缺少 employee_id。");
            }

            taskId = EnrollmentTaskStore.CreateTask(employeeId, "CaptureFaceStream");

            FaceCaptureResult capture = refreshManager.CaptureFaceFromEnrollmentDevice();
            if (!capture.Success)
            {
                string errorCode = ResolveCaptureErrorCode(capture);
                EnrollmentTaskStore.Complete(taskId, false, capture.ErrorMessage, "CAPTURE_FAILED");

                List<GrpcErrorDetail> details = new List<GrpcErrorDetail>();
                if (capture.DeviceId.HasValue ||
                    !string.IsNullOrWhiteSpace(capture.DeviceName) ||
                    !string.IsNullOrWhiteSpace(capture.DeviceIp))
                {
                    details.Add(new GrpcErrorDetail
                    {
                        EmployeeId = employeeId,
                        DeviceId = capture.DeviceId,
                        DeviceName = capture.DeviceName,
                        DeviceIp = capture.DeviceIp,
                        Code = errorCode,
                        Message = capture.ErrorMessage
                    });
                }

                var errorPayload = new
                {
                    taskId,
                    employeeId,
                    status = "Failed",
                    message = capture.ErrorMessage,
                    errorCode = "CAPTURE_FAILED"
                };

                string responsePayload = BuildStandardPayload(
                    requestId,
                    false,
                    errorCode,
                    capture.ErrorMessage,
                    new[] { capture.ErrorMessage },
                    details,
                    errorPayload);

                await responseStream.WriteAsync(responsePayload).ConfigureAwait(false);
                return;
            }

            var framePayload = new
            {
                taskId,
                employeeId,
                frameIndex = 1,
                faceImageBase64 = capture.FaceImageBase64,
                faceImageFormat = capture.Format,
                qualityScore = (int?)null,
                recommend = true
            };

            string successPayload = BuildStandardPayload(
                requestId,
                true,
                GrpcErrorCodes.Ok,
                "采集成功。",
                Array.Empty<string>(),
                Array.Empty<GrpcErrorDetail>(),
                framePayload);

            await responseStream.WriteAsync(successPayload).ConfigureAwait(false);
            EnrollmentTaskStore.Complete(taskId, true, "采集完成");
        }

        private static string DescribePeer(ServerCallContext context)
        {
            if (context == null)
            {
                return "未知客户端";
            }

            if (!string.IsNullOrWhiteSpace(context.Peer))
            {
                return context.Peer;
            }

            Metadata headers = context.RequestHeaders;
            if (headers != null)
            {
                Metadata.Entry forwardEntry = headers.FirstOrDefault(
                    h => string.Equals(h.Key, "x-forwarded-for", StringComparison.OrdinalIgnoreCase));
                if (forwardEntry != null && !string.IsNullOrWhiteSpace(forwardEntry.Value))
                {
                    return forwardEntry.Value;
                }
            }

            return "未知客户端";
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

        private static void LogGrpcSummary(string peer, string requestId, PermissionRefreshSummary summary, TimeSpan duration)
        {
            string baseMessage = string.Format(
                CultureInfo.InvariantCulture,
                "{0} 请求 {1} / 客户端 {2} 权限同步完成：总数 {3}，成功 {4}，跳过 {5}，失败 {6}，耗时 {7} ms。",
                GrpcLogPrefix,
                requestId,
                peer,
                summary.TotalUsers,
                summary.UsersUpdated,
                summary.UsersSkipped,
                summary.UsersFailed,
                Math.Round(duration.TotalMilliseconds, 2, MidpointRounding.AwayFromZero));

            if (summary.Errors.Count == 0)
            {
                ServiceLogger.Info(baseMessage);
                return;
            }

            string errorPreview = string.Join(" | ", summary.Errors.Take(3));
            if (summary.Errors.Count > 3)
            {
                errorPreview += " ...";
            }

            ServiceLogger.Warn($"{baseMessage} 错误示例：{errorPreview}");
        }

        private static void LogPersonSummary(string peer, string requestId, PersonSyncSummary summary, TimeSpan duration)
        {
            string baseMessage = string.Format(
                CultureInfo.InvariantCulture,
                "{0} 请求 {1} / 客户端 {2} 人员下发完成：人员 {3}，成功 {4}，失败 {5}，人脸下发 {6} 次，涉及 {7} 台设备，耗时 {8} ms。",
                GrpcLogPrefix,
                requestId,
                peer,
                summary.TotalPersons,
                summary.SuccessfulPersons,
                summary.FailedPersons,
                summary.FacesUploaded,
                summary.TargetDevices,
                Math.Round(duration.TotalMilliseconds, 2, MidpointRounding.AwayFromZero));

            if (summary.Errors.Count == 0)
            {
                ServiceLogger.Info(baseMessage);
                return;
            }

            string errorPreview = string.Join(" | ", summary.Errors.Take(3));
            if (summary.Errors.Count > 3)
            {
                errorPreview += " ...";
            }

            ServiceLogger.Warn($"{baseMessage} 错误示例：{errorPreview}");
        }

        private static List<PermissionUpdateInfo> ParseUpdates(string payload)
        {
            if (string.IsNullOrWhiteSpace(payload))
            {
                return new List<PermissionUpdateInfo>();
            }

            JToken root = JToken.Parse(payload);
            List<PermissionUpdateInfo> updates = new List<PermissionUpdateInfo>();

            if (root.Type == JTokenType.Array)
            {
                foreach (JToken item in root)
                {
                    TryAddUpdate(item, updates);
                }
            }
            else if (root.Type == JTokenType.Object)
            {
                if (root["items"] is JArray itemsArray)
                {
                    foreach (JToken item in itemsArray)
                    {
                        TryAddUpdate(item, updates);
                    }
                }
                else if (root["records"] is JArray recordsArray)
                {
                    foreach (JToken item in recordsArray)
                    {
                        TryAddUpdate(item, updates);
                    }
                }
                else
                {
                    TryAddUpdate(root, updates);
                }
            }
            else
            {
                throw new JsonException("不支持的JSON结构。");
            }

            if (updates.Count > MaxBatchSize)
            {
                throw new GrpcValidationException(
                    string.Format(CultureInfo.InvariantCulture,
                        "批量上限为 {0} 条，当前 {1} 条。",
                        MaxBatchSize,
                        updates.Count),
                    GrpcErrorCodes.BatchTooLarge);
            }

            return updates;
        }

        private static void TryAddUpdate(JToken token, ICollection<PermissionUpdateInfo> target)
        {
            if (token == null || token.Type != JTokenType.Object)
            {
                return;
            }

            string employeeId = token.Value<string>("employee_id");
            if (string.IsNullOrWhiteSpace(employeeId))
            {
                throw new ArgumentException("字段 employee_id 不能为空。");
            }

            JToken permissionToken = token["permission_code"];
            if (permissionToken == null || permissionToken.Type == JTokenType.Null)
            {
                throw new ArgumentException(string.Format(CultureInfo.InvariantCulture,
                    "员工 {0} 缺少权限编号字段。", employeeId));
            }

            if (!int.TryParse(permissionToken.ToString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int permissionCode))
            {
                throw new ArgumentException(string.Format(CultureInfo.InvariantCulture,
                    "员工 {0} 的权限编号无效：{1}", employeeId, permissionToken));
            }

            target.Add(new PermissionUpdateInfo(employeeId, permissionCode));
        }

        private List<PersonSyncRequest> ParsePersonSyncRequests(string payload)
        {
            if (string.IsNullOrWhiteSpace(payload))
            {
                return new List<PersonSyncRequest>();
            }

            JToken root = JToken.Parse(payload);
            List<PersonSyncRequest> persons = new List<PersonSyncRequest>();

            foreach (JToken token in EnumeratePersonTokens(root))
            {
                PersonSyncRequest request = ConvertPersonToken(token);
                if (request != null)
                {
                    persons.Add(request);
                }
            }

            if (persons.Count > MaxBatchSize)
            {
                throw new GrpcValidationException(
                    string.Format(CultureInfo.InvariantCulture,
                        "批量上限为 {0} 条，当前 {1} 条。",
                        MaxBatchSize,
                        persons.Count),
                    GrpcErrorCodes.BatchTooLarge);
            }

            return persons;
        }

        private static IEnumerable<JToken> EnumeratePersonTokens(JToken root)
        {
            if (root == null)
            {
                yield break;
            }

            if (root.Type == JTokenType.Array)
            {
                foreach (JToken item in root)
                {
                    if (item != null)
                    {
                        yield return item;
                    }
                }

                yield break;
            }

            if (root.Type == JTokenType.Object)
            {
                foreach (string property in new[] { "people", "items", "records", "data" })
                {
                    if (root[property] is JArray array)
                    {
                        foreach (JToken item in array)
                        {
                            if (item != null)
                            {
                                yield return item;
                            }
                        }

                        yield break;
                    }
                }

                yield return root;
                yield break;
            }

            throw new JsonException("不支持的JSON结构。");
        }

        private PersonSyncRequest ConvertPersonToken(JToken token)
        {
            if (token == null || token.Type != JTokenType.Object)
            {
                return null;
            }

            string employeeId = ReadFirstString(token, "employee_id", "employeeId", "employee_no", "employeeNo");
            if (string.IsNullOrWhiteSpace(employeeId))
            {
                throw new ArgumentException("字段 employee_id 不能为空。");
            }

            PersonSyncRequest request = new PersonSyncRequest
            {
                EmployeeId = employeeId,
                FullName = ReadFirstString(token, "name", "full_name", "fullName"),
                Gender = ReadFirstString(token, "gender", "sex"),
                Enabled = ReadNullableBool(token, "enabled", "active", "is_active") ?? true,
                ValidFrom = ParseNullableDateTime(ReadFirstString(token, "valid_from", "validFrom")),
                ValidTo = ParseNullableDateTime(ReadFirstString(token, "valid_to", "validTo")),
                FaceImageFormat = ReadFirstString(token, "face_image_format", "faceImageFormat")
            };

            string faceBase64 = ReadFirstString(token, "face_image_base64", "faceImageBase64", "face_base64", "faceBase64", "face_image");
            if (!string.IsNullOrWhiteSpace(faceBase64))
            {
                request.FaceImageBytes = ParseFaceBytes(faceBase64);
            }

            return request;
        }

        private static string ReadFirstString(JToken token, params string[] aliases)
        {
            if (token == null || aliases == null)
            {
                return null;
            }

            foreach (string alias in aliases)
            {
                if (string.IsNullOrWhiteSpace(alias))
                {
                    continue;
                }

                JToken valueToken = token[alias];
                if (valueToken == null || valueToken.Type == JTokenType.Null)
                {
                    continue;
                }

                if (valueToken.Type == JTokenType.String || valueToken.Type == JTokenType.Integer)
                {
                    string value = valueToken.ToString();
                    if (!string.IsNullOrWhiteSpace(value))
                    {
                        return value;
                    }
                }
            }

            return null;
        }

        private static bool? ReadNullableBool(JToken token, params string[] aliases)
        {
            foreach (string alias in aliases)
            {
                JToken valueToken = token?[alias];
                if (valueToken == null || valueToken.Type == JTokenType.Null)
                {
                    continue;
                }

                if (valueToken.Type == JTokenType.Boolean)
                {
                    return valueToken.Value<bool>();
                }

                if (valueToken.Type == JTokenType.String)
                {
                    string text = valueToken.Value<string>();
                    if (bool.TryParse(text, out bool boolValue))
                    {
                        return boolValue;
                    }

                    if (string.Equals(text, "enabled", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(text, "active", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(text, "true", StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }

                    if (string.Equals(text, "disabled", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(text, "false", StringComparison.OrdinalIgnoreCase))
                    {
                        return false;
                    }
                }
            }

            return null;
        }

        private static DateTime? ParseNullableDateTime(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            if (DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out DateTime parsed))
            {
                return parsed;
            }

            throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, "时间字段格式不正确：{0}", value));
        }

        private static byte[] ParseFaceBytes(string base64Value)
        {
            string normalized = base64Value.Trim();
            int commaIndex = normalized.IndexOf(',');
            if (commaIndex >= 0)
            {
                normalized = normalized.Substring(commaIndex + 1);
            }

            try
            {
                return Convert.FromBase64String(normalized);
            }
            catch (FormatException ex)
            {
                throw new ArgumentException(string.Format(CultureInfo.InvariantCulture,
                    "人脸图片Base64解析失败：{0}", ex.Message));
            }
        }

        private static string BuildResponse(string requestId, PermissionRefreshSummary summary)
        {
            int succeeded = summary.UsersUpdated + summary.UsersSkipped;
            ResolveSummaryMeta(succeeded, summary.UsersFailed, summary.Errors.Count > 0,
                "权限同步完成。",
                "权限同步部分失败。",
                "权限同步失败。",
                out bool success,
                out string code,
                out string message);

            var payload = new
            {
                total = summary.TotalUsers,
                updated = summary.UsersUpdated,
                skipped = summary.UsersSkipped,
                failed = summary.UsersFailed
            };

            return BuildStandardPayload(requestId, success, code, message, summary.Errors, summary.ErrorDetails, payload);
        }

        private static string BuildPersonSyncResponse(string requestId, PersonSyncSummary summary)
        {
            ResolveSummaryMeta(summary.SuccessfulPersons, summary.FailedPersons, summary.Errors.Count > 0,
                "人员下发完成。",
                "人员下发部分失败。",
                "人员下发失败。",
                out bool success,
                out string code,
                out string message);

            var payload = new
            {
                total = summary.TotalPersons,
                succeeded = summary.SuccessfulPersons,
                failed = summary.FailedPersons,
                facesUploaded = summary.FacesUploaded,
                targetDevices = summary.TargetDevices
            };

            return BuildStandardPayload(requestId, success, code, message, summary.Errors, summary.ErrorDetails, payload);
        }

        private static string BuildFaceOperationResponse(string requestId, FaceOperationSummary summary, string successMessage, string partialMessage, string failedMessage)
        {
            ResolveSummaryMeta(summary.Succeeded, summary.Failed, summary.Errors.Count > 0,
                successMessage,
                partialMessage,
                failedMessage,
                out bool success,
                out string code,
                out string message);

            var payload = new
            {
                total = summary.Total,
                succeeded = summary.Succeeded,
                failed = summary.Failed,
                targetDevices = summary.TargetDevices,
                items = summary.Items.Select(i => new
                {
                    employeeId = i.EmployeeId,
                    success = i.Success,
                    faceImageBase64 = i.FaceImageBase64,
                    rawResponse = i.RawResponse,
                    error = i.Error
                }).ToArray()
            };

            return BuildStandardPayload(requestId, success, code, message, summary.Errors, summary.ErrorDetails, payload);
        }

        private static string BuildPersonDeleteResponse(string requestId, PersonDeleteSummary summary)
        {
            ResolveSummaryMeta(summary.Succeeded, summary.Failed, summary.Errors.Count > 0,
                "人员删除完成。",
                "人员删除部分失败。",
                "人员删除失败。",
                out bool success,
                out string code,
                out string message);

            var payload = new
            {
                total = summary.Total,
                succeeded = summary.Succeeded,
                failed = summary.Failed,
                targetDevices = summary.TargetDevices,
                items = summary.Items.Select(i => new
                {
                    employeeId = i.EmployeeId,
                    success = i.Success,
                    successDevices = i.SuccessDevices,
                    failedDevices = i.FailedDevices,
                    deviceErrors = i.DeviceErrors.ToArray()
                }).ToArray()
            };

            return BuildStandardPayload(requestId, success, code, message, summary.Errors, summary.ErrorDetails, payload);
        }


        private static void ResolveSummaryMeta(int succeeded, int failed, bool hasErrors,
            string successMessage,
            string partialMessage,
            string failedMessage,
            out bool success,
            out string code,
            out string message)
        {
            if (failed <= 0 && succeeded <= 0 && hasErrors)
            {
                success = false;
                code = GrpcErrorCodes.Failed;
                message = failedMessage;
                return;
            }

            if (failed <= 0)
            {
                success = true;
                code = GrpcErrorCodes.Ok;
                message = successMessage;
                return;
            }

            if (succeeded > 0)
            {
                success = false;
                code = GrpcErrorCodes.PartialSuccess;
                message = partialMessage;
                return;
            }

            success = false;
            code = GrpcErrorCodes.Failed;
            message = failedMessage;
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

        private static string ResolveCaptureErrorCode(FaceCaptureResult capture)
        {
            string message = capture?.ErrorMessage ?? string.Empty;
            if (message.IndexOf("200KB", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return GrpcErrorCodes.FaceTooLarge;
            }

            return GrpcErrorCodes.DeviceError;
        }

        private static List<string> ParseEmployeeIdList(string payload)
        {
            if (string.IsNullOrWhiteSpace(payload))
            {
                throw new ArgumentException("请求体不能为空。");
            }

            JToken root = JToken.Parse(payload);
            List<string> ids = new List<string>();

            void tryAdd(JToken token)
            {
                if (token == null || token.Type == JTokenType.Null)
                {
                    return;
                }

                if (token.Type == JTokenType.String || token.Type == JTokenType.Integer)
                {
                    string value = token.ToString();
                    if (!string.IsNullOrWhiteSpace(value))
                    {
                        ids.Add(value.Trim());
                    }
                    return;
                }

                if (token.Type == JTokenType.Object)
                {
                    string id = ((JObject)token).Value<string>("employee_id") ??
                                ((JObject)token).Value<string>("employeeId") ??
                                ((JObject)token).Value<string>("employee_no") ??
                                ((JObject)token).Value<string>("employeeNo");
                    if (!string.IsNullOrWhiteSpace(id))
                    {
                        ids.Add(id.Trim());
                    }
                }
            }

            if (root.Type == JTokenType.Array)
            {
                foreach (JToken item in root)
                {
                    tryAdd(item);
                }
            }
            else if (root.Type == JTokenType.Object)
            {
                if (root["items"] is JArray itemsArray)
                {
                    foreach (JToken item in itemsArray)
                    {
                        tryAdd(item);
                    }
                }
                else if (root["records"] is JArray recordsArray)
                {
                    foreach (JToken item in recordsArray)
                    {
                        tryAdd(item);
                    }
                }
                else
                {
                    tryAdd(root);
                }
            }
            else
            {
                tryAdd(root);
            }

            if (ids.Count == 0)
            {
                throw new ArgumentException("未解析到有效的员工编号。");
            }

            List<string> normalized = ids
                .Where(id => !string.IsNullOrWhiteSpace(id))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (normalized.Count > MaxBatchSize)
            {
                throw new GrpcValidationException(
                    string.Format(CultureInfo.InvariantCulture,
                        "批量上限为 {0} 条，当前 {1} 条。",
                        MaxBatchSize,
                        normalized.Count),
                    GrpcErrorCodes.BatchTooLarge);
            }

            return normalized;
        }

        private void LogPayloadIfEnabled(string direction, string requestId, string payload, int payloadBytes)
        {
            if (!logPayloads)
            {
                return;
            }

            bool truncated;
            string formattedPayload = FormatPayloadForLog(payload, out truncated);
            string sizeInfo = truncated
                ? string.Format(CultureInfo.InvariantCulture,
                    "（原始 {0} 字节，已截断至 {1} 字符）",
                    payloadBytes,
                    payloadLogMaxChars)
                : string.Format(CultureInfo.InvariantCulture, "（{0} 字节）", payloadBytes);

            ServiceLogger.Debug(string.Format(CultureInfo.InvariantCulture,
                "{0} 请求 {1} {2} JSON {3}：{4}",
                GrpcLogPrefix,
                requestId,
                direction,
                sizeInfo,
                formattedPayload));
        }

        private string FormatPayloadForLog(string payload, out bool truncated)
        {
            truncated = false;

            if (string.IsNullOrEmpty(payload))
            {
                return "<空>";
            }

            if (payloadLogMaxChars <= 0 || payload.Length <= payloadLogMaxChars)
            {
                return payload;
            }

            truncated = true;
            return payload.Substring(0, payloadLogMaxChars) + "...";
        }

        public void Dispose()
        {
            StopAsync().GetAwaiter().GetResult();
        }
    }
}
