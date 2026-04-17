using System;

namespace ControlEntradaSalida
{
    internal static class Program
    {
        private static void Main(string[] args)
        {
            if (Host.WindowsService.HostModeResolver.Resolve(Environment.UserInteractive, args) == Host.WindowsService.HostRunMode.Interactive)
            {
                Console.Title = "ControlEntradaSalida 门禁服务（重构宿主）";
                using (var service = new ControlEntradaSalidaWindowsService())
                {
                    service.StartInteractive(args);
                    Console.WriteLine("服务已在交互模式下启动，按任意键停止...");
                    Console.ReadKey();
                    service.StopInteractive();
                }
            }
            else
            {
                System.ServiceProcess.ServiceBase.Run(new ControlEntradaSalidaWindowsService());
            }
        }
    }
}
