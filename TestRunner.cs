using System;
using System.Threading.Tasks;

namespace ControlEntradaSalida
{
    /// <summary>
    /// 测试运行器 - 用于验证设备状态监控功能
    /// </summary>
    public class TestRunner
    {
        /// <summary>
        /// 运行设备状态监控测试
        /// 这可以在开发环境中独立调用来验证功能
        /// </summary>
        public static async Task RunDeviceStatusTests()
        {
            try
            {
                var tester = new DeviceStatusTest();
                await tester.RunAllTests();
                
                // 额外测试模拟SDK数据
                tester.TestWithSimulatedSDKData();
                
                Console.WriteLine("\n========================================");
                Console.WriteLine("✓ 所有测试执行完成");
                Console.WriteLine("设备状态监控功能已准备就绪");
                Console.WriteLine("========================================");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\n❌ 测试执行出错: {ex.Message}");
                Console.WriteLine($"堆栈跟踪: {ex.StackTrace}");
            }
        }

        /// <summary>
        /// 简单的控制台测试入口（可选）
        /// 注意：在实际项目中不需要这个Main方法
        /// </summary>
#if DEBUG
        public static async Task Main(string[] args)
        {
            Console.WriteLine("设备状态监控功能测试");
            Console.WriteLine("====================");
            
            await RunDeviceStatusTests();
            
            Console.WriteLine("\n按任意键退出...");
            Console.ReadKey();
        }
#endif
    }
}