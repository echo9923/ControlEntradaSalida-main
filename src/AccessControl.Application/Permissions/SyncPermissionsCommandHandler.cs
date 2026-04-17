using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ControlEntradaSalida.Application.Abstractions;
using ControlEntradaSalida.Application.Models;
using ControlEntradaSalida.Domain.Common;

namespace ControlEntradaSalida.Application.Permissions
{
    public sealed class SyncPermissionsCommandHandler
    {
        private readonly ILegacyPermissionOperations operations;

        public SyncPermissionsCommandHandler(ILegacyPermissionOperations operations)
        {
            this.operations = operations;
        }

        public Task<OperationResult> HandleAsync(
            IReadOnlyList<PermissionUpdateCommandItem> items,
            RequestContext requestContext,
            CancellationToken cancellationToken)
        {
            return operations.SyncPermissionsAsync(items, requestContext, cancellationToken);
        }
    }
}
