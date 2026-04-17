using System;
using System.Collections.Generic;

namespace ControlEntradaSalida.Application.Abstractions
{
    public sealed class RuntimeServiceConfiguration
    {
        public int GrpcListenPort { get; set; }

        public string LogDirectory { get; set; }

        public int LogRetentionDays { get; set; }

        public bool VerboseLogging { get; set; }

        public bool LogGrpcPayloads { get; set; }

        public int GrpcPayloadLogMaxChars { get; set; }

        public string GrpcManagementApiKey { get; set; }

        public RuntimeFaceEventOptions FaceEvent { get; set; } = new RuntimeFaceEventOptions();

        public RuntimeDeviceConnectionOptions DeviceConnection { get; set; } = new RuntimeDeviceConnectionOptions();

        public RuntimeReconnectOptions Reconnect { get; set; } = new RuntimeReconnectOptions();

        public RuntimeDeviceOperationRetryOptions DeviceOperationRetry { get; set; } = new RuntimeDeviceOperationRetryOptions();
    }

    public enum RuntimeFaceEventBufferOverflowPolicy
    {
        DropNewest = 0,
        DropOldest = 1,
        FlushDirect = 2
    }

    public sealed class RuntimeFaceEventOptions
    {
        public bool Enabled { get; set; }

        public string SnapshotRootDirectory { get; set; }

        public bool OfflineCompensationEnabled { get; set; }

        public int QueueCapacity { get; set; }

        public int BatchSize { get; set; }

        public int RetryIntervalSeconds { get; set; }

        public int ShutdownFlushTimeoutSeconds { get; set; }

        public int CompensationLookbackMinutes { get; set; }

        public int CompensationRealtimeBufferLimit { get; set; }

        public int CompensationMaxDurationSeconds { get; set; }

        public RuntimeFaceEventBufferOverflowPolicy CompensationBufferOverflowPolicy { get; set; }

        public int CompensationReleaseBatchSize { get; set; }

        public int CompensationTailWindowSeconds { get; set; }

        public byte AlarmDeployType { get; set; }

        public IReadOnlyCollection<int> ExcludedDeviceIds { get; set; } = Array.Empty<int>();

        public IReadOnlyCollection<string> ExcludedDeviceIps { get; set; } = Array.Empty<string>();
    }

    public sealed class RuntimeDeviceConnectionOptions
    {
        public int StatusCheckIntervalMs { get; set; }

        public int StatusCheckSdkLockTimeoutMs { get; set; }

        public int DeviceSdkLockTimeoutMs { get; set; }

        public int ConnectTimeoutMs { get; set; }

        public int DisconnectTimeoutMs { get; set; }

        public int MaxConcurrentConnections { get; set; }
    }

    public sealed class RuntimeReconnectOptions
    {
        public int MaxReconnectAttempts { get; set; }

        public int BaseDelayMs { get; set; }

        public int MaxDelayMs { get; set; }

        public int PermanentFailureCooldownMs { get; set; }

        public int ReconnectCheckIntervalMs { get; set; }
    }

    public sealed class RuntimeDeviceOperationRetryOptions
    {
        public bool Enabled { get; set; }

        public int ScanIntervalSeconds { get; set; }

        public int RetryIntervalSeconds { get; set; }

        public int MaxRetryAttempts { get; set; }

        public int FailureRetentionDays { get; set; }
    }
}
