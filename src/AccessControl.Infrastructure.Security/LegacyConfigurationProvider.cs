using ControlEntradaSalida.Application.Abstractions;

namespace ControlEntradaSalida.Infrastructure.Security
{
    public sealed class LegacyConfigurationProvider : IConfigurationProvider
    {
        public RuntimeServiceConfiguration Current => Map(global::ControlEntradaSalida.ServiceConfiguration.Current);

        private static RuntimeServiceConfiguration Map(global::ControlEntradaSalida.ServiceConfiguration configuration)
        {
            return new RuntimeServiceConfiguration
            {
                GrpcListenPort = configuration.GrpcListenPort,
                LogDirectory = configuration.LogDirectory,
                LogRetentionDays = configuration.LogRetentionDays,
                VerboseLogging = configuration.VerboseLogging,
                LogGrpcPayloads = configuration.LogGrpcPayloads,
                GrpcPayloadLogMaxChars = configuration.GrpcPayloadLogMaxChars,
                GrpcManagementApiKey = configuration.GrpcManagementApiKey,
                FaceEvent = Map(configuration.FaceEvent),
                DeviceConnection = Map(configuration.DeviceConnection),
                Reconnect = Map(configuration.Reconnect),
                DeviceOperationRetry = Map(configuration.DeviceOperationRetry)
            };
        }

        private static RuntimeFaceEventOptions Map(global::ControlEntradaSalida.ServiceConfiguration.FaceEventOptions options)
        {
            if (options == null)
            {
                return new RuntimeFaceEventOptions();
            }

            return new RuntimeFaceEventOptions
            {
                Enabled = options.Enabled,
                SnapshotRootDirectory = options.SnapshotRootDirectory,
                OfflineCompensationEnabled = options.OfflineCompensationEnabled,
                QueueCapacity = options.QueueCapacity,
                BatchSize = options.BatchSize,
                RetryIntervalSeconds = options.RetryIntervalSeconds,
                ShutdownFlushTimeoutSeconds = options.ShutdownFlushTimeoutSeconds,
                CompensationLookbackMinutes = options.CompensationLookbackMinutes,
                CompensationRealtimeBufferLimit = options.CompensationRealtimeBufferLimit,
                CompensationMaxDurationSeconds = options.CompensationMaxDurationSeconds,
                CompensationBufferOverflowPolicy = Map(options.CompensationBufferOverflowPolicy),
                CompensationReleaseBatchSize = options.CompensationReleaseBatchSize,
                CompensationTailWindowSeconds = options.CompensationTailWindowSeconds,
                AlarmDeployType = options.AlarmDeployType,
                ExcludedDeviceIds = options.ExcludedDeviceIds,
                ExcludedDeviceIps = options.ExcludedDeviceIps
            };
        }

        private static RuntimeDeviceConnectionOptions Map(global::ControlEntradaSalida.ServiceConfiguration.DeviceConnectionOptions options)
        {
            if (options == null)
            {
                return new RuntimeDeviceConnectionOptions();
            }

            return new RuntimeDeviceConnectionOptions
            {
                StatusCheckIntervalMs = options.StatusCheckIntervalMs,
                StatusCheckSdkLockTimeoutMs = options.StatusCheckSdkLockTimeoutMs,
                DeviceSdkLockTimeoutMs = options.DeviceSdkLockTimeoutMs,
                ConnectTimeoutMs = options.ConnectTimeoutMs,
                DisconnectTimeoutMs = options.DisconnectTimeoutMs,
                MaxConcurrentConnections = options.MaxConcurrentConnections
            };
        }

        private static RuntimeReconnectOptions Map(global::ControlEntradaSalida.ServiceConfiguration.ReconnectOptions options)
        {
            if (options == null)
            {
                return new RuntimeReconnectOptions();
            }

            return new RuntimeReconnectOptions
            {
                MaxReconnectAttempts = options.MaxReconnectAttempts,
                BaseDelayMs = options.BaseDelayMs,
                MaxDelayMs = options.MaxDelayMs,
                PermanentFailureCooldownMs = options.PermanentFailureCooldownMs,
                ReconnectCheckIntervalMs = options.ReconnectCheckIntervalMs
            };
        }

        private static RuntimeDeviceOperationRetryOptions Map(global::ControlEntradaSalida.ServiceConfiguration.DeviceOperationRetryOptions options)
        {
            if (options == null)
            {
                return new RuntimeDeviceOperationRetryOptions();
            }

            return new RuntimeDeviceOperationRetryOptions
            {
                Enabled = options.Enabled,
                ScanIntervalSeconds = options.ScanIntervalSeconds,
                RetryIntervalSeconds = options.RetryIntervalSeconds,
                MaxRetryAttempts = options.MaxRetryAttempts,
                FailureRetentionDays = options.FailureRetentionDays
            };
        }

        private static RuntimeFaceEventBufferOverflowPolicy Map(global::ControlEntradaSalida.ServiceConfiguration.FaceEventBufferOverflowPolicy policy)
        {
            switch (policy)
            {
                case global::ControlEntradaSalida.ServiceConfiguration.FaceEventBufferOverflowPolicy.DropOldest:
                    return RuntimeFaceEventBufferOverflowPolicy.DropOldest;
                case global::ControlEntradaSalida.ServiceConfiguration.FaceEventBufferOverflowPolicy.FlushDirect:
                    return RuntimeFaceEventBufferOverflowPolicy.FlushDirect;
                default:
                    return RuntimeFaceEventBufferOverflowPolicy.DropNewest;
            }
        }
    }
}
