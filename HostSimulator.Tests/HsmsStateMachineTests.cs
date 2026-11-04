using System.Collections.Generic;
using HostSimulator.Domain;
using Xunit;

namespace HostSimulator.Tests
{
    public class HsmsStateMachineTests
    {
        private class StubLogger : ILogger
        {
            public List<string> Messages { get; } = new List<string>();
            public void Info(string message) => Messages.Add("[INFO] " + message);
            public void Warn(string message) => Messages.Add("[WARN] " + message);
            public void Error(string message) => Messages.Add("[ERROR] " + message);
        }

        private HsmsStateMachine CreateMachine() => new HsmsStateMachine(new StubLogger());

        [Fact]
        public void Initial_state_is_Disconnected()
        {
            var sm = CreateMachine();
            Assert.Equal(ProtocolConnectionState.Disconnected, sm.CurrentState);
        }

        [Fact]
        public void Disconnected_to_Connecting_is_valid()
        {
            var sm = CreateMachine();
            var result = sm.TryTransition(ProtocolConnectionState.Connecting, StateChangeReason.UserRequest);
            Assert.True(result);
            Assert.Equal(ProtocolConnectionState.Connecting, sm.CurrentState);
        }

        [Fact]
        public void Connecting_to_Connected_is_valid()
        {
            var sm = CreateMachine();
            sm.TryTransition(ProtocolConnectionState.Connecting, StateChangeReason.UserRequest);
            var result = sm.TryTransition(ProtocolConnectionState.Connected, StateChangeReason.ConnectionEstablished);
            Assert.True(result);
            Assert.Equal(ProtocolConnectionState.Connected, sm.CurrentState);
        }

        [Fact]
        public void Connected_to_Selected_is_valid()
        {
            var sm = CreateMachine();
            sm.TryTransition(ProtocolConnectionState.Connecting, StateChangeReason.UserRequest);
            sm.TryTransition(ProtocolConnectionState.Connected, StateChangeReason.ConnectionEstablished);
            var result = sm.TryTransition(ProtocolConnectionState.Selected, StateChangeReason.SelectCompleted);
            Assert.True(result);
            Assert.Equal(ProtocolConnectionState.Selected, sm.CurrentState);
        }

        [Fact]
        public void Selected_to_Disconnecting_is_valid()
        {
            var sm = CreateMachine();
            sm.TryTransition(ProtocolConnectionState.Connecting, StateChangeReason.UserRequest);
            sm.TryTransition(ProtocolConnectionState.Connected, StateChangeReason.ConnectionEstablished);
            sm.TryTransition(ProtocolConnectionState.Selected, StateChangeReason.SelectCompleted);
            var result = sm.TryTransition(ProtocolConnectionState.Disconnecting, StateChangeReason.UserRequest);
            Assert.True(result);
            Assert.Equal(ProtocolConnectionState.Disconnecting, sm.CurrentState);
        }

        [Fact]
        public void Disconnecting_to_Disconnected_is_valid()
        {
            var sm = CreateMachine();
            sm.TryTransition(ProtocolConnectionState.Connecting, StateChangeReason.UserRequest);
            sm.TryTransition(ProtocolConnectionState.Connected, StateChangeReason.ConnectionEstablished);
            sm.TryTransition(ProtocolConnectionState.Selected, StateChangeReason.SelectCompleted);
            sm.TryTransition(ProtocolConnectionState.Disconnecting, StateChangeReason.UserRequest);
            var result = sm.TryTransition(ProtocolConnectionState.Disconnected, StateChangeReason.UserRequest);
            Assert.True(result);
            Assert.Equal(ProtocolConnectionState.Disconnected, sm.CurrentState);
        }

        [Fact]
        public void Disconnected_to_Connected_is_invalid()
        {
            var sm = CreateMachine();
            var result = sm.TryTransition(ProtocolConnectionState.Connected, StateChangeReason.ConnectionEstablished);
            Assert.False(result);
            Assert.Equal(ProtocolConnectionState.Disconnected, sm.CurrentState);
        }

        [Fact]
        public void Disconnected_to_Selected_is_invalid()
        {
            var sm = CreateMachine();
            var result = sm.TryTransition(ProtocolConnectionState.Selected, StateChangeReason.SelectCompleted);
            Assert.False(result);
            Assert.Equal(ProtocolConnectionState.Disconnected, sm.CurrentState);
        }

        [Fact]
        public void Connecting_to_Selected_is_invalid()
        {
            var sm = CreateMachine();
            sm.TryTransition(ProtocolConnectionState.Connecting, StateChangeReason.UserRequest);
            var result = sm.TryTransition(ProtocolConnectionState.Selected, StateChangeReason.SelectCompleted);
            Assert.False(result);
            Assert.Equal(ProtocolConnectionState.Connecting, sm.CurrentState);
        }

        [Fact]
        public void Connected_to_Connecting_is_invalid()
        {
            var sm = CreateMachine();
            sm.TryTransition(ProtocolConnectionState.Connecting, StateChangeReason.UserRequest);
            sm.TryTransition(ProtocolConnectionState.Connected, StateChangeReason.ConnectionEstablished);
            var result = sm.TryTransition(ProtocolConnectionState.Connecting, StateChangeReason.ReconnectAttempt);
            Assert.False(result);
        }

        [Fact]
        public void Connecting_to_Disconnected_is_valid()
        {
            var sm = CreateMachine();
            sm.TryTransition(ProtocolConnectionState.Connecting, StateChangeReason.UserRequest);
            var result = sm.TryTransition(ProtocolConnectionState.Disconnected, StateChangeReason.CommunicationError);
            Assert.True(result);
            Assert.Equal(ProtocolConnectionState.Disconnected, sm.CurrentState);
        }

        [Fact]
        public void Selected_to_Disconnected_is_valid_on_error()
        {
            var sm = CreateMachine();
            sm.TryTransition(ProtocolConnectionState.Connecting, StateChangeReason.UserRequest);
            sm.TryTransition(ProtocolConnectionState.Connected, StateChangeReason.ConnectionEstablished);
            sm.TryTransition(ProtocolConnectionState.Selected, StateChangeReason.SelectCompleted);
            var result = sm.TryTransition(ProtocolConnectionState.Disconnected, StateChangeReason.CommunicationError);
            Assert.True(result);
            Assert.Equal(ProtocolConnectionState.Disconnected, sm.CurrentState);
        }

        [Fact]
        public void ForceState_changes_regardless_of_validity()
        {
            var sm = CreateMachine();
            sm.ForceState(ProtocolConnectionState.Selected, StateChangeReason.SelectCompleted);
            Assert.Equal(ProtocolConnectionState.Selected, sm.CurrentState);
        }

        [Fact]
        public void ForceState_same_state_does_not_fire_event()
        {
            var sm = CreateMachine();
            var eventFired = false;
            sm.StateChanged += (s, e) => eventFired = true;
            sm.ForceState(ProtocolConnectionState.Disconnected, StateChangeReason.None);
            Assert.False(eventFired);
        }

        [Fact]
        public void StateChanged_event_includes_reason()
        {
            var sm = CreateMachine();
            StateChangedEventArgs received = null;
            sm.StateChanged += (s, e) => received = e;

            sm.TryTransition(ProtocolConnectionState.Connecting, StateChangeReason.UserRequest);

            Assert.NotNull(received);
            Assert.Equal(ProtocolConnectionState.Disconnected, received.PreviousState);
            Assert.Equal(ProtocolConnectionState.Connecting, received.NewState);
            Assert.Equal(StateChangeReason.UserRequest, received.Reason);
        }

        [Fact]
        public void Full_lifecycle_Disconnect_Connecting_Connected_Selected_Disconnecting_Disconnected()
        {
            var sm = CreateMachine();
            var states = new List<ProtocolConnectionState>();
            sm.StateChanged += (s, e) => states.Add(e.NewState);

            Assert.True(sm.TryTransition(ProtocolConnectionState.Connecting, StateChangeReason.UserRequest));
            Assert.True(sm.TryTransition(ProtocolConnectionState.Connected, StateChangeReason.ConnectionEstablished));
            Assert.True(sm.TryTransition(ProtocolConnectionState.Selected, StateChangeReason.SelectCompleted));
            Assert.True(sm.TryTransition(ProtocolConnectionState.Disconnecting, StateChangeReason.UserRequest));
            Assert.True(sm.TryTransition(ProtocolConnectionState.Disconnected, StateChangeReason.UserRequest));

            Assert.Equal(5, states.Count);
            Assert.Equal(ProtocolConnectionState.Connecting, states[0]);
            Assert.Equal(ProtocolConnectionState.Connected, states[1]);
            Assert.Equal(ProtocolConnectionState.Selected, states[2]);
            Assert.Equal(ProtocolConnectionState.Disconnecting, states[3]);
            Assert.Equal(ProtocolConnectionState.Disconnected, states[4]);
        }

        [Fact]
        public void All_state_changes_are_logged()
        {
            var logger = new StubLogger();
            var sm = new HsmsStateMachine(logger);

            sm.TryTransition(ProtocolConnectionState.Connecting, StateChangeReason.UserRequest);
            sm.TryTransition(ProtocolConnectionState.Connected, StateChangeReason.ConnectionEstablished);

            Assert.True(logger.Messages.Count >= 2);
            Assert.Contains(logger.Messages, m => m.Contains("Connecting") && m.Contains("UserRequest"));
            Assert.Contains(logger.Messages, m => m.Contains("Connected") && m.Contains("ConnectionEstablished"));
        }

        [Fact]
        public void Invalid_transition_is_logged_as_warning()
        {
            var logger = new StubLogger();
            var sm = new HsmsStateMachine(logger);

            sm.TryTransition(ProtocolConnectionState.Selected, StateChangeReason.SelectCompleted);

            Assert.Contains(logger.Messages, m => m.Contains("[WARN]") && m.Contains("Invalid"));
        }

        [Theory]
        [InlineData(ProtocolConnectionState.Disconnected, ProtocolConnectionState.Connecting, true)]
        [InlineData(ProtocolConnectionState.Disconnected, ProtocolConnectionState.Connected, false)]
        [InlineData(ProtocolConnectionState.Disconnected, ProtocolConnectionState.Selected, false)]
        [InlineData(ProtocolConnectionState.Disconnected, ProtocolConnectionState.Disconnecting, false)]
        [InlineData(ProtocolConnectionState.Connecting, ProtocolConnectionState.Connected, true)]
        [InlineData(ProtocolConnectionState.Connecting, ProtocolConnectionState.Disconnected, true)]
        [InlineData(ProtocolConnectionState.Connecting, ProtocolConnectionState.Selected, false)]
        [InlineData(ProtocolConnectionState.Connected, ProtocolConnectionState.Selected, true)]
        [InlineData(ProtocolConnectionState.Connected, ProtocolConnectionState.Disconnecting, true)]
        [InlineData(ProtocolConnectionState.Connected, ProtocolConnectionState.Disconnected, true)]
        [InlineData(ProtocolConnectionState.Connected, ProtocolConnectionState.Connecting, false)]
        [InlineData(ProtocolConnectionState.Selected, ProtocolConnectionState.Disconnecting, true)]
        [InlineData(ProtocolConnectionState.Selected, ProtocolConnectionState.Disconnected, true)]
        [InlineData(ProtocolConnectionState.Selected, ProtocolConnectionState.Connecting, false)]
        [InlineData(ProtocolConnectionState.Disconnecting, ProtocolConnectionState.Disconnected, true)]
        [InlineData(ProtocolConnectionState.Disconnecting, ProtocolConnectionState.Connecting, false)]
        public void IsValidTransition_covers_all_cases(ProtocolConnectionState from, ProtocolConnectionState to, bool expected)
        {
            Assert.Equal(expected, HsmsStateMachine.IsValidTransition(from, to));
        }

        [Fact]
        public void StateChangedEventArgs_ToString_is_readable()
        {
            var args = new StateChangedEventArgs(
                ProtocolConnectionState.Connected,
                ProtocolConnectionState.Selected,
                StateChangeReason.SelectCompleted);

            var str = args.ToString();
            Assert.Contains("Connected", str);
            Assert.Contains("Selected", str);
            Assert.Contains("SelectCompleted", str);
        }
    }
}
