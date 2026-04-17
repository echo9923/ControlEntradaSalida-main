using System;
using ControlEntradaSalida.Application.Abstractions;

namespace ControlEntradaSalida
{
    public sealed class LegacyRuntimeBridge
    {
        public bool InitializeSdk()
        {
            return Common.InicializarSDKHikVision();
        }

        public void EnsureDataDirectory()
        {
            Common.CrearDirectorioData();
        }

        public FaceEventService CreateFaceEventService(RuntimeServiceConfiguration configuration)
        {
            return new FaceEventService(configuration);
        }

        public void ApplyDeviceConfiguration(DeviceConnectionManager deviceManager, RuntimeServiceConfiguration configuration)
        {
            if (deviceManager == null)
            {
                throw new ArgumentNullException(nameof(deviceManager));
            }

            deviceManager.ApplyConfiguration(configuration);
        }

        public DeviceOperationRetryManager CreateRetryManager(
            RuntimeServiceConfiguration configuration,
            DeviceOperationRetryStore retryStore,
            PermissionRefreshManager permissionManager)
        {
            if (configuration == null)
            {
                throw new ArgumentNullException(nameof(configuration));
            }

            return new DeviceOperationRetryManager(configuration.DeviceOperationRetry, retryStore, permissionManager);
        }
    }
}
