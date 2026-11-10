using System;

namespace HostSimulator.Domain
{
    public enum MessageSendStatus
    {
        Success,
        T3Timeout,
        Abort,
        NotConnected,
        Error
    }

    public sealed class MessageSendResult
    {
        public MessageSendStatus Status { get; }
        public SecsMessageData Reply { get; }
        public string ErrorMessage { get; }

        private MessageSendResult(MessageSendStatus status, SecsMessageData reply, string errorMessage)
        {
            Status = status;
            Reply = reply;
            ErrorMessage = errorMessage;
        }

        public bool IsSuccess => Status == MessageSendStatus.Success;

        public static MessageSendResult Success(SecsMessageData reply)
        {
            if (reply == null)
                throw new ArgumentNullException(nameof(reply));
            return new MessageSendResult(MessageSendStatus.Success, reply, null);
        }

        public static MessageSendResult T3Timeout(uint systemByte)
        {
            return new MessageSendResult(MessageSendStatus.T3Timeout, null,
                $"T3 timeout waiting for reply to SystemByte={systemByte}");
        }

        public static MessageSendResult Abort(SecsMessageData abortMessage)
        {
            return new MessageSendResult(MessageSendStatus.Abort, abortMessage,
                abortMessage != null ? $"Abort received: {abortMessage.ToHeaderString()}" : "Abort received");
        }

        public static MessageSendResult NotConnected()
        {
            return new MessageSendResult(MessageSendStatus.NotConnected, null, "Not in Selected state");
        }

        public static MessageSendResult Error(string message)
        {
            return new MessageSendResult(MessageSendStatus.Error, null, message ?? "Unknown error");
        }

        public override string ToString()
        {
            if (IsSuccess)
                return $"Success: {Reply.ToHeaderString()}";
            return $"{Status}: {ErrorMessage}";
        }
    }
}
