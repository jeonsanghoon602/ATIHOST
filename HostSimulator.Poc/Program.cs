using System;
using System.Threading.Tasks;
using HostSimulator.Application;
using HostSimulator.Domain;
using HostSimulator.Infrastructure.Logging;
using HostSimulator.Protocol;

namespace HostSimulator.Poc
{
    class Program
    {
        static async Task<int> Main(string[] args)
        {
            if (args.Length < 7)
            {
                Console.WriteLine("HOST-003 SDK PoC — Secs4Net 2.4.4 connect/disconnect test");
                Console.WriteLine();
                Console.WriteLine("Usage: HostSimulator.Poc <ip> <port> <active|passive> <deviceId> <cycles> <timeoutSeconds> <cycleDelaySeconds>");
                Console.WriteLine();
                Console.WriteLine("Example:");
                Console.WriteLine("  HostSimulator.Poc 127.0.0.1 5000 active 0 100 10 3");
                return 1;
            }

            string ip = args[0];

            if (!int.TryParse(args[1], out int port))
            {
                Console.Error.WriteLine("Invalid port: " + args[1]);
                return 1;
            }

            string modeArg = args[2].ToLowerInvariant();
            if (modeArg != "active" && modeArg != "passive")
            {
                Console.Error.WriteLine("Mode must be 'active' or 'passive': " + args[2]);
                return 1;
            }
            bool isActive = modeArg == "active";

            if (!int.TryParse(args[3], out int deviceId))
            {
                Console.Error.WriteLine("Invalid device ID: " + args[3]);
                return 1;
            }

            if (!int.TryParse(args[4], out int cycles) || cycles < 1)
            {
                Console.Error.WriteLine("Cycles must be >= 1: " + args[4]);
                return 1;
            }

            if (!double.TryParse(args[5], out double timeoutSec) || timeoutSec <= 0)
            {
                Console.Error.WriteLine("Timeout must be > 0 seconds: " + args[5]);
                return 1;
            }

            if (!double.TryParse(args[6], out double delaySec) || delaySec < 0)
            {
                Console.Error.WriteLine("Cycle delay must be >= 0 seconds: " + args[6]);
                return 1;
            }

            ConnectionParameters parameters;
            try
            {
                parameters = new ConnectionParameters(ip, port, isActive, deviceId);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("Invalid parameters: " + ex.Message);
                return 1;
            }

            var timeout = TimeSpan.FromSeconds(timeoutSec);
            var cycleDelay = TimeSpan.FromSeconds(delaySec);
            var logger = new FileLogger("logs");

            Console.WriteLine("Target : " + ip + ":" + port + " (" + modeArg + "), DeviceId=" + deviceId);
            Console.WriteLine("Cycles : " + cycles + ", Timeout: " + timeoutSec + "s, Delay: " + delaySec + "s between cycles");
            Console.WriteLine();

            using (var adapter = new Secs4NetAdapter(logger))
            {
                var service = new ConnectionTestService();
                var result = await service.RunConnectDisconnectCyclesAsync(
                    adapter, parameters, cycles, timeout, logger, cycleDelay);

                Console.WriteLine("Result: " + result.SuccessfulCycles + "/" + result.TotalCycles + " cycles passed");
                if (!result.AllPassed)
                {
                    Console.WriteLine("Errors:");
                    foreach (var error in result.Errors)
                        Console.WriteLine("  " + error);
                }

                return result.AllPassed ? 0 : 2;
            }
        }
    }
}
