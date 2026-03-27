using Newtonsoft.Json;

namespace ControlEntradaSalida
{
    /// <summary>
    /// 表示已进入设备离线补偿队列的明细。
    /// </summary>
    public sealed class QueuedOperationDetail
    {
        [JsonProperty("employeeId")]
        public string EmployeeId { get; set; }

        [JsonProperty("deviceId")]
        public int? DeviceId { get; set; }

        [JsonProperty("deviceName")]
        public string DeviceName { get; set; }

        [JsonProperty("deviceIp")]
        public string DeviceIp { get; set; }

        [JsonProperty("operation")]
        public string Operation { get; set; }

        [JsonProperty("message")]
        public string Message { get; set; }
    }
}
