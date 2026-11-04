using System;
using System.Threading;
using System.Threading.Tasks;

namespace HostSimulator.Domain
{
    public interface IHsmsConnectionManager : IDisposable
    {
        ProtocolConnectionState State { get; }
        event EventHandler<StateChangedEventArgs> StateChanged;
        Task StartAsync(EquipmentProfile profile, CancellationToken cancellationToken = default);
        Task StopAsync();
        Task<bool> LinktestAsync(CancellationToken cancellationToken = default);
    }
}
