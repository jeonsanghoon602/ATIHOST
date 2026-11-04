using HostSimulator.Domain;
using HostSimulator.Infrastructure.Logging;
using HostSimulator.Infrastructure.Settings;
using HostSimulator.Protocol;
using Xunit;

namespace HostSimulator.Tests
{
    public class SolutionStructureTests
    {
        [Fact]
        public void FileLogger_ImplementsILogger()
        {
            Assert.True(typeof(ILogger).IsAssignableFrom(typeof(FileLogger)));
        }

        [Fact]
        public void AppSettingsProvider_ImplementsISettingsProvider()
        {
            Assert.True(typeof(ISettingsProvider).IsAssignableFrom(typeof(AppSettingsProvider)));
        }

        [Fact]
        public void Secs4NetAdapter_ImplementsIProtocolAdapter()
        {
            Assert.True(typeof(IProtocolAdapter).IsAssignableFrom(typeof(Secs4NetAdapter)));
        }
    }
}
