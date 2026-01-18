using System;

namespace ControlEntradaSalida
{
    public sealed class FaceCaptureResult
    {
        public bool Success { get; private set; }
        public string FaceImageBase64 { get; private set; }
        public string Format { get; private set; }
        public string ErrorMessage { get; private set; }

        public int? DeviceId { get; private set; }
        public string DeviceName { get; private set; }
        public string DeviceIp { get; private set; }

        public static FaceCaptureResult Fail(string message, int? deviceId = null, string deviceName = null, string deviceIp = null)
        {
            return new FaceCaptureResult
            {
                Success = false,
                ErrorMessage = message,
                DeviceId = deviceId,
                DeviceName = deviceName,
                DeviceIp = deviceIp
            };
        }

        public static FaceCaptureResult Ok(string base64, string format, int? deviceId = null, string deviceName = null, string deviceIp = null)
        {
            return new FaceCaptureResult
            {
                Success = true,
                FaceImageBase64 = base64,
                Format = string.IsNullOrWhiteSpace(format) ? "jpg" : format,
                DeviceId = deviceId,
                DeviceName = deviceName,
                DeviceIp = deviceIp
            };
        }
    }
}
