namespace HostSimulator.Domain
{
    public interface ISettingsProvider
    {
        string GetSetting(string key);
        string GetSetting(string key, string defaultValue);
    }
}
