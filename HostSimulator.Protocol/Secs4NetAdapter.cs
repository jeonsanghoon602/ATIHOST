using System;
using System.Threading;
using System.Threading.Tasks;
using HostSimulator.Domain;
using Microsoft.Extensions.Options;
using Secs4Net;

namespace HostSimulator.Protocol
{
    public sealed class Secs4NetAdapter : IProtocolAdapter
    {
        private readonly ISecsGemLogger _secsLogger;
        private readonly Domain.ILogger _logger;
        private HsmsConnection _connection;
        private CancellationTokenSource _lifetimeCts;
        private readonly object _lock = new object();
        private bool _disposed;

        public ProtocolConnectionState State { get; private set; } = ProtocolConnectionState.Disconnected;

        public event EventHandler<StateChangedEventArgs> StateChanged;

        public Secs4NetAdapter(Domain.ILogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _secsLogger = new Secs4NetLoggerBridge(logger);
        }

        public async Task ConnectAsync(
            ConnectionParameters parameters,
            TimeSpan timeout,
            CancellationToken cancellationToken = default)
        {
            if (parameters == null)
                throw new ArgumentNullException(nameof(parameters));
            if (timeout <= TimeSpan.Zero)
                throw new ArgumentOutOfRangeException(nameof(timeout), "Timeout must be greater than zero.");

            HsmsConnection conn;
            CancellationTokenSource lifetimeCts;
            var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

            lock (_lock)
            {
                if (_disposed)
                    throw new ObjectDisposedException(nameof(Secs4NetAdapter));
                if (_connection != null)
                    throw new InvalidOperationException("Already connected. Disconnect first.");

                SetState(ProtocolConnectionState.Connecting, StateChangeReason.UserRequest);

                var options = Options.Create(new SecsGemOptions
                {
                    IpAddress = parameters.IpAddress.ToString(),
                    Port = parameters.Port,
                    IsActive = parameters.IsActive,
                    DeviceId = (ushort)parameters.DeviceId,
                    T3 = parameters.T3Seconds * 1000,
                    T5 = parameters.T5Seconds * 1000,
                    T6 = parameters.T6Seconds * 1000,
                    T7 = parameters.T7Seconds * 1000,
                    T8 = parameters.T8Seconds * 1000,
                    LinkTestInterval = parameters.LinkTestIntervalSeconds * 1000,
                });

                lifetimeCts = new CancellationTokenSource();
                conn = new HsmsConnection(options, _secsLogger);
                conn.ConnectionChanged += OnConnectionChanged;
                _connection = conn;
                _lifetimeCts = lifetimeCts;
            }

            void onConnected(object s, ConnectionState state)
            {
                if (state == ConnectionState.Selected)
                    tcs.TrySetResult(true);
            }

            conn.ConnectionChanged += onConnected;
            try
            {
                conn.Start(lifetimeCts.Token);

                if (conn.State == ConnectionState.Selected)
                    return;

                using (var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken))
                {
                    timeoutCts.CancelAfter(timeout);
                    using (timeoutCts.Token.Register(() => tcs.TrySetCanceled()))
                    {
                        await tcs.Task.ConfigureAwait(false);
                    }
                }
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                throw new TimeoutException(
                    $"Connection did not reach Selected state within {timeout.TotalSeconds}s.");
            }
            finally
            {
                conn.ConnectionChanged -= onConnected;
            }
        }

        public async Task DisconnectAsync()
        {
            HsmsConnection conn;
            CancellationTokenSource lifetimeCts;

            lock (_lock)
            {
                conn = _connection;
                _connection = null;
                lifetimeCts = _lifetimeCts;
                _lifetimeCts = null;
            }

            SetState(ProtocolConnectionState.Disconnecting, StateChangeReason.UserRequest);

            if (lifetimeCts != null)
            {
                lifetimeCts.Cancel();
                lifetimeCts.Dispose();
            }

            if (conn != null)
            {
                conn.ConnectionChanged -= OnConnectionChanged;
                await conn.DisposeAsync().ConfigureAwait(false);
            }

            SetState(ProtocolConnectionState.Disconnected, StateChangeReason.UserRequest);
        }

        public Task<bool> LinktestAsync(TimeSpan timeout, CancellationToken cancellationToken = default)
        {
            HsmsConnection conn;
            lock (_lock)
            {
                if (_disposed)
                    throw new ObjectDisposedException(nameof(Secs4NetAdapter));
                conn = _connection;
            }

            if (conn == null || State != ProtocolConnectionState.Selected)
            {
                _logger.Warn("Linktest requested but not in Selected state.");
                return Task.FromResult(false);
            }

            // Secs4Net 2.4.4 handles Linktest internally via LinkTestEnabled.
            // Verify the connection is alive by confirming the SDK state is Selected.
            var sdkSelected = conn.State == ConnectionState.Selected;
            if (sdkSelected)
            {
                conn.LinkTestEnabled = true;
                _logger.Info("Linktest: connection is Selected, periodic LinkTest is enabled.");
            }
            else
            {
                _logger.Warn($"Linktest: SDK state is {conn.State}, not Selected.");
            }

            return Task.FromResult(sdkSelected);
        }

        public void Dispose()
        {
            HsmsConnection conn;
            CancellationTokenSource lifetimeCts;

            lock (_lock)
            {
                if (_disposed) return;
                _disposed = true;
                conn = _connection;
                _connection = null;
                lifetimeCts = _lifetimeCts;
                _lifetimeCts = null;
            }

            if (lifetimeCts != null)
            {
                lifetimeCts.Cancel();
                lifetimeCts.Dispose();
            }

            if (conn != null)
            {
                conn.ConnectionChanged -= OnConnectionChanged;
                conn.DisposeAsync().AsTask().GetAwaiter().GetResult();
            }

            SetState(ProtocolConnectionState.Disconnected, StateChangeReason.Disposed);
        }

        private void OnConnectionChanged(object sender, ConnectionState state)
        {
            var mapped = MapState(state);
            var reason = MapReason(state);
            _logger.Info($"ConnectionChanged: {state} -> {mapped} ({reason})");
            SetState(mapped, reason);

            if (state == ConnectionState.Selected)
            {
                var conn = _connection;
                if (conn != null)
                    conn.LinkTestEnabled = true;
            }
        }

        public static ProtocolConnectionState MapState(ConnectionState secsState)
        {
            switch (secsState)
            {
                case ConnectionState.Connecting:
                    return ProtocolConnectionState.Connecting;
                case ConnectionState.Retry:
                    return ProtocolConnectionState.Connecting;
                case ConnectionState.Connected:
                    return ProtocolConnectionState.Connected;
                case ConnectionState.Selected:
                    return ProtocolConnectionState.Selected;
                default:
                    return ProtocolConnectionState.Disconnected;
            }
        }

        public static StateChangeReason MapReason(ConnectionState secsState)
        {
            switch (secsState)
            {
                case ConnectionState.Connecting:
                    return StateChangeReason.UserRequest;
                case ConnectionState.Connected:
                    return StateChangeReason.ConnectionEstablished;
                case ConnectionState.Selected:
                    return StateChangeReason.SelectCompleted;
                case ConnectionState.Retry:
                    return StateChangeReason.ReconnectAttempt;
                default:
                    return StateChangeReason.CommunicationError;
            }
        }

        private void SetState(ProtocolConnectionState newState, StateChangeReason reason)
        {
            var previous = State;
            if (previous == newState)
                return;
            State = newState;
            _logger.Info($"State: {previous} -> {newState} ({reason})");
            StateChanged?.Invoke(this, new StateChangedEventArgs(previous, newState, reason));
        }
    }
}
