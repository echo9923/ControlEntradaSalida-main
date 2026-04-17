using ControlEntradaSalida.Application.Abstractions;

namespace ControlEntradaSalida.Infrastructure.Observability
{
    public sealed class LegacyLoggerFacade : ILoggerFacade
    {
        public void Info(string message)
        {
            ControlEntradaSalida.ServiceLogger.Info(message);
        }

        public void Warn(string message)
        {
            ControlEntradaSalida.ServiceLogger.Warn(message);
        }

        public void Debug(string message)
        {
            ControlEntradaSalida.ServiceLogger.Debug(message);
        }

        public void Error(string message, System.Exception exception = null)
        {
            ControlEntradaSalida.ServiceLogger.Error(message, exception);
        }
    }
}
