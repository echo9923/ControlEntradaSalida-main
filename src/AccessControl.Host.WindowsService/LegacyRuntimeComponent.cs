using System;
using ControlEntradaSalida.Application.Abstractions;

namespace ControlEntradaSalida.Host.WindowsService
{
    public sealed class LegacyRuntimeComponent : IHostedComponent
    {
        private readonly RuntimeServiceConfiguration configuration;
        private readonly global::ControlEntradaSalida.PermissionRefreshManager permissionManager;
        private readonly global::ControlEntradaSalida.DeviceOperationRetryStore retryStore;
        private readonly global::ControlEntradaSalida.LegacyRuntimeBridge runtimeBridge;
        private readonly ILoggerFacade logger;

        private global::ControlEntradaSalida.DeviceOperationRetryManager retryManager;
        private global::ControlEntradaSalida.FaceEventService faceEventService;

        public LegacyRuntimeComponent(
            RuntimeServiceConfiguration configuration,
            global::ControlEntradaSalida.PermissionRefreshManager permissionManager,
            global::ControlEntradaSalida.DeviceOperationRetryStore retryStore,
            global::ControlEntradaSalida.LegacyRuntimeBridge runtimeBridge,
            ILoggerFacade logger)
        {
            this.configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            this.permissionManager = permissionManager ?? throw new ArgumentNullException(nameof(permissionManager));
            this.retryStore = retryStore ?? throw new ArgumentNullException(nameof(retryStore));
            this.runtimeBridge = runtimeBridge ?? throw new ArgumentNullException(nameof(runtimeBridge));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public void Start()
        {
            global::ControlEntradaSalida.ServiceLogger.Initialize(configuration.LogDirectory, configuration.LogRetentionDays, configuration.VerboseLogging);
            logger.Info("鏂板涓诲紑濮嬪垵濮嬪寲 Legacy 杩愯鏃躲€?");

            if (!runtimeBridge.InitializeSdk())
            {
                throw new InvalidOperationException("娴峰悍濞佽SDK鍒濆鍖栧け璐ワ紝璇锋鏌DK鐜銆?");
            }

            runtimeBridge.EnsureDataDirectory();

            faceEventService = runtimeBridge.CreateFaceEventService(configuration);
            faceEventService.Start();

            runtimeBridge.ApplyDeviceConfiguration(global::ControlEntradaSalida.DeviceConnectionManager.Instance, configuration);
            global::ControlEntradaSalida.DeviceConnectionManager.Instance.LoadAllDevices();
            global::ControlEntradaSalida.DeviceConnectionManager.Instance.ResumeMonitoring();

            retryManager = runtimeBridge.CreateRetryManager(configuration, retryStore, permissionManager);
            retryManager.Start();
        }

        public void Stop()
        {
            try
            {
                global::ControlEntradaSalida.DeviceConnectionManager.Instance.SuspendMonitoring();
            }
            catch
            {
            }

            try
            {
                retryManager?.Dispose();
            }
            catch
            {
            }
            finally
            {
                retryManager = null;
            }

            try
            {
                faceEventService?.Dispose();
            }
            catch
            {
            }
            finally
            {
                faceEventService = null;
            }

            try
            {
                global::ControlEntradaSalida.DeviceConnectionManager.Instance.DisconnectAllDevices();
                global::ControlEntradaSalida.DeviceConnectionManager.Instance.Dispose();
            }
            catch
            {
            }
            finally
            {
                global::ControlEntradaSalida.HCNetSDK.NET_DVR_Cleanup();
            }
        }
    }
}
