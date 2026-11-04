using System;
using System.Threading;
using System.Threading.Tasks;

namespace HostSimulator.Domain
{
    public interface IProtocolAdapter : IDisposable
    {
        ProtocolConnectionState State { get; }
        event EventHandler<ProtocolConnectionState> StateChanged;
        Task ConnectAsync(ConnectionParameters parameters, TimeSpan timeout, CancellationToken cancellationToken = default);
        Task DisconnectAsync();
    }
}
