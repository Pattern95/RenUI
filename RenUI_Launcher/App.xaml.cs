using System;
using System.IO;
using System.Windows;

namespace RenUI_Launcher
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            AppDomain.CurrentDomain.UnhandledException += (s, ev) =>
            {
                try
                {
                    File.WriteAllText("RenUI_crash.log", ev.ExceptionObject.ToString());
                    MessageBox.Show(ev.ExceptionObject.ToString(), "RenUI Launcher Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                catch { }
            };

            DispatcherUnhandledException += (s, ev) =>
            {
                try
                {
                    File.WriteAllText("RenUI_crash.log", ev.Exception.ToString());
                    MessageBox.Show(ev.Exception.ToString(), "RenUI Launcher Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                catch { }
                ev.Handled = true;
            };

            base.OnStartup(e);
        }
    }
}
