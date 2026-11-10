using System;

namespace HostSimulator.Domain
{
    public sealed class PrimaryMessageReceivedEventArgs : EventArgs
    {
        public SecsMessageData Message { get; }

        public PrimaryMessageReceivedEventArgs(SecsMessageData message)
        {
            Message = message ?? throw new ArgumentNullException(nameof(message));
        }
    }
}
