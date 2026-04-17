using System.Threading;
using System.Threading.Tasks;
using ControlEntradaSalida.Application.Abstractions;
using ControlEntradaSalida.Application.Models;
using ControlEntradaSalida.Domain.Common;

namespace ControlEntradaSalida.Application.Devices
{
    public sealed class DisconnectDeviceCommandHandler
    {
        private readonly IDeviceRegistryService deviceRegistryService;

        public DisconnectDeviceCommandHandler(IDeviceRegistryService deviceRegistryService)
        {
            this.deviceRegistryService = deviceRegistryService;
        }

        public Task<OperationResult> HandleAsync(
            DisconnectDeviceCommand command,
            RequestContext requestContext,
            CancellationToken cancellationToken)
        {
            return deviceRegistryService.DisconnectDeviceAsync(command, requestContext, cancellationToken);
        }
    }
}
