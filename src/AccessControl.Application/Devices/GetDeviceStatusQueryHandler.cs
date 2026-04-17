using System.Threading;
using System.Threading.Tasks;
using ControlEntradaSalida.Application.Abstractions;
using ControlEntradaSalida.Application.Models;
using ControlEntradaSalida.Domain.Common;

namespace ControlEntradaSalida.Application.Devices
{
    public sealed class GetDeviceStatusQueryHandler
    {
        private readonly IDeviceRegistryService deviceRegistryService;

        public GetDeviceStatusQueryHandler(IDeviceRegistryService deviceRegistryService)
        {
            this.deviceRegistryService = deviceRegistryService;
        }

        public Task<OperationResult> HandleAsync(
            DeviceStatusQuery query,
            RequestContext requestContext,
            CancellationToken cancellationToken)
        {
            return deviceRegistryService.GetDeviceStatusAsync(query, requestContext, cancellationToken);
        }
    }
}
