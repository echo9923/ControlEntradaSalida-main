using Grpc.Core;

namespace ControlEntradaSalida.Host.WindowsService
{
    public sealed class GrpcServerHostedComponent : IHostedComponent
    {
        private readonly Server grpcServer;

        public GrpcServerHostedComponent(Server grpcServer)
        {
            this.grpcServer = grpcServer;
        }

        public void Start()
        {
            grpcServer.Start();
        }

        public void Stop()
        {
            grpcServer.ShutdownAsync().GetAwaiter().GetResult();
        }
    }
}
