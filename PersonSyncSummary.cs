using System.Collections.Generic;

namespace ControlEntradaSalida
{
    /// <summary>
    /// 统计人员及人脸同步结果的摘要。
    /// </summary>
    public sealed class PersonSyncSummary
    {
        public int TotalPersons { get; set; }

        public int SuccessfulPersons { get; set; }

        public int FailedPersons { get; set; }

        /// <summary>
        /// 成功下发的人脸数量。
        /// </summary>
        public int FacesUploaded { get; set; }

        /// <summary>
        /// 实际参与下发的在线设备数量。
        /// </summary>
        public int TargetDevices { get; set; }

        public int QueuedCount { get; set; }

        public List<QueuedOperationDetail> QueuedDetails { get; } = new List<QueuedOperationDetail>();

        public List<string> Errors { get; } = new List<string>();

        public List<GrpcErrorDetail> ErrorDetails { get; } = new List<GrpcErrorDetail>();
    }
}
