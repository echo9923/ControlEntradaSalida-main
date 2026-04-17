namespace ControlEntradaSalida.Host.WindowsService
{
    public interface IHostedComponent
    {
        void Start();

        void Stop();
    }
}
