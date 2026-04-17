using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ControlEntradaSalida.Application.Abstractions;
using ControlEntradaSalida.Domain.Common;

namespace ControlEntradaSalida.Application.Faces
{
    public sealed class DeleteFacesCommandHandler
    {
        private readonly ILegacyPermissionOperations operations;

        public DeleteFacesCommandHandler(ILegacyPermissionOperations operations)
        {
            this.operations = operations;
        }

        public Task<OperationResult> HandleAsync(
            IReadOnlyList<string> employeeIds,
            RequestContext requestContext,
            CancellationToken cancellationToken)
        {
            return operations.DeleteFacesAsync(employeeIds, requestContext, cancellationToken);
        }
    }
}
