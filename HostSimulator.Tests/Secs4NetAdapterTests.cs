using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using HostSimulator.Domain;
using HostSimulator.Protocol;
using Secs4Net;
using Xunit;

namespace HostSimulator.Tests
{
    public class Secs4NetAdapterTests
    {
        private class StubLogger : Domain.ILogger
        {
            public List<string> Messages { get; } = new List<string>();
            public void Info(string message) => Messages.Add("[INFO] " + message);
            public void Warn(string message) => Messages.Add("[WARN] " + message);
            public void Error(string message) => Messages.Add("[ERROR] " + message);
        }

        private class StubMessageLogger : IMessageLogger
        {
            public List<MessageLogEntry> Entries { get; } = new List<MessageLogEntry>();
            public void Log(MessageLogEntry entry) => Entries.Add(entry);
        }

        private class FakeSecsGem : ISecsGem
        {
            public ushort DeviceId { get; set; }
            public int WireId { get; set; } = 42;
            public Secs4NetLoggerBridge Bridge { get; set; }
            public Func<SecsMessage, CancellationToken, Task<SecsMessage>> OnSendAsync { get; set; }

            public Task<SecsMessage> SendAsync(SecsMessage message, CancellationToken cancellationToken = default)
            {
                Bridge?.MessageOut(message, WireId);

                if (OnSendAsync != null)
                    return OnSendAsync(message, cancellationToken);
                return Task.FromResult<SecsMessage>(null);
            }

            public IAsyncEnumerable<PrimaryMessageWrapper> GetPrimaryMessageAsync(CancellationToken cancellationToken)
            {
                return new EmptyAsyncEnumerable(cancellationToken);
            }

            private sealed class EmptyAsyncEnumerable : IAsyncEnumerable<PrimaryMessageWrapper>
            {
                private readonly CancellationToken _ct;
                public EmptyAsyncEnumerable(CancellationToken ct) { _ct = ct; }
                public IAsyncEnumerator<PrimaryMessageWrapper> GetAsyncEnumerator(CancellationToken cancellationToken = default)
                    => new EmptyAsyncEnumerator();
            }

            private sealed class EmptyAsyncEnumerator : IAsyncEnumerator<PrimaryMessageWrapper>
            {
                public PrimaryMessageWrapper Current => default;
                public ValueTask<bool> MoveNextAsync() => new ValueTask<bool>(false);
                public ValueTask DisposeAsync() => default;
            }
        }

        private Secs4NetAdapter CreateAdapter(FakeSecsGem gem, StubLogger logger = null,
            StubMessageLogger msgLogger = null)
        {
            var adapter = new Secs4NetAdapter(
                logger ?? new StubLogger(),
                msgLogger,
                gem,
                ProtocolConnectionState.Selected);
            gem.Bridge = adapter.SecsLoggerBridge;
            return adapter;
        }

        #region SendAsync tests

        [Fact]
        public async Task SendAsync_NormalReply_ReturnsSuccess()
        {
            var gem = new FakeSecsGem
            {
                OnSendAsync = (msg, ct) =>
                    Task.FromResult(new SecsMessage(msg.S, (byte)(msg.F + 1), false))
            };
            var adapter = CreateAdapter(gem);

            var message = new SecsMessageData(1, 13, true, 0, SecsItemData.CreateList());
            var result = await adapter.SendAsync(message);

            Assert.True(result.IsSuccess);
            Assert.Equal(1, result.Reply.Stream);
            Assert.Equal(14, result.Reply.Function);
        }

        [Fact]
        public async Task SendAsync_NoWBit_NoReply_ReturnsSuccess()
        {
            var gem = new FakeSecsGem
            {
                OnSendAsync = (msg, ct) => Task.FromResult<SecsMessage>(null)
            };
            var adapter = CreateAdapter(gem);

            var message = new SecsMessageData(1, 2, false, 0, SecsItemData.U1(0));
            var result = await adapter.SendAsync(message);

            Assert.True(result.IsSuccess);
        }

        [Fact]
        public async Task SendAsync_T3Timeout_ViaSecsException()
        {
            var gem = new FakeSecsGem
            {
                OnSendAsync = (msg, ct) => throw new SecsException("T3 timeout expired")
            };
            var adapter = CreateAdapter(gem);

            var message = new SecsMessageData(1, 13, true, 0, null);
            var result = await adapter.SendAsync(message);

            Assert.Equal(MessageSendStatus.T3Timeout, result.Status);
        }

        [Fact]
        public async Task SendAsync_SxF0Abort_ViaSecsException()
        {
            var gem = new FakeSecsGem
            {
                OnSendAsync = (msg, ct) =>
                {
                    var abortMsg = new SecsMessage(msg.S, 0, false);
                    throw new SecsException(abortMsg, "Abort received");
                }
            };
            var adapter = CreateAdapter(gem);

            var message = new SecsMessageData(1, 13, true, 0, null);
            var result = await adapter.SendAsync(message);

            Assert.Equal(MessageSendStatus.Abort, result.Status);
            Assert.NotNull(result.Reply);
            Assert.Equal(0, result.Reply.Function);
        }

        [Fact]
        public async Task SendAsync_S9Error_ViaSecsException()
        {
            var gem = new FakeSecsGem
            {
                OnSendAsync = (msg, ct) =>
                {
                    var s9Msg = new SecsMessage(9, 3, false);
                    throw new SecsException(s9Msg, "S9F3 received");
                }
            };
            var logger = new StubLogger();
            var adapter = CreateAdapter(gem, logger);

            SecsMessageData unexpectedReceived = null;
            adapter.UnexpectedMessageReceived += (s, e) => unexpectedReceived = e;

            var message = new SecsMessageData(1, 13, true, 0, null);
            var result = await adapter.SendAsync(message);

            Assert.Equal(MessageSendStatus.Error, result.Status);
            Assert.Contains("S9", result.ErrorMessage);
            Assert.NotNull(unexpectedReceived);
            Assert.Equal((byte)9, unexpectedReceived.Stream);
        }

        [Fact]
        public async Task SendAsync_AbortReply_InNormalFlow()
        {
            var gem = new FakeSecsGem
            {
                OnSendAsync = (msg, ct) =>
                    Task.FromResult(new SecsMessage(msg.S, 0, false))
            };
            var adapter = CreateAdapter(gem);

            var message = new SecsMessageData(1, 13, true, 0, null);
            var result = await adapter.SendAsync(message);

            Assert.Equal(MessageSendStatus.Abort, result.Status);
        }

        [Fact]
        public async Task SendAsync_S9Reply_InNormalFlow()
        {
            var gem = new FakeSecsGem
            {
                OnSendAsync = (msg, ct) =>
                    Task.FromResult(new SecsMessage(9, 7, false))
            };
            var adapter = CreateAdapter(gem);

            var message = new SecsMessageData(1, 13, true, 0, null);
            var result = await adapter.SendAsync(message);

            Assert.Equal(MessageSendStatus.Error, result.Status);
            Assert.Contains("S9", result.ErrorMessage);
        }

        [Fact]
        public async Task SendAsync_NotConnected_ReturnsNotConnected()
        {
            var gem = new FakeSecsGem();
            var adapter = new Secs4NetAdapter(
                new StubLogger(), null, gem, ProtocolConnectionState.Disconnected);

            var message = new SecsMessageData(1, 13, true, 0, null);
            var result = await adapter.SendAsync(message);

            Assert.Equal(MessageSendStatus.NotConnected, result.Status);
        }

        [Fact]
        public async Task SendAsync_LogsWithRawBytes()
        {
            var gem = new FakeSecsGem
            {
                OnSendAsync = (msg, ct) =>
                    Task.FromResult(new SecsMessage(msg.S, (byte)(msg.F + 1), false))
            };
            var msgLogger = new StubMessageLogger();
            var adapter = CreateAdapter(gem, msgLogger: msgLogger);

            var message = new SecsMessageData(1, 13, true, 0, null);
            await adapter.SendAsync(message);

            Assert.True(msgLogger.Entries.Count >= 1);
            var sentEntry = msgLogger.Entries[0];
            Assert.Equal(MessageDirection.Sent, sentEntry.Direction);
            Assert.NotNull(sentEntry.RawBytes);
            Assert.True(sentEntry.RawBytes.Length >= 10, "Raw bytes should include 10-byte SECS-II header");
        }

        [Fact]
        public async Task SendAsync_T3Timeout_StillLogsSentEntry()
        {
            var gem = new FakeSecsGem
            {
                WireId = 0x1234,
                OnSendAsync = (msg, ct) => throw new SecsException("T3 timeout expired")
            };
            var msgLogger = new StubMessageLogger();
            var adapter = CreateAdapter(gem, msgLogger: msgLogger);

            var message = new SecsMessageData(1, 13, true, 0, null);
            var result = await adapter.SendAsync(message);

            Assert.Equal(MessageSendStatus.T3Timeout, result.Status);
            Assert.True(msgLogger.Entries.Count >= 1,
                "Sent log entry should be written even on T3 timeout");
            Assert.Equal(MessageDirection.Sent, msgLogger.Entries[0].Direction);
        }

        [Fact]
        public async Task SendAsync_CapturesWireIdFromBridge()
        {
            var gem = new FakeSecsGem
            {
                WireId = 0xABCD,
                OnSendAsync = (msg, ct) =>
                    Task.FromResult(new SecsMessage(msg.S, (byte)(msg.F + 1), false))
            };
            var msgLogger = new StubMessageLogger();
            var adapter = CreateAdapter(gem, msgLogger: msgLogger);

            var message = new SecsMessageData(1, 13, true, 0, null);
            var result = await adapter.SendAsync(message);

            Assert.True(result.IsSuccess);
            Assert.Equal(0xABCDu, result.Reply.SystemByte);
            var sentEntry = msgLogger.Entries[0];
            Assert.Equal(MessageDirection.Sent, sentEntry.Direction);
            Assert.Contains("SysByte=43981", sentEntry.Header);
        }

        [Fact]
        public async Task SendAsync_MissedWireId_ReturnsError()
        {
            var gem = new FakeSecsGem
            {
                OnSendAsync = (msg, ct) =>
                    Task.FromResult(new SecsMessage(msg.S, (byte)(msg.F + 1), false))
            };
            var adapter = CreateAdapter(gem);
            gem.Bridge = null;

            var message = new SecsMessageData(1, 13, true, 0, null);
            var result = await adapter.SendAsync(message);

            Assert.False(result.IsSuccess);
            Assert.Contains("Wire System Byte not captured", result.ErrorMessage);
        }

        [Fact]
        public async Task SendAsync_TimestampCapturedBeforeAwait()
        {
            var before = DateTimeOffset.Now;
            var gem = new FakeSecsGem
            {
                OnSendAsync = async (msg, ct) =>
                {
                    await Task.Delay(100, ct);
                    return new SecsMessage(msg.S, (byte)(msg.F + 1), false);
                }
            };
            var msgLogger = new StubMessageLogger();
            var adapter = CreateAdapter(gem, msgLogger: msgLogger);

            var message = new SecsMessageData(1, 13, true, 0, null);
            await adapter.SendAsync(message);

            var sentEntry = msgLogger.Entries[0];
            Assert.True(sentEntry.Timestamp >= before);
            Assert.True(sentEntry.Timestamp <= before.AddMilliseconds(100));
        }

        #endregion

        #region HandlePrimaryMessage tests

        [Fact]
        public void HandlePrimaryMessageCore_NormalMessage_FiresPrimaryReceived()
        {
            var gem = new FakeSecsGem();
            var adapter = CreateAdapter(gem);

            SecsMessageData received = null;
            adapter.PrimaryMessageReceived += (s, e) => received = e.Message;

            var msg = new SecsMessage(6, 11, true) { SecsItem = Item.L() };
            adapter.HandlePrimaryMessageCore(
                msg, 42u, (reply, ct) => Task.FromResult(true));

            Assert.NotNull(received);
            Assert.Equal((byte)6, received.Stream);
            Assert.Equal((byte)11, received.Function);
            Assert.Equal(42u, received.SystemByte);
        }

        [Fact]
        public void HandlePrimaryMessageCore_S9Message_FiresUnexpected()
        {
            var gem = new FakeSecsGem();
            var adapter = CreateAdapter(gem);

            SecsMessageData unexpected = null;
            adapter.UnexpectedMessageReceived += (s, e) => unexpected = e;

            var msg = new SecsMessage(9, 3, false);
            var stored = adapter.HandlePrimaryMessageCore(
                msg, 100u, (reply, ct) => Task.FromResult(true));

            Assert.False(stored);
            Assert.NotNull(unexpected);
            Assert.Equal((byte)9, unexpected.Stream);
        }

        [Fact]
        public void HandlePrimaryMessageCore_SxF0Message_FiresUnexpected()
        {
            var gem = new FakeSecsGem();
            var adapter = CreateAdapter(gem);

            SecsMessageData unexpected = null;
            adapter.UnexpectedMessageReceived += (s, e) => unexpected = e;

            var msg = new SecsMessage(1, 0, false);
            var stored = adapter.HandlePrimaryMessageCore(
                msg, 200u, (reply, ct) => Task.FromResult(true));

            Assert.False(stored);
            Assert.NotNull(unexpected);
            Assert.Equal((byte)0, unexpected.Function);
        }

        [Fact]
        public void HandlePrimaryMessageCore_WBitSet_StoresForReply()
        {
            var gem = new FakeSecsGem();
            var adapter = CreateAdapter(gem);

            adapter.PrimaryMessageReceived += (s, e) => { };

            var msg = new SecsMessage(1, 1, true);
            var stored = adapter.HandlePrimaryMessageCore(
                msg, 500u, (reply, ct) => Task.FromResult(true));

            Assert.True(stored);
        }

        [Fact]
        public void HandlePrimaryMessageCore_NoWBit_NotStored()
        {
            var gem = new FakeSecsGem();
            var adapter = CreateAdapter(gem);

            adapter.PrimaryMessageReceived += (s, e) => { };

            var msg = new SecsMessage(1, 2, false);
            var stored = adapter.HandlePrimaryMessageCore(
                msg, 600u, (reply, ct) => Task.FromResult(true));

            Assert.False(stored);
        }

        [Fact]
        public void HandlePrimaryMessageCore_UsesRealSystemByte()
        {
            var gem = new FakeSecsGem();
            var adapter = CreateAdapter(gem);

            SecsMessageData received = null;
            adapter.PrimaryMessageReceived += (s, e) => received = e.Message;

            var msg = new SecsMessage(6, 11, false);
            adapter.HandlePrimaryMessageCore(msg, 12345u, (reply, ct) => Task.FromResult(true));

            Assert.NotNull(received);
            Assert.Equal(12345u, received.SystemByte);
        }

        #endregion

        #region ReplyAsync tests

        [Fact]
        public async Task ReplyAsync_WithStoredContext_CallsTryReply()
        {
            var gem = new FakeSecsGem();
            var adapter = CreateAdapter(gem);

            var replyCalled = false;
            var msg = new SecsMessage(1, 1, true);
            adapter.HandlePrimaryMessageCore(
                msg, 777u,
                (reply, ct) => { replyCalled = true; return Task.FromResult(true); });

            var primary = new SecsMessageData(1, 1, true, 777u, null);
            var replyData = new SecsMessageData(1, 2, false, 777u, SecsItemData.U1(0));
            await adapter.ReplyAsync(primary, replyData);

            Assert.True(replyCalled);
        }

        [Fact]
        public async Task ReplyAsync_NoStoredContext_ThrowsInvalidOperation()
        {
            var gem = new FakeSecsGem();
            var adapter = CreateAdapter(gem);

            var primary = new SecsMessageData(1, 1, true, 999u, null);
            var replyData = new SecsMessageData(1, 2, false, 999u, null);

            await Assert.ThrowsAsync<InvalidOperationException>(
                () => adapter.ReplyAsync(primary, replyData));
        }

        [Fact]
        public async Task ReplyAsync_FailedTryReply_ThrowsInvalidOperation()
        {
            var gem = new FakeSecsGem();
            var msgLogger = new StubMessageLogger();
            var adapter = CreateAdapter(gem, msgLogger: msgLogger);

            var msg = new SecsMessage(1, 1, true);
            adapter.HandlePrimaryMessageCore(
                msg, 888u,
                (reply, ct) => Task.FromResult(false));

            var primary = new SecsMessageData(1, 1, true, 888u, null);
            var replyData = new SecsMessageData(1, 2, false, 888u, SecsItemData.U1(0));

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(
                () => adapter.ReplyAsync(primary, replyData));
            Assert.Contains("Reply rejected", ex.Message);
        }

        [Fact]
        public async Task ReplyAsync_DoubleReply_ThrowsInvalidOperation()
        {
            var gem = new FakeSecsGem();
            var adapter = CreateAdapter(gem);

            var msg = new SecsMessage(1, 1, true);
            adapter.HandlePrimaryMessageCore(
                msg, 1234u, (reply, ct) => Task.FromResult(true));

            var primary = new SecsMessageData(1, 1, true, 1234u, null);
            var replyData = new SecsMessageData(1, 2, false, 1234u, null);
            await adapter.ReplyAsync(primary, replyData);

            await Assert.ThrowsAsync<InvalidOperationException>(
                () => adapter.ReplyAsync(primary, new SecsMessageData(1, 2, false, 1234u, null)));
        }

        [Fact]
        public async Task ReplyAsync_NullPrimary_Throws()
        {
            var gem = new FakeSecsGem();
            var adapter = CreateAdapter(gem);

            await Assert.ThrowsAsync<ArgumentNullException>(
                () => adapter.ReplyAsync(null, new SecsMessageData(1, 2, false, 0, null)));
        }

        [Fact]
        public async Task ReplyAsync_NullReply_Throws()
        {
            var gem = new FakeSecsGem();
            var adapter = CreateAdapter(gem);

            await Assert.ThrowsAsync<ArgumentNullException>(
                () => adapter.ReplyAsync(new SecsMessageData(1, 1, true, 0, null), null));
        }

        #endregion

        #region Raw bytes tests

        [Fact]
        public void EncodeRawBytes_IncludesHeader()
        {
            var msg = new SecsMessage(3, 25, true) { SecsItem = Item.A("test") };
            var rawBytes = SecsMessageConverter.EncodeRawBytes(msg, 0x12345678);

            Assert.True(rawBytes.Length >= 10);
            Assert.Equal(0x83, rawBytes[2]); // W-bit (0x80) + Stream 3
            Assert.Equal(25, rawBytes[3]); // Function
            Assert.Equal(0x12, rawBytes[6]); // System byte MSB
            Assert.Equal(0x34, rawBytes[7]);
            Assert.Equal(0x56, rawBytes[8]);
            Assert.Equal(0x78, rawBytes[9]); // System byte LSB
        }

        [Fact]
        public void EncodeRawBytes_NoItem_HeaderOnly()
        {
            var msg = new SecsMessage(1, 2, false);
            var rawBytes = SecsMessageConverter.EncodeRawBytes(msg, 42);

            Assert.Equal(10, rawBytes.Length);
            Assert.Equal(0x01, rawBytes[2]); // No W-bit, Stream 1
            Assert.Equal(2, rawBytes[3]); // Function
        }

        [Fact]
        public void EncodeRawBytes_NonZeroDeviceId_AppearsInHeader()
        {
            var msg = new SecsMessage(1, 1, true);
            ushort deviceId = 5;
            var rawBytes = SecsMessageConverter.EncodeRawBytes(msg, 100u, deviceId);

            Assert.Equal(10, rawBytes.Length);
            Assert.Equal(0x00, rawBytes[0]);
            Assert.Equal(0x05, rawBytes[1]);
        }

        [Fact]
        public void EncodeRawBytes_LargeDeviceId_MaskedCorrectly()
        {
            var msg = new SecsMessage(1, 1, false);
            ushort deviceId = 0x7FFF;
            var rawBytes = SecsMessageConverter.EncodeRawBytes(msg, 0u, deviceId);

            Assert.Equal(0x7F, rawBytes[0]);
            Assert.Equal(0xFF, rawBytes[1]);
        }

        #endregion

        #region JIS8 round-trip tests

        [Fact]
        public void ConvertItemToSecs4Net_Jis8_UsesItemJ()
        {
            var jisData = SecsItemData.Create(SecsDataFormat.Jis8, "テスト");
            var secsMsg = new SecsMessageData(10, 1, true, 0, jisData);
            var secs4net = SecsMessageConverter.ToSecs4Net(secsMsg);

            Assert.NotNull(secs4net.SecsItem);
            Assert.Equal(SecsFormat.JIS8, secs4net.SecsItem.Format);
        }

        [Fact]
        public void Jis8_RoundTrip_ValuePreserved()
        {
            var original = "テスト文字列";
            var jisData = SecsItemData.Create(SecsDataFormat.Jis8, original);
            var secsMsg = new SecsMessageData(10, 1, true, 0, jisData);
            var secs4net = SecsMessageConverter.ToSecs4Net(secsMsg);

            var roundTripped = SecsMessageConverter.ConvertItemFromSecs4Net(secs4net.SecsItem);
            Assert.Equal(SecsDataFormat.Jis8, roundTripped.Format);
            Assert.Equal(original, (string)roundTripped.Value);
        }

        #endregion

        #region Logger composition tests

        [Fact]
        public void Adapter_WithMessageLogger_LogsOnPrimaryReceived()
        {
            var gem = new FakeSecsGem();
            var msgLogger = new StubMessageLogger();
            var adapter = CreateAdapter(gem, msgLogger: msgLogger);

            adapter.PrimaryMessageReceived += (s, e) => { };

            var msg = new SecsMessage(6, 11, true) { SecsItem = Item.L() };
            adapter.HandlePrimaryMessageCore(msg, 42u, (reply, ct) => Task.FromResult(true));

            Assert.True(msgLogger.Entries.Count >= 1);
            var entry = msgLogger.Entries[0];
            Assert.Equal(MessageDirection.Received, entry.Direction);
            Assert.NotNull(entry.RawBytes);
        }

        [Fact]
        public void Adapter_WithoutMessageLogger_DoesNotThrow()
        {
            var gem = new FakeSecsGem();
            var adapter = new Secs4NetAdapter(
                new StubLogger(), null, gem, ProtocolConnectionState.Selected);

            adapter.PrimaryMessageReceived += (s, e) => { };

            var msg = new SecsMessage(6, 11, true) { SecsItem = Item.L() };
            adapter.HandlePrimaryMessageCore(msg, 42u, (reply, ct) => Task.FromResult(true));
        }

        [Fact]
        public void HandlePrimaryMessageCore_ReplaceSameSystemByte_DisposesOldContext()
        {
            var gem = new FakeSecsGem();
            var adapter = CreateAdapter(gem);

            var msg1 = new SecsMessage(1, 1, true);
            adapter.HandlePrimaryMessageCore(
                msg1, 500u, (reply, ct) => Task.FromResult(true));

            var msg2 = new SecsMessage(1, 1, true);
            adapter.HandlePrimaryMessageCore(
                msg2, 500u, (reply, ct) => Task.FromResult(true));

            var primary = new SecsMessageData(1, 1, true, 500u, null);
            var replyData = new SecsMessageData(1, 2, false, 500u, null);
            adapter.ReplyAsync(primary, replyData).GetAwaiter().GetResult();
        }

        [Fact]
        public async Task SendAsync_WithNonZeroDeviceId_RawBytesContainDeviceId()
        {
            var gem = new FakeSecsGem
            {
                DeviceId = 7,
                OnSendAsync = (msg, ct) =>
                    Task.FromResult(new SecsMessage(msg.S, (byte)(msg.F + 1), false))
            };
            var msgLogger = new StubMessageLogger();
            var adapter = CreateAdapter(gem, msgLogger: msgLogger);

            var message = new SecsMessageData(1, 13, true, 0, null);
            await adapter.SendAsync(message);

            Assert.True(msgLogger.Entries.Count >= 1);
            var sentEntry = msgLogger.Entries[0];
            Assert.Equal(MessageDirection.Sent, sentEntry.Direction);
            Assert.True(sentEntry.RawBytes.Length >= 10);
            Assert.Equal(0x00, sentEntry.RawBytes[0]);
            Assert.Equal(0x07, sentEntry.RawBytes[1]);
        }

        #endregion

        #region Bridge lifetime tests

        [Fact]
        public void Bridge_UntrackedMessage_NotCached()
        {
            var bridge = new Secs4NetLoggerBridge(new StubLogger());
            var msg = new SecsMessage(1, 1, true);

            bridge.MessageOut(msg, 99);

            Assert.False(bridge.TryGetOutboundId(msg, out _));
        }

        [Fact]
        public void Bridge_TrackedMessage_CapturedAndRemoved()
        {
            var bridge = new Secs4NetLoggerBridge(new StubLogger());
            var msg = new SecsMessage(1, 1, true);

            bridge.TrackOutbound(msg);
            bridge.MessageOut(msg, 123);

            Assert.True(bridge.TryGetOutboundId(msg, out var id));
            Assert.Equal(123, id);
            Assert.False(bridge.TryGetOutboundId(msg, out _));
        }

        [Fact]
        public void Bridge_UntrackOutbound_CleansUpBothDictionaries()
        {
            var bridge = new Secs4NetLoggerBridge(new StubLogger());
            var msg = new SecsMessage(1, 1, true);

            bridge.TrackOutbound(msg);
            bridge.MessageOut(msg, 456);
            bridge.UntrackOutbound(msg);

            Assert.False(bridge.TryGetOutboundId(msg, out _));
        }

        #endregion
    }
}
