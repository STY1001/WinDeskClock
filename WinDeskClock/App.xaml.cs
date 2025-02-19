using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;

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
            string args = "";
            if (StartupOptions.FullScreen && !StartupOptions.KioskMode)
            {
                args += " -fullscreen";
            }

            if (StartupOptions.KioskMode)
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
    }

}
