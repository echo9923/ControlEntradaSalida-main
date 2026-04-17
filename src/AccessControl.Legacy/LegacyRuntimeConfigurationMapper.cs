using System;
using ControlEntradaSalida.Application.Abstractions;

namespace ControlEntradaSalida
{
    internal static class LegacyRuntimeConfigurationMapper
    {
        public static ServiceConfiguration.FaceEventOptions ToLegacyOptions(RuntimeFaceEventOptions options)
        {
            if (options == null)
            {
                return new ServiceConfiguration.FaceEventOptions();
            }

            return new ServiceConfiguration.FaceEventOptions
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
                CompensationBufferOverflowPolicy = ToLegacyPolicy(options.CompensationBufferOverflowPolicy),
                CompensationReleaseBatchSize = options.CompensationReleaseBatchSize,
                CompensationTailWindowSeconds = options.CompensationTailWindowSeconds,
                AlarmDeployType = options.AlarmDeployType,
                ExcludedDeviceIds = options.ExcludedDeviceIds ?? Array.Empty<int>(),
                ExcludedDeviceIps = options.ExcludedDeviceIps ?? Array.Empty<string>()
            };
        }

        public static ServiceConfiguration.DeviceConnectionOptions ToLegacyOptions(RuntimeDeviceConnectionOptions options)
        {
            if (options == null)
            {
                return new ServiceConfiguration.DeviceConnectionOptions();
            }

            return new ServiceConfiguration.DeviceConnectionOptions
            {
                StatusCheckIntervalMs = options.StatusCheckIntervalMs,
                StatusCheckSdkLockTimeoutMs = options.StatusCheckSdkLockTimeoutMs,
                DeviceSdkLockTimeoutMs = options.DeviceSdkLockTimeoutMs,
                ConnectTimeoutMs = options.ConnectTimeoutMs,
                DisconnectTimeoutMs = options.DisconnectTimeoutMs,
                MaxConcurrentConnections = options.MaxConcurrentConnections
            };
        }

        public static ServiceConfiguration.ReconnectOptions ToLegacyOptions(RuntimeReconnectOptions options)
        {
            if (options == null)
            {
                return new ServiceConfiguration.ReconnectOptions();
            }

            return new ServiceConfiguration.ReconnectOptions
            {
                MaxReconnectAttempts = options.MaxReconnectAttempts,
                BaseDelayMs = options.BaseDelayMs,
                MaxDelayMs = options.MaxDelayMs,
                PermanentFailureCooldownMs = options.PermanentFailureCooldownMs,
                ReconnectCheckIntervalMs = options.ReconnectCheckIntervalMs
            };
        }

        public static ServiceConfiguration.DeviceOperationRetryOptions ToLegacyOptions(RuntimeDeviceOperationRetryOptions options)
        {
            if (options == null)
            {
                return new ServiceConfiguration.DeviceOperationRetryOptions();
            }

            return new ServiceConfiguration.DeviceOperationRetryOptions
            {
                Enabled = options.Enabled,
                ScanIntervalSeconds = options.ScanIntervalSeconds,
                RetryIntervalSeconds = options.RetryIntervalSeconds,
                MaxRetryAttempts = options.MaxRetryAttempts,
                FailureRetentionDays = options.FailureRetentionDays
            };
        }

        private static ServiceConfiguration.FaceEventBufferOverflowPolicy ToLegacyPolicy(RuntimeFaceEventBufferOverflowPolicy policy)
        {
            switch (policy)
            {
                case RuntimeFaceEventBufferOverflowPolicy.DropOldest:
                    return ServiceConfiguration.FaceEventBufferOverflowPolicy.DropOldest;
                case RuntimeFaceEventBufferOverflowPolicy.FlushDirect:
                    return ServiceConfiguration.FaceEventBufferOverflowPolicy.FlushDirect;
                default:
                    return ServiceConfiguration.FaceEventBufferOverflowPolicy.DropNewest;
            }
        }
    }
}
