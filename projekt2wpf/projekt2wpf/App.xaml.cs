using System;
using System.IO;
using System.Windows;
using System.Windows.Threading;

namespace projekt2wpf
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            DispatcherUnhandledException += OnDispatcherUnhandledException;
            AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
        }

        private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            LogCrash(e.Exception);
            MessageBox.Show(
                $"Nieoczekiwany błąd:\n{e.Exception.Message}\n\nSzczegóły zapisano w {CrashLogPath()}",
                "Libraria — błąd",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            e.Handled = true;
        }

        private static void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            if (e.ExceptionObject is Exception ex)
                LogCrash(ex);
        }

        private static void LogCrash(Exception ex)
        {
            try
            {
                File.AppendAllText(CrashLogPath(),
                    $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}]\n{ex}\n\n");
            }
            catch { }
        }

        private static string CrashLogPath()
            => Path.Combine(Path.GetTempPath(), "libraria_crash.log");
    }
}
