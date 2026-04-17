using System.Collections.Generic;

namespace ControlEntradaSalida.Application.Models
{
    public sealed class DeviceStatusQuery
    {
        public DeviceStatusQuery(bool includeDisabled, bool refresh, int? deviceId, IReadOnlyList<int> deviceIds, string ipAddress)
        {
            IncludeDisabled = includeDisabled;
            Refresh = refresh;
            DeviceId = deviceId;
            DeviceIds = deviceIds;
            IpAddress = ipAddress;
        }

        public bool IncludeDisabled { get; }

        public bool Refresh { get; }

        public int? DeviceId { get; }

        public IReadOnlyList<int> DeviceIds { get; }

        public string IpAddress { get; }
    }

    public sealed class AddDeviceCommand
    {
        public int DeviceId { get; set; }

        public string DeviceName { get; set; }

        public string IpAddress { get; set; }

        public string Port { get; set; }

        public string Username { get; set; }

        public string Password { get; set; }

        public string Description { get; set; }

        public bool Enabled { get; set; }

        public bool ConnectNow { get; set; }
    }

    public sealed class DeleteDeviceCommand
    {
        public int DeviceId { get; set; }

        public bool DisconnectFirst { get; set; }
    }

    public sealed class DisconnectDeviceCommand
    {
        public int DeviceId { get; set; }
    }

    public sealed class ReconnectDeviceCommand
    {
        public int DeviceId { get; set; }

        public bool Force { get; set; }
    }
}
