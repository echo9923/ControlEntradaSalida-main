using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace ControlEntradaSalida.Domain.Common
{
    public sealed class OperationResult
    {
        public bool IsSuccess { get; }

        public string Code { get; }

        public string Message { get; }

        public object Payload { get; }

        public IReadOnlyList<string> Errors { get; }

        public IReadOnlyList<OperationErrorDetail> ErrorDetails { get; }

        private OperationResult(
            bool success,
            string code,
            string message,
            object payload,
            IEnumerable<string> errors,
            IEnumerable<OperationErrorDetail> errorDetails)
        {
            IsSuccess = success;
            Code = code ?? "internal_error";
            Message = message ?? string.Empty;
            Payload = payload;
            Errors = (errors ?? Enumerable.Empty<string>()).ToArray();
            ErrorDetails = (errorDetails ?? Enumerable.Empty<OperationErrorDetail>()).ToArray();
        }

        public static OperationResult Success(
            string code,
            string message,
            object payload = null,
            IEnumerable<string> errors = null,
            IEnumerable<OperationErrorDetail> errorDetails = null)
        {
            return new OperationResult(true, code, message, payload, errors, errorDetails);
        }

        public static OperationResult Failure(
            string code,
            string message,
            IEnumerable<string> errors = null,
            IEnumerable<OperationErrorDetail> errorDetails = null,
            object payload = null)
        {
            return new OperationResult(false, code, message, payload, errors, errorDetails);
        }
    }

    public sealed class OperationErrorDetail
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
