using System.Collections.Generic;

namespace ControlEntradaSalida.Domain.Common
{
    public sealed class RequestContext
    {
        public RequestContext(string requestId, string peer = null, IReadOnlyDictionary<string, string> headers = null)
        {
            RequestId = requestId;
            Peer = peer;
            Headers = headers ?? new Dictionary<string, string>();
        }

        public string RequestId { get; }

        public string Peer { get; }

        public IReadOnlyDictionary<string, string> Headers { get; }
    }
}
