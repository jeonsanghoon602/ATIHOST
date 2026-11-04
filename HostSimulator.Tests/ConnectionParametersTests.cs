using System;
using HostSimulator.Domain;
using Xunit;

namespace HostSimulator.Tests
{
    public class ConnectionParametersTests
    {
        [Fact]
        public void ValidParameters_CreatesInstance()
        {
            var p = new ConnectionParameters("127.0.0.1", 5000, true, 0);

            Assert.Equal("127.0.0.1", p.IpAddress.ToString());
            Assert.Equal(5000, p.Port);
            Assert.True(p.IsActive);
            Assert.Equal(0, p.DeviceId);
        }

        [Fact]
        public void PassiveMode_CreatesInstance()
        {
            var p = new ConnectionParameters("192.0.2.1", 5000, false, 1);
            Assert.False(p.IsActive);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void NullOrWhitespaceIp_Throws(string ip)
        {
            Assert.ThrowsAny<ArgumentException>(() =>
                new ConnectionParameters(ip, 5000, true, 0));
        }

        [Theory]
        [InlineData("not-an-ip")]
        [InlineData("999.999.999.999")]
        [InlineData("abc.def.ghi.jkl")]
        public void InvalidIpFormat_Throws(string ip)
        {
            Assert.Throws<ArgumentException>(() =>
                new ConnectionParameters(ip, 5000, true, 0));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        [InlineData(65536)]
        public void InvalidPort_Throws(int port)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new ConnectionParameters("127.0.0.1", port, true, 0));
        }

        [Theory]
        [InlineData(1)]
        [InlineData(65535)]
        public void BoundaryPorts_Succeed(int port)
        {
            var p = new ConnectionParameters("127.0.0.1", port, true, 0);
            Assert.Equal(port, p.Port);
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(65536)]
        public void InvalidDeviceId_Throws(int deviceId)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new ConnectionParameters("127.0.0.1", 5000, true, deviceId));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(65535)]
        public void BoundaryDeviceIds_Succeed(int deviceId)
        {
            var p = new ConnectionParameters("127.0.0.1", 5000, true, deviceId);
            Assert.Equal(deviceId, p.DeviceId);
        }
    }
}
