using System.Threading;
using System.Threading.Tasks;
using ControlEntradaSalida.Application.Abstractions;
using ControlEntradaSalida.Application.Models;
using ControlEntradaSalida.Domain.Common;

namespace ControlEntradaSalida.Application.Devices
{
    public sealed class AddDeviceCommandHandler
    {
        private readonly IDeviceRegistryService deviceRegistryService;

        public AddDeviceCommandHandler(IDeviceRegistryService deviceRegistryService)
        {
            this.deviceRegistryService = deviceRegistryService;
        }

        public Task<OperationResult> HandleAsync(
            AddDeviceCommand command,
            RequestContext requestContext,
            CancellationToken cancellationToken)
        {
            return deviceRegistryService.AddDeviceAsync(command, requestContext, cancellationToken);
        }
    }
}
