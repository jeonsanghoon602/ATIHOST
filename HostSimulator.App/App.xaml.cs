using System.Windows;
using HostSimulator.Infrastructure.Logging;
using HostSimulator.Infrastructure.Settings;

namespace HostSimulator.App
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            var settings = new AppSettingsProvider();
            var logDirectory = settings.GetSetting("LogDirectory", "logs");
            var logger = new FileLogger(logDirectory);
            logger.Info("Application started");
        }
    }
}
