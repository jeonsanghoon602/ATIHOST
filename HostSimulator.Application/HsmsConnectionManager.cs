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
        private Task _reconnectTask;
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

            try
            {
                await ConnectAdapterAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                await CleanupAdapterAsync().ConfigureAwait(false);
                lock (_lock) { _running = false; }
                SetState(ProtocolConnectionState.Disconnected, StateChangeReason.UserRequest);
                throw;
            }
            catch (Exception ex)
            {
                _logger.Error($"Connection failed: {ex.Message}");
                await CleanupAdapterAsync().ConfigureAwait(false);
                lock (_lock) { _running = false; }
                SetState(ProtocolConnectionState.Disconnected, StateChangeReason.CommunicationError);
            }
        }

        public async Task StopAsync()
        {
            CancellationTokenSource reconnectCts;
            Task reconnectTask;
            IProtocolAdapter adapter;

            lock (_lock)
            {
                if (!_running)
                    return;

                _running = false;
                reconnectCts = _reconnectCts;
                _reconnectCts = null;
                reconnectTask = _reconnectTask;
                _reconnectTask = null;
                adapter = _adapter;
            }

            reconnectCts?.Cancel();

            if (reconnectTask != null)
            {
                try { await reconnectTask.ConfigureAwait(false); }
                catch (OperationCanceledException) { }
                catch (Exception ex) { _logger.Warn($"Reconnect task error on stop: {ex.Message}"); }
            }

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

            var parameters = ConnectionParameters.FromProfile(profile);
            var timeout = TimeSpan.FromSeconds(parameters.LinkTestIntervalSeconds + profile.T6 + 2);
            return adapter.LinktestAsync(timeout, cancellationToken);
        }

        public void Dispose()
        {
            CancellationTokenSource reconnectCts;
            lock (_lock)
            {
                if (_disposed) return;
                _disposed = true;
                _running = false;
                reconnectCts = _reconnectCts;
                _reconnectCts = null;
            }

            reconnectCts?.Cancel();
            reconnectCts?.Dispose();

            if (_adapter != null)
            {
                _adapter.StateChanged -= OnAdapterStateChanged;
                _adapter.Dispose();
                _adapter = null;
            }

            SetState(ProtocolConnectionState.Disconnected, StateChangeReason.Disposed);
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

            await adapter.ConnectAsync(parameters, timeout, cancellationToken).ConfigureAwait(false);
        }

        private async Task CleanupAdapterAsync()
        {
            IProtocolAdapter adapter;
            lock (_lock)
            {
                adapter = _adapter;
                _adapter = null;
            }

            if (adapter != null)
            {
                adapter.StateChanged -= OnAdapterStateChanged;
                try { await adapter.DisconnectAsync().ConfigureAwait(false); }
                catch { /* best-effort cleanup */ }
                adapter.Dispose();
            }
        }

        private void OnAdapterStateChanged(object sender, StateChangedEventArgs e)
        {
            SetState(e.NewState, e.Reason);

            bool shouldReconnect;
            lock (_lock)
            {
                shouldReconnect = _running
                    && _profile != null
                    && _profile.AutoReconnect
                    && e.NewState == ProtocolConnectionState.Disconnected
                    && e.Reason != StateChangeReason.UserRequest
                    && e.Reason != StateChangeReason.Disposed;
            }

            if (shouldReconnect)
            {
                _logger.Info($"Connection lost ({e.Reason}). Scheduling auto-reconnect...");
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
                if (_reconnectTask != null && !_reconnectTask.IsCompleted)
                {
                    _logger.Info("Reconnect already scheduled, skipping duplicate.");
                    return;
                }

                intervalSeconds = _profile.ReconnectIntervalSeconds;
                reconnectCts = _reconnectCts;
                if (reconnectCts == null || reconnectCts.IsCancellationRequested)
                    return;

                _reconnectTask = RunReconnectAsync(intervalSeconds, reconnectCts.Token);
            }
        }

        private async Task RunReconnectAsync(int intervalSeconds, CancellationToken cancellationToken)
        {
            bool shouldRetry = false;
            try
            {
                _logger.Info($"Reconnecting in {intervalSeconds} seconds...");
                await Task.Delay(TimeSpan.FromSeconds(intervalSeconds), cancellationToken).ConfigureAwait(false);

                await CleanupAdapterAsync().ConfigureAwait(false);
                await ConnectAdapterAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                _logger.Info("Auto-reconnect cancelled.");
            }
            catch (Exception ex)
            {
                _logger.Error($"Auto-reconnect failed: {ex.Message}");
                shouldRetry = true;
            }
            finally
            {
                lock (_lock) { _reconnectTask = null; }
                if (shouldRetry)
                    ScheduleReconnect();
            }
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
