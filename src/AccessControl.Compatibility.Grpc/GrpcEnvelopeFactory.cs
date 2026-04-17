using System.Collections.Generic;
using ControlEntradaSalida.Domain.Common;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ControlEntradaSalida.Compatibility.Grpc
{
    public static class GrpcEnvelopeFactory
    {
        public static string Create(string requestId, OperationResult result)
        {
            JObject payload = result?.Payload == null ? new JObject() : JObject.FromObject(result.Payload);
            payload["requestId"] = requestId;
            payload["success"] = result?.IsSuccess ?? false;
            payload["code"] = result?.Code ?? "internal_error";
            payload["message"] = result?.Message ?? string.Empty;
            payload["errors"] = JArray.FromObject(result?.Errors ?? new List<string>());
            payload["errorDetails"] = JArray.FromObject(result?.ErrorDetails ?? new List<OperationErrorDetail>());
            return payload.ToString(Formatting.None);
        }
    }
}
