using System;
using Secs4Net;
using DomainLogger = HostSimulator.Domain.ILogger;

namespace HostSimulator.Protocol
{
    internal sealed class Secs4NetLoggerBridge : ISecsGemLogger
    {
        private readonly DomainLogger _logger;

        public Secs4NetLoggerBridge(DomainLogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public void Info(string msg) => _logger.Info(msg);

        public void Warning(string msg) => _logger.Warn(msg);

        public void Error(string msg) => _logger.Error(msg);

        public void Error(string msg, Exception ex) =>
            _logger.Error(msg + " | " + ex.Message);

        public void Error(string msg, SecsMessage message = null, Exception ex = null)
        {
            if (ex != null)
                _logger.Error(msg + " | " + ex.Message);
            else
                _logger.Error(msg);
        }

        public void Debug(string msg) => _logger.Info("[DEBUG] " + msg);

        public void MessageIn(SecsMessage msg, int id)
            => _logger.Info("SECS IN [" + id + "]: " + msg);

        public void MessageOut(SecsMessage msg, int id)
            => _logger.Info("SECS OUT [" + id + "]: " + msg);
    }
}
