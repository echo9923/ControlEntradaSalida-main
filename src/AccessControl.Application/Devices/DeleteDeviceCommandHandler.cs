using System.Threading;
using System.Threading.Tasks;
using ControlEntradaSalida.Application.Abstractions;
using ControlEntradaSalida.Application.Models;
using ControlEntradaSalida.Domain.Common;

namespace ControlEntradaSalida.Application.Devices
{
    public sealed class DeleteDeviceCommandHandler
    {
        private readonly IDeviceRegistryService deviceRegistryService;

        public DeleteDeviceCommandHandler(IDeviceRegistryService deviceRegistryService)
        {
            this.deviceRegistryService = deviceRegistryService;
        }

        public Task<OperationResult> HandleAsync(
            DeleteDeviceCommand command,
            RequestContext requestContext,
            CancellationToken cancellationToken)
        {
            return deviceRegistryService.DeleteDeviceAsync(command, requestContext, cancellationToken);
        }
    }
}
