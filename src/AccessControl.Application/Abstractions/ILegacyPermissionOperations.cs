using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ControlEntradaSalida.Application.Models;
using ControlEntradaSalida.Domain.Common;

namespace ControlEntradaSalida.Application.Abstractions
{
    public interface ILegacyPermissionOperations
    {
        Task<OperationResult> SyncPermissionsAsync(
            IReadOnlyList<PermissionUpdateCommandItem> items,
            RequestContext requestContext,
            CancellationToken cancellationToken);

        Task<OperationResult> SyncPersonsAsync(
            IReadOnlyList<PersonSyncCommandItem> items,
            RequestContext requestContext,
            CancellationToken cancellationToken);

        Task<OperationResult> DeleteFacesAsync(
            IReadOnlyList<string> employeeIds,
            RequestContext requestContext,
            CancellationToken cancellationToken);

        Task<OperationResult> DeletePersonsAsync(
            IReadOnlyList<string> employeeIds,
            RequestContext requestContext,
            CancellationToken cancellationToken);

        Task<OperationResult> GetFacesAsync(
            IReadOnlyList<string> employeeIds,
            RequestContext requestContext,
            CancellationToken cancellationToken);

        Task<OperationResult> GetEnrollmentStatusAsync(
            EnrollmentStatusQuery query,
            RequestContext requestContext,
            CancellationToken cancellationToken);

        Task<IReadOnlyList<OperationResult>> CaptureFaceStreamAsync(
            CaptureFaceStreamCommand command,
            RequestContext requestContext,
            CancellationToken cancellationToken);
    }
}
