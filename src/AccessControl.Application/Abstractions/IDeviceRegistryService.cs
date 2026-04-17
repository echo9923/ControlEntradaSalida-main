using System.Threading;
using System.Threading.Tasks;
using ControlEntradaSalida.Application.Models;
using ControlEntradaSalida.Domain.Common;

namespace ControlEntradaSalida.Application.Abstractions
{
    public interface IDeviceRegistryService
    {
        Task<OperationResult> GetDeviceStatusAsync(DeviceStatusQuery query, RequestContext requestContext, CancellationToken cancellationToken);

        Task<OperationResult> AddDeviceAsync(AddDeviceCommand command, RequestContext requestContext, CancellationToken cancellationToken);

        Task<OperationResult> DeleteDeviceAsync(DeleteDeviceCommand command, RequestContext requestContext, CancellationToken cancellationToken);

        Task<OperationResult> DisconnectDeviceAsync(DisconnectDeviceCommand command, RequestContext requestContext, CancellationToken cancellationToken);

        Task<OperationResult> ReconnectDeviceAsync(ReconnectDeviceCommand command, RequestContext requestContext, CancellationToken cancellationToken);
    }
}
