using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ControlEntradaSalida.Application.Abstractions;
using ControlEntradaSalida.Application.Devices;
using ControlEntradaSalida.Application.Faces;
using ControlEntradaSalida.Application.Models;
using ControlEntradaSalida.Application.People;
using ControlEntradaSalida.Application.Permissions;
using ControlEntradaSalida.Compatibility.Grpc;
using ControlEntradaSalida.Domain.Common;
using Grpc.Core;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;

namespace AccessControl.Contract.Tests
{
    [TestClass]
    public sealed class GrpcContractTests
    {
        [TestMethod]
        public async Task PermissionSyncCompatibilityService_SyncPermissions_ReturnsEnvelope()
        {
            PermissionSyncCompatibilityService service = CreatePermissionService();

            string response = await service.ExecuteSyncPermissionsAsync("[{\"employee_id\":\"EMP-1\",\"permission_code\":2}]");

            AssertEnvelope(response, "permissions");
        }

        [TestMethod]
        public async Task PermissionSyncCompatibilityService_SyncPersons_ReturnsEnvelope()
        {
            PermissionSyncCompatibilityService service = CreatePermissionService();

            string response = await service.ExecuteSyncPersonsAsync("[{\"employee_id\":\"EMP-2\"}]");

            AssertEnvelope(response, "persons");
        }

        [TestMethod]
        public async Task PermissionSyncCompatibilityService_DeleteFaces_ReturnsEnvelope()
        {
            PermissionSyncCompatibilityService service = CreatePermissionService();

            string response = await service.ExecuteDeleteFacesAsync("[\"EMP-3\"]");

            AssertEnvelope(response, "deleteFaces");
        }

        [TestMethod]
        public async Task PermissionSyncCompatibilityService_DeletePersons_ReturnsEnvelope()
        {
            PermissionSyncCompatibilityService service = CreatePermissionService();

            string response = await service.ExecuteDeletePersonsAsync("[\"EMP-4\"]");

            AssertEnvelope(response, "deletePersons");
        }

        [TestMethod]
        public async Task PermissionSyncCompatibilityService_GetFaces_ReturnsEnvelope()
        {
            PermissionSyncCompatibilityService service = CreatePermissionService();

            string response = await service.ExecuteGetFacesAsync("[\"EMP-5\"]");

            AssertEnvelope(response, "getFaces");
        }

        [TestMethod]
        public async Task PermissionSyncCompatibilityService_GetEnrollmentStatus_ReturnsEnvelope()
        {
            PermissionSyncCompatibilityService service = CreatePermissionService();

            string response = await service.ExecuteGetEnrollmentStatusAsync("{\"taskId\":\"task-1\"}");

            AssertEnvelope(response, "status");
        }

        [TestMethod]
        public async Task PermissionSyncCompatibilityService_CaptureFaceStream_ReturnsFrameEnvelope()
        {
            PermissionSyncCompatibilityService service = CreatePermissionService();

            IReadOnlyList<string> frames = await service.ExecuteCaptureFaceStreamAsync("{\"employee_id\":\"EMP-6\"}");

            Assert.HasCount(1, frames);
            AssertEnvelope(frames[0], "capture");
        }

        [TestMethod]
        public async Task DeviceManagementCompatibilityService_GetDeviceStatus_ReturnsEnvelope()
        {
            DeviceManagementCompatibilityService service = CreateDeviceService();

            string response = await service.ExecuteGetDeviceStatusAsync("{\"deviceId\":1}");

            AssertEnvelope(response, "statusQuery");
        }

        [TestMethod]
        public async Task DeviceManagementCompatibilityService_AddDevice_ReturnsEnvelope()
        {
            DeviceManagementCompatibilityService service = CreateDeviceService();

            string response = await service.ExecuteAddDeviceAsync("{\"deviceId\":2,\"deviceName\":\"D1\",\"ipAddress\":\"10.0.0.1\",\"password\":\"abc\"}");

            AssertEnvelope(response, "addDevice");
        }

        [TestMethod]
        public async Task DeviceManagementCompatibilityService_DeleteDevice_ReturnsEnvelope()
        {
            DeviceManagementCompatibilityService service = CreateDeviceService();

            string response = await service.ExecuteDeleteDeviceAsync("{\"deviceId\":3}");

            AssertEnvelope(response, "deleteDevice");
        }

        [TestMethod]
        public async Task DeviceManagementCompatibilityService_DisconnectDevice_ReturnsEnvelope()
        {
            DeviceManagementCompatibilityService service = CreateDeviceService();

            string response = await service.ExecuteDisconnectDeviceAsync("{\"deviceId\":4}");

            AssertEnvelope(response, "disconnectDevice");
        }

        [TestMethod]
        public async Task DeviceManagementCompatibilityService_ReconnectDevice_ReturnsEnvelope()
        {
            DeviceManagementCompatibilityService service = CreateDeviceService();

            string response = await service.ExecuteReconnectDeviceAsync("{\"deviceId\":5}");

            AssertEnvelope(response, "reconnectDevice");
        }

        [TestMethod]
        public async Task DeviceManagementCompatibilityService_AddDevice_RequiresApiKeyWhenConfigured()
        {
            DeviceManagementCompatibilityService service = CreateDeviceService(apiKey: "secret-key");

            RpcException exception = await Assert.ThrowsExactlyAsync<RpcException>(
                () => service.ExecuteAddDeviceAsync("{\"deviceId\":2,\"deviceName\":\"D1\",\"ipAddress\":\"10.0.0.1\",\"password\":\"abc\"}"));

            Assert.AreEqual(Grpc.Core.StatusCode.Unauthenticated, exception.StatusCode);
            JObject envelope = JObject.Parse(exception.Status.Detail);
            Assert.AreEqual("unauthenticated", envelope.Value<string>("code"));
        }

        [TestMethod]
        public async Task DeviceManagementCompatibilityService_AddDevice_UsesRequestIdHeaderInEnvelope()
        {
            DeviceManagementCompatibilityService service = CreateDeviceService(apiKey: "secret-key");
            var headers = new Grpc.Core.Metadata
            {
                { "x-request-id", "req-123" },
                { "x-api-key", "secret-key" }
            };

            string response = await service.ExecuteAddDeviceAsync("{\"deviceId\":2,\"deviceName\":\"D1\",\"ipAddress\":\"10.0.0.1\",\"password\":\"abc\"}", headers);

            JObject envelope = JObject.Parse(response);
            Assert.AreEqual("req-123", envelope.Value<string>("requestId"));
            Assert.AreEqual("addDevice", envelope.Value<string>("marker"));
        }

        private static void AssertEnvelope(string response, string marker)
        {
            JObject root = JObject.Parse(response);
            Assert.IsFalse(string.IsNullOrWhiteSpace(root.Value<string>("requestId")));
            Assert.IsTrue(root.ContainsKey("success"));
            Assert.IsTrue(root.ContainsKey("code"));
            Assert.AreEqual(marker, root.Value<string>("marker"));
        }

        private static PermissionSyncCompatibilityService CreatePermissionService()
        {
            var operations = new FakeLegacyPermissionOperations();
            return new PermissionSyncCompatibilityService(
                new SyncPermissionsCommandHandler(operations),
                new SyncPersonsCommandHandler(operations),
                new DeleteFacesCommandHandler(operations),
                new DeletePersonsCommandHandler(operations),
                new GetFacesQueryHandler(operations),
                new GetEnrollmentTaskStatusQueryHandler(operations),
                new CaptureEnrollmentFaceCommandHandler(operations),
                NullLoggerFacade.Instance,
                logPayloads: false,
                payloadLogMaxChars: 0);
        }

        private static DeviceManagementCompatibilityService CreateDeviceService(string apiKey = null)
        {
            var registry = new FakeDeviceRegistryService();
            return new DeviceManagementCompatibilityService(
                new GetDeviceStatusQueryHandler(registry),
                new AddDeviceCommandHandler(registry),
                new DeleteDeviceCommandHandler(registry),
                new DisconnectDeviceCommandHandler(registry),
                new ReconnectDeviceCommandHandler(registry),
                NullLoggerFacade.Instance,
                logPayloads: false,
                payloadLogMaxChars: 0,
                apiKey: apiKey);
        }

        private sealed class FakeLegacyPermissionOperations : ILegacyPermissionOperations
        {
            public Task<OperationResult> SyncPermissionsAsync(IReadOnlyList<PermissionUpdateCommandItem> items, RequestContext requestContext, CancellationToken cancellationToken)
            {
                return Task.FromResult(OperationResult.Success("ok", "permissions", new { marker = "permissions" }));
            }

            public Task<OperationResult> SyncPersonsAsync(IReadOnlyList<PersonSyncCommandItem> items, RequestContext requestContext, CancellationToken cancellationToken)
            {
                return Task.FromResult(OperationResult.Success("ok", "persons", new { marker = "persons" }));
            }

            public Task<OperationResult> DeleteFacesAsync(IReadOnlyList<string> employeeIds, RequestContext requestContext, CancellationToken cancellationToken)
            {
                return Task.FromResult(OperationResult.Success("ok", "deleteFaces", new { marker = "deleteFaces" }));
            }

            public Task<OperationResult> DeletePersonsAsync(IReadOnlyList<string> employeeIds, RequestContext requestContext, CancellationToken cancellationToken)
            {
                return Task.FromResult(OperationResult.Success("ok", "deletePersons", new { marker = "deletePersons" }));
            }

            public Task<OperationResult> GetFacesAsync(IReadOnlyList<string> employeeIds, RequestContext requestContext, CancellationToken cancellationToken)
            {
                return Task.FromResult(OperationResult.Success("ok", "getFaces", new { marker = "getFaces" }));
            }

            public Task<OperationResult> GetEnrollmentStatusAsync(EnrollmentStatusQuery query, RequestContext requestContext, CancellationToken cancellationToken)
            {
                return Task.FromResult(OperationResult.Success("ok", "status", new { marker = "status" }));
            }

            public Task<IReadOnlyList<OperationResult>> CaptureFaceStreamAsync(CaptureFaceStreamCommand command, RequestContext requestContext, CancellationToken cancellationToken)
            {
                IReadOnlyList<OperationResult> frames = new[]
                {
                    OperationResult.Success("ok", "capture", new { marker = "capture" })
                };
                return Task.FromResult(frames);
            }
        }

        private sealed class FakeDeviceRegistryService : IDeviceRegistryService
        {
            public Task<OperationResult> GetDeviceStatusAsync(DeviceStatusQuery query, RequestContext requestContext, CancellationToken cancellationToken)
            {
                return Task.FromResult(OperationResult.Success("ok", "statusQuery", new { marker = "statusQuery" }));
            }

            public Task<OperationResult> AddDeviceAsync(AddDeviceCommand command, RequestContext requestContext, CancellationToken cancellationToken)
            {
                return Task.FromResult(OperationResult.Success("ok", "addDevice", new { marker = "addDevice" }));
            }

            public Task<OperationResult> DeleteDeviceAsync(DeleteDeviceCommand command, RequestContext requestContext, CancellationToken cancellationToken)
            {
                return Task.FromResult(OperationResult.Success("ok", "deleteDevice", new { marker = "deleteDevice" }));
            }

            public Task<OperationResult> DisconnectDeviceAsync(DisconnectDeviceCommand command, RequestContext requestContext, CancellationToken cancellationToken)
            {
                return Task.FromResult(OperationResult.Success("ok", "disconnectDevice", new { marker = "disconnectDevice" }));
            }

            public Task<OperationResult> ReconnectDeviceAsync(ReconnectDeviceCommand command, RequestContext requestContext, CancellationToken cancellationToken)
            {
                return Task.FromResult(OperationResult.Success("ok", "reconnectDevice", new { marker = "reconnectDevice" }));
            }
        }
    }
}
