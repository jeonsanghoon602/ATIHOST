using System;
using System.Threading;
using System.Threading.Tasks;
using HostSimulator.Domain;

namespace HostSimulator.Application
{
    public sealed class HsmsConnectionManager : IHsmsConnectionManager
    {
        private readonly Func<IProtocolAdapter> _adapterFactory;
        private readonly ILogger _logger;
        private readonly object _lock = new object();

        private IProtocolAdapter _adapter;
        private EquipmentProfile _profile;
        private CancellationTokenSource _reconnectCts;
        private bool _disposed;
        private bool _running;

        public ProtocolConnectionState State { get; private set; } = ProtocolConnectionState.Disconnected;

        public event EventHandler<StateChangedEventArgs> StateChanged;

        public HsmsConnectionManager(Func<IProtocolAdapter> adapterFactory, ILogger logger)
        {
            _adapterFactory = adapterFactory ?? throw new ArgumentNullException(nameof(adapterFactory));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task StartAsync(EquipmentProfile profile, CancellationToken cancellationToken = default)
        {
            if (profile == null)
                throw new ArgumentNullException(nameof(profile));

            lock (_lock)
            {
                if (_disposed)
                    throw new ObjectDisposedException(nameof(HsmsConnectionManager));
                if (_running)
                    throw new InvalidOperationException("Already running. Call StopAsync first.");

                _profile = profile;
                _running = true;
                _reconnectCts = new CancellationTokenSource();
            }

            _logger.Info($"Starting HSMS connection: {profile.ToSafeLogString()}");

            await ConnectWithRetryAsync(cancellationToken).ConfigureAwait(false);
        }

        public async Task StopAsync()
        {
            CancellationTokenSource reconnectCts;
            IProtocolAdapter adapter;

            lock (_lock)
            {
                if (!_running)
                    return;

                _running = false;
                reconnectCts = _reconnectCts;
                _reconnectCts = null;
                adapter = _adapter;
            }

            reconnectCts?.Cancel();
            reconnectCts?.Dispose();

            if (adapter != null)
            {
                try
                {
                    await adapter.DisconnectAsync().ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    _logger.Warn($"Error during disconnect: {ex.Message}");
                }
                finally
                {
                    adapter.StateChanged -= OnAdapterStateChanged;
                    adapter.Dispose();
                    lock (_lock) { _adapter = null; }
                }
            }

            SetState(ProtocolConnectionState.Disconnected, StateChangeReason.UserRequest);
            _logger.Info("HSMS connection stopped.");
        }

        public Task<bool> LinktestAsync(CancellationToken cancellationToken = default)
        {
            IProtocolAdapter adapter;
            EquipmentProfile profile;

            lock (_lock)
            {
                if (_disposed)
                    throw new ObjectDisposedException(nameof(HsmsConnectionManager));
                adapter = _adapter;
                profile = _profile;
            }

            if (adapter == null || profile == null)
                return Task.FromResult(false);

            var timeout = TimeSpan.FromSeconds(profile.T6);
            return adapter.LinktestAsync(timeout, cancellationToken);
        }

        public void Dispose()
        {
            lock (_lock)
            {
                if (_disposed) return;
                _disposed = true;
                _running = false;
            }

            _reconnectCts?.Cancel();
            _reconnectCts?.Dispose();

            if (_adapter != null)
            {
                _adapter.StateChanged -= OnAdapterStateChanged;
                _adapter.Dispose();
                _adapter = null;
            }

            SetState(ProtocolConnectionState.Disconnected, StateChangeReason.Disposed);
        }

        private async Task ConnectWithRetryAsync(CancellationToken cancellationToken)
        {
            CancellationTokenSource reconnectCts;

            lock (_lock)
            {
                reconnectCts = _reconnectCts;
            }

            if (reconnectCts == null || reconnectCts.IsCancellationRequested)
                return;

            using (var linked = CancellationTokenSource.CreateLinkedTokenSource(
                cancellationToken, reconnectCts.Token))
            {
                await ConnectAdapterAsync(linked.Token).ConfigureAwait(false);
            }
        }

        private async Task ConnectAdapterAsync(CancellationToken cancellationToken)
        {
            EquipmentProfile profile;
            lock (_lock) { profile = _profile; }

            var parameters = ConnectionParameters.FromProfile(profile);
            var timeout = TimeSpan.FromSeconds(profile.T7 + profile.T5 + 5);

            var adapter = _adapterFactory();
            adapter.StateChanged += OnAdapterStateChanged;

            lock (_lock) { _adapter = adapter; }

            try
            {
                await adapter.ConnectAsync(parameters, timeout, cancellationToken).ConfigureAwait(false);
            }
            catch (TimeoutException ex)
            {
                _logger.Error($"Connection timeout: {ex.Message}");
                HandleConnectionLoss(StateChangeReason.T7Timeout);
                return;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.Error($"Connection failed: {ex.Message}");
                HandleConnectionLoss(StateChangeReason.CommunicationError);
            }
        }

        private void OnAdapterStateChanged(object sender, StateChangedEventArgs e)
        {
            SetState(e.NewState, e.Reason);

            if (e.NewState == ProtocolConnectionState.Disconnected
                || (e.NewState == ProtocolConnectionState.Connecting
                    && e.Reason == StateChangeReason.ReconnectAttempt))
            {
                bool shouldReconnect;
                lock (_lock)
                {
                    shouldReconnect = _running && _profile != null && _profile.AutoReconnect
                        && e.NewState == ProtocolConnectionState.Disconnected
                        && e.Reason != StateChangeReason.UserRequest
                        && e.Reason != StateChangeReason.Disposed;
                }

                if (shouldReconnect)
                {
                    _logger.Info("Connection lost. Scheduling auto-reconnect...");
                    ScheduleReconnect();
                }
            }
        }

        private void HandleConnectionLoss(StateChangeReason reason)
        {
            bool shouldReconnect;

            lock (_lock)
            {
                shouldReconnect = _running && _profile != null && _profile.AutoReconnect;
            }

            SetState(ProtocolConnectionState.Disconnected, reason);

            if (shouldReconnect)
            {
                _logger.Info($"Connection lost ({reason}). Scheduling auto-reconnect...");
                ScheduleReconnect();
            }
        }

        private void ScheduleReconnect()
        {
            int intervalSeconds;
            CancellationTokenSource reconnectCts;

            lock (_lock)
            {
                if (!_running || _profile == null)
                    return;
                intervalSeconds = _profile.ReconnectIntervalSeconds;
                reconnectCts = _reconnectCts;
            }

            if (reconnectCts == null || reconnectCts.IsCancellationRequested)
                return;

            Task.Run(async () =>
            {
                try
                {
                    _logger.Info($"Reconnecting in {intervalSeconds} seconds...");
                    await Task.Delay(TimeSpan.FromSeconds(intervalSeconds), reconnectCts.Token).ConfigureAwait(false);

                    // Dispose old adapter
                    IProtocolAdapter oldAdapter;
                    lock (_lock) { oldAdapter = _adapter; _adapter = null; }
                    if (oldAdapter != null)
                    {
                        oldAdapter.StateChanged -= OnAdapterStateChanged;
                        oldAdapter.Dispose();
                    }

                    await ConnectAdapterAsync(reconnectCts.Token).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    _logger.Info("Auto-reconnect cancelled.");
                }
                catch (Exception ex)
                {
                    _logger.Error($"Auto-reconnect failed: {ex.Message}");
                }
            });
        }

        private void SetState(ProtocolConnectionState newState, StateChangeReason reason)
        {
            var previous = State;
            if (previous == newState)
                return;
            State = newState;
            StateChanged?.Invoke(this, new StateChangedEventArgs(previous, newState, reason));
        }
    }
}
