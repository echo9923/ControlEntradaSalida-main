using System.Collections.Generic;

namespace ControlEntradaSalida
{
    /// <summary>
    /// 人脸查询/删除等操作的摘要结果。
    /// </summary>
    public sealed class FaceOperationSummary
    {
        public int Total { get; set; }

        public int Succeeded { get; set; }

        public int Failed { get; set; }

        /// <summary>
        /// 实际参与操作的在线设备数量。
        /// </summary>
        public int TargetDevices { get; set; }

        public List<string> Errors { get; } = new List<string>();

        public List<FaceOperationItem> Items { get; } = new List<FaceOperationItem>();
    }

    public sealed class FaceOperationItem
    {
        public string EmployeeId { get; set; }

        public bool Success { get; set; }

        public string FaceImageBase64 { get; set; }

        public string RawResponse { get; set; }

        public string Error { get; set; }
    }

    public sealed class FaceQueryResult
    {
        public bool Success { get; set; }
        public string FaceImageBase64 { get; set; }
        public string RawResponse { get; set; }
        public string ErrorMessage { get; set; }
    }
}
