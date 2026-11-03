using System;
using System.IO;
using HostSimulator.Domain;

namespace HostSimulator.Infrastructure.Logging
{
    public class FileLogger : ILogger
    {
        private readonly string _logDirectory;
        private readonly object _lock = new object();

        public FileLogger(string logDirectory)
        {
            _logDirectory = logDirectory ?? throw new ArgumentNullException(nameof(logDirectory));
            if (!Directory.Exists(_logDirectory))
            {
                Directory.CreateDirectory(_logDirectory);
            }
        }

        public void Info(string message) => WriteLog("INFO", message);
        public void Warn(string message) => WriteLog("WARN", message);
        public void Error(string message) => WriteLog("ERROR", message);

        private void WriteLog(string level, string message)
        {
            var now = DateTime.Now;
            var fileName = Path.Combine(_logDirectory, $"{now:yyyyMMdd}.log");
            var entry = $"{now:yyyy-MM-dd HH:mm:ss.fff} [{level}] {message}{Environment.NewLine}";
            lock (_lock)
            {
                File.AppendAllText(fileName, entry);
            }
        }
    }
}
