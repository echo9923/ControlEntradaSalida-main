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
    }
}
