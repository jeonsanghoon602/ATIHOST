using System;
using System.Threading;
using System.Threading.Tasks;

namespace HostSimulator.Domain
{
    public interface IProtocolAdapter : IDisposable
    {
        ProtocolConnectionState State { get; }
        event EventHandler<StateChangedEventArgs> StateChanged;
        Task ConnectAsync(ConnectionParameters parameters, TimeSpan timeout, CancellationToken cancellationToken = default);
        Task DisconnectAsync();
        Task<bool> LinktestAsync(TimeSpan timeout, CancellationToken cancellationToken = default);
    }
}
