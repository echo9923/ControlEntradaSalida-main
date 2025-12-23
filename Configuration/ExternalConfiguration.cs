using System;
using System.IO;
using System.Text;
using Newtonsoft.Json;

namespace ControlEntradaSalida.Configuration
{
    /// <summary>
    /// 外部配置加载器，负责读取可部署的 JSON 配置文件并在运行期缓存。
    /// </summary>
    public sealed class ExternalConfiguration
    {
        private const string ConfigRelativePath = "Configuration\\appsettings.json";

        private static readonly Lazy<ExternalConfiguration> LazyInstance =
            new Lazy<ExternalConfiguration>(Load, true);

        public static ExternalConfiguration Current => LazyInstance.Value;

        [JsonIgnore]
        public string SourcePath { get; private set; }

        public ServiceSection Service { get; set; } = new ServiceSection();

        public DatabaseSection Database { get; set; } = new DatabaseSection();

        public FaceEventLoggingSection FaceEventLogging { get; set; } = new FaceEventLoggingSection();

        public DeviceConnectionSection DeviceConnection { get; set; } = new DeviceConnectionSection();

        public ReconnectSection Reconnect { get; set; } = new ReconnectSection();

        private static ExternalConfiguration Load()
        {
            string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
            string configPath = Path.Combine(baseDirectory, ConfigRelativePath);

            if (!File.Exists(configPath))
            {
                throw new FileNotFoundException($"未找到外部配置文件，请确认路径: {configPath}");
            }

            string json = File.ReadAllText(configPath, Encoding.UTF8);

            ExternalConfiguration configuration;
            try
            {
                configuration = JsonConvert.DeserializeObject<ExternalConfiguration>(json) ?? new ExternalConfiguration();
            }
            catch (JsonException ex)
            {
                throw new InvalidOperationException($"外部配置文件格式不正确，请检查 {configPath}", ex);
            }

            configuration.SourcePath = configPath;
            configuration.Service ??= new ServiceSection();
            configuration.Database ??= new DatabaseSection();
            configuration.FaceEventLogging ??= new FaceEventLoggingSection();
            configuration.DeviceConnection ??= new DeviceConnectionSection();
            configuration.Reconnect ??= new ReconnectSection();

            return configuration;
        }

        public sealed class ServiceSection
        {
            /// <summary>
            /// gRPC 监听端口，可通过外部配置覆盖。
            /// </summary>
            public int? GrpcListenPort { get; set; }

            /// <summary>
            /// 日志目录路径。
            /// </summary>
            public string LogDirectory { get; set; }

            /// <summary>
            /// 日志保留天数。
            /// </summary>
            public int? LogRetentionDays { get; set; }

            /// <summary>
            /// 是否记录 gRPC 请求与响应的 JSON 载荷。
            /// </summary>
            public bool? LogGrpcPayloads { get; set; }

            /// <summary>
            /// gRPC JSON 日志的最大字符数，超出部分将被截断。
            /// </summary>
            public int? GrpcPayloadLogMaxChars { get; set; }
        }

        public sealed class DatabaseSection
        {
            /// <summary>
            /// SQL Server 连接字符串。
            /// </summary>
            public string ConnectionString { get; set; }

            /// <summary>
            /// 数据库命令超时时间（秒）。
            /// </summary>
            public int? CommandTimeoutSeconds { get; set; }
        }

        public sealed class FaceEventLoggingSection
        {
            /// <summary>
            /// 是否启用人脸事件入库。
            /// </summary>
            public bool? Enabled { get; set; }

            /// <summary>
            /// 事件处理队列容量。
            /// </summary>
            public int? QueueCapacity { get; set; }

            /// <summary>
            /// 每批写库条数。
            /// </summary>
            public int? BatchSize { get; set; }

            /// <summary>
            /// 写库失败后的重试间隔（秒）。
            /// </summary>
            public int? RetryIntervalSeconds { get; set; }

            /// <summary>
            /// 设备重连后的补偿时间窗口（分钟）。
            /// </summary>
            public int? CompensationLookbackMinutes { get; set; }

            /// <summary>
            /// 报警布防类型（技术规范：0-客户端布防（实时+离线），1-实时布防（仅实时））。
            /// </summary>
            public int? AlarmDeployType { get; set; }
        }

        public sealed class DeviceConnectionSection
        {
            /// <summary>
            /// 状态检查间隔（毫秒）。
            /// </summary>
            public int? StatusCheckIntervalMs { get; set; }

            /// <summary>
            /// 状态检查获取设备 SDK 锁的等待时间（毫秒），超时将跳过本轮状态检查。
            /// </summary>
            public int? StatusCheckSdkLockTimeoutMs { get; set; }

            /// <summary>
            /// 设备级 SDK 锁等待时间（毫秒），用于登录/登出/远程配置等互斥操作。
            /// </summary>
            public int? DeviceSdkLockTimeoutMs { get; set; }

            /// <summary>
            /// 连接操作等待超时（毫秒）。
            /// </summary>
            public int? ConnectTimeoutMs { get; set; }

            /// <summary>
            /// 断开连接操作等待超时（毫秒）。
            /// </summary>
            public int? DisconnectTimeoutMs { get; set; }

            /// <summary>
            /// 最大并发连接/状态检查并发度。
            /// </summary>
            public int? MaxConcurrentConnections { get; set; }
        }

        public sealed class ReconnectSection
        {
            /// <summary>
            /// 最大重连尝试次数。
            /// </summary>
            public int? MaxReconnectAttempts { get; set; }

            /// <summary>
            /// 重连基准延迟（毫秒）。
            /// </summary>
            public int? BaseDelayMs { get; set; }

            /// <summary>
            /// 重连最大延迟（毫秒）。
            /// </summary>
            public int? MaxDelayMs { get; set; }

            /// <summary>
            /// 达到最大重连次数后的冷却期（毫秒）。
            /// </summary>
            public int? PermanentFailureCooldownMs { get; set; }

            /// <summary>
            /// 重连检查间隔（毫秒）。
            /// </summary>
            public int? ReconnectCheckIntervalMs { get; set; }
        }
    }
}
