using System.Configuration;
using HostSimulator.Domain;

namespace HostSimulator.Infrastructure.Settings
{
    public class AppSettingsProvider : ISettingsProvider
    {
        public string GetSetting(string key)
        {
            return ConfigurationManager.AppSettings[key];
        }

        public string GetSetting(string key, string defaultValue)
        {
            return ConfigurationManager.AppSettings[key] ?? defaultValue;
        }
    }
}
