using System;

namespace HostSimulator.Domain
{
    public sealed class SecsMessageData
    {
        public byte Stream { get; }
        public byte Function { get; }
        public bool WBit { get; }
        public uint SystemByte { get; }
        public SecsItemData Item { get; }
        public byte[] RawBytes { get; }
        public DateTimeOffset Timestamp { get; }

        public SecsMessageData(
            byte stream,
            byte function,
            bool wBit,
            uint systemByte,
            SecsItemData item,
            byte[] rawBytes = null,
            DateTimeOffset? timestamp = null)
        {
            Stream = stream;
            Function = function;
            WBit = wBit;
            SystemByte = systemByte;
            Item = item;
            RawBytes = rawBytes;
            Timestamp = timestamp ?? DateTimeOffset.Now;
        }

        public bool IsPrimary => Function % 2 != 0;

        public string ToHeaderString()
        {
            return $"S{Stream}F{Function} {(WBit ? "W" : "")} SysByte={SystemByte}";
        }

        public override string ToString()
        {
            var header = ToHeaderString();
            return Item != null ? $"{header} {Item}" : header;
        }
    }
}
