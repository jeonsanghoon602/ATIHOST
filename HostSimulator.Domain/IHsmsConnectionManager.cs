using System;
using System.Threading;
using System.Threading.Tasks;

namespace HostSimulator.Domain
{
    public interface IHsmsConnectionManager : IDisposable
    {
        ProtocolConnectionState State { get; }
        event EventHandler<StateChangedEventArgs> StateChanged;
        event EventHandler<PrimaryMessageReceivedEventArgs> PrimaryMessageReceived;
        event EventHandler<SecsMessageData> UnexpectedMessageReceived;
        Task StartAsync(EquipmentProfile profile, CancellationToken cancellationToken = default);
        Task StopAsync();
        Task<bool> LinktestAsync(CancellationToken cancellationToken = default);
        Task<MessageSendResult> SendAsync(SecsMessageData message, CancellationToken cancellationToken = default);
        Task ReplyAsync(SecsMessageData primaryMessage, SecsMessageData replyMessage);
    }
}
