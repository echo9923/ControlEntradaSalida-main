using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ControlEntradaSalida
{
    /// <summary>
    /// 可刷新窗体接口
    /// 定义了支持自动数据刷新的窗体必须实现的方法
    /// </summary>
    public interface IRefreshableForm
    {
        /// <summary>
        /// 刷新设备数据
        /// </summary>
        void RefreshDeviceData();


        /// <summary>
        /// 窗体是否可见
        /// 用于判断是否需要刷新（性能优化）
        /// </summary>
        bool IsFormVisible { get; }
    }
}