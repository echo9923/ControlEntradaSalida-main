using Newtonsoft.Json;

namespace ControlEntradaSalida
{
    /// <summary>
    /// 统一错误详情（员工 + 设备粒度）。
    /// </summary>
    public sealed class GrpcErrorDetail
    {
        [JsonProperty("employeeId")]
        public string EmployeeId { get; set; }

        [JsonProperty("deviceId")]
        public int? DeviceId { get; set; }

        [JsonProperty("deviceName")]
        public string DeviceName { get; set; }

        [JsonProperty("deviceIp")]
        public string DeviceIp { get; set; }

        [JsonProperty("code")]
        public string Code { get; set; }

        [JsonProperty("message")]
        public string Message { get; set; }

        [JsonProperty("rawResponse")]
        public string RawResponse { get; set; }
    }
}
