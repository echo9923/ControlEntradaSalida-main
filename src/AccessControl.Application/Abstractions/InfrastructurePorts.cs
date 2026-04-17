namespace ControlEntradaSalida.Application.Abstractions
{
    public interface IDeviceRepository
    {
    }

    public interface IUserRepository
    {
    }

    public interface IDeviceWorkStateRepository
    {
    }

    public interface IHikvisionDeviceGateway
    {
    }

    public interface ILoggerFacade
    {
        void Info(string message);

        void Warn(string message);

        void Debug(string message);

        void Error(string message, System.Exception exception = null);
    }

    public interface IConfigurationProvider
    {
        RuntimeServiceConfiguration Current { get; }
    }
}
