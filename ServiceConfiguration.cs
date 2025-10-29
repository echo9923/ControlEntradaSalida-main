using System;
using System.Configuration;
using System.IO;

namespace ControlEntradaSalida
{
    /// <summary>
    /// 服务配置读取器，负责从App.config加载 gRPC、日志等参数。
    /// </summary>
    public sealed class ServiceConfiguration
    {
        private const string GrpcPortKey = "GrpcListenPort";
        private const string LogDirectoryKey = "LogDirectory";

        private static readonly Lazy<ServiceConfiguration> LazyInstance =
            new Lazy<ServiceConfiguration>(Load);

        public static ServiceConfiguration Current => LazyInstance.Value;

        public int GrpcListenPort { get; private set; }

        public string LogDirectory { get; private set; }

        private ServiceConfiguration()
        {
        }

        private static ServiceConfiguration Load()
        {
            var configuration = new ServiceConfiguration();

            configuration.GrpcListenPort = ReadIntSetting(GrpcPortKey, defaultValue: 5001);
            configuration.LogDirectory = ReadStringSetting(LogDirectoryKey,
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs"));

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

        private static int ReadIntSetting(string key, int defaultValue)
        {
            string rawValue = ConfigurationManager.AppSettings[key];
            return int.TryParse(rawValue, out int parsed) ? parsed : defaultValue;
        }

        private static string ReadStringSetting(string key, string defaultValue)
        {
            string rawValue = ConfigurationManager.AppSettings[key];
            return string.IsNullOrWhiteSpace(rawValue) ? defaultValue : rawValue.Trim();
        }
    }
}
