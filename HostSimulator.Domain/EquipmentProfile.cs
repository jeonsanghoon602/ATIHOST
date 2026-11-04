using System;
using System.Collections.Generic;
using System.Net;

namespace HostSimulator.Domain
{
    public sealed class EquipmentProfile
    {
        public string ProfileName { get; }
        public string IpAddress { get; }
        public int Port { get; }
        public bool IsActive { get; }
        public int DeviceId { get; }
        public int T3 { get; }
        public int T5 { get; }
        public int T6 { get; }
        public int T7 { get; }
        public int T8 { get; }
        public bool AutoReconnect { get; }
        public int ReconnectIntervalSeconds { get; }

        public EquipmentProfile(
            string profileName,
            string ipAddress,
            int port,
            bool isActive,
            int deviceId,
            int t3,
            int t5,
            int t6,
            int t7,
            int t8,
            bool autoReconnect,
            int reconnectIntervalSeconds)
        {
            var errors = Validate(profileName, ipAddress, port, deviceId, t3, t5, t6, t7, t8, reconnectIntervalSeconds);
            if (errors.Count > 0)
                throw new ProfileValidationException(errors);

            ProfileName = profileName;
            IpAddress = ipAddress;
            Port = port;
            IsActive = isActive;
            DeviceId = deviceId;
            T3 = t3;
            T5 = t5;
            T6 = t6;
            T7 = t7;
            T8 = t8;
            AutoReconnect = autoReconnect;
            ReconnectIntervalSeconds = reconnectIntervalSeconds;
        }

        public static List<string> Validate(
            string profileName,
            string ipAddress,
            int port,
            int deviceId,
            int t3,
            int t5,
            int t6,
            int t7,
            int t8,
            int reconnectIntervalSeconds)
        {
            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(profileName))
                errors.Add("ProfileName is required.");

            if (string.IsNullOrWhiteSpace(ipAddress))
                errors.Add("IpAddress is required.");
            else if (!IPAddress.TryParse(ipAddress, out _))
                errors.Add($"IpAddress is not a valid IP address: {ipAddress}");

            if (port < 1 || port > 65535)
                errors.Add($"Port must be between 1 and 65535, but was {port}.");

            if (deviceId < 0 || deviceId > 65535)
                errors.Add($"DeviceId must be between 0 and 65535, but was {deviceId}.");

            // SEMI E37 standard timer ranges
            ValidateTimer(errors, nameof(T3), t3, 1, 120);
            ValidateTimer(errors, nameof(T5), t5, 1, 240);
            ValidateTimer(errors, nameof(T6), t6, 1, 240);
            ValidateTimer(errors, nameof(T7), t7, 1, 240);
            ValidateTimer(errors, nameof(T8), t8, 1, 120);

            if (reconnectIntervalSeconds < 1 || reconnectIntervalSeconds > 600)
                errors.Add($"ReconnectIntervalSeconds must be between 1 and 600, but was {reconnectIntervalSeconds}.");

            if (reconnectIntervalSeconds < t5 && reconnectIntervalSeconds >= 1 && t5 >= 1 && t5 <= 240)
                errors.Add($"ReconnectIntervalSeconds ({reconnectIntervalSeconds}) must be >= T5 ({t5}).");

            return errors;
        }

        private static void ValidateTimer(List<string> errors, string name, int value, int min, int max)
        {
            if (value < min || value > max)
                errors.Add($"{name} must be between {min} and {max}, but was {value}.");
        }

        public string ToSafeLogString()
        {
            return $"Profile={ProfileName}, Active={IsActive}, DeviceId={DeviceId}";
        }
    }
}
