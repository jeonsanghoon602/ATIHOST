using System;

namespace HostSimulator.Domain
{
    public sealed class StateChangedEventArgs : EventArgs
    {
        public ProtocolConnectionState PreviousState { get; }
        public ProtocolConnectionState NewState { get; }
        public StateChangeReason Reason { get; }

        public StateChangedEventArgs(
            ProtocolConnectionState previousState,
            ProtocolConnectionState newState,
            StateChangeReason reason)
        {
            PreviousState = previousState;
            NewState = newState;
            Reason = reason;
        }

        public override string ToString()
        {
            return $"{PreviousState} -> {NewState} ({Reason})";
        }
    }
}
