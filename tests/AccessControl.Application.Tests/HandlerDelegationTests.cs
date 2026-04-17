using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ControlEntradaSalida.Application.Abstractions;
using ControlEntradaSalida.Application.Devices;
using ControlEntradaSalida.Application.Models;
using ControlEntradaSalida.Application.Permissions;
using ControlEntradaSalida.Domain.Common;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AccessControl.Application.Tests
{
    [TestClass]
    public sealed class HandlerDelegationTests
    {
        [TestMethod]
        public async Task SyncPermissionsCommandHandler_HandleAsync_DelegatesToLegacyPermissionOperations()
        {
            var operations = new FakeLegacyPermissionOperations();
            var handler = new SyncPermissionsCommandHandler(operations);
            var context = new RequestContext("req-1");
            var items = new[] { new PermissionUpdateCommandItem("EMP-1", 2) };

            OperationResult result = await handler.HandleAsync(items, context, CancellationToken.None);

            Assert.AreSame(operations.ExpectedResult, result);
            Assert.AreEqual(1, operations.SyncPermissionsCalls);
            Assert.AreEqual("EMP-1", operations.LastPermissionItems[0].EmployeeId);
            Assert.AreEqual("req-1", operations.LastRequestContext.RequestId);
        }

        [TestMethod]
        public async Task GetDeviceStatusQueryHandler_HandleAsync_DelegatesToDeviceRegistryService()
        {
            var deviceRegistry = new FakeDeviceRegistryService();
            var handler = new GetDeviceStatusQueryHandler(deviceRegistry);
            var context = new RequestContext("req-2");
            var query = new DeviceStatusQuery(includeDisabled: false, refresh: true, deviceId: 9, deviceIds: null, ipAddress: null);

            OperationResult result = await handler.HandleAsync(query, context, CancellationToken.None);

            Assert.AreSame(deviceRegistry.ExpectedResult, result);
            Assert.AreEqual(1, deviceRegistry.GetDeviceStatusCalls);
            Assert.AreEqual(9, deviceRegistry.LastStatusQuery.DeviceId);
            Assert.IsFalse(deviceRegistry.LastStatusQuery.IncludeDisabled);
            Assert.IsTrue(deviceRegistry.LastStatusQuery.Refresh);
            Assert.AreEqual("req-2", deviceRegistry.LastRequestContext.RequestId);
        }

        private sealed class FakeLegacyPermissionOperations : ILegacyPermissionOperations
        {
            public int SyncPermissionsCalls { get; private set; }

            public IReadOnlyList<PermissionUpdateCommandItem> LastPermissionItems { get; private set; }

            public RequestContext LastRequestContext { get; private set; }

            public OperationResult ExpectedResult { get; } = OperationResult.Success("ok", "done");

            public Task<OperationResult> SyncPermissionsAsync(
                IReadOnlyList<PermissionUpdateCommandItem> items,
                RequestContext requestContext,
                CancellationToken cancellationToken)
            {
                SyncPermissionsCalls++;
                LastPermissionItems = items;
                LastRequestContext = requestContext;
                return Task.FromResult(ExpectedResult);
            }

            public Task<OperationResult> SyncPersonsAsync(
                IReadOnlyList<PersonSyncCommandItem> items,
                RequestContext requestContext,
                CancellationToken cancellationToken)
            {
                return Task.FromResult(ExpectedResult);
            }

            public Task<OperationResult> DeleteFacesAsync(
                IReadOnlyList<string> employeeIds,
                RequestContext requestContext,
                CancellationToken cancellationToken)
            {
                return Task.FromResult(ExpectedResult);
            }

            public Task<OperationResult> DeletePersonsAsync(
                IReadOnlyList<string> employeeIds,
                RequestContext requestContext,
                CancellationToken cancellationToken)
            {
                return Task.FromResult(ExpectedResult);
            }

            public Task<OperationResult> GetFacesAsync(
                IReadOnlyList<string> employeeIds,
                RequestContext requestContext,
                CancellationToken cancellationToken)
            {
                return Task.FromResult(ExpectedResult);
            }

            public Task<OperationResult> GetEnrollmentStatusAsync(
                EnrollmentStatusQuery query,
                RequestContext requestContext,
                CancellationToken cancellationToken)
            {
                return Task.FromResult(ExpectedResult);
            }

            public Task<IReadOnlyList<OperationResult>> CaptureFaceStreamAsync(
                CaptureFaceStreamCommand command,
                RequestContext requestContext,
                CancellationToken cancellationToken)
            {
                return Task.FromResult<IReadOnlyList<OperationResult>>(new[] { ExpectedResult });
            }
        }

        private sealed class FakeDeviceRegistryService : IDeviceRegistryService
        {
            public int GetDeviceStatusCalls { get; private set; }

            public DeviceStatusQuery LastStatusQuery { get; private set; }

            public RequestContext LastRequestContext { get; private set; }

            public OperationResult ExpectedResult { get; } = OperationResult.Success("ok", "done");

            public Task<OperationResult> GetDeviceStatusAsync(
                DeviceStatusQuery query,
                RequestContext requestContext,
                CancellationToken cancellationToken)
            {
                GetDeviceStatusCalls++;
                LastStatusQuery = query;
                LastRequestContext = requestContext;
                return Task.FromResult(ExpectedResult);
            }

            public Task<OperationResult> AddDeviceAsync(AddDeviceCommand command, RequestContext requestContext, CancellationToken cancellationToken)
            {
                return Task.FromResult(ExpectedResult);
            }

            public Task<OperationResult> DeleteDeviceAsync(DeleteDeviceCommand command, RequestContext requestContext, CancellationToken cancellationToken)
            {
                return Task.FromResult(ExpectedResult);
            }

            public Task<OperationResult> DisconnectDeviceAsync(DisconnectDeviceCommand command, RequestContext requestContext, CancellationToken cancellationToken)
            {
                return Task.FromResult(ExpectedResult);
            }

            public Task<OperationResult> ReconnectDeviceAsync(ReconnectDeviceCommand command, RequestContext requestContext, CancellationToken cancellationToken)
            {
                return Task.FromResult(ExpectedResult);
            }
        }
    }
}
