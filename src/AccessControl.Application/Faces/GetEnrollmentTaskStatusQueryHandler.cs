using System.Threading;
using System.Threading.Tasks;
using ControlEntradaSalida.Application.Abstractions;
using ControlEntradaSalida.Application.Models;
using ControlEntradaSalida.Domain.Common;

namespace ControlEntradaSalida.Application.Faces
{
    public sealed class GetEnrollmentTaskStatusQueryHandler
    {
        private readonly ILegacyPermissionOperations operations;

        public GetEnrollmentTaskStatusQueryHandler(ILegacyPermissionOperations operations)
        {
            this.operations = operations;
        }

        public Task<OperationResult> HandleAsync(
            EnrollmentStatusQuery query,
            RequestContext requestContext,
            CancellationToken cancellationToken)
        {
            return operations.GetEnrollmentStatusAsync(query, requestContext, cancellationToken);
        }
    }
}
