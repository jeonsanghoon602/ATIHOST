using System;
using System.Threading;
using System.Threading.Tasks;
using HostSimulator.Domain;
using Microsoft.Extensions.Options;
using Secs4Net;

namespace HostSimulator.Protocol
{
    public sealed class Secs4NetAdapter : IProtocolAdapter
    {
        private readonly ISecsGemLogger _secsLogger;
        private readonly Domain.ILogger _logger;
        private readonly IMessageLogger _messageLogger;
        private readonly HsmsStateMachine _stateMachine;
        private readonly SystemByteGenerator _systemByteGenerator = new SystemByteGenerator();
        private HsmsConnection _connection;
        private SecsGem _secsGem;
        private CancellationTokenSource _lifetimeCts;
        private Task _receiveTask;
        private readonly object _lock = new object();
        private bool _disposed;

        public ProtocolConnectionState State => _stateMachine.CurrentState;

        public event EventHandler<StateChangedEventArgs> StateChanged
        {
            add { _stateMachine.StateChanged += value; }
            remove { _stateMachine.StateChanged -= value; }
        }

        public event EventHandler<PrimaryMessageReceivedEventArgs> PrimaryMessageReceived;
        public event EventHandler<SecsMessageData> UnexpectedMessageReceived;

        public Secs4NetAdapter(Domain.ILogger logger, IMessageLogger messageLogger = null)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _messageLogger = messageLogger;
            _secsLogger = new Secs4NetLoggerBridge(logger);
            _stateMachine = new HsmsStateMachine(logger);
        }

        public async Task ConnectAsync(
            ConnectionParameters parameters,
            TimeSpan timeout,
            CancellationToken cancellationToken = default)
        {
            if (parameters == null)
                throw new ArgumentNullException(nameof(parameters));
            if (timeout <= TimeSpan.Zero)
                throw new ArgumentOutOfRangeException(nameof(timeout), "Timeout must be greater than zero.");

            HsmsConnection conn;
            SecsGem gem;
            CancellationTokenSource lifetimeCts;
            var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

            lock (_lock)
            {
                if (_disposed)
                    throw new ObjectDisposedException(nameof(Secs4NetAdapter));
                if (_connection != null)
                    throw new InvalidOperationException("Already connected. Disconnect first.");

                _stateMachine.TryTransition(ProtocolConnectionState.Connecting, StateChangeReason.UserRequest);

                var options = Options.Create(new SecsGemOptions
                {
                    IpAddress = parameters.IpAddress.ToString(),
                    Port = parameters.Port,
                    IsActive = parameters.IsActive,
                    DeviceId = (ushort)parameters.DeviceId,
                    T3 = parameters.T3Seconds * 1000,
                    T5 = parameters.T5Seconds * 1000,
                    T6 = parameters.T6Seconds * 1000,
                    T7 = parameters.T7Seconds * 1000,
                    T8 = parameters.T8Seconds * 1000,
                    LinkTestInterval = parameters.LinkTestIntervalSeconds * 1000,
                });

                lifetimeCts = new CancellationTokenSource();
                conn = new HsmsConnection(options, _secsLogger);
                gem = new SecsGem(options, conn, _secsLogger);
                conn.ConnectionChanged += OnConnectionChanged;
                _connection = conn;
                _secsGem = gem;
                _lifetimeCts = lifetimeCts;
            }

            void onConnected(object s, ConnectionState state)
            {
                if (state == ConnectionState.Selected)
                    tcs.TrySetResult(true);
                else if (state == ConnectionState.Retry)
                    tcs.TrySetException(new InvalidOperationException(
                        "Connection entered Retry during initial connection."));
            }

            conn.ConnectionChanged += onConnected;
            try
            {
                conn.Start(lifetimeCts.Token);

                if (conn.State == ConnectionState.Selected)
                    return;

                using (var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken))
                {
                    timeoutCts.CancelAfter(timeout);
                    using (timeoutCts.Token.Register(() => tcs.TrySetCanceled()))
                    {
                        await tcs.Task.ConfigureAwait(false);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                await CleanupConnectionAsync().ConfigureAwait(false);

                if (!cancellationToken.IsCancellationRequested)
                {
                    _stateMachine.ForceState(ProtocolConnectionState.Disconnected, StateChangeReason.CommunicationError);
                    throw new TimeoutException(
                        $"Connection did not reach Selected state within {timeout.TotalSeconds}s.");
                }

                _stateMachine.ForceState(ProtocolConnectionState.Disconnected, StateChangeReason.UserRequest);
                throw;
            }
            catch (Exception)
            {
                await CleanupConnectionAsync().ConfigureAwait(false);
                _stateMachine.ForceState(ProtocolConnectionState.Disconnected, StateChangeReason.CommunicationError);
                throw;
            }
            finally
            {
                conn.ConnectionChanged -= onConnected;
            }
        }

        public async Task DisconnectAsync()
        {
            HsmsConnection conn;
            SecsGem gem;
            CancellationTokenSource lifetimeCts;
            Task receiveTask;

            lock (_lock)
            {
                conn = _connection;
                _connection = null;
                gem = _secsGem;
                _secsGem = null;
                lifetimeCts = _lifetimeCts;
                _lifetimeCts = null;
                receiveTask = _receiveTask;
                _receiveTask = null;
            }

            _stateMachine.TryTransition(ProtocolConnectionState.Disconnecting, StateChangeReason.UserRequest);

            if (lifetimeCts != null)
            {
                lifetimeCts.Cancel();
                lifetimeCts.Dispose();
            }

            if (receiveTask != null)
            {
                try { await receiveTask.ConfigureAwait(false); }
                catch (OperationCanceledException) { }
                catch (Exception ex) { _logger.Warn($"Receive loop error on disconnect: {ex.Message}"); }
            }

            if (gem != null)
                gem.Dispose();

            if (conn != null)
            {
                conn.ConnectionChanged -= OnConnectionChanged;
                await conn.DisposeAsync().ConfigureAwait(false);
            }

            _stateMachine.ForceState(ProtocolConnectionState.Disconnected, StateChangeReason.UserRequest);
        }

        public async Task<bool> LinktestAsync(TimeSpan timeout, CancellationToken cancellationToken = default)
        {
            HsmsConnection conn;
            lock (_lock)
            {
                if (_disposed)
                    throw new ObjectDisposedException(nameof(Secs4NetAdapter));
                conn = _connection;
            }

            if (conn == null || State != ProtocolConnectionState.Selected)
            {
                _logger.Warn("Linktest requested but not in Selected state.");
                return false;
            }

            var stateBefore = conn.State;
            if (stateBefore != ConnectionState.Selected)
            {
                _logger.Warn($"Linktest: SDK state is {stateBefore}, not Selected.");
                return false;
            }

            var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

            void onStateChange(object sender, ConnectionState state)
            {
                if (state != ConnectionState.Selected)
                    tcs.TrySetResult(false);
            }

            conn.ConnectionChanged += onStateChange;
            try
            {
                conn.LinkTestEnabled = false;
                conn.LinkTestEnabled = true;

                using (var cts = new CancellationTokenSource())
                {
                    cts.CancelAfter(timeout);

                    using (cancellationToken.Register(() => tcs.TrySetCanceled(cancellationToken)))
                    using (cts.Token.Register(() => tcs.TrySetResult(true)))
                    {
                        var result = await tcs.Task.ConfigureAwait(false);
                        _logger.Info($"Linktest result: {result} (state={conn.State})");
                        return result;
                    }
                }
            }
            finally
            {
                conn.ConnectionChanged -= onStateChange;
            }
        }

        public async Task<MessageSendResult> SendAsync(SecsMessageData message, CancellationToken cancellationToken = default)
        {
            if (message == null)
                throw new ArgumentNullException(nameof(message));

            SecsGem gem;
            lock (_lock)
            {
                if (_disposed)
                    throw new ObjectDisposedException(nameof(Secs4NetAdapter));
                gem = _secsGem;
            }

            if (gem == null || State != ProtocolConnectionState.Selected)
                return MessageSendResult.NotConnected();

            var systemByte = _systemByteGenerator.Next();
            var outgoing = new SecsMessageData(
                message.Stream, message.Function, message.WBit, systemByte,
                message.Item, message.RawBytes, message.Timestamp);

            var secsMsg = SecsMessageConverter.ToSecs4Net(outgoing);
            LogMessage(outgoing, MessageDirection.Sent);
            _logger.Info($"Send: {outgoing.ToHeaderString()}");

            try
            {
                var reply = await gem.SendAsync(secsMsg, cancellationToken).ConfigureAwait(false);

                if (reply == null)
                {
                    if (!message.WBit)
                        return MessageSendResult.Success(outgoing);
                    return MessageSendResult.Error("No reply received");
                }

                var replyData = SecsMessageConverter.FromSecs4Net(reply, systemByte);
                LogMessage(replyData, MessageDirection.Received);

                if (reply.F == 0)
                {
                    _logger.Warn($"Abort received: {replyData.ToHeaderString()}");
                    return MessageSendResult.Abort(replyData);
                }

                if (reply.S == 9)
                {
                    _logger.Warn($"S9 error received: {replyData.ToHeaderString()}");
                    return MessageSendResult.Error($"S9 error: {replyData.ToHeaderString()}");
                }

                _logger.Info($"Reply: {replyData.ToHeaderString()}");
                return MessageSendResult.Success(replyData);
            }
            catch (SecsException ex) when (ex.Message != null && ex.Message.Contains("T3"))
            {
                _logger.Warn($"T3 timeout: SystemByte={systemByte}, S{message.Stream}F{message.Function}");
                return MessageSendResult.T3Timeout(systemByte);
            }
            catch (SecsException ex)
            {
                _logger.Error($"SECS error: {ex.Message}");
                return MessageSendResult.Error(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.Error($"Send failed: {ex.Message}");
                return MessageSendResult.Error(ex.Message);
            }
        }

        public Task ReplyAsync(SecsMessageData primaryMessage, SecsMessageData replyMessage)
        {
            if (primaryMessage == null)
                throw new ArgumentNullException(nameof(primaryMessage));
            if (replyMessage == null)
                throw new ArgumentNullException(nameof(replyMessage));

            lock (_lock)
            {
                if (_disposed)
                    throw new ObjectDisposedException(nameof(Secs4NetAdapter));
            }

            if (State != ProtocolConnectionState.Selected)
                throw new InvalidOperationException("Not in Selected state.");

            var secsReply = SecsMessageConverter.ToSecs4Net(replyMessage);
            LogMessage(replyMessage, MessageDirection.Sent);
            _logger.Info($"Reply sent: {replyMessage.ToHeaderString()}");

            return Task.CompletedTask;
        }

        public void Dispose()
        {
            HsmsConnection conn;
            SecsGem gem;
            CancellationTokenSource lifetimeCts;

            lock (_lock)
            {
                if (_disposed) return;
                _disposed = true;
                conn = _connection;
                _connection = null;
                gem = _secsGem;
                _secsGem = null;
                lifetimeCts = _lifetimeCts;
                _lifetimeCts = null;
            }

            if (lifetimeCts != null)
            {
                lifetimeCts.Cancel();
                lifetimeCts.Dispose();
            }

            if (gem != null)
                gem.Dispose();

            if (conn != null)
            {
                conn.ConnectionChanged -= OnConnectionChanged;
                conn.DisposeAsync().AsTask().GetAwaiter().GetResult();
            }

            _stateMachine.ForceState(ProtocolConnectionState.Disconnected, StateChangeReason.Disposed);
        }

        private async Task CleanupConnectionAsync()
        {
            HsmsConnection conn;
            SecsGem gem;
            CancellationTokenSource lifetimeCts;
            Task receiveTask;

            lock (_lock)
            {
                conn = _connection;
                _connection = null;
                gem = _secsGem;
                _secsGem = null;
                lifetimeCts = _lifetimeCts;
                _lifetimeCts = null;
                receiveTask = _receiveTask;
                _receiveTask = null;
            }

            if (lifetimeCts != null)
            {
                lifetimeCts.Cancel();
                lifetimeCts.Dispose();
            }

            if (receiveTask != null)
            {
                try { await receiveTask.ConfigureAwait(false); }
                catch { }
            }

            if (gem != null)
                gem.Dispose();

            if (conn != null)
            {
                conn.ConnectionChanged -= OnConnectionChanged;
                await conn.DisposeAsync().ConfigureAwait(false);
            }
        }

        private void OnConnectionChanged(object sender, ConnectionState state)
        {
            var reason = MapReason(state, _stateMachine.CurrentState);
            _logger.Info($"ConnectionChanged: {state} ({reason})");

            if (state == ConnectionState.Retry)
            {
                lock (_lock)
                {
                    _lifetimeCts?.Cancel();
                }
                _stateMachine.ForceState(ProtocolConnectionState.Disconnected, reason);
                return;
            }

            var mapped = MapState(state);
            _stateMachine.TryTransition(mapped, reason);

            if (state == ConnectionState.Selected)
            {
                var conn = _connection;
                if (conn != null)
                {
                    conn.LinkTestEnabled = true;
                    StartReceiveLoop();
                }
            }
        }

        private void StartReceiveLoop()
        {
            CancellationToken ct;
            lock (_lock)
            {
                if (_lifetimeCts == null || _lifetimeCts.IsCancellationRequested)
                    return;
                ct = _lifetimeCts.Token;
                if (_receiveTask != null && !_receiveTask.IsCompleted)
                    return;
                var gem = _secsGem;
                if (gem == null)
                    return;
                _receiveTask = Task.Run(() => ReceiveLoopAsync(gem, ct), ct);
            }
        }

        private async Task ReceiveLoopAsync(ISecsGem gem, CancellationToken ct)
        {
            try
            {
                var enumerator = gem.GetPrimaryMessageAsync(ct).GetAsyncEnumerator(ct);
                try
                {
                    while (await enumerator.MoveNextAsync().ConfigureAwait(false))
                    {
                        var wrapper = enumerator.Current;
                        try
                        {
                            HandlePrimaryMessage(wrapper);
                        }
                        finally
                        {
                            // PrimaryMessageWrapper does not implement IDisposable;
                            // SecsMessage (PrimaryMessage) does — dispose it.
                            wrapper.PrimaryMessage?.Dispose();
                        }
                    }
                }
                finally
                {
                    await enumerator.DisposeAsync().ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                _logger.Error($"Receive loop error: {ex.Message}");
            }
        }

        private void HandlePrimaryMessage(PrimaryMessageWrapper wrapper)
        {
            try
            {
                var msg = wrapper.PrimaryMessage;
                var systemByte = _systemByteGenerator.Next();
                var data = SecsMessageConverter.FromSecs4Net(msg, systemByte);
                LogMessage(data, MessageDirection.Received);
                _logger.Info($"Primary received: {data.ToHeaderString()}");

                if (msg.S == 9)
                {
                    _logger.Warn($"S9 error message received: {data.ToHeaderString()}");
                    UnexpectedMessageReceived?.Invoke(this, data);
                    return;
                }

                if (msg.F == 0)
                {
                    _logger.Warn($"SxF0 abort message received: {data.ToHeaderString()}");
                    UnexpectedMessageReceived?.Invoke(this, data);
                    return;
                }

                PrimaryMessageReceived?.Invoke(this, new PrimaryMessageReceivedEventArgs(data));
            }
            catch (Exception ex)
            {
                _logger.Error($"Error handling primary message: {ex.Message}");
            }
        }

        private void LogMessage(SecsMessageData message, MessageDirection direction)
        {
            if (_messageLogger == null)
                return;

            try
            {
                var entry = MessageLogEntry.FromMessage(message, direction);
                _messageLogger.Log(entry);
            }
            catch (Exception ex)
            {
                _logger.Error($"Message logging failed: {ex.Message}");
            }
        }

        public static ProtocolConnectionState MapState(ConnectionState secsState)
        {
            switch (secsState)
            {
                case ConnectionState.Connecting:
                    return ProtocolConnectionState.Connecting;
                case ConnectionState.Retry:
                    return ProtocolConnectionState.Connecting;
                case ConnectionState.Connected:
                    return ProtocolConnectionState.Connected;
                case ConnectionState.Selected:
                    return ProtocolConnectionState.Selected;
                default:
                    return ProtocolConnectionState.Disconnected;
            }
        }

        // Secs4Net 2.4.x does not expose per-event detail for Retry:
        // socket errors, T6/T7/T8 timeouts, Separate, and Select rejection
        // all surface as a single Retry state. We map them honestly to
        // CommunicationError. Distinguishing SelectFailed from SelectTimeout
        // requires SDK-level signal that is not available.
        public static StateChangeReason MapReason(ConnectionState secsState, ProtocolConnectionState currentState)
        {
            switch (secsState)
            {
                case ConnectionState.Connecting:
                    return StateChangeReason.UserRequest;
                case ConnectionState.Connected:
                    return StateChangeReason.ConnectionEstablished;
                case ConnectionState.Selected:
                    return StateChangeReason.SelectCompleted;
                case ConnectionState.Retry:
                    return StateChangeReason.CommunicationError;
                default:
                    return StateChangeReason.CommunicationError;
            }
        }
    }
}
