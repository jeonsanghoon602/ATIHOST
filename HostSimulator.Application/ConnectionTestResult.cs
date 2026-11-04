using System;
using System.Collections.Generic;

namespace HostSimulator.Application
{
    public sealed class ConnectionTestResult
    {
        public int TotalCycles { get; }
        public int SuccessfulCycles { get; }
        public int FailedCycles => TotalCycles - SuccessfulCycles;
        public IReadOnlyList<string> Errors { get; }
        public bool AllPassed => SuccessfulCycles == TotalCycles;

        public ConnectionTestResult(int totalCycles, int successfulCycles, IReadOnlyList<string> errors)
        {
            TotalCycles = totalCycles;
            SuccessfulCycles = successfulCycles;
            Errors = errors ?? Array.Empty<string>();
        }
    }
}
