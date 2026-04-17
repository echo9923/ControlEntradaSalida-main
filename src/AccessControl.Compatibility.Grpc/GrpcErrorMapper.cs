using Grpc.Core;

namespace ControlEntradaSalida.Compatibility.Grpc
{
    public static class GrpcErrorMapper
    {
        public static StatusCode MapToStatusCode(string code)
        {
            switch ((code ?? string.Empty).ToLowerInvariant())
            {
                case "invalid_argument":
                case "batch_too_large":
                    return StatusCode.InvalidArgument;
                case "not_found":
                    return StatusCode.NotFound;
                case "unauthenticated":
                    return StatusCode.Unauthenticated;
                default:
                    return StatusCode.Internal;
            }
        }
    }
}
