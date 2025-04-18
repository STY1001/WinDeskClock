using Microsoft.Win32;
using Newtonsoft.Json.Linq;
using System.Diagnostics;
using System.IO;
using System.Media;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using WinDeskClock.Utils;
using Wpf.Ui.Controls;
using Wpf.Ui.Extensions;

namespace WinDeskClock
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>

    public class User32
    {
        private const uint WM_SYSCOMMAND = 0x0112;
        private const uint SC_MONITORPOWER = 0xF170;
        private const int MONITOR_OFF = 2;
        private const int MONITOR_ON = -1;
        public uint User32WM_SYSCOMMAND { get { return WM_SYSCOMMAND; } }
        public uint User32SC_MONITORPOWER { get { return SC_MONITORPOWER; } }
        public int User32MONITOR_OFF { get { return MONITOR_OFF; } }
        public int User32MONITOR_ON { get { return MONITOR_ON; } }

        [DllImport("user32.dll")]
        private static extern int SendMessage(int hWnd, int hMsg, int wParam, int lParam);
        [DllImport("user32.dll")]
        private static extern int GetMessage(out MSG lpMsg, int hWnd, uint wMsgFilterMin, uint wMsgFilterMax);
        [DllImport("user32.dll")]
        private static extern IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong);
        [DllImport("user32.dll")]
        private static extern IntPtr GetWindowLongPtr(IntPtr hWnd, int nIndex);
        [DllImport("user32.dll")]
        private static extern IntPtr CallWindowProc(IntPtr lpPrevWndProc, IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);
        [DllImport("user32.dll")]
        private static extern IntPtr DefWindowProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);
        public int User32SendMessage(int hWnd, int hMsg, int wParam, int lParam)
        {
            return SendMessage(hWnd, hMsg, wParam, lParam);
        }
        public int User32GetMessage(out MSG lpMsg, int hWnd, uint wMsgFilterMin, uint wMsgFilterMax)
        {
            return GetMessage(out lpMsg, hWnd, wMsgFilterMin, wMsgFilterMax);
        }
        public IntPtr User32SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong)
        {
            return SetWindowLongPtr(hWnd, nIndex, dwNewLong);
        }
        public IntPtr User32GetWindowLongPtr(IntPtr hWnd, int nIndex)
        {
            return GetWindowLongPtr(hWnd, nIndex);
        }
        public IntPtr User32CallWindowProc(IntPtr lpPrevWndProc, IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam)
        {
            return CallWindowProc(lpPrevWndProc, hWnd, msg, wParam, lParam);
        }
        public IntPtr User32DefWindowProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam)
        {
            return DefWindowProc(hWnd, msg, wParam, lParam);
        }
    }

    public partial class MainWindow : FluentWindow
    {
        public User32 user32dll = new User32();

        private Page ClockPage;

        private List<Grid> MenuClockGrids = new List<Grid>();
        private DispatcherTimer time;
        private Stopwatch stopwatch;
        private DispatcherTimer stopwatchTimer;
        private DispatcherTimer timerTimer;
        private DispatcherTimer alarm;
        private DispatcherTimer carousel;
        private TimeSpan timer;

        private Dictionary<string, Page> SettingsTabs = new Dictionary<string, Page>();
        private List<string> CarouselPluginList = new List<string>();
        private List<string> PluginList = new List<string>();

        public MainWindow()
        {
            InitializeComponent();

            OverlayGrid.Visibility = Visibility.Visible;
            DownMenuClockGrid.Visibility = Visibility.Visible;
            DownMenuPluginGrid.Visibility = Visibility.Visible;

            // Prevent the window from being resized, minimized or windowed when FullScreenBtn mode is enabled
            this.StateChanged += async (sender, e) =>
            {
                await Task.Delay(100);
                if (FullScreenBtn.IsChecked == true && this.WindowState != WindowState.Maximized)
                {
                    WindowState = WindowState.Normal;
                    WindowStyle = WindowStyle.None;
                    WindowState = WindowState.Maximized;
                }
            };

            // Hide cursor after 3 seconds of inactivity
            this.MouseMove += MouseMoved;

            Loaded += MainWindow_Loaded;
        }

        // Change UI elements depending on the settings
        private async Task UIUpdate()
        {
            if (ConfigManager.Variables.ClockFbxStyle)
            {
                ClockPage = new Clocks.FbxClock();
            }
            else
            {
                ClockPage = new Clocks.FluentClock();
            }
            ClockFrame.Navigate(ClockPage);

            if (PluginLoader.PluginModules.Count != 0)
            {
                foreach (string id in PluginLoader.PluginModules.Keys)
                {
                    PluginList.Add(id);
                }
            }

            if (ConfigManager.Variables.PinnedPlugin.Count != 0)
            {
                foreach (string id in ConfigManager.Variables.PinnedPlugin)
                {
                    if (PluginList.Contains(id))
                    {
                        CarouselPluginList.Add(id);
                    }
                }
            }

            if (PluginList.Count == 0)
            {
                Grid grid = new Grid
                {
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    RowDefinitions =
                    {
                        new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) },
                        new RowDefinition { Height = new GridLength(10) },
                        new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) }
                    }
                };
                System.Windows.Controls.Image img = new System.Windows.Controls.Image
                {
                    Source = new BitmapImage(new Uri("pack://application:,,,/Resources/Icons/Plugin.png")),
                    Width = 72,
                    Height = 72
                };
                img.SetValue(Grid.RowProperty, 0);
                img.SetValue(RenderOptions.BitmapScalingModeProperty, BitmapScalingMode.HighQuality);
                Wpf.Ui.Controls.TextBlock tb = new Wpf.Ui.Controls.TextBlock
                {
                    Text = await LangSystem.GetLang("plugin.noload"),
                    FontSize = 20,
                    Foreground = (Brush)FindResource("TextFillColorPrimaryBrush")
                };
                tb.SetValue(Grid.RowProperty, 2);
                grid.Children.Add(img);
                grid.Children.Add(tb);
                CarouselPluginFrame.Navigate(grid);
                EnterDownMenuPluginBtn.IsEnabled = false;
            }
            else if (CarouselPluginList.Count == 0)
            {
                Grid grid = new Grid
                {
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    RowDefinitions =
                    {
                        new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) },
                        new RowDefinition { Height = new GridLength(10) },
                        new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) }
                    }
                };
                System.Windows.Controls.Image img = new System.Windows.Controls.Image
                {
                    Source = new BitmapImage(new Uri("pack://application:,,,/Resources/Icons/Carousel.png")),
                    Width = 72,
                    Height = 72
                };
                img.SetValue(Grid.RowProperty, 0);
                img.SetValue(RenderOptions.BitmapScalingModeProperty, BitmapScalingMode.HighQuality);
                Wpf.Ui.Controls.TextBlock tb = new Wpf.Ui.Controls.TextBlock
                {
                    Text = await LangSystem.GetLang("plugin.nopin"),
                    FontSize = 20,
                    Foreground = (Brush)FindResource("TextFillColorPrimaryBrush")
                };
                tb.SetValue(Grid.RowProperty, 2);
                grid.Children.Add(img);
                grid.Children.Add(tb);
                CarouselPluginFrame.Navigate(grid);
            }

            if (CarouselPluginList.Count != 0)
            {
                CarouselPluginFrame.Navigate(PluginLoader.PluginModules[CarouselPluginList[0]].GetMain());
            }

            if (!ConfigManager.Variables.BlurEffect)
            {
                AlarmAlertBlur.Visibility = Visibility.Collapsed;
                TimeUpBlur.Visibility = Visibility.Collapsed;
                SettingsBlur.Visibility = Visibility.Collapsed;
                GlobalMenuBlur.Visibility = Visibility.Collapsed;

                AlarmAlertBlur.Fill = new SolidColorBrush(Colors.Transparent);
                TimeUpBlur.Fill = new SolidColorBrush(Colors.Transparent);
                SettingsBlur.Fill = new SolidColorBrush(Colors.Transparent);
                GlobalMenuBlur.Fill = new SolidColorBrush(Colors.Transparent);

                AlarmAlertBlur.Effect = null;
                TimeUpBlur.Effect = null;
                SettingsBlur.Effect = null;
                GlobalMenuBlur.Effect = null;

                AlarmAlertBlur.CacheMode = null;
                TimeUpBlur.CacheMode = null;
                SettingsBlur.CacheMode = null;
                GlobalMenuBlur.CacheMode = null;
            }

            // Lang apply
            BackBtn.Content = await LangSystem.GetLang("mainmenu.back");
            ScreenOffBtn.Content = await LangSystem.GetLang("mainmenu.screenoff");
            FullScreenBtnText.Text = await LangSystem.GetLang("mainmenu.fullscreen");
            KioskModeBtnText.Text = await LangSystem.GetLang("mainmenu.kiosk");
            SettingsBtn.Content = await LangSystem.GetLang("mainmenu.settings");
            ExitAppBtn.Content = await LangSystem.GetLang("mainmenu.exit");
            AlarmAlertStopBtn.Content = await LangSystem.GetLang("alarm.stop");
            AlarmAlertText.Text = await LangSystem.GetLang("alarm.titlename");
            TimeUpStopBtn.Content = await LangSystem.GetLang("timer.stop");
            TimeUpText.Text = await LangSystem.GetLang("timer.timeup");
            AboutTabItemHeader.Text = await LangSystem.GetLang("settings.about.titlename");
            PMTabItemHeader.Text = await LangSystem.GetLang("settings.pluginmanager.titlename");
            GeneralTabItemHeader.Text = await LangSystem.GetLang("settings.general.titlename");
            SettingsSaveBtn.Content = await LangSystem.GetLang("settings.save");
            SettingsTitleText.Text = await LangSystem.GetLang("settings.titlename");
            ResetTimerBtn.Content = await LangSystem.GetLang("timer.reset");
            PauseTimerBtn.Content = await LangSystem.GetLang("timer.pause");
            StartTimerBtn.Content = await LangSystem.GetLang("timer.start");
            LapStopwatchBtn.Content = await LangSystem.GetLang("stopwatch.lap");
            ResetStopwatchBtn.Content = await LangSystem.GetLang("stopwatch.reset");
            PauseStopwatchBtn.Content = await LangSystem.GetLang("stopwatch.pause");
            StartStopwatchBtn.Content = await LangSystem.GetLang("stopwatch.start");
            AddAlarmBtn.Content = await LangSystem.GetLang("alarm.add");
            NoAlarmText.Text = await LangSystem.GetLang("alarm.noalarm");
        }

        // Create all timer of the app
        private async Task CreateTimers()
        {
            // Create a timer to update the clock every second
            time = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            time.Tick += Time_Tick;
            time.Start();

            // Create the stopwatch for... the stopwatch (lol)
            stopwatch = new Stopwatch();
            stopwatchTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(10)
            };
            stopwatchTimer.Tick += Stopwatch_Tick;

            // Create the timer for the... timer (lol... Ok I'll stop)
            timerTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            timerTimer.Tick += Timer_Tick;

            // Create the timer for the alarm
            alarm = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            alarm.Tick += Alarm_Tick;
            alarm.Start();

            // Create the timer for the carousel
            carousel = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(ConfigManager.Variables.CarouselDelay)
            };
            carousel.Tick += Carousel_Tick;
            carousel.Start();
        }

        private int TotalSplashStep = 10 + 1;
        private int CurrentSplashStep = 0;
        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // Splash start
            RootTitleBar.Visibility = Visibility.Hidden;
            MainGrid.Visibility = Visibility.Hidden;
            SplashLoadingText.Text = "Hi !";
            SplashPercentageText.Text = "0%";
            SplashProgressBar.Value = 0;
            SplashVersionText.Text = "Version " + App.AppVersion;
            CurrentSplashStep = 0;

            await Task.Delay(300);
            SplashGrid.Visibility = Visibility.Visible;

            {
                var fadeAnimation = new DoubleAnimation
                {
                    From = 0,
                    To = 1,
                    Duration = TimeSpan.FromSeconds(0.2),
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn }
                };
                var zoomAnimation1 = new DoubleAnimation
                {
                    From = 0.9,
                    To = 1,
                    Duration = TimeSpan.FromSeconds(0.5),
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                };
                var zoomAnimation2 = new DoubleAnimation
                {
                    From = 0.9,
                    To = 1,
                    Duration = TimeSpan.FromSeconds(0.5),
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                };
                Storyboard.SetTarget(fadeAnimation, SplashGrid);
                Storyboard.SetTargetProperty(fadeAnimation, new PropertyPath(UIElement.OpacityProperty));
                Storyboard.SetTarget(zoomAnimation1, SplashGrid);
                Storyboard.SetTarget(zoomAnimation2, SplashGrid);
                Storyboard.SetTargetProperty(zoomAnimation1, new PropertyPath("(UIElement.RenderTransform).(ScaleTransform.ScaleX)"));
                Storyboard.SetTargetProperty(zoomAnimation2, new PropertyPath("(UIElement.RenderTransform).(ScaleTransform.ScaleY)"));
                var storyboard = new Storyboard();
                storyboard.Children.Add(fadeAnimation);
                storyboard.Children.Add(zoomAnimation1);
                storyboard.Children.Add(zoomAnimation2);
                storyboard.Begin();
            }

            await Task.Delay(700);

            CurrentSplashStep++;
            SplashProgressBar.Value = (CurrentSplashStep * 100) / TotalSplashStep;
            SplashPercentageText.Text = ((CurrentSplashStep * 100) / TotalSplashStep) + "%";
            SplashLoadingText.Text = "Applying arguments...";
            // Specific startup options
            if (App.StartupOptions.FullScreen)
            {
                FullScreenBtn.IsChecked = true;
                WindowState = WindowState.Normal;
                WindowStyle = WindowStyle.None;
                WindowState = WindowState.Maximized;
                RootTitleBar.ShowMaximize = false;
                RootTitleBar.ShowMinimize = false;
            }
            if (App.StartupOptions.KioskMode)
            {
                KioskModeBtn.IsChecked = true;
                FullScreenBtn.IsChecked = true;
                FullScreenBtn.IsEnabled = false;
                WindowState = WindowState.Normal;
                WindowStyle = WindowStyle.None;
                WindowState = WindowState.Maximized;
                RootTitleBar.ShowMaximize = false;
                RootTitleBar.ShowMinimize = false;
                RootTitleBar.ShowClose = false;
                Process.Start("taskkill", "/f /im explorer.exe");
            }

            CurrentSplashStep++;
            SplashProgressBar.Value = (CurrentSplashStep * 100) / TotalSplashStep;
            SplashPercentageText.Text = ((CurrentSplashStep * 100) / TotalSplashStep) + "%";
            SplashLoadingText.Text = "Checking files...";
            // Check and create the config files
            await ConfigManager.CheckAndCreateConfigs();


            CurrentSplashStep++;
            SplashProgressBar.Value = (CurrentSplashStep * 100) / TotalSplashStep;
            SplashPercentageText.Text = ((CurrentSplashStep * 100) / TotalSplashStep) + "%";
            SplashLoadingText.Text = "Loading settings...";
            // Load settings
            await ConfigManager.LoadSettings();

            CurrentSplashStep++;
            SplashProgressBar.Value = (CurrentSplashStep * 100) / TotalSplashStep;
            SplashPercentageText.Text = ((CurrentSplashStep * 100) / TotalSplashStep) + "%";
            SplashLoadingText.Text = "Loading language...";
            // Init language
            await LangSystem.InitLang();

            CurrentSplashStep++;
            SplashProgressBar.Value = (CurrentSplashStep * 100) / TotalSplashStep;
            SplashPercentageText.Text = ((CurrentSplashStep * 100) / TotalSplashStep) + "%";
            SplashLoadingText.Text = "Loading UI (ClockMenu)...";
            // Add the grids to the list
            MenuClockGrids.Add(AlarmGrid);
            MenuClockGrids.Add(StopwatchGrid);
            MenuClockGrids.Add(TimerGrid);

            CurrentSplashStep++;
            SplashProgressBar.Value = (CurrentSplashStep * 100) / TotalSplashStep;
            SplashPercentageText.Text = ((CurrentSplashStep * 100) / TotalSplashStep) + "%";
            SplashLoadingText.Text = "Loading UI (Settings)...";
            // Load the settings tabs
            SettingsTabs.Add("General", new Pages.Settings.General());
            SettingsTabs.Add("PluginManager", new Pages.Settings.PluginManager());
            SettingsTabs.Add("About", new Pages.Settings.About());

            CurrentSplashStep++;
            SplashProgressBar.Value = (CurrentSplashStep * 100) / TotalSplashStep;
            SplashPercentageText.Text = ((CurrentSplashStep * 100) / TotalSplashStep) + "%";
            SplashLoadingText.Text = "Loading alarms...";
            // Load the alarms
            await AlarmCardRestore();

            CurrentSplashStep++;
            SplashProgressBar.Value = (CurrentSplashStep * 100) / TotalSplashStep;
            SplashPercentageText.Text = ((CurrentSplashStep * 100) / TotalSplashStep) + "%";
            SplashLoadingText.Text = "Loading plugins...";
            // Load plugins
            await PluginLoader.LoadPlugins();

            CurrentSplashStep++;
            SplashProgressBar.Value = (CurrentSplashStep * 100) / TotalSplashStep;
            SplashPercentageText.Text = ((CurrentSplashStep * 100) / TotalSplashStep) + "%";
            SplashLoadingText.Text = "Loading UI...";
            // Change UI depending on the settings
            await UIUpdate();

            CurrentSplashStep++;
            SplashProgressBar.Value = (CurrentSplashStep * 100) / TotalSplashStep;
            SplashPercentageText.Text = ((CurrentSplashStep * 100) / TotalSplashStep) + "%";
            SplashLoadingText.Text = "Creating timers...";
            // Create every timers of the app
            await CreateTimers();

            // Splash end
            CurrentSplashStep++;
            SplashProgressBar.Value = (CurrentSplashStep * 100) / TotalSplashStep;
            SplashPercentageText.Text = ((CurrentSplashStep * 100) / TotalSplashStep) + "%";
            SplashLoadingText.Text = "Ready !";

            await Task.Delay(700);

            {
                var fadeAnimation = new DoubleAnimation
                {
                    From = 1,
                    To = 0,
                    Duration = TimeSpan.FromSeconds(0.2),
                    BeginTime = TimeSpan.FromSeconds(0.1),
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                };
                var zoomAnimation1 = new DoubleAnimation
                {
                    From = 1,
                    To = 1.1,
                    Duration = TimeSpan.FromSeconds(0.3),
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn }
                };
                var zoomAnimation2 = new DoubleAnimation
                {
                    From = 1,
                    To = 1.1,
                    Duration = TimeSpan.FromSeconds(0.3),
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn }
                };
                Storyboard.SetTarget(fadeAnimation, SplashGrid);
                Storyboard.SetTargetProperty(fadeAnimation, new PropertyPath(UIElement.OpacityProperty));
                Storyboard.SetTarget(zoomAnimation1, SplashGrid);
                Storyboard.SetTarget(zoomAnimation2, SplashGrid);
                Storyboard.SetTargetProperty(zoomAnimation1, new PropertyPath("(UIElement.RenderTransform).(ScaleTransform.ScaleX)"));
                Storyboard.SetTargetProperty(zoomAnimation2, new PropertyPath("(UIElement.RenderTransform).(ScaleTransform.ScaleY)"));
                var storyboard = new Storyboard();
                storyboard.Children.Add(fadeAnimation);
                storyboard.Children.Add(zoomAnimation1);
                storyboard.Children.Add(zoomAnimation2);
                storyboard.Begin();
            }

            await Task.Delay(300);

            SplashGrid.Visibility = Visibility.Hidden;
            RootTitleBar.Visibility = Visibility.Visible;
            MainGrid.Visibility = Visibility.Visible;

            {
                var fadeAnimation = new DoubleAnimation
                {
                    From = 0,
                    To = 1,
                    Duration = TimeSpan.FromSeconds(0.2),
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn }
                };
                var zoomAnimation1 = new DoubleAnimation
                {
                    From = 0.9,
                    To = 1,
                    Duration = TimeSpan.FromSeconds(0.5),
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                };
                var zoomAnimation2 = new DoubleAnimation
                {
                    From = 0.9,
                    To = 1,
                    Duration = TimeSpan.FromSeconds(0.5),
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                };
                Storyboard.SetTarget(fadeAnimation, MainGrid);
                Storyboard.SetTargetProperty(fadeAnimation, new PropertyPath(UIElement.OpacityProperty));
                Storyboard.SetTarget(zoomAnimation1, MainGrid);
                Storyboard.SetTarget(zoomAnimation2, MainGrid);
                Storyboard.SetTargetProperty(zoomAnimation1, new PropertyPath("(UIElement.RenderTransform).(ScaleTransform.ScaleX)"));
                Storyboard.SetTargetProperty(zoomAnimation2, new PropertyPath("(UIElement.RenderTransform).(ScaleTransform.ScaleY)"));
                var storyboard = new Storyboard();
                storyboard.Children.Add(fadeAnimation);
                storyboard.Children.Add(zoomAnimation1);
                storyboard.Children.Add(zoomAnimation2);
                storyboard.Begin();
            }
            {
                var translateAnimation = new DoubleAnimation
                {
                    From = -100,
                    To = 0,
                    Duration = TimeSpan.FromSeconds(0.5),
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                };
                Storyboard.SetTarget(translateAnimation, RootTitleBar);
                Storyboard.SetTargetProperty(translateAnimation, new PropertyPath("(UIElement.RenderTransform).(TranslateTransform.Y)"));
                var storyboard = new Storyboard();
                storyboard.Children.Add(translateAnimation);
                storyboard.Begin();
            }
        }

        #region Screen Off/On
        private bool MouseMovedExecuted = false;
        private async void MouseMoved(object sender, MouseEventArgs e)
        {
            if (!MouseMovedExecuted && FullScreenBtn.IsChecked == true)
            {
                MouseMovedExecuted = true;
                this.Cursor = Cursors.Arrow;
                await Task.Delay(3000);
                this.Cursor = Cursors.None;
                MouseMovedExecuted = false;
            }
        }

        /*private const int GWL_WNDPROC = -4;
        private IntPtr oldWndProc;
        private delegate IntPtr WndProcDelegate(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);
        private IntPtr WndProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam)
        {
            Debug.WriteLine("WndProc: " + msg);
            if (msg == (int)user32dll.User32WM_SYSCOMMAND)
            {
                if (wParam.ToInt32() == (int)user32dll.User32SC_MONITORPOWER)
                {
                    if (lParam.ToInt32() == user32dll.User32MONITOR_OFF)
                    {
                        Console.WriteLine("L'écran est éteint.");
                    }
                    else if (lParam.ToInt32() == user32dll.User32MONITOR_ON)
                    {
                        Console.WriteLine("L'écran est allumé.");
                    }
                }
            }
            return user32dll.User32CallWindowProc(oldWndProc, hWnd, msg, wParam, lParam);
        }*/

        //private DispatcherTimer ScreenPolling;
        private bool TurnOnSignal = false;
        private async Task TurnScreenOff()
        {
            await ScreenOffAnimation("crt");
            await Task.Delay(200);
            user32dll.User32SendMessage(0xFFFF, (int)user32dll.User32WM_SYSCOMMAND, (int)user32dll.User32SC_MONITORPOWER, user32dll.User32MONITOR_OFF);
            TurnOnSignal = false;

            this.MouseMove += async (sender, e) =>
            {
                TurnOnSignal = true;
            };
            this.KeyDown += async (sender, e) =>
            {
                TurnOnSignal = true;
            };
            this.MouseDown += async (sender, e) =>
            {
                TurnOnSignal = true;
            };

            while (!TurnOnSignal)
            {
                await Task.Delay(100);
            }

            this.MouseDown -= async (sender, e) =>
            {
                TurnOnSignal = true;
            };
            this.KeyDown -= async (sender, e) =>
            {
                TurnOnSignal = true;
            };
            this.MouseMove -= async (sender, e) =>
            {
                TurnOnSignal = true;
            };

            await Task.Delay(1000);

            await ScreenOnAnimation("crt");

            //nint thisHandle = new WindowInteropHelper(this).Handle;
            //oldWndProc = user32dll.User32SetWindowLongPtr(thisHandle, GWL_WNDPROC, Marshal.GetFunctionPointerForDelegate(new WndProcDelegate(WndProc)));

            //await Task.Delay(200);
            //await ScreenOnAnimation("crt");
        }

        private async Task ScreenOffAnimation(string animtype)
        {
            switch (animtype)
            {
                case "crt":
                    CRTRectUp.Visibility = Visibility.Visible;
                    CRTRectDown.Visibility = Visibility.Visible;
                    {
                        var rectangleupsize = new DoubleAnimation
                        {
                            From = 0,
                            To = 1,
                            Duration = TimeSpan.FromSeconds(0.2),
                            EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn }
                        };
                        var rectangledownsize = new DoubleAnimation
                        {
                            From = 0,
                            To = 1,
                            Duration = TimeSpan.FromSeconds(0.2),
                            EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn }
                        };
                        var appgridsize = new DoubleAnimation
                        {
                            From = 1,
                            To = 0,
                            Duration = TimeSpan.FromSeconds(0.2),
                            EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn }
                        };
                        Storyboard.SetTarget(rectangleupsize, CRTRectUp);
                        Storyboard.SetTarget(rectangledownsize, CRTRectDown);
                        Storyboard.SetTarget(appgridsize, AppGrid);
                        Storyboard.SetTargetProperty(rectangleupsize, new PropertyPath("(UIElement.RenderTransform).(ScaleTransform.ScaleY)"));
                        Storyboard.SetTargetProperty(rectangledownsize, new PropertyPath("(UIElement.RenderTransform).(ScaleTransform.ScaleY)"));
                        Storyboard.SetTargetProperty(appgridsize, new PropertyPath("(UIElement.RenderTransform).(ScaleTransform.ScaleY)"));
                        var storyboard = new Storyboard();
                        storyboard.Children.Add(rectangleupsize);
                        storyboard.Children.Add(rectangledownsize);
                        storyboard.Children.Add(appgridsize);
                        storyboard.Begin();
                    }
                    await Task.Delay(150);
                    CRTRectMiddle.Visibility = Visibility.Visible;
                    await Task.Delay(50);
                    {
                        var rectanglemiddlesize = new DoubleAnimation
                        {
                            From = 1,
                            To = 0,
                            Duration = TimeSpan.FromSeconds(0.2),
                            EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn }
                        };
                        Storyboard.SetTarget(rectanglemiddlesize, CRTRectMiddle);
                        Storyboard.SetTargetProperty(rectanglemiddlesize, new PropertyPath("(UIElement.RenderTransform).(ScaleTransform.ScaleX)"));
                        var storyboard = new Storyboard();
                        storyboard.Children.Add(rectanglemiddlesize);
                        storyboard.Begin();
                    }
                    await Task.Delay(200);
                    break;

            }
        }

        private async Task ScreenOnAnimation(string animtype)
        {
            switch (animtype)
            {
                case "crt":
                    {
                        var rectanglemiddlesize = new DoubleAnimation
                        {
                            From = 0,
                            To = 1,
                            Duration = TimeSpan.FromSeconds(0.2),
                            EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                        };
                        Storyboard.SetTarget(rectanglemiddlesize, CRTRectMiddle);
                        Storyboard.SetTargetProperty(rectanglemiddlesize, new PropertyPath("(UIElement.RenderTransform).(ScaleTransform.ScaleX)"));
                        var storyboard = new Storyboard();
                        storyboard.Children.Add(rectanglemiddlesize);
                        storyboard.Begin();
                    }
                    await Task.Delay(200);
                    CRTRectMiddle.Visibility = Visibility.Hidden;
                    {
                        var rectangleupsize = new DoubleAnimation
                        {
                            From = 1,
                            To = 0,
                            Duration = TimeSpan.FromSeconds(0.2),
                            EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                        };
                        var rectangledownsize = new DoubleAnimation
                        {
                            From = 1,
                            To = 0,
                            Duration = TimeSpan.FromSeconds(0.2),
                            EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                        };
                        var appgridsize = new DoubleAnimation
                        {
                            From = 0,
                            To = 1,
                            Duration = TimeSpan.FromSeconds(0.2),
                            EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                        };
                        Storyboard.SetTarget(rectangleupsize, CRTRectUp);
                        Storyboard.SetTarget(rectangledownsize, CRTRectDown);
                        Storyboard.SetTarget(appgridsize, AppGrid);
                        Storyboard.SetTargetProperty(rectangleupsize, new PropertyPath("(UIElement.RenderTransform).(ScaleTransform.ScaleY)"));
                        Storyboard.SetTargetProperty(rectangledownsize, new PropertyPath("(UIElement.RenderTransform).(ScaleTransform.ScaleY)"));
                        Storyboard.SetTargetProperty(appgridsize, new PropertyPath("(UIElement.RenderTransform).(ScaleTransform.ScaleY)"));
                        var storyboard = new Storyboard();
                        storyboard.Children.Add(rectangleupsize);
                        storyboard.Children.Add(rectangledownsize);
                        storyboard.Children.Add(appgridsize);
                        storyboard.Begin();
                    }
                    CRTRectUp.Visibility = Visibility.Hidden;
                    CRTRectDown.Visibility = Visibility.Hidden;
                    await Task.Delay(200);
                    break;
            }
        }
        #endregion

        #region Clock Menu
        // Animation variables
        // - Fade speed
        private double fadespeed = 0.30;
        // - Zoom speed
        private double zoomspeed = 0.35;

        // Current ClockMenu page index
        private int MenuClockIndex = 0;

        // Update the MiniClock
        private async void Time_Tick(object sender, EventArgs e)
        {
            DateTime now = DateTime.Now;
            await UpdateMiniClockHour(now.Hour.ToString("0"));  // Update the MiniClock Hour
            await UpdateMiniClockMinute(now.Minute.ToString("00"));  // Update the MiniClock Minute
        }

        // MiniClock Update
        // - Slide speed
        private double txtslidespeed = 0.15;
        // - Delay between slides
        private int txtdelay = 150;
        // - Actual MiniClock Hour
        private string ActualMCHour = "17";
        // - Actual MiniClock Minute
        private string ActualMCMinute = "20";
        // - Update MiniClock Hour
        private async Task UpdateMiniClockHour(string text)
        {
            if (ActualMCHour != text)
            {
                MiniClockHour.Text = ActualMCHour;
                ActualMCHour = text;
                {
                    var translateAnimation = new DoubleAnimation
                    {
                        From = 0,
                        To = -180,
                        Duration = TimeSpan.FromSeconds(txtslidespeed),
                        EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn }
                    };
                    Storyboard.SetTarget(translateAnimation, MiniClockHour);
                    Storyboard.SetTargetProperty(translateAnimation, new PropertyPath("(UIElement.RenderTransform).(TranslateTransform.Y)"));
                    var storyboard = new Storyboard();
                    storyboard.Children.Add(translateAnimation);
                    storyboard.Begin();
                }
                await Task.Delay(txtdelay);
                MiniClockHour.Text = text;
                {
                    var translateAnimation = new DoubleAnimation
                    {
                        From = 180,
                        To = 0,
                        Duration = TimeSpan.FromSeconds(txtslidespeed),
                        EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                    };
                    Storyboard.SetTarget(translateAnimation, MiniClockHour);
                    Storyboard.SetTargetProperty(translateAnimation, new PropertyPath("(UIElement.RenderTransform).(TranslateTransform.Y)"));
                    var storyboard = new Storyboard();
                    storyboard.Children.Add(translateAnimation);
                    storyboard.Begin();
                }
            }
        }
        // - Update MiniClock Minute
        private async Task UpdateMiniClockMinute(string text)
        {
            if (ActualMCMinute != text)
            {
                MiniClockMinute.Text = ActualMCMinute;
                ActualMCMinute = text;
                {
                    var translateAnimation = new DoubleAnimation
                    {
                        From = 0,
                        To = -180,
                        Duration = TimeSpan.FromSeconds(txtslidespeed),
                        EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn }
                    };
                    Storyboard.SetTarget(translateAnimation, MiniClockMinute);
                    Storyboard.SetTargetProperty(translateAnimation, new PropertyPath("(UIElement.RenderTransform).(TranslateTransform.Y)"));
                    var storyboard = new Storyboard();
                    storyboard.Children.Add(translateAnimation);
                    storyboard.Begin();
                }
                await Task.Delay(txtdelay);
                MiniClockMinute.Text = text;
                {
                    var translateAnimation = new DoubleAnimation
                    {
                        From = 180,
                        To = 0,
                        Duration = TimeSpan.FromSeconds(txtslidespeed),
                        EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                    };
                    Storyboard.SetTarget(translateAnimation, MiniClockMinute);
                    Storyboard.SetTargetProperty(translateAnimation, new PropertyPath("(UIElement.RenderTransform).(TranslateTransform.Y)"));
                    var storyboard = new Storyboard();
                    storyboard.Children.Add(translateAnimation);
                    storyboard.Begin();
                }
            }
        }

        // Navigation functions
        // - Open Clock Menu
        private async void EnterDownMenuClockBtn_Click(object sender, RoutedEventArgs e)
        {
            {
                var fadeAnimation = new DoubleAnimation
                {
                    From = 1,
                    To = 0,
                    Duration = TimeSpan.FromSeconds(fadespeed),
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                };
                var zoomAnimation1 = new DoubleAnimation
                {
                    From = 1,
                    To = 0,
                    Duration = TimeSpan.FromSeconds(zoomspeed),
                    EasingFunction = new QuarticEase { EasingMode = EasingMode.EaseIn }
                };
                var zoomAnimation2 = new DoubleAnimation
                {
                    From = 1,
                    To = 0,
                    Duration = TimeSpan.FromSeconds(zoomspeed),
                    EasingFunction = new QuarticEase { EasingMode = EasingMode.EaseIn }
                };
                Storyboard.SetTarget(fadeAnimation, ClockGrid);
                Storyboard.SetTargetProperty(fadeAnimation, new PropertyPath(UIElement.OpacityProperty));
                Storyboard.SetTarget(zoomAnimation1, ClockGrid);
                Storyboard.SetTarget(zoomAnimation2, ClockGrid);
                Storyboard.SetTargetProperty(zoomAnimation1, new PropertyPath("(UIElement.RenderTransform).(ScaleTransform.ScaleX)"));
                Storyboard.SetTargetProperty(zoomAnimation2, new PropertyPath("(UIElement.RenderTransform).(ScaleTransform.ScaleY)"));
                var storyboard = new Storyboard();
                storyboard.Children.Add(fadeAnimation);
                storyboard.Children.Add(zoomAnimation1);
                storyboard.Children.Add(zoomAnimation2);
                storyboard.Begin();
            }

            await Task.Delay(400);

            ClockGrid.Visibility = Visibility.Hidden;
            MenuClockGrid.Visibility = Visibility.Visible;

            {
                var fadeAnimation = new DoubleAnimation
                {
                    From = 0,
                    To = 1,
                    Duration = TimeSpan.FromSeconds(fadespeed),
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn }
                };
                var translateAnimation = new DoubleAnimation
                {
                    From = 100,
                    To = 0,
                    Duration = TimeSpan.FromSeconds(zoomspeed),
                    EasingFunction = new QuarticEase { EasingMode = EasingMode.EaseOut }
                };
                Storyboard.SetTarget(fadeAnimation, MenuClockGrid);
                Storyboard.SetTargetProperty(fadeAnimation, new PropertyPath(UIElement.OpacityProperty));
                Storyboard.SetTarget(translateAnimation, MenuClockGrid);
                Storyboard.SetTargetProperty(translateAnimation, new PropertyPath("(UIElement.RenderTransform).(TranslateTransform.Y)"));
                var storyboard = new Storyboard();
                storyboard.Children.Add(fadeAnimation);
                storyboard.Children.Add(translateAnimation);
                storyboard.Begin();
            }

            MiniClockBorder.Visibility = Visibility.Visible;
            ExitDownMenuClockBtn.Visibility = Visibility.Visible;

            {
                var fadeAnimation = new DoubleAnimation
                {
                    From = 0,
                    To = 1,
                    Duration = TimeSpan.FromSeconds(fadespeed),
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn }
                };
                var fadeAnimation2 = new DoubleAnimation
                {
                    From = 0,
                    To = 1,
                    Duration = TimeSpan.FromSeconds(fadespeed),
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn }
                };
                var zoomAnimation1 = new DoubleAnimation
                {
                    From = 0,
                    To = 1,
                    Duration = TimeSpan.FromSeconds(zoomspeed),
                    EasingFunction = new QuarticEase { EasingMode = EasingMode.EaseOut }
                };
                var zoomAnimation2 = new DoubleAnimation
                {
                    From = 0,
                    To = 1,
                    Duration = TimeSpan.FromSeconds(zoomspeed),
                    EasingFunction = new QuarticEase { EasingMode = EasingMode.EaseOut }
                };
                Storyboard.SetTarget(fadeAnimation, MiniClockBorder);
                Storyboard.SetTarget(fadeAnimation2, ExitDownMenuClockBtn);
                Storyboard.SetTargetProperty(fadeAnimation, new PropertyPath(UIElement.OpacityProperty));
                Storyboard.SetTargetProperty(fadeAnimation2, new PropertyPath(UIElement.OpacityProperty));
                Storyboard.SetTarget(zoomAnimation1, MiniClockBorder);
                Storyboard.SetTarget(zoomAnimation2, MiniClockBorder);
                Storyboard.SetTargetProperty(zoomAnimation1, new PropertyPath("(UIElement.RenderTransform).(ScaleTransform.ScaleX)"));
                Storyboard.SetTargetProperty(zoomAnimation2, new PropertyPath("(UIElement.RenderTransform).(ScaleTransform.ScaleY)"));
                var storyboard = new Storyboard();
                storyboard.Children.Add(fadeAnimation);
                storyboard.Children.Add(fadeAnimation2);
                storyboard.Children.Add(zoomAnimation1);
                storyboard.Children.Add(zoomAnimation2);
                storyboard.Begin();
            }
        }
        // - Close Clock Menu
        private async void ExitDownMenuClockBtn_Click(object sender, RoutedEventArgs e)
        {
            {
                var fadeAnimation = new DoubleAnimation
                {
                    From = 1,
                    To = 0,
                    Duration = TimeSpan.FromSeconds(fadespeed),
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                };
                var translateAnimation = new DoubleAnimation
                {
                    From = 0,
                    To = 100,
                    Duration = TimeSpan.FromSeconds(zoomspeed),
                    EasingFunction = new QuarticEase { EasingMode = EasingMode.EaseIn }
                };
                Storyboard.SetTarget(fadeAnimation, MenuClockGrid);
                Storyboard.SetTargetProperty(fadeAnimation, new PropertyPath(UIElement.OpacityProperty));
                Storyboard.SetTarget(translateAnimation, MenuClockGrid);
                Storyboard.SetTargetProperty(translateAnimation, new PropertyPath("(UIElement.RenderTransform).(TranslateTransform.Y)"));
                var storyboard = new Storyboard();
                storyboard.Children.Add(fadeAnimation);
                storyboard.Children.Add(translateAnimation);
                storyboard.Begin();
            }

            {
                var fadeAnimation = new DoubleAnimation
                {
                    From = 1,
                    To = 0,
                    Duration = TimeSpan.FromSeconds(fadespeed),
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                };
                var fadeAnimation2 = new DoubleAnimation
                {
                    From = 1,
                    To = 0,
                    Duration = TimeSpan.FromSeconds(fadespeed),
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                };
                var zoomAnimation1 = new DoubleAnimation
                {
                    From = 1,
                    To = 0,
                    Duration = TimeSpan.FromSeconds(zoomspeed),
                    EasingFunction = new QuarticEase { EasingMode = EasingMode.EaseIn }
                };
                var zoomAnimation2 = new DoubleAnimation
                {
                    From = 1,
                    To = 0,
                    Duration = TimeSpan.FromSeconds(zoomspeed),
                    EasingFunction = new QuarticEase { EasingMode = EasingMode.EaseIn }
                };
                Storyboard.SetTarget(fadeAnimation, MiniClockBorder);
                Storyboard.SetTarget(fadeAnimation2, ExitDownMenuClockBtn);
                Storyboard.SetTargetProperty(fadeAnimation, new PropertyPath(UIElement.OpacityProperty));
                Storyboard.SetTargetProperty(fadeAnimation2, new PropertyPath(UIElement.OpacityProperty));
                Storyboard.SetTarget(zoomAnimation1, MiniClockBorder);
                Storyboard.SetTarget(zoomAnimation2, MiniClockBorder);
                Storyboard.SetTargetProperty(zoomAnimation1, new PropertyPath("(UIElement.RenderTransform).(ScaleTransform.ScaleX)"));
                Storyboard.SetTargetProperty(zoomAnimation2, new PropertyPath("(UIElement.RenderTransform).(ScaleTransform.ScaleY)"));
                var storyboard = new Storyboard();
                storyboard.Children.Add(fadeAnimation);
                storyboard.Children.Add(fadeAnimation2);
                storyboard.Children.Add(zoomAnimation1);
                storyboard.Children.Add(zoomAnimation2);
                storyboard.Begin();
            }

            await Task.Delay(400);

            MenuClockGrid.Visibility = Visibility.Hidden;
            MiniClockBorder.Visibility = Visibility.Hidden;
            ExitDownMenuClockBtn.Visibility = Visibility.Hidden;
            ClockGrid.Visibility = Visibility.Visible;

            {
                var fadeAnimation = new DoubleAnimation
                {
                    From = 0,
                    To = 1,
                    Duration = TimeSpan.FromSeconds(fadespeed),
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn }
                };
                var zoomAnimation1 = new DoubleAnimation
                {
                    From = 0,
                    To = 1,
                    Duration = TimeSpan.FromSeconds(zoomspeed),
                    EasingFunction = new QuarticEase { EasingMode = EasingMode.EaseOut }
                };
                var zoomAnimation2 = new DoubleAnimation
                {
                    From = 0,
                    To = 1,
                    Duration = TimeSpan.FromSeconds(zoomspeed),
                    EasingFunction = new QuarticEase { EasingMode = EasingMode.EaseOut }
                };
                Storyboard.SetTarget(fadeAnimation, ClockGrid);
                Storyboard.SetTargetProperty(fadeAnimation, new PropertyPath(UIElement.OpacityProperty));
                Storyboard.SetTarget(zoomAnimation1, ClockGrid);
                Storyboard.SetTarget(zoomAnimation2, ClockGrid);
                Storyboard.SetTargetProperty(zoomAnimation1, new PropertyPath("(UIElement.RenderTransform).(ScaleTransform.ScaleX)"));
                Storyboard.SetTargetProperty(zoomAnimation2, new PropertyPath("(UIElement.RenderTransform).(ScaleTransform.ScaleY)"));
                var storyboard = new Storyboard();
                storyboard.Children.Add(fadeAnimation);
                storyboard.Children.Add(zoomAnimation1);
                storyboard.Children.Add(zoomAnimation2);
                storyboard.Begin();
            }
        }
        // - Go to the left page (in Clock Menu)
        private async void LeftMenuClockBtn_Click(object sender, RoutedEventArgs e)
        {
            int ActualIndex = MenuClockIndex;
            MenuClockIndex--;
            RightMenuClockBtn.IsEnabled = true;
            if (MenuClockIndex == 0)
            {
                LeftMenuClockBtn.IsEnabled = false;
            }

            Grid PrevGrid = MenuClockGrids[ActualIndex];
            {
                var fadeAnimation = new DoubleAnimation
                {
                    From = 1,
                    To = 0,
                    Duration = TimeSpan.FromSeconds(fadespeed),
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                };
                var translateAnimation = new DoubleAnimation
                {
                    From = 0,
                    To = 100,
                    Duration = TimeSpan.FromSeconds(zoomspeed),
                    EasingFunction = new QuarticEase { EasingMode = EasingMode.EaseIn }
                };
                Storyboard.SetTarget(fadeAnimation, PrevGrid);
                Storyboard.SetTargetProperty(fadeAnimation, new PropertyPath(UIElement.OpacityProperty));
                Storyboard.SetTarget(translateAnimation, PrevGrid);
                Storyboard.SetTargetProperty(translateAnimation, new PropertyPath("(UIElement.RenderTransform).(TranslateTransform.X)"));
                var storyboard = new Storyboard();
                storyboard.Children.Add(fadeAnimation);
                storyboard.Children.Add(translateAnimation);
                storyboard.Begin();
            }

            await Task.Delay(300);
            PrevGrid.Visibility = Visibility.Hidden;

            Grid NextGrid = MenuClockGrids[MenuClockIndex];
            NextGrid.Visibility = Visibility.Visible;
            {
                var fadeAnimation = new DoubleAnimation
                {
                    From = 0,
                    To = 1,
                    Duration = TimeSpan.FromSeconds(fadespeed),
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn }
                };
                var translateAnimation = new DoubleAnimation
                {
                    From = -100,
                    To = 0,
                    Duration = TimeSpan.FromSeconds(zoomspeed),
                    EasingFunction = new QuarticEase { EasingMode = EasingMode.EaseOut }
                };
                Storyboard.SetTarget(fadeAnimation, NextGrid);
                Storyboard.SetTargetProperty(fadeAnimation, new PropertyPath(UIElement.OpacityProperty));
                Storyboard.SetTarget(translateAnimation, NextGrid);
                Storyboard.SetTargetProperty(translateAnimation, new PropertyPath("(UIElement.RenderTransform).(TranslateTransform.X)"));
                var storyboard = new Storyboard();
                storyboard.Children.Add(fadeAnimation);
                storyboard.Children.Add(translateAnimation);
                storyboard.Begin();
            }
        }
        // - Go to the right page (in Clock Menu)
        private async void RightMenuClockBtn_Click(object sender, RoutedEventArgs e)
        {
            int ActualIndex = MenuClockIndex;
            MenuClockIndex++;
            LeftMenuClockBtn.IsEnabled = true;
            if (MenuClockIndex == MenuClockGrids.Count - 1)
            {
                RightMenuClockBtn.IsEnabled = false;
            }

            Grid PrevGrid = MenuClockGrids[ActualIndex];
            {
                var fadeAnimation = new DoubleAnimation
                {
                    From = 1,
                    To = 0,
                    Duration = TimeSpan.FromSeconds(fadespeed),
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                };
                var translateAnimation = new DoubleAnimation
                {
                    From = 0,
                    To = -100,
                    Duration = TimeSpan.FromSeconds(zoomspeed),
                    EasingFunction = new QuarticEase { EasingMode = EasingMode.EaseIn }
                };
                Storyboard.SetTarget(fadeAnimation, PrevGrid);
                Storyboard.SetTargetProperty(fadeAnimation, new PropertyPath(UIElement.OpacityProperty));
                Storyboard.SetTarget(translateAnimation, PrevGrid);
                Storyboard.SetTargetProperty(translateAnimation, new PropertyPath("(UIElement.RenderTransform).(TranslateTransform.X)"));
                var storyboard = new Storyboard();
                storyboard.Children.Add(fadeAnimation);
                storyboard.Children.Add(translateAnimation);
                storyboard.Begin();
            }

            await Task.Delay(300);
            PrevGrid.Visibility = Visibility.Hidden;

            Grid NextGrid = MenuClockGrids[MenuClockIndex];
            NextGrid.Visibility = Visibility.Visible;
            {
                var fadeAnimation = new DoubleAnimation
                {
                    From = 0,
                    To = 1,
                    Duration = TimeSpan.FromSeconds(fadespeed),
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn }
                };
                var translateAnimation = new DoubleAnimation
                {
                    From = 100,
                    To = 0,
                    Duration = TimeSpan.FromSeconds(zoomspeed),
                    EasingFunction = new QuarticEase { EasingMode = EasingMode.EaseOut }
                };
                Storyboard.SetTarget(fadeAnimation, NextGrid);
                Storyboard.SetTargetProperty(fadeAnimation, new PropertyPath(UIElement.OpacityProperty));
                Storyboard.SetTarget(translateAnimation, NextGrid);
                Storyboard.SetTargetProperty(translateAnimation, new PropertyPath("(UIElement.RenderTransform).(TranslateTransform.X)"));
                var storyboard = new Storyboard();
                storyboard.Children.Add(fadeAnimation);
                storyboard.Children.Add(translateAnimation);
                storyboard.Begin();
            }
        }
        #endregion

        #region Stopwatch

        /*
         * Time Digit Legend:
         * H1 = [0]0:00:00.00
         * H2 = 0[0]:00:00.00
         * M1 = 00:[0]0:00.00
         * M2 = 00:0[0]:00.00
         * S1 = 00:00:[0]0.00
         * S2 = 00:00:0[0].00
         * MS1 = 00:00:00.[0]0
         * MS2 = 00:00:00.0[0]
         */

        // Stopwatch state
        // - Is the stopwatch running?
        private bool StopwatchIsRunning = false;
        // - Is the lap list deployed? (for the animation)
        private bool StopwatchLapDeployed = false;
        // - Is the progress bar flipped (reversed)? (for the animation)
        private bool StopwatchProgressFlipped = false;

        // Animation variables
        // - Fade speed
        private double swfadespeed = 0.15;
        // - Zoom speed
        private double swzoomspeed = 0.30;
        // - Slide speed
        private double swtxtslidespeed = 0.15;
        // - Delay between slides
        private int swtxtdelay = 150;

        // Current lap number
        private int LapIndex = 0;

        // Current char of the stopwatch
        private string ActualStopwatchH1 = "0";
        private string ActualStopwatchH2 = "0";
        private string ActualStopwatchM1 = "0";
        private string ActualStopwatchM2 = "0";
        private string ActualStopwatchS1 = "0";
        private string ActualStopwatchS2 = "0";
        private string ActualStopwatchMS1 = "0";
        private string ActualStopwatchMS2 = "0";

        // Last Lap Time for interval calculation
        private TimeSpan LastLapTime;

        // Tick event
        private async void Stopwatch_Tick(object sender, EventArgs e)
        {
            UpdateStopwatchTextH1(stopwatch.Elapsed.Hours.ToString("00")[0].ToString());
            UpdateStopwatchTextH2(stopwatch.Elapsed.Hours.ToString("00")[1].ToString());
            UpdateStopwatchTextM1(stopwatch.Elapsed.Minutes.ToString("00")[0].ToString());
            UpdateStopwatchTextM2(stopwatch.Elapsed.Minutes.ToString("00")[1].ToString());
            UpdateStopwatchTextS1(stopwatch.Elapsed.Seconds.ToString("00")[0].ToString());
            UpdateStopwatchTextS2(stopwatch.Elapsed.Seconds.ToString("00")[1].ToString());
            UpdateStopwatchTextMS1(stopwatch.Elapsed.Milliseconds.ToString("000")[0].ToString(), false);
            UpdateStopwatchTextMS2(stopwatch.Elapsed.Milliseconds.ToString("000")[1].ToString(), false);

            if (stopwatch.Elapsed.Seconds % 2 == 0 && StopwatchProgressFlipped)
            {
                StopwatchProgressFlipped = false;
                StopwatchProgressBar.RenderTransform = new ScaleTransform(1, 1);

            }
            else if (stopwatch.Elapsed.Seconds % 2 != 0 && !StopwatchProgressFlipped)
            {
                StopwatchProgressFlipped = true;
                StopwatchProgressBar.RenderTransform = new ScaleTransform(-1, 1);
            }

            if (StopwatchProgressFlipped)
            {
                StopwatchProgressBar.Value = 1000 - stopwatch.Elapsed.Milliseconds;
            }
            else
            {
                StopwatchProgressBar.Value = stopwatch.Elapsed.Milliseconds;
            }
        }

        // Stopwatch functions
        // - Start
        private async void StartStopwatchBtn_Click(object sender, RoutedEventArgs e)
        {
            stopwatch.Start();
            stopwatchTimer.Start();

            if (!StopwatchIsRunning)
            {
                StartStopwatchBtn.Visibility = Visibility.Hidden;
                PauseStopwatchBtn.Visibility = Visibility.Visible;
                ResetStopwatchBtn.Visibility = Visibility.Hidden;
                LapStopwatchBtn.Visibility = Visibility.Visible;

                {
                    var changeAnimation1 = new DoubleAnimation
                    {
                        From = 200,
                        To = 100,
                        Duration = TimeSpan.FromSeconds(swzoomspeed),
                        EasingFunction = new QuarticEase { EasingMode = EasingMode.EaseOut }
                    };
                    var changeAnimation2 = new DoubleAnimation
                    {
                        From = 200,
                        To = 100,
                        Duration = TimeSpan.FromSeconds(swzoomspeed),
                        EasingFunction = new QuarticEase { EasingMode = EasingMode.EaseOut }
                    };
                    Storyboard.SetTarget(changeAnimation1, StartStopwatchBtn);
                    Storyboard.SetTargetProperty(changeAnimation1, new PropertyPath(WidthProperty));
                    Storyboard.SetTarget(changeAnimation2, PauseStopwatchBtn);
                    Storyboard.SetTargetProperty(changeAnimation2, new PropertyPath(WidthProperty));
                    var storyboard = new Storyboard();
                    storyboard.Children.Add(changeAnimation1);
                    storyboard.Children.Add(changeAnimation2);
                    storyboard.Begin();
                }

                {
                    var changeAnimation1 = new DoubleAnimation
                    {
                        From = 0,
                        To = 100,
                        Duration = TimeSpan.FromSeconds(swzoomspeed),
                        EasingFunction = new QuarticEase { EasingMode = EasingMode.EaseOut }
                    };
                    var changeAnimation2 = new DoubleAnimation
                    {
                        From = 0,
                        To = 100,
                        Duration = TimeSpan.FromSeconds(swzoomspeed),
                        EasingFunction = new QuarticEase { EasingMode = EasingMode.EaseOut }
                    };
                    Storyboard.SetTarget(changeAnimation1, ResetStopwatchBtn);
                    Storyboard.SetTargetProperty(changeAnimation1, new PropertyPath(WidthProperty));
                    Storyboard.SetTarget(changeAnimation2, LapStopwatchBtn);
                    Storyboard.SetTargetProperty(changeAnimation2, new PropertyPath(WidthProperty));
                    var storyboard = new Storyboard();
                    storyboard.Children.Add(changeAnimation1);
                    storyboard.Children.Add(changeAnimation2);
                    storyboard.Begin();
                }

                {
                    var changeAnimation1 = new DoubleAnimation
                    {
                        From = 0,
                        To = 20,
                        Duration = TimeSpan.FromSeconds(swzoomspeed),
                        EasingFunction = new QuarticEase { EasingMode = EasingMode.EaseOut }
                    };
                    Storyboard.SetTarget(changeAnimation1, SWButtonSep);
                    Storyboard.SetTargetProperty(changeAnimation1, new PropertyPath(WidthProperty));
                    var storyboard = new Storyboard();
                    storyboard.Children.Add(changeAnimation1);
                    storyboard.Begin();
                }
            }
            else
            {
                {
                    var fadeAnimation1 = new DoubleAnimation
                    {
                        From = 1,
                        To = 0,
                        Duration = TimeSpan.FromSeconds(swfadespeed),
                        EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseInOut }
                    };
                    var fadeAnimation2 = new DoubleAnimation
                    {
                        From = 1,
                        To = 0,
                        Duration = TimeSpan.FromSeconds(swfadespeed),
                        EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseInOut }
                    };
                    Storyboard.SetTarget(fadeAnimation1, StartStopwatchBtn);
                    Storyboard.SetTargetProperty(fadeAnimation1, new PropertyPath(UIElement.OpacityProperty));
                    Storyboard.SetTarget(fadeAnimation2, ResetStopwatchBtn);
                    Storyboard.SetTargetProperty(fadeAnimation2, new PropertyPath(UIElement.OpacityProperty));
                    var storyboard = new Storyboard();
                    storyboard.Children.Add(fadeAnimation1);
                    storyboard.Children.Add(fadeAnimation2);
                    storyboard.Begin();
                }

                await Task.Delay(150);

                StartStopwatchBtn.Visibility = Visibility.Hidden;
                PauseStopwatchBtn.Visibility = Visibility.Visible;
                ResetStopwatchBtn.Visibility = Visibility.Hidden;
                LapStopwatchBtn.Visibility = Visibility.Visible;

                {
                    var fadeAnimation1 = new DoubleAnimation
                    {
                        From = 0,
                        To = 1,
                        Duration = TimeSpan.FromSeconds(swfadespeed),
                        EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseInOut }
                    };
                    var fadeAnimation2 = new DoubleAnimation
                    {
                        From = 0,
                        To = 1,
                        Duration = TimeSpan.FromSeconds(swfadespeed),
                        EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseInOut }
                    };
                    Storyboard.SetTarget(fadeAnimation1, PauseStopwatchBtn);
                    Storyboard.SetTargetProperty(fadeAnimation1, new PropertyPath(UIElement.OpacityProperty));
                    Storyboard.SetTarget(fadeAnimation2, LapStopwatchBtn);
                    Storyboard.SetTargetProperty(fadeAnimation2, new PropertyPath(UIElement.OpacityProperty));
                    var storyboard = new Storyboard();
                    storyboard.Children.Add(fadeAnimation1);
                    storyboard.Children.Add(fadeAnimation2);
                    storyboard.Begin();
                }
            }

            StopwatchIsRunning = true;
        }
        // - Pause
        private async void PauseStopwatchBtn_Click(object sender, RoutedEventArgs e)
        {
            stopwatch.Stop();
            stopwatchTimer.Stop();

            {
                var fadeAnimation1 = new DoubleAnimation
                {
                    From = 1,
                    To = 0,
                    Duration = TimeSpan.FromSeconds(swfadespeed),
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseInOut }
                };
                var fadeAnimation2 = new DoubleAnimation
                {
                    From = 1,
                    To = 0,
                    Duration = TimeSpan.FromSeconds(swfadespeed),
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseInOut }
                };
                Storyboard.SetTarget(fadeAnimation1, PauseStopwatchBtn);
                Storyboard.SetTargetProperty(fadeAnimation1, new PropertyPath(UIElement.OpacityProperty));
                Storyboard.SetTarget(fadeAnimation2, LapStopwatchBtn);
                Storyboard.SetTargetProperty(fadeAnimation2, new PropertyPath(UIElement.OpacityProperty));
                var storyboard = new Storyboard();
                storyboard.Children.Add(fadeAnimation1);
                storyboard.Children.Add(fadeAnimation2);
                storyboard.Begin();
            }

            await Task.Delay(150);

            StartStopwatchBtn.Visibility = Visibility.Visible;
            PauseStopwatchBtn.Visibility = Visibility.Hidden;
            ResetStopwatchBtn.Visibility = Visibility.Visible;
            LapStopwatchBtn.Visibility = Visibility.Hidden;

            {
                var fadeAnimation1 = new DoubleAnimation
                {
                    From = 0,
                    To = 1,
                    Duration = TimeSpan.FromSeconds(swfadespeed),
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseInOut }
                };
                var fadeAnimation2 = new DoubleAnimation
                {
                    From = 0,
                    To = 1,
                    Duration = TimeSpan.FromSeconds(swfadespeed),
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseInOut }
                };
                Storyboard.SetTarget(fadeAnimation1, StartStopwatchBtn);
                Storyboard.SetTargetProperty(fadeAnimation1, new PropertyPath(UIElement.OpacityProperty));
                Storyboard.SetTarget(fadeAnimation2, ResetStopwatchBtn);
                Storyboard.SetTargetProperty(fadeAnimation2, new PropertyPath(UIElement.OpacityProperty));
                var storyboard = new Storyboard();
                storyboard.Children.Add(fadeAnimation1);
                storyboard.Children.Add(fadeAnimation2);
                storyboard.Begin();
            }
        }
        // - Reset
        private async void ResetStopwatchBtn_Click(object sender, RoutedEventArgs e)
        {
            stopwatch.Reset();
            stopwatchTimer.Stop();

            foreach (Card lap in LapStopwatchStack.Children)
            {
                var fadeAnimation = new DoubleAnimation
                {
                    From = 1,
                    To = 0,
                    Duration = TimeSpan.FromSeconds(0.3),
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                };
                Storyboard.SetTarget(fadeAnimation, lap);
                Storyboard.SetTargetProperty(fadeAnimation, new PropertyPath(UIElement.OpacityProperty));
                var storyboard = new Storyboard();
                storyboard.Children.Add(fadeAnimation);
                storyboard.Begin();
            }

            await Task.Delay(300);

            LapStopwatchStack.Children.Clear();
            LapIndex = 0;

            if (StopwatchLapDeployed)
            {
                StopwatchLapDeployed = false;
                StopwatchLapRow.Height = new GridLength(1, GridUnitType.Auto);

                double oldRowLapHeight = StopwatchSubGrid.ActualHeight;
                oldRowLapHeight *= 0.6;
                {
                    var sizeAnimation = new DoubleAnimation
                    {
                        From = oldRowLapHeight,
                        To = 0,
                        Duration = TimeSpan.FromSeconds(0.3),
                        EasingFunction = new QuarticEase { EasingMode = EasingMode.EaseOut }
                    };
                    Storyboard.SetTarget(sizeAnimation, StopwatchLapScrollView);
                    Storyboard.SetTargetProperty(sizeAnimation, new PropertyPath(HeightProperty));
                    var storyboard = new Storyboard();
                    storyboard.Children.Add(sizeAnimation);
                    storyboard.Begin();
                }

                await Task.Delay(300);

                StopwatchLapScrollView.Height = double.NaN;
            }

            UpdateStopwatchTextH1("0");
            UpdateStopwatchTextH2("0");
            UpdateStopwatchTextM1("0");
            UpdateStopwatchTextM2("0");
            UpdateStopwatchTextS1("0");
            UpdateStopwatchTextS2("0");
            UpdateStopwatchTextMS1("0", true);
            UpdateStopwatchTextMS2("0", true);
            StopwatchProgressBar.Value = 0;

            {
                var changeAnimation1 = new DoubleAnimation
                {
                    From = 100,
                    To = 200,
                    Duration = TimeSpan.FromSeconds(swzoomspeed),
                    EasingFunction = new QuarticEase { EasingMode = EasingMode.EaseOut }
                };
                var changeAnimation2 = new DoubleAnimation
                {
                    From = 100,
                    To = 200,
                    Duration = TimeSpan.FromSeconds(swzoomspeed),
                    EasingFunction = new QuarticEase { EasingMode = EasingMode.EaseOut }
                };
                Storyboard.SetTarget(changeAnimation1, StartStopwatchBtn);
                Storyboard.SetTargetProperty(changeAnimation1, new PropertyPath(WidthProperty));
                Storyboard.SetTarget(changeAnimation2, PauseStopwatchBtn);
                Storyboard.SetTargetProperty(changeAnimation2, new PropertyPath(WidthProperty));
                var storyboard = new Storyboard();
                storyboard.Children.Add(changeAnimation1);
                storyboard.Children.Add(changeAnimation2);
                storyboard.Begin();
            }
            {
                var changeAnimation1 = new DoubleAnimation
                {
                    From = 100,
                    To = 0,
                    Duration = TimeSpan.FromSeconds(swzoomspeed),
                    EasingFunction = new QuarticEase { EasingMode = EasingMode.EaseOut }
                };
                var changeAnimation2 = new DoubleAnimation
                {
                    From = 100,
                    To = 0,
                    Duration = TimeSpan.FromSeconds(swzoomspeed),
                    EasingFunction = new QuarticEase { EasingMode = EasingMode.EaseOut }
                };
                Storyboard.SetTarget(changeAnimation1, ResetStopwatchBtn);
                Storyboard.SetTargetProperty(changeAnimation1, new PropertyPath(WidthProperty));
                Storyboard.SetTarget(changeAnimation2, LapStopwatchBtn);
                Storyboard.SetTargetProperty(changeAnimation2, new PropertyPath(WidthProperty));
                var storyboard = new Storyboard();
                storyboard.Children.Add(changeAnimation1);
                storyboard.Children.Add(changeAnimation2);
                storyboard.Begin();
            }
            {
                var changeAnimation1 = new DoubleAnimation
                {
                    From = 20,
                    To = 0,
                    Duration = TimeSpan.FromSeconds(swzoomspeed),
                    EasingFunction = new QuarticEase { EasingMode = EasingMode.EaseOut }
                };
                Storyboard.SetTarget(changeAnimation1, SWButtonSep);
                Storyboard.SetTargetProperty(changeAnimation1, new PropertyPath(WidthProperty));
                var storyboard = new Storyboard();
                storyboard.Children.Add(changeAnimation1);
                storyboard.Begin();
            }

            await Task.Delay(150);

            StartStopwatchBtn.Visibility = Visibility.Visible;
            PauseStopwatchBtn.Visibility = Visibility.Hidden;
            ResetStopwatchBtn.Visibility = Visibility.Hidden;
            LapStopwatchBtn.Visibility = Visibility.Hidden;

            {
                var fadeAnimation1 = new DoubleAnimation
                {
                    From = 0,
                    To = 1,
                    Duration = TimeSpan.FromSeconds(swfadespeed),
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseInOut }
                };
                var fadeAnimation2 = new DoubleAnimation
                {
                    From = 0,
                    To = 1,
                    Duration = TimeSpan.FromSeconds(swfadespeed),
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseInOut }
                };
                Storyboard.SetTarget(fadeAnimation1, PauseStopwatchBtn);
                Storyboard.SetTargetProperty(fadeAnimation1, new PropertyPath(UIElement.OpacityProperty));
                Storyboard.SetTarget(fadeAnimation2, LapStopwatchBtn);
                Storyboard.SetTargetProperty(fadeAnimation2, new PropertyPath(UIElement.OpacityProperty));
                var storyboard = new Storyboard();
                storyboard.Children.Add(fadeAnimation1);
                storyboard.Children.Add(fadeAnimation2);
                storyboard.Begin();
            }

            StopwatchIsRunning = false;
        }
        // - New lap
        private async void LapStopwatchBtn_Click(object sender, RoutedEventArgs e)
        {
            LapIndex++;

            if (!StopwatchLapDeployed)
            {
                StopwatchLapDeployed = true;

                double newRowLapHeight = StopwatchSubGrid.ActualHeight;
                newRowLapHeight *= 0.6;
                {
                    var sizeAnimation = new DoubleAnimation
                    {
                        From = 0,
                        To = newRowLapHeight,
                        Duration = TimeSpan.FromSeconds(0.3),
                        EasingFunction = new QuarticEase { EasingMode = EasingMode.EaseOut }
                    };
                    Storyboard.SetTarget(sizeAnimation, StopwatchLapScrollView);
                    Storyboard.SetTargetProperty(sizeAnimation, new PropertyPath(HeightProperty));
                    var storyboard = new Storyboard();
                    storyboard.Children.Add(sizeAnimation);
                    storyboard.Begin();
                }

                await Task.Delay(300);

                StopwatchLapRow.Height = new GridLength(0.6, GridUnitType.Star);
                StopwatchLapScrollView.Height = double.NaN;
            }

            TimeSpan LapTimeGap = stopwatch.Elapsed - LastLapTime;
            LastLapTime = stopwatch.Elapsed;

            Card LapCard = new Card();
            LapCard.Margin = new Thickness(20, 0, 20, 10);

            Grid LapGrid = new Grid();

            System.Windows.Controls.TextBlock LapTextIndex = new System.Windows.Controls.TextBlock();
            LapTextIndex.Text = LapIndex.ToString();
            LapTextIndex.FontSize = 18;
            LapTextIndex.FontFamily = new FontFamily("Segoe UI Variable Display SemiBold");
            LapTextIndex.Foreground = (Brush)FindResource("TextFillColorPrimaryBrush");
            LapTextIndex.HorizontalAlignment = HorizontalAlignment.Left;
            LapTextIndex.VerticalAlignment = VerticalAlignment.Center;

            System.Windows.Controls.TextBlock LapTextGap = new System.Windows.Controls.TextBlock();
            LapTextGap.Text = "+" + LapTimeGap.ToString("hh\\:mm\\:ss\\.ff");
            LapTextGap.FontSize = 18;
            LapTextGap.FontFamily = new FontFamily("Segoe UI Variable Display Regular");
            LapTextGap.Foreground = (Brush)FindResource("TextFillColorSecondaryBrush");
            LapTextGap.HorizontalAlignment = HorizontalAlignment.Center;
            LapTextGap.VerticalAlignment = VerticalAlignment.Center;

            System.Windows.Controls.TextBlock LapTextTotal = new System.Windows.Controls.TextBlock();
            LapTextTotal.Text = stopwatch.Elapsed.ToString("hh\\:mm\\:ss\\.ff");
            LapTextTotal.FontSize = 18;
            LapTextTotal.FontFamily = new FontFamily("Segoe UI Variable Display Regular");
            LapTextTotal.Foreground = (Brush)FindResource("TextFillColorPrimaryBrush");
            LapTextTotal.HorizontalAlignment = HorizontalAlignment.Right;
            LapTextTotal.VerticalAlignment = VerticalAlignment.Center;

            LapGrid.Children.Add(LapTextIndex);
            LapGrid.Children.Add(LapTextGap);
            LapGrid.Children.Add(LapTextTotal);

            LapCard.Content = LapGrid;

            LapStopwatchStack.Children.Insert(0, LapCard);

            LapCard = LapStopwatchStack.Children[0] as Card;
            double newCardHeight = 58;
            LapCard.Opacity = 0;

            {
                var sizeAnimation = new DoubleAnimation
                {
                    From = 0,
                    To = newCardHeight,
                    Duration = TimeSpan.FromSeconds(0.3),
                    EasingFunction = new QuarticEase { EasingMode = EasingMode.EaseOut }
                };
                Storyboard.SetTarget(sizeAnimation, LapCard);
                Storyboard.SetTargetProperty(sizeAnimation, new PropertyPath(HeightProperty));
                var storyboard = new Storyboard();
                storyboard.Children.Add(sizeAnimation);
                storyboard.Begin();
            }

            await Task.Delay(300);

            {
                var fadeAnimation = new DoubleAnimation
                {
                    From = 0,
                    To = 1,
                    Duration = TimeSpan.FromSeconds(0.3),
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                };
                Storyboard.SetTarget(fadeAnimation, LapCard);
                Storyboard.SetTargetProperty(fadeAnimation, new PropertyPath(OpacityProperty));
                var storyboard = new Storyboard();
                storyboard.Children.Add(fadeAnimation);
                storyboard.Begin();
            }
        }

        // Stopwatch text update with animation
        private async Task UpdateStopwatchTextH1(string text)
        {
            if (ActualStopwatchH1 != text)
            {
                StopwatchTextH1.Text = ActualStopwatchH1;
                ActualStopwatchH1 = text;
                {
                    var translateAnimation = new DoubleAnimation
                    {
                        From = 0,
                        To = -50,
                        Duration = TimeSpan.FromSeconds(swtxtslidespeed),
                        EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn }
                    };
                    Storyboard.SetTarget(translateAnimation, StopwatchTextH1);
                    Storyboard.SetTargetProperty(translateAnimation, new PropertyPath("(UIElement.RenderTransform).(TranslateTransform.Y)"));
                    var storyboard = new Storyboard();
                    storyboard.Children.Add(translateAnimation);
                    storyboard.Begin();
                }
                await Task.Delay(swtxtdelay);
                StopwatchTextH1.Text = text;
                {
                    var translateAnimation = new DoubleAnimation
                    {
                        From = 50,
                        To = 0,
                        Duration = TimeSpan.FromSeconds(swtxtslidespeed),
                        EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                    };
                    Storyboard.SetTarget(translateAnimation, StopwatchTextH1);
                    Storyboard.SetTargetProperty(translateAnimation, new PropertyPath("(UIElement.RenderTransform).(TranslateTransform.Y)"));
                    var storyboard = new Storyboard();
                    storyboard.Children.Add(translateAnimation);
                    storyboard.Begin();
                }
            }
        }
        private async Task UpdateStopwatchTextH2(string text)
        {
            if (ActualStopwatchH2 != text)
            {
                StopwatchTextH2.Text = ActualStopwatchH2;
                ActualStopwatchH2 = text;
                {
                    var translateAnimation = new DoubleAnimation
                    {
                        From = 0,
                        To = -50,
                        Duration = TimeSpan.FromSeconds(swtxtslidespeed),
                        EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn }
                    };
                    Storyboard.SetTarget(translateAnimation, StopwatchTextH2);
                    Storyboard.SetTargetProperty(translateAnimation, new PropertyPath("(UIElement.RenderTransform).(TranslateTransform.Y)"));
                    var storyboard = new Storyboard();
                    storyboard.Children.Add(translateAnimation);
                    storyboard.Begin();
                }
                await Task.Delay(swtxtdelay);
                StopwatchTextH2.Text = text;
                {
                    var translateAnimation = new DoubleAnimation
                    {
                        From = 50,
                        To = 0,
                        Duration = TimeSpan.FromSeconds(swtxtslidespeed),
                        EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                    };
                    Storyboard.SetTarget(translateAnimation, StopwatchTextH2);
                    Storyboard.SetTargetProperty(translateAnimation, new PropertyPath("(UIElement.RenderTransform).(TranslateTransform.Y)"));
                    var storyboard = new Storyboard();
                    storyboard.Children.Add(translateAnimation);
                    storyboard.Begin();
                }
            }
        }
        private async Task UpdateStopwatchTextM1(string text)
        {
            if (ActualStopwatchM1 != text)
            {
                StopwatchTextM1.Text = ActualStopwatchM1;
                ActualStopwatchM1 = text;
                {
                    var translateAnimation = new DoubleAnimation
                    {
                        From = 0,
                        To = -50,
                        Duration = TimeSpan.FromSeconds(swtxtslidespeed),
                        EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn }
                    };
                    Storyboard.SetTarget(translateAnimation, StopwatchTextM1);
                    Storyboard.SetTargetProperty(translateAnimation, new PropertyPath("(UIElement.RenderTransform).(TranslateTransform.Y)"));
                    var storyboard = new Storyboard();
                    storyboard.Children.Add(translateAnimation);
                    storyboard.Begin();
                }
                await Task.Delay(swtxtdelay);
                StopwatchTextM1.Text = text;
                {
                    var translateAnimation = new DoubleAnimation
                    {
                        From = 50,
                        To = 0,
                        Duration = TimeSpan.FromSeconds(swtxtslidespeed),
                        EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                    };
                    Storyboard.SetTarget(translateAnimation, StopwatchTextM1);
                    Storyboard.SetTargetProperty(translateAnimation, new PropertyPath("(UIElement.RenderTransform).(TranslateTransform.Y)"));
                    var storyboard = new Storyboard();
                    storyboard.Children.Add(translateAnimation);
                    storyboard.Begin();
                }
            }
        }
        private async Task UpdateStopwatchTextM2(string text)
        {
            if (ActualStopwatchM2 != text)
            {
                StopwatchTextM2.Text = ActualStopwatchM2;
                ActualStopwatchM2 = text;
                {
                    var translateAnimation = new DoubleAnimation
                    {
                        From = 0,
                        To = -50,
                        Duration = TimeSpan.FromSeconds(swtxtslidespeed),
                        EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn }
                    };
                    Storyboard.SetTarget(translateAnimation, StopwatchTextM2);
                    Storyboard.SetTargetProperty(translateAnimation, new PropertyPath("(UIElement.RenderTransform).(TranslateTransform.Y)"));
                    var storyboard = new Storyboard();
                    storyboard.Children.Add(translateAnimation);
                    storyboard.Begin();
                }
                await Task.Delay(swtxtdelay);
                StopwatchTextM2.Text = text;
                {
                    var translateAnimation = new DoubleAnimation
                    {
                        From = 50,
                        To = 0,
                        Duration = TimeSpan.FromSeconds(swtxtslidespeed),
                        EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                    };
                    Storyboard.SetTarget(translateAnimation, StopwatchTextM2);
                    Storyboard.SetTargetProperty(translateAnimation, new PropertyPath("(UIElement.RenderTransform).(TranslateTransform.Y)"));
                    var storyboard = new Storyboard();
                    storyboard.Children.Add(translateAnimation);
                    storyboard.Begin();
                }
            }
        }
        private async Task UpdateStopwatchTextS1(string text)
        {
            if (ActualStopwatchS1 != text)
            {
                StopwatchTextS1.Text = ActualStopwatchS1;
                ActualStopwatchS1 = text;
                {
                    var translateAnimation = new DoubleAnimation
                    {
                        From = 0,
                        To = -50,
                        Duration = TimeSpan.FromSeconds(swtxtslidespeed),
                        EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn }
                    };
                    Storyboard.SetTarget(translateAnimation, StopwatchTextS1);
                    Storyboard.SetTargetProperty(translateAnimation, new PropertyPath("(UIElement.RenderTransform).(TranslateTransform.Y)"));
                    var storyboard = new Storyboard();
                    storyboard.Children.Add(translateAnimation);
                    storyboard.Begin();
                }
                await Task.Delay(swtxtdelay);
                StopwatchTextS1.Text = text;
                {
                    var translateAnimation = new DoubleAnimation
                    {
                        From = 50,
                        To = 0,
                        Duration = TimeSpan.FromSeconds(swtxtslidespeed),
                        EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                    };
                    Storyboard.SetTarget(translateAnimation, StopwatchTextS1);
                    Storyboard.SetTargetProperty(translateAnimation, new PropertyPath("(UIElement.RenderTransform).(TranslateTransform.Y)"));
                    var storyboard = new Storyboard();
                    storyboard.Children.Add(translateAnimation);
                    storyboard.Begin();
                }
            }
        }
        private async Task UpdateStopwatchTextS2(string text)
        {
            if (ActualStopwatchS2 != text)
            {
                StopwatchTextS2.Text = ActualStopwatchS2;
                ActualStopwatchS2 = text;
                {
                    var translateAnimation = new DoubleAnimation
                    {
                        From = 0,
                        To = -50,
                        Duration = TimeSpan.FromSeconds(swtxtslidespeed),
                        EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn }
                    };
                    Storyboard.SetTarget(translateAnimation, StopwatchTextS2);
                    Storyboard.SetTargetProperty(translateAnimation, new PropertyPath("(UIElement.RenderTransform).(TranslateTransform.Y)"));
                    var storyboard = new Storyboard();
                    storyboard.Children.Add(translateAnimation);
                    storyboard.Begin();
                }
                await Task.Delay(swtxtdelay);
                StopwatchTextS2.Text = text;
                {
                    var translateAnimation = new DoubleAnimation
                    {
                        From = 50,
                        To = 0,
                        Duration = TimeSpan.FromSeconds(swtxtslidespeed),
                        EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                    };
                    Storyboard.SetTarget(translateAnimation, StopwatchTextS2);
                    Storyboard.SetTargetProperty(translateAnimation, new PropertyPath("(UIElement.RenderTransform).(TranslateTransform.Y)"));
                    var storyboard = new Storyboard();
                    storyboard.Children.Add(translateAnimation);
                    storyboard.Begin();
                }
            }
        }

        // Stopwatch text update without animation
        private async Task UpdateStopwatchTextMS1(string text, bool reset)
        {
            if (ActualStopwatchMS1 != text)
            {
                StopwatchTextMS1.Text = ActualStopwatchMS1;
                if (reset)
                {
                    {
                        var translateAnimation = new DoubleAnimation
                        {
                            From = 0,
                            To = -50,
                            Duration = TimeSpan.FromSeconds(swtxtslidespeed),
                            EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn }
                        };
                        Storyboard.SetTarget(translateAnimation, StopwatchTextMS1);
                        Storyboard.SetTargetProperty(translateAnimation, new PropertyPath("(UIElement.RenderTransform).(TranslateTransform.Y)"));
                        var storyboard = new Storyboard();
                        storyboard.Children.Add(translateAnimation);
                        storyboard.Begin();
                    }
                    await Task.Delay(swtxtdelay);
                    StopwatchTextMS1.Text = text;
                    {
                        var translateAnimation = new DoubleAnimation
                        {
                            From = 50,
                            To = 0,
                            Duration = TimeSpan.FromSeconds(swtxtslidespeed),
                            EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                        };
                        Storyboard.SetTarget(translateAnimation, StopwatchTextMS1);
                        Storyboard.SetTargetProperty(translateAnimation, new PropertyPath("(UIElement.RenderTransform).(TranslateTransform.Y)"));
                        var storyboard = new Storyboard();
                        storyboard.Children.Add(translateAnimation);
                        storyboard.Begin();
                    }
                }
                StopwatchTextMS1.Text = text;
                ActualStopwatchMS1 = text;
            }
        }
        private async Task UpdateStopwatchTextMS2(string text, bool reset)
        {
            if (ActualStopwatchMS2 != text)
            {
                StopwatchTextMS2.Text = ActualStopwatchMS2;
                if (reset)
                {
                    {
                        var translateAnimation = new DoubleAnimation
                        {
                            From = 0,
                            To = -50,
                            Duration = TimeSpan.FromSeconds(swtxtslidespeed),
                            EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn }
                        };
                        Storyboard.SetTarget(translateAnimation, StopwatchTextMS2);
                        Storyboard.SetTargetProperty(translateAnimation, new PropertyPath("(UIElement.RenderTransform).(TranslateTransform.Y)"));
                        var storyboard = new Storyboard();
                        storyboard.Children.Add(translateAnimation);
                        storyboard.Begin();
                    }
                    await Task.Delay(swtxtdelay);
                    StopwatchTextMS2.Text = text;
                    {
                        var translateAnimation = new DoubleAnimation
                        {
                            From = 50,
                            To = 0,
                            Duration = TimeSpan.FromSeconds(swtxtslidespeed),
                            EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                        };
                        Storyboard.SetTarget(translateAnimation, StopwatchTextMS2);
                        Storyboard.SetTargetProperty(translateAnimation, new PropertyPath("(UIElement.RenderTransform).(TranslateTransform.Y)"));
                        var storyboard = new Storyboard();
                        storyboard.Children.Add(translateAnimation);
                        storyboard.Begin();
                    }
                }
                StopwatchTextMS2.Text = text;
                ActualStopwatchMS2 = text;
            }
        }
        #endregion

        #region Timer

        /*
         * Time Digit Legend:
         * H1 = [0]0:00:00
         * H2 = 0[0]:00:00
         * M1 = 00:[0]0:00
         * M2 = 00:0[0]:00
         * S1 = 00:00:[0]0
         * S2 = 00:00:0[0]
         */

        // Current char of the timer
        private string ActualTimerH1 = "0";
        private string ActualTimerH2 = "0";
        private string ActualTimerM1 = "0";
        private string ActualTimerM2 = "0";
        private string ActualTimerS1 = "0";
        private string ActualTimerS2 = "0";

        // Current progress ring value
        private int ActualTimeProgress = 100;

        // Timer values when timer starts
        private TimeSpan timerSet = TimeSpan.Zero;

        // Timer status
        // - Is the timer running?
        private bool TimerIsRunning = false;
        // - Is time up?
        private bool TimeUp = false;

        // Animation variables
        // - Fade speed
        private double tfadespeed = 0.30;
        // - Zoom speed
        private double tzoomspeed = 0.30;
        // - Text slide speed
        private double ttxtslidespeed = 0.15;
        // - Text slide delay
        private int ttxtdelay = 150;

        // Sound player instance for time up sound
        private SoundPlayer TimeUpSoundPlayer;

        // Timer functions
        // - Start
        private async void StartTimerBtn_Click(object sender, RoutedEventArgs e)
        {
            if (!TimerIsRunning)
            {
                TimerProgressRing.Progress = 100;
                timer = TimeSpan.Zero;
                timerSet = TimeSpan.Zero;
                timerSet = timerSet.Add(TimeSpan.FromHours(int.Parse(ActualTimerH1) * 10 + int.Parse(ActualTimerH2)));
                timerSet = timerSet.Add(TimeSpan.FromMinutes(int.Parse(ActualTimerM1) * 10 + int.Parse(ActualTimerM2)));
                timerSet = timerSet.Add(TimeSpan.FromSeconds(int.Parse(ActualTimerS1) * 10 + int.Parse(ActualTimerS2)));
                if (timerSet != TimeSpan.Zero)
                {
                    timer = timer.Add(timerSet);
                    timerTimer.Start();
                    StartTimerBtn.Visibility = Visibility.Hidden;
                    PauseTimerBtn.Visibility = Visibility.Visible;
                    ResetTimerBtn.Visibility = Visibility.Visible;
                    PauseTimerBtn.Opacity = 1;
                    {
                        var changeAnimation1 = new DoubleAnimation
                        {
                            From = 200,
                            To = 100,
                            Duration = TimeSpan.FromSeconds(tzoomspeed),
                            EasingFunction = new QuarticEase { EasingMode = EasingMode.EaseOut }
                        };
                        var changeAnimation2 = new DoubleAnimation
                        {
                            From = 200,
                            To = 100,
                            Duration = TimeSpan.FromSeconds(tzoomspeed),
                            EasingFunction = new QuarticEase { EasingMode = EasingMode.EaseOut }
                        };
                        Storyboard.SetTarget(changeAnimation1, StartTimerBtn);
                        Storyboard.SetTargetProperty(changeAnimation1, new PropertyPath(WidthProperty));
                        Storyboard.SetTarget(changeAnimation2, PauseTimerBtn);
                        Storyboard.SetTargetProperty(changeAnimation2, new PropertyPath(WidthProperty));
                        var storyboard = new Storyboard();
                        storyboard.Children.Add(changeAnimation1);
                        storyboard.Children.Add(changeAnimation2);
                        storyboard.Begin();
                    }
                    {
                        var changeAnimation1 = new DoubleAnimation
                        {
                            From = 0,
                            To = 100,
                            Duration = TimeSpan.FromSeconds(tzoomspeed),
                            EasingFunction = new QuarticEase { EasingMode = EasingMode.EaseOut }
                        };
                        Storyboard.SetTarget(changeAnimation1, ResetTimerBtn);
                        Storyboard.SetTargetProperty(changeAnimation1, new PropertyPath(WidthProperty));
                        var storyboard = new Storyboard();
                        storyboard.Children.Add(changeAnimation1);
                        storyboard.Begin();
                    }
                    {
                        var changeAnimation1 = new DoubleAnimation
                        {
                            From = 0,
                            To = 20,
                            Duration = TimeSpan.FromSeconds(tzoomspeed),
                            EasingFunction = new QuarticEase { EasingMode = EasingMode.EaseOut }
                        };
                        Storyboard.SetTarget(changeAnimation1, TButtonSep);
                        Storyboard.SetTargetProperty(changeAnimation1, new PropertyPath(WidthProperty));
                        var storyboard = new Storyboard();
                        storyboard.Children.Add(changeAnimation1);
                        storyboard.Begin();
                    }
                    {
                        var zommAnimation1 = new DoubleAnimation
                        {
                            From = 0,
                            To = 1,
                            Duration = TimeSpan.FromSeconds(tzoomspeed),
                            EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                        };
                        var zommAnimation2 = new DoubleAnimation
                        {
                            From = 0,
                            To = 1,
                            Duration = TimeSpan.FromSeconds(tzoomspeed),
                            EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                        };
                        var fadeAnimation1 = new DoubleAnimation
                        {
                            From = 0,
                            To = 1,
                            Duration = TimeSpan.FromSeconds(tfadespeed),
                            EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn }
                        };
                        Storyboard.SetTarget(zommAnimation1, TimerProgressRing);
                        Storyboard.SetTargetProperty(zommAnimation1, new PropertyPath("(UIElement.RenderTransform).(ScaleTransform.ScaleX)"));
                        Storyboard.SetTarget(zommAnimation2, TimerProgressRing);
                        Storyboard.SetTargetProperty(zommAnimation2, new PropertyPath("(UIElement.RenderTransform).(ScaleTransform.ScaleY)"));
                        Storyboard.SetTarget(fadeAnimation1, TimerProgressRing);
                        Storyboard.SetTargetProperty(fadeAnimation1, new PropertyPath(OpacityProperty));
                        var storyboard = new Storyboard();
                        storyboard.Children.Add(zommAnimation1);
                        storyboard.Children.Add(zommAnimation2);
                        storyboard.Children.Add(fadeAnimation1);
                        storyboard.Begin();
                    }
                    foreach (UIElement element in TimerwBtnGrid.Children)
                    {
                        if (element is System.Windows.Controls.Button)
                        {
                            var zoomAnimation1 = new DoubleAnimation
                            {
                                From = 31,
                                To = 0,
                                Duration = TimeSpan.FromSeconds(tzoomspeed),
                                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                            };
                            Storyboard.SetTarget(zoomAnimation1, element);
                            Storyboard.SetTargetProperty(zoomAnimation1, new PropertyPath(HeightProperty));
                            var storyboard = new Storyboard();
                            storyboard.Children.Add(zoomAnimation1);
                            storyboard.Begin();
                        }
                    }
                    TimerIsRunning = true;
                }
            }
            else
            {
                timerTimer.Start();
                {
                    var fadeAnimation1 = new DoubleAnimation
                    {
                        From = 1,
                        To = 0,
                        Duration = TimeSpan.FromSeconds(tfadespeed),
                        EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseInOut }
                    };
                    var fadeAnimation2 = new DoubleAnimation
                    {
                        From = 1,
                        To = 0,
                        Duration = TimeSpan.FromSeconds(tfadespeed),
                        EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseInOut }
                    };
                    Storyboard.SetTarget(fadeAnimation1, StartTimerBtn);
                    Storyboard.SetTargetProperty(fadeAnimation1, new PropertyPath(UIElement.OpacityProperty));
                    Storyboard.SetTarget(fadeAnimation2, ResetTimerBtn);
                    Storyboard.SetTargetProperty(fadeAnimation2, new PropertyPath(UIElement.OpacityProperty));
                    var storyboard = new Storyboard();
                    storyboard.Children.Add(fadeAnimation1);
                    storyboard.Children.Add(fadeAnimation2);
                    storyboard.Begin();
                }
                await Task.Delay(150);
                StartTimerBtn.Visibility = Visibility.Hidden;
                PauseTimerBtn.Visibility = Visibility.Visible;
                ResetTimerBtn.Visibility = Visibility.Visible;
                {
                    var fadeAnimation1 = new DoubleAnimation
                    {
                        From = 0,
                        To = 1,
                        Duration = TimeSpan.FromSeconds(tfadespeed),
                        EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseInOut }
                    };
                    var fadeAnimation2 = new DoubleAnimation
                    {
                        From = 0,
                        To = 1,
                        Duration = TimeSpan.FromSeconds(tfadespeed),
                        EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseInOut }
                    };
                    var fadeAnimation3 = new DoubleAnimation
                    {
                        From = 0,
                        To = 1,
                        Duration = TimeSpan.FromSeconds(tfadespeed),
                        EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseInOut }
                    };
                    Storyboard.SetTarget(fadeAnimation1, PauseTimerBtn);
                    Storyboard.SetTargetProperty(fadeAnimation1, new PropertyPath(UIElement.OpacityProperty));
                    Storyboard.SetTarget(fadeAnimation2, ResetTimerBtn);
                    Storyboard.SetTargetProperty(fadeAnimation2, new PropertyPath(UIElement.OpacityProperty));
                    Storyboard.SetTarget(fadeAnimation3, StartTimerBtn);
                    Storyboard.SetTargetProperty(fadeAnimation3, new PropertyPath(UIElement.OpacityProperty));
                    var storyboard = new Storyboard();
                    storyboard.Children.Add(fadeAnimation1);
                    storyboard.Children.Add(fadeAnimation2);
                    storyboard.Children.Add(fadeAnimation3);
                    storyboard.Begin();
                }
                TimerIsRunning = true;
            }
        }
        // - Pause
        private async void PauseTimerBtn_Click(object sender, RoutedEventArgs e)
        {
            timerTimer.Stop();
            {
                var fadeAnimation1 = new DoubleAnimation
                {
                    From = 1,
                    To = 0,
                    Duration = TimeSpan.FromSeconds(tfadespeed),
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseInOut }
                };
                var fadeAnimation2 = new DoubleAnimation
                {
                    From = 1,
                    To = 0,
                    Duration = TimeSpan.FromSeconds(tfadespeed),
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseInOut }
                };
                Storyboard.SetTarget(fadeAnimation1, PauseTimerBtn);
                Storyboard.SetTargetProperty(fadeAnimation1, new PropertyPath(UIElement.OpacityProperty));
                Storyboard.SetTarget(fadeAnimation2, ResetTimerBtn);
                Storyboard.SetTargetProperty(fadeAnimation2, new PropertyPath(UIElement.OpacityProperty));
                var storyboard = new Storyboard();
                storyboard.Children.Add(fadeAnimation1);
                storyboard.Children.Add(fadeAnimation2);
                storyboard.Begin();
            }
            await Task.Delay(150);
            StartTimerBtn.Visibility = Visibility.Visible;
            PauseTimerBtn.Visibility = Visibility.Hidden;
            ResetTimerBtn.Visibility = Visibility.Visible;
            {
                var fadeAnimation1 = new DoubleAnimation
                {
                    From = 0,
                    To = 1,
                    Duration = TimeSpan.FromSeconds(tfadespeed),
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseInOut }
                };
                var fadeAnimation2 = new DoubleAnimation
                {
                    From = 0,
                    To = 1,
                    Duration = TimeSpan.FromSeconds(tfadespeed),
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseInOut }
                };
                var fadeAnimation3 = new DoubleAnimation
                {
                    From = 0,
                    To = 1,
                    Duration = TimeSpan.FromSeconds(tfadespeed),
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseInOut }
                };
                Storyboard.SetTarget(fadeAnimation1, StartTimerBtn);
                Storyboard.SetTargetProperty(fadeAnimation1, new PropertyPath(UIElement.OpacityProperty));
                Storyboard.SetTarget(fadeAnimation2, ResetTimerBtn);
                Storyboard.SetTargetProperty(fadeAnimation2, new PropertyPath(UIElement.OpacityProperty));
                Storyboard.SetTarget(fadeAnimation3, PauseTimerBtn);
                Storyboard.SetTargetProperty(fadeAnimation3, new PropertyPath(UIElement.OpacityProperty));
                var storyboard = new Storyboard();
                storyboard.Children.Add(fadeAnimation1);
                storyboard.Children.Add(fadeAnimation2);
                storyboard.Children.Add(fadeAnimation3);
                storyboard.Begin();
            }
        }
        // - Reset
        private async void ResetTimerBtn_Click(object sender, RoutedEventArgs e)
        {
            timerTimer.Stop();
            timer = TimeSpan.Zero;
            UpdateTimerTextH1((timerSet.Hours / 10).ToString(), true);
            UpdateTimerTextH2((timerSet.Hours % 10).ToString(), true);
            UpdateTimerTextM1((timerSet.Minutes / 10).ToString(), true);
            UpdateTimerTextM2((timerSet.Minutes % 10).ToString(), true);
            UpdateTimerTextS1((timerSet.Seconds / 10).ToString(), true);
            UpdateTimerTextS2((timerSet.Seconds % 10).ToString(), true);
            UpdateTimerProgressRing(100);
            StartTimerBtn.Visibility = Visibility.Visible;
            TimerIsRunning = false;
            {
                var changeAnimation1 = new DoubleAnimation
                {
                    From = 100,
                    To = 200,
                    Duration = TimeSpan.FromSeconds(tzoomspeed),
                    EasingFunction = new QuarticEase { EasingMode = EasingMode.EaseOut }
                };
                var changeAnimation2 = new DoubleAnimation
                {
                    From = 100,
                    To = 200,
                    Duration = TimeSpan.FromSeconds(tzoomspeed),
                    EasingFunction = new QuarticEase { EasingMode = EasingMode.EaseOut }
                };
                Storyboard.SetTarget(changeAnimation1, StartTimerBtn);
                Storyboard.SetTargetProperty(changeAnimation1, new PropertyPath(WidthProperty));
                Storyboard.SetTarget(changeAnimation2, PauseTimerBtn);
                Storyboard.SetTargetProperty(changeAnimation2, new PropertyPath(WidthProperty));
                var storyboard = new Storyboard();
                storyboard.Children.Add(changeAnimation1);
                storyboard.Children.Add(changeAnimation2);
                storyboard.Begin();
            }
            {
                var changeAnimation1 = new DoubleAnimation
                {
                    From = 100,
                    To = 0,
                    Duration = TimeSpan.FromSeconds(tzoomspeed),
                    EasingFunction = new QuarticEase { EasingMode = EasingMode.EaseOut }
                };
                Storyboard.SetTarget(changeAnimation1, ResetTimerBtn);
                Storyboard.SetTargetProperty(changeAnimation1, new PropertyPath(WidthProperty));
                var storyboard = new Storyboard();
                storyboard.Children.Add(changeAnimation1);
                storyboard.Begin();
            }
            {
                var changeAnimation1 = new DoubleAnimation
                {
                    From = 20,
                    To = 0,
                    Duration = TimeSpan.FromSeconds(tzoomspeed),
                    EasingFunction = new QuarticEase { EasingMode = EasingMode.EaseOut }
                };
                Storyboard.SetTarget(changeAnimation1, TButtonSep);
                Storyboard.SetTargetProperty(changeAnimation1, new PropertyPath(WidthProperty));
                var storyboard = new Storyboard();
                storyboard.Children.Add(changeAnimation1);
                storyboard.Begin();
            }

            await Task.Delay(150);

            PauseTimerBtn.Visibility = Visibility.Hidden;
            ResetTimerBtn.Visibility = Visibility.Hidden;

            {
                var zommAnimation1 = new DoubleAnimation
                {
                    From = 1,
                    To = 0,
                    Duration = TimeSpan.FromSeconds(tzoomspeed),
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn }
                };
                var zommAnimation2 = new DoubleAnimation
                {
                    From = 1,
                    To = 0,
                    Duration = TimeSpan.FromSeconds(tzoomspeed),
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn }
                };
                var fadeAnimation1 = new DoubleAnimation
                {
                    From = 1,
                    To = 0,
                    Duration = TimeSpan.FromSeconds(tfadespeed),
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                };
                Storyboard.SetTarget(zommAnimation1, TimerProgressRing);
                Storyboard.SetTargetProperty(zommAnimation1, new PropertyPath("(UIElement.RenderTransform).(ScaleTransform.ScaleX)"));
                Storyboard.SetTarget(zommAnimation2, TimerProgressRing);
                Storyboard.SetTargetProperty(zommAnimation2, new PropertyPath("(UIElement.RenderTransform).(ScaleTransform.ScaleY)"));
                Storyboard.SetTarget(fadeAnimation1, TimerProgressRing);
                Storyboard.SetTargetProperty(fadeAnimation1, new PropertyPath(OpacityProperty));
                var storyboard = new Storyboard();
                storyboard.Children.Add(zommAnimation1);
                storyboard.Children.Add(zommAnimation2);
                storyboard.Children.Add(fadeAnimation1);
                storyboard.Begin();
            }
            foreach (UIElement element in TimerwBtnGrid.Children)
            {
                if (element is System.Windows.Controls.Button)
                {
                    var zoomAnimation1 = new DoubleAnimation
                    {
                        From = 0,
                        To = 31,
                        Duration = TimeSpan.FromSeconds(tzoomspeed),
                        EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                    };
                    Storyboard.SetTarget(zoomAnimation1, element);
                    Storyboard.SetTargetProperty(zoomAnimation1, new PropertyPath(HeightProperty));
                    var storyboard = new Storyboard();
                    storyboard.Children.Add(zoomAnimation1);
                    storyboard.Begin();
                }
            }
        }

        // Tick event
        private async void Timer_Tick(object sender, EventArgs e)
        {
            if (timer > TimeSpan.Zero)
            {
                timer = timer.Subtract(TimeSpan.FromSeconds(1));
                UpdateTimerTextH1((timer.Hours / 10).ToString(), false);
                UpdateTimerTextH2((timer.Hours % 10).ToString(), false);
                UpdateTimerTextM1((timer.Minutes / 10).ToString(), false);
                UpdateTimerTextM2((timer.Minutes % 10).ToString(), false);
                UpdateTimerTextS1((timer.Seconds / 10).ToString(), false);
                UpdateTimerTextS2((timer.Seconds % 10).ToString(), false);
                UpdateTimerProgressRing((int)(100 * (timerSet.TotalSeconds - (timerSet.TotalSeconds - timer.TotalSeconds)) / timerSet.TotalSeconds));
            }
            else
            {
                timerTimer.Stop();
                TimeUp = true;
                TimeUpShow();
            }
        }

        // Time up function
        private async Task TimeUpShow()
        {
            TimeUpSoundPlayer = new SoundPlayer(ConfigManager.Variables.DefaultTimeUpSound);
            TimeUpSoundPlayer.Load();
            TimeUpSoundPlayer.PlayLooping();
            TimeUpGrid.Visibility = Visibility.Visible;
            {
                var fadeAnimation1 = new DoubleAnimation
                {
                    From = 0,
                    To = 1,
                    Duration = TimeSpan.FromSeconds(tfadespeed),
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn }
                };
                var zoomAnimation1 = new DoubleAnimation
                {
                    From = 1.1,
                    To = 1,
                    Duration = TimeSpan.FromSeconds(tzoomspeed),
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                };
                var zoomAnimation2 = new DoubleAnimation
                {
                    From = 1.1,
                    To = 1,
                    Duration = TimeSpan.FromSeconds(tzoomspeed),
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                };
                Storyboard.SetTarget(fadeAnimation1, TimeUpGrid);
                Storyboard.SetTargetProperty(fadeAnimation1, new PropertyPath(OpacityProperty));
                Storyboard.SetTarget(zoomAnimation1, TimeUpGrid);
                Storyboard.SetTargetProperty(zoomAnimation1, new PropertyPath("(UIElement.RenderTransform).(ScaleTransform.ScaleX)"));
                Storyboard.SetTarget(zoomAnimation2, TimeUpGrid);
                Storyboard.SetTargetProperty(zoomAnimation2, new PropertyPath("(UIElement.RenderTransform).(ScaleTransform.ScaleY)"));
                var storyboard = new Storyboard();
                storyboard.Children.Add(fadeAnimation1);
                storyboard.Children.Add(zoomAnimation1);
                storyboard.Children.Add(zoomAnimation2);
                storyboard.Begin();
            }
            while (TimeUp)
            {
                await Task.Delay(100);
            }
            {
                var fadeAnimation1 = new DoubleAnimation
                {
                    From = 1,
                    To = 0,
                    Duration = TimeSpan.FromSeconds(tfadespeed),
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                };
                var zoomAnimation1 = new DoubleAnimation
                {
                    From = 1,
                    To = 1.1,
                    Duration = TimeSpan.FromSeconds(tzoomspeed),
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn }
                };
                var zoomAnimation2 = new DoubleAnimation
                {
                    From = 1,
                    To = 1.1,
                    Duration = TimeSpan.FromSeconds(tzoomspeed),
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn }
                };
                Storyboard.SetTarget(fadeAnimation1, TimeUpGrid);
                Storyboard.SetTargetProperty(fadeAnimation1, new PropertyPath(OpacityProperty));
                Storyboard.SetTarget(zoomAnimation1, TimeUpGrid);
                Storyboard.SetTargetProperty(zoomAnimation1, new PropertyPath("(UIElement.RenderTransform).(ScaleTransform.ScaleX)"));
                Storyboard.SetTarget(zoomAnimation2, TimeUpGrid);
                Storyboard.SetTargetProperty(zoomAnimation2, new PropertyPath("(UIElement.RenderTransform).(ScaleTransform.ScaleY)"));
                var storyboard = new Storyboard();
                storyboard.Children.Add(fadeAnimation1);
                storyboard.Children.Add(zoomAnimation1);
                storyboard.Children.Add(zoomAnimation2);
                storyboard.Begin();
            }
            TimeUpSoundPlayer.Stop();
            TimeUpSoundPlayer.Dispose();
            timer = TimeSpan.Zero;
            UpdateTimerTextH1((timerSet.Hours / 10).ToString(), true);
            UpdateTimerTextH2((timerSet.Hours % 10).ToString(), true);
            UpdateTimerTextM1((timerSet.Minutes / 10).ToString(), true);
            UpdateTimerTextM2((timerSet.Minutes % 10).ToString(), true);
            UpdateTimerTextS1((timerSet.Seconds / 10).ToString(), true);
            UpdateTimerTextS2((timerSet.Seconds % 10).ToString(), true);
            UpdateTimerProgressRing(100);
            StartTimerBtn.Visibility = Visibility.Visible;
            TimerIsRunning = false;
            {
                var changeAnimation1 = new DoubleAnimation
                {
                    From = 100,
                    To = 200,
                    Duration = TimeSpan.FromSeconds(tzoomspeed),
                    EasingFunction = new QuarticEase { EasingMode = EasingMode.EaseOut }
                };
                var changeAnimation2 = new DoubleAnimation
                {
                    From = 100,
                    To = 200,
                    Duration = TimeSpan.FromSeconds(tzoomspeed),
                    EasingFunction = new QuarticEase { EasingMode = EasingMode.EaseOut }
                };
                Storyboard.SetTarget(changeAnimation1, StartTimerBtn);
                Storyboard.SetTargetProperty(changeAnimation1, new PropertyPath(WidthProperty));
                Storyboard.SetTarget(changeAnimation2, PauseTimerBtn);
                Storyboard.SetTargetProperty(changeAnimation2, new PropertyPath(WidthProperty));
                var storyboard = new Storyboard();
                storyboard.Children.Add(changeAnimation1);
                storyboard.Children.Add(changeAnimation2);
                storyboard.Begin();
            }
            {
                var changeAnimation1 = new DoubleAnimation
                {
                    From = 100,
                    To = 0,
                    Duration = TimeSpan.FromSeconds(tzoomspeed),
                    EasingFunction = new QuarticEase { EasingMode = EasingMode.EaseOut }
                };
                Storyboard.SetTarget(changeAnimation1, ResetTimerBtn);
                Storyboard.SetTargetProperty(changeAnimation1, new PropertyPath(WidthProperty));
                var storyboard = new Storyboard();
                storyboard.Children.Add(changeAnimation1);
                storyboard.Begin();
            }
            {
                var changeAnimation1 = new DoubleAnimation
                {
                    From = 20,
                    To = 0,
                    Duration = TimeSpan.FromSeconds(tzoomspeed),
                    EasingFunction = new QuarticEase { EasingMode = EasingMode.EaseOut }
                };
                Storyboard.SetTarget(changeAnimation1, TButtonSep);
                Storyboard.SetTargetProperty(changeAnimation1, new PropertyPath(WidthProperty));
                var storyboard = new Storyboard();
                storyboard.Children.Add(changeAnimation1);
                storyboard.Begin();
            }

            await Task.Delay(300);

            PauseTimerBtn.Visibility = Visibility.Hidden;
            ResetTimerBtn.Visibility = Visibility.Hidden;

            {
                var zommAnimation1 = new DoubleAnimation
                {
                    From = 1,
                    To = 0,
                    Duration = TimeSpan.FromSeconds(tzoomspeed),
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn }
                };
                var zommAnimation2 = new DoubleAnimation
                {
                    From = 1,
                    To = 0,
                    Duration = TimeSpan.FromSeconds(tzoomspeed),
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn }
                };
                var fadeAnimation1 = new DoubleAnimation
                {
                    From = 1,
                    To = 0,
                    Duration = TimeSpan.FromSeconds(tfadespeed),
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                };
                Storyboard.SetTarget(zommAnimation1, TimerProgressRing);
                Storyboard.SetTargetProperty(zommAnimation1, new PropertyPath("(UIElement.RenderTransform).(ScaleTransform.ScaleX)"));
                Storyboard.SetTarget(zommAnimation2, TimerProgressRing);
                Storyboard.SetTargetProperty(zommAnimation2, new PropertyPath("(UIElement.RenderTransform).(ScaleTransform.ScaleY)"));
                Storyboard.SetTarget(fadeAnimation1, TimerProgressRing);
                Storyboard.SetTargetProperty(fadeAnimation1, new PropertyPath(OpacityProperty));
                var storyboard = new Storyboard();
                storyboard.Children.Add(zommAnimation1);
                storyboard.Children.Add(zommAnimation2);
                storyboard.Children.Add(fadeAnimation1);
                storyboard.Begin();
            }
            foreach (UIElement element in TimerwBtnGrid.Children)
            {
                if (element is System.Windows.Controls.Button)
                {
                    var zoomAnimation1 = new DoubleAnimation
                    {
                        From = 0,
                        To = 31,
                        Duration = TimeSpan.FromSeconds(tzoomspeed),
                        EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                    };
                    Storyboard.SetTarget(zoomAnimation1, element);
                    Storyboard.SetTargetProperty(zoomAnimation1, new PropertyPath(HeightProperty));
                    var storyboard = new Storyboard();
                    storyboard.Children.Add(zoomAnimation1);
                    storyboard.Begin();
                }
            }
            TimeUpGrid.Visibility = Visibility.Hidden;
        }

        // Timer set buttons
        private async void TimeUpStopBtn_Click(object sender, EventArgs e)
        {
            TimeUp = false;
        }
        private async void TimerHourUpBtn_Click(object sender, RoutedEventArgs e)
        {
            if (ActualTimerH1 == "2" && ActualTimerH2 == "3")
            {
                UpdateTimerTextH1("0", false);
                UpdateTimerTextH2("0", false);
            }
            else if (ActualTimerH2 == "9")
            {
                UpdateTimerTextH2("0", false);
                UpdateTimerTextH1((int.Parse(ActualTimerH1) + 1).ToString(), false);
            }
            else
            {
                UpdateTimerTextH2((int.Parse(ActualTimerH2) + 1).ToString(), false);
            }
        }
        private async void TimerHourDownBtn_Click(object sender, RoutedEventArgs e)
        {
            if (ActualTimerH1 == "0" && ActualTimerH2 == "0")
            {
                UpdateTimerTextH1("2", false);
                UpdateTimerTextH2("3", false);
            }
            else if (ActualTimerH2 == "0")
            {
                UpdateTimerTextH2("9", false);
                UpdateTimerTextH1((int.Parse(ActualTimerH1) - 1).ToString(), false);
            }
            else
            {
                UpdateTimerTextH2((int.Parse(ActualTimerH2) - 1).ToString(), false);
            }
        }
        private async void TimerMinuteUpBtn_Click(object sender, RoutedEventArgs e)
        {
            if (ActualTimerM1 == "5" && ActualTimerM2 == "9")
            {
                UpdateTimerTextM1("0", false);
                UpdateTimerTextM2("0", false);
            }
            else if (ActualTimerM2 == "9")
            {
                UpdateTimerTextM2("0", false);
                UpdateTimerTextM1((int.Parse(ActualTimerM1) + 1).ToString(), false);
            }
            else
            {
                UpdateTimerTextM2((int.Parse(ActualTimerM2) + 1).ToString(), false);
            }
        }
        private async void TimerMinuteDownBtn_Click(object sender, RoutedEventArgs e)
        {
            if (ActualTimerM1 == "0" && ActualTimerM2 == "0")
            {
                UpdateTimerTextM1("5", false);
                UpdateTimerTextM2("9", false);
            }
            else if (ActualTimerM2 == "0")
            {
                UpdateTimerTextM2("9", false);
                UpdateTimerTextM1((int.Parse(ActualTimerM1) - 1).ToString(), false);
            }
            else
            {
                UpdateTimerTextM2((int.Parse(ActualTimerM2) - 1).ToString(), false);
            }
        }
        private async void TimerSecondUpBtn_Click(object sender, RoutedEventArgs e)
        {
            if (ActualTimerS1 == "5" && ActualTimerS2 == "9")
            {
                UpdateTimerTextS1("0", false);
                UpdateTimerTextS2("0", false);
            }
            else if (ActualTimerS2 == "9")
            {
                UpdateTimerTextS2("0", false);
                UpdateTimerTextS1((int.Parse(ActualTimerS1) + 1).ToString(), false);
            }
            else
            {
                UpdateTimerTextS2((int.Parse(ActualTimerS2) + 1).ToString(), false);
            }
        }
        private async void TimerSecondDownBtn_Click(object sender, RoutedEventArgs e)
        {
            if (ActualTimerS1 == "0" && ActualTimerS2 == "0")
            {
                UpdateTimerTextS1("5", false);
                UpdateTimerTextS2("9", false);
            }
            else if (ActualTimerS2 == "0")
            {
                UpdateTimerTextS2("9", false);
                UpdateTimerTextS1((int.Parse(ActualTimerS1) - 1).ToString(), false);
            }
            else
            {
                UpdateTimerTextS2((int.Parse(ActualTimerS2) - 1).ToString(), false);
            }
        }

        // Timer text update with animation
        private async Task UpdateTimerTextH1(string text, bool reset)
        {
            string direction = "none";
            if ((text == "0" && ActualTimerH1 == "2") && !reset)
            {
                direction = "up";
            }
            else if ((text == "2" && ActualTimerH1 == "0") && !reset)
            {
                direction = "down";
            }
            else if ((text != ActualTimerH1) && reset)
            {
                direction = "up";
            }
            else if (int.Parse(text) > int.Parse(ActualTimerH1))
            {
                direction = "up";
            }
            else if (int.Parse(text) < int.Parse(ActualTimerH1))
            {
                direction = "down";
            }
            else if (text == ActualTimerH1)
            {
                direction = "none";
            }

            if (direction == "up")
            {
                {
                    var translateAnimation = new DoubleAnimation
                    {
                        From = 0,
                        To = -50,
                        Duration = TimeSpan.FromSeconds(ttxtslidespeed),
                        EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn }
                    };
                    Storyboard.SetTarget(translateAnimation, TimerH1Text);
                    Storyboard.SetTargetProperty(translateAnimation, new PropertyPath("(UIElement.RenderTransform).(TranslateTransform.Y)"));
                    var storyboard = new Storyboard();
                    storyboard.Children.Add(translateAnimation);
                    storyboard.Begin();
                }
                await Task.Delay(ttxtdelay);
                TimerH1Text.Text = text;
                {
                    var translateAnimation = new DoubleAnimation
                    {
                        From = 50,
                        To = 0,
                        Duration = TimeSpan.FromSeconds(ttxtslidespeed),
                        EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                    };
                    Storyboard.SetTarget(translateAnimation, TimerH1Text);
                    Storyboard.SetTargetProperty(translateAnimation, new PropertyPath("(UIElement.RenderTransform).(TranslateTransform.Y)"));
                    var storyboard = new Storyboard();
                    storyboard.Children.Add(translateAnimation);
                    storyboard.Begin();
                }
            }
            else if (direction == "down")
            {
                {
                    var translateAnimation = new DoubleAnimation
                    {
                        From = 0,
                        To = 50,
                        Duration = TimeSpan.FromSeconds(ttxtslidespeed),
                        EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn }
                    };
                    Storyboard.SetTarget(translateAnimation, TimerH1Text);
                    Storyboard.SetTargetProperty(translateAnimation, new PropertyPath("(UIElement.RenderTransform).(TranslateTransform.Y)"));
                    var storyboard = new Storyboard();
                    storyboard.Children.Add(translateAnimation);
                    storyboard.Begin();
                }
                await Task.Delay(ttxtdelay);
                TimerH1Text.Text = text;
                {
                    var translateAnimation = new DoubleAnimation
                    {
                        From = -50,
                        To = 0,
                        Duration = TimeSpan.FromSeconds(ttxtslidespeed),
                        EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                    };
                    Storyboard.SetTarget(translateAnimation, TimerH1Text);
                    Storyboard.SetTargetProperty(translateAnimation, new PropertyPath("(UIElement.RenderTransform).(TranslateTransform.Y)"));
                    var storyboard = new Storyboard();
                    storyboard.Children.Add(translateAnimation);
                    storyboard.Begin();
                }
            }
            ActualTimerH1 = text;
        }
        private async Task UpdateTimerTextH2(string text, bool reset)
        {
            string direction = "none";
            if ((text == "0" && ActualTimerH2 == "9") && !reset)
            {
                direction = "up";
            }
            else if ((text == "9" && ActualTimerH2 == "0") && !reset)
            {
                direction = "down";
            }
            else if ((text == "0" && ActualTimerH2 == "3") && !reset)
            {
                direction = "up";
            }
            else if ((text == "3" && ActualTimerH2 == "0") && !reset)
            {
                direction = "down";
            }
            else if ((text != ActualTimerH2) && reset)
            {
                direction = "up";
            }
            else if (int.Parse(text) > int.Parse(ActualTimerH2))
            {
                direction = "up";
            }
            else if (int.Parse(text) < int.Parse(ActualTimerH2))
            {
                direction = "down";
            }
            else if (text == ActualTimerH2)
            {
                direction = "none";
            }

            if (direction == "up")
            {
                {
                    var translateAnimation = new DoubleAnimation
                    {
                        From = 0,
                        To = -50,
                        Duration = TimeSpan.FromSeconds(ttxtslidespeed),
                        EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn }
                    };
                    Storyboard.SetTarget(translateAnimation, TimerH2Text);
                    Storyboard.SetTargetProperty(translateAnimation, new PropertyPath("(UIElement.RenderTransform).(TranslateTransform.Y)"));
                    var storyboard = new Storyboard();
                    storyboard.Children.Add(translateAnimation);
                    storyboard.Begin();
                }
                await Task.Delay(ttxtdelay);
                TimerH2Text.Text = text;
                {
                    var translateAnimation = new DoubleAnimation
                    {
                        From = 50,
                        To = 0,
                        Duration = TimeSpan.FromSeconds(ttxtslidespeed),
                        EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                    };
                    Storyboard.SetTarget(translateAnimation, TimerH2Text);
                    Storyboard.SetTargetProperty(translateAnimation, new PropertyPath("(UIElement.RenderTransform).(TranslateTransform.Y)"));
                    var storyboard = new Storyboard();
                    storyboard.Children.Add(translateAnimation);
                    storyboard.Begin();
                }
            }
            else if (direction == "down")
            {
                {
                    var translateAnimation = new DoubleAnimation
                    {
                        From = 0,
                        To = 50,
                        Duration = TimeSpan.FromSeconds(ttxtslidespeed),
                        EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn }
                    };
                    Storyboard.SetTarget(translateAnimation, TimerH2Text);
                    Storyboard.SetTargetProperty(translateAnimation, new PropertyPath("(UIElement.RenderTransform).(TranslateTransform.Y)"));
                    var storyboard = new Storyboard();
                    storyboard.Children.Add(translateAnimation);
                    storyboard.Begin();
                }
                await Task.Delay(ttxtdelay);
                TimerH2Text.Text = text;
                {
                    var translateAnimation = new DoubleAnimation
                    {
                        From = -50,
                        To = 0,
                        Duration = TimeSpan.FromSeconds(ttxtslidespeed),
                        EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                    };
                    Storyboard.SetTarget(translateAnimation, TimerH2Text);
                    Storyboard.SetTargetProperty(translateAnimation, new PropertyPath("(UIElement.RenderTransform).(TranslateTransform.Y)"));
                    var storyboard = new Storyboard();
                    storyboard.Children.Add(translateAnimation);
                    storyboard.Begin();
                }
            }
            ActualTimerH2 = text;
        }
        private async Task UpdateTimerTextM1(string text, bool reset)
        {
            string direction = "none";
            if ((text == "0" && ActualTimerM1 == "5") && !reset)
            {
                direction = "up";
            }
            else if ((text == "5" && ActualTimerM1 == "0") && !reset)
            {
                direction = "down";
            }
            else if (((text != ActualTimerM1) && reset) && reset)
            {
                direction = "up";
            }
            else if (int.Parse(text) > int.Parse(ActualTimerM1))
            {
                direction = "up";
            }
            else if (int.Parse(text) < int.Parse(ActualTimerM1))
            {
                direction = "down";
            }
            else if (text == ActualTimerM1)
            {
                direction = "none";
            }

            if (direction == "up")
            {
                {
                    var translateAnimation = new DoubleAnimation
                    {
                        From = 0,
                        To = -50,
                        Duration = TimeSpan.FromSeconds(ttxtslidespeed),
                        EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn }
                    };
                    Storyboard.SetTarget(translateAnimation, TimerM1Text);
                    Storyboard.SetTargetProperty(translateAnimation, new PropertyPath("(UIElement.RenderTransform).(TranslateTransform.Y)"));
                    var storyboard = new Storyboard();
                    storyboard.Children.Add(translateAnimation);
                    storyboard.Begin();
                }
                await Task.Delay(ttxtdelay);
                TimerM1Text.Text = text;
                {
                    var translateAnimation = new DoubleAnimation
                    {
                        From = 50,
                        To = 0,
                        Duration = TimeSpan.FromSeconds(ttxtslidespeed),
                        EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                    };
                    Storyboard.SetTarget(translateAnimation, TimerM1Text);
                    Storyboard.SetTargetProperty(translateAnimation, new PropertyPath("(UIElement.RenderTransform).(TranslateTransform.Y)"));
                    var storyboard = new Storyboard();
                    storyboard.Children.Add(translateAnimation);
                    storyboard.Begin();
                }
            }
            else if (direction == "down")
            {
                {
                    var translateAnimation = new DoubleAnimation
                    {
                        From = 0,
                        To = 50,
                        Duration = TimeSpan.FromSeconds(ttxtslidespeed),
                        EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn }
                    };
                    Storyboard.SetTarget(translateAnimation, TimerM1Text);
                    Storyboard.SetTargetProperty(translateAnimation, new PropertyPath("(UIElement.RenderTransform).(TranslateTransform.Y)"));
                    var storyboard = new Storyboard();
                    storyboard.Children.Add(translateAnimation);
                    storyboard.Begin();
                }
                await Task.Delay(ttxtdelay);
                TimerM1Text.Text = text;
                {
                    var translateAnimation = new DoubleAnimation
                    {
                        From = -50,
                        To = 0,
                        Duration = TimeSpan.FromSeconds(ttxtslidespeed),
                        EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                    };
                    Storyboard.SetTarget(translateAnimation, TimerM1Text);
                    Storyboard.SetTargetProperty(translateAnimation, new PropertyPath("(UIElement.RenderTransform).(TranslateTransform.Y)"));
                    var storyboard = new Storyboard();
                    storyboard.Children.Add(translateAnimation);
                    storyboard.Begin();
                }
            }
            ActualTimerM1 = text;
        }
        private async Task UpdateTimerTextM2(string text, bool reset)
        {
            string direction = "none";
            if ((text == "0" && ActualTimerM2 == "9") && !reset)
            {
                direction = "up";
            }
            else if ((text == "9" && ActualTimerM2 == "0") && !reset)
            {
                direction = "down";
            }
            else if (((text != ActualTimerM2) && reset) && reset)
            {
                direction = "up";
            }
            else if (int.Parse(text) > int.Parse(ActualTimerM2))
            {
                direction = "up";
            }
            else if (int.Parse(text) < int.Parse(ActualTimerM2))
            {
                direction = "down";
            }
            else if (text == ActualTimerM2)
            {
                direction = "none";
            }

            if (direction == "up")
            {
                {
                    var translateAnimation = new DoubleAnimation
                    {
                        From = 0,
                        To = -50,
                        Duration = TimeSpan.FromSeconds(ttxtslidespeed),
                        EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn }
                    };
                    Storyboard.SetTarget(translateAnimation, TimerM2Text);
                    Storyboard.SetTargetProperty(translateAnimation, new PropertyPath("(UIElement.RenderTransform).(TranslateTransform.Y)"));
                    var storyboard = new Storyboard();
                    storyboard.Children.Add(translateAnimation);
                    storyboard.Begin();
                }
                await Task.Delay(ttxtdelay);
                TimerM2Text.Text = text;
                {
                    var translateAnimation = new DoubleAnimation
                    {
                        From = 50,
                        To = 0,
                        Duration = TimeSpan.FromSeconds(ttxtslidespeed),
                        EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                    };
                    Storyboard.SetTarget(translateAnimation, TimerM2Text);
                    Storyboard.SetTargetProperty(translateAnimation, new PropertyPath("(UIElement.RenderTransform).(TranslateTransform.Y)"));
                    var storyboard = new Storyboard();
                    storyboard.Children.Add(translateAnimation);
                    storyboard.Begin();
                }
            }
            else if (direction == "down")
            {
                {
                    var translateAnimation = new DoubleAnimation
                    {
                        From = 0,
                        To = 50,
                        Duration = TimeSpan.FromSeconds(ttxtslidespeed),
                        EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn }
                    };
                    Storyboard.SetTarget(translateAnimation, TimerM2Text);
                    Storyboard.SetTargetProperty(translateAnimation, new PropertyPath("(UIElement.RenderTransform).(TranslateTransform.Y)"));
                    var storyboard = new Storyboard();
                    storyboard.Children.Add(translateAnimation);
                    storyboard.Begin();
                }
                await Task.Delay(ttxtdelay);
                TimerM2Text.Text = text;
                {
                    var translateAnimation = new DoubleAnimation
                    {
                        From = -50,
                        To = 0,
                        Duration = TimeSpan.FromSeconds(ttxtslidespeed),
                        EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                    };
                    Storyboard.SetTarget(translateAnimation, TimerM2Text);
                    Storyboard.SetTargetProperty(translateAnimation, new PropertyPath("(UIElement.RenderTransform).(TranslateTransform.Y)"));
                    var storyboard = new Storyboard();
                    storyboard.Children.Add(translateAnimation);
                    storyboard.Begin();
                }
            }
            ActualTimerM2 = text;
        }
        private async Task UpdateTimerTextS1(string text, bool reset)
        {
            string direction = "none";
            if ((text == "0" && ActualTimerS1 == "5") && !reset)
            {
                direction = "up";
            }
            else if ((text == "5" && ActualTimerS1 == "0") && !reset)
            {
                direction = "down";
            }
            else if (((text != ActualTimerS1) && reset) && reset)
            {
                direction = "up";
            }
            else if (int.Parse(text) > int.Parse(ActualTimerS1))
            {
                direction = "up";
            }
            else if (int.Parse(text) < int.Parse(ActualTimerS1))
            {
                direction = "down";
            }
            else if (text == ActualTimerS1)
            {
                direction = "none";
            }

            if (direction == "up")
            {
                {
                    var translateAnimation = new DoubleAnimation
                    {
                        From = 0,
                        To = -50,
                        Duration = TimeSpan.FromSeconds(ttxtslidespeed),
                        EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn }
                    };
                    Storyboard.SetTarget(translateAnimation, TimerS1Text);
                    Storyboard.SetTargetProperty(translateAnimation, new PropertyPath("(UIElement.RenderTransform).(TranslateTransform.Y)"));
                    var storyboard = new Storyboard();
                    storyboard.Children.Add(translateAnimation);
                    storyboard.Begin();
                }
                await Task.Delay(ttxtdelay);
                TimerS1Text.Text = text;
                {
                    var translateAnimation = new DoubleAnimation
                    {
                        From = 50,
                        To = 0,
                        Duration = TimeSpan.FromSeconds(ttxtslidespeed),
                        EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                    };
                    Storyboard.SetTarget(translateAnimation, TimerS1Text);
                    Storyboard.SetTargetProperty(translateAnimation, new PropertyPath("(UIElement.RenderTransform).(TranslateTransform.Y)"));
                    var storyboard = new Storyboard();
                    storyboard.Children.Add(translateAnimation);
                    storyboard.Begin();
                }
            }
            else if (direction == "down")
            {
                {
                    var translateAnimation = new DoubleAnimation
                    {
                        From = 0,
                        To = 50,
                        Duration = TimeSpan.FromSeconds(ttxtslidespeed),
                        EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn }
                    };
                    Storyboard.SetTarget(translateAnimation, TimerS1Text);
                    Storyboard.SetTargetProperty(translateAnimation, new PropertyPath("(UIElement.RenderTransform).(TranslateTransform.Y)"));
                    var storyboard = new Storyboard();
                    storyboard.Children.Add(translateAnimation);
                    storyboard.Begin();
                }
                await Task.Delay(ttxtdelay);
                TimerS1Text.Text = text;
                {
                    var translateAnimation = new DoubleAnimation
                    {
                        From = -50,
                        To = 0,
                        Duration = TimeSpan.FromSeconds(ttxtslidespeed),
                        EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                    };
                    Storyboard.SetTarget(translateAnimation, TimerS1Text);
                    Storyboard.SetTargetProperty(translateAnimation, new PropertyPath("(UIElement.RenderTransform).(TranslateTransform.Y)"));
                    var storyboard = new Storyboard();
                    storyboard.Children.Add(translateAnimation);
                    storyboard.Begin();
                }
            }
            ActualTimerS1 = text;
        }
        private async Task UpdateTimerTextS2(string text, bool reset)
        {
            string direction = "none";
            if ((text == "0" && ActualTimerS2 == "9") && !reset)
            {
                direction = "up";
            }
            else if ((text == "9" && ActualTimerS2 == "0") && !reset)
            {
                direction = "down";
            }
            else if (((text != ActualTimerS2) && reset) && reset)
            {
                direction = "up";
            }
            else if (int.Parse(text) > int.Parse(ActualTimerS2))
            {
                direction = "up";
            }
            else if (int.Parse(text) < int.Parse(ActualTimerS2))
            {
                direction = "down";
            }
            else if (text == ActualTimerS2)
            {
                direction = "none";
            }

            if (direction == "up")
            {
                {
                    var translateAnimation = new DoubleAnimation
                    {
                        From = 0,
                        To = -50,
                        Duration = TimeSpan.FromSeconds(ttxtslidespeed),
                        EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn }
                    };
                    Storyboard.SetTarget(translateAnimation, TimerS2Text);
                    Storyboard.SetTargetProperty(translateAnimation, new PropertyPath("(UIElement.RenderTransform).(TranslateTransform.Y)"));
                    var storyboard = new Storyboard();
                    storyboard.Children.Add(translateAnimation);
                    storyboard.Begin();
                }
                await Task.Delay(ttxtdelay);
                TimerS2Text.Text = text;
                {
                    var translateAnimation = new DoubleAnimation
                    {
                        From = 50,
                        To = 0,
                        Duration = TimeSpan.FromSeconds(ttxtslidespeed),
                        EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                    };
                    Storyboard.SetTarget(translateAnimation, TimerS2Text);
                    Storyboard.SetTargetProperty(translateAnimation, new PropertyPath("(UIElement.RenderTransform).(TranslateTransform.Y)"));
                    var storyboard = new Storyboard();
                    storyboard.Children.Add(translateAnimation);
                    storyboard.Begin();
                }
            }
            else if (direction == "down")
            {
                {
                    var translateAnimation = new DoubleAnimation
                    {
                        From = 0,
                        To = 50,
                        Duration = TimeSpan.FromSeconds(ttxtslidespeed),
                        EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn }
                    };
                    Storyboard.SetTarget(translateAnimation, TimerS2Text);
                    Storyboard.SetTargetProperty(translateAnimation, new PropertyPath("(UIElement.RenderTransform).(TranslateTransform.Y)"));
                    var storyboard = new Storyboard();
                    storyboard.Children.Add(translateAnimation);
                    storyboard.Begin();
                }
                await Task.Delay(ttxtdelay);
                TimerS2Text.Text = text;
                {
                    var translateAnimation = new DoubleAnimation
                    {
                        From = -50,
                        To = 0,
                        Duration = TimeSpan.FromSeconds(ttxtslidespeed),
                        EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                    };
                    Storyboard.SetTarget(translateAnimation, TimerS2Text);
                    Storyboard.SetTargetProperty(translateAnimation, new PropertyPath("(UIElement.RenderTransform).(TranslateTransform.Y)"));
                    var storyboard = new Storyboard();
                    storyboard.Children.Add(translateAnimation);
                    storyboard.Begin();
                }
            }
            ActualTimerS2 = text;
        }

        // Timer progress ring update with animation
        private async Task UpdateTimerProgressRing(int value)
        {
            var transtion = new DoubleAnimation
            {
                From = TimerProgressRing.Progress,
                To = value,
                Duration = TimeSpan.FromSeconds(0.5),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
            };
            Storyboard.SetTarget(transtion, TimerProgressRing);
            Storyboard.SetTargetProperty(transtion, new PropertyPath(ProgressRing.ProgressProperty));
            var storyboard = new Storyboard();
            storyboard.Children.Add(transtion);
            storyboard.Begin();
        }
        #endregion

        #region Alarm

        // Alarm status
        // - Is the alarm alerting?
        private bool AlarmAlert = false;
        // - Alerting alarm UID
        private string AlarmAlertUID = "";
        // - Alarm sound player instance
        private SoundPlayer AlarmSoundPlayer;
        // - Is Alarm ActionCard deployed ?
        private bool CardDeployed = false;
        // - Temp value for Alarm_Tick synchronization
        private int AlarmTickTempMinute = 0;
        // Tick event
        private async void Alarm_Tick(object sender, EventArgs e)
        {
            DateTime now = DateTime.Now;
            if (now.Minute != AlarmTickTempMinute)
            {
                AlarmTickTempMinute = now.Minute;
                AlarmCardETAUpdate();

                string nowdaynumber = "0";
                string nowday = now.DayOfWeek.ToString();

                switch (nowday)
                {
                    case "Monday":
                        nowdaynumber = "1";
                        break;
                    case "Tuesday":
                        nowdaynumber = "2";
                        break;
                    case "Wednesday":
                        nowdaynumber = "3";
                        break;
                    case "Thursday":
                        nowdaynumber = "4";
                        break;
                    case "Friday":
                        nowdaynumber = "5";
                        break;
                    case "Saturday":
                        nowdaynumber = "6";
                        break;
                    case "Sunday":
                        nowdaynumber = "7";
                        break;
                }

                foreach (CardAction card in AlarmStack.Children)
                {
                    string uid = card.Tag.ToString().Replace("_AlarmCard", "");
                    string time = await ConfigManager.GetAlarm($"{uid}.time");
                    string days = await ConfigManager.GetAlarm($"{uid}.days");
                    string enabled = await ConfigManager.GetAlarm($"{uid}.enabled");
                    if (time == now.ToString("HH:mm") && (days.Contains(nowdaynumber) || days == "0") && enabled.Contains("true"))
                    {
                        AlarmAlertShow(uid);
                    }
                }
            }
        }

        // Alarm alert show
        private async Task AlarmAlertShow(string uid)
        {
            if (!AlarmAlert)
            {
                AlarmAlert = true;
                AlarmAlertUID = uid;
                string name = await ConfigManager.GetAlarm($"{uid}.name");
                if (name == "") name = "Alarm";
                AlarmAlertText.Text = name;
                string time = await ConfigManager.GetAlarm($"{uid}.time");
                AlarmAlertTime.Text = time;
                string sound = await ConfigManager.GetAlarm($"{uid}.sound");
                if (!sound.Contains("none"))
                {
                    if (sound.Contains("default")) sound = ConfigManager.Variables.DefaultAlarmSound;
                    AlarmSoundPlayer = new SoundPlayer(sound);
                    AlarmSoundPlayer.Load();
                    AlarmSoundPlayer.PlayLooping();
                }
                AlarmAlertGrid.Visibility = Visibility.Visible;
                {
                    var fadeAnimation1 = new DoubleAnimation
                    {
                        From = 0,
                        To = 1,
                        Duration = TimeSpan.FromSeconds(tfadespeed),
                        EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn }
                    };
                    var zoomAnimation1 = new DoubleAnimation
                    {
                        From = 1.1,
                        To = 1,
                        Duration = TimeSpan.FromSeconds(tzoomspeed),
                        EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                    };
                    var zoomAnimation2 = new DoubleAnimation
                    {
                        From = 1.1,
                        To = 1,
                        Duration = TimeSpan.FromSeconds(tzoomspeed),
                        EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                    };
                    Storyboard.SetTarget(fadeAnimation1, AlarmAlertGrid);
                    Storyboard.SetTargetProperty(fadeAnimation1, new PropertyPath(OpacityProperty));
                    Storyboard.SetTarget(zoomAnimation1, AlarmAlertGrid);
                    Storyboard.SetTargetProperty(zoomAnimation1, new PropertyPath("(UIElement.RenderTransform).(ScaleTransform.ScaleX)"));
                    Storyboard.SetTarget(zoomAnimation2, AlarmAlertGrid);
                    Storyboard.SetTargetProperty(zoomAnimation2, new PropertyPath("(UIElement.RenderTransform).(ScaleTransform.ScaleY)"));
                    var storyboard = new Storyboard();
                    storyboard.Children.Add(fadeAnimation1);
                    storyboard.Children.Add(zoomAnimation1);
                    storyboard.Children.Add(zoomAnimation2);
                    storyboard.Begin();
                }

                double timeout = ConfigManager.Variables.AlarmTimeoutDelay;
                TimeSpan alerttime = new TimeSpan(0, 0, 0, 0, 0);

                while (AlarmAlert)
                {
                    await Task.Delay(100);
                    alerttime = alerttime.Add(TimeSpan.FromMilliseconds(100));

                    if (alerttime.TotalMinutes >= timeout)
                    {
                        break;
                    }
                }
                {
                    var fadeAnimation1 = new DoubleAnimation
                    {
                        From = 1,
                        To = 0,
                        Duration = TimeSpan.FromSeconds(tfadespeed),
                        EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                    };
                    var zoomAnimation1 = new DoubleAnimation
                    {
                        From = 1,
                        To = 1.1,
                        Duration = TimeSpan.FromSeconds(tzoomspeed),
                        EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn }
                    };
                    var zoomAnimation2 = new DoubleAnimation
                    {
                        From = 1,
                        To = 1.1,
                        Duration = TimeSpan.FromSeconds(tzoomspeed),
                        EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn }
                    };
                    Storyboard.SetTarget(fadeAnimation1, AlarmAlertGrid);
                    Storyboard.SetTargetProperty(fadeAnimation1, new PropertyPath(OpacityProperty));
                    Storyboard.SetTarget(zoomAnimation1, AlarmAlertGrid);
                    Storyboard.SetTargetProperty(zoomAnimation1, new PropertyPath("(UIElement.RenderTransform).(ScaleTransform.ScaleX)"));
                    Storyboard.SetTarget(zoomAnimation2, AlarmAlertGrid);
                    Storyboard.SetTargetProperty(zoomAnimation2, new PropertyPath("(UIElement.RenderTransform).(ScaleTransform.ScaleY)"));
                    var storyboard = new Storyboard();
                    storyboard.Children.Add(fadeAnimation1);
                    storyboard.Children.Add(zoomAnimation1);
                    storyboard.Children.Add(zoomAnimation2);
                    storyboard.Begin();
                }
                if (sound != "none")
                {
                    AlarmSoundPlayer.Stop();
                    AlarmSoundPlayer.Dispose();
                }

                string days = await ConfigManager.GetAlarm($"{uid}.days");
                if (days == "0")
                {
                    FrameworkElement AlarmCard = await TagSearch.FindFrameworkElementwithTag(AlarmStack, $"{uid}_AlarmCard");
                    FrameworkElement AlarmCardToggleSmall = await TagSearch.FindFrameworkElementwithTag(AlarmCard, $"{uid}_AlarmCardToggleSmall");
                    FrameworkElement AlarmCardToggleBig = await TagSearch.FindFrameworkElementwithTag(AlarmCard, $"{uid}_AlarmCardToggleBig");
                    ((ToggleSwitch)AlarmCardToggleBig).IsChecked = false;
                    ((ToggleSwitch)AlarmCardToggleSmall).IsChecked = false;

                    await ConfigManager.SetAlarm($"{uid}.enabled", "false");
                }

                AlarmAlert = false;
                AlarmAlertUID = "";

                await Task.Delay(500);

                AlarmAlertGrid.Visibility = Visibility.Hidden;
            }
        }

        // Update alarm card ETA
        private async Task AlarmCardETAUpdate()
        {
            foreach (CardAction card in AlarmStack.Children)
            {
                string uid = card.Tag.ToString().Replace("_AlarmCard", "");
                FrameworkElement AlarmCardDescText = await TagSearch.FindFrameworkElementwithTag(card, $"{uid}_AlarmCardDescText");
                string days = "";
                string daysArray = await ConfigManager.GetAlarm($"{uid}.days");
                if (daysArray == "0")
                {
                    days = await LangSystem.GetLang("alarm.repeat.once");
                }
                else
                {
                    if (daysArray == "12345")
                    {
                        days = await LangSystem.GetLang("alarm.repeat.workdays");
                    }
                    else if (daysArray == "67")
                    {
                        days = await LangSystem.GetLang("alarm.repeat.weekend");
                    }
                    else if (daysArray == "1234567")
                    {
                        days = await LangSystem.GetLang("alarm.repeat.everyday");
                    }
                    else
                    {
                        foreach (char day in daysArray)
                        {
                            switch (day)
                            {
                                case '1':
                                    days += $"{await LangSystem.GetLang("clock.days.long.monday")}, ";
                                    break;
                                case '2':
                                    days += $"{await LangSystem.GetLang("clock.days.long.tuesday")}, ";
                                    break;
                                case '3':
                                    days += $"{await LangSystem.GetLang("clock.days.long.wednesday")}, ";
                                    break;
                                case '4':
                                    days += $"{await LangSystem.GetLang("clock.days.long.thursday")}, ";
                                    break;
                                case '5':
                                    days += $"{await LangSystem.GetLang("clock.days.long.friday")}, ";
                                    break;
                                case '6':
                                    days += $"{await LangSystem.GetLang("clock.days.long.saturday")}, ";
                                    break;
                                case '7':
                                    days += $"{await LangSystem.GetLang("clock.days.long.sunday")}, ";
                                    break;
                            }
                        }
                        days = days.Remove(days.Length - 2);
                    }
                }
                string remaningtime = await GetETAAlarmString(uid);
                ((System.Windows.Controls.TextBlock)AlarmCardDescText).Text = days + " • " + remaningtime;
            }
        }

        // Recreate saved alarm cards from config file
        private async Task AlarmCardRestore()
        {
            string alarmsJSON = await File.ReadAllTextAsync(ConfigManager.AlarmPath);
            JObject alarms = JObject.Parse(alarmsJSON);
            List<string> alarmsUID = new List<string>();
            foreach (var alarm in alarms)
            {
                alarmsUID.Add(alarm.Key);
            }
            foreach (string uid in alarmsUID)
            {
                await CreateAlarmCard(uid);
            }
            await SortAlarmCards();
        }

        // Get DateTime of Time  with a UID
        private async Task<DateTime> GetAlarmTime(string uid)
        {
            string time = await ConfigManager.GetAlarm($"{uid}.time");
            string[] timeArray = time.Split(':');
            return new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, int.Parse(timeArray[0]), int.Parse(timeArray[1]), 0);
        }

        // Get TimeSpan of ETA with a UID
        private async Task<TimeSpan> GetETAAlarm(string uid)
        {
            string time = await ConfigManager.GetAlarm($"{uid}.time");
            string days = await ConfigManager.GetAlarm($"{uid}.days");

            string[] timeArray = time.Split(":");
            int hours = int.Parse(timeArray[0]);
            int minutes = int.Parse(timeArray[1]);

            DateTime now = DateTime.Now;
            DateTime alarm = new DateTime(now.Year, now.Month, now.Day, hours, minutes, 0);

            if (days == "0")
            {
                if (alarm < now)
                {
                    alarm = alarm.AddDays(-1);
                }
            }
            else
            {
                List<int> alarmDays = days.Select(c => int.Parse(c.ToString())).ToList();

                int today = ((int)now.DayOfWeek == 0) ? 7 : (int)now.DayOfWeek;

                int nextDay = alarmDays
                    .Where(d => (d > today || (d == today && alarm >= now)))
                    .DefaultIfEmpty(alarmDays.Min())
                    .First();

                int daysToAdd = (nextDay >= today) ? nextDay - today : (7 - today + nextDay);

                alarm = alarm.AddDays(daysToAdd);
            }

            return alarm - now;
        }

        // Sort alarm cards by time and ETA/Days
        private async Task SortAlarmCards()
        {
            StackPanel stackPanel = AlarmStack;

            var items = stackPanel.Children.OfType<CardAction>().ToList();

            var cardData = new Dictionary<CardAction, (DateTime time, TimeSpan eta)>();
            foreach (var card in items)
            {
                var tag = card.Tag.ToString().Replace("_AlarmCard", "");
                cardData[card] = (
                    time: await GetAlarmTime(tag),
                    eta: await GetETAAlarm(tag)
                );
            }

            var sortedItems = items.OrderBy(card => cardData[card].time)
                                   .ThenBy(card => cardData[card].eta)
                                   .ToList();

            var tasks = new List<Task>();
            for (int i = 0; i < items.Count; i++)
            {
                var card = items[i];
                var newIndex = sortedItems.IndexOf(card);

                if (newIndex != i)
                {
                    if (double.IsNaN(card.Height)) card.Height = card.ActualHeight;

                    var shrinkAnimation = new DoubleAnimation
                    {
                        To = 0,
                        Duration = TimeSpan.FromMilliseconds(300),
                        EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn }
                    };

                    var tcs = new TaskCompletionSource();
                    shrinkAnimation.Completed += (s, e) => tcs.SetResult();
                    card.BeginAnimation(HeightProperty, shrinkAnimation);
                    tasks.Add(tcs.Task);
                }
            }

            await Task.WhenAll(tasks);
            await Task.Delay(150);

            stackPanel.Children.Clear();
            foreach (var card in sortedItems)
            {
                stackPanel.Children.Add(card);
            }

            tasks.Clear();
            foreach (var card in sortedItems)
            {
                if (double.IsNaN(card.Height)) continue;

                var growAnimation = new DoubleAnimation
                {
                    From = 0,
                    To = 64,
                    Duration = TimeSpan.FromMilliseconds(500),
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                };

                var tcs = new TaskCompletionSource();
                growAnimation.Completed += (s, e) => tcs.SetResult();
                card.BeginAnimation(HeightProperty, growAnimation);
                tasks.Add(tcs.Task);
            }

            await Task.WhenAll(tasks);

            foreach (CardAction card in AlarmStack.Children)
            {
                card.BeginAnimation(HeightProperty, null);
                card.Height = double.NaN;
            }
        }

        // Create alarm card with a UID
        private async Task CreateAlarmCard(string uid)
        {
            CardAction AlarmCard = new CardAction();
            AlarmCard.Tag = $"{uid}_AlarmCard";
            AlarmCard.IsChevronVisible = false;
            AlarmCard.Margin = new Thickness(0, 0, 0, 10);
            AlarmCard.Padding = new Thickness(10);
            AlarmCard.PreviewMouseDown += async (sender, e) =>
            {
                await AlarmCardOpen(sender, e, uid);
            };

            Grid AlarmCardMainGrid = new Grid();
            AlarmCardMainGrid.Tag = $"{uid}_AlarmCardMainGrid";

            Grid AlarmCardSmallGrid = new Grid();
            AlarmCardSmallGrid.Tag = $"{uid}_AlarmCardSmallGrid";
            AlarmCardSmallGrid.Visibility = Visibility.Visible;
            AlarmCardSmallGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Auto) });
            AlarmCardSmallGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Auto) });
            AlarmCardSmallGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Auto) });
            AlarmCardSmallGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            System.Windows.Controls.TextBlock AlarmCardTimeText = new System.Windows.Controls.TextBlock();
            AlarmCardTimeText.Tag = $"{uid}_AlarmCardTimeText";
            AlarmCardTimeText.Text = await ConfigManager.GetAlarm($"{uid}.time");
            AlarmCardTimeText.FontSize = 32;
            AlarmCardTimeText.FontFamily = new FontFamily("Segoe UI Variable Display SemiBold");
            AlarmCardTimeText.Foreground = (Brush)FindResource("TextFillColorPrimaryBrush");
            AlarmCardTimeText.Margin = new Thickness(10, 0, 20, 0);
            AlarmCardTimeText.HorizontalAlignment = HorizontalAlignment.Left;
            AlarmCardTimeText.VerticalAlignment = VerticalAlignment.Center;

            System.Windows.Controls.TextBlock AlarmCardNameText = new System.Windows.Controls.TextBlock();
            AlarmCardNameText.Tag = $"{uid}_AlarmCardNameText";
            AlarmCardNameText.Text = await ConfigManager.GetAlarm($"{uid}.name");
            AlarmCardNameText.FontSize = 18;
            AlarmCardNameText.FontFamily = new FontFamily("Segoe UI SemiBold");
            AlarmCardNameText.Foreground = (Brush)FindResource("TextFillColorPrimaryBrush");
            AlarmCardNameText.SetValue(Grid.ColumnProperty, 1);
            AlarmCardNameText.Margin = new Thickness(0, 0, 10, 0);
            AlarmCardNameText.VerticalAlignment = VerticalAlignment.Center;

            string days = "";
            string daysArray = await ConfigManager.GetAlarm($"{uid}.days");
            if (daysArray == "0")
            {
                days = await LangSystem.GetLang("alarm.repeat.once");
            }
            else
            {
                if (daysArray == "12345")
                {
                    days = await LangSystem.GetLang("alarm.repeat.workdays");
                }
                else if (daysArray == "67")
                {
                    days = await LangSystem.GetLang("alarm.repeat.weekend");
                }
                else if (daysArray == "1234567")
                {
                    days = await LangSystem.GetLang("alarm.repeat.everyday");
                }
                else
                {
                    foreach (char day in daysArray)
                    {
                        switch (day)
                        {
                            case '1':
                                days += $"{await LangSystem.GetLang("clock.days.long.monday")}, ";
                                break;
                            case '2':
                                days += $"{await LangSystem.GetLang("clock.days.long.tuesday")}, ";
                                break;
                            case '3':
                                days += $"{await LangSystem.GetLang("clock.days.long.wednesday")}, ";
                                break;
                            case '4':
                                days += $"{await LangSystem.GetLang("clock.days.long.thursday")}, ";
                                break;
                            case '5':
                                days += $"{await LangSystem.GetLang("clock.days.long.friday")}, ";
                                break;
                            case '6':
                                days += $"{await LangSystem.GetLang("clock.days.long.saturday")}, ";
                                break;
                            case '7':
                                days += $"{await LangSystem.GetLang("clock.days.long.sunday")}, ";
                                break;
                        }
                    }
                    days = days.Remove(days.Length - 2);
                }
            }
            string remaningtime = await GetETAAlarmString(uid);

            System.Windows.Controls.TextBlock AlarmCardDescText = new System.Windows.Controls.TextBlock();
            AlarmCardDescText.Tag = $"{uid}_AlarmCardDescText";
            AlarmCardDescText.Text = days + " • " + remaningtime;
            AlarmCardDescText.FontSize = 14;
            AlarmCardDescText.FontFamily = new FontFamily("Segoe UI");
            AlarmCardDescText.Foreground = (Brush)FindResource("TextFillColorSecondaryBrush");
            AlarmCardDescText.SetValue(Grid.ColumnProperty, 2);
            AlarmCardDescText.Margin = new Thickness(0, 0, 10, 0);
            AlarmCardDescText.VerticalAlignment = VerticalAlignment.Center;

            ToggleSwitch AlarmCardToggleSmall = new ToggleSwitch();
            AlarmCardToggleSmall.Tag = $"{uid}_AlarmCardToggleSmall";
            AlarmCardToggleSmall.HorizontalAlignment = HorizontalAlignment.Right;
            AlarmCardToggleSmall.VerticalAlignment = VerticalAlignment.Center;
            AlarmCardToggleSmall.Margin = new Thickness(0, 0, 10, 0);
            AlarmCardToggleSmall.SetValue(Grid.ColumnProperty, 3);
            if ((await ConfigManager.GetAlarm($"{uid}.enabled")).Contains("true"))
            {
                AlarmCardToggleSmall.IsChecked = true;
            }
            else
            {
                AlarmCardToggleSmall.IsChecked = false;
            }
            AlarmCardToggleSmall.Click += async (sender, e) =>
            {
                await AlarmCardToggle(uid);
            };

            Grid AlarmCardBigGrid = new Grid();
            AlarmCardBigGrid.Tag = $"{uid}_AlarmCardBigGrid";
            AlarmCardBigGrid.Visibility = Visibility.Collapsed;
            AlarmCardBigGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) });
            AlarmCardBigGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            AlarmCardBigGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) });

            Grid AlarmCardBigGridTop = new Grid();
            AlarmCardBigGridTop.Tag = $"{uid}_AlarmCardBigGridTop";
            AlarmCardBigGridTop.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            AlarmCardBigGridTop.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            AlarmCardBigGridTop.SetValue(Grid.RowProperty, 0);

            Wpf.Ui.Controls.TextBox AlarmCardNameTextBox = new Wpf.Ui.Controls.TextBox();
            AlarmCardNameTextBox.Icon = new ImageIcon { Source = new BitmapImage(new Uri("pack://application:,,,/Resources/Icons/rename.png")), Width = 16, Height = 16 };
            AlarmCardNameTextBox.Tag = $"{uid}_AlarmCardNameTextBox";
            AlarmCardNameTextBox.Text = await ConfigManager.GetAlarm($"{uid}.name");
            AlarmCardNameTextBox.SetValue(Grid.ColumnProperty, 0);
            AlarmCardNameTextBox.PlaceholderEnabled = true;
            AlarmCardNameTextBox.PlaceholderText = await LangSystem.GetLang("alarm.name");
            AlarmCardNameTextBox.VerticalAlignment = VerticalAlignment.Center;
            AlarmCardNameTextBox.Margin = new Thickness(0, 0, 10, 0);

            ToggleSwitch AlarmCardToggleBig = new ToggleSwitch();
            AlarmCardToggleBig.Tag = $"{uid}_AlarmCardToggleBig";
            AlarmCardToggleBig.HorizontalAlignment = HorizontalAlignment.Right;
            AlarmCardToggleBig.Margin = new Thickness(0, 0, 10, 0);
            AlarmCardToggleBig.SetValue(Grid.ColumnProperty, 1);
            if ((await ConfigManager.GetAlarm($"{uid}.enabled")).Contains("true"))
            {
                AlarmCardToggleBig.IsChecked = true;
            }
            else
            {
                AlarmCardToggleBig.IsChecked = false;
            }

            Grid AlarmCardBigGridMiddle = new Grid();
            AlarmCardBigGridMiddle.Tag = $"{uid}_AlarmCardBigGridMiddle";
            AlarmCardBigGridMiddle.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(0.4, GridUnitType.Star) });
            AlarmCardBigGridMiddle.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(0.6, GridUnitType.Star) });
            AlarmCardBigGridMiddle.SetValue(Grid.RowProperty, 1);

            Grid AlarmCardBigGridMiddleLeft = new Grid();
            AlarmCardBigGridMiddleLeft.Tag = $"{uid}_AlarmCardBigGridMiddleLeft";
            AlarmCardBigGridMiddleLeft.SetValue(Grid.ColumnProperty, 0);

            Border AlarmCardEditBorder = new Border();
            AlarmCardEditBorder.Tag = $"{uid}_AlarmCardEditBorder";
            AlarmCardEditBorder.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#0affffff"));
            AlarmCardEditBorder.CornerRadius = new CornerRadius(8);
            AlarmCardEditBorder.Padding = new Thickness(10);
            AlarmCardEditBorder.VerticalAlignment = VerticalAlignment.Center;
            AlarmCardEditBorder.HorizontalAlignment = HorizontalAlignment.Center;

            Grid AlarmCardEditGrid = new Grid();
            AlarmCardEditGrid.Tag = $"{uid}_AlarmCardEditGrid";
            AlarmCardEditGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            AlarmCardEditGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Auto) });
            AlarmCardEditGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            AlarmCardEditGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) });
            AlarmCardEditGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            AlarmCardEditGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) });

            Wpf.Ui.Controls.Button AlarmCardEditHourUpBtn = new Wpf.Ui.Controls.Button();
            AlarmCardEditHourUpBtn.Tag = $"{uid}_AlarmCardEditHourUpBtn";
            AlarmCardEditHourUpBtn.SetValue(Grid.ColumnProperty, 0);
            AlarmCardEditHourUpBtn.SetValue(Grid.RowProperty, 0);
            AlarmCardEditHourUpBtn.Icon = new ImageIcon { Source = new BitmapImage(new Uri("pack://application:,,,/Resources/Icons/up.png")), Width = 16, Height = 16 };
            AlarmCardEditHourUpBtn.HorizontalAlignment = HorizontalAlignment.Stretch;
            AlarmCardEditHourUpBtn.Appearance = ControlAppearance.Secondary;
            AlarmCardEditHourUpBtn.Click += async (sender, e) =>
            {
                await AlarmCardEditHour(uid, "up");
            };

            Wpf.Ui.Controls.Button AlarmCardEditHourDownBtn = new Wpf.Ui.Controls.Button();
            AlarmCardEditHourDownBtn.Tag = $"{uid}_AlarmCardEditHourDownBtn";
            AlarmCardEditHourDownBtn.SetValue(Grid.ColumnProperty, 0);
            AlarmCardEditHourDownBtn.SetValue(Grid.RowProperty, 2);
            AlarmCardEditHourDownBtn.Icon = new ImageIcon { Source = new BitmapImage(new Uri("pack://application:,,,/Resources/Icons/down.png")), Width = 16, Height = 16 };
            AlarmCardEditHourDownBtn.HorizontalAlignment = HorizontalAlignment.Stretch;
            AlarmCardEditHourDownBtn.Appearance = ControlAppearance.Secondary;
            AlarmCardEditHourDownBtn.Click += async (sender, e) =>
            {
                await AlarmCardEditHour(uid, "down");
            };

            Wpf.Ui.Controls.Button AlarmCardEditMinuteUpBtn = new Wpf.Ui.Controls.Button();
            AlarmCardEditMinuteUpBtn.Tag = $"{uid}_AlarmCardEditMinuteUpBtn";
            AlarmCardEditMinuteUpBtn.SetValue(Grid.ColumnProperty, 2);
            AlarmCardEditMinuteUpBtn.SetValue(Grid.RowProperty, 0);
            AlarmCardEditMinuteUpBtn.Icon = new ImageIcon { Source = new BitmapImage(new Uri("pack://application:,,,/Resources/Icons/up.png")), Width = 16, Height = 16 };
            AlarmCardEditMinuteUpBtn.HorizontalAlignment = HorizontalAlignment.Stretch;
            AlarmCardEditMinuteUpBtn.Appearance = ControlAppearance.Secondary;
            AlarmCardEditMinuteUpBtn.Click += async (sender, e) =>
            {
                await AlarmCardEditMinute(uid, "up");
            };

            Wpf.Ui.Controls.Button AlarmCardEditMinuteDownBtn = new Wpf.Ui.Controls.Button();
            AlarmCardEditMinuteDownBtn.Tag = $"{uid}_AlarmCardEditMinuteDownBtn";
            AlarmCardEditMinuteDownBtn.SetValue(Grid.ColumnProperty, 2);
            AlarmCardEditMinuteDownBtn.SetValue(Grid.RowProperty, 2);
            AlarmCardEditMinuteDownBtn.Icon = new ImageIcon { Source = new BitmapImage(new Uri("pack://application:,,,/Resources/Icons/down.png")), Width = 16, Height = 16 };
            AlarmCardEditMinuteDownBtn.HorizontalAlignment = HorizontalAlignment.Stretch;
            AlarmCardEditMinuteDownBtn.Appearance = ControlAppearance.Secondary;
            AlarmCardEditMinuteDownBtn.Click += async (sender, e) =>
            {
                await AlarmCardEditMinute(uid, "down");
            };

            Grid AlarmCardEditHourGrid = new Grid();
            AlarmCardEditHourGrid.Tag = $"{uid}_AlarmCardEditHourGrid";
            AlarmCardEditHourGrid.SetValue(Grid.ColumnProperty, 0);
            AlarmCardEditHourGrid.SetValue(Grid.RowProperty, 1);
            AlarmCardEditHourGrid.VerticalAlignment = VerticalAlignment.Center;
            AlarmCardEditHourGrid.HorizontalAlignment = HorizontalAlignment.Center;
            AlarmCardEditHourGrid.ClipToBounds = true;
            AlarmCardEditHourGrid.Height = 50;
            AlarmCardEditHourGrid.Width = 50;

            System.Windows.Controls.TextBlock AlarmCardEditHourText = new System.Windows.Controls.TextBlock();
            AlarmCardEditHourText.Tag = $"{uid}_AlarmCardEditHourText";
            AlarmCardEditHourText.Text = (await ConfigManager.GetAlarm($"{uid}.time")).Split(":")[0];
            AlarmCardEditHourText.FontSize = 48;
            AlarmCardEditHourText.LineHeight = 50;
            AlarmCardEditHourText.LineStackingStrategy = LineStackingStrategy.BlockLineHeight;
            AlarmCardEditHourText.FontFamily = new FontFamily("Segoe UI Variable Display SemiBold");
            AlarmCardEditHourText.Foreground = (Brush)FindResource("TextFillColorPrimaryBrush");
            AlarmCardEditHourText.HorizontalAlignment = HorizontalAlignment.Center;
            AlarmCardEditHourText.VerticalAlignment = VerticalAlignment.Center;
            AlarmCardEditHourText.RenderTransformOrigin = new Point(0.5, 0.5);
            AlarmCardEditHourText.RenderTransform = new TranslateTransform(0, 0);

            Grid AlarmCardEditMinuteGrid = new Grid();
            AlarmCardEditMinuteGrid.Tag = $"{uid}_AlarmCardEditMinuteGrid";
            AlarmCardEditMinuteGrid.SetValue(Grid.ColumnProperty, 2);
            AlarmCardEditMinuteGrid.SetValue(Grid.RowProperty, 1);
            AlarmCardEditMinuteGrid.VerticalAlignment = VerticalAlignment.Center;
            AlarmCardEditMinuteGrid.HorizontalAlignment = HorizontalAlignment.Center;
            AlarmCardEditMinuteGrid.ClipToBounds = true;
            AlarmCardEditMinuteGrid.Height = 50;
            AlarmCardEditMinuteGrid.Width = 50;

            System.Windows.Controls.TextBlock AlarmCardEditMinuteText = new System.Windows.Controls.TextBlock();
            AlarmCardEditMinuteText.Tag = $"{uid}_AlarmCardEditMinuteText";
            AlarmCardEditMinuteText.Text = (await ConfigManager.GetAlarm($"{uid}.time")).Split(":")[1];
            AlarmCardEditMinuteText.FontSize = 48;
            AlarmCardEditMinuteText.LineHeight = 50;
            AlarmCardEditMinuteText.LineStackingStrategy = LineStackingStrategy.BlockLineHeight;
            AlarmCardEditMinuteText.FontFamily = new FontFamily("Segoe UI Variable Display SemiBold");
            AlarmCardEditMinuteText.Foreground = (Brush)FindResource("TextFillColorPrimaryBrush");
            AlarmCardEditMinuteText.HorizontalAlignment = HorizontalAlignment.Center;
            AlarmCardEditMinuteText.VerticalAlignment = VerticalAlignment.Center;
            AlarmCardEditMinuteText.RenderTransformOrigin = new Point(0.5, 0.5);
            AlarmCardEditMinuteText.RenderTransform = new TranslateTransform(0, 0);

            System.Windows.Controls.TextBlock AlarmCardEditColonText = new System.Windows.Controls.TextBlock();
            AlarmCardEditColonText.Tag = $"{uid}_AlarmCardEditColonText";
            AlarmCardEditColonText.Text = ":";
            AlarmCardEditColonText.FontSize = 48;
            AlarmCardEditColonText.LineHeight = 50;
            AlarmCardEditColonText.LineStackingStrategy = LineStackingStrategy.BlockLineHeight;
            AlarmCardEditColonText.FontFamily = new FontFamily("Segoe UI Variable Display SemiBold");
            AlarmCardEditColonText.Foreground = (Brush)FindResource("TextFillColorPrimaryBrush");
            AlarmCardEditColonText.HorizontalAlignment = HorizontalAlignment.Center;
            AlarmCardEditColonText.VerticalAlignment = VerticalAlignment.Center;
            AlarmCardEditColonText.SetValue(Grid.ColumnProperty, 1);
            AlarmCardEditColonText.SetValue(Grid.RowProperty, 1);
            AlarmCardEditColonText.Margin = new Thickness(3);

            Grid AlarmCardBigGridMiddleRight = new Grid();
            AlarmCardBigGridMiddleRight.Tag = $"{uid}_AlarmCardBigGridMiddleRight";
            AlarmCardBigGridMiddleRight.SetValue(Grid.ColumnProperty, 1);
            AlarmCardBigGridMiddleRight.VerticalAlignment = VerticalAlignment.Center;
            AlarmCardBigGridMiddleRight.HorizontalAlignment = HorizontalAlignment.Center;


            StackPanel AlarmCardBigGridMiddleRightStack = new StackPanel();
            AlarmCardBigGridMiddleRightStack.Tag = $"{uid}_AlarmCardBigGridMiddleRightStack";
            AlarmCardBigGridMiddleRightStack.Orientation = Orientation.Vertical;

            StackPanel AlarmCardDaysStack = new StackPanel();
            AlarmCardDaysStack.Tag = $"{uid}_AlarmCardDaysStack";
            AlarmCardDaysStack.Margin = new Thickness(0, 0, 0, 10);
            AlarmCardDaysStack.Orientation = Orientation.Horizontal;

            string daysstr = await ConfigManager.GetAlarm($"{uid}.days");
            ToggleButton AlarmCardDayOnce = new ToggleButton();
            AlarmCardDayOnce.Tag = $"{uid}_AlarmCardDayOnce";
            AlarmCardDayOnce.Content = "1";
            AlarmCardDayOnce.Margin = new Thickness(0, 0, 5, 0);
            AlarmCardDayOnce.Foreground = (Brush)FindResource("TextFillColorPrimaryBrush");
            AlarmCardDayOnce.FontFamily = new FontFamily("Segoe UI Variable Display Regular");
            if (daysstr.Contains("0"))
            {
                AlarmCardDayOnce.IsChecked = true;
            }
            AlarmCardDayOnce.Click += async (sender, e) =>
            {
                await AlarmCardDaysToggleButton(uid, "once");
            };

            ToggleButton AlarmCardDayMonday = new ToggleButton();
            AlarmCardDayMonday.Tag = $"{uid}_AlarmCardDayMonday";
            AlarmCardDayMonday.Content = await LangSystem.GetLang("clock.days.abbr.monday");
            AlarmCardDayMonday.Margin = new Thickness(0, 0, 3, 0);
            AlarmCardDayMonday.Foreground = (Brush)FindResource("TextFillColorPrimaryBrush");
            AlarmCardDayMonday.FontFamily = new FontFamily("Segoe UI Variable Display Regular");
            if (daysstr.Contains("1"))
            {
                AlarmCardDayMonday.IsChecked = true;
            }
            AlarmCardDayMonday.Click += async (sender, e) =>
            {
                await AlarmCardDaysToggleButton(uid, "days");
            };

            ToggleButton AlarmCardDayTuesday = new ToggleButton();
            AlarmCardDayTuesday.Tag = $"{uid}_AlarmCardDayTuesday";
            AlarmCardDayTuesday.Content = await LangSystem.GetLang("clock.days.abbr.tuesday");
            AlarmCardDayTuesday.Margin = new Thickness(0, 0, 3, 0);
            AlarmCardDayTuesday.Foreground = (Brush)FindResource("TextFillColorPrimaryBrush");
            AlarmCardDayTuesday.FontFamily = new FontFamily("Segoe UI Variable Display Regular");
            if (daysstr.Contains("2"))
            {
                AlarmCardDayTuesday.IsChecked = true;
            }
            AlarmCardDayTuesday.Click += async (sender, e) =>
            {
                await AlarmCardDaysToggleButton(uid, "days");
            };

            ToggleButton AlarmCardDayWednesday = new ToggleButton();
            AlarmCardDayWednesday.Tag = $"{uid}_AlarmCardDayWednesday";
            AlarmCardDayWednesday.Content = await LangSystem.GetLang("clock.days.abbr.wednesday");
            AlarmCardDayWednesday.Margin = new Thickness(0, 0, 3, 0);
            AlarmCardDayWednesday.Foreground = (Brush)FindResource("TextFillColorPrimaryBrush");
            AlarmCardDayWednesday.FontFamily = new FontFamily("Segoe UI Variable Display Regular");
            if (daysstr.Contains("3"))
            {
                AlarmCardDayWednesday.IsChecked = true;
            }
            AlarmCardDayWednesday.Click += async (sender, e) =>
            {
                await AlarmCardDaysToggleButton(uid, "days");
            };

            ToggleButton AlarmCardDayThursday = new ToggleButton();
            AlarmCardDayThursday.Tag = $"{uid}_AlarmCardDayThursday";
            AlarmCardDayThursday.Content = await LangSystem.GetLang("clock.days.abbr.thursday");
            AlarmCardDayThursday.Margin = new Thickness(0, 0, 3, 0);
            AlarmCardDayThursday.Foreground = (Brush)FindResource("TextFillColorPrimaryBrush");
            AlarmCardDayThursday.FontFamily = new FontFamily("Segoe UI Variable Display Regular");
            if (daysstr.Contains("4"))
            {
                AlarmCardDayThursday.IsChecked = true;
            }
            AlarmCardDayThursday.Click += async (sender, e) =>
            {
                await AlarmCardDaysToggleButton(uid, "days");
            };

            ToggleButton AlarmCardDayFriday = new ToggleButton();
            AlarmCardDayFriday.Tag = $"{uid}_AlarmCardDayFriday";
            AlarmCardDayFriday.Content = await LangSystem.GetLang("clock.days.abbr.friday");
            AlarmCardDayFriday.Margin = new Thickness(0, 0, 3, 0);
            AlarmCardDayFriday.Foreground = (Brush)FindResource("TextFillColorPrimaryBrush");
            AlarmCardDayFriday.FontFamily = new FontFamily("Segoe UI Variable Display Regular");
            if (daysstr.Contains("5"))
            {
                AlarmCardDayFriday.IsChecked = true;
            }
            AlarmCardDayFriday.Click += async (sender, e) =>
            {
                await AlarmCardDaysToggleButton(uid, "days");
            };

            ToggleButton AlarmCardDaySaturday = new ToggleButton();
            AlarmCardDaySaturday.Tag = $"{uid}_AlarmCardDaySaturday";
            AlarmCardDaySaturday.Content = await LangSystem.GetLang("clock.days.abbr.saturday");
            AlarmCardDaySaturday.Margin = new Thickness(0, 0, 3, 0);
            AlarmCardDaySaturday.Foreground = (Brush)FindResource("TextFillColorPrimaryBrush");
            AlarmCardDaySaturday.FontFamily = new FontFamily("Segoe UI Variable Display Regular");
            if (daysstr.Contains("6"))
            {
                AlarmCardDaySaturday.IsChecked = true;
            }
            AlarmCardDaySaturday.Click += async (sender, e) =>
            {
                await AlarmCardDaysToggleButton(uid, "days");
            };

            ToggleButton AlarmCardDaySunday = new ToggleButton();
            AlarmCardDaySunday.Tag = $"{uid}_AlarmCardDaySunday";
            AlarmCardDaySunday.Content = await LangSystem.GetLang("clock.days.abbr.sunday");
            AlarmCardDaySunday.Margin = new Thickness(0, 0, 3, 0);
            AlarmCardDaySunday.Foreground = (Brush)FindResource("TextFillColorPrimaryBrush");
            AlarmCardDaySunday.FontFamily = new FontFamily("Segoe UI Variable Display Regular");
            if (daysstr.Contains("7"))
            {
                AlarmCardDaySunday.IsChecked = true;
            }
            AlarmCardDaySunday.Click += async (sender, e) =>
            {
                await AlarmCardDaysToggleButton(uid, "days");
            };

            Grid AlarmCardSoundGrid = new Grid();
            AlarmCardSoundGrid.Tag = $"{uid}_AlarmCardSoundGrid";
            AlarmCardSoundGrid.Margin = new Thickness(0, 0, 0, 10);
            AlarmCardSoundGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Auto) });
            AlarmCardSoundGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            ToggleSwitch AlarmCardSoundToggle = new ToggleSwitch();
            AlarmCardSoundToggle.Tag = $"{uid}_AlarmCardSoundToggle";
            AlarmCardSoundToggle.HorizontalAlignment = HorizontalAlignment.Center;
            AlarmCardSoundToggle.VerticalAlignment = VerticalAlignment.Center;
            AlarmCardSoundToggle.Margin = new Thickness(0, 0, 10, 0);
            AlarmCardSoundToggle.SetValue(Grid.ColumnProperty, 0);
            if ((await ConfigManager.GetAlarm($"{uid}.sound")).Contains("none"))
            {
                AlarmCardSoundToggle.IsChecked = false;
            }
            else
            {
                AlarmCardSoundToggle.IsChecked = true;
            }
            AlarmCardSoundToggle.Click += async (sender, e) =>
            {
                await AlarmCardSoundToggleClick(uid);
            };

            ComboBox AlarmCardSoundComboBox = new ComboBox();
            AlarmCardSoundComboBox.Tag = $"{uid}_AlarmCardSoundComboBox";
            AlarmCardSoundComboBox.SetValue(Grid.ColumnProperty, 1);
            AlarmCardSoundComboBox.Width = 234;
            if (AlarmCardSoundToggle.IsChecked == false)
            {
                AlarmCardSoundComboBox.IsEnabled = false;
            }
            else
            {
                AlarmCardSoundComboBox.IsEnabled = true;
            }
            AlarmCardSoundComboBox.DropDownOpened += async (sender, e) =>
            {
                await AlarmCardSoundComboBoxOpen(uid);
            };
            AlarmCardSoundComboBox.DropDownClosed += async (sender, e) =>
            {
                await AlarmCardSoundComboBoxClose(uid);
            };


            ComboBoxItem AlarmCardSoundComboBoxItemDefault = new ComboBoxItem();
            AlarmCardSoundComboBoxItemDefault.Tag = $"{uid}_AlarmCardSoundComboBoxItemDefault";
            AlarmCardSoundComboBoxItemDefault.Content = await LangSystem.GetLang("alarm.defsounds");
            if (!(await ConfigManager.GetAlarm($"{uid}.sound")).Contains("default"))
            {
                AlarmCardSoundComboBoxItemDefault.IsSelected = false;
            }
            else
            {
                AlarmCardSoundComboBoxItemDefault.IsSelected = true;
            }

            ComboBoxItem AlarmCardSoundComboBoxItemCustom = new ComboBoxItem();
            AlarmCardSoundComboBoxItemCustom.Tag = $"{uid}_AlarmCardSoundComboBoxItemCustom";
            AlarmCardSoundComboBoxItemCustom.Content = await LangSystem.GetLang("alarm.custsounds");
            if (AlarmCardSoundComboBox.IsEnabled && !AlarmCardSoundComboBoxItemDefault.IsSelected)
            {
                AlarmCardSoundComboBoxItemCustom.IsSelected = true;
                AlarmCardSoundComboBoxItemCustom.Content = await ConfigManager.GetAlarm($"{uid}.sound");
            }
            else
            {
                AlarmCardSoundComboBoxItemCustom.IsSelected = false;
            }

            Grid AlarmCardBigGridBottom = new Grid();
            AlarmCardBigGridBottom.Tag = $"{uid}_AlarmCardBigGridBottom";
            AlarmCardBigGridBottom.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            AlarmCardBigGridBottom.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            AlarmCardBigGridBottom.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            AlarmCardBigGridBottom.SetValue(Grid.RowProperty, 2);

            Wpf.Ui.Controls.Button AlarmCardBigGridBottomBtnDelete = new Wpf.Ui.Controls.Button();
            AlarmCardBigGridBottomBtnDelete.Tag = $"{uid}_AlarmCardBigGridBottomBtnDelete";
            AlarmCardBigGridBottomBtnDelete.Content = await LangSystem.GetLang("alarm.delete");
            AlarmCardBigGridBottomBtnDelete.Foreground = (Brush)FindResource("TextFillColorPrimaryBrush");
            AlarmCardBigGridBottomBtnDelete.HorizontalAlignment = HorizontalAlignment.Stretch;
            AlarmCardBigGridBottomBtnDelete.Appearance = ControlAppearance.Secondary;
            AlarmCardBigGridBottomBtnDelete.SetValue(Grid.ColumnProperty, 0);
            AlarmCardBigGridBottomBtnDelete.Icon = new ImageIcon { Source = new BitmapImage(new Uri("pack://application:,,,/Resources/Icons/delete.png")), Width = 16, Height = 16 };
            AlarmCardBigGridBottomBtnDelete.Click += async (sender, e) =>
            {
                await AlarmCardClose(uid);
                await AlarmCardDelete(uid);
            };

            Wpf.Ui.Controls.Button AlarmCardBigGridBottomBtnCancel = new Wpf.Ui.Controls.Button();
            AlarmCardBigGridBottomBtnCancel.Tag = $"{uid}_AlarmCardBigGridBottomBtnCancel";
            AlarmCardBigGridBottomBtnCancel.Content = await LangSystem.GetLang("alarm.cancel");
            AlarmCardBigGridBottomBtnCancel.Foreground = (Brush)FindResource("TextFillColorPrimaryBrush");
            AlarmCardBigGridBottomBtnCancel.HorizontalAlignment = HorizontalAlignment.Stretch;
            AlarmCardBigGridBottomBtnCancel.Appearance = ControlAppearance.Secondary;
            AlarmCardBigGridBottomBtnCancel.SetValue(Grid.ColumnProperty, 1);
            AlarmCardBigGridBottomBtnCancel.Margin = new Thickness(10, 0, 10, 0);
            AlarmCardBigGridBottomBtnCancel.Icon = new ImageIcon { Source = new BitmapImage(new Uri("pack://application:,,,/Resources/Icons/cross.png")), Width = 16, Height = 16 };
            AlarmCardBigGridBottomBtnCancel.Click += async (sender, e) =>
            {
                await AlarmCardCancel(uid);
                await AlarmCardClose(uid);
            };

            Wpf.Ui.Controls.Button AlarmCardBigGridBottomBtnSave = new Wpf.Ui.Controls.Button();
            AlarmCardBigGridBottomBtnSave.Tag = $"{uid}_AlarmCardBigGridBottomBtnSave";
            AlarmCardBigGridBottomBtnSave.Content = await LangSystem.GetLang("alarm.save");
            AlarmCardBigGridBottomBtnSave.Foreground = (Brush)FindResource("TextFillColorPrimaryBrush");
            AlarmCardBigGridBottomBtnSave.HorizontalAlignment = HorizontalAlignment.Stretch;
            AlarmCardBigGridBottomBtnSave.Appearance = ControlAppearance.Primary;
            AlarmCardBigGridBottomBtnSave.SetValue(Grid.ColumnProperty, 2);
            AlarmCardBigGridBottomBtnSave.Icon = new ImageIcon { Source = new BitmapImage(new Uri("pack://application:,,,/Resources/Icons/checkmark.png")), Width = 16, Height = 16 };
            AlarmCardBigGridBottomBtnSave.Click += async (sender, e) =>
            {
                await AlarmCardSave(uid);
                await AlarmCardClose(uid);
            };

            AlarmCardSmallGrid.Children.Add(AlarmCardTimeText);
            AlarmCardSmallGrid.Children.Add(AlarmCardNameText);
            AlarmCardSmallGrid.Children.Add(AlarmCardDescText);
            AlarmCardSmallGrid.Children.Add(AlarmCardToggleSmall);

            AlarmCardBigGridTop.Children.Add(AlarmCardNameTextBox);
            AlarmCardBigGridTop.Children.Add(AlarmCardToggleBig);

            AlarmCardEditHourGrid.Children.Add(AlarmCardEditHourText);
            AlarmCardEditMinuteGrid.Children.Add(AlarmCardEditMinuteText);

            AlarmCardEditGrid.Children.Add(AlarmCardEditHourUpBtn);
            AlarmCardEditGrid.Children.Add(AlarmCardEditHourGrid);
            AlarmCardEditGrid.Children.Add(AlarmCardEditHourDownBtn);
            AlarmCardEditGrid.Children.Add(AlarmCardEditMinuteUpBtn);
            AlarmCardEditGrid.Children.Add(AlarmCardEditMinuteGrid);
            AlarmCardEditGrid.Children.Add(AlarmCardEditMinuteDownBtn);
            AlarmCardEditGrid.Children.Add(AlarmCardEditColonText);

            AlarmCardEditBorder.Child = AlarmCardEditGrid;

            AlarmCardBigGridMiddleLeft.Children.Add(AlarmCardEditBorder);

            AlarmCardDaysStack.Children.Add(AlarmCardDayOnce);
            AlarmCardDaysStack.Children.Add(AlarmCardDayMonday);
            AlarmCardDaysStack.Children.Add(AlarmCardDayTuesday);
            AlarmCardDaysStack.Children.Add(AlarmCardDayWednesday);
            AlarmCardDaysStack.Children.Add(AlarmCardDayThursday);
            AlarmCardDaysStack.Children.Add(AlarmCardDayFriday);
            AlarmCardDaysStack.Children.Add(AlarmCardDaySaturday);
            AlarmCardDaysStack.Children.Add(AlarmCardDaySunday);

            AlarmCardSoundComboBox.Items.Add(AlarmCardSoundComboBoxItemDefault);
            AlarmCardSoundComboBox.Items.Add(AlarmCardSoundComboBoxItemCustom);

            AlarmCardSoundGrid.Children.Add(AlarmCardSoundToggle);
            AlarmCardSoundGrid.Children.Add(AlarmCardSoundComboBox);

            AlarmCardBigGridMiddleRightStack.Children.Add(AlarmCardDaysStack);
            AlarmCardBigGridMiddleRightStack.Children.Add(AlarmCardSoundGrid);

            AlarmCardBigGridMiddleRight.Children.Add(AlarmCardBigGridMiddleRightStack);

            AlarmCardBigGridMiddle.Children.Add(AlarmCardBigGridMiddleLeft);
            AlarmCardBigGridMiddle.Children.Add(AlarmCardBigGridMiddleRight);

            AlarmCardBigGridBottom.Children.Add(AlarmCardBigGridBottomBtnDelete);
            AlarmCardBigGridBottom.Children.Add(AlarmCardBigGridBottomBtnCancel);
            AlarmCardBigGridBottom.Children.Add(AlarmCardBigGridBottomBtnSave);

            AlarmCardBigGrid.Children.Add(AlarmCardBigGridTop);
            AlarmCardBigGrid.Children.Add(AlarmCardBigGridMiddle);
            AlarmCardBigGrid.Children.Add(AlarmCardBigGridBottom);

            AlarmCardMainGrid.Children.Add(AlarmCardSmallGrid);
            AlarmCardMainGrid.Children.Add(AlarmCardBigGrid);

            AlarmCard.Content = AlarmCardMainGrid;

            AlarmStack.Children.Add(AlarmCard);

            NoAlarmGrid.Visibility = Visibility.Collapsed;
        }

        // Get alarme ETA with UID
        private async Task<string> GetETAAlarmString(string uid)
        {
            string time = await ConfigManager.GetAlarm($"{uid}.time");
            string days = await ConfigManager.GetAlarm($"{uid}.days");

            string[] timeArray = time.Split(":");
            int hours = int.Parse(timeArray[0]);
            int minutes = int.Parse(timeArray[1]);

            DateTime now = DateTime.Now;
            DateTime alarm = new DateTime(now.Year, now.Month, now.Day, hours, minutes, 0);

            if (days == "0")
            {
                if (alarm < now)
                {
                    alarm = alarm.AddDays(1);
                }
            }
            else
            {
                List<int> alarmDays = days.Select(c => int.Parse(c.ToString())).ToList();

                int today = ((int)now.DayOfWeek == 0) ? 7 : (int)now.DayOfWeek;

                int nextDay = alarmDays
                    .Where(d => (d > today || (d == today && alarm >= now)))
                    .DefaultIfEmpty(alarmDays.Min())
                    .First();

                int daysToAdd = (nextDay >= today) ? nextDay - today : (7 - today + nextDay);

                alarm = alarm.AddDays(daysToAdd);
            }

            TimeSpan diff = alarm - now;

            if (diff.Days == 0)
            {
                if (diff.Hours == 0)
                {
                    return await LangSystem.GetLang("alarm.remaining.inmin", diff.Minutes.ToString());
                }
                else
                {
                    return await LangSystem.GetLang("alarm.remaining.inhour", diff.Hours.ToString(), diff.Minutes.ToString());
                }
            }
            else
            {
                if (diff.Days == 1)
                {
                    return await LangSystem.GetLang("alarm.remaining.inday", diff.Days.ToString());
                }
                else
                {
                    return await LangSystem.GetLang("alarm.remaining.indays", diff.Days.ToString());
                }
            }
        }

        // Components in alarm card update
        private async Task AlarmCardSoundToggleClick(string uid)
        {
            FrameworkElement AlarmCard = await TagSearch.FindFrameworkElementwithTag(AlarmStack, $"{uid}_AlarmCard");
            FrameworkElement AlarmCardSoundToggle = await TagSearch.FindFrameworkElementwithTag(AlarmCard, $"{uid}_AlarmCardSoundToggle");
            FrameworkElement AlarmCardSoundComboBox = await TagSearch.FindFrameworkElementwithTag(AlarmCard, $"{uid}_AlarmCardSoundComboBox");
            FrameworkElement AlarmCardSoundComboBoxItemDefault = await TagSearch.FindItemwithTag(((ComboBox)AlarmCardSoundComboBox).Items, $"{uid}_AlarmCardSoundComboBoxItemDefault");
            FrameworkElement AlarmCardSoundComboBoxItemCustom = await TagSearch.FindItemwithTag(((ComboBox)AlarmCardSoundComboBox).Items, $"{uid}_AlarmCardSoundComboBoxItemCustom");
            if (((ToggleButton)AlarmCardSoundToggle).IsChecked == true)
            {
                ((ComboBox)AlarmCardSoundComboBox).IsEnabled = true;
            }
            else
            {
                ((ComboBox)AlarmCardSoundComboBox).IsEnabled = false;
            }

            ((ComboBoxItem)AlarmCardSoundComboBoxItemDefault).IsSelected = true;
            ((ComboBoxItem)AlarmCardSoundComboBoxItemCustom).IsSelected = false;
            ((ComboBoxItem)AlarmCardSoundComboBoxItemCustom).Content = await LangSystem.GetLang("alarm.custsounds");

        }
        private async Task AlarmCardDaysToggleButton(string uid, string source)
        {
            FrameworkElement AlarmCard = await TagSearch.FindFrameworkElementwithTag(AlarmStack, $"{uid}_AlarmCard");
            FrameworkElement AlarmCardDayOnce = await TagSearch.FindFrameworkElementwithTag(AlarmCard, $"{uid}_AlarmCardDayOnce");
            FrameworkElement AlarmCardDayMonday = await TagSearch.FindFrameworkElementwithTag(AlarmCard, $"{uid}_AlarmCardDayMonday");
            FrameworkElement AlarmCardDayTuesday = await TagSearch.FindFrameworkElementwithTag(AlarmCard, $"{uid}_AlarmCardDayTuesday");
            FrameworkElement AlarmCardDayWednesday = await TagSearch.FindFrameworkElementwithTag(AlarmCard, $"{uid}_AlarmCardDayWednesday");
            FrameworkElement AlarmCardDayThursday = await TagSearch.FindFrameworkElementwithTag(AlarmCard, $"{uid}_AlarmCardDayThursday");
            FrameworkElement AlarmCardDayFriday = await TagSearch.FindFrameworkElementwithTag(AlarmCard, $"{uid}_AlarmCardDayFriday");
            FrameworkElement AlarmCardDaySaturday = await TagSearch.FindFrameworkElementwithTag(AlarmCard, $"{uid}_AlarmCardDaySaturday");
            FrameworkElement AlarmCardDaySunday = await TagSearch.FindFrameworkElementwithTag(AlarmCard, $"{uid}_AlarmCardDaySunday");

            if (((ToggleButton)AlarmCardDayOnce).IsChecked == true && source == "once")
            {
                ((ToggleButton)AlarmCardDayMonday).IsChecked = false;
                ((ToggleButton)AlarmCardDayTuesday).IsChecked = false;
                ((ToggleButton)AlarmCardDayWednesday).IsChecked = false;
                ((ToggleButton)AlarmCardDayThursday).IsChecked = false;
                ((ToggleButton)AlarmCardDayFriday).IsChecked = false;
                ((ToggleButton)AlarmCardDaySaturday).IsChecked = false;
                ((ToggleButton)AlarmCardDaySunday).IsChecked = false;
            }
            else if (((ToggleButton)AlarmCardDayOnce).IsChecked == false && source == "once")
            {
                ((ToggleButton)AlarmCardDayMonday).IsChecked = true;
                ((ToggleButton)AlarmCardDayTuesday).IsChecked = true;
                ((ToggleButton)AlarmCardDayWednesday).IsChecked = true;
                ((ToggleButton)AlarmCardDayThursday).IsChecked = true;
                ((ToggleButton)AlarmCardDayFriday).IsChecked = true;
                ((ToggleButton)AlarmCardDaySaturday).IsChecked = true;
                ((ToggleButton)AlarmCardDaySunday).IsChecked = true;
            }


            if (((ToggleButton)AlarmCardDayMonday).IsChecked == false && ((ToggleButton)AlarmCardDayTuesday).IsChecked == false && ((ToggleButton)AlarmCardDayWednesday).IsChecked == false && ((ToggleButton)AlarmCardDayThursday).IsChecked == false && ((ToggleButton)AlarmCardDayFriday).IsChecked == false && ((ToggleButton)AlarmCardDaySaturday).IsChecked == false && ((ToggleButton)AlarmCardDaySunday).IsChecked == false && source == "days")
            {
                ((ToggleButton)AlarmCardDayOnce).IsChecked = true;
            }
            else if ((((ToggleButton)AlarmCardDayMonday).IsChecked == true || ((ToggleButton)AlarmCardDayTuesday).IsChecked == true || ((ToggleButton)AlarmCardDayWednesday).IsChecked == true || ((ToggleButton)AlarmCardDayThursday).IsChecked == true || ((ToggleButton)AlarmCardDayFriday).IsChecked == true || ((ToggleButton)AlarmCardDaySaturday).IsChecked == true || ((ToggleButton)AlarmCardDaySunday).IsChecked == true) && source == "days")
            {
                ((ToggleButton)AlarmCardDayOnce).IsChecked = false;
            }
        }
        private async Task AlarmCardSoundComboBoxClose(string uid)
        {
            FrameworkElement AlarmCard = await TagSearch.FindFrameworkElementwithTag(AlarmStack, $"{uid}_AlarmCard");
            FrameworkElement AlarmCardSoundComboBox = await TagSearch.FindFrameworkElementwithTag(AlarmCard, $"{uid}_AlarmCardSoundComboBox");
            FrameworkElement AlarmCardSoundComboBoxItemDefault = await TagSearch.FindItemwithTag(((ComboBox)AlarmCardSoundComboBox).Items, $"{uid}_AlarmCardSoundComboBoxItemDefault");
            FrameworkElement AlarmCardSoundComboBoxItemCustom = await TagSearch.FindItemwithTag(((ComboBox)AlarmCardSoundComboBox).Items, $"{uid}_AlarmCardSoundComboBoxItemCustom");

            if (((ComboBoxItem)AlarmCardSoundComboBoxItemCustom).IsSelected)
            {
                var ofd = new OpenFileDialog();
                ofd.DefaultExt = "wav";
                ofd.Filter = "WAV Audio File (*.wav)|*.wav";
                ofd.FilterIndex = 1;
                ofd.Title = "WinDeskClock Custom Alarm Sound";
                var result = ofd.ShowDialog();
                if (result == true)
                {
                    string filename = ofd.FileName;
                    ((ComboBoxItem)AlarmCardSoundComboBoxItemCustom).Content = filename;
                }
                else
                {
                    ((ComboBoxItem)AlarmCardSoundComboBoxItemDefault).IsSelected = true;
                    ((ComboBoxItem)AlarmCardSoundComboBoxItemCustom).IsSelected = false;
                }
            }
        }
        private async Task AlarmCardSoundComboBoxOpen(string uid)
        {
            FrameworkElement AlarmCard = await TagSearch.FindFrameworkElementwithTag(AlarmStack, $"{uid}_AlarmCard");
            FrameworkElement AlarmCardSoundComboBox = await TagSearch.FindFrameworkElementwithTag(AlarmCard, $"{uid}_AlarmCardSoundComboBox");
            FrameworkElement AlarmCardSoundComboBoxItemDefault = await TagSearch.FindItemwithTag(((ComboBox)AlarmCardSoundComboBox).Items, $"{uid}_AlarmCardSoundComboBoxItemDefault");
            FrameworkElement AlarmCardSoundComboBoxItemCustom = await TagSearch.FindItemwithTag(((ComboBox)AlarmCardSoundComboBox).Items, $"{uid}_AlarmCardSoundComboBoxItemCustom");

            ((ComboBoxItem)AlarmCardSoundComboBoxItemCustom).Content = "Custom";
        }

        // Alarm enable/disable
        private async Task AlarmCardToggle(string uid)
        {
            FrameworkElement AlarmCard = await TagSearch.FindFrameworkElementwithTag(AlarmStack, $"{uid}_AlarmCard");
            FrameworkElement AlarmCardToggleSmall = await TagSearch.FindFrameworkElementwithTag(AlarmCard, $"{uid}_AlarmCardToggleSmall");
            FrameworkElement AlarmCardToggleBig = await TagSearch.FindFrameworkElementwithTag(AlarmCard, $"{uid}_AlarmCardToggleBig");

            if (((ToggleSwitch)AlarmCardToggleSmall).IsChecked == true)
            {
                ((ToggleSwitch)AlarmCardToggleBig).IsChecked = true;
                await ConfigManager.SetAlarm($"{uid}.enabled", "true");
            }
            else
            {
                ((ToggleSwitch)AlarmCardToggleBig).IsChecked = false;
                await ConfigManager.SetAlarm($"{uid}.enabled", "false");
            }
        }

        // Alarm card buttons
        // - Save
        private async Task AlarmCardSave(string uid)
        {
            FrameworkElement AlarmCard = await TagSearch.FindFrameworkElementwithTag(AlarmStack, $"{uid}_AlarmCard");
            FrameworkElement AlarmCardEditHourText = await TagSearch.FindFrameworkElementwithTag(AlarmCard, $"{uid}_AlarmCardEditHourText");
            FrameworkElement AlarmCardEditMinuteText = await TagSearch.FindFrameworkElementwithTag(AlarmCard, $"{uid}_AlarmCardEditMinuteText");
            FrameworkElement AlarmCardNameTextBox = await TagSearch.FindFrameworkElementwithTag(AlarmCard, $"{uid}_AlarmCardNameTextBox");
            FrameworkElement AlarmCardDayOnce = await TagSearch.FindFrameworkElementwithTag(AlarmCard, $"{uid}_AlarmCardDayOnce");
            FrameworkElement AlarmCardDayMonday = await TagSearch.FindFrameworkElementwithTag(AlarmCard, $"{uid}_AlarmCardDayMonday");
            FrameworkElement AlarmCardDayTuesday = await TagSearch.FindFrameworkElementwithTag(AlarmCard, $"{uid}_AlarmCardDayTuesday");
            FrameworkElement AlarmCardDayWednesday = await TagSearch.FindFrameworkElementwithTag(AlarmCard, $"{uid}_AlarmCardDayWednesday");
            FrameworkElement AlarmCardDayThursday = await TagSearch.FindFrameworkElementwithTag(AlarmCard, $"{uid}_AlarmCardDayThursday");
            FrameworkElement AlarmCardDayFriday = await TagSearch.FindFrameworkElementwithTag(AlarmCard, $"{uid}_AlarmCardDayFriday");
            FrameworkElement AlarmCardDaySaturday = await TagSearch.FindFrameworkElementwithTag(AlarmCard, $"{uid}_AlarmCardDaySaturday");
            FrameworkElement AlarmCardDaySunday = await TagSearch.FindFrameworkElementwithTag(AlarmCard, $"{uid}_AlarmCardDaySunday");
            FrameworkElement AlarmCardSoundToggle = await TagSearch.FindFrameworkElementwithTag(AlarmCard, $"{uid}_AlarmCardSoundToggle");
            FrameworkElement AlarmCardSoundComboBox = await TagSearch.FindFrameworkElementwithTag(AlarmCard, $"{uid}_AlarmCardSoundComboBox");
            FrameworkElement AlarmCardSoundComboBoxItemDefault = await TagSearch.FindItemwithTag(((ComboBox)AlarmCardSoundComboBox).Items, $"{uid}_AlarmCardSoundComboBoxItemDefault");
            FrameworkElement AlarmCardSoundComboBoxItemCustom = await TagSearch.FindItemwithTag(((ComboBox)AlarmCardSoundComboBox).Items, $"{uid}_AlarmCardSoundComboBoxItemCustom");

            FrameworkElement AlarmCardToggleBig = await TagSearch.FindFrameworkElementwithTag(AlarmCard, $"{uid}_AlarmCardToggleBig");
            FrameworkElement AlarmCardToggleSmall = await TagSearch.FindFrameworkElementwithTag(AlarmCard, $"{uid}_AlarmCardToggleSmall");

            FrameworkElement AlarmCardTimeText = await TagSearch.FindFrameworkElementwithTag(AlarmCard, $"{uid}_AlarmCardTimeText");
            FrameworkElement AlarmCardNameText = await TagSearch.FindFrameworkElementwithTag(AlarmCard, $"{uid}_AlarmCardNameText");
            FrameworkElement AlarmCardDescText = await TagSearch.FindFrameworkElementwithTag(AlarmCard, $"{uid}_AlarmCardDescText");

            string time = $"{((System.Windows.Controls.TextBlock)AlarmCardEditHourText).Text}:{((System.Windows.Controls.TextBlock)AlarmCardEditMinuteText).Text}";
            ((System.Windows.Controls.TextBlock)AlarmCardTimeText).Text = time;

            string name = ((Wpf.Ui.Controls.TextBox)AlarmCardNameTextBox).Text;
            ((System.Windows.Controls.TextBlock)AlarmCardNameText).Text = name;

            string daysstr = "";
            if (((ToggleButton)AlarmCardDayOnce).IsChecked == true)
            {
                daysstr += "0";
            }
            else
            {
                if (((ToggleButton)AlarmCardDayMonday).IsChecked == true)
                {
                    daysstr += "1";
                }
                if (((ToggleButton)AlarmCardDayTuesday).IsChecked == true)
                {
                    daysstr += "2";
                }
                if (((ToggleButton)AlarmCardDayWednesday).IsChecked == true)
                {
                    daysstr += "3";
                }
                if (((ToggleButton)AlarmCardDayThursday).IsChecked == true)
                {
                    daysstr += "4";
                }
                if (((ToggleButton)AlarmCardDayFriday).IsChecked == true)
                {
                    daysstr += "5";
                }
                if (((ToggleButton)AlarmCardDaySaturday).IsChecked == true)
                {
                    daysstr += "6";
                }
                if (((ToggleButton)AlarmCardDaySunday).IsChecked == true)
                {
                    daysstr += "7";
                }
            }

            string sound = "";
            if (((ToggleSwitch)AlarmCardSoundToggle).IsChecked == false)
            {
                sound = "none";
            }
            else
            {
                if (((ComboBoxItem)AlarmCardSoundComboBoxItemDefault).IsSelected == true)
                {
                    sound = "default";
                }
                else
                {
                    sound = ((ComboBoxItem)AlarmCardSoundComboBoxItemCustom).Content.ToString();
                }
            }

            string enabled = "false";
            if (((ToggleSwitch)AlarmCardToggleBig).IsChecked == true)
            {
                ((ToggleSwitch)AlarmCardToggleSmall).IsChecked = true;
                enabled = "true";
            }
            else
            {
                ((ToggleSwitch)AlarmCardToggleSmall).IsChecked = false;
                enabled = "false";
            }

            await ConfigManager.SetAlarm($"{uid}.time", time);
            await ConfigManager.SetAlarm($"{uid}.name", name);
            await ConfigManager.SetAlarm($"{uid}.days", daysstr);
            await ConfigManager.SetAlarm($"{uid}.sound", sound);
            await ConfigManager.SetAlarm($"{uid}.enabled", enabled);
        }
        // - Cancel
        private async Task AlarmCardCancel(string uid)
        {
            FrameworkElement AlarmCard = await TagSearch.FindFrameworkElementwithTag(AlarmStack, $"{uid}_AlarmCard");
            FrameworkElement AlarmCardEditHourText = await TagSearch.FindFrameworkElementwithTag(AlarmCard, $"{uid}_AlarmCardEditHourText");
            FrameworkElement AlarmCardEditMinuteText = await TagSearch.FindFrameworkElementwithTag(AlarmCard, $"{uid}_AlarmCardEditMinuteText");
            FrameworkElement AlarmCardNameTextBox = await TagSearch.FindFrameworkElementwithTag(AlarmCard, $"{uid}_AlarmCardNameTextBox");
            FrameworkElement AlarmCardDayOnce = await TagSearch.FindFrameworkElementwithTag(AlarmCard, $"{uid}_AlarmCardDayOnce");
            FrameworkElement AlarmCardDayMonday = await TagSearch.FindFrameworkElementwithTag(AlarmCard, $"{uid}_AlarmCardDayMonday");
            FrameworkElement AlarmCardDayTuesday = await TagSearch.FindFrameworkElementwithTag(AlarmCard, $"{uid}_AlarmCardDayTuesday");
            FrameworkElement AlarmCardDayWednesday = await TagSearch.FindFrameworkElementwithTag(AlarmCard, $"{uid}_AlarmCardDayWednesday");
            FrameworkElement AlarmCardDayThursday = await TagSearch.FindFrameworkElementwithTag(AlarmCard, $"{uid}_AlarmCardDayThursday");
            FrameworkElement AlarmCardDayFriday = await TagSearch.FindFrameworkElementwithTag(AlarmCard, $"{uid}_AlarmCardDayFriday");
            FrameworkElement AlarmCardDaySaturday = await TagSearch.FindFrameworkElementwithTag(AlarmCard, $"{uid}_AlarmCardDaySaturday");
            FrameworkElement AlarmCardDaySunday = await TagSearch.FindFrameworkElementwithTag(AlarmCard, $"{uid}_AlarmCardDaySunday");
            FrameworkElement AlarmCardSoundToggle = await TagSearch.FindFrameworkElementwithTag(AlarmCard, $"{uid}_AlarmCardSoundToggle");
            FrameworkElement AlarmCardSoundComboBox = await TagSearch.FindFrameworkElementwithTag(AlarmCard, $"{uid}_AlarmCardSoundComboBox");
            FrameworkElement AlarmCardSoundComboBoxItemDefault = await TagSearch.FindItemwithTag(((ComboBox)AlarmCardSoundComboBox).Items, $"{uid}_AlarmCardSoundComboBoxItemDefault");
            FrameworkElement AlarmCardSoundComboBoxItemCustom = await TagSearch.FindItemwithTag(((ComboBox)AlarmCardSoundComboBox).Items, $"{uid}_AlarmCardSoundComboBoxItemCustom");

            FrameworkElement AlarmCardToggleBig = await TagSearch.FindFrameworkElementwithTag(AlarmCard, $"{uid}_AlarmCardToggleBig");

            string name = await ConfigManager.GetAlarm($"{uid}.name");
            ((Wpf.Ui.Controls.TextBox)AlarmCardNameTextBox).Text = name;

            string time = await ConfigManager.GetAlarm($"{uid}.time");
            string[] timeArray = time.Split(":");
            ((System.Windows.Controls.TextBlock)AlarmCardEditHourText).Text = timeArray[0];
            ((System.Windows.Controls.TextBlock)AlarmCardEditMinuteText).Text = timeArray[1];
            ((Wpf.Ui.Controls.TextBox)AlarmCardNameTextBox).Text = await ConfigManager.GetAlarm($"{uid}.name");

            string daysstr = await ConfigManager.GetAlarm($"{uid}.days");
            if (daysstr.Contains("0"))
            {
                ((ToggleButton)AlarmCardDayOnce).IsChecked = true;
            }
            else
            {
                ((ToggleButton)AlarmCardDayOnce).IsChecked = false;
                if (daysstr.Contains("1"))
                {
                    ((ToggleButton)AlarmCardDayMonday).IsChecked = true;
                }
                if (daysstr.Contains("2"))
                {
                    ((ToggleButton)AlarmCardDayTuesday).IsChecked = true;
                }
                if (daysstr.Contains("3"))
                {
                    ((ToggleButton)AlarmCardDayWednesday).IsChecked = true;
                }
                if (daysstr.Contains("4"))
                {
                    ((ToggleButton)AlarmCardDayThursday).IsChecked = true;
                }
                if (daysstr.Contains("5"))
                {
                    ((ToggleButton)AlarmCardDayFriday).IsChecked = true;
                }
                if (daysstr.Contains("6"))
                {
                    ((ToggleButton)AlarmCardDaySaturday).IsChecked = true;
                }
                if (daysstr.Contains("7"))
                {
                    ((ToggleButton)AlarmCardDaySunday).IsChecked = true;
                }
            }

            string sound = await ConfigManager.GetAlarm($"{uid}.sound");
            if (sound.Contains("none"))
            {
                ((ToggleSwitch)AlarmCardSoundToggle).IsChecked = false;
            }
            else
            {
                ((ToggleSwitch)AlarmCardSoundToggle).IsChecked = true;
                if (sound.Contains("default"))
                {
                    ((ComboBoxItem)AlarmCardSoundComboBoxItemDefault).IsSelected = true;
                }
                else
                {
                    ((ComboBoxItem)AlarmCardSoundComboBoxItemCustom).IsSelected = true;
                    ((ComboBoxItem)AlarmCardSoundComboBoxItemCustom).Content = sound;
                }
            }

            string enabled = await ConfigManager.GetAlarm($"{uid}.enabled");
            if (enabled.Contains("true"))
            {
                ((ToggleSwitch)AlarmCardToggleBig).IsChecked = true;
            }
            else
            {
                ((ToggleSwitch)AlarmCardToggleBig).IsChecked = false;
            }
        }
        // - Delete
        private async Task AlarmCardDelete(string uid)
        {
            await ConfigManager.DelAlarm(uid);
            FrameworkElement AlarmCard = await TagSearch.FindFrameworkElementwithTag(AlarmStack, $"{uid}_AlarmCard");
            {
                var sizeAnimation = new DoubleAnimation
                {
                    From = AlarmCard.ActualHeight,
                    To = 0,
                    Duration = TimeSpan.FromSeconds(0.2),
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                };
                Storyboard.SetTarget(sizeAnimation, AlarmCard);
                Storyboard.SetTargetProperty(sizeAnimation, new PropertyPath(HeightProperty));
                var storyboard = new Storyboard();
                storyboard.Children.Add(sizeAnimation);
                storyboard.Begin();
            }
            await Task.Delay(200);
            AlarmStack.Children.Remove((CardAction)AlarmCard);
            if (AlarmStack.Children.Count == 0)
            {
                NoAlarmGrid.Visibility = Visibility.Visible;
            }
        }
        // - Hour Up/Down
        private async Task AlarmCardEditHour(string uid, string direction)
        {
            FrameworkElement AlarmCard = await TagSearch.FindFrameworkElementwithTag(AlarmStack, $"{uid}_AlarmCard");
            FrameworkElement AlarmCardEditHourText = await TagSearch.FindFrameworkElementwithTag(AlarmCard, $"{uid}_AlarmCardEditHourText");
            int hour = int.Parse(((System.Windows.Controls.TextBlock)AlarmCardEditHourText).Text);
            if (direction == "up")
            {
                if (hour == 23)
                {
                    hour = 0;
                }
                else
                {
                    hour++;
                }

                {
                    var translateAnimation = new DoubleAnimation
                    {
                        From = 0,
                        To = 50,
                        Duration = TimeSpan.FromSeconds(0.1),
                        EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                    };
                    Storyboard.SetTarget(translateAnimation, AlarmCardEditHourText);
                    Storyboard.SetTargetProperty(translateAnimation, new PropertyPath("(UIElement.RenderTransform).(TranslateTransform.Y)"));
                    var storyboard = new Storyboard();
                    storyboard.Children.Add(translateAnimation);
                    storyboard.Begin();
                }

                await Task.Delay(100);

                {
                    var translateAnimation = new DoubleAnimation
                    {
                        From = -50,
                        To = 0,
                        Duration = TimeSpan.FromSeconds(0.1),
                        EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                    };
                    Storyboard.SetTarget(translateAnimation, AlarmCardEditHourText);
                    Storyboard.SetTargetProperty(translateAnimation, new PropertyPath("(UIElement.RenderTransform).(TranslateTransform.Y)"));
                    var storyboard = new Storyboard();
                    storyboard.Children.Add(translateAnimation);
                    storyboard.Begin();
                }
            }
            else if (direction == "down")
            {
                if (hour == 0)
                {
                    hour = 23;
                }
                else
                {
                    hour--;
                }

                {
                    var translateAnimation = new DoubleAnimation
                    {
                        From = 0,
                        To = -50,
                        Duration = TimeSpan.FromSeconds(0.1),
                        EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                    };
                    Storyboard.SetTarget(translateAnimation, AlarmCardEditHourText);
                    Storyboard.SetTargetProperty(translateAnimation, new PropertyPath("(UIElement.RenderTransform).(TranslateTransform.Y)"));
                    var storyboard = new Storyboard();
                    storyboard.Children.Add(translateAnimation);
                    storyboard.Begin();
                }

                await Task.Delay(100);

                {
                    var translateAnimation = new DoubleAnimation
                    {
                        From = 50,
                        To = 0,
                        Duration = TimeSpan.FromSeconds(0.1),
                        EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                    };
                    Storyboard.SetTarget(translateAnimation, AlarmCardEditHourText);
                    Storyboard.SetTargetProperty(translateAnimation, new PropertyPath("(UIElement.RenderTransform).(TranslateTransform.Y)"));
                    var storyboard = new Storyboard();
                    storyboard.Children.Add(translateAnimation);
                    storyboard.Begin();
                }
            }
            ((System.Windows.Controls.TextBlock)AlarmCardEditHourText).Text = hour.ToString("00");
        }
        // - Minute Up/Down
        private async Task AlarmCardEditMinute(string uid, string direction)
        {
            FrameworkElement AlarmCard = await TagSearch.FindFrameworkElementwithTag(AlarmStack, $"{uid}_AlarmCard");
            FrameworkElement AlarmCardEditMinuteText = await TagSearch.FindFrameworkElementwithTag(AlarmCard, $"{uid}_AlarmCardEditMinuteText");
            int minute = int.Parse(((System.Windows.Controls.TextBlock)AlarmCardEditMinuteText).Text);
            if (direction == "up")
            {
                if (minute == 59)
                {
                    minute = 0;
                }
                else
                {
                    minute++;
                }

                {
                    var translateAnimation = new DoubleAnimation
                    {
                        From = 0,
                        To = 50,
                        Duration = TimeSpan.FromSeconds(0.1),
                        EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                    };
                    Storyboard.SetTarget(translateAnimation, AlarmCardEditMinuteText);
                    Storyboard.SetTargetProperty(translateAnimation, new PropertyPath("(UIElement.RenderTransform).(TranslateTransform.Y)"));
                    var storyboard = new Storyboard();
                    storyboard.Children.Add(translateAnimation);
                    storyboard.Begin();
                }

                await Task.Delay(100);

                {
                    var translateAnimation = new DoubleAnimation
                    {
                        From = -50,
                        To = 0,
                        Duration = TimeSpan.FromSeconds(0.1),
                        EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                    };
                    Storyboard.SetTarget(translateAnimation, AlarmCardEditMinuteText);
                    Storyboard.SetTargetProperty(translateAnimation, new PropertyPath("(UIElement.RenderTransform).(TranslateTransform.Y)"));
                    var storyboard = new Storyboard();
                    storyboard.Children.Add(translateAnimation);
                    storyboard.Begin();
                }
            }
            else if (direction == "down")
            {
                if (minute == 0)
                {
                    minute = 59;
                }
                else
                {
                    minute--;
                }

                {
                    var translateAnimation = new DoubleAnimation
                    {
                        From = 0,
                        To = -50,
                        Duration = TimeSpan.FromSeconds(0.1),
                        EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                    };
                    Storyboard.SetTarget(translateAnimation, AlarmCardEditMinuteText);
                    Storyboard.SetTargetProperty(translateAnimation, new PropertyPath("(UIElement.RenderTransform).(TranslateTransform.Y)"));
                    var storyboard = new Storyboard();
                    storyboard.Children.Add(translateAnimation);
                    storyboard.Begin();
                }

                await Task.Delay(100);

                {
                    var translateAnimation = new DoubleAnimation
                    {
                        From = 50,
                        To = 0,
                        Duration = TimeSpan.FromSeconds(0.1),
                        EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                    };
                    Storyboard.SetTarget(translateAnimation, AlarmCardEditMinuteText);
                    Storyboard.SetTargetProperty(translateAnimation, new PropertyPath("(UIElement.RenderTransform).(TranslateTransform.Y)"));
                    var storyboard = new Storyboard();
                    storyboard.Children.Add(translateAnimation);
                    storyboard.Begin();
                }
            }
            ((System.Windows.Controls.TextBlock)AlarmCardEditMinuteText).Text = minute.ToString("00");
        }

        // Alarm card open
        private async Task AlarmCardOpen(object sender, MouseButtonEventArgs e, string uid)
        {
            if ((e.Source is CardAction || e.Source is System.Windows.Controls.TextBlock) && !CardDeployed)
            {
                CardDeployed = true;

                foreach (CardAction card in AlarmStack.Children)
                {
                    if (card.Tag.ToString() != $"{uid}_AlarmCard")
                    {
                        var sizeAnimation = new DoubleAnimation
                        {
                            From = card.ActualHeight,
                            To = 0,
                            Duration = TimeSpan.FromSeconds(0.3),
                            EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                        };
                        Storyboard.SetTarget(sizeAnimation, card);
                        Storyboard.SetTargetProperty(sizeAnimation, new PropertyPath(HeightProperty));
                        var storyboard = new Storyboard();
                        storyboard.Children.Add(sizeAnimation);
                        storyboard.Begin();
                    }
                }

                FrameworkElement AlarmCard = await TagSearch.FindFrameworkElementwithTag(AlarmStack, $"{uid}_AlarmCard");
                FrameworkElement AlarmCardMainGrid = await TagSearch.FindFrameworkElementwithTag(AlarmCard, $"{uid}_AlarmCardMainGrid");
                FrameworkElement AlarmCardBigGrid = await TagSearch.FindFrameworkElementwithTag(AlarmCard, $"{uid}_AlarmCardBigGrid");
                FrameworkElement AlarmCardSmallGrid = await TagSearch.FindFrameworkElementwithTag(AlarmCard, $"{uid}_AlarmCardSmallGrid");

                AlarmCardSmallGrid.Visibility = Visibility.Collapsed;
                AlarmCardBigGrid.Visibility = Visibility.Visible;

                {
                    var sizeAnimation = new DoubleAnimation
                    {
                        From = AlarmCard.ActualHeight,
                        To = AlarmScroll.ActualHeight - 23,
                        Duration = TimeSpan.FromSeconds(0.3),
                        EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                    };
                    Storyboard.SetTarget(sizeAnimation, AlarmCardMainGrid);
                    Storyboard.SetTargetProperty(sizeAnimation, new PropertyPath(HeightProperty));
                    var storyboard = new Storyboard();
                    storyboard.Children.Add(sizeAnimation);
                    storyboard.Begin();

                    var fadeAnimation = new DoubleAnimation
                    {
                        From = 0,
                        To = 1,
                        Duration = TimeSpan.FromSeconds(0.5),
                        EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn }
                    };
                    Storyboard.SetTarget(fadeAnimation, AlarmCardBigGrid);
                    Storyboard.SetTargetProperty(fadeAnimation, new PropertyPath(OpacityProperty));
                    var storyboard2 = new Storyboard();
                    storyboard2.Children.Add(fadeAnimation);
                    storyboard2.Begin();
                }

                await Task.Delay(150);

                foreach (CardAction card in AlarmStack.Children)
                {
                    if (card.Tag.ToString() != $"{uid}_AlarmCard")
                    {
                        card.Visibility = Visibility.Collapsed;
                    }
                }

                await Task.Delay(150);
            }
        }

        // Alarm card close
        private async Task AlarmCardClose(string uid)
        {

            foreach (CardAction card in AlarmStack.Children)
            {
                card.Visibility = Visibility.Visible;
            }

            await Task.Delay(150);

            FrameworkElement AlarmCard = await TagSearch.FindFrameworkElementwithTag(AlarmStack, $"{uid}_AlarmCard");
            FrameworkElement AlarmCardMainGrid = await TagSearch.FindFrameworkElementwithTag(AlarmCard, $"{uid}_AlarmCardMainGrid");
            FrameworkElement AlarmCardBigGrid = await TagSearch.FindFrameworkElementwithTag(AlarmCard, $"{uid}_AlarmCardBigGrid");
            FrameworkElement AlarmCardSmallGrid = await TagSearch.FindFrameworkElementwithTag(AlarmCard, $"{uid}_AlarmCardSmallGrid");

            AlarmCardBigGrid.Visibility = Visibility.Collapsed;
            AlarmCardSmallGrid.Visibility = Visibility.Visible;

            {
                var sizeAnimation = new DoubleAnimation
                {
                    From = AlarmScroll.ActualHeight - 23,
                    To = AlarmCardSmallGrid.ActualHeight,
                    Duration = TimeSpan.FromSeconds(0.3),
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                };
                Storyboard.SetTarget(sizeAnimation, AlarmCardMainGrid);
                Storyboard.SetTargetProperty(sizeAnimation, new PropertyPath(HeightProperty));
                var storyboard = new Storyboard();
                storyboard.Children.Add(sizeAnimation);
                storyboard.Begin();

                var fadeAnimation = new DoubleAnimation
                {
                    From = 0,
                    To = 1,
                    Duration = TimeSpan.FromSeconds(0.5),
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn }
                };
                Storyboard.SetTarget(fadeAnimation, AlarmCardSmallGrid);
                Storyboard.SetTargetProperty(fadeAnimation, new PropertyPath(OpacityProperty));
                var storyboard2 = new Storyboard();
                storyboard2.Children.Add(fadeAnimation);
                storyboard2.Begin();
            }

            foreach (CardAction card in AlarmStack.Children)
            {
                if (card.Tag.ToString() != $"{uid}_AlarmCard")
                {
                    var sizeAnimation = new DoubleAnimation
                    {
                        From = 0,
                        To = 64,
                        Duration = TimeSpan.FromSeconds(0.3),
                        EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                    };
                    Storyboard.SetTarget(sizeAnimation, card);
                    Storyboard.SetTargetProperty(sizeAnimation, new PropertyPath(HeightProperty));
                    var storyboard = new Storyboard();
                    storyboard.Children.Add(sizeAnimation);
                    storyboard.Begin();
                }
            }

            await Task.Delay(300);

            AlarmCardMainGrid.Height = double.NaN;
            foreach (CardAction card in AlarmStack.Children)
            {
                card.BeginAnimation(HeightProperty, null);
                card.Height = double.NaN;
            }

            CardDeployed = false;

            await Task.Delay(500);

            await SortAlarmCards();
            await AlarmCardETAUpdate();
        }

        // Add alarm
        private async void AddAlarmBtn_Click(object sender, RoutedEventArgs e)
        {
            if (!CardDeployed)
            {

                CardDeployed = true;
                string uid = Guid.NewGuid().ToString();

                DateTime now = DateTime.Now;
                string time = $"{now.Hour.ToString("00")}:{now.Minute.ToString("00")}";
                string name = "";
                string days = "0";
                string sound = "default";
                string enabled = "true";
                await ConfigManager.SetAlarm($"{uid}.time", time);
                await ConfigManager.SetAlarm($"{uid}.name", name);
                await ConfigManager.SetAlarm($"{uid}.days", days);
                await ConfigManager.SetAlarm($"{uid}.sound", sound);
                await ConfigManager.SetAlarm($"{uid}.enabled", enabled);

                await CreateAlarmCard(uid);

                FrameworkElement AlarmCard = await TagSearch.FindFrameworkElementwithTag(AlarmStack, $"{uid}_AlarmCard");

                {
                    var sizeAnimation = new DoubleAnimation
                    {
                        From = 0,
                        To = 64,
                        Duration = TimeSpan.FromSeconds(0.3),
                        EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                    };
                    Storyboard.SetTarget(sizeAnimation, AlarmCard);
                    Storyboard.SetTargetProperty(sizeAnimation, new PropertyPath(HeightProperty));
                    var storyboard = new Storyboard();
                    storyboard.Children.Add(sizeAnimation);
                    storyboard.Begin();
                }

                await Task.Delay(300);

                AlarmCard.BeginAnimation(HeightProperty, null);
                AlarmCard.Height = double.NaN;

                foreach (CardAction card in AlarmStack.Children)
                {
                    if (card.Tag.ToString() != $"{uid}_AlarmCard")
                    {
                        var sizeAnimation = new DoubleAnimation
                        {
                            From = card.ActualHeight,
                            To = 0,
                            Duration = TimeSpan.FromSeconds(0.3),
                            EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                        };
                        Storyboard.SetTarget(sizeAnimation, card);
                        Storyboard.SetTargetProperty(sizeAnimation, new PropertyPath(HeightProperty));
                        var storyboard = new Storyboard();
                        storyboard.Children.Add(sizeAnimation);
                        storyboard.Begin();
                    }
                }

                FrameworkElement AlarmCardMainGrid = await TagSearch.FindFrameworkElementwithTag(AlarmCard, $"{uid}_AlarmCardMainGrid");
                FrameworkElement AlarmCardBigGrid = await TagSearch.FindFrameworkElementwithTag(AlarmCard, $"{uid}_AlarmCardBigGrid");
                FrameworkElement AlarmCardSmallGrid = await TagSearch.FindFrameworkElementwithTag(AlarmCard, $"{uid}_AlarmCardSmallGrid");

                AlarmCardSmallGrid.Visibility = Visibility.Collapsed;
                AlarmCardBigGrid.Visibility = Visibility.Visible;

                {
                    var sizeAnimation = new DoubleAnimation
                    {
                        From = AlarmCard.ActualHeight,
                        To = AlarmScroll.ActualHeight - 23,
                        Duration = TimeSpan.FromSeconds(0.3),
                        EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                    };
                    Storyboard.SetTarget(sizeAnimation, AlarmCardMainGrid);
                    Storyboard.SetTargetProperty(sizeAnimation, new PropertyPath(HeightProperty));
                    var storyboard = new Storyboard();
                    storyboard.Children.Add(sizeAnimation);
                    storyboard.Begin();

                    var fadeAnimation = new DoubleAnimation
                    {
                        From = 0,
                        To = 1,
                        Duration = TimeSpan.FromSeconds(0.5),
                        EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn }
                    };
                    Storyboard.SetTarget(fadeAnimation, AlarmCardBigGrid);
                    Storyboard.SetTargetProperty(fadeAnimation, new PropertyPath(OpacityProperty));
                    var storyboard2 = new Storyboard();
                    storyboard2.Children.Add(fadeAnimation);
                    storyboard2.Begin();
                }

                await Task.Delay(150);
                foreach (CardAction card in AlarmStack.Children)
                {
                    if (card.Tag.ToString() != $"{uid}_AlarmCard")
                    {
                        card.Visibility = Visibility.Collapsed;
                    }
                }
                await Task.Delay(150);
            }
        }

        // Stop alarm alert
        private void AlarmAlertStopBtn_Click(object sender, RoutedEventArgs e)
        {
            AlarmAlert = false;
        }
        #endregion

        #region Global Menu
        private double animspeedzoomgm = 0.2;
        private async void GlobalMenuBtn_Click(object sender, RoutedEventArgs e)
        {

            {
                var sizeAnimation1 = new DoubleAnimation
                {
                    From = 1,
                    To = 2,
                    Duration = TimeSpan.FromSeconds(animspeedzoomgm),
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn }
                };
                var sizeAnimation2 = new DoubleAnimation
                {
                    From = 1,
                    To = 2,
                    Duration = TimeSpan.FromSeconds(animspeedzoomgm),
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn }
                };
                Storyboard.SetTarget(sizeAnimation1, GlobalMenuBtn);
                Storyboard.SetTargetProperty(sizeAnimation1, new PropertyPath("(UIElement.RenderTransform).(ScaleTransform.ScaleX)"));
                Storyboard.SetTarget(sizeAnimation2, GlobalMenuBtn);
                Storyboard.SetTargetProperty(sizeAnimation2, new PropertyPath("(UIElement.RenderTransform).(ScaleTransform.ScaleY)"));
                var storyboard = new Storyboard();
                storyboard.Children.Add(sizeAnimation1);
                storyboard.Children.Add(sizeAnimation2);
                storyboard.Begin();
            }
            {
                var fadeAnimation = new DoubleAnimation
                {
                    From = 1,
                    To = 0,
                    Duration = TimeSpan.FromSeconds(animspeedzoomgm),
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn }
                };
                Storyboard.SetTarget(fadeAnimation, GlobalMenuBtn);
                Storyboard.SetTargetProperty(fadeAnimation, new PropertyPath(OpacityProperty));
                var storyboard = new Storyboard();
                storyboard.Children.Add(fadeAnimation);
                storyboard.Begin();
            }
            await Task.Delay(150);
            GlobalMenuGrid.Visibility = Visibility.Visible;
            {
                var fadeAnimation = new DoubleAnimation
                {
                    From = 0,
                    To = 1,
                    Duration = TimeSpan.FromSeconds(animspeedzoomgm),
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn }
                };
                Storyboard.SetTarget(fadeAnimation, GlobalMenuGrid);
                Storyboard.SetTargetProperty(fadeAnimation, new PropertyPath(OpacityProperty));
                var storyboard = new Storyboard();
                storyboard.Children.Add(fadeAnimation);
                storyboard.Begin();
            }
            {
                var sizeAnimation1 = new DoubleAnimation
                {
                    From = 0,
                    To = 1,
                    Duration = TimeSpan.FromSeconds(animspeedzoomgm),
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                };
                var sizeAnimation2 = new DoubleAnimation
                {
                    From = 0,
                    To = 1,
                    Duration = TimeSpan.FromSeconds(animspeedzoomgm),
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                };
                Storyboard.SetTarget(sizeAnimation1, GlobalMenuBorder);
                Storyboard.SetTargetProperty(sizeAnimation1, new PropertyPath("(UIElement.RenderTransform).(ScaleTransform.ScaleX)"));
                Storyboard.SetTarget(sizeAnimation2, GlobalMenuBorder);
                Storyboard.SetTargetProperty(sizeAnimation2, new PropertyPath("(UIElement.RenderTransform).(ScaleTransform.ScaleY)"));
                var storyboard = new Storyboard();
                storyboard.Children.Add(sizeAnimation1);
                storyboard.Children.Add(sizeAnimation2);
                storyboard.Begin();
            }
        }
        private async void BackBtn_Click(object sender, RoutedEventArgs e)
        {
            {
                var fadeAnimation = new DoubleAnimation
                {
                    From = 1,
                    To = 0,
                    Duration = TimeSpan.FromSeconds(animspeedzoomgm),
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                };
                Storyboard.SetTarget(fadeAnimation, GlobalMenuGrid);
                Storyboard.SetTargetProperty(fadeAnimation, new PropertyPath(OpacityProperty));
                var storyboard = new Storyboard();
                storyboard.Children.Add(fadeAnimation);
                storyboard.Begin();
            }
            {
                var sizeAnimation1 = new DoubleAnimation
                {
                    From = 1,
                    To = 0,
                    Duration = TimeSpan.FromSeconds(animspeedzoomgm),
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn }
                };
                var sizeAnimation2 = new DoubleAnimation
                {
                    From = 1,
                    To = 0,
                    Duration = TimeSpan.FromSeconds(animspeedzoomgm),
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn }
                };
                Storyboard.SetTarget(sizeAnimation1, GlobalMenuBorder);
                Storyboard.SetTargetProperty(sizeAnimation1, new PropertyPath("(UIElement.RenderTransform).(ScaleTransform.ScaleX)"));
                Storyboard.SetTarget(sizeAnimation2, GlobalMenuBorder);
                Storyboard.SetTargetProperty(sizeAnimation2, new PropertyPath("(UIElement.RenderTransform).(ScaleTransform.ScaleY)"));
                var storyboard = new Storyboard();
                storyboard.Children.Add(sizeAnimation1);
                storyboard.Children.Add(sizeAnimation2);
                storyboard.Begin();
            }
            await Task.Delay(150);
            {
                var fadeAnimation = new DoubleAnimation
                {
                    From = 0,
                    To = 1,
                    Duration = TimeSpan.FromSeconds(animspeedzoomgm),
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                };
                Storyboard.SetTarget(fadeAnimation, GlobalMenuBtn);
                Storyboard.SetTargetProperty(fadeAnimation, new PropertyPath(OpacityProperty));
                var storyboard = new Storyboard();
                storyboard.Children.Add(fadeAnimation);
                storyboard.Begin();
            }
            {
                var sizeAnimation1 = new DoubleAnimation
                {
                    From = 2,
                    To = 1,
                    Duration = TimeSpan.FromSeconds(animspeedzoomgm),
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                };
                var sizeAnimation2 = new DoubleAnimation
                {
                    From = 2,
                    To = 1,
                    Duration = TimeSpan.FromSeconds(animspeedzoomgm),
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                };
                Storyboard.SetTarget(sizeAnimation1, GlobalMenuBtn);
                Storyboard.SetTargetProperty(sizeAnimation1, new PropertyPath("(UIElement.RenderTransform).(ScaleTransform.ScaleX)"));
                Storyboard.SetTarget(sizeAnimation2, GlobalMenuBtn);
                Storyboard.SetTargetProperty(sizeAnimation2, new PropertyPath("(UIElement.RenderTransform).(ScaleTransform.ScaleY)"));
                var storyboard = new Storyboard();
                storyboard.Children.Add(sizeAnimation1);
                storyboard.Children.Add(sizeAnimation2);
                storyboard.Begin();
            }
            await Task.Delay(300);
            GlobalMenuGrid.Visibility = Visibility.Hidden;
        }
        private async void ScreenOffBtn_Click(object sender, RoutedEventArgs e)
        {
            GlobalMenuGrid.Visibility = Visibility.Hidden;
            {
                var fadeAnimation = new DoubleAnimation
                {
                    From = 0,
                    To = 1,
                    Duration = TimeSpan.FromSeconds(0.3),
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                };
                Storyboard.SetTarget(fadeAnimation, GlobalMenuBtn);
                Storyboard.SetTargetProperty(fadeAnimation, new PropertyPath(OpacityProperty));
                var storyboard = new Storyboard();
                storyboard.Children.Add(fadeAnimation);
                storyboard.Begin();
            }
            {
                var sizeAnimation1 = new DoubleAnimation
                {
                    From = 2,
                    To = 1,
                    Duration = TimeSpan.FromSeconds(0.3),
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                };
                var sizeAnimation2 = new DoubleAnimation
                {
                    From = 2,
                    To = 1,
                    Duration = TimeSpan.FromSeconds(0.3),
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                };
                Storyboard.SetTarget(sizeAnimation1, GlobalMenuBtn);
                Storyboard.SetTargetProperty(sizeAnimation1, new PropertyPath("(UIElement.RenderTransform).(ScaleTransform.ScaleX)"));
                Storyboard.SetTarget(sizeAnimation2, GlobalMenuBtn);
                Storyboard.SetTargetProperty(sizeAnimation2, new PropertyPath("(UIElement.RenderTransform).(ScaleTransform.ScaleY)"));
                var storyboard = new Storyboard();
                storyboard.Children.Add(sizeAnimation1);
                storyboard.Children.Add(sizeAnimation2);
                storyboard.Begin();
            }
            await TurnScreenOff();
        }
        private async void FullScreenBtn_Click(object sender, RoutedEventArgs e)
        {
            if (FullScreenBtn.IsChecked == true)
            {
                WindowState = WindowState.Normal;
                WindowStyle = WindowStyle.None;
                WindowState = WindowState.Maximized;
                RootTitleBar.ShowMaximize = false;
                RootTitleBar.ShowMinimize = false;
            }
            else
            {
                WindowState = WindowState.Normal;
                WindowStyle = WindowStyle.SingleBorderWindow;
                WindowState = WindowState.Maximized;
                RootTitleBar.ShowMaximize = true;
                RootTitleBar.ShowMinimize = true;
            }
        }
        private async void KioskModeBtn_Click(object sender, RoutedEventArgs e)
        {
            if (KioskModeBtn.IsChecked == true)
            {
                FullScreenBtn.IsChecked = true;
                FullScreenBtn.IsEnabled = false;
                WindowState = WindowState.Normal;
                WindowStyle = WindowStyle.None;
                WindowState = WindowState.Maximized;
                RootTitleBar.ShowMaximize = false;
                RootTitleBar.ShowMinimize = false;
                RootTitleBar.ShowClose = false;
                Process.Start("taskkill", "/f /im explorer.exe");
            }
            else
            {
                FullScreenBtn.IsChecked = false;
                FullScreenBtn.IsEnabled = true;
                WindowState = WindowState.Normal;
                WindowStyle = WindowStyle.SingleBorderWindow;
                WindowState = WindowState.Maximized;
                RootTitleBar.ShowMaximize = true;
                RootTitleBar.ShowMinimize = true;
                RootTitleBar.ShowClose = true;
                Process.Start("explorer.exe");
            }
        }

        private double animzoomspeeds = 0.3;
        private async void SettingsBtn_Click(object sender, RoutedEventArgs e)
        {
            {
                var fadeAnimation = new DoubleAnimation
                {
                    From = 1,
                    To = 0,
                    Duration = TimeSpan.FromSeconds(animspeedzoomgm),
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                };
                Storyboard.SetTarget(fadeAnimation, GlobalMenuGrid);
                Storyboard.SetTargetProperty(fadeAnimation, new PropertyPath(OpacityProperty));
                var storyboard = new Storyboard();
                storyboard.Children.Add(fadeAnimation);
                storyboard.Begin();
            }
            {
                var sizeAnimation1 = new DoubleAnimation
                {
                    From = 1,
                    To = 0,
                    Duration = TimeSpan.FromSeconds(animspeedzoomgm),
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn }
                };
                var sizeAnimation2 = new DoubleAnimation
                {
                    From = 1,
                    To = 0,
                    Duration = TimeSpan.FromSeconds(animspeedzoomgm),
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn }
                };
                Storyboard.SetTarget(sizeAnimation1, GlobalMenuBorder);
                Storyboard.SetTargetProperty(sizeAnimation1, new PropertyPath("(UIElement.RenderTransform).(ScaleTransform.ScaleX)"));
                Storyboard.SetTarget(sizeAnimation2, GlobalMenuBorder);
                Storyboard.SetTargetProperty(sizeAnimation2, new PropertyPath("(UIElement.RenderTransform).(ScaleTransform.ScaleY)"));
                var storyboard = new Storyboard();
                storyboard.Children.Add(sizeAnimation1);
                storyboard.Children.Add(sizeAnimation2);
                storyboard.Begin();
            }
            await Task.Delay(150);
            {
                var fadeAnimation = new DoubleAnimation
                {
                    From = 0,
                    To = 1,
                    Duration = TimeSpan.FromSeconds(animspeedzoomgm),
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                };
                Storyboard.SetTarget(fadeAnimation, GlobalMenuBtn);
                Storyboard.SetTargetProperty(fadeAnimation, new PropertyPath(OpacityProperty));
                var storyboard = new Storyboard();
                storyboard.Children.Add(fadeAnimation);
                storyboard.Begin();
            }
            {
                var sizeAnimation1 = new DoubleAnimation
                {
                    From = 2,
                    To = 1,
                    Duration = TimeSpan.FromSeconds(animspeedzoomgm),
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                };
                var sizeAnimation2 = new DoubleAnimation
                {
                    From = 2,
                    To = 1,
                    Duration = TimeSpan.FromSeconds(animspeedzoomgm),
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                };
                Storyboard.SetTarget(sizeAnimation1, GlobalMenuBtn);
                Storyboard.SetTargetProperty(sizeAnimation1, new PropertyPath("(UIElement.RenderTransform).(ScaleTransform.ScaleX)"));
                Storyboard.SetTarget(sizeAnimation2, GlobalMenuBtn);
                Storyboard.SetTargetProperty(sizeAnimation2, new PropertyPath("(UIElement.RenderTransform).(ScaleTransform.ScaleY)"));
                var storyboard = new Storyboard();
                storyboard.Children.Add(sizeAnimation1);
                storyboard.Children.Add(sizeAnimation2);
                storyboard.Begin();
            }
            await Task.Delay(300);
            GlobalMenuGrid.Visibility = Visibility.Hidden;
            SettingsGrid.Visibility = Visibility.Visible;
            {
                var fadeAnimation = new DoubleAnimation
                {
                    From = 0,
                    To = 1,
                    Duration = TimeSpan.FromSeconds(animzoomspeeds),
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn }
                };
                Storyboard.SetTarget(fadeAnimation, SettingsGrid);
                Storyboard.SetTargetProperty(fadeAnimation, new PropertyPath(OpacityProperty));
                var storyboard = new Storyboard();
                storyboard.Children.Add(fadeAnimation);
                storyboard.Begin();
            }
            {
                var sizeAnimation1 = new DoubleAnimation
                {
                    From = 1.1,
                    To = 1,
                    Duration = TimeSpan.FromSeconds(animzoomspeeds),
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                };
                var sizeAnimation2 = new DoubleAnimation
                {
                    From = 1.1,
                    To = 1,
                    Duration = TimeSpan.FromSeconds(animzoomspeeds),
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                };
                Storyboard.SetTarget(sizeAnimation1, SettingsBorder);
                Storyboard.SetTargetProperty(sizeAnimation1, new PropertyPath("(UIElement.RenderTransform).(ScaleTransform.ScaleX)"));
                Storyboard.SetTarget(sizeAnimation2, SettingsBorder);
                Storyboard.SetTargetProperty(sizeAnimation2, new PropertyPath("(UIElement.RenderTransform).(ScaleTransform.ScaleY)"));
                var storyboard = new Storyboard();
                storyboard.Children.Add(sizeAnimation1);
                storyboard.Children.Add(sizeAnimation2);
                storyboard.Begin();
            }
            SettingsTabControl.SelectedIndex = 0;
            GeneralFrame.Navigate(SettingsTabs["General"]);
            {
                var opacityAnimation = new DoubleAnimation
                {
                    From = 0,
                    To = 1,
                    Duration = TimeSpan.FromSeconds(stabcontrolpage / 2),
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn }
                };
                var sizeAnimation1 = new DoubleAnimation
                {
                    From = 0.9,
                    To = 1,
                    Duration = TimeSpan.FromSeconds(stabcontrolpage),
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                };
                var sizeAnimation2 = new DoubleAnimation
                {
                    From = 0.9,
                    To = 1,
                    Duration = TimeSpan.FromSeconds(stabcontrolpage),
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                };
                Storyboard.SetTarget(opacityAnimation, GeneralFrame);
                Storyboard.SetTargetProperty(opacityAnimation, new PropertyPath(OpacityProperty));
                Storyboard.SetTarget(sizeAnimation1, GeneralFrame);
                Storyboard.SetTargetProperty(sizeAnimation1, new PropertyPath("(UIElement.RenderTransform).(ScaleTransform.ScaleX)"));
                Storyboard.SetTarget(sizeAnimation2, GeneralFrame);
                Storyboard.SetTargetProperty(sizeAnimation2, new PropertyPath("(UIElement.RenderTransform).(ScaleTransform.ScaleY)"));
                var storyboard = new Storyboard();
                storyboard.Children.Add(opacityAnimation);
                storyboard.Children.Add(sizeAnimation1);
                storyboard.Children.Add(sizeAnimation2);
                storyboard.Begin();
            }
        }
        private async void ExitAppBtn_Click(object sender, RoutedEventArgs e)
        {
            ExitAppBtn.Content = await LangSystem.GetLang("mainmenu.exiting");
            if (Process.GetProcessesByName("explorer").Length == 0)
            {
                Process.Start("explorer.exe");
            }
            await Task.Delay(1000);
            Application.Current.Shutdown();
        }
        #endregion

        #region Settings
        private async void SettingsBackBtn_Click(object sender, RoutedEventArgs e)
        {
            {
                var fadeAnimation = new DoubleAnimation
                {
                    From = 1,
                    To = 0,
                    Duration = TimeSpan.FromSeconds(animzoomspeeds),
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                };
                Storyboard.SetTarget(fadeAnimation, SettingsGrid);
                Storyboard.SetTargetProperty(fadeAnimation, new PropertyPath(OpacityProperty));
                var storyboard = new Storyboard();
                storyboard.Children.Add(fadeAnimation);
                storyboard.Begin();
            }
            {
                var sizeAnimation1 = new DoubleAnimation
                {
                    From = 1,
                    To = 1.1,
                    Duration = TimeSpan.FromSeconds(animzoomspeeds),
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn }
                };
                var sizeAnimation2 = new DoubleAnimation
                {
                    From = 1,
                    To = 1.1,
                    Duration = TimeSpan.FromSeconds(animzoomspeeds),
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn }
                };
                Storyboard.SetTarget(sizeAnimation1, SettingsBorder);
                Storyboard.SetTargetProperty(sizeAnimation1, new PropertyPath("(UIElement.RenderTransform).(ScaleTransform.ScaleX)"));
                Storyboard.SetTarget(sizeAnimation2, SettingsBorder);
                Storyboard.SetTargetProperty(sizeAnimation2, new PropertyPath("(UIElement.RenderTransform).(ScaleTransform.ScaleY)"));
                var storyboard = new Storyboard();
                storyboard.Children.Add(sizeAnimation1);
                storyboard.Children.Add(sizeAnimation2);
                storyboard.Begin();
            }
            await Task.Delay(300);
            SettingsGrid.Visibility = Visibility.Hidden;
        }

        private double stabcontrolpage = 0.3;
        private async void AboutFrame_Loaded(object sender, EventArgs e)
        {
            if (SettingsGrid.Visibility == Visibility.Visible)
            {
                AboutFrame.Navigate(SettingsTabs["About"]);
                {
                    var opacityAnimation = new DoubleAnimation
                    {
                        From = 0,
                        To = 1,
                        Duration = TimeSpan.FromSeconds(stabcontrolpage / 2),
                        EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn }
                    };
                    var sizeAnimation1 = new DoubleAnimation
                    {
                        From = 0.9,
                        To = 1,
                        Duration = TimeSpan.FromSeconds(stabcontrolpage),
                        EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                    };
                    var sizeAnimation2 = new DoubleAnimation
                    {
                        From = 0.9,
                        To = 1,
                        Duration = TimeSpan.FromSeconds(stabcontrolpage),
                        EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                    };
                    Storyboard.SetTarget(opacityAnimation, AboutFrame);
                    Storyboard.SetTargetProperty(opacityAnimation, new PropertyPath(OpacityProperty));
                    Storyboard.SetTarget(sizeAnimation1, AboutFrame);
                    Storyboard.SetTargetProperty(sizeAnimation1, new PropertyPath("(UIElement.RenderTransform).(ScaleTransform.ScaleX)"));
                    Storyboard.SetTarget(sizeAnimation2, AboutFrame);
                    Storyboard.SetTargetProperty(sizeAnimation2, new PropertyPath("(UIElement.RenderTransform).(ScaleTransform.ScaleY)"));
                    var storyboard = new Storyboard();
                    storyboard.Children.Add(opacityAnimation);
                    storyboard.Children.Add(sizeAnimation1);
                    storyboard.Children.Add(sizeAnimation2);
                    storyboard.Begin();
                }
            }

        }

        private void GeneralFrame_Loaded(object sender, EventArgs e)
        {
            if (SettingsGrid.Visibility == Visibility.Visible)
            {
                GeneralFrame.Navigate(SettingsTabs["General"]);
                {
                    var opacityAnimation = new DoubleAnimation
                    {
                        From = 0,
                        To = 1,
                        Duration = TimeSpan.FromSeconds(stabcontrolpage / 2),
                        EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn }
                    };
                    var sizeAnimation1 = new DoubleAnimation
                    {
                        From = 0.9,
                        To = 1,
                        Duration = TimeSpan.FromSeconds(stabcontrolpage),
                        EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                    };
                    var sizeAnimation2 = new DoubleAnimation
                    {
                        From = 0.9,
                        To = 1,
                        Duration = TimeSpan.FromSeconds(stabcontrolpage),
                        EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                    };
                    Storyboard.SetTarget(opacityAnimation, GeneralFrame);
                    Storyboard.SetTargetProperty(opacityAnimation, new PropertyPath(OpacityProperty));
                    Storyboard.SetTarget(sizeAnimation1, GeneralFrame);
                    Storyboard.SetTargetProperty(sizeAnimation1, new PropertyPath("(UIElement.RenderTransform).(ScaleTransform.ScaleX)"));
                    Storyboard.SetTarget(sizeAnimation2, GeneralFrame);
                    Storyboard.SetTargetProperty(sizeAnimation2, new PropertyPath("(UIElement.RenderTransform).(ScaleTransform.ScaleY)"));
                    var storyboard = new Storyboard();
                    storyboard.Children.Add(opacityAnimation);
                    storyboard.Children.Add(sizeAnimation1);
                    storyboard.Children.Add(sizeAnimation2);
                    storyboard.Begin();
                }
            }
        }

        private void PluginManagerFrame_Loaded(object sender, EventArgs e)
        {
            if (SettingsGrid.Visibility == Visibility.Visible)
            {
                PluginManagerFrame.Navigate(SettingsTabs["PluginManager"]);
                {
                    var opacityAnimation = new DoubleAnimation
                    {
                        From = 0,
                        To = 1,
                        Duration = TimeSpan.FromSeconds(stabcontrolpage / 2),
                        EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn }
                    };
                    var sizeAnimation1 = new DoubleAnimation
                    {
                        From = 0.9,
                        To = 1,
                        Duration = TimeSpan.FromSeconds(stabcontrolpage),
                        EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                    };
                    var sizeAnimation2 = new DoubleAnimation
                    {
                        From = 0.9,
                        To = 1,
                        Duration = TimeSpan.FromSeconds(stabcontrolpage),
                        EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                    };
                    Storyboard.SetTarget(opacityAnimation, PluginManagerFrame);
                    Storyboard.SetTargetProperty(opacityAnimation, new PropertyPath(OpacityProperty));
                    Storyboard.SetTarget(sizeAnimation1, PluginManagerFrame);
                    Storyboard.SetTargetProperty(sizeAnimation1, new PropertyPath("(UIElement.RenderTransform).(ScaleTransform.ScaleX)"));
                    Storyboard.SetTarget(sizeAnimation2, PluginManagerFrame);
                    Storyboard.SetTargetProperty(sizeAnimation2, new PropertyPath("(UIElement.RenderTransform).(ScaleTransform.ScaleY)"));
                    var storyboard = new Storyboard();
                    storyboard.Children.Add(opacityAnimation);
                    storyboard.Children.Add(sizeAnimation1);
                    storyboard.Children.Add(sizeAnimation2);
                    storyboard.Begin();
                }
            }
        }

        private async void SettingsSaveBtn_Click(object sender, RoutedEventArgs e)
        {
            await ConfigManager.SaveNewSettings();

            if (ConfigManager.NewVariables.RestartNeeded)
            {
                await App.RestartApp();
            }
        }
        #endregion

        #region Plugin Menu

        private async void EnterDownMenuPluginBtn_Click(object sender, RoutedEventArgs e)
        {
            {
                var translateAnimation = new DoubleAnimation
                {
                    From = 0,
                    To = -100,
                    Duration = TimeSpan.FromSeconds(zoomspeed),
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn }
                };
                var fadeAnimation = new DoubleAnimation
                {
                    From = 1,
                    To = 0,
                    Duration = TimeSpan.FromSeconds(fadespeed),
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                };
                Storyboard.SetTarget(translateAnimation, PluginGrid);
                Storyboard.SetTargetProperty(translateAnimation, new PropertyPath("(UIElement.RenderTransform).(TranslateTransform.Y)"));
                Storyboard.SetTarget(fadeAnimation, PluginGrid);
                Storyboard.SetTargetProperty(fadeAnimation, new PropertyPath(OpacityProperty));
                var storyboard = new Storyboard();
                storyboard.Children.Add(translateAnimation);
                storyboard.Children.Add(fadeAnimation);
                storyboard.Begin();
            }

            await Task.Delay(400);

            PluginGrid.Visibility = Visibility.Hidden;
            MenuPluginGrid.Visibility = Visibility.Visible;

            LeftMenuPluginBtn.IsEnabled = false;
            if (PluginList.Count == 1)
            {
                RightMenuPluginBtn.IsEnabled = false;
            }
            if (CarouselPluginList.Count != 0)
            {
                CarouselPluginFrame.Navigate(null);
                MenuPluginFrame.Navigate(PluginLoader.PluginModules[CarouselPluginList[0]].GetMain());
            }

            {
                var translateAnimation = new DoubleAnimation
                {
                    From = 100,
                    To = 0,
                    Duration = TimeSpan.FromSeconds(zoomspeed),
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                };
                var fadeAnimation = new DoubleAnimation
                {
                    From = 0,
                    To = 1,
                    Duration = TimeSpan.FromSeconds(fadespeed),
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn }
                };
                Storyboard.SetTarget(translateAnimation, MenuPluginGrid);
                Storyboard.SetTargetProperty(translateAnimation, new PropertyPath("(UIElement.RenderTransform).(TranslateTransform.Y)"));
                Storyboard.SetTarget(fadeAnimation, MenuPluginGrid);
                Storyboard.SetTargetProperty(fadeAnimation, new PropertyPath(OpacityProperty));
                var storyboard = new Storyboard();
                storyboard.Children.Add(translateAnimation);
                storyboard.Children.Add(fadeAnimation);
                storyboard.Begin();
            }
        }

        private async void ExitDownMenuPluginBtn_Click(object sender, RoutedEventArgs e)
        {
            {
                var translateAnimation = new DoubleAnimation
                {
                    From = 0,
                    To = 100,
                    Duration = TimeSpan.FromSeconds(zoomspeed),
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn }
                };
                var fadeAnimation = new DoubleAnimation
                {
                    From = 1,
                    To = 0,
                    Duration = TimeSpan.FromSeconds(fadespeed),
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                };
                Storyboard.SetTarget(translateAnimation, MenuPluginGrid);
                Storyboard.SetTargetProperty(translateAnimation, new PropertyPath("(UIElement.RenderTransform).(TranslateTransform.Y)"));
                Storyboard.SetTarget(fadeAnimation, MenuPluginGrid);
                Storyboard.SetTargetProperty(fadeAnimation, new PropertyPath(OpacityProperty));
                var storyboard = new Storyboard();
                storyboard.Children.Add(translateAnimation);
                storyboard.Children.Add(fadeAnimation);
                storyboard.Begin();
            }

            await Task.Delay(400);

            MenuPluginGrid.Visibility = Visibility.Hidden;
            PluginGrid.Visibility = Visibility.Visible;

            if (CarouselPluginList.Count != 0)
            {
                MenuPluginFrame.Navigate(null);
                CarouselPluginFrame.Navigate(PluginLoader.PluginModules[CarouselPluginList[0]].GetMain());
            }

            {
                var translateAnimation = new DoubleAnimation
                {
                    From = -100,
                    To = 0,
                    Duration = TimeSpan.FromSeconds(zoomspeed),
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                };
                var fadeAnimation = new DoubleAnimation
                {
                    From = 0,
                    To = 1,
                    Duration = TimeSpan.FromSeconds(fadespeed),
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn }
                };
                Storyboard.SetTarget(translateAnimation, PluginGrid);
                Storyboard.SetTargetProperty(translateAnimation, new PropertyPath("(UIElement.RenderTransform).(TranslateTransform.Y)"));
                Storyboard.SetTarget(fadeAnimation, PluginGrid);
                Storyboard.SetTargetProperty(fadeAnimation, new PropertyPath(OpacityProperty));
                var storyboard = new Storyboard();
                storyboard.Children.Add(translateAnimation);
                storyboard.Children.Add(fadeAnimation);
                storyboard.Begin();
            }
        }

        private int PluginIndex = 0;
        private async void LeftMenuPluginBtn_Click(object sender, RoutedEventArgs e)
        {
            PluginIndex--;
            if (PluginList.Count != 1)
            {
                RightMenuPluginBtn.IsEnabled = true;
            }
            if (PluginIndex == 0)
            {
                LeftMenuPluginBtn.IsEnabled = false;
            }

            {
                var fadeanimation = new DoubleAnimation
                {
                    From = 1,
                    To = 0,
                    Duration = TimeSpan.FromSeconds(fadespeed),
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                };
                var translateanimation = new DoubleAnimation
                {
                    From = 0,
                    To = 100,
                    Duration = TimeSpan.FromSeconds(zoomspeed),
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn }
                };
                Storyboard.SetTarget(fadeanimation, MenuPluginFrame);
                Storyboard.SetTargetProperty(fadeanimation, new PropertyPath(OpacityProperty));
                Storyboard.SetTarget(translateanimation, MenuPluginFrame);
                Storyboard.SetTargetProperty(translateanimation, new PropertyPath("(UIElement.RenderTransform).(TranslateTransform.X)"));
                var storyboard = new Storyboard();
                storyboard.Children.Add(fadeanimation);
                storyboard.Children.Add(translateanimation);
                storyboard.Begin();
            }

            await Task.Delay(400);
            CarouselPluginFrame.Navigate(null);
            MenuPluginFrame.Navigate(PluginLoader.PluginModules[PluginList[PluginIndex]].GetMain());

            {
                var fadeanimation = new DoubleAnimation
                {
                    From = 0,
                    To = 1,
                    Duration = TimeSpan.FromSeconds(fadespeed),
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn }
                };
                var translateanimation = new DoubleAnimation
                {
                    From = -100,
                    To = 0,
                    Duration = TimeSpan.FromSeconds(zoomspeed),
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                };
                Storyboard.SetTarget(fadeanimation, MenuPluginFrame);
                Storyboard.SetTargetProperty(fadeanimation, new PropertyPath(OpacityProperty));
                Storyboard.SetTarget(translateanimation, MenuPluginFrame);
                Storyboard.SetTargetProperty(translateanimation, new PropertyPath("(UIElement.RenderTransform).(TranslateTransform.X)"));
                var storyboard = new Storyboard();
                storyboard.Children.Add(fadeanimation);
                storyboard.Children.Add(translateanimation);
                storyboard.Begin();
            }
        }

        private async void RightMenuPluginBtn_Click(object sender, RoutedEventArgs e)
        {
            PluginIndex++;
            if (PluginList.Count != 1)
            {
                LeftMenuPluginBtn.IsEnabled = true;
            }
            if (PluginIndex == PluginList.Count - 1)
            {
                RightMenuPluginBtn.IsEnabled = false;
            }
            {
                var fadeanimation = new DoubleAnimation
                {
                    From = 1,
                    To = 0,
                    Duration = TimeSpan.FromSeconds(fadespeed),
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                };
                var translateanimation = new DoubleAnimation
                {
                    From = 0,
                    To = -100,
                    Duration = TimeSpan.FromSeconds(zoomspeed),
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn }
                };
                Storyboard.SetTarget(fadeanimation, MenuPluginFrame);
                Storyboard.SetTargetProperty(fadeanimation, new PropertyPath(OpacityProperty));
                Storyboard.SetTarget(translateanimation, MenuPluginFrame);
                Storyboard.SetTargetProperty(translateanimation, new PropertyPath("(UIElement.RenderTransform).(TranslateTransform.X)"));
                var storyboard = new Storyboard();
                storyboard.Children.Add(fadeanimation);
                storyboard.Children.Add(translateanimation);
                storyboard.Begin();
            }

            await Task.Delay(400);
            CarouselPluginFrame.Navigate(null);
            MenuPluginFrame.Navigate(PluginLoader.PluginModules[PluginList[PluginIndex]].GetMain());

            {
                var fadeanimation = new DoubleAnimation
                {
                    From = 0,
                    To = 1,
                    Duration = TimeSpan.FromSeconds(fadespeed),
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn }
                };
                var translateanimation = new DoubleAnimation
                {
                    From = 100,
                    To = 0,
                    Duration = TimeSpan.FromSeconds(zoomspeed),
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                };
                Storyboard.SetTarget(fadeanimation, MenuPluginFrame);
                Storyboard.SetTargetProperty(fadeanimation, new PropertyPath(OpacityProperty));
                Storyboard.SetTarget(translateanimation, MenuPluginFrame);
                Storyboard.SetTargetProperty(translateanimation, new PropertyPath("(UIElement.RenderTransform).(TranslateTransform.X)"));
                var storyboard = new Storyboard();
                storyboard.Children.Add(fadeanimation);
                storyboard.Children.Add(translateanimation);
                storyboard.Begin();
            }
        }

        private int CarouselIndex = 0;
        private async void Carousel_Tick(object sender, EventArgs e)
        {
            if (CarouselPluginList.Count != 0 && PluginLoader.PluginModules.Count != 0)
            {
                if (CarouselPluginList.Count > 1)
                {
                    if (CarouselIndex == CarouselPluginList.Count - 1)
                    {
                        CarouselIndex = 0;
                    }
                    else
                    {
                        CarouselIndex++;
                    }

                    {
                        var fadeanimation = new DoubleAnimation
                        {
                            From = 1,
                            To = 0,
                            Duration = TimeSpan.FromSeconds(0.5),
                            EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                        };
                        var zoomanimation1 = new DoubleAnimation
                        {
                            From = 1,
                            To = 1.1,
                            Duration = TimeSpan.FromSeconds(0.5),
                            EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn }
                        };
                        var zoomanimation2 = new DoubleAnimation
                        {
                            From = 1,
                            To = 1.1,
                            Duration = TimeSpan.FromSeconds(0.5),
                            EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn }
                        };
                        var translateanimation = new DoubleAnimation
                        {
                            From = 0,
                            To = 10,
                            Duration = TimeSpan.FromSeconds(0.5),
                            EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn }
                        };
                        Storyboard.SetTarget(fadeanimation, CarouselPluginFrame);
                        Storyboard.SetTargetProperty(fadeanimation, new PropertyPath(OpacityProperty));
                        Storyboard.SetTarget(zoomanimation1, CarouselPluginFrame);
                        Storyboard.SetTargetProperty(zoomanimation1, new PropertyPath("(UIElement.RenderTransform).Children[0].(ScaleTransform.ScaleX)"));
                        Storyboard.SetTarget(zoomanimation2, CarouselPluginFrame);
                        Storyboard.SetTargetProperty(zoomanimation2, new PropertyPath("(UIElement.RenderTransform).Children[0].(ScaleTransform.ScaleY)"));
                        Storyboard.SetTarget(translateanimation, CarouselPluginFrame);
                        Storyboard.SetTargetProperty(translateanimation, new PropertyPath("(UIElement.RenderTransform).Children[1].(TranslateTransform.Y)"));
                        var storyboard = new Storyboard();
                        storyboard.Children.Add(fadeanimation);
                        storyboard.Children.Add(zoomanimation1);
                        storyboard.Children.Add(zoomanimation2);
                        storyboard.Children.Add(translateanimation);
                        storyboard.Begin();
                    }

                    await Task.Delay(500);
                    MenuPluginFrame.Navigate(null);
                    CarouselPluginFrame.Navigate(PluginLoader.PluginModules[CarouselPluginList[CarouselIndex]].GetMain());

                    {
                        var fadeanimation = new DoubleAnimation
                        {
                            From = 0,
                            To = 1,
                            Duration = TimeSpan.FromSeconds(0.5),
                            EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn }
                        };
                        var zoomanimation1 = new DoubleAnimation
                        {
                            From = 0.9,
                            To = 1,
                            Duration = TimeSpan.FromSeconds(0.5),
                            EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                        };
                        var zoomanimation2 = new DoubleAnimation
                        {
                            From = 0.9,
                            To = 1,
                            Duration = TimeSpan.FromSeconds(0.5),
                            EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                        };
                        var translateanimation = new DoubleAnimation
                        {
                            From = -10,
                            To = 0,
                            Duration = TimeSpan.FromSeconds(0.5),
                            EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                        };
                        Storyboard.SetTarget(fadeanimation, CarouselPluginFrame);
                        Storyboard.SetTargetProperty(fadeanimation, new PropertyPath(OpacityProperty));
                        Storyboard.SetTarget(zoomanimation1, CarouselPluginFrame);
                        Storyboard.SetTargetProperty(zoomanimation1, new PropertyPath("(UIElement.RenderTransform).Children[0].(ScaleTransform.ScaleX)"));
                        Storyboard.SetTarget(zoomanimation2, CarouselPluginFrame);
                        Storyboard.SetTargetProperty(zoomanimation2, new PropertyPath("(UIElement.RenderTransform).Children[0].(ScaleTransform.ScaleY)"));
                        Storyboard.SetTarget(translateanimation, CarouselPluginFrame);
                        Storyboard.SetTargetProperty(translateanimation, new PropertyPath("(UIElement.RenderTransform).Children[1].(TranslateTransform.Y)"));
                        var storyboard = new Storyboard();
                        storyboard.Children.Add(fadeanimation);
                        storyboard.Children.Add(zoomanimation1);
                        storyboard.Children.Add(zoomanimation2);
                        storyboard.Children.Add(translateanimation);
                        storyboard.Begin();
                    }
                }
            }
        }

        private async void CarouselFrameClickGrid_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            if (CarouselPluginList.Count != 0)
            {
                {
                    var translateAnimation = new DoubleAnimation
                    {
                        From = 0,
                        To = -100,
                        Duration = TimeSpan.FromSeconds(zoomspeed),
                        EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn }
                    };
                    var fadeAnimation = new DoubleAnimation
                    {
                        From = 1,
                        To = 0,
                        Duration = TimeSpan.FromSeconds(fadespeed),
                        EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                    };
                    Storyboard.SetTarget(translateAnimation, PluginGrid);
                    Storyboard.SetTargetProperty(translateAnimation, new PropertyPath("(UIElement.RenderTransform).(TranslateTransform.Y)"));
                    Storyboard.SetTarget(fadeAnimation, PluginGrid);
                    Storyboard.SetTargetProperty(fadeAnimation, new PropertyPath(OpacityProperty));
                    var storyboard = new Storyboard();
                    storyboard.Children.Add(translateAnimation);
                    storyboard.Children.Add(fadeAnimation);
                    storyboard.Begin();
                }

                await Task.Delay(400);

                PluginGrid.Visibility = Visibility.Hidden;
                MenuPluginGrid.Visibility = Visibility.Visible;

                int index = 0;
                foreach (var plugin in PluginList)
                {
                    if (plugin == CarouselPluginList[CarouselIndex])
                    {
                        PluginIndex = index;
                        break;
                    }
                }
                if (index == 0)
                {
                    LeftMenuPluginBtn.IsEnabled = false;
                }
                if (index == PluginList.Count - 1)
                {
                    RightMenuPluginBtn.IsEnabled = false;
                }
                CarouselPluginFrame.Navigate(null);
                MenuPluginFrame.Navigate(PluginLoader.PluginModules[PluginList[index]].GetMain());

                {
                    var translateAnimation = new DoubleAnimation
                    {
                        From = 100,
                        To = 0,
                        Duration = TimeSpan.FromSeconds(zoomspeed),
                        EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                    };
                    var fadeAnimation = new DoubleAnimation
                    {
                        From = 0,
                        To = 1,
                        Duration = TimeSpan.FromSeconds(fadespeed),
                        EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn }
                    };
                    Storyboard.SetTarget(translateAnimation, MenuPluginGrid);
                    Storyboard.SetTargetProperty(translateAnimation, new PropertyPath("(UIElement.RenderTransform).(TranslateTransform.Y)"));
                    Storyboard.SetTarget(fadeAnimation, MenuPluginGrid);
                    Storyboard.SetTargetProperty(fadeAnimation, new PropertyPath(OpacityProperty));
                    var storyboard = new Storyboard();
                    storyboard.Children.Add(translateAnimation);
                    storyboard.Children.Add(fadeAnimation);
                    storyboard.Begin();
                }
            }
        }

        #endregion
    }
}