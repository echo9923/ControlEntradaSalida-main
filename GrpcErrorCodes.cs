namespace ControlEntradaSalida
{
    /// <summary>
    /// gRPC 统一错误码。
    /// </summary>
    public static class GrpcErrorCodes
    {
        public const string Ok = "OK";
        public const string PartialSuccess = "PARTIAL_SUCCESS";
        public const string Failed = "FAILED";

        public const string InvalidArgument = "INVALID_ARGUMENT";
        public const string BatchTooLarge = "BATCH_TOO_LARGE";
        public const string NotFound = "NOT_FOUND";
        public const string InternalError = "INTERNAL_ERROR";

        public const string Unauthenticated = "UNAUTHENTICATED";

        public const string DeviceError = "DEVICE_ERROR";
        public const string DbError = "DB_ERROR";
        public const string SdkError = "SDK_ERROR";
        public const string FaceTooLarge = "FACE_TOO_LARGE";
    }
}
