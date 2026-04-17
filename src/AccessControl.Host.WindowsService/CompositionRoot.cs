using ControlEntradaSalida.Application.Devices;
using ControlEntradaSalida.Application.Faces;
using ControlEntradaSalida.Application.People;
using ControlEntradaSalida.Application.Permissions;
using ControlEntradaSalida.Compatibility.Grpc;
using ControlEntradaSalida.Infrastructure.Hikvision;
using ControlEntradaSalida.Infrastructure.Observability;
using ControlEntradaSalida.Infrastructure.Security;
using Grpc.Core;

namespace ControlEntradaSalida.Host.WindowsService
{
    public static class CompositionRoot
    {
        public static CompositeServiceRuntime CreateRuntime()
        {
            var configurationProvider = new LegacyConfigurationProvider();
            var runtimeBridge = new global::ControlEntradaSalida.LegacyRuntimeBridge();
            var logger = new LegacyLoggerFacade();
            var retryStore = new global::ControlEntradaSalida.DeviceOperationRetryStore();
            var permissionManager = new global::ControlEntradaSalida.PermissionRefreshManager(retryStore);
            var permissionOperations = new LegacyPermissionOperationsAdapter(permissionManager);
            var deviceRegistry = new LegacyDeviceRegistryService(global::ControlEntradaSalida.DeviceConnectionManager.Instance);

            var permissionSyncService = new PermissionSyncCompatibilityService(
                new SyncPermissionsCommandHandler(permissionOperations),
                new SyncPersonsCommandHandler(permissionOperations),
                new DeleteFacesCommandHandler(permissionOperations),
                new DeletePersonsCommandHandler(permissionOperations),
                new GetFacesQueryHandler(permissionOperations),
                new GetEnrollmentTaskStatusQueryHandler(permissionOperations),
                new CaptureEnrollmentFaceCommandHandler(permissionOperations),
                logger,
                configurationProvider.Current.LogGrpcPayloads,
                configurationProvider.Current.GrpcPayloadLogMaxChars);

            var deviceManagementService = new DeviceManagementCompatibilityService(
                new GetDeviceStatusQueryHandler(deviceRegistry),
                new AddDeviceCommandHandler(deviceRegistry),
                new DeleteDeviceCommandHandler(deviceRegistry),
                new DisconnectDeviceCommandHandler(deviceRegistry),
                new ReconnectDeviceCommandHandler(deviceRegistry),
                logger,
                configurationProvider.Current.LogGrpcPayloads,
                configurationProvider.Current.GrpcPayloadLogMaxChars,
                configurationProvider.Current.GrpcManagementApiKey);

            var grpcServer = new Server();
            grpcServer.Services.Add(permissionSyncService.BuildServiceDefinition());
            grpcServer.Services.Add(deviceManagementService.BuildServiceDefinition());
            grpcServer.Ports.Add(new ServerPort("0.0.0.0", configurationProvider.Current.GrpcListenPort, ServerCredentials.Insecure));

            return new CompositeServiceRuntime(new IHostedComponent[]
            {
                new LegacyRuntimeComponent(configurationProvider.Current, permissionManager, retryStore, runtimeBridge, logger),
                new GrpcServerHostedComponent(grpcServer)
            });
        }
    }
}
