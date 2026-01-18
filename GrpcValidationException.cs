using System;

namespace ControlEntradaSalida
{
    /// <summary>
    /// 用于携带统一错误码的参数校验异常。
    /// </summary>
    public sealed class GrpcValidationException : ArgumentException
    {
        public string ErrorCode { get; }

        public GrpcValidationException(string message, string errorCode)
            : base(message)
        {
            ErrorCode = errorCode;
        }
    }
}
