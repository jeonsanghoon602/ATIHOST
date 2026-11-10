using System;
using HostSimulator.Domain;
using Xunit;

namespace HostSimulator.Tests
{
    public class SecsMessageDataTests
    {
        [Fact]
        public void Constructor_stores_properties()
        {
            var item = SecsItemData.Ascii("test");
            var msg = new SecsMessageData(1, 13, true, 100, item);

            Assert.Equal(1, msg.Stream);
            Assert.Equal(13, msg.Function);
            Assert.True(msg.WBit);
            Assert.Equal(100u, msg.SystemByte);
            Assert.Same(item, msg.Item);
        }

        [Fact]
        public void IsPrimary_odd_function_is_true()
        {
            var msg = new SecsMessageData(1, 13, true, 1, null);
            Assert.True(msg.IsPrimary);
        }

        [Fact]
        public void IsPrimary_even_function_is_false()
        {
            var msg = new SecsMessageData(1, 14, false, 1, null);
            Assert.False(msg.IsPrimary);
        }

        [Fact]
        public void ToHeaderString_includes_stream_function()
        {
            var msg = new SecsMessageData(2, 41, true, 42, null);
            var header = msg.ToHeaderString();
            Assert.Contains("S2F41", header);
            Assert.Contains("W", header);
            Assert.Contains("42", header);
        }

        [Fact]
        public void ToString_with_item()
        {
            var msg = new SecsMessageData(1, 1, true, 1, SecsItemData.Ascii("hello"));
            var text = msg.ToString();
            Assert.Contains("S1F1", text);
            Assert.Contains("hello", text);
        }

        [Fact]
        public void ToString_without_item()
        {
            var msg = new SecsMessageData(1, 1, false, 1, null);
            Assert.Contains("S1F1", msg.ToString());
        }

        [Fact]
        public void Timestamp_defaults_to_now()
        {
            var before = DateTimeOffset.Now;
            var msg = new SecsMessageData(1, 1, false, 1, null);
            Assert.True(msg.Timestamp >= before);
        }

        [Fact]
        public void Timestamp_explicit_value()
        {
            var ts = new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero);
            var msg = new SecsMessageData(1, 1, false, 1, null, null, ts);
            Assert.Equal(ts, msg.Timestamp);
        }

        [Fact]
        public void RawBytes_stored()
        {
            var raw = new byte[] { 0x01, 0x02 };
            var msg = new SecsMessageData(1, 1, false, 1, null, raw);
            Assert.Equal(raw, msg.RawBytes);
        }
    }
}
