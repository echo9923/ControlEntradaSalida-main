using System;
using System.Diagnostics;
using System.Windows.Forms;

namespace ControlEntradaSalida
{
    static class Program
    {
        private static PermissionUpdateGrpcServer permissionServer;

        /// <summary>
        /// 应用程序的主要入口点
        /// </summary>
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            Application.ApplicationExit += OnApplicationExit;
            InitializeBackgroundServices();

            Application.Run(new MDIParent());
        }

        private static void InitializeBackgroundServices()
        {
            try
            {
                PermissionRefreshManager refreshManager = new PermissionRefreshManager();
                permissionServer = new PermissionUpdateGrpcServer(refreshManager);
                permissionServer.Start();
            }
            catch (Exception ex)
            {
                Trace.TraceError($"启动权限GRPC服务失败: {ex}");
            }
        }

        private static void OnApplicationExit(object sender, EventArgs e)
        {
            if (permissionServer == null)
            {
                return;
            }

            try
            {
                permissionServer.Dispose();
            }
            catch (Exception ex)
            {
                Trace.TraceError($"停止权限GRPC服务失败: {ex}");
            }
            finally
            {
                permissionServer = null;
            }
        }
    }
}
