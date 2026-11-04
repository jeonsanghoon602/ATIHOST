using System;
using System.Net;

namespace HostSimulator.Domain
{
    public sealed class ConnectionParameters
    {
        public IPAddress IpAddress { get; }
        public int Port { get; }
        public bool IsActive { get; }
        public int DeviceId { get; }

        public ConnectionParameters(string ipAddress, int port, bool isActive, int deviceId)
        {
            if (string.IsNullOrWhiteSpace(ipAddress))
                throw new ArgumentException("IP address must not be null or empty.", nameof(ipAddress));

            if (!IPAddress.TryParse(ipAddress, out var parsed))
                throw new ArgumentException($"Invalid IP address: {ipAddress}", nameof(ipAddress));

            if (port < 1 || port > 65535)
                throw new ArgumentOutOfRangeException(nameof(port), port, "Port must be between 1 and 65535.");

            if (deviceId < 0 || deviceId > 65535)
                throw new ArgumentOutOfRangeException(nameof(deviceId), deviceId, "Device ID must be between 0 and 65535.");

            IpAddress = parsed;
            Port = port;
            IsActive = isActive;
            DeviceId = deviceId;
        }
    }
}
