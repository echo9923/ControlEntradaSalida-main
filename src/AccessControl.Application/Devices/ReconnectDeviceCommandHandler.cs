using System.Threading;
using System.Threading.Tasks;
using ControlEntradaSalida.Application.Abstractions;
using ControlEntradaSalida.Application.Models;
using ControlEntradaSalida.Domain.Common;

namespace ControlEntradaSalida.Application.Devices
{
    public sealed class ReconnectDeviceCommandHandler
    {
        private readonly IDeviceRegistryService deviceRegistryService;

        public ReconnectDeviceCommandHandler(IDeviceRegistryService deviceRegistryService)
        {
            this.deviceRegistryService = deviceRegistryService;
        }

        public Task<OperationResult> HandleAsync(
            ReconnectDeviceCommand command,
            RequestContext requestContext,
            CancellationToken cancellationToken)
        {
            return deviceRegistryService.ReconnectDeviceAsync(command, requestContext, cancellationToken);
        }
    }
}
