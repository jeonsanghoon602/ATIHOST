using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using HostSimulator.Application;
using HostSimulator.Domain;
using Xunit;

namespace HostSimulator.Tests
{
    public class HsmsConnectionManagerTests
    {
        private static EquipmentProfile DefaultProfile(bool autoReconnect = false, int reconnectInterval = 10)
        {
            return new EquipmentProfile(
                "TestEQ", "127.0.0.1", 5000, true, 0,
                45, 10, 5, 10, 5, autoReconnect, reconnectInterval);
        }

        private class StubLogger : ILogger
        {
            public List<string> Messages { get; } = new List<string>();
            public void Info(string message) => Messages.Add("[INFO] " + message);
            public void Warn(string message) => Messages.Add("[WARN] " + message);
            public void Error(string message) => Messages.Add("[ERROR] " + message);
        }

        private class StubAdapter : IProtocolAdapter
        {
            public ProtocolConnectionState State { get; private set; } = ProtocolConnectionState.Disconnected;
            public event EventHandler<StateChangedEventArgs> StateChanged;
            public int ConnectCount { get; private set; }
            public int DisconnectCount { get; private set; }
            public bool FailConnect { get; set; }
            public bool TimeoutConnect { get; set; }
            public bool LinktestResult { get; set; } = true;
            public bool DelayCancelConnect { get; set; }

            public async Task ConnectAsync(ConnectionParameters parameters, TimeSpan timeout, CancellationToken cancellationToken = default)
            {
                ConnectCount++;
                if (DelayCancelConnect)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    await Task.Delay(TimeSpan.FromSeconds(30), cancellationToken).ConfigureAwait(false);
                }
                if (TimeoutConnect)
                    throw new TimeoutException("Simulated timeout");
                if (FailConnect)
                    throw new InvalidOperationException("Simulated failure");

                var prev = State;
                State = ProtocolConnectionState.Selected;
                StateChanged?.Invoke(this, new StateChangedEventArgs(prev, ProtocolConnectionState.Connecting, StateChangeReason.UserRequest));
                StateChanged?.Invoke(this, new StateChangedEventArgs(ProtocolConnectionState.Connecting, ProtocolConnectionState.Connected, StateChangeReason.ConnectionEstablished));
                StateChanged?.Invoke(this, new StateChangedEventArgs(ProtocolConnectionState.Connected, ProtocolConnectionState.Selected, StateChangeReason.SelectCompleted));
                return;
            }

            public Task DisconnectAsync()
            {
                DisconnectCount++;
                var prev = State;
                State = ProtocolConnectionState.Disconnected;
                StateChanged?.Invoke(this, new StateChangedEventArgs(prev, ProtocolConnectionState.Disconnecting, StateChangeReason.UserRequest));
                StateChanged?.Invoke(this, new StateChangedEventArgs(ProtocolConnectionState.Disconnecting, ProtocolConnectionState.Disconnected, StateChangeReason.UserRequest));
                return Task.CompletedTask;
            }

            public Task<bool> LinktestAsync(TimeSpan timeout, CancellationToken cancellationToken = default)
            {
                return Task.FromResult(LinktestResult);
            }

            public void SimulateConnectionLoss()
            {
                var prev = State;
                State = ProtocolConnectionState.Disconnected;
                StateChanged?.Invoke(this, new StateChangedEventArgs(prev, ProtocolConnectionState.Disconnected, StateChangeReason.CommunicationError));
            }

            public void Dispose() { }
        }

        [Fact]
        public async Task StartAsync_connects_successfully()
        {
            StubAdapter adapter = null;
            var manager = new HsmsConnectionManager(
                () => { adapter = new StubAdapter(); return adapter; },
                new StubLogger());

            await manager.StartAsync(DefaultProfile());

            Assert.NotNull(adapter);
            Assert.Equal(1, adapter.ConnectCount);
            Assert.Equal(ProtocolConnectionState.Selected, manager.State);
        }

        [Fact]
        public async Task StopAsync_disconnects()
        {
            StubAdapter adapter = null;
            var manager = new HsmsConnectionManager(
                () => { adapter = new StubAdapter(); return adapter; },
                new StubLogger());

            await manager.StartAsync(DefaultProfile());
            await manager.StopAsync();

            Assert.Equal(ProtocolConnectionState.Disconnected, manager.State);
        }

        [Fact]
        public async Task StateChanged_fires_through_lifecycle()
        {
            var states = new List<ProtocolConnectionState>();
            var manager = new HsmsConnectionManager(
                () => new StubAdapter(),
                new StubLogger());
            manager.StateChanged += (s, e) => states.Add(e.NewState);

            await manager.StartAsync(DefaultProfile());

            Assert.Contains(ProtocolConnectionState.Selected, states);
        }

        [Fact]
        public async Task LinktestAsync_returns_true_when_selected()
        {
            var manager = new HsmsConnectionManager(
                () => new StubAdapter(),
                new StubLogger());

            await manager.StartAsync(DefaultProfile());
            var result = await manager.LinktestAsync();

            Assert.True(result);
        }

        [Fact]
        public async Task LinktestAsync_returns_false_when_not_started()
        {
            var manager = new HsmsConnectionManager(
                () => new StubAdapter(),
                new StubLogger());

            var result = await manager.LinktestAsync();

            Assert.False(result);
        }

        [Fact]
        public async Task StartAsync_twice_throws()
        {
            var manager = new HsmsConnectionManager(
                () => new StubAdapter(),
                new StubLogger());

            await manager.StartAsync(DefaultProfile());

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                manager.StartAsync(DefaultProfile()));
        }

        [Fact]
        public async Task StopAsync_when_not_started_is_noop()
        {
            var manager = new HsmsConnectionManager(
                () => new StubAdapter(),
                new StubLogger());

            await manager.StopAsync();

            Assert.Equal(ProtocolConnectionState.Disconnected, manager.State);
        }

        [Fact]
        public async Task Connection_timeout_does_not_crash()
        {
            var manager = new HsmsConnectionManager(
                () => new StubAdapter { TimeoutConnect = true },
                new StubLogger());

            await manager.StartAsync(DefaultProfile());

            Assert.Equal(ProtocolConnectionState.Disconnected, manager.State);
        }

        [Fact]
        public async Task Connection_failure_does_not_crash()
        {
            var manager = new HsmsConnectionManager(
                () => new StubAdapter { FailConnect = true },
                new StubLogger());

            await manager.StartAsync(DefaultProfile());

            Assert.Equal(ProtocolConnectionState.Disconnected, manager.State);
        }

        [Fact]
        public void Dispose_cleans_up()
        {
            var manager = new HsmsConnectionManager(
                () => new StubAdapter(),
                new StubLogger());

            manager.Dispose();

            Assert.Equal(ProtocolConnectionState.Disconnected, manager.State);
        }

        [Fact]
        public async Task Dispose_after_start_cleans_up()
        {
            var manager = new HsmsConnectionManager(
                () => new StubAdapter(),
                new StubLogger());

            await manager.StartAsync(DefaultProfile());
            manager.Dispose();

            Assert.Equal(ProtocolConnectionState.Disconnected, manager.State);
        }

        [Fact]
        public async Task StartAsync_after_dispose_throws()
        {
            var manager = new HsmsConnectionManager(
                () => new StubAdapter(),
                new StubLogger());

            manager.Dispose();

            await Assert.ThrowsAsync<ObjectDisposedException>(() =>
                manager.StartAsync(DefaultProfile()));
        }

        [Fact]
        public void NullAdapterFactory_throws()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new HsmsConnectionManager(null, new StubLogger()));
        }

        [Fact]
        public void NullLogger_throws()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new HsmsConnectionManager(() => new StubAdapter(), null));
        }

        [Fact]
        public async Task NullProfile_throws()
        {
            var manager = new HsmsConnectionManager(
                () => new StubAdapter(),
                new StubLogger());

            await Assert.ThrowsAsync<ArgumentNullException>(() =>
                manager.StartAsync(null));
        }

        [Fact]
        public async Task State_change_reasons_are_logged()
        {
            var logger = new StubLogger();
            var manager = new HsmsConnectionManager(
                () => new StubAdapter(),
                logger);

            await manager.StartAsync(DefaultProfile());

            Assert.Contains(logger.Messages, m => m.Contains("Starting HSMS connection"));
        }

        [Fact]
        public async Task StopAsync_logs_stopped()
        {
            var logger = new StubLogger();
            var manager = new HsmsConnectionManager(
                () => new StubAdapter(),
                logger);

            await manager.StartAsync(DefaultProfile());
            await manager.StopAsync();

            Assert.Contains(logger.Messages, m => m.Contains("stopped"));
        }

        [Fact]
        public async Task AutoReconnect_schedules_on_connection_loss()
        {
            var adapterCount = 0;
            var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            var logger = new StubLogger();

            StubAdapter currentAdapter = null;
            var manager = new HsmsConnectionManager(
                () =>
                {
                    adapterCount++;
                    currentAdapter = new StubAdapter();
                    if (adapterCount >= 2)
                        tcs.TrySetResult(true);
                    return currentAdapter;
                },
                logger);

            var profile = DefaultProfile(autoReconnect: true, reconnectInterval: 10);
            await manager.StartAsync(profile);

            Assert.Equal(1, adapterCount);
            currentAdapter.SimulateConnectionLoss();

            using (var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15)))
            {
                cts.Token.Register(() => tcs.TrySetCanceled());
                await tcs.Task;
            }

            Assert.True(adapterCount >= 2, $"Expected reconnect to create a new adapter, but count was {adapterCount}");

            await manager.StopAsync();
        }

        [Fact]
        public async Task Duplicate_disconnect_events_do_not_schedule_multiple_reconnects()
        {
            var adapterCount = 0;
            var logger = new StubLogger();
            StubAdapter currentAdapter = null;
            var manager = new HsmsConnectionManager(
                () => { adapterCount++; currentAdapter = new StubAdapter(); return currentAdapter; },
                logger);

            var profile = DefaultProfile(autoReconnect: true, reconnectInterval: 60);
            await manager.StartAsync(profile);
            Assert.Equal(1, adapterCount);

            currentAdapter.SimulateConnectionLoss();
            currentAdapter.SimulateConnectionLoss();

            await Task.Delay(200);

            var skipCount = 0;
            foreach (var m in logger.Messages)
            {
                if (m.Contains("already scheduled"))
                    skipCount++;
            }

            Assert.True(skipCount >= 1, "Second disconnect should have been skipped as duplicate");

            await manager.StopAsync();
        }

        [Fact]
        public async Task StopAsync_cancels_pending_reconnect()
        {
            var logger = new StubLogger();
            StubAdapter currentAdapter = null;
            var manager = new HsmsConnectionManager(
                () => { currentAdapter = new StubAdapter(); return currentAdapter; },
                logger);

            var profile = DefaultProfile(autoReconnect: true, reconnectInterval: 60);
            await manager.StartAsync(profile);

            currentAdapter.SimulateConnectionLoss();
            await Task.Delay(50);

            await manager.StopAsync();

            Assert.Equal(ProtocolConnectionState.Disconnected, manager.State);
            Assert.Contains(logger.Messages, m => m.Contains("cancelled") || m.Contains("stopped"));
        }

        [Fact]
        public async Task Concurrent_StopAsync_calls_are_safe()
        {
            StubAdapter currentAdapter = null;
            var manager = new HsmsConnectionManager(
                () => { currentAdapter = new StubAdapter(); return currentAdapter; },
                new StubLogger());

            await manager.StartAsync(DefaultProfile());

            var stop1 = manager.StopAsync();
            var stop2 = manager.StopAsync();

            await Task.WhenAll(stop1, stop2);

            Assert.Equal(ProtocolConnectionState.Disconnected, manager.State);
        }

        [Fact]
        public async Task Start_after_stop_works()
        {
            var manager = new HsmsConnectionManager(
                () => new StubAdapter(),
                new StubLogger());

            await manager.StartAsync(DefaultProfile());
            await manager.StopAsync();
            await manager.StartAsync(DefaultProfile());

            Assert.Equal(ProtocolConnectionState.Selected, manager.State);

            await manager.StopAsync();
        }

        [Fact]
        public async Task StartAsync_cancellation_propagates()
        {
            using (var cts = new CancellationTokenSource())
            {
                cts.Cancel();

                var manager = new HsmsConnectionManager(
                    () => new StubAdapter { DelayCancelConnect = true },
                    new StubLogger());

                await Assert.ThrowsAsync<OperationCanceledException>(() =>
                    manager.StartAsync(DefaultProfile(), cts.Token));

                Assert.Equal(ProtocolConnectionState.Disconnected, manager.State);
            }
        }
    }
}
