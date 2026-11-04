using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using HostSimulator.Domain;

namespace HostSimulator.Application
{
    public class ConnectionTestService
    {
        public async Task<ConnectionTestResult> RunConnectDisconnectCyclesAsync(
            IProtocolAdapter adapter,
            ConnectionParameters parameters,
            int cycles,
            TimeSpan connectionTimeout,
            ILogger logger,
            TimeSpan cycleDelay = default)
        {
            if (adapter == null) throw new ArgumentNullException(nameof(adapter));
            if (parameters == null) throw new ArgumentNullException(nameof(parameters));
            if (cycles < 1) throw new ArgumentOutOfRangeException(nameof(cycles), cycles, "Cycle count must be at least 1.");
            if (logger == null) throw new ArgumentNullException(nameof(logger));
            if (cycleDelay < TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(cycleDelay), "Cycle delay must be non-negative.");

            var errors = new List<string>();
            int successCount = 0;

            for (int i = 1; i <= cycles; i++)
            {
                try
                {
                    await adapter.ConnectAsync(parameters, connectionTimeout).ConfigureAwait(false);
                    await adapter.DisconnectAsync().ConfigureAwait(false);
                    successCount++;
                }
                catch (Exception ex)
                {
                    errors.Add($"Cycle {i}: {ex.GetType().Name} - {ex.Message}");
                    logger.Error($"Cycle {i} failed: {ex}");

                    try { await adapter.DisconnectAsync().ConfigureAwait(false); }
                    catch (Exception cleanupEx)
                    {
                        logger.Warn($"Cycle {i} cleanup failed: {cleanupEx.Message}");
                    }
                }

                if (i < cycles && cycleDelay > TimeSpan.Zero)
                    await Task.Delay(cycleDelay).ConfigureAwait(false);
            }

            var result = new ConnectionTestResult(cycles, successCount, errors);
            logger.Info($"Connect/disconnect test completed: {successCount}/{cycles} passed");
            return result;
        }
    }
}
