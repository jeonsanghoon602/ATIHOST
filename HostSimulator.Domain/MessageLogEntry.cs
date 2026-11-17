using System;

namespace HostSimulator.Domain
{
    public sealed class MessageLogEntry
    {
        public DateTimeOffset Timestamp { get; }
        public MessageDirection Direction { get; }
        public string Header { get; }
        public string ParsedText { get; }
        public byte[] RawBytes { get; }

        public MessageLogEntry(
            DateTimeOffset timestamp,
            MessageDirection direction,
            string header,
            string parsedText,
            byte[] rawBytes)
        {
            Timestamp = timestamp;
            Direction = direction;
            Header = header ?? throw new ArgumentNullException(nameof(header));
            ParsedText = parsedText ?? "";
            RawBytes = rawBytes;
        }

        public static MessageLogEntry FromMessage(SecsMessageData message, MessageDirection direction)
        {
            if (message == null)
                throw new ArgumentNullException(nameof(message));

            return new MessageLogEntry(
                message.Timestamp,
                direction,
                message.ToHeaderString(),
                message.ToString(),
                message.RawBytes);
        }

        public override string ToString()
        {
            var dir = Direction == MessageDirection.Sent ? ">>>" : "<<<";
            return $"[{Timestamp:HH:mm:ss.fff}] {dir} {Header}";
        }
    }
}
