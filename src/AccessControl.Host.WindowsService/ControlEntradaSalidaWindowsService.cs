using System.ServiceProcess;
using ControlEntradaSalida.Host.WindowsService;

namespace ControlEntradaSalida
{
    public sealed class ControlEntradaSalidaWindowsService : ServiceBase
    {
        private CompositeServiceRuntime runtime;

        public ControlEntradaSalidaWindowsService()
        {
            ServiceName = "ControlEntradaSalidaService";
            CanPauseAndContinue = true;
            CanShutdown = true;
            AutoLog = false;
        }

        protected override void OnStart(string[] args)
        {
            runtime = CompositionRoot.CreateRuntime();
            runtime.Start();
        }

        protected override void OnStop()
        {
            runtime?.Dispose();
            runtime = null;
        }

        protected override void OnShutdown()
        {
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
    }
}
