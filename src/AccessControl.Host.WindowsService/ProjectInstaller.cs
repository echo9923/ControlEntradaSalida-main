using System.ComponentModel;
using System.Configuration.Install;
using System.ServiceProcess;

namespace ControlEntradaSalida
{
    [RunInstaller(true)]
    public sealed class ProjectInstaller : Installer
    {
        public ProjectInstaller()
        {
            Installers.Add(new ServiceProcessInstaller
            {
                Account = ServiceAccount.LocalSystem
            });

            Installers.Add(new ServiceInstaller
            {
                ServiceName = "ControlEntradaSalidaService",
                DisplayName = "Control Entrada Salida Service",
                Description = "门禁设备连接与权限同步服务。",
                StartType = ServiceStartMode.Automatic
            });
        }
    }
}
