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

        public event EventHandler<ProtocolConnectionState> StateChanged;

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
            // RunContinuationsAsynchronously: Secs4Net fires ConnectionChanged mid-state-transition; inline continuation would Disconnect before the SDK finishes its timer cleanup.
            var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

            lock (_lock)
            {
                if (_disposed)
                    throw new ObjectDisposedException(nameof(Secs4NetAdapter));
                if (_connection != null)
                    throw new InvalidOperationException("Already connected. Disconnect first.");

                SetState(ProtocolConnectionState.Connecting);

                var options = Options.Create(new SecsGemOptions
                {
                    IpAddress = parameters.IpAddress.ToString(),
                    Port = parameters.Port,
                    IsActive = parameters.IsActive,
                    DeviceId = (ushort)parameters.DeviceId,
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

            SetState(ProtocolConnectionState.Disconnecting);

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

            SetState(ProtocolConnectionState.Disconnected);
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

            SetState(ProtocolConnectionState.Disconnected);
        }

        private void OnConnectionChanged(object sender, ConnectionState state)
        {
            _logger.Info("ConnectionChanged: " + state);
            SetState(MapState(state));
        }

        internal static ProtocolConnectionState MapState(ConnectionState secsState)
        {
            switch (secsState)
            {
                case ConnectionState.Connecting:
                case ConnectionState.Retry:
                    return ProtocolConnectionState.Connecting;
                case ConnectionState.Connected:
                case ConnectionState.Selected:
                    return ProtocolConnectionState.Connected;
                default:
                    return ProtocolConnectionState.Disconnected;
            }
        }

        private void SetState(ProtocolConnectionState newState)
        {
            State = newState;
            StateChanged?.Invoke(this, newState);
        }
    }
}
