using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ControlEntradaSalida
{
    // 测试修复的类
    public class TestFix
    {
        public static void Main(string[] args)
        {
            Console.WriteLine("测试修复是否有效");
            
            // 测试设备管理器初始化
            var deviceManager = DeviceConnectionManager.Instance;
            Console.WriteLine("设备管理器初始化成功");
            
            // 测试数据变更通知器
            var notifier = DataChangeNotifier.Instance;
            Console.WriteLine("数据变更通知器初始化成功");
            
            Console.WriteLine("所有测试通过!");
            Console.ReadKey();
        }
    }
}