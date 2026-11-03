using System;
using System.IO;
using HostSimulator.Infrastructure.Logging;
using Xunit;

namespace HostSimulator.Tests
{
    public class FileLoggerTests : IDisposable
    {
        private readonly string _tempDir;

        public FileLoggerTests()
        {
            _tempDir = Path.Combine(Path.GetTempPath(), "HostSimTests_" + Guid.NewGuid().ToString("N"));
        }

        public void Dispose()
        {
            if (Directory.Exists(_tempDir))
            {
                Directory.Delete(_tempDir, true);
            }
        }

        private string ReadSingleLogContent()
        {
            var files = Directory.GetFiles(_tempDir, "*.log");
            Assert.Single(files);
            return File.ReadAllText(files[0]);
        }

        [Fact]
        public void Constructor_CreatesLogDirectory()
        {
            var logger = new FileLogger(_tempDir);
            Assert.True(Directory.Exists(_tempDir));
        }

        [Fact]
        public void Info_WritesEntryToLogFile()
        {
            var logger = new FileLogger(_tempDir);
            logger.Info("test message");

            var content = ReadSingleLogContent();
            Assert.Contains("[INFO]", content);
            Assert.Contains("test message", content);
        }

        [Fact]
        public void Error_WritesEntryToLogFile()
        {
            var logger = new FileLogger(_tempDir);
            logger.Error("error occurred");

            var content = ReadSingleLogContent();
            Assert.Contains("[ERROR]", content);
            Assert.Contains("error occurred", content);
        }

        [Fact]
        public void Warn_WritesEntryToLogFile()
        {
            var logger = new FileLogger(_tempDir);
            logger.Warn("warning issued");

            var content = ReadSingleLogContent();
            Assert.Contains("[WARN]", content);
            Assert.Contains("warning issued", content);
        }

        [Fact]
        public void Constructor_ThrowsOnNullDirectory()
        {
            Assert.Throws<ArgumentNullException>(() => new FileLogger(null));
        }

        [Fact]
        public void Constructor_ThrowsIOExceptionForInvalidPath()
        {
            Directory.CreateDirectory(_tempDir);
            var blocker = Path.Combine(_tempDir, "blocker");
            File.WriteAllText(blocker, "x");
            var invalidPath = Path.Combine(blocker, "subdir");
            Assert.ThrowsAny<IOException>(() => new FileLogger(invalidPath));
        }

        [Fact]
        public void Constructor_ThrowsArgumentExceptionForEmptyPath()
        {
            Assert.ThrowsAny<ArgumentException>(() => new FileLogger(string.Empty));
        }
    }
}
