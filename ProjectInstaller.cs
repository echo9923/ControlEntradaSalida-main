using System.ComponentModel;
using System.Configuration.Install;
using System.ServiceProcess;

namespace ControlEntradaSalida
{
    /// <summary>
    /// Windows服务安装程序定义，支持InstallUtil或PowerShell进行安装与卸载。
    /// </summary>
    [RunInstaller(true)]
    public sealed class ProjectInstaller : Installer
    {
        private readonly ServiceProcessInstaller processInstaller;
        private readonly ServiceInstaller serviceInstaller;

        public ProjectInstaller()
        {
            processInstaller = new ServiceProcessInstaller
            {
                Account = ServiceAccount.LocalSystem
            };

            serviceInstaller = new ServiceInstaller
            {
                ServiceName = "ControlEntradaSalidaService",
                DisplayName = "Control Entrada Salida Service",
                Description = "门禁设备连接与权限同步服务。",
                StartType = ServiceStartMode.Automatic
            };

            Installers.Add(processInstaller);
            Installers.Add(serviceInstaller);
        }
    }
}
