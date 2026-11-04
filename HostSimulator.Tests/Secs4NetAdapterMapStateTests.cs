using HostSimulator.Domain;
using HostSimulator.Protocol;
using Secs4Net;
using Xunit;

namespace HostSimulator.Tests
{
    public class Secs4NetAdapterMapStateTests
    {
        [Fact]
        public void Connecting_maps_to_Connecting()
        {
            Assert.Equal(ProtocolConnectionState.Connecting, Secs4NetAdapter.MapState(ConnectionState.Connecting));
        }

        [Fact]
        public void Retry_maps_to_Connecting()
        {
            Assert.Equal(ProtocolConnectionState.Connecting, Secs4NetAdapter.MapState(ConnectionState.Retry));
        }

        [Fact]
        public void Connected_maps_to_Connected()
        {
            Assert.Equal(ProtocolConnectionState.Connected, Secs4NetAdapter.MapState(ConnectionState.Connected));
        }

        [Fact]
        public void Selected_maps_to_Selected()
        {
            Assert.Equal(ProtocolConnectionState.Selected, Secs4NetAdapter.MapState(ConnectionState.Selected));
        }

        [Fact]
        public void Connecting_reason_is_UserRequest()
        {
            Assert.Equal(StateChangeReason.UserRequest,
                Secs4NetAdapter.MapReason(ConnectionState.Connecting, ProtocolConnectionState.Disconnected));
        }

        [Fact]
        public void Connected_reason_is_ConnectionEstablished()
        {
            Assert.Equal(StateChangeReason.ConnectionEstablished,
                Secs4NetAdapter.MapReason(ConnectionState.Connected, ProtocolConnectionState.Connecting));
        }

        [Fact]
        public void Selected_reason_is_SelectCompleted()
        {
            Assert.Equal(StateChangeReason.SelectCompleted,
                Secs4NetAdapter.MapReason(ConnectionState.Selected, ProtocolConnectionState.Connected));
        }

        [Fact]
        public void Retry_from_Connected_is_SelectTimeout()
        {
            Assert.Equal(StateChangeReason.SelectTimeout,
                Secs4NetAdapter.MapReason(ConnectionState.Retry, ProtocolConnectionState.Connected));
        }

        [Fact]
        public void Retry_from_Selected_is_LinktestFailed()
        {
            Assert.Equal(StateChangeReason.LinktestFailed,
                Secs4NetAdapter.MapReason(ConnectionState.Retry, ProtocolConnectionState.Selected));
        }

        [Fact]
        public void Retry_from_Connecting_is_ReconnectAttempt()
        {
            Assert.Equal(StateChangeReason.ReconnectAttempt,
                Secs4NetAdapter.MapReason(ConnectionState.Retry, ProtocolConnectionState.Connecting));
        }
    }
}
