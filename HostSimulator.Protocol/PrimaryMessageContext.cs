using System;
using System.Threading;
using System.Threading.Tasks;
using Secs4Net;

namespace HostSimulator.Protocol
{
    internal sealed class PrimaryMessageContext : IDisposable
    {
        public uint SystemByte { get; }
        public SecsMessage Message { get; }
        private readonly Func<SecsMessage, CancellationToken, Task<bool>> _tryReplyAsync;
        private int _completed;

        public PrimaryMessageContext(uint systemByte, SecsMessage message,
            Func<SecsMessage, CancellationToken, Task<bool>> tryReplyAsync)
        {
            SystemByte = systemByte;
            Message = message ?? throw new ArgumentNullException(nameof(message));
            _tryReplyAsync = tryReplyAsync ?? throw new ArgumentNullException(nameof(tryReplyAsync));
        }

        public async Task<bool> TryReplyAsync(SecsMessage reply, CancellationToken cancellationToken = default)
        {
            if (Interlocked.Exchange(ref _completed, 1) != 0)
                return false;
            return await _tryReplyAsync(reply, cancellationToken).ConfigureAwait(false);
        }

        public void Dispose()
        {
            Interlocked.Exchange(ref _completed, 1);
            Message?.Dispose();
        }
    }
}
