using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Markup;

namespace WinDeskClock
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        // App version
        // - [major].[minor].[build number]
        // - If build number is 0, it is a release version
        public static string AppVersion = "0.1.1";
        public static string AppSlogan = "Transform your Windows Tablet (or PC) into a Smart Clock";

        public static class StartupOptions
        {
            public static bool FullScreen = false;
            public static bool KioskMode = false;
        }

        public static async Task RestartApp()
        {
            var mainWindow = (MainWindow)Application.Current.MainWindow;
            string args = "";
            if (/*(StartupOptions.FullScreen && !StartupOptions.KioskMode) ||*/ ((mainWindow.FullScreenBtn.IsChecked == true) && (mainWindow.KioskModeBtn.IsChecked == false)))
            {
                args += " -fullscreen";
            }

            if (/*StartupOptions.KioskMode ||*/ (mainWindow.KioskModeBtn.IsChecked == true))
            {
                args += " -kiosk";
            }

            Process.Start(Environment.ProcessPath, args);
            Application.Current.Shutdown();
        }

        private async Task PrintVersion()
        {
            Console.WriteLine("WinDeskClock version " + AppVersion);
            Console.WriteLine(AppSlogan);
            Console.WriteLine("by STY1001");
            Console.WriteLine();
        }

        private bool ConsoleLaunched = false;
        private async Task LaunchConsole()
        {
            if(!ConsoleLaunched)
            {
                [DllImport("kernel32.dll")]
                static extern bool AllocConsole();
                AllocConsole();
                ConsoleLaunched = true;
                await PrintVersion();
            }
        }

        private async void OnStartup(object sender, StartupEventArgs e)
        {
            string[] args = e.Args;

            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] == "-console")
                {
                    await LaunchConsole();
                }
                if (args[i] == "-help")
                {
                    await LaunchConsole();
                    Console.WriteLine("Usage: WinDeskClock.exe [options]");
                    Console.WriteLine("Options:");
                    Console.WriteLine("  -help: Show this help message");
                    Console.WriteLine("  -console: Start with console");
                    Console.WriteLine("  -fullscreen: Start in fullscreen mode");
                    Console.WriteLine("  -kiosk: Start in kiosk mode (fullscreen auto trigger)");
                    Console.ReadKey();
                    Application.Current.Shutdown();
                }
                if (args[i] == "-fullscreen")
                {
                    StartupOptions.FullScreen = true;
                }
                if (args[i] == "-kiosk")
                {
                    StartupOptions.KioskMode = true;
                }
            }

            var mainWindow = new MainWindow();
            mainWindow.Show();
        }

        private async void Application_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            e.Handled = true;
            Exception exp = e.Exception;
            MessageBoxResult result = System.Windows.MessageBox.Show($"WinDeskClock has crashed.\n\n\nReason:\n{exp.Message}\n\n\n - Yes: Restart WinDeskClock\n\n - No: Close WinDeskClock\n\n - Cancel: Try to continue (can be unstable)", "WinDeskClock crash handler", MessageBoxButton.YesNoCancel, MessageBoxImage.Error);
            if (result == MessageBoxResult.Yes)
            {
                RestartApp();

            }
            else if (result == MessageBoxResult.No)
            {
                Application.Current.Shutdown();
            }
            else if (result == MessageBoxResult.Cancel)
            {

            }
        }
    }

}
