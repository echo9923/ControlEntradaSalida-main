using Grpc.Core;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
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

        private CancellationTokenSource shutdownTokenSource;
        private Server grpcServer;
        private Thread listenerThread;
        private int listenPort = DefaultPort;

        public PermissionUpdateGrpcServer(PermissionRefreshManager refreshManager)
        {
            this.refreshManager = refreshManager ?? throw new ArgumentNullException(nameof(refreshManager));
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
                    System.Diagnostics.Trace.TraceError(
                        string.Format(CultureInfo.InvariantCulture,
                            "停止权限GRPC服务时发生异常：{0}", ex));
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

                System.Diagnostics.Trace.TraceInformation(
                    string.Format(CultureInfo.InvariantCulture,
                        "权限GRPC服务已启动，端口：{0}", listenPort));

                CancellationToken token = shutdownTokenSource.Token;
                token.WaitHandle.WaitOne();
            }
            catch (IOException ex)
            {
                System.Diagnostics.Trace.TraceError(
                    string.Format(CultureInfo.InvariantCulture,
                        "权限GRPC服务启动失败，可能端口被占用：{0}", ex.Message));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.TraceError(
                    string.Format(CultureInfo.InvariantCulture,
                        "权限GRPC服务运行异常：{0}", ex));
            }
        }

        private Task<string> HandlePermissionUpdateAsync(string request, ServerCallContext context)
        {
            try
            {
                List<PermissionUpdateInfo> updates = ParseUpdates(request);
                if (updates.Count == 0)
                {
                    PermissionRefreshSummary summary = new PermissionRefreshSummary();
                    summary.Errors.Add("未解析到有效的权限更新数据。");
                    summary.UsersFailed = 0;
                    return Task.FromResult(BuildResponse(summary));
                }

                PermissionRefreshSummary result = refreshManager.RefreshPermissionsForEmployees(updates);
                return Task.FromResult(BuildResponse(result));
            }
            catch (JsonException ex)
            {
                throw new RpcException(
                    new Status(StatusCode.InvalidArgument,
                        string.Format(CultureInfo.InvariantCulture,
                            "JSON格式错误：{0}", ex.Message)));
            }
            catch (ArgumentException ex)
            {
                throw new RpcException(new Status(StatusCode.InvalidArgument, ex.Message));
            }
            catch (RpcException)
            {
                throw;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.TraceError(
                    string.Format(CultureInfo.InvariantCulture,
                        "处理权限更新时发生异常：{0}", ex));
                throw new RpcException(new Status(StatusCode.Internal, "处理权限更新时发生未知错误。"));
            }
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

        public void Dispose()
        {
            StopAsync().GetAwaiter().GetResult();
        }
    }
}
