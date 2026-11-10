using System;
using System.IO;
using System.Text;
using HostSimulator.Domain;

namespace HostSimulator.Infrastructure.Logging
{
    public sealed class MessageFileLogger : IMessageLogger
    {
        private readonly string _logDirectory;
        private readonly object _lock = new object();

        public MessageFileLogger(string logDirectory)
        {
            _logDirectory = logDirectory ?? throw new ArgumentNullException(nameof(logDirectory));
            if (!Directory.Exists(_logDirectory))
                Directory.CreateDirectory(_logDirectory);
        }

        public void Log(MessageLogEntry entry)
        {
            if (entry == null)
                return;

            var fileName = $"secs2_{entry.Timestamp:yyyyMMdd}.log";
            var filePath = Path.Combine(_logDirectory, fileName);

            var sb = new StringBuilder();
            sb.AppendLine($"[{entry.Timestamp:yyyy-MM-dd HH:mm:ss.fff}] {entry.Direction} {entry.Header}");
            sb.AppendLine($"  Parsed: {entry.ParsedText}");

            if (entry.RawBytes != null && entry.RawBytes.Length > 0)
                sb.AppendLine($"  Raw: {BitConverter.ToString(entry.RawBytes)}");

            sb.AppendLine();

            lock (_lock)
            {
                File.AppendAllText(filePath, sb.ToString(), Encoding.UTF8);
            }
        }
    }
}
