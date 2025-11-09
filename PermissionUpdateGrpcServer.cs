using Grpc.Core;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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
        private const string ServiceName = "permission.PermissionSyncService";
        private const string MethodName = "SyncPermissions";

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

        private readonly PermissionRefreshManager refreshManager;
        private readonly object lifecycleLock = new object();
        private readonly bool logPayloads;
        private readonly int payloadLogMaxChars;

        private CancellationTokenSource shutdownTokenSource;
        private Server grpcServer;
        private Thread listenerThread;
        private int listenPort = DefaultPort;

        public PermissionUpdateGrpcServer(PermissionRefreshManager refreshManager, bool logPayloads, int payloadLogMaxChars)
        {
            this.refreshManager = refreshManager ?? throw new ArgumentNullException(nameof(refreshManager));
            this.logPayloads = logPayloads;
            this.payloadLogMaxChars = payloadLogMaxChars > 0 ? payloadLogMaxChars : 0;
        }

        public void Start(int port = DefaultPort)
        {
            lock (lifecycleLock)
            {
                if (grpcServer != null)
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
                if (grpcServer == null)
                {
                    return Task.CompletedTask;
                }

                serverToShutdown = grpcServer;
                tokenSource = shutdownTokenSource;
                threadToJoin = listenerThread;

                grpcServer = null;
                listenerThread = null;
                shutdownTokenSource = null;
            }

            tokenSource?.Cancel();

            async Task ShutdownCoreAsync()
            {
                try
                {
                    await serverToShutdown.ShutdownAsync().ConfigureAwait(false);
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
            try
            {
                grpcServer = new Server
                {
                    Services =
                    {
                        ServerServiceDefinition.CreateBuilder()
                            .AddMethod(UpdatePermissionMethod, HandlePermissionUpdateAsync)
                            .Build()
                    },
                    Ports =
                    {
                        new ServerPort("0.0.0.0", listenPort, ServerCredentials.Insecure)
                    }
                };

                grpcServer.Start();

                ServiceLogger.Info(string.Format(CultureInfo.InvariantCulture,
                    "权限GRPC服务已启动，端口：{0}。", listenPort));

                CancellationToken token = shutdownTokenSource.Token;
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
                    string responsePayload = BuildResponse(summary);
                    LogPayloadIfEnabled("出站", requestId, responsePayload, Encoding.UTF8.GetByteCount(responsePayload));
                    return Task.FromResult(responsePayload);
                }

                PermissionRefreshSummary result = refreshManager.RefreshPermissionsForEmployees(updates);
                stopwatch.Stop();
                LogGrpcSummary(peer, requestId, result, stopwatch.Elapsed);
                string successPayload = BuildResponse(result);
                LogPayloadIfEnabled("出站", requestId, successPayload, Encoding.UTF8.GetByteCount(successPayload));
                return Task.FromResult(successPayload);
            }
            catch (JsonException ex)
            {
                stopwatch.Stop();
                ServiceLogger.Warn($"{GrpcLogPrefix} 请求 {requestId} JSON格式错误：{ex.Message}");
                throw new RpcException(
                    new Status(StatusCode.InvalidArgument,
                        string.Format(CultureInfo.InvariantCulture,
                            "JSON格式错误：{0}", ex.Message)));
            }
            catch (ArgumentException ex)
            {
                stopwatch.Stop();
                ServiceLogger.Warn($"{GrpcLogPrefix} 请求 {requestId} 参数非法：{ex.Message}");
                throw new RpcException(new Status(StatusCode.InvalidArgument, ex.Message));
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
                throw new RpcException(new Status(StatusCode.Internal, "处理权限更新时发生未知错误。"));
            }
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

        private static string BuildResponse(PermissionRefreshSummary summary)
        {
            var payload = new
            {
                total = summary.TotalUsers,
                updated = summary.UsersUpdated,
                skipped = summary.UsersSkipped,
                failed = summary.UsersFailed,
                errors = summary.Errors.ToArray()
            };

            return JsonConvert.SerializeObject(payload);
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
