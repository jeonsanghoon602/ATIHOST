using System;
using HostSimulator.Domain;
using Xunit;

namespace HostSimulator.Tests
{
    public class MessageLogEntryTests
    {
        [Fact]
        public void FromMessage_sent()
        {
            var msg = new SecsMessageData(1, 1, true, 42, SecsItemData.Ascii("test"),
                new byte[] { 0x01 });
            var entry = MessageLogEntry.FromMessage(msg, MessageDirection.Sent);

            Assert.Equal(MessageDirection.Sent, entry.Direction);
            Assert.Contains("S1F1", entry.Header);
            Assert.Contains("test", entry.ParsedText);
            Assert.NotNull(entry.RawBytes);
        }

        [Fact]
        public void FromMessage_received()
        {
            var msg = new SecsMessageData(6, 11, true, 1, null);
            var entry = MessageLogEntry.FromMessage(msg, MessageDirection.Received);

            Assert.Equal(MessageDirection.Received, entry.Direction);
            Assert.Contains("S6F11", entry.Header);
        }

        [Fact]
        public void FromMessage_null_throws()
        {
            Assert.Throws<ArgumentNullException>(() =>
                MessageLogEntry.FromMessage(null, MessageDirection.Sent));
        }

        [Fact]
        public void Constructor_null_header_throws()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new MessageLogEntry(DateTimeOffset.Now, MessageDirection.Sent, null, "", null));
        }

        [Fact]
        public void ToString_sent_uses_arrow()
        {
            var entry = new MessageLogEntry(DateTimeOffset.Now, MessageDirection.Sent,
                "S1F1 W", "S1F1 W <A \"test\">", null);
            Assert.Contains(">>>", entry.ToString());
        }

        [Fact]
        public void ToString_received_uses_arrow()
        {
            var entry = new MessageLogEntry(DateTimeOffset.Now, MessageDirection.Received,
                "S1F2", "S1F2", null);
            Assert.Contains("<<<", entry.ToString());
        }
    }
}
