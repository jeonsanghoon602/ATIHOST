using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using HostSimulator.Application;
using HostSimulator.Domain;
using Xunit;

namespace HostSimulator.Tests
{
    public class ConnectionTestServiceTests
    {
        private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(5);

        private static ConnectionParameters MakeParams()
        {
            return new ConnectionParameters("127.0.0.1", 5000, true, 0);
        }

        #region StubAdapter

        private class StubAdapter : IProtocolAdapter
        {
            public ProtocolConnectionState State { get; private set; } = ProtocolConnectionState.Disconnected;
            public event EventHandler<ProtocolConnectionState> StateChanged;
            public int ConnectCount { get; private set; }
            public int DisconnectCount { get; private set; }
            public int FailOnCycle { get; set; }
            public bool SimulateTimeout { get; set; }

            public Task ConnectAsync(ConnectionParameters parameters, TimeSpan timeout, CancellationToken cancellationToken = default)
            {
                ConnectCount++;
                if (FailOnCycle > 0 && ConnectCount == FailOnCycle)
                    throw new InvalidOperationException("Simulated failure");
                if (SimulateTimeout)
                    throw new TimeoutException("Simulated timeout");
                State = ProtocolConnectionState.Connected;
                StateChanged?.Invoke(this, State);
                return Task.CompletedTask;
            }

            public Task DisconnectAsync()
            {
                DisconnectCount++;
                State = ProtocolConnectionState.Disconnected;
                StateChanged?.Invoke(this, State);
                return Task.CompletedTask;
            }

            public void Dispose() { }
        }

        private class StubLogger : ILogger
        {
            public List<string> Messages { get; } = new List<string>();
            public void Info(string message) => Messages.Add("[INFO] " + message);
            public void Warn(string message) => Messages.Add("[WARN] " + message);
            public void Error(string message) => Messages.Add("[ERROR] " + message);
        }

        #endregion

        [Fact]
        public async Task SingleCycle_Succeeds()
        {
            var adapter = new StubAdapter();
            var logger = new StubLogger();
            var svc = new ConnectionTestService();

            var result = await svc.RunConnectDisconnectCyclesAsync(
                adapter, MakeParams(), 1, DefaultTimeout, logger);

            Assert.True(result.AllPassed);
            Assert.Equal(1, result.TotalCycles);
            Assert.Equal(1, result.SuccessfulCycles);
            Assert.Equal(0, result.FailedCycles);
            Assert.Empty(result.Errors);
        }

        [Fact]
        public async Task HundredCycles_AllPass()
        {
            var adapter = new StubAdapter();
            var logger = new StubLogger();
            var svc = new ConnectionTestService();

            var result = await svc.RunConnectDisconnectCyclesAsync(
                adapter, MakeParams(), 100, DefaultTimeout, logger);

            Assert.True(result.AllPassed);
            Assert.Equal(100, result.SuccessfulCycles);
            Assert.Equal(100, adapter.ConnectCount);
            Assert.Equal(100, adapter.DisconnectCount);
        }

        [Fact]
        public async Task PartialFailure_ReportsCorrectCounts()
        {
            var adapter = new StubAdapter { FailOnCycle = 3 };
            var logger = new StubLogger();
            var svc = new ConnectionTestService();

            var result = await svc.RunConnectDisconnectCyclesAsync(
                adapter, MakeParams(), 5, DefaultTimeout, logger);

            Assert.False(result.AllPassed);
            Assert.Equal(5, result.TotalCycles);
            Assert.Equal(4, result.SuccessfulCycles);
            Assert.Equal(1, result.FailedCycles);
            Assert.Single(result.Errors);
            Assert.Contains("Cycle 3", result.Errors[0]);
        }

        [Fact]
        public async Task TimeoutFailure_ReportsAsError()
        {
            var adapter = new StubAdapter { SimulateTimeout = true };
            var logger = new StubLogger();
            var svc = new ConnectionTestService();

            var result = await svc.RunConnectDisconnectCyclesAsync(
                adapter, MakeParams(), 3, DefaultTimeout, logger);

            Assert.False(result.AllPassed);
            Assert.Equal(0, result.SuccessfulCycles);
            Assert.Equal(3, result.FailedCycles);
            Assert.Equal(3, result.Errors.Count);
            Assert.Contains("TimeoutException", result.Errors[0]);
        }

        [Fact]
        public async Task NullAdapter_Throws()
        {
            var svc = new ConnectionTestService();
            await Assert.ThrowsAsync<ArgumentNullException>(() =>
                svc.RunConnectDisconnectCyclesAsync(null, MakeParams(), 1, DefaultTimeout, new StubLogger()));
        }

        [Fact]
        public async Task NullParameters_Throws()
        {
            var svc = new ConnectionTestService();
            await Assert.ThrowsAsync<ArgumentNullException>(() =>
                svc.RunConnectDisconnectCyclesAsync(new StubAdapter(), null, 1, DefaultTimeout, new StubLogger()));
        }

        [Fact]
        public async Task ZeroCycles_Throws()
        {
            var svc = new ConnectionTestService();
            await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
                svc.RunConnectDisconnectCyclesAsync(new StubAdapter(), MakeParams(), 0, DefaultTimeout, new StubLogger()));
        }

        [Fact]
        public async Task NullLogger_Throws()
        {
            var svc = new ConnectionTestService();
            await Assert.ThrowsAsync<ArgumentNullException>(() =>
                svc.RunConnectDisconnectCyclesAsync(new StubAdapter(), MakeParams(), 1, DefaultTimeout, null));
        }

        [Fact]
        public async Task NegativeCycleDelay_Throws()
        {
            var svc = new ConnectionTestService();
            await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
                svc.RunConnectDisconnectCyclesAsync(new StubAdapter(), MakeParams(), 1, DefaultTimeout, new StubLogger(), TimeSpan.FromSeconds(-1)));
        }
    }
}
