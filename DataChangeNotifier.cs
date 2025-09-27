using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ControlEntradaSalida
{
    /// <summary>
    /// 数据变更事件参数基类
    /// </summary>
    public abstract class DataChangeEventArgs : EventArgs
    {
        public DateTime Timestamp { get; }
        public string Source { get; }

        protected DataChangeEventArgs(string source)
        {
            Timestamp = DateTime.Now;
            Source = source;
        }
    }

    /// <summary>
    /// 设备数据变更事件参数
    /// </summary>
    public class DeviceDataChangedEventArgs : DataChangeEventArgs
    {
        public string DeviceId { get; }
        public string DeviceInfo { get; }
        public DeviceChangeType ChangeType { get; }

        public DeviceDataChangedEventArgs(string deviceId, string deviceInfo,
            DeviceChangeType changeType, string source) : base(source)
        {
            DeviceId = deviceId;
            DeviceInfo = deviceInfo;
            ChangeType = changeType;
        }
    }

    /// <summary>
    /// 员工数据变更事件参数
    /// </summary>
    public class EmployeeDataChangedEventArgs : DataChangeEventArgs
    {
        public string EmployeeId { get; }
        public string EmployeeName { get; }
        public EmployeeChangeType ChangeType { get; }

        public EmployeeDataChangedEventArgs(string employeeId, string employeeName,
            EmployeeChangeType changeType, string source) : base(source)
        {
            EmployeeId = employeeId;
            EmployeeName = employeeName;
            ChangeType = changeType;
        }
    }

    /// <summary>
    /// 设备变更类型枚举
    /// </summary>
    public enum DeviceChangeType
    {
        Added,
        Updated,
        Deleted,
        StatusChanged
    }

    /// <summary>
    /// 员工变更类型枚举
    /// </summary>
    public enum EmployeeChangeType
    {
        Added,
        Updated,
        Deleted,
        PermissionChanged
    }

    /// <summary>
    /// 门状态枚举
    /// </summary>
    public enum DoorStatus
    {
        Closed,        // 关闭
        Opened,        // 开启
        AlwaysOpen,    // 常开
        AlwaysClosed   // 常闭
    }

    /// <summary>
    /// 数据变更通知器 - 全局事件总线
    /// 使用单例模式确保整个应用程序共享同一个事件总线实例
    /// </summary>
    public sealed class DataChangeNotifier
    {
        private static readonly Lazy<DataChangeNotifier> _instance =
            new Lazy<DataChangeNotifier>(() => new DataChangeNotifier());

        public static DataChangeNotifier Instance => _instance.Value;

        // 设备数据变更事件
        public event EventHandler<DeviceDataChangedEventArgs> DeviceDataChanged;

        // 员工数据变更事件
        public event EventHandler<EmployeeDataChangedEventArgs> EmployeeDataChanged;

        private readonly object _eventLock = new object();

        private DataChangeNotifier() { }

        /// <summary>
        /// 通知设备数据变更
        /// </summary>
        /// <param name="deviceId">设备ID</param>
        /// <param name="deviceInfo">设备信息</param>
        /// <param name="changeType">变更类型</param>
        /// <param name="source">变更来源（调用者类名）</param>
        public void NotifyDeviceDataChanged(string deviceId, string deviceInfo,
            DeviceChangeType changeType, string source)
        {
            lock (_eventLock)
            {
                try
                {
                    DeviceDataChanged?.Invoke(this,
                        new DeviceDataChangedEventArgs(deviceId, deviceInfo, changeType, source));
                }
                catch (Exception ex)
                {
                    // 记录异常但不中断程序运行
                    System.Diagnostics.Debug.WriteLine($"设备数据变更通知异常: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// 通知员工数据变更
        /// </summary>
        /// <param name="employeeId">员工ID</param>
        /// <param name="employeeName">员工姓名</param>
        /// <param name="changeType">变更类型</param>
        /// <param name="source">变更来源（调用者类名）</param>
        public void NotifyEmployeeDataChanged(string employeeId, string employeeName,
            EmployeeChangeType changeType, string source)
        {
            lock (_eventLock)
            {
                try
                {
                    EmployeeDataChanged?.Invoke(this,
                        new EmployeeDataChangedEventArgs(employeeId, employeeName, changeType, source));
                }
                catch (Exception ex)
                {
                    // 记录异常但不中断程序运行
                    System.Diagnostics.Debug.WriteLine($"员工数据变更通知异常: {ex.Message}");
                }
            }
        }
    }
}