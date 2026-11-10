using System.Threading;

namespace HostSimulator.Protocol
{
    public sealed class SystemByteGenerator
    {
        private int _counter;

        public SystemByteGenerator(uint initial = 0)
        {
            _counter = unchecked((int)initial);
        }

        public uint Next()
        {
            return unchecked((uint)Interlocked.Increment(ref _counter));
        }
    }
}
