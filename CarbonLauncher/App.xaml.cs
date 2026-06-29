using System;
using System.IO;
using System.Windows;
using System.Windows.Threading;

namespace CarbonLauncher
{
    public partial class App : Application
    {
        public App()
        {
            DispatcherUnhandledException += OnDispatcherUnhandledException;
            AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
        }

        private static void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            WriteFatalLog("AppDomain.CurrentDomain.UnhandledException", e.ExceptionObject as Exception);
        }

        private static void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            WriteFatalLog("Application.DispatcherUnhandledException", e.Exception);

            try
            {
                MessageBox.Show(
                    "Carbon Launcher hit a startup error. See logs/startup-fatal.log for details.",
                    "Carbon Launcher",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            catch
            {
            }

            e.Handled = true;
        }

        private static void WriteFatalLog(string source, Exception? exception)
        {
            try
            {
                string root = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "CarbonLauncher");
                string logsDirectory = Path.Combine(root, "logs");
                Directory.CreateDirectory(logsDirectory);

                string message = exception == null
                    ? "No exception object was provided."
                    : exception.ToString();

                File.AppendAllText(
                    Path.Combine(logsDirectory, "startup-fatal.log"),
                    $"[{DateTime.Now:O}] {source}{Environment.NewLine}{message}{Environment.NewLine}{Environment.NewLine}");
            }
            catch
            {
            }
        }
    }
}
