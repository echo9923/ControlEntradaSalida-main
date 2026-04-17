using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ControlEntradaSalida.Application.Abstractions;
using ControlEntradaSalida.Domain.Common;

namespace ControlEntradaSalida.Application.Faces
{
    public sealed class GetFacesQueryHandler
    {
        private readonly ILegacyPermissionOperations operations;

        public GetFacesQueryHandler(ILegacyPermissionOperations operations)
        {
            this.operations = operations;
        }

        public Task<OperationResult> HandleAsync(
            IReadOnlyList<string> employeeIds,
            RequestContext requestContext,
            CancellationToken cancellationToken)
        {
            return operations.GetFacesAsync(employeeIds, requestContext, cancellationToken);
        }
    }
}
