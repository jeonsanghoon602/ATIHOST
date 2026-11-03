using HostSimulator.Domain;
using HostSimulator.Infrastructure.Settings;
using Xunit;

namespace HostSimulator.Tests
{
    public class AppSettingsProviderTests
    {
        [Fact]
        public void GetSetting_ReturnsNullForMissingKey()
        {
            var provider = new AppSettingsProvider();
            var result = provider.GetSetting("NonExistentKey");
            Assert.Null(result);
        }

        [Fact]
        public void GetSettingWithDefault_ReturnsDefaultForMissingKey()
        {
            var provider = new AppSettingsProvider();
            var result = provider.GetSetting("NonExistentKey", "fallback");
            Assert.Equal("fallback", result);
        }
    }
}
