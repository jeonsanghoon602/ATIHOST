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
        public int T3Seconds { get; }
        public int T5Seconds { get; }
        public int T6Seconds { get; }
        public int T7Seconds { get; }
        public int T8Seconds { get; }
        public int LinkTestIntervalSeconds { get; }

        public ConnectionParameters(string ipAddress, int port, bool isActive, int deviceId)
            : this(ipAddress, port, isActive, deviceId, 45, 10, 5, 10, 5, 60)
        {
        }

        public ConnectionParameters(
            string ipAddress,
            int port,
            bool isActive,
            int deviceId,
            int t3Seconds,
            int t5Seconds,
            int t6Seconds,
            int t7Seconds,
            int t8Seconds,
            int linkTestIntervalSeconds = 60)
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
            T3Seconds = t3Seconds;
            T5Seconds = t5Seconds;
            T6Seconds = t6Seconds;
            T7Seconds = t7Seconds;
            T8Seconds = t8Seconds;
            LinkTestIntervalSeconds = linkTestIntervalSeconds;
        }

        public static ConnectionParameters FromProfile(EquipmentProfile profile)
        {
            if (profile == null)
                throw new ArgumentNullException(nameof(profile));

            return new ConnectionParameters(
                profile.IpAddress,
                profile.Port,
                profile.IsActive,
                profile.DeviceId,
                profile.T3,
                profile.T5,
                profile.T6,
                profile.T7,
                profile.T8);
        }
    }
}
