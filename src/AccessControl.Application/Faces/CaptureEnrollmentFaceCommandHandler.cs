using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ControlEntradaSalida.Application.Abstractions;
using ControlEntradaSalida.Application.Models;
using ControlEntradaSalida.Domain.Common;

namespace ControlEntradaSalida.Application.Faces
{
    public sealed class CaptureEnrollmentFaceCommandHandler
    {
        private readonly ILegacyPermissionOperations operations;

        public CaptureEnrollmentFaceCommandHandler(ILegacyPermissionOperations operations)
        {
            this.operations = operations;
        }

        public Task<IReadOnlyList<OperationResult>> HandleAsync(
            CaptureFaceStreamCommand command,
            RequestContext requestContext,
            CancellationToken cancellationToken)
        {
            return operations.CaptureFaceStreamAsync(command, requestContext, cancellationToken);
        }
    }
}
