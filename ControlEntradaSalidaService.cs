using System;
using System.ServiceProcess;

namespace ControlEntradaSalida
{
    /// <summary>
    /// Windows服务入口，实现设备连接与gRPC监听的生命周期管理。
    /// </summary>
    public sealed class ControlEntradaSalidaService : ServiceBase
    {
        private PermissionUpdateGrpcServer grpcServer;
        private PermissionRefreshManager refreshManager;
        private bool initialized;

        public ControlEntradaSalidaService()
        {
            ServiceName = "ControlEntradaSalidaService";
            CanPauseAndContinue = true;
            CanShutdown = true;
            AutoLog = false;
        }

        protected override void OnStart(string[] args)
        {
            try
            {
                StartCore(args);
                ServiceLogger.Info("服务启动完成。");
            }
            catch (Exception ex)
            {
                ServiceLogger.Error("服务启动失败。", ex);
                throw;
            }
        }

        protected override void OnStop()
        {
            try
            {
                StopCore();
                ServiceLogger.Info("服务已停止。");
            }
            catch (Exception ex)
            {
                ServiceLogger.Error("服务停止过程中出现异常。", ex);
                throw;
            }
        }

        protected override void OnPause()
        {
            try
            {
                DeviceConnectionManager.Instance.SuspendMonitoring();
                ServiceLogger.Info("服务已暂停设备状态监控。");
                base.OnPause();
            }
            catch (Exception ex)
            {
                ServiceLogger.Error("暂停服务时发生异常。", ex);
                throw;
            }
        }

        protected override void OnContinue()
        {
            try
            {
                DeviceConnectionManager.Instance.ResumeMonitoring();
                ServiceLogger.Info("服务继续运行。");
                base.OnContinue();
            }
            catch (Exception ex)
            {
                ServiceLogger.Error("继续服务时发生异常。", ex);
                throw;
            }
        }

        protected override void OnShutdown()
        {
            ServiceLogger.Warn("检测到系统关闭，开始停止服务。");
            OnStop();
            base.OnShutdown();
        }

        public void StartInteractive(string[] args)
        {
            OnStart(args);
        }

        public void StopInteractive()
        {
            OnStop();
        }

        private void StartCore(string[] args)
        {
            if (initialized)
            {
                ServiceLogger.Warn("服务已初始化，跳过重复启动。");
                return;
            }

            var config = ServiceConfiguration.Current;
            ServiceLogger.Initialize(config.LogDirectory);
            ServiceLogger.Info("日志系统初始化完成。");

            if (!Common.InicializarSDKHikVision())
            {
                throw new InvalidOperationException("海康威视SDK初始化失败，请检查SDK环境。");
            }

            if (!Common.CrearDirectorioData())
            {
                ServiceLogger.Warn("数据目录创建失败，将继续运行。");
            }

            DeviceConnectionManager.Instance.ApplyConfiguration(config);
            DeviceConnectionManager.Instance.LoadAllDevices();
            DeviceConnectionManager.Instance.ResumeMonitoring();

            refreshManager = new PermissionRefreshManager();
            grpcServer = new PermissionUpdateGrpcServer(refreshManager);
            grpcServer.Start(config.GrpcListenPort);

            initialized = true;
        }

        private void StopCore()
        {
            DeviceConnectionManager.Instance.SuspendMonitoring();

            try
            {
                grpcServer?.Dispose();
            }
            catch (Exception ex)
            {
                ServiceLogger.Error("停止gRPC服务时出现异常。", ex);
            }
            finally
            {
                grpcServer = null;
            }

            try
            {
                DeviceConnectionManager.Instance.DisconnectAllDevices();
                DeviceConnectionManager.Instance.Dispose();
            }
            catch (Exception ex)
            {
                ServiceLogger.Error("断开设备连接时出现异常。", ex);
            }
            finally
            {
                HCNetSDK.NET_DVR_Cleanup();
            }

            initialized = false;
        }
    }
}
