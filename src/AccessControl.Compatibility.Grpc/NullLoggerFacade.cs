using ControlEntradaSalida.Application.Abstractions;

namespace ControlEntradaSalida.Compatibility.Grpc
{
    public sealed class NullLoggerFacade : ILoggerFacade
    {
        public static NullLoggerFacade Instance { get; } = new NullLoggerFacade();

        private NullLoggerFacade()
        {
        }

        public void Info(string message)
        {
        }

        public void Warn(string message)
        {
        }

        public void Debug(string message)
        {
        }

        public void Error(string message, System.Exception exception = null)
        {
        }
    }
}
