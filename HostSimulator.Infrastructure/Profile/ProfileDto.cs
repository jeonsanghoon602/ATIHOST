using Newtonsoft.Json;

namespace HostSimulator.Infrastructure.Profile
{
    internal sealed class ProfileDto
    {
        [JsonProperty("profileName")]
        public string ProfileName { get; set; }

        [JsonProperty("ipAddress")]
        public string IpAddress { get; set; }

        [JsonProperty("port")]
        public int Port { get; set; }

        [JsonProperty("isActive")]
        public bool IsActive { get; set; }

        [JsonProperty("deviceId")]
        public int DeviceId { get; set; }

        [JsonProperty("t3")]
        public int T3 { get; set; } = 45;

        [JsonProperty("t5")]
        public int T5 { get; set; } = 10;

        [JsonProperty("t6")]
        public int T6 { get; set; } = 5;

        [JsonProperty("t7")]
        public int T7 { get; set; } = 10;

        [JsonProperty("t8")]
        public int T8 { get; set; } = 5;

        [JsonProperty("autoReconnect")]
        public bool AutoReconnect { get; set; } = true;

        [JsonProperty("reconnectIntervalSeconds")]
        public int ReconnectIntervalSeconds { get; set; } = 10;
    }
}
