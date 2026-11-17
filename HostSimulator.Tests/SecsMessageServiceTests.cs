using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using HostSimulator.Application;
using HostSimulator.Domain;
using Xunit;

namespace HostSimulator.Tests
{
    public class SecsMessageServiceTests
    {
        private static EquipmentProfile DefaultProfile()
        {
            return new EquipmentProfile(
                "TestEQ", "127.0.0.1", 5000, true, 0,
                45, 10, 5, 10, 5, false, 10);
        }

        private class StubLogger : ILogger
        {
            public List<string> Messages { get; } = new List<string>();
            public void Info(string message) => Messages.Add("[INFO] " + message);
            public void Warn(string message) => Messages.Add("[WARN] " + message);
            public void Error(string message) => Messages.Add("[ERROR] " + message);
        }

        private class MessageStubAdapter : IProtocolAdapter
        {
            public ProtocolConnectionState State { get; set; } = ProtocolConnectionState.Disconnected;
            public event EventHandler<StateChangedEventArgs> StateChanged;
            public event EventHandler<PrimaryMessageReceivedEventArgs> PrimaryMessageReceived;
            public event EventHandler<SecsMessageData> UnexpectedMessageReceived;
            public int ConnectCount { get; private set; }
            public int SendCount { get; private set; }
            public int ReplyCount { get; private set; }
            public SecsMessageData LastSentMessage { get; private set; }
            public SecsMessageData LastReplyPrimary { get; private set; }
            public SecsMessageData LastReplyMessage { get; private set; }
            public MessageSendResult NextSendResult { get; set; }
            public bool SimulateT3Timeout { get; set; }
            public bool SimulateAbort { get; set; }

            public async Task ConnectAsync(ConnectionParameters parameters, TimeSpan timeout, CancellationToken cancellationToken = default)
            {
                ConnectCount++;
                State = ProtocolConnectionState.Selected;
                StateChanged?.Invoke(this, new StateChangedEventArgs(
                    ProtocolConnectionState.Disconnected, ProtocolConnectionState.Selected, StateChangeReason.SelectCompleted));
                await Task.CompletedTask;
            }

            public Task DisconnectAsync()
            {
                State = ProtocolConnectionState.Disconnected;
                return Task.CompletedTask;
            }

            public Task<bool> LinktestAsync(TimeSpan timeout, CancellationToken cancellationToken = default)
            {
                return Task.FromResult(true);
            }

            public Task<MessageSendResult> SendAsync(SecsMessageData message, CancellationToken cancellationToken = default)
            {
                SendCount++;
                LastSentMessage = message;

                if (NextSendResult != null)
                    return Task.FromResult(NextSendResult);

                if (SimulateT3Timeout)
                    return Task.FromResult(MessageSendResult.T3Timeout(message.SystemByte));

                if (SimulateAbort)
                {
                    var abort = new SecsMessageData(message.Stream, 0, false, message.SystemByte, null);
                    return Task.FromResult(MessageSendResult.Abort(abort));
                }

                var reply = new SecsMessageData(
                    message.Stream, (byte)(message.Function + 1), false,
                    message.SystemByte, SecsItemData.U1(0));
                return Task.FromResult(MessageSendResult.Success(reply));
            }

            public Task ReplyAsync(SecsMessageData primaryMessage, SecsMessageData replyMessage)
            {
                ReplyCount++;
                LastReplyPrimary = primaryMessage;
                LastReplyMessage = replyMessage;
                return Task.CompletedTask;
            }

            public void SimulatePrimaryReceived(SecsMessageData message)
            {
                PrimaryMessageReceived?.Invoke(this, new PrimaryMessageReceivedEventArgs(message));
            }

            public void SimulateUnexpectedMessage(SecsMessageData message)
            {
                UnexpectedMessageReceived?.Invoke(this, message);
            }

            public void Dispose() { }
        }

        [Fact]
        public async Task SendAsync_returns_reply()
        {
            MessageStubAdapter adapter = null;
            var manager = new HsmsConnectionManager(
                () => { adapter = new MessageStubAdapter(); return adapter; },
                new StubLogger());

            await manager.StartAsync(DefaultProfile());
            var msg = new SecsMessageData(1, 13, true, 0, SecsItemData.CreateList());
            var result = await manager.SendAsync(msg);

            Assert.True(result.IsSuccess);
            Assert.Equal(1, result.Reply.Stream);
            Assert.Equal(14, result.Reply.Function);
        }

        [Fact]
        public async Task SendAsync_when_not_connected_returns_not_connected()
        {
            var manager = new HsmsConnectionManager(
                () => new MessageStubAdapter(),
                new StubLogger());

            var msg = new SecsMessageData(1, 13, true, 0, null);
            var result = await manager.SendAsync(msg);

            Assert.Equal(MessageSendStatus.NotConnected, result.Status);
        }

        [Fact]
        public async Task SendAsync_t3_timeout()
        {
            MessageStubAdapter adapter = null;
            var manager = new HsmsConnectionManager(
                () => { adapter = new MessageStubAdapter { SimulateT3Timeout = true }; return adapter; },
                new StubLogger());

            await manager.StartAsync(DefaultProfile());
            var msg = new SecsMessageData(2, 41, true, 0, null);
            var result = await manager.SendAsync(msg);

            Assert.Equal(MessageSendStatus.T3Timeout, result.Status);
        }

        [Fact]
        public async Task SendAsync_abort()
        {
            MessageStubAdapter adapter = null;
            var manager = new HsmsConnectionManager(
                () => { adapter = new MessageStubAdapter { SimulateAbort = true }; return adapter; },
                new StubLogger());

            await manager.StartAsync(DefaultProfile());
            var msg = new SecsMessageData(1, 1, true, 0, null);
            var result = await manager.SendAsync(msg);

            Assert.Equal(MessageSendStatus.Abort, result.Status);
        }

        [Fact]
        public async Task SendAsync_null_throws()
        {
            var manager = new HsmsConnectionManager(
                () => new MessageStubAdapter(),
                new StubLogger());

            await Assert.ThrowsAsync<ArgumentNullException>(() => manager.SendAsync(null));
        }

        [Fact]
        public async Task ReplyAsync_delegates_to_adapter()
        {
            MessageStubAdapter adapter = null;
            var manager = new HsmsConnectionManager(
                () => { adapter = new MessageStubAdapter(); return adapter; },
                new StubLogger());

            await manager.StartAsync(DefaultProfile());
            var primary = new SecsMessageData(1, 1, true, 1, null);
            var reply = new SecsMessageData(1, 2, false, 1, SecsItemData.U1(0));
            await manager.ReplyAsync(primary, reply);

            Assert.Equal(1, adapter.ReplyCount);
            Assert.Same(primary, adapter.LastReplyPrimary);
            Assert.Same(reply, adapter.LastReplyMessage);
        }

        [Fact]
        public async Task ReplyAsync_when_not_connected_throws()
        {
            var manager = new HsmsConnectionManager(
                () => new MessageStubAdapter(),
                new StubLogger());

            var primary = new SecsMessageData(1, 1, true, 1, null);
            var reply = new SecsMessageData(1, 2, false, 1, null);
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                manager.ReplyAsync(primary, reply));
        }

        [Fact]
        public async Task PrimaryMessageReceived_forwarded()
        {
            MessageStubAdapter adapter = null;
            var received = new List<SecsMessageData>();
            var manager = new HsmsConnectionManager(
                () => { adapter = new MessageStubAdapter(); return adapter; },
                new StubLogger());
            manager.PrimaryMessageReceived += (s, e) => received.Add(e.Message);

            await manager.StartAsync(DefaultProfile());

            var msg = new SecsMessageData(6, 11, true, 1, SecsItemData.CreateList());
            adapter.SimulatePrimaryReceived(msg);

            Assert.Single(received);
            Assert.Same(msg, received[0]);
        }

        [Fact]
        public async Task UnexpectedMessageReceived_forwarded()
        {
            MessageStubAdapter adapter = null;
            var received = new List<SecsMessageData>();
            var manager = new HsmsConnectionManager(
                () => { adapter = new MessageStubAdapter(); return adapter; },
                new StubLogger());
            manager.UnexpectedMessageReceived += (s, e) => received.Add(e);

            await manager.StartAsync(DefaultProfile());

            var msg = new SecsMessageData(9, 1, false, 1, null);
            adapter.SimulateUnexpectedMessage(msg);

            Assert.Single(received);
            Assert.Equal(9, received[0].Stream);
        }

        [Fact]
        public async Task WBit_false_send_succeeds()
        {
            MessageStubAdapter adapter = null;
            var manager = new HsmsConnectionManager(
                () => { adapter = new MessageStubAdapter(); return adapter; },
                new StubLogger());

            await manager.StartAsync(DefaultProfile());
            var msg = new SecsMessageData(1, 2, false, 0, SecsItemData.U1(0));
            var result = await manager.SendAsync(msg);

            Assert.True(result.IsSuccess);
        }

        [Fact]
        public async Task SystemByte_populated_in_reply()
        {
            MessageStubAdapter adapter = null;
            var manager = new HsmsConnectionManager(
                () => { adapter = new MessageStubAdapter(); return adapter; },
                new StubLogger());

            await manager.StartAsync(DefaultProfile());
            var msg = new SecsMessageData(1, 13, true, 0, null);
            var result = await manager.SendAsync(msg);

            Assert.True(result.IsSuccess);
            Assert.Equal(msg.SystemByte, result.Reply.SystemByte);
        }

        [Fact]
        public async Task Multiple_sends_all_get_results()
        {
            MessageStubAdapter adapter = null;
            var manager = new HsmsConnectionManager(
                () => { adapter = new MessageStubAdapter(); return adapter; },
                new StubLogger());

            await manager.StartAsync(DefaultProfile());

            for (int i = 0; i < 10; i++)
            {
                var msg = new SecsMessageData(1, 13, true, 0, SecsItemData.Ascii($"msg{i}"));
                var result = await manager.SendAsync(msg);
                Assert.True(result.IsSuccess);
            }

            Assert.Equal(10, adapter.SendCount);
        }
    }
}
