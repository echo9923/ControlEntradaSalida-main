using System;
using System.Collections.Generic;

namespace ControlEntradaSalida
{
    /// <summary>
    /// 设备状态信息类
    /// 封装从海康威视SDK获取的设备详细状态信息
    /// </summary>
    public class DeviceStatusInfo
    {
        /// <summary>
        /// 设备ID
        /// </summary>
        public int DeviceId { get; set; }

        /// <summary>
        /// 设备是否在线
        /// </summary>
        public bool IsOnline { get; set; }

        /// <summary>
        /// 门状态
        /// </summary>
        public DoorStatus DoorStatus { get; set; }

        /// <summary>
        /// 门锁状态（继电器状态）
        /// </summary>
        public DoorLockStatus DoorLockStatus { get; set; }

        /// <summary>
        /// 磁力锁状态
        /// </summary>
        public MagneticLockStatus MagneticLockStatus { get; set; }

        /// <summary>
        /// 读卡器在线状态数组
        /// </summary>
        public bool[] CardReaderOnlineStatus { get; set; }

        /// <summary>
        /// 读卡器验证模式数组
        /// </summary>
        public CardReaderVerifyMode[] CardReaderVerifyModes { get; set; }

        /// <summary>
        /// 读卡器防拆状态数组
        /// </summary>
        public bool[] CardReaderAntiDismantleStatus { get; set; }

        /// <summary>
        /// 电池电压（单位：V，实际值需要除以10）
        /// </summary>
        public float BatteryVoltage { get; set; }

        /// <summary>
        /// 是否低电压
        /// </summary>
        public bool IsLowVoltage { get; set; }

        /// <summary>
        /// 电源供应状态（1-交流供电，2-电池供电）
        /// </summary>
        public byte PowerSupplyStatus { get; set; }

        /// <summary>
        /// 多门互锁状态
        /// </summary>
        public bool MultiDoorInterlockStatus { get; set; }

        /// <summary>
        /// 反潜回状态
        /// </summary>
        public bool AntiSneakStatus { get; set; }

        /// <summary>
        /// 主机防拆状态
        /// </summary>
        public bool HostAntiDismantleStatus { get; set; }

        /// <summary>
        /// 指示灯状态
        /// </summary>
        public bool IndicatorLightStatus { get; set; }

        /// <summary>
        /// 机箱传感器状态数组
        /// </summary>
        public bool[] CaseStatus { get; set; }

        /// <summary>
        /// 报警输入设防状态数组
        /// </summary>
        public bool[] SetupAlarmStatus { get; set; }

        /// <summary>
        /// 报警输入状态数组
        /// </summary>
        public bool[] AlarmInStatus { get; set; }

        /// <summary>
        /// 报警输出状态数组
        /// </summary>
        public bool[] AlarmOutStatus { get; set; }

        /// <summary>
        /// 添加卡数量
        /// </summary>
        public uint CardNum { get; set; }

        /// <summary>
        /// 火警报警状态
        /// </summary>
        public byte FireAlarmStatus { get; set; }

        /// <summary>
        /// 设备综合状态
        /// </summary>
        public DeviceOverallStatus OverallStatus { get; set; }

        /// <summary>
        /// 状态描述信息
        /// </summary>
        public string StatusMessage { get; set; }

        /// <summary>
        /// 最后更新时间
        /// </summary>
        public DateTime LastUpdated { get; set; }

        /// <summary>
        /// 原始门状态值（用于调试）
        /// </summary>
        public byte RawDoorStatus { get; set; }

        /// <summary>
        /// 原始锁状态值（用于调试）
        /// </summary>
        public byte RawLockStatus { get; set; }

        /// <summary>
        /// 原始磁力锁状态值（用于调试）
        /// </summary>
        public byte RawMagneticStatus { get; set; }

        /// <summary>
        /// 构造函数
        /// </summary>
        public DeviceStatusInfo()
        {
            IsOnline = false;
            DoorStatus = DoorStatus.Invalid;
            DoorLockStatus = DoorLockStatus.NormalClose;
            MagneticLockStatus = MagneticLockStatus.NormalClose;
            OverallStatus = DeviceOverallStatus.Offline;
            StatusMessage = "未知状态";
            LastUpdated = DateTime.Now;
            
            // 初始化数组（海康威视SDK支持最大数量）
            CardReaderOnlineStatus = new bool[512];
            CardReaderVerifyModes = new CardReaderVerifyMode[512];
            CardReaderAntiDismantleStatus = new bool[512];
            CaseStatus = new bool[8];
            SetupAlarmStatus = new bool[512];
            AlarmInStatus = new bool[512];
            AlarmOutStatus = new bool[512];
        }

        /// <summary>
        /// 获取门状态的中文描述
        /// </summary>
        /// <returns>门状态描述</returns>
        public string GetDoorStatusDescription()
        {
            switch (DoorStatus)
            {
                case DoorStatus.Invalid:
                    return "无效状态";
                case DoorStatus.Sleep:
                    return "休眠状态";
                case DoorStatus.AlwaysOpen:
                    return "常开状态（自由通行）";
                case DoorStatus.AlwaysClose:
                    return "常闭状态（禁止通行）";
                case DoorStatus.Normal:
                    return "普通状态（按计划控制）";
                default:
                    return $"未知状态({(byte)DoorStatus})";
            }
        }

        /// <summary>
        /// 获取门锁状态的中文描述
        /// </summary>
        /// <returns>门锁状态描述</returns>
        public string GetDoorLockStatusDescription()
        {
            switch (DoorLockStatus)
            {
                case DoorLockStatus.NormalClose:
                    return "门锁常闭";
                case DoorLockStatus.NormalOpen:
                    return "门锁常开";
                case DoorLockStatus.ShortCircuit:
                    return "门锁短路报警";
                case DoorLockStatus.OpenCircuit:
                    return "门锁断路报警";
                case DoorLockStatus.Abnormal:
                    return "门锁异常报警";
                default:
                    return $"未知状态({(byte)DoorLockStatus})";
            }
        }

        /// <summary>
        /// 获取磁力锁状态的中文描述
        /// </summary>
        /// <returns>磁力锁状态描述</returns>
        public string GetMagneticLockStatusDescription()
        {
            switch (MagneticLockStatus)
            {
                case MagneticLockStatus.NormalClose:
                    return "磁力锁常闭";
                case MagneticLockStatus.NormalOpen:
                    return "磁力锁常开";
                case MagneticLockStatus.ShortCircuit:
                    return "磁力锁短路报警";
                case MagneticLockStatus.OpenCircuit:
                    return "磁力锁断路报警";
                case MagneticLockStatus.Abnormal:
                    return "磁力锁异常报警";
                default:
                    return $"未知状态({(byte)MagneticLockStatus})";
            }
        }

        /// <summary>
        /// 检查是否有硬件错误
        /// </summary>
        /// <returns>是否存在硬件错误</returns>
        public bool HasHardwareError()
        {
            return DoorLockStatus == DoorLockStatus.ShortCircuit ||
                   DoorLockStatus == DoorLockStatus.OpenCircuit ||
                   DoorLockStatus == DoorLockStatus.Abnormal ||
                   MagneticLockStatus == MagneticLockStatus.ShortCircuit ||
                   MagneticLockStatus == MagneticLockStatus.OpenCircuit ||
                   MagneticLockStatus == MagneticLockStatus.Abnormal;
        }

        /// <summary>
        /// 检查是否有警告状态
        /// </summary>
        /// <returns>是否存在警告状态</returns>
        public bool HasWarning()
        {
            return DoorStatus == DoorStatus.Sleep || IsLowVoltage;
        }

        /// <summary>
        /// 获取在线读卡器数量
        /// </summary>
        /// <returns>在线读卡器数量</returns>
        public int GetOnlineCardReaderCount()
        {
            int count = 0;
            for (int i = 0; i < CardReaderOnlineStatus.Length; i++)
            {
                if (CardReaderOnlineStatus[i])
                    count++;
            }
            return count;
        }

        /// <summary>
        /// 获取电源状态描述
        /// </summary>
        /// <returns>电源状态描述</returns>
        public string GetPowerSupplyStatusDescription()
        {
            switch (PowerSupplyStatus)
            {
                case 1:
                    return "交流供电";
                case 2:
                    return "电池供电";
                default:
                    return "未知供电状态";
            }
        }

        /// <summary>
        /// 获取火警状态描述
        /// </summary>
        /// <returns>火警状态描述</returns>
        public string GetFireAlarmStatusDescription()
        {
            switch (FireAlarmStatus)
            {
                case 0:
                    return "火警正常";
                case 1:
                    return "火警短路报警";
                case 2:
                    return "火警断路报警";
                default:
                    return "火警状态异常";
            }
        }
    }
}