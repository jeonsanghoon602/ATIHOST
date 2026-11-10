using System;
using System.Threading;
using System.Threading.Tasks;

namespace HostSimulator.Domain
{
    public interface IProtocolAdapter : IDisposable
    {
        ProtocolConnectionState State { get; }
        event EventHandler<StateChangedEventArgs> StateChanged;
        event EventHandler<PrimaryMessageReceivedEventArgs> PrimaryMessageReceived;
        event EventHandler<SecsMessageData> UnexpectedMessageReceived;
        Task ConnectAsync(ConnectionParameters parameters, TimeSpan timeout, CancellationToken cancellationToken = default);
        Task DisconnectAsync();
        Task<bool> LinktestAsync(TimeSpan timeout, CancellationToken cancellationToken = default);
        Task<MessageSendResult> SendAsync(SecsMessageData message, CancellationToken cancellationToken = default);
        Task ReplyAsync(SecsMessageData primaryMessage, SecsMessageData replyMessage);
    }
}
