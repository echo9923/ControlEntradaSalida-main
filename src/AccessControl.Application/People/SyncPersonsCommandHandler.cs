using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ControlEntradaSalida.Application.Abstractions;
using ControlEntradaSalida.Application.Models;
using ControlEntradaSalida.Domain.Common;

namespace ControlEntradaSalida.Application.People
{
    public sealed class SyncPersonsCommandHandler
    {
        private readonly ILegacyPermissionOperations operations;

        public SyncPersonsCommandHandler(ILegacyPermissionOperations operations)
        {
            this.operations = operations;
        }

        public Task<OperationResult> HandleAsync(
            IReadOnlyList<PersonSyncCommandItem> items,
            RequestContext requestContext,
            CancellationToken cancellationToken)
        {
            return operations.SyncPersonsAsync(items, requestContext, cancellationToken);
        }
    }
}
