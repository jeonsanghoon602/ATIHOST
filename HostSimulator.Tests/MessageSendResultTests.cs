using System;
using HostSimulator.Domain;
using Xunit;

namespace HostSimulator.Tests
{
    public class MessageSendResultTests
    {
        [Fact]
        public void Success_stores_reply()
        {
            var reply = new SecsMessageData(1, 2, false, 1, null);
            var result = MessageSendResult.Success(reply);

            Assert.Equal(MessageSendStatus.Success, result.Status);
            Assert.True(result.IsSuccess);
            Assert.Same(reply, result.Reply);
            Assert.Null(result.ErrorMessage);
        }

        [Fact]
        public void Success_null_reply_throws()
        {
            Assert.Throws<ArgumentNullException>(() => MessageSendResult.Success(null));
        }

        [Fact]
        public void T3Timeout_stores_system_byte()
        {
            var result = MessageSendResult.T3Timeout(42);

            Assert.Equal(MessageSendStatus.T3Timeout, result.Status);
            Assert.False(result.IsSuccess);
            Assert.Null(result.Reply);
            Assert.Contains("42", result.ErrorMessage);
        }

        [Fact]
        public void Abort_stores_message()
        {
            var abort = new SecsMessageData(1, 0, false, 1, null);
            var result = MessageSendResult.Abort(abort);

            Assert.Equal(MessageSendStatus.Abort, result.Status);
            Assert.Same(abort, result.Reply);
        }

        [Fact]
        public void NotConnected()
        {
            var result = MessageSendResult.NotConnected();

            Assert.Equal(MessageSendStatus.NotConnected, result.Status);
            Assert.False(result.IsSuccess);
        }

        [Fact]
        public void Error_stores_message()
        {
            var result = MessageSendResult.Error("test error");

            Assert.Equal(MessageSendStatus.Error, result.Status);
            Assert.Contains("test error", result.ErrorMessage);
        }

        [Fact]
        public void ToString_success()
        {
            var reply = new SecsMessageData(1, 2, false, 1, null);
            var result = MessageSendResult.Success(reply);
            Assert.Contains("Success", result.ToString());
        }

        [Fact]
        public void ToString_timeout()
        {
            var result = MessageSendResult.T3Timeout(1);
            Assert.Contains("T3Timeout", result.ToString());
        }
    }
}
