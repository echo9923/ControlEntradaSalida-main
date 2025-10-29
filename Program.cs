using System;
using System.ServiceProcess;

namespace ControlEntradaSalida
{
    internal static class Program
    {
        /// <summary>
        /// 应用程序入口点，支持服务模式与交互式调试模式。
        /// </summary>
        private static void Main(string[] args)
        {
            if (Environment.UserInteractive || Array.Exists(args, a => string.Equals(a, "--console", StringComparison.OrdinalIgnoreCase)))
            {
                RunAsConsole(args);
            }
            else
            {
                RunAsService();
            }
        }

        private static void RunAsService()
        {
            ServiceBase.Run(new ControlEntradaSalidaService());
        }

        private static void RunAsConsole(string[] args)
        {
            Console.Title = "ControlEntradaSalida 门禁服务（调试模式）";
            using (var service = new ControlEntradaSalidaService())
            {
                service.StartInteractive(args);
                Console.WriteLine("服务已在交互模式下启动，按任意键停止...");
                Console.ReadKey();
                service.StopInteractive();
            }
        }
    }
}
