using System;
using System.Collections.Generic;

namespace ControlEntradaSalida
{
    /// <summary>
    /// 设备状态辅助类
    /// 提供状态转换、错误处理和状态描述功能
    /// </summary>
    public static class DeviceStatusHelper
    {
        /// <summary>
        /// 门状态描述映射表 - 基于海康威视SDK官方定义
        /// </summary>
        private static readonly Dictionary<DoorStatus, string> DoorStatusDescriptions = 
            new Dictionary<DoorStatus, string>
            {
                { DoorStatus.Invalid, "无效状态" },
                { DoorStatus.Sleep, "休眠状态" },
                { DoorStatus.AlwaysOpen, "常开状态（自由通行）" },
                { DoorStatus.AlwaysClose, "常闭状态（禁止通行）" },
                { DoorStatus.Normal, "普通状态（按计划控制）" }
            };

        /// <summary>
        /// 门锁状态描述映射表
        /// </summary>
        private static readonly Dictionary<DoorLockStatus, string> DoorLockStatusDescriptions = 
            new Dictionary<DoorLockStatus, string>
            {
                { DoorLockStatus.NormalClose, "门锁常闭" },
                { DoorLockStatus.NormalOpen, "门锁常开" },
                { DoorLockStatus.ShortCircuit, "门锁短路报警" },
                { DoorLockStatus.OpenCircuit, "门锁断路报警" },
                { DoorLockStatus.Abnormal, "门锁异常报警" }
            };

        /// <summary>
        /// 磁力锁状态描述映射表
        /// </summary>
        private static readonly Dictionary<MagneticLockStatus, string> MagneticLockStatusDescriptions = 
            new Dictionary<MagneticLockStatus, string>
            {
                { MagneticLockStatus.NormalClose, "磁力锁常闭" },
                { MagneticLockStatus.NormalOpen, "磁力锁常开" },
                { MagneticLockStatus.ShortCircuit, "磁力锁短路报警" },
                { MagneticLockStatus.OpenCircuit, "磁力锁断路报警" },
                { MagneticLockStatus.Abnormal, "磁力锁异常报警" }
            };

        /// <summary>
        /// 读卡器验证模式描述映射表 - 已移除所有刷卡相关模式
        /// </summary>
        private static readonly Dictionary<CardReaderVerifyMode, string> CardReaderVerifyModeDescriptions = 
            new Dictionary<CardReaderVerifyMode, string>
            {
                { CardReaderVerifyMode.Invalid, "无效" },
                { CardReaderVerifyMode.Sleep, "休眠" },
                { CardReaderVerifyMode.Fingerprint, "指纹" },
                { CardReaderVerifyMode.FingerprintAndPassword, "指纹+密码" },
                { CardReaderVerifyMode.FaceAndFingerprint, "人脸+指纹" },
                { CardReaderVerifyMode.FaceAndPassword, "人脸+密码" },
                { CardReaderVerifyMode.Face, "人脸" },
                { CardReaderVerifyMode.EmployeeNoAndPassword, "工号+密码" },
                { CardReaderVerifyMode.FingerprintOrPassword, "指纹或密码" },
                { CardReaderVerifyMode.EmployeeNoAndFingerprint, "工号+指纹" },
                { CardReaderVerifyMode.EmployeeNoAndFingerprintAndPassword, "工号+指纹+密码" },
                { CardReaderVerifyMode.FaceAndPasswordAndFingerprint, "人脸+密码+指纹" },
                { CardReaderVerifyMode.EmployeeNoAndFace, "工号+人脸" },
                { CardReaderVerifyMode.FingerprintOrFace, "指纹或人脸" }
            };

        /// <summary>
        /// 获取门状态的中文描述
        /// </summary>
        /// <param name="doorStatus">门状态枚举值</param>
        /// <returns>门状态描述</returns>
        public static string GetDoorStatusDescription(DoorStatus doorStatus)
        {
            return DoorStatusDescriptions.TryGetValue(doorStatus, out string description) 
                ? description 
                : $"未知门状态({(byte)doorStatus})";
        }

        /// <summary>
        /// 获取门锁状态的中文描述
        /// </summary>
        /// <param name="lockStatus">门锁状态枚举值</param>
        /// <returns>门锁状态描述</returns>
        public static string GetDoorLockStatusDescription(DoorLockStatus lockStatus)
        {
            return DoorLockStatusDescriptions.TryGetValue(lockStatus, out string description) 
                ? description 
                : $"未知门锁状态({(byte)lockStatus})";
        }

        /// <summary>
        /// 获取磁力锁状态的中文描述
        /// </summary>
        /// <param name="magneticStatus">磁力锁状态枚举值</param>
        /// <returns>磁力锁状态描述</returns>
        public static string GetMagneticLockStatusDescription(MagneticLockStatus magneticStatus)
        {
            return MagneticLockStatusDescriptions.TryGetValue(magneticStatus, out string description) 
                ? description 
                : $"未知磁力锁状态({(byte)magneticStatus})";
        }

        /// <summary>
        /// 获取读卡器验证模式的中文描述
        /// </summary>
        /// <param name="verifyMode">验证模式枚举值</param>
        /// <returns>验证模式描述</returns>
        public static string GetCardReaderVerifyModeDescription(CardReaderVerifyMode verifyMode)
        {
            return CardReaderVerifyModeDescriptions.TryGetValue(verifyMode, out string description) 
                ? description 
                : $"未知验证模式({(byte)verifyMode})";
        }

        /// <summary>
        /// 判断是否为严重错误状态
        /// </summary>
        /// <param name="statusInfo">设备状态信息</param>
        /// <returns>是否为严重错误</returns>
        public static bool IsCriticalError(DeviceStatusInfo statusInfo)
        {
            if (statusInfo == null || !statusInfo.IsOnline)
                return true;

            return statusInfo.DoorLockStatus == DoorLockStatus.ShortCircuit ||
                   statusInfo.DoorLockStatus == DoorLockStatus.OpenCircuit ||
                   statusInfo.DoorLockStatus == DoorLockStatus.Abnormal ||
                   statusInfo.MagneticLockStatus == MagneticLockStatus.ShortCircuit ||
                   statusInfo.MagneticLockStatus == MagneticLockStatus.OpenCircuit ||
                   statusInfo.MagneticLockStatus == MagneticLockStatus.Abnormal ||
                   statusInfo.FireAlarmStatus != 0;
        }

        /// <summary>
        /// 判断是否为警告状态
        /// </summary>
        /// <param name="statusInfo">设备状态信息</param>
        /// <returns>是否为警告状态</returns>
        public static bool IsWarningStatus(DeviceStatusInfo statusInfo)
        {
            if (statusInfo == null || !statusInfo.IsOnline)
                return false;

            return statusInfo.DoorStatus == DoorStatus.Sleep ||
                   statusInfo.IsLowVoltage ||
                   statusInfo.PowerSupplyStatus == 2; // 电池供电
        }

        /// <summary>
        /// 获取设备综合状态
        /// </summary>
        /// <param name="statusInfo">设备状态信息</param>
        /// <returns>设备综合状态</returns>
        public static DeviceOverallStatus DetermineOverallStatus(DeviceStatusInfo statusInfo)
        {
            if (statusInfo == null || !statusInfo.IsOnline)
                return DeviceOverallStatus.Offline;

            if (IsCriticalError(statusInfo))
                return DeviceOverallStatus.OnlineWithError;

            if (IsWarningStatus(statusInfo))
                return DeviceOverallStatus.OnlineWithWarning;

            return DeviceOverallStatus.Online;
        }

        /// <summary>
        /// 生成设备状态的详细描述
        /// </summary>
        /// <param name="statusInfo">设备状态信息</param>
        /// <returns>状态详细描述</returns>
        public static string GenerateDetailedStatusMessage(DeviceStatusInfo statusInfo)
        {
            if (statusInfo == null)
                return "设备状态信息为空";

            if (!statusInfo.IsOnline)
                return statusInfo.StatusMessage ?? "设备离线";

            var messages = new List<string>();

            // 添加门状态信息（如果不是正常状态）
            if (statusInfo.DoorStatus != DoorStatus.Normal && statusInfo.DoorStatus != DoorStatus.Invalid)
            {
                messages.Add($"门状态: {GetDoorStatusDescription(statusInfo.DoorStatus)}");
            }

            // 添加门锁报警信息
            if (statusInfo.DoorLockStatus >= DoorLockStatus.ShortCircuit)
            {
                messages.Add($"门锁: {GetDoorLockStatusDescription(statusInfo.DoorLockStatus)}");
            }

            // 添加磁力锁报警信息
            if (statusInfo.MagneticLockStatus >= MagneticLockStatus.ShortCircuit)
            {
                messages.Add($"磁力锁: {GetMagneticLockStatusDescription(statusInfo.MagneticLockStatus)}");
            }

            // 添加电源相关信息
            if (statusInfo.IsLowVoltage)
            {
                messages.Add($"电池低压报警 ({statusInfo.BatteryVoltage:F1}V)");
            }

            if (statusInfo.PowerSupplyStatus == 2)
            {
                messages.Add($"当前电池供电 ({statusInfo.BatteryVoltage:F1}V)");
            }

            // 添加火警信息
            if (statusInfo.FireAlarmStatus != 0)
            {
                string fireStatus = GetFireAlarmDescription(statusInfo.FireAlarmStatus);
                messages.Add($"火警: {fireStatus}");
            }

            // 添加读卡器状态信息
            var onlineCardReaders = statusInfo.GetOnlineCardReaderCount();
            if (onlineCardReaders > 0)
            {
                messages.Add($"读卡器: {onlineCardReaders}个在线");
            }

            // 添加其他状态信息
            var otherStatus = new List<string>();
            if (statusInfo.MultiDoorInterlockStatus)
                otherStatus.Add("多门互锁");
            if (statusInfo.AntiSneakStatus)
                otherStatus.Add("反潜回");
            if (statusInfo.HostAntiDismantleStatus)
                otherStatus.Add("主机防拆报警");

            if (otherStatus.Count > 0)
            {
                messages.Add($"其他: {string.Join(", ", otherStatus)}");
            }

            return messages.Count > 0 ? string.Join("; ", messages) : "设备状态正常";
        }

        /// <summary>
        /// 获取火警状态描述
        /// </summary>
        /// <param name="fireAlarmStatus">火警状态值</param>
        /// <returns>火警状态描述</returns>
        private static string GetFireAlarmDescription(byte fireAlarmStatus)
        {
            switch (fireAlarmStatus)
            {
                case 0:
                    return "正常";
                case 1:
                    return "短路报警";
                case 2:
                    return "断路报警";
                default:
                    return $"异常({fireAlarmStatus})";
            }
        }

        /// <summary>
        /// 验证设备状态数据的有效性
        /// </summary>
        /// <param name="statusInfo">设备状态信息</param>
        /// <returns>验证结果和错误消息</returns>
        public static (bool IsValid, string ErrorMessage) ValidateStatusData(DeviceStatusInfo statusInfo)
        {
            if (statusInfo == null)
                return (false, "状态信息为空");

            // 验证基本字段
            if (statusInfo.DeviceId <= 0)
                return (false, "设备ID无效");

            // 验证时间戳
            if (statusInfo.LastUpdated == default(DateTime))
                return (false, "更新时间无效");

            // 验证状态值范围
            if (!Enum.IsDefined(typeof(DoorStatus), statusInfo.DoorStatus))
                return (false, $"门状态值无效: {(byte)statusInfo.DoorStatus}");

            if (!Enum.IsDefined(typeof(DoorLockStatus), statusInfo.DoorLockStatus))
                return (false, $"门锁状态值无效: {(byte)statusInfo.DoorLockStatus}");

            if (!Enum.IsDefined(typeof(MagneticLockStatus), statusInfo.MagneticLockStatus))
                return (false, $"磁力锁状态值无效: {(byte)statusInfo.MagneticLockStatus}");

            // 验证电池电压范围（合理范围：6V-15V）
            if (statusInfo.BatteryVoltage < 0 || statusInfo.BatteryVoltage > 50)
                return (false, $"电池电压值异常: {statusInfo.BatteryVoltage}V");

            // 验证电源状态
            if (statusInfo.PowerSupplyStatus != 1 && statusInfo.PowerSupplyStatus != 2)
                return (false, $"电源状态值无效: {statusInfo.PowerSupplyStatus}");

            return (true, "验证通过");
        }

        /// <summary>
        /// 创建设备状态变化事件参数
        /// </summary>
        /// <param name="deviceId">设备ID</param>
        /// <param name="oldStatus">旧状态</param>
        /// <param name="newStatus">新状态</param>
        /// <returns>状态变化事件参数</returns>
        public static DeviceStatusChangedEventArgs CreateStatusChangedEvent(
            int deviceId, 
            DeviceStatusInfo oldStatus, 
            DeviceStatusInfo newStatus)
        {
            return new DeviceStatusChangedEventArgs
            {
                DeviceId = deviceId,
                OldStatus = oldStatus,
                NewStatus = newStatus,
                ChangeTime = DateTime.Now,
                IsStatusImproved = IsStatusImproved(oldStatus, newStatus),
                IsCriticalChange = IsCriticalStatusChange(oldStatus, newStatus)
            };
        }

        /// <summary>
        /// 判断状态是否改善
        /// </summary>
        private static bool IsStatusImproved(DeviceStatusInfo oldStatus, DeviceStatusInfo newStatus)
        {
            if (oldStatus == null || newStatus == null)
                return false;

            var oldOverallStatus = DetermineOverallStatus(oldStatus);
            var newOverallStatus = DetermineOverallStatus(newStatus);

            return (int)newOverallStatus < (int)oldOverallStatus;
        }

        /// <summary>
        /// 判断是否为关键状态变化
        /// </summary>
        private static bool IsCriticalStatusChange(DeviceStatusInfo oldStatus, DeviceStatusInfo newStatus)
        {
            if (oldStatus == null || newStatus == null)
                return true;

            // 在线状态变化
            if (oldStatus.IsOnline != newStatus.IsOnline)
                return true;

            // 门状态变化
            if (oldStatus.DoorStatus != newStatus.DoorStatus)
                return true;

            // 硬件错误状态变化
            var oldHasError = IsCriticalError(oldStatus);
            var newHasError = IsCriticalError(newStatus);
            if (oldHasError != newHasError)
                return true;

            return false;
        }
    }

    /// <summary>
    /// 设备状态变化事件参数
    /// </summary>
    public class DeviceStatusChangedEventArgs : EventArgs
    {
        /// <summary>
        /// 设备ID
        /// </summary>
        public int DeviceId { get; set; }

        /// <summary>
        /// 旧状态
        /// </summary>
        public DeviceStatusInfo OldStatus { get; set; }

        /// <summary>
        /// 新状态
        /// </summary>
        public DeviceStatusInfo NewStatus { get; set; }

        /// <summary>
        /// 变化时间
        /// </summary>
        public DateTime ChangeTime { get; set; }

        /// <summary>
        /// 状态是否改善
        /// </summary>
        public bool IsStatusImproved { get; set; }

        /// <summary>
        /// 是否为关键变化
        /// </summary>
        public bool IsCriticalChange { get; set; }
    }
}