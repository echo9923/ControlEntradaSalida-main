using System;
using System.IO;
using ControlEntradaSalida.Configuration;

namespace ControlEntradaSalida
{
    /// <summary>
    /// 服务配置读取器，负责从外部配置文件加载 gRPC、日志等参数。
    /// </summary>
    public sealed class ServiceConfiguration
    {
        private const int DefaultLogRetentionDays = 90;
        private const int DefaultPayloadLogMaxChars = 2048;

        private static readonly Lazy<ServiceConfiguration> LazyInstance =
            new Lazy<ServiceConfiguration>(Load);

        public static ServiceConfiguration Current => LazyInstance.Value;

        public int GrpcListenPort { get; private set; }

        public string LogDirectory { get; private set; }

        public int LogRetentionDays { get; private set; }

        public bool LogGrpcPayloads { get; private set; }

        public int GrpcPayloadLogMaxChars { get; private set; }

        private ServiceConfiguration()
        {
        }

        private static ServiceConfiguration Load()
        {
            var configuration = new ServiceConfiguration();

            ExternalConfiguration.ServiceSection serviceSection = ExternalConfiguration.Current.Service;

            configuration.GrpcListenPort = serviceSection?.GrpcListenPort ?? 5001;
            configuration.LogDirectory = ResolveLogDirectory(serviceSection?.LogDirectory);
            configuration.LogRetentionDays = ResolveLogRetentionDays(serviceSection?.LogRetentionDays);
            configuration.LogGrpcPayloads = serviceSection?.LogGrpcPayloads ?? false;
            configuration.GrpcPayloadLogMaxChars = ResolvePayloadLogMaxChars(serviceSection?.GrpcPayloadLogMaxChars);

            EnsureLogDirectory(configuration.LogDirectory);

            return configuration;
        }

        private static void EnsureLogDirectory(string directory)
        {
            if (string.IsNullOrWhiteSpace(directory))
            {
                return;
            }

            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
        }

        private static string ResolveLogDirectory(string configuredDirectory)
        {
            if (string.IsNullOrWhiteSpace(configuredDirectory))
            {
                return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs");
            }

            return configuredDirectory.Trim();
        }

        private static int ResolveLogRetentionDays(int? configuredRetentionDays)
        {
            if (configuredRetentionDays.HasValue && configuredRetentionDays.Value > 0)
            {
                return configuredRetentionDays.Value;
            }

            return DefaultLogRetentionDays;
        }

        private static int ResolvePayloadLogMaxChars(int? configuredMaxChars)
        {
            if (configuredMaxChars.HasValue && configuredMaxChars.Value > 0)
            {
                return configuredMaxChars.Value;
            }

            return DefaultPayloadLogMaxChars;
        }
    }
}
