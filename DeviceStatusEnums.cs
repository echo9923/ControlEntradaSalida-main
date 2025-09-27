using System;

namespace ControlEntradaSalida
{
    /// <summary>
    /// 门状态枚举 - 基于海康威视SDK NET_DVR_ACS_WORK_STATUS_V50.byDoorStatus
    /// 参考：设备网络SDK编程指南（明眸-以人为中心）
    /// </summary>
    public enum DoorStatus : byte
    {
        Invalid = 0,    // 无效
        Sleep = 1,      // 休眠状态
        AlwaysOpen = 2, // 常开状态（自由通行）
        AlwaysClose = 3,// 常闭状态（禁止通行）
        Normal = 4      // 普通状态（按计划控制）
    }

    /// <summary>
    /// 门锁状态枚举 - 基于海康威视SDK NET_DVR_ACS_WORK_STATUS_V50.byDoorLockStatus
    /// 参考：设备网络SDK编程指南（明眸-以人为中心）
    /// </summary>
    public enum DoorLockStatus : byte
    {
        NormalClose = 0,    // 常闭（继电器状态）
        NormalOpen = 1,     // 常开（继电器状态）
        ShortCircuit = 2,   // 损坏短路报警
        OpenCircuit = 3,    // 损坏断路报警
        Abnormal = 4        // 异常报警
    }

    /// <summary>
    /// 磁力锁状态枚举 - 基于海康威视SDK NET_DVR_ACS_WORK_STATUS_V50.byMagneticStatus
    /// 参考：设备网络SDK编程指南（明眸-以人为中心）
    /// </summary>
    public enum MagneticLockStatus : byte
    {
        NormalClose = 0,    // 常闭
        NormalOpen = 1,     // 常开
        ShortCircuit = 2,   // 损坏短路报警
        OpenCircuit = 3,    // 损坏断路报警
        Abnormal = 4        // 异常报警
    }

    /// <summary>
    /// 读卡器验证方式枚举 - 已移除所有刷卡相关模式，仅保留人脸和指纹认证
    /// 参考：设备网络SDK编程指南（明眸-以人为中心）
    /// </summary>
    public enum CardReaderVerifyMode : byte
    {
        Invalid = 0,            // 无效
        Sleep = 1,              // 休眠
        Fingerprint = 5,        // 指纹
        FingerprintAndPassword = 6,  // 指纹+密码
        FaceAndFingerprint = 11, // 人脸+指纹
        FaceAndPassword = 12,   // 人脸+密码
        Face = 14,              // 人脸
        EmployeeNoAndPassword = 15, // 工号+密码
        FingerprintOrPassword = 16, // 指纹或密码
        EmployeeNoAndFingerprint = 17, // 工号+指纹
        EmployeeNoAndFingerprintAndPassword = 18, // 工号+指纹+密码
        FaceAndPasswordAndFingerprint = 20, // 人脸+密码+指纹
        EmployeeNoAndFace = 21, // 工号+人脸
        FingerprintOrFace = 23  // 指纹或人脸
    }

    /// <summary>
    /// 设备综合状态枚举
    /// </summary>
    public enum DeviceOverallStatus
    {
        Online,             // 在线正常
        OnlineWithWarning,  // 在线但有警告
        OnlineWithError,    // 在线但有错误
        Offline             // 离线
    }

}