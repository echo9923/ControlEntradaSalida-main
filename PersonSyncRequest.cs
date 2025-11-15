using System;

namespace ControlEntradaSalida
{
    /// <summary>
    /// 表示向门禁设备下发的单个人员及其人脸数据。
    /// </summary>
    public sealed class PersonSyncRequest
    {
        /// <summary>
        /// 工号/人员唯一标识。必填。
        /// </summary>
        public string EmployeeId { get; set; }

        /// <summary>
        /// 人员姓名，可选。
        /// </summary>
        public string FullName { get; set; }

        /// <summary>
        /// 性别，取值 male/female/unknown。
        /// </summary>
        public string Gender { get; set; } = "unknown";

        /// <summary>
        /// 开始有效时间。
        /// </summary>
        public DateTime? ValidFrom { get; set; }

        /// <summary>
        /// 结束有效时间。
        /// </summary>
        public DateTime? ValidTo { get; set; }

        /// <summary>
        /// 是否启用该人员。
        /// </summary>
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// 人脸图片二进制数据，可选。
        /// </summary>
        public byte[] FaceImageBytes { get; set; }

        /// <summary>
        /// 人脸图片格式（jpg/png等），仅用于日志展示。
        /// </summary>
        public string FaceImageFormat { get; set; }

        /// <summary>
        /// 当前请求是否包含人脸图片。
        /// </summary>
        public bool HasFace => FaceImageBytes != null && FaceImageBytes.Length > 0;
    }
}
