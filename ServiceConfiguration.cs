using System;
using System.Configuration;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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

        private const int DefaultCompensationRealtimeBufferLimit = 2000;
        private const int DefaultCompensationMaxDurationSeconds = 150;
        private const int DefaultCompensationReleaseBatchSize = 200;
        private const int DefaultCompensationTailWindowSeconds = 10;
        private const FaceEventBufferOverflowPolicy DefaultCompensationBufferOverflowPolicy = FaceEventBufferOverflowPolicy.FlushDirect;

        private const int DefaultStatusCheckIntervalMs = 30000;
        private const int DefaultStatusCheckSdkLockTimeoutMs = 1000;
        private const int DefaultDeviceSdkLockTimeoutMs = 30000;
        private const int DefaultConnectTimeoutMs = 5000;
        private const int DefaultDisconnectTimeoutMs = 5000;
        private const int DefaultMaxConcurrentConnections = 10;

        private const int DefaultReconnectMaxAttempts = 10;
        private const int DefaultReconnectBaseDelayMs = 1000;
        private const int DefaultReconnectMaxDelayMs = 300000;
        private const int DefaultReconnectPermanentFailureCooldownMs = 600000;
        private const int DefaultReconnectCheckIntervalMs = 5000;

        private static readonly Lazy<ServiceConfiguration> LazyInstance =
            new Lazy<ServiceConfiguration>(Load);

        public static ServiceConfiguration Current => LazyInstance.Value;

        public int GrpcListenPort { get; private set; }

        public string LogDirectory { get; private set; }

        public int LogRetentionDays { get; private set; }

        public bool VerboseLogging { get; private set; }

        public bool LogGrpcPayloads { get; private set; }

        public int GrpcPayloadLogMaxChars { get; private set; }

        public FaceEventOptions FaceEvent { get; private set; }

        public DeviceConnectionOptions DeviceConnection { get; private set; }

        public ReconnectOptions Reconnect { get; private set; }

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
            configuration.VerboseLogging = serviceSection?.VerboseLogging ?? false;
            configuration.LogGrpcPayloads = serviceSection?.LogGrpcPayloads ?? false;
            configuration.GrpcPayloadLogMaxChars = ResolvePayloadLogMaxChars(serviceSection?.GrpcPayloadLogMaxChars);
            configuration.FaceEvent = ResolveFaceEventOptions(ExternalConfiguration.Current.FaceEventLogging);
            configuration.DeviceConnection = ResolveDeviceConnectionOptions(ExternalConfiguration.Current.DeviceConnection);
            configuration.Reconnect = ResolveReconnectOptions(ExternalConfiguration.Current.Reconnect);

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

        private static string ReadAppSetting(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                return null;
            }

            try
            {
                return ConfigurationManager.AppSettings[key]?.Trim();
            }
            catch
            {
                return null;
            }
        }

        private static int? ReadAppSettingInt(string key)
        {
            string value = ReadAppSetting(key);
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            return int.TryParse(value, out int result) ? (int?)result : null;
        }

        private static bool? ReadAppSettingBool(string key)
        {
            string value = ReadAppSetting(key);
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            return bool.TryParse(value, out bool result) ? (bool?)result : null;
        }

        private static int[] ReadAppSettingIntArray(string key)
        {
            string value = ReadAppSetting(key);
            if (value == null)
            {
                return null;
            }

            return value
                .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(item => item?.Trim())
                .Where(item => !string.IsNullOrWhiteSpace(item))
                .Select(item => int.TryParse(item, out int parsed) ? parsed : 0)
                .Where(parsed => parsed > 0)
                .Distinct()
                .ToArray();
        }

        private static string[] ReadAppSettingStringArray(string key)
        {
            string value = ReadAppSetting(key);
            if (value == null)
            {
                return null;
            }

            return value
                .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(item => item?.Trim())
                .Where(item => !string.IsNullOrWhiteSpace(item))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }

        private static FaceEventBufferOverflowPolicy? ReadAppSettingOverflowPolicy(string key)
        {
            string value = ReadAppSetting(key);
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            return Enum.TryParse(value, true, out FaceEventBufferOverflowPolicy result)
                ? (FaceEventBufferOverflowPolicy?)result
                : null;
        }

        private static FaceEventBufferOverflowPolicy ResolveOverflowPolicy(
            string configuredValue,
            FaceEventBufferOverflowPolicy defaultValue)
        {
            if (!string.IsNullOrWhiteSpace(configuredValue)
                && Enum.TryParse(configuredValue, true, out FaceEventBufferOverflowPolicy result))
            {
                return result;
            }

            return defaultValue;
        }

        private static FaceEventOptions ResolveFaceEventOptions(ExternalConfiguration.FaceEventLoggingSection section)
        {
            bool? enabledSetting = ReadAppSettingBool("FaceEventLogging.Enabled");
            bool? offlineCompensationEnabledSetting = ReadAppSettingBool("FaceEventLogging.OfflineCompensationEnabled");
            int? queueCapacitySetting = ReadAppSettingInt("FaceEventLogging.QueueCapacity");
            int? batchSizeSetting = ReadAppSettingInt("FaceEventLogging.BatchSize");
            int? retryIntervalSetting = ReadAppSettingInt("FaceEventLogging.RetryIntervalSeconds");
            int? shutdownFlushSetting = ReadAppSettingInt("FaceEventLogging.ShutdownFlushTimeoutSeconds");
            int? lookbackMinutesSetting = ReadAppSettingInt("FaceEventLogging.CompensationLookbackMinutes");
            int? alarmDeploySetting = ReadAppSettingInt("FaceEventLogging.AlarmDeployType");

            int? bufferLimitSetting = ReadAppSettingInt("FaceEventLogging.CompensationRealtimeBufferLimit");
            int? maxDurationSetting = ReadAppSettingInt("FaceEventLogging.CompensationMaxDurationSeconds");
            int? releaseBatchSetting = ReadAppSettingInt("FaceEventLogging.CompensationReleaseBatchSize");
            int? tailWindowSetting = ReadAppSettingInt("FaceEventLogging.CompensationTailWindowSeconds");
            FaceEventBufferOverflowPolicy? overflowPolicySetting =
                ReadAppSettingOverflowPolicy("FaceEventLogging.CompensationBufferOverflowPolicy");

            int[] rawExcludedDeviceIds =
                ReadAppSettingIntArray("FaceEventLogging.ExcludedDeviceIds")
                ?? (section?.ExcludedDeviceIds ?? Array.Empty<int>());

            string[] rawExcludedDeviceIps =
                ReadAppSettingStringArray("FaceEventLogging.ExcludedDeviceIps")
                ?? (section?.ExcludedDeviceIps ?? Array.Empty<string>());

            int[] excludedDeviceIds = rawExcludedDeviceIds
                .Where(deviceId => deviceId > 0)
                .Distinct()
                .ToArray();

            string[] excludedDeviceIps = rawExcludedDeviceIps
                .Select(ip => ip?.Trim())
                .Where(ip => !string.IsNullOrWhiteSpace(ip))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();

            int alarmDeployTypeValue = alarmDeploySetting
                ?? section?.AlarmDeployType
                ?? 0;

            FaceEventBufferOverflowPolicy overflowPolicy = overflowPolicySetting
                ?? ResolveOverflowPolicy(section?.CompensationBufferOverflowPolicy, DefaultCompensationBufferOverflowPolicy);

            var options = new FaceEventOptions
            {
                Enabled = enabledSetting ?? section?.Enabled ?? false,
                OfflineCompensationEnabled = offlineCompensationEnabledSetting ?? section?.OfflineCompensationEnabled ?? true,
                QueueCapacity = ResolveIntInRange(queueCapacitySetting ?? section?.QueueCapacity, 2000, minValue: 100, maxValue: 200000),
                BatchSize = ResolveIntInRange(batchSizeSetting ?? section?.BatchSize, 20, minValue: 1, maxValue: 2000),
                RetryIntervalSeconds = ResolveIntInRange(retryIntervalSetting ?? section?.RetryIntervalSeconds, 5, minValue: 1, maxValue: 600),
                ShutdownFlushTimeoutSeconds = ResolveIntInRange(
                    shutdownFlushSetting ?? section?.ShutdownFlushTimeoutSeconds,
                    30,
                    minValue: 1,
                    maxValue: 600),
                CompensationLookbackMinutes = ResolveIntInRange(
                    lookbackMinutesSetting ?? section?.CompensationLookbackMinutes,
                    60,
                    minValue: 1,
                    maxValue: 1440),
                AlarmDeployType = (byte)(alarmDeployTypeValue == 1 ? 1 : 0),
                CompensationRealtimeBufferLimit = ResolveIntInRange(
                    bufferLimitSetting ?? section?.CompensationRealtimeBufferLimit,
                    DefaultCompensationRealtimeBufferLimit,
                    minValue: 100,
                    maxValue: 200000),
                CompensationMaxDurationSeconds = ResolveIntInRange(
                    maxDurationSetting ?? section?.CompensationMaxDurationSeconds,
                    DefaultCompensationMaxDurationSeconds,
                    minValue: 1,
                    maxValue: 3600),
                CompensationReleaseBatchSize = ResolveIntInRange(
                    releaseBatchSetting ?? section?.CompensationReleaseBatchSize,
                    DefaultCompensationReleaseBatchSize,
                    minValue: 1,
                    maxValue: 5000),
                CompensationTailWindowSeconds = ResolveIntInRange(
                    tailWindowSetting ?? section?.CompensationTailWindowSeconds,
                    DefaultCompensationTailWindowSeconds,
                    minValue: 0,
                    maxValue: 300),
                CompensationBufferOverflowPolicy = overflowPolicy,
                ExcludedDeviceIds = excludedDeviceIds,
                ExcludedDeviceIps = excludedDeviceIps
            };

            return options;
        }

        private static DeviceConnectionOptions ResolveDeviceConnectionOptions(ExternalConfiguration.DeviceConnectionSection section)
        {
            var options = new DeviceConnectionOptions
            {
                StatusCheckIntervalMs = ResolveIntInRange(
                    section?.StatusCheckIntervalMs,
                    DefaultStatusCheckIntervalMs,
                    minValue: 5000,
                    maxValue: 600000),
                StatusCheckSdkLockTimeoutMs = ResolveIntInRange(
                    section?.StatusCheckSdkLockTimeoutMs,
                    DefaultStatusCheckSdkLockTimeoutMs,
                    minValue: 1,
                    maxValue: 10000),
                DeviceSdkLockTimeoutMs = ResolveIntInRange(
                    section?.DeviceSdkLockTimeoutMs,
                    DefaultDeviceSdkLockTimeoutMs,
                    minValue: 1000,
                    maxValue: 120000),
                ConnectTimeoutMs = ResolveIntInRange(
                    section?.ConnectTimeoutMs,
                    DefaultConnectTimeoutMs,
                    minValue: 100,
                    maxValue: 60000),
                DisconnectTimeoutMs = ResolveIntInRange(
                    section?.DisconnectTimeoutMs,
                    DefaultDisconnectTimeoutMs,
                    minValue: 100,
                    maxValue: 60000),
                MaxConcurrentConnections = ResolveIntInRange(
                    section?.MaxConcurrentConnections,
                    DefaultMaxConcurrentConnections,
                    minValue: 1,
                    maxValue: 128)
            };

            return options;
        }

        private static ReconnectOptions ResolveReconnectOptions(ExternalConfiguration.ReconnectSection section)
        {
            int baseDelayMs = ResolveIntInRange(
                section?.BaseDelayMs,
                DefaultReconnectBaseDelayMs,
                minValue: 100,
                maxValue: 600000);

            int maxDelayMs = section?.MaxDelayMs.HasValue == true
                ? Clamp(section.MaxDelayMs.Value, baseDelayMs, 3600000)
                : Math.Max(DefaultReconnectMaxDelayMs, baseDelayMs);

            var options = new ReconnectOptions
            {
                MaxReconnectAttempts = ResolveIntInRange(
                    section?.MaxReconnectAttempts,
                    DefaultReconnectMaxAttempts,
                    minValue: 1,
                    maxValue: 100),
                BaseDelayMs = baseDelayMs,
                MaxDelayMs = maxDelayMs,
                PermanentFailureCooldownMs = ResolveIntInRange(
                    section?.PermanentFailureCooldownMs,
                    DefaultReconnectPermanentFailureCooldownMs,
                    minValue: 0,
                    maxValue: 86400000),
                ReconnectCheckIntervalMs = ResolveIntInRange(
                    section?.ReconnectCheckIntervalMs,
                    DefaultReconnectCheckIntervalMs,
                    minValue: 500,
                    maxValue: 60000)
            };

            return options;
        }

        private static int ResolveIntInRange(int? configuredValue, int defaultValue, int minValue, int maxValue)
        {
            if (!configuredValue.HasValue)
            {
                return defaultValue;
            }

            return Clamp(configuredValue.Value, minValue, maxValue);
        }

        private static int Clamp(int value, int minValue, int maxValue)
        {
            if (value < minValue)
            {
                return minValue;
            }

            if (value > maxValue)
            {
                return maxValue;
            }

            return value;
        }

        public enum FaceEventBufferOverflowPolicy
        {
            DropOldest,
            DropNewest,
            FlushDirect
        }

        public sealed class FaceEventOptions
        {
            public bool Enabled { get; set; }

            public bool OfflineCompensationEnabled { get; set; }

            public int QueueCapacity { get; set; }

            public int BatchSize { get; set; }

            public int RetryIntervalSeconds { get; set; }

            public int ShutdownFlushTimeoutSeconds { get; set; }

            public int CompensationLookbackMinutes { get; set; }

            public int CompensationRealtimeBufferLimit { get; set; }

            public int CompensationMaxDurationSeconds { get; set; }

            public FaceEventBufferOverflowPolicy CompensationBufferOverflowPolicy { get; set; }

            public int CompensationReleaseBatchSize { get; set; }

            public int CompensationTailWindowSeconds { get; set; }

            public byte AlarmDeployType { get; set; }

            public IReadOnlyCollection<int> ExcludedDeviceIds { get; set; } = Array.Empty<int>();

            public IReadOnlyCollection<string> ExcludedDeviceIps { get; set; } = Array.Empty<string>();
        }

        public sealed class DeviceConnectionOptions
        {
            public int StatusCheckIntervalMs { get; set; }

            public int StatusCheckSdkLockTimeoutMs { get; set; }

            public int DeviceSdkLockTimeoutMs { get; set; }

            public int ConnectTimeoutMs { get; set; }

            public int DisconnectTimeoutMs { get; set; }

            public int MaxConcurrentConnections { get; set; }
        }

        public sealed class ReconnectOptions
        {
            public int MaxReconnectAttempts { get; set; }

            public int BaseDelayMs { get; set; }

            public int MaxDelayMs { get; set; }

            public int PermanentFailureCooldownMs { get; set; }

            public int ReconnectCheckIntervalMs { get; set; }
        }
    }
}
