using System;

namespace ControlEntradaSalida.Host.WindowsService
{
    public static class HostModeResolver
    {
        public static HostRunMode Resolve(bool isUserInteractive, string[] args)
        {
            if (isUserInteractive || Array.Exists(args ?? Array.Empty<string>(), value => string.Equals(value, "--console", StringComparison.OrdinalIgnoreCase)))
            {
                return HostRunMode.Interactive;
            }

            return HostRunMode.Service;
        }
    }
}
