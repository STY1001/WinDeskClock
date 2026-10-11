using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using WinDeskClock.Services;
using WinDeskClock.Utils;
using WinDeskClock.ViewModels;

namespace WinDeskClock
{
    /// <summary>
    /// Interaction logic for App.xaml.
    ///
    /// This is the app's composition root: every service and ViewModel is constructed exactly
    /// once here, in OnStartup, and handed down through plain constructor parameters (no DI
    /// container, no service locator - see the MVVM notes in ViewModels/MainViewModel.cs for why).
    /// </summary>
    public partial class App : Application
    {
        // App version
        // - [major].[minor].[build number]
        // - If build number is 0, it is a release version
        public static string AppVersion = "0.1.2233090326";
        public static string AppSlogan = "Transform your Windows Tablet (or PC) into a Smart Clock";

        public static class StartupOptions
        {
            public static bool FullScreen = false;
            public static bool KioskMode = false;
        }

        public static async Task RestartApp()
        {
            _ = Log.Info("Restarting WinDeskClock...");
            var mainWindow = (MainWindow)Application.Current.MainWindow;
            string args = "";
            if (ConsoleLaunched)
            {
                args += " -console";
            }

            bool isFullScreen = mainWindow.ViewModel.IsFullScreen;
            bool isKiosk = mainWindow.ViewModel.IsKioskMode;

            if (isFullScreen && !isKiosk)
            {
                args += " -fullscreen";
            }
            if (isKiosk)
            {
                args += " -kiosk";
            }

            _ = Log.Info("Command line arguments for restart: " + args.Trim());
            Process.Start(Environment.ProcessPath!, args);
            Application.Current.Shutdown();
        }

        private async Task PrintVersion()
        {
            Console.WriteLine("WinDeskClock version " + AppVersion);
            Console.WriteLine(AppSlogan);
            Console.WriteLine("by STY1001");
            Console.WriteLine();
        }

        private static bool ConsoleLaunched = false;
        private async Task LaunchConsole()
        {
            _ = Log.Info("Launching console...");
            if (!ConsoleLaunched)
            {
                [DllImport("kernel32.dll")]
                static extern bool AllocConsole();
                AllocConsole();
                ConsoleLaunched = true;
                await PrintVersion();
            }
            else
            {
                _ = Log.Warning("Console already launched, skipping...");
            }
        }

        private async void OnStartup(object sender, StartupEventArgs e)
        {
            _ = Log.Write2File(""); // Create a useless line to separate logs from different runs
            _ = Log.Info("Starting WinDeskClock...");
            _ = Log.Info("WinDeskClock version " + AppVersion);
            string[] args = e.Args;
            _ = Log.Info("Command line arguments: " + string.Join(" ", args));

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

            // ----- Composition root: build services, then the ViewModel tree, then the window. -----
            var config = new ConfigService();
            var localization = new LocalizationService();
            var alarmRepository = new AlarmRepository();
            var pluginCatalog = new PluginCatalogService();
            var shellInterop = new ShellInteropService();
            var fileDialogService = new FileDialogService();
            var processLauncher = new ProcessLauncherService();
            var audioVolumeService = new AudioVolumeService();
            var cameraService = new CameraService();
            // Separate SoundService instances so an alarm ringing and a timer finishing don't stop each other's sound.
            var alarmSoundService = new SoundService();
            var timerSoundService = new SoundService();

            var clockVM = new ClockViewModel(localization, config);
            var miniClockVM = new MiniClockViewModel();
            var stopwatchVM = new StopwatchViewModel();
            var timerVM = new TimerViewModel(config, timerSoundService);
            var alarmsVM = new AlarmsViewModel(alarmRepository, localization, fileDialogService, config, alarmSoundService);
            var generalVM = new GeneralSettingsViewModel(config, localization, fileDialogService, audioVolumeService, pluginCatalog);
            var pluginManagerVM = new PluginManagerViewModel(pluginCatalog, processLauncher, config);
            var aboutVM = new AboutViewModel();
            var settingsVM = new SettingsViewModel(config, generalVM, pluginManagerVM, aboutVM);
            var mirrorVM = new MirrorViewModel(cameraService);
            var pluginCarouselVM = new PluginCarouselViewModel(pluginCatalog, config);

            var mainViewModel = new MainViewModel(config, clockVM, miniClockVM, stopwatchVM, timerVM, alarmsVM, settingsVM, mirrorVM, pluginCarouselVM);

            _ = Log.Info("Launching the main window...");
            var mainWindow = new MainWindow(mainViewModel, config, localization, pluginCatalog, shellInterop);
            mainWindow.Show();
        }

        private async void Application_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            // When debugged, don't handle the exception, let Visual Studio catch it
            if (Debugger.IsAttached)
            {
                return;
            }
            else
            {
                e.Handled = true;
                Exception exp = e.Exception;
                MessageBoxResult result = System.Windows.MessageBox.Show($"WinDeskClock has crashed.\n\n\nReason:\n{exp.Message}\n\n\n - Yes: Restart WinDeskClock\n\n - No: Close WinDeskClock\n\n - Cancel: Try to continue (can be unstable)", "WinDeskClock crash handler", MessageBoxButton.YesNoCancel, MessageBoxImage.Error);
                if (result == MessageBoxResult.Yes)
                {
                    await RestartApp();
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
}
