using System;
using System.Collections.Generic;
using HostSimulator.Domain;
using Xunit;

namespace HostSimulator.Tests
{
    public class EquipmentProfileTests
    {
        private EquipmentProfile CreateValid(
            string profileName = "EQ01",
            string ipAddress = "192.168.1.100",
            int port = 5000,
            bool isActive = true,
            int deviceId = 0,
            int t3 = 45,
            int t5 = 10,
            int t6 = 5,
            int t7 = 10,
            int t8 = 5,
            bool autoReconnect = true,
            int reconnectIntervalSeconds = 10)
        {
            return new EquipmentProfile(
                profileName, ipAddress, port, isActive, deviceId,
                t3, t5, t6, t7, t8, autoReconnect, reconnectIntervalSeconds);
        }

        [Fact]
        public void Valid_profile_is_created_successfully()
        {
            var profile = CreateValid();

            Assert.Equal("EQ01", profile.ProfileName);
            Assert.Equal("192.168.1.100", profile.IpAddress);
            Assert.Equal(5000, profile.Port);
            Assert.True(profile.IsActive);
            Assert.Equal(0, profile.DeviceId);
            Assert.Equal(45, profile.T3);
            Assert.Equal(10, profile.T5);
            Assert.Equal(5, profile.T6);
            Assert.Equal(10, profile.T7);
            Assert.Equal(5, profile.T8);
            Assert.True(profile.AutoReconnect);
            Assert.Equal(10, profile.ReconnectIntervalSeconds);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void Missing_profile_name_is_rejected(string name)
        {
            var ex = Assert.Throws<ProfileValidationException>(() => CreateValid(profileName: name));
            Assert.Contains("ProfileName is required", ex.Message);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void Missing_ip_address_is_rejected(string ip)
        {
            var ex = Assert.Throws<ProfileValidationException>(() => CreateValid(ipAddress: ip));
            Assert.Contains("IpAddress is required", ex.Message);
        }

        [Fact]
        public void Invalid_ip_address_is_rejected()
        {
            var ex = Assert.Throws<ProfileValidationException>(() => CreateValid(ipAddress: "999.999.999.999"));
            Assert.Contains("not a valid IP address", ex.Message);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        [InlineData(65536)]
        public void Port_out_of_range_is_rejected(int port)
        {
            var ex = Assert.Throws<ProfileValidationException>(() => CreateValid(port: port));
            Assert.Contains("Port must be between 1 and 65535", ex.Message);
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(65536)]
        public void DeviceId_out_of_range_is_rejected(int deviceId)
        {
            var ex = Assert.Throws<ProfileValidationException>(() => CreateValid(deviceId: deviceId));
            Assert.Contains("DeviceId must be between 0 and 65535", ex.Message);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(121)]
        public void T3_out_of_range_is_rejected(int t3)
        {
            var ex = Assert.Throws<ProfileValidationException>(() => CreateValid(t3: t3));
            Assert.Contains("T3 must be between 1 and 120", ex.Message);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(241)]
        public void T5_out_of_range_is_rejected(int t5)
        {
            var ex = Assert.Throws<ProfileValidationException>(() => CreateValid(t5: t5));
            Assert.Contains("T5 must be between 1 and 240", ex.Message);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(241)]
        public void T6_out_of_range_is_rejected(int t6)
        {
            var ex = Assert.Throws<ProfileValidationException>(() => CreateValid(t6: t6));
            Assert.Contains("T6 must be between 1 and 240", ex.Message);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(241)]
        public void T7_out_of_range_is_rejected(int t7)
        {
            var ex = Assert.Throws<ProfileValidationException>(() => CreateValid(t7: t7));
            Assert.Contains("T7 must be between 1 and 240", ex.Message);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(121)]
        public void T8_out_of_range_is_rejected(int t8)
        {
            var ex = Assert.Throws<ProfileValidationException>(() => CreateValid(t8: t8));
            Assert.Contains("T8 must be between 1 and 120", ex.Message);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(601)]
        public void ReconnectInterval_out_of_range_is_rejected(int interval)
        {
            var ex = Assert.Throws<ProfileValidationException>(() => CreateValid(reconnectIntervalSeconds: interval));
            Assert.Contains("ReconnectIntervalSeconds must be between 1 and 600", ex.Message);
        }

        [Fact]
        public void Multiple_errors_are_collected()
        {
            var ex = Assert.Throws<ProfileValidationException>(() =>
                new EquipmentProfile("", "invalid", 0, true, -1, 0, 0, 0, 0, 0, true, 0));

            Assert.True(ex.Errors.Count >= 2);
        }

        [Fact]
        public void ToSafeLogString_excludes_ip_and_port()
        {
            var profile = CreateValid();
            var log = profile.ToSafeLogString();

            Assert.DoesNotContain("192.168.1.100", log);
            Assert.DoesNotContain("5000", log);
            Assert.Contains("EQ01", log);
        }

        [Fact]
        public void ReconnectInterval_less_than_T5_is_rejected()
        {
            var ex = Assert.Throws<ProfileValidationException>(() =>
                CreateValid(t5: 10, reconnectIntervalSeconds: 5));
            Assert.Contains("ReconnectIntervalSeconds (5) must be >= T5 (10)", ex.Message);
        }

        [Fact]
        public void ReconnectInterval_equal_to_T5_is_accepted()
        {
            var profile = CreateValid(t5: 10, reconnectIntervalSeconds: 10);
            Assert.Equal(10, profile.ReconnectIntervalSeconds);
        }

        [Fact]
        public void DeviceId_65535_is_accepted()
        {
            var profile = CreateValid(deviceId: 65535);
            Assert.Equal(65535, profile.DeviceId);
        }

        [Fact]
        public void Boundary_values_are_accepted()
        {
            var profile = new EquipmentProfile(
                "Boundary", "0.0.0.0", 1, false, 0,
                1, 1, 1, 1, 1, false, 1);
            Assert.Equal("Boundary", profile.ProfileName);

            var profileMax = new EquipmentProfile(
                "Max", "255.255.255.255", 65535, true, 65535,
                120, 240, 240, 240, 120, true, 600);
            Assert.Equal("Max", profileMax.ProfileName);
        }
    }
}
