using System;

namespace ControlEntradaSalida
{
    public sealed class FaceCaptureResult
    {
        public bool Success { get; private set; }
        public string FaceImageBase64 { get; private set; }
        public string Format { get; private set; }
        public string ErrorMessage { get; private set; }

        public static FaceCaptureResult Fail(string message)
        {
            return new FaceCaptureResult
            {
                Success = false,
                ErrorMessage = message
            };
        }

        public static FaceCaptureResult Success(string base64, string format)
        {
            return new FaceCaptureResult
            {
                Success = true,
                FaceImageBase64 = base64,
                Format = string.IsNullOrWhiteSpace(format) ? "jpg" : format
            };
        }
    }
}
