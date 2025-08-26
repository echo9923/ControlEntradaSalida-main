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
        /// 刷新员工数据
        /// </summary>
        void RefreshEmployeeData();

        /// <summary>
        /// 刷新门状态
        /// </summary>
        /// <param name="deviceId">设备ID</param>
        /// <param name="status">门状态</param>
        void RefreshDoorStatus(string deviceId, DoorStatus status);

        /// <summary>
        /// 窗体是否可见
        /// 用于判断是否需要刷新（性能优化）
        /// </summary>
        bool IsFormVisible { get; }
    }
}