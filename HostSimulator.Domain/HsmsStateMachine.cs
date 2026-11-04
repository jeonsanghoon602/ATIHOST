using System;

namespace HostSimulator.Domain
{
    public sealed class HsmsStateMachine
    {
        private readonly object _lock = new object();
        private readonly ILogger _logger;

        public ProtocolConnectionState CurrentState { get; private set; } = ProtocolConnectionState.Disconnected;

        public event EventHandler<StateChangedEventArgs> StateChanged;

        public HsmsStateMachine(ILogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public bool TryTransition(ProtocolConnectionState target, StateChangeReason reason)
        {
            lock (_lock)
            {
                if (!IsValidTransition(CurrentState, target))
                {
                    _logger.Warn($"Invalid state transition: {CurrentState} -> {target} ({reason})");
                    return false;
                }

                var previous = CurrentState;
                CurrentState = target;
                _logger.Info($"State: {previous} -> {target} ({reason})");
                StateChanged?.Invoke(this, new StateChangedEventArgs(previous, target, reason));
                return true;
            }
        }

        public void ForceState(ProtocolConnectionState target, StateChangeReason reason)
        {
            lock (_lock)
            {
                var previous = CurrentState;
                if (previous == target)
                    return;

                CurrentState = target;
                _logger.Info($"State forced: {previous} -> {target} ({reason})");
                StateChanged?.Invoke(this, new StateChangedEventArgs(previous, target, reason));
            }
        }

        public static bool IsValidTransition(ProtocolConnectionState from, ProtocolConnectionState to)
        {
            switch (from)
            {
                case ProtocolConnectionState.Disconnected:
                    return to == ProtocolConnectionState.Connecting;

                case ProtocolConnectionState.Connecting:
                    return to == ProtocolConnectionState.Connected
                        || to == ProtocolConnectionState.Disconnected;

                case ProtocolConnectionState.Connected:
                    return to == ProtocolConnectionState.Selected
                        || to == ProtocolConnectionState.Disconnecting
                        || to == ProtocolConnectionState.Disconnected
                        || to == ProtocolConnectionState.Connecting;

                case ProtocolConnectionState.Selected:
                    return to == ProtocolConnectionState.Disconnecting
                        || to == ProtocolConnectionState.Disconnected
                        || to == ProtocolConnectionState.Connecting;

                case ProtocolConnectionState.Disconnecting:
                    return to == ProtocolConnectionState.Disconnected;

                default:
                    return false;
            }
        }
    }
}
