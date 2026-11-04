using System;
using System.Diagnostics;
using System.IO;
using System.Security;
using System.Windows;
using HostSimulator.Infrastructure.Logging;
using HostSimulator.Infrastructure.Settings;

namespace HostSimulator.App
{
    public partial class App : System.Windows.Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            try
            {
                var settings = new AppSettingsProvider();
                var logDirectory = settings.GetSetting("LogDirectory", "logs");
                var logger = new FileLogger(logDirectory);
                logger.Info("Application started");
            }
            catch (Exception ex) when (
                ex is ArgumentException ||
                ex is IOException ||
                ex is UnauthorizedAccessException ||
                ex is SecurityException ||
                ex is NotSupportedException)
            {
                Trace.TraceError("Log initialization failed: " + ex.Message);
            }
        }
    }
}
