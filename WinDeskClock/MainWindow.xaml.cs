using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using WinDeskClock.Controls;
using WinDeskClock.Pages;
using WinDeskClock.Pages.Settings;
using WinDeskClock.Services;
using WinDeskClock.Utils;
using WinDeskClock.ViewModels;
using WinDeskClock.Views.Animation;

namespace WinDeskClock
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml.
    ///
    /// This class is now a View in the MVVM sense: all app state and business logic live in
    /// <see cref="ViewModel"/> and its children (see ViewModels/MainViewModel.cs). What's left
    /// here is what genuinely has to live in code-behind:
    ///  - Window-chrome operations that need an actual Window/HWND (fullscreen, kiosk, monitor
    ///    power, the splash screen sequence).
    ///  - Triggering the matching UiAnimator animation whenever a piece of ViewModel state changes
    ///    (the View decides *how* to animate a state change; the ViewModel only decides *that* it
    ///    changed).
    ///  - Wiring dynamically-created child views (AlarmCardView, plugin pages) into their host
    ///    panels, since that's inherently visual-tree work.
    /// </summary>
    public partial class MainWindow : Wpf.Ui.Controls.FluentWindow
    {
        public MainViewModel ViewModel { get; }

        private readonly IConfigService _config;
        private readonly ILocalizationService _localization;
        private readonly IPluginCatalogService _pluginCatalog;
        private readonly IShellInteropService _shellInterop;

        // ----- Animation timing constants -----
        private const double FadeSpeed = 0.2;
        private const double ZoomSpeed = 0.4;
        private const int AnimDelayMs = 400;
        private const double MiniClockSlideSpeed = 0.25;
        private const double SwFadeSpeed = 0.15, SwZoomSpeed = 0.30, SwTextSlideSpeed = 0.15;
        private const int SwFadeDelayMs = 150, SwZoomDelayMs = 300, SwTextDelayMs = 150;
        private const double TFadeSpeed = 0.30, TZoomSpeed = 0.30, TTextSlideSpeed = 0.15;
        private const int TFadeDelayMs = 300, TZoomDelayMs = 300, TTextDelayMs = 150;
        private const int GlobalAnimSpeedMs = 350;
        private const double SettingsTabAnimSpeed = 0.3;

        // ----- Window-level guard flags -----
        private bool _globalMenuBlocked = true;

        // ----- Splash screen -----
        private int _splashStep;
        private const int TotalSplashSteps = 11;

        // ----- Clock menu paging -----
        private readonly List<Grid> _clockMenuGrids = new();

        // ----- Settings tabs -----
        private readonly Dictionary<string, Page> _settingsTabs = new();

        // ----- Timers owned by the View -----
        private DispatcherTimer? _mouseHideTimer;
        private DispatcherTimer? _screenWakeUpTimer;
        private DispatcherTimer? _carouselTimer;

        // ----- Screen off/on -----
        private bool _turnOnSignal;
        private IntPtr _powerNotificationHandle;

        // ----- Alarm card hosting -----
        private readonly Dictionary<AlarmItemViewModel, AlarmCardView> _alarmCardsByViewModel = new();
        private AlarmItemViewModel? _expandedAlarmItem;

        public MainWindow(MainViewModel viewModel, IConfigService config, ILocalizationService localization, IPluginCatalogService pluginCatalog, IShellInteropService shellInterop)
        {
            InitializeComponent();

            ViewModel = viewModel;
            DataContext = viewModel;
            _config = config;
            _localization = localization;
            _pluginCatalog = pluginCatalog;
            _shellInterop = shellInterop;

            OverlayGrid.Visibility = Visibility.Visible;
            DownMenuClockGrid.Visibility = Visibility.Visible;
            DownMenuPluginGrid.Visibility = Visibility.Visible;

            // Prevent the window from being resized out of fullscreen while FullScreen or Kiosk is on.
            // Checking IsFullScreen alone (instead of the original's FullScreenBtn.IsChecked, which the
            // original also forced true/false from KioskModeEnable/Disable) meant this silently stopped
            // re-asserting fullscreen whenever only Kiosk mode was active.
            StateChanged += async (_, _) =>
            {
                await Task.Delay(100);
                if (ViewModel.IsFullScreenIndicatorChecked && WindowState != WindowState.Maximized)
                {
                    WindowState = WindowState.Normal;
                    WindowStyle = WindowStyle.None;
                    WindowState = WindowState.Maximized;
                }
            };

            // Auto layout for horizontal vs vertical screens - unchanged pure-layout logic.
            SizeChanged += async (_, _) =>
            {
                await Task.Delay(100);
                if (ViewModel.IsFullScreenIndicatorChecked && WindowState != WindowState.Maximized)
                {
                    WindowState = WindowState.Normal;
                    WindowStyle = WindowStyle.None;
                    WindowState = WindowState.Maximized;
                }

                if (ActualWidth >= ActualHeight)
                {
                    LeftGrid.SetValue(Grid.ColumnProperty, 0);
                    LeftGrid.SetValue(Grid.ColumnSpanProperty, 1);
                    LeftGrid.SetValue(Grid.RowProperty, 0);
                    LeftGrid.SetValue(Grid.RowSpanProperty, 3);
                    RightGrid.SetValue(Grid.ColumnProperty, 2);
                    RightGrid.SetValue(Grid.ColumnSpanProperty, 1);
                    RightGrid.SetValue(Grid.RowProperty, 0);
                    RightGrid.SetValue(Grid.RowSpanProperty, 3);
                    GlobalMenuSep.SetValue(Grid.ColumnProperty, 1);
                    GlobalMenuSep.SetValue(Grid.ColumnSpanProperty, 1);
                    GlobalMenuSep.SetValue(Grid.RowProperty, 0);
                    GlobalMenuSep.SetValue(Grid.RowSpanProperty, 3);
                    GlobalMenuBtn.SetValue(Grid.ColumnProperty, 1);
                    GlobalMenuBtn.SetValue(Grid.ColumnSpanProperty, 1);
                    GlobalMenuBtn.SetValue(Grid.RowProperty, 0);
                    GlobalMenuBtn.SetValue(Grid.RowSpanProperty, 3);
                }
                else
                {
                    LeftGrid.SetValue(Grid.ColumnProperty, 0);
                    LeftGrid.SetValue(Grid.ColumnSpanProperty, 3);
                    LeftGrid.SetValue(Grid.RowProperty, 0);
                    LeftGrid.SetValue(Grid.RowSpanProperty, 1);
                    RightGrid.SetValue(Grid.ColumnProperty, 0);
                    RightGrid.SetValue(Grid.ColumnSpanProperty, 3);
                    RightGrid.SetValue(Grid.RowProperty, 2);
                    RightGrid.SetValue(Grid.RowSpanProperty, 1);
                    GlobalMenuSep.SetValue(Grid.ColumnProperty, 0);
                    GlobalMenuSep.SetValue(Grid.ColumnSpanProperty, 3);
                    GlobalMenuSep.SetValue(Grid.RowProperty, 1);
                    GlobalMenuSep.SetValue(Grid.RowSpanProperty, 1);
                    GlobalMenuBtn.SetValue(Grid.ColumnProperty, 0);
                    GlobalMenuBtn.SetValue(Grid.ColumnSpanProperty, 3);
                    GlobalMenuBtn.SetValue(Grid.RowProperty, 1);
                    GlobalMenuBtn.SetValue(Grid.RowSpanProperty, 1);
                }
            };

            WireViewModelReactions();

            Loaded += MainWindow_Loaded;
        }

        /// <summary>WndProc hook: keeps the app's screen-off state in sync if Windows itself reports the display turned back on.</summary>
        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == WindowsAPI.User32.WM_POWERBROADCAST && wParam.ToInt32() == WindowsAPI.User32.PBT_POWERSETTINGCHANGE)
            {
                var powerSetting = (WindowsAPI.User32.PowerBroadcastSetting)Marshal.PtrToStructure(lParam, typeof(WindowsAPI.User32.PowerBroadcastSetting))!;
                if (powerSetting.PowerSetting == WindowsAPI.User32.CONSOLE_DISPLAY_STATE && powerSetting.Data == 1)
                {
                    _ = Log.Info("System screen turned on");
                    _ = TurnScreenOnAsync();
                }
            }
            return IntPtr.Zero;
        }

        /// <summary>
        /// Subscribes to every ViewModel PropertyChanged we need to react to visually. Kept in one
        /// place so the "what triggers what animation" mapping is easy to scan.
        /// </summary>
        private void WireViewModelReactions()
        {
            ViewModel.PropertyChanged += (_, e) =>
            {
                switch (e.PropertyName)
                {
                    case nameof(MainViewModel.IsFullScreen): OnFullScreenChanged(ViewModel.IsFullScreen); break;
                    case nameof(MainViewModel.IsKioskMode): OnKioskModeChanged(ViewModel.IsKioskMode); break;
                    case nameof(MainViewModel.IsScreenOff): OnScreenOffChanged(ViewModel.IsScreenOff); break;
                    case nameof(MainViewModel.IsGlobalMenuOpen): OnGlobalMenuOpenChanged(ViewModel.IsGlobalMenuOpen); break;
                    case nameof(MainViewModel.IsClockMenuOpen): OnClockMenuOpenChanged(ViewModel.IsClockMenuOpen); break;
                    case nameof(MainViewModel.IsSettingsOpen): OnSettingsOpenChanged(ViewModel.IsSettingsOpen); break;
                    case nameof(MainViewModel.IsMirrorOpen): OnMirrorOpenChanged(ViewModel.IsMirrorOpen); break;
                    case nameof(MainViewModel.IsPluginMenuOpen): OnPluginMenuOpenChanged(ViewModel.IsPluginMenuOpen); break;
                }
            };

            ViewModel.Alarms.ScreenWakeRequested += async (_, _) => await TurnScreenOnAsync();
            // Pure reordering (Move) is driven explicitly from OnAlarmExpansionChanged/FinishCollapse
            // instead of reactively here, so a deferred sort can't race the still-running close
            // animation that triggered it - only additions/removals need to react immediately.
            ViewModel.Alarms.Alarms.CollectionChanged += (_, e) =>
            {
                if (e.Action != System.Collections.Specialized.NotifyCollectionChangedAction.Move)
                {
                    SyncAlarmCards();
                }
            };

            WireStopwatchDigitAnimations();
            WireTimerDigitAnimations();
            WireTimerOverlayReactions();
            WireAlarmAlertReactions();
            WireSettingsReactions();
            WireMiniClockDigitAnimation();
        }

        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            _ = Log.Info("MainWindow loaded, showing splash screen...");
            WindowState = WindowState.Maximized;
            RootTitleBar.Visibility = Visibility.Hidden;
            MainGrid.Visibility = Visibility.Hidden;
            SplashLoadingText.Text = "Hi !";
            SplashPercentageText.Text = "0%";
            SplashProgressBar.Value = 0;
            SplashVersionText.Text = "Version " + App.AppVersion;
            _splashStep = 0;

            await Task.Delay(300);
            SplashGrid.Visibility = Visibility.Visible;
            UiAnimator.Fade(SplashGrid, 0, 1, 0.2, new QuadraticEase { EasingMode = EasingMode.EaseIn });
            UiAnimator.Scale(SplashGrid, 0.9, 1, 0.5, new QuinticEase { EasingMode = EasingMode.EaseOut });

            await Task.Delay(700);

            AdvanceSplash("Applying arguments...");
            _ = Log.Info("Applying startup arguments...");
            if (App.StartupOptions.FullScreen)
            {
                ViewModel.IsFullScreen = true;
            }
            if (App.StartupOptions.KioskMode)
            {
                ViewModel.IsKioskMode = true;
            }

            AdvanceSplash("Checking files...");
            await _config.InitializeAsync();

            AdvanceSplash("Loading language...");
            await _localization.InitializeAsync();

            AdvanceSplash("Loading UI (ClockMenu)...");
            _clockMenuGrids.Add(AlarmGrid);
            _clockMenuGrids.Add(StopwatchGrid);
            _clockMenuGrids.Add(TimerGrid);

            AdvanceSplash("Loading UI (Settings)...");
            _settingsTabs["General"] = new General(ViewModel.Settings.General, _localization);
            _settingsTabs["PluginManager"] = new PluginManager(ViewModel.Settings.PluginManager);
            _settingsTabs["About"] = new About(ViewModel.Settings.About);

            AdvanceSplash("Loading alarms...");
            await ViewModel.Alarms.LoadAsync();
            SyncAlarmCards();

            AdvanceSplash("Loading plugins...");
            await _pluginCatalog.LoadPluginsAsync();
            ViewModel.PluginCarousel.Load();

            AdvanceSplash("Loading UI...");
            ApplyStaticLanguageTexts();
            ApplyBlurSetting();
            ClockFrame.Navigate(_config.CurrentClockFbxStyle
                ? new Clocks.FbxClock(ViewModel.Clock)
                : new Clocks.FluentClock(ViewModel.Clock));
            RefreshPluginCarouselPlaceholder();

            AdvanceSplash("Creating timers...");
            CreateViewTimers();
            ViewModel.StartClocks();

            AdvanceSplash("Ready !");
            await Task.Delay(700);

            UiAnimator.Fade(SplashGrid, 1, 0, 0.2, new QuadraticEase { EasingMode = EasingMode.EaseOut }, beginTimeSeconds: 0.3);
            UiAnimator.Scale(SplashGrid, 1, 1.1, 0.5, new QuinticEase { EasingMode = EasingMode.EaseIn });

            await Task.Delay(500);

            SplashGrid.Visibility = Visibility.Hidden;
            RootTitleBar.Visibility = Visibility.Visible;
            MainGrid.Visibility = Visibility.Visible;

            UiAnimator.Fade(MainGrid, 0, 1, 0.2, new QuadraticEase { EasingMode = EasingMode.EaseIn });
            UiAnimator.Scale(MainGrid, 0.9, 1, 1, new QuinticEase { EasingMode = EasingMode.EaseOut });
            UiAnimator.Translate(RootTitleBar, SlideAxis.Y, -100, 0, 0.5, new QuinticEase { EasingMode = EasingMode.EaseOut });

            MouseMove += MainWindow_MouseMove;
        }

        private void AdvanceSplash(string message)
        {
            _splashStep++;
            SplashProgressBar.Value = _splashStep * 100 / TotalSplashSteps;
            SplashPercentageText.Text = _splashStep * 100 / TotalSplashSteps + "%";
            SplashLoadingText.Text = message;
        }

        private void ApplyBlurSetting()
        {
            if (_config.CurrentBlurEffectEnabled)
            {
                return;
            }

            _ = Log.Info("Disabling blur effect");
            foreach (var rect in new[] { AlarmAlertBlur, TimeUpBlur, SettingsBlur, GlobalMenuBlur, MirrorBlur })
            {
                rect.Visibility = Visibility.Collapsed;
                rect.Fill = new SolidColorBrush(Colors.Transparent);
                rect.Effect = null;
                rect.CacheMode = null;
            }
            WindowBackdropType = Wpf.Ui.Controls.WindowBackdropType.None;
        }

        private void ApplyStaticLanguageTexts()
        {
            _ = Log.Info("Applying language to UI elements");
            BackBtn.Content = _localization.GetString("mainmenu.back");
            ScreenOffBtn.Content = _localization.GetString("mainmenu.screenoff");
            FullScreenBtnText.Text = _localization.GetString("mainmenu.fullscreen");
            KioskModeBtnText.Text = _localization.GetString("mainmenu.kiosk");
            SettingsBtn.Content = _localization.GetString("mainmenu.settings");
            ExitAppBtn.Content = _localization.GetString("mainmenu.exit");
            AlarmAlertStopBtn.Content = _localization.GetString("alarm.stop");
            AlarmAlertText.Text = _localization.GetString("alarm.titlename");
            TimeUpStopBtn.Content = _localization.GetString("timer.stop");
            TimeUpText.Text = _localization.GetString("timer.timeup");
            AboutTabItemHeader.Text = _localization.GetString("settings.about.titlename");
            PMTabItemHeader.Text = _localization.GetString("settings.pluginmanager.titlename");
            GeneralTabItemHeader.Text = _localization.GetString("settings.general.titlename");
            SettingsSaveBtn.Content = _localization.GetString("settings.save");
            SettingsTitleText.Text = _localization.GetString("settings.titlename");
            ResetTimerBtn.Content = _localization.GetString("timer.reset");
            PauseTimerBtn.Content = _localization.GetString("timer.pause");
            StartTimerBtn.Content = _localization.GetString("timer.start");
            LapStopwatchBtn.Content = _localization.GetString("stopwatch.lap");
            ResetStopwatchBtn.Content = _localization.GetString("stopwatch.reset");
            PauseStopwatchBtn.Content = _localization.GetString("stopwatch.pause");
            StartStopwatchBtn.Content = _localization.GetString("stopwatch.start");
            AddAlarmBtn.Content = _localization.GetString("alarm.add");
            NoAlarmText.Text = _localization.GetString("alarm.noalarm");
            MirrorTitleText.Text = _localization.GetString("mirror.titlename");
            MirrorBtn.Content = _localization.GetString("mirror.titlename");
            MirrorBackBtn.Content = _localization.GetString("mirror.back");
        }

        private void CreateViewTimers()
        {
            _mouseHideTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(3) };
            _mouseHideTimer.Tick += (_, _) =>
            {
                _mouseHideTimer!.Stop();
                if (ViewModel.IsFullScreen)
                {
                    Cursor = System.Windows.Input.Cursors.None;
                }
            };
            _mouseHideTimer.Start();

            _screenWakeUpTimer = new DispatcherTimer { Interval = TimeSpan.FromMinutes(1) };
            _screenWakeUpTimer.Tick += async (_, _) =>
            {
                if (!_config.CurrentScreenAutoWakeUp)
                {
                    return;
                }
                DateTime now = DateTime.Now;
                var wakeTime = _config.CurrentScreenAutoWakeUpTime;
                if (now.Hour == wakeTime.Hour && now.Minute == wakeTime.Minute)
                {
                    await TurnScreenOnAsync();
                }
                _screenWakeUpTimer!.Interval = TimeSpan.FromSeconds(60 - DateTime.Now.Second);
            };
            _screenWakeUpTimer.Start();

            _carouselTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(Math.Max(1, _config.CurrentCarouselDelaySeconds)) };
            _carouselTimer.Tick += async (_, _) => await AdvanceCarouselAsync();
            _carouselTimer.Start();
        }

        private void MainWindow_MouseMove(object sender, MouseEventArgs e)
        {
            if (ViewModel.IsFullScreen)
            {
                Cursor = System.Windows.Input.Cursors.Arrow;
                _mouseHideTimer?.Start();
            }
            ViewModel.NotifyUserActivity();
        }

        #region Fullscreen / Kiosk mode

        /// <summary>
        /// Marking e.Handled here was meant to stop the ToggleButton's own click-to-toggle from also
        /// firing, but PreviewMouseUp runs *before* ButtonBase's own release handling - so Handled=true
        /// suppressed that too, including its ReleaseMouseCapture() call. The button had captured the
        /// mouse on press and, with its release handling skipped, never released it: every click
        /// anywhere afterwards kept landing on this button instead of whatever was actually under the
        /// cursor. The original never fought the button's own click lifecycle at all; it let the click
        /// complete normally and resynced FullScreenBtn.IsChecked to its own tracked state 10ms later.
        /// </summary>
        private async void FullScreenBtn_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (_globalMenuBlocked) return;
            await Task.Delay(10);
            ViewModel.IsFullScreen = !ViewModel.IsFullScreen;
        }

        private async void KioskModeBtn_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (_globalMenuBlocked) return;
            await Task.Delay(10);
            ViewModel.IsKioskMode = !ViewModel.IsKioskMode;
        }

        private void OnFullScreenChanged(bool enabled)
        {
            _ = Log.Info(enabled ? "Enabling full screen mode" : "Disabling full screen mode");
            WindowState = WindowState.Normal;
            WindowStyle = enabled ? WindowStyle.None : WindowStyle.SingleBorderWindow;
            WindowState = WindowState.Maximized;
            Topmost = enabled;
            RootTitleBar.ShowMaximize = !enabled;
            RootTitleBar.ShowMinimize = !enabled;
            RootTitleBar.Width = enabled ? 50 : double.NaN;
            RootTitleBar.Padding = enabled ? new Thickness(0) : new Thickness(0, 0, 30, 0);
            RootTitleBar.HorizontalAlignment = enabled ? HorizontalAlignment.Right : HorizontalAlignment.Stretch;
            RootTitleBar.Title = enabled ? "" : "WinDeskClock";
            AppTitleText.Visibility = enabled ? Visibility.Visible : Visibility.Hidden;
        }

        private void OnKioskModeChanged(bool enabled)
        {
            _ = Log.Info(enabled ? "Enabling kiosk mode" : "Disabling kiosk mode");
            WindowState = WindowState.Normal;
            WindowStyle = enabled ? WindowStyle.None : WindowStyle.SingleBorderWindow;
            WindowState = WindowState.Maximized;
            Topmost = enabled;
            RootTitleBar.ShowMaximize = !enabled;
            RootTitleBar.ShowMinimize = !enabled;
            RootTitleBar.ShowClose = !enabled;
            RootTitleBar.Visibility = enabled ? Visibility.Hidden : Visibility.Visible;
            AppTitleText.Visibility = enabled ? Visibility.Visible : Visibility.Hidden;
            RootTitleBar.Width = enabled ? 0 : double.NaN;

            if (enabled)
            {
                _shellInterop.HideTaskbar();
                _shellInterop.HideDesktopIcons();
                Deactivated += PreventKioskEscape;
            }
            else
            {
                _shellInterop.ShowTaskbar();
                _shellInterop.ShowDesktopIcons();
                Deactivated -= PreventKioskEscape;
            }
        }

        private async void PreventKioskEscape(object? sender, EventArgs e)
        {
            _ = Log.Warning("Window deactivated, preventing escape from kiosk mode");
            await Task.Delay(100);
            WindowsAPI.User32.KeybdEvent(WindowsAPI.User32.VK_ESC, 0, 0, UIntPtr.Zero);
            WindowsAPI.User32.KeybdEvent(WindowsAPI.User32.VK_ESC, 0, WindowsAPI.User32.KEYEVENTF_KEYUP, UIntPtr.Zero);
            Activate();
        }

        #endregion

        #region Screen off / on

        private async Task TurnScreenOffAsync()
        {
            _ = Log.Info("Turning screen off...");
            var hwnd = new WindowInteropHelper(this).Handle;
            HwndSource.FromHwnd(hwnd).AddHook(WndProc);
            _powerNotificationHandle = _shellInterop.RegisterPowerNotifications(hwnd);

            await PlayScreenOffAnimationAsync(_config.CurrentScreenOnOffEffect);
            await Task.Delay(200);
            _shellInterop.RequestMonitorPower(turnOn: false);
            _turnOnSignal = false;

            while (!_turnOnSignal)
            {
                await Task.Delay(100);
            }

            _ = Log.Info("Turning screen on...");
            _shellInterop.RequestMonitorPower(turnOn: true);
            _shellInterop.UnregisterPowerNotifications(_powerNotificationHandle);
            _powerNotificationHandle = IntPtr.Zero;

            await Task.Delay(1000);
            await PlayScreenOnAnimationAsync(_config.CurrentScreenOnOffEffect);

            ViewModel.IsScreenOff = false;
        }

        private async Task TurnScreenOnAsync()
        {
            _shellInterop.NudgeMouseCursor();
            _shellInterop.RequestMonitorPower(turnOn: true);
            _turnOnSignal = true;
            await Task.CompletedTask;
        }

        private void OnScreenOffChanged(bool isOff)
        {
            if (isOff)
            {
                _ = TurnScreenOffAsync();
            }
        }

        private async void ScreenOffBtn_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (_globalMenuBlocked) return;
            CloseGlobalMenu();
            await Task.Delay(150);
            ViewModel.IsScreenOff = true;
        }

        private async Task PlayScreenOffAnimationAsync(string animationType)
        {
            switch (animationType)
            {
                case "none":
                    NoneRect.Visibility = Visibility.Visible;
                    break;
                case "crt":
                    CRTRectUp.Visibility = Visibility.Visible;
                    CRTRectDown.Visibility = Visibility.Visible;
                    // ScaleY only: the bars close vertically while staying full-width, and AppGrid
                    // compresses vertically into the middle line - ScaleX on all three stays at its
                    // XAML default of 1 throughout. Using the both-axes Scale/ScaleTransform helper
                    // here (as the previous version did) also drags ScaleX from 0, so the bars/content
                    // shrunk inward horizontally too instead of staying full width.
                    UiAnimator.ScaleAxis((ScaleTransform)CRTRectUp.RenderTransform, SlideAxis.Y, 0, 1, 0.2, new QuadraticEase { EasingMode = EasingMode.EaseIn });
                    UiAnimator.ScaleAxis((ScaleTransform)CRTRectDown.RenderTransform, SlideAxis.Y, 0, 1, 0.2, new QuadraticEase { EasingMode = EasingMode.EaseIn });
                    UiAnimator.ScaleAxis((ScaleTransform)AppGrid.RenderTransform, SlideAxis.Y, 1, 0, 0.2, new QuadraticEase { EasingMode = EasingMode.EaseIn });
                    await Task.Delay(150);
                    CRTRectMiddle.Visibility = Visibility.Visible;
                    await Task.Delay(50);
                    // ScaleX only here: the middle line closes horizontally (its height is fixed at 3px in XAML).
                    UiAnimator.ScaleAxis((ScaleTransform)CRTRectMiddle.RenderTransform, SlideAxis.X, 1, 0, 0.2, new QuadraticEase { EasingMode = EasingMode.EaseIn });
                    await Task.Delay(200);
                    break;
                case "fade":
                    FadeRect.Visibility = Visibility.Visible;
                    UiAnimator.Fade(FadeGrid, 0, 1, 0.5, new QuadraticEase { EasingMode = EasingMode.EaseIn });
                    await Task.Delay(500);
                    break;
            }
        }

        private async Task PlayScreenOnAnimationAsync(string animationType)
        {
            switch (animationType)
            {
                case "none":
                    NoneRect.Visibility = Visibility.Hidden;
                    break;
                case "crt":
                    UiAnimator.ScaleAxis((ScaleTransform)CRTRectMiddle.RenderTransform, SlideAxis.X, 0, 1, 0.2, new QuadraticEase { EasingMode = EasingMode.EaseOut });
                    await Task.Delay(200);
                    CRTRectMiddle.Visibility = Visibility.Hidden;
                    UiAnimator.ScaleAxis((ScaleTransform)CRTRectUp.RenderTransform, SlideAxis.Y, 1, 0, 0.2, new QuadraticEase { EasingMode = EasingMode.EaseOut });
                    UiAnimator.ScaleAxis((ScaleTransform)CRTRectDown.RenderTransform, SlideAxis.Y, 1, 0, 0.2, new QuadraticEase { EasingMode = EasingMode.EaseOut });
                    UiAnimator.ScaleAxis((ScaleTransform)AppGrid.RenderTransform, SlideAxis.Y, 0, 1, 0.2, new QuadraticEase { EasingMode = EasingMode.EaseOut });
                    CRTRectUp.Visibility = Visibility.Hidden;
                    CRTRectDown.Visibility = Visibility.Hidden;
                    await Task.Delay(200);
                    break;
                case "fade":
                    UiAnimator.Fade(FadeGrid, 1, 0, 0.5, new QuadraticEase { EasingMode = EasingMode.EaseOut });
                    await Task.Delay(500);
                    FadeRect.Visibility = Visibility.Hidden;
                    break;
            }
        }

        #endregion

        #region Clock menu

        private void WireMiniClockDigitAnimation()
        {
            ViewModel.MiniClock.PropertyChanged += (_, e) =>
            {
                switch (e.PropertyName)
                {
                    case nameof(MiniClockViewModel.Hour):
                        _ = UiAnimator.SlideReplaceAsync(MiniClockHour, SlideAxis.Y, -180, 180, () => MiniClockHour.Text = ViewModel.MiniClock.Hour,
                            MiniClockSlideSpeed, MiniClockSlideSpeed, 25, new QuinticEase { EasingMode = EasingMode.EaseIn }, new QuinticEase { EasingMode = EasingMode.EaseOut });
                        break;
                    case nameof(MiniClockViewModel.Minute):
                        _ = UiAnimator.SlideReplaceAsync(MiniClockMinute, SlideAxis.Y, -180, 180, () => MiniClockMinute.Text = ViewModel.MiniClock.Minute,
                            MiniClockSlideSpeed, MiniClockSlideSpeed, 25, new QuinticEase { EasingMode = EasingMode.EaseIn }, new QuinticEase { EasingMode = EasingMode.EaseOut });
                        break;
                }
            };
        }

        private void EnterDownMenuClockBtn_Click(object sender, RoutedEventArgs e)
        {
            if (ViewModel.CurrentClockTab == ClockMenuTab.Alarm)
            {
                ViewModel.Alarms.RefreshEtaTexts();
            }
            ViewModel.IsClockMenuOpen = true;
        }

        private void ExitDownMenuClockBtn_Click(object sender, RoutedEventArgs e) => ViewModel.IsClockMenuOpen = false;

        private async void OnClockMenuOpenChanged(bool isOpen)
        {
            if (isOpen)
            {
                UiAnimator.Fade(ClockGrid, 1, 0, FadeSpeed, new QuadraticEase { EasingMode = EasingMode.EaseOut }, beginTimeSeconds: ZoomSpeed - FadeSpeed);
                UiAnimator.Scale(ClockGrid, 1, 0, ZoomSpeed, new QuinticEase { EasingMode = EasingMode.EaseIn });

                await Task.Delay(AnimDelayMs);

                ClockGrid.Visibility = Visibility.Hidden;
                MenuClockGrid.Visibility = Visibility.Visible;

                UiAnimator.Fade(MenuClockGrid, 0, 1, FadeSpeed, new QuadraticEase { EasingMode = EasingMode.EaseIn });
                UiAnimator.Translate(MenuClockGrid, SlideAxis.Y, 100, 0, ZoomSpeed, new QuinticEase { EasingMode = EasingMode.EaseOut });

                MiniClockBorder.Visibility = Visibility.Visible;
                ExitDownMenuClockBtn.Visibility = Visibility.Visible;

                UiAnimator.Fade(MiniClockBorder, 0, 1, FadeSpeed, new QuadraticEase { EasingMode = EasingMode.EaseIn });
                UiAnimator.Fade(ExitDownMenuClockBtn, 0, 1, FadeSpeed, new QuadraticEase { EasingMode = EasingMode.EaseIn });
                UiAnimator.Scale(MiniClockBorder, 0, 1, ZoomSpeed, new QuinticEase { EasingMode = EasingMode.EaseOut });
            }
            else
            {
                UiAnimator.Fade(MenuClockGrid, 1, 0, FadeSpeed, new QuadraticEase { EasingMode = EasingMode.EaseOut }, beginTimeSeconds: ZoomSpeed - FadeSpeed);
                UiAnimator.Translate(MenuClockGrid, SlideAxis.Y, 0, 100, ZoomSpeed, new QuinticEase { EasingMode = EasingMode.EaseIn });

                UiAnimator.Fade(MiniClockBorder, 1, 0, FadeSpeed, new QuadraticEase { EasingMode = EasingMode.EaseOut }, beginTimeSeconds: ZoomSpeed - FadeSpeed);
                UiAnimator.Fade(ExitDownMenuClockBtn, 1, 0, FadeSpeed, new QuadraticEase { EasingMode = EasingMode.EaseOut }, beginTimeSeconds: ZoomSpeed - FadeSpeed);
                UiAnimator.Scale(MiniClockBorder, 1, 0, ZoomSpeed, new QuinticEase { EasingMode = EasingMode.EaseIn });

                await Task.Delay(AnimDelayMs);

                MenuClockGrid.Visibility = Visibility.Hidden;
                MiniClockBorder.Visibility = Visibility.Hidden;
                ExitDownMenuClockBtn.Visibility = Visibility.Hidden;
                ClockGrid.Visibility = Visibility.Visible;

                UiAnimator.Fade(ClockGrid, 0, 1, FadeSpeed, new QuadraticEase { EasingMode = EasingMode.EaseIn });
                UiAnimator.Scale(ClockGrid, 0, 1, ZoomSpeed, new QuinticEase { EasingMode = EasingMode.EaseOut });
            }
        }

        private async void LeftMenuClockBtn_Click(object sender, RoutedEventArgs e)
        {
            var previousTab = ViewModel.CurrentClockTab;
            ViewModel.CurrentClockTab--;
            RightMenuClockBtn.IsEnabled = true;
            if (ViewModel.CurrentClockTab == ClockMenuTab.Alarm)
            {
                ViewModel.Alarms.RefreshEtaTexts();
                LeftMenuClockBtn.IsEnabled = false;
            }
            await SlideClockTabAsync(_clockMenuGrids[(int)previousTab], _clockMenuGrids[(int)ViewModel.CurrentClockTab], goingLeft: true);
        }

        private async void RightMenuClockBtn_Click(object sender, RoutedEventArgs e)
        {
            var previousTab = ViewModel.CurrentClockTab;
            ViewModel.CurrentClockTab++;
            LeftMenuClockBtn.IsEnabled = true;
            if ((int)ViewModel.CurrentClockTab == _clockMenuGrids.Count - 1)
            {
                RightMenuClockBtn.IsEnabled = false;
            }
            await SlideClockTabAsync(_clockMenuGrids[(int)previousTab], _clockMenuGrids[(int)ViewModel.CurrentClockTab], goingLeft: false);
        }

        private async Task SlideClockTabAsync(Grid previousGrid, Grid nextGrid, bool goingLeft)
        {
            double outDirection = goingLeft ? 100 : -100;
            UiAnimator.Fade(previousGrid, 1, 0, FadeSpeed, new QuadraticEase { EasingMode = EasingMode.EaseOut }, beginTimeSeconds: ZoomSpeed - FadeSpeed);
            UiAnimator.Translate(previousGrid, SlideAxis.X, 0, outDirection, ZoomSpeed, new QuinticEase { EasingMode = EasingMode.EaseIn });

            await Task.Delay(AnimDelayMs);
            previousGrid.Visibility = Visibility.Hidden;

            nextGrid.Visibility = Visibility.Visible;
            UiAnimator.Fade(nextGrid, 0, 1, FadeSpeed, new QuadraticEase { EasingMode = EasingMode.EaseIn });
            UiAnimator.Translate(nextGrid, SlideAxis.X, -outDirection, 0, ZoomSpeed, new QuinticEase { EasingMode = EasingMode.EaseOut });
        }

        #endregion

        #region Stopwatch

        private void WireStopwatchDigitAnimations()
        {
            ViewModel.Stopwatch.PropertyChanged += (_, e) =>
            {
                var vm = ViewModel.Stopwatch;
                switch (e.PropertyName)
                {
                    case nameof(StopwatchViewModel.H1): AnimateStopwatchDigit(StopwatchTextH1, vm.H1); break;
                    case nameof(StopwatchViewModel.H2): AnimateStopwatchDigit(StopwatchTextH2, vm.H2); break;
                    case nameof(StopwatchViewModel.M1): AnimateStopwatchDigit(StopwatchTextM1, vm.M1); break;
                    case nameof(StopwatchViewModel.M2): AnimateStopwatchDigit(StopwatchTextM2, vm.M2); break;
                    case nameof(StopwatchViewModel.S1): AnimateStopwatchDigit(StopwatchTextS1, vm.S1); break;
                    case nameof(StopwatchViewModel.S2): AnimateStopwatchDigit(StopwatchTextS2, vm.S2); break;
                    // Milliseconds change ~100x/s so they snap while counting; like the original they only
                    // slide when the stopwatch is being reset.
                    case nameof(StopwatchViewModel.MS1): SetStopwatchMillisecondDigit(StopwatchTextMS1, vm.MS1); break;
                    case nameof(StopwatchViewModel.MS2): SetStopwatchMillisecondDigit(StopwatchTextMS2, vm.MS2); break;
                    case nameof(StopwatchViewModel.ProgressValue): StopwatchProgressBar.Value = vm.ProgressValue; break;
                    case nameof(StopwatchViewModel.IsProgressFlipped):
                        StopwatchProgressBar.RenderTransform = new ScaleTransform(vm.IsProgressFlipped ? -1 : 1, 1);
                        break;
                }
            };
        }

        private void SetStopwatchMillisecondDigit(TextBlock target, string newText)
        {
            if (_stopwatchResetting)
            {
                AnimateStopwatchDigit(target, newText);
            }
            else
            {
                target.Text = newText;
            }
        }

        private static void AnimateStopwatchDigit(TextBlock target, string newText) =>
            _ = UiAnimator.SlideReplaceAsync(target, SlideAxis.Y, -50, 50, () => target.Text = newText,
                SwTextSlideSpeed, SwTextSlideSpeed, SwTextDelayMs, new QuinticEase { EasingMode = EasingMode.EaseIn }, new QuinticEase { EasingMode = EasingMode.EaseOut });

        private async void StartStopwatchBtn_Click(object sender, RoutedEventArgs e)
        {
            bool firstStart = !ViewModel.Stopwatch.IsRunning;
            ViewModel.Stopwatch.Start();

            if (firstStart)
            {
                StartStopwatchBtn.Visibility = Visibility.Hidden;
                PauseStopwatchBtn.Visibility = Visibility.Visible;
                ResetStopwatchBtn.Visibility = Visibility.Hidden;
                LapStopwatchBtn.Visibility = Visibility.Visible;

                var widthEasing = new QuarticEase { EasingMode = EasingMode.EaseOut };
                UiAnimator.Width(StartStopwatchBtn, 200, 100, SwZoomSpeed, widthEasing);
                UiAnimator.Width(PauseStopwatchBtn, 200, 100, SwZoomSpeed, widthEasing);
                UiAnimator.Width(ResetStopwatchBtn, 0, 100, SwZoomSpeed, widthEasing);
                UiAnimator.Width(LapStopwatchBtn, 0, 100, SwZoomSpeed, widthEasing);
                UiAnimator.Width(SWButtonSep, 0, 20, SwZoomSpeed, widthEasing);
            }
            else
            {
                var fadeEasing = new QuadraticEase { EasingMode = EasingMode.EaseInOut };
                UiAnimator.Fade(StartStopwatchBtn, 1, 0, SwFadeSpeed, fadeEasing);
                UiAnimator.Fade(ResetStopwatchBtn, 1, 0, SwFadeSpeed, fadeEasing);

                await Task.Delay(SwFadeDelayMs);

                StartStopwatchBtn.Visibility = Visibility.Hidden;
                PauseStopwatchBtn.Visibility = Visibility.Visible;
                ResetStopwatchBtn.Visibility = Visibility.Hidden;
                LapStopwatchBtn.Visibility = Visibility.Visible;

                UiAnimator.Fade(PauseStopwatchBtn, 0, 1, SwFadeSpeed, fadeEasing);
                UiAnimator.Fade(LapStopwatchBtn, 0, 1, SwFadeSpeed, fadeEasing);
            }
        }

        private async void PauseStopwatchBtn_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.Stopwatch.Pause();

            var fadeEasing = new QuadraticEase { EasingMode = EasingMode.EaseInOut };
            UiAnimator.Fade(PauseStopwatchBtn, 1, 0, SwFadeSpeed, fadeEasing);
            UiAnimator.Fade(LapStopwatchBtn, 1, 0, SwFadeSpeed, fadeEasing);

            await Task.Delay(SwFadeDelayMs);

            StartStopwatchBtn.Visibility = Visibility.Visible;
            PauseStopwatchBtn.Visibility = Visibility.Hidden;
            ResetStopwatchBtn.Visibility = Visibility.Visible;
            LapStopwatchBtn.Visibility = Visibility.Hidden;

            UiAnimator.Fade(StartStopwatchBtn, 0, 1, SwFadeSpeed, fadeEasing);
            UiAnimator.Fade(ResetStopwatchBtn, 0, 1, SwFadeSpeed, fadeEasing);
        }

        private bool _stopwatchResetting;
        private Task? _stopwatchLapDeployTask;

        private async void ResetStopwatchBtn_Click(object sender, RoutedEventArgs e)
        {
            if (_stopwatchResetting)
            {
                return;
            }
            _stopwatchResetting = true;

            // Freeze the display *now*, before the lap fade-out (if any) even starts - otherwise the
            // digits keep counting up for the length of that fade instead of stopping the instant the
            // button was clicked.
            ViewModel.Stopwatch.StopTicking();

            bool hadLaps = ViewModel.Stopwatch.IsLapListVisible;

            if (hadLaps)
            {
                // Same order as the original: the lap cards fade out first (0.15s)...
                UiAnimator.Fade(LapStopwatchItems, 1, 0, SwFadeSpeed, new QuadraticEase { EasingMode = EasingMode.EaseOut });
                await Task.Delay(SwFadeDelayMs);
            }

            // ...and only then are they cleared. This also zeroes the digits, which slide to 0 (the
            // milliseconds too, because _stopwatchResetting is still set).
            ViewModel.Stopwatch.Reset();
            _stopwatchLapDeployTask = null;

            if (hadLaps)
            {
                LapStopwatchItems.BeginAnimation(UIElement.OpacityProperty, null);
                LapStopwatchItems.Opacity = 1;

                // The row goes back to Auto *before* the ScrollViewer collapses so it follows it down
                // smoothly. The height animation is left to run (the previous port released it half-way,
                // which cut the collapse short and made it snap).
                StopwatchLapRow.Height = new GridLength(1, GridUnitType.Auto);
                UiAnimator.Height(StopwatchLapScrollView, StopwatchSubGrid.ActualHeight * 0.6, 0, SwZoomSpeed, new QuinticEase { EasingMode = EasingMode.EaseOut });
            }

            var widthEasing = new QuarticEase { EasingMode = EasingMode.EaseOut };
            UiAnimator.Width(StartStopwatchBtn, 100, 200, SwZoomSpeed, widthEasing);
            UiAnimator.Width(PauseStopwatchBtn, 100, 200, SwZoomSpeed, widthEasing);
            UiAnimator.Width(ResetStopwatchBtn, 100, 0, SwZoomSpeed, widthEasing);
            UiAnimator.Width(LapStopwatchBtn, 100, 0, SwZoomSpeed, widthEasing);
            UiAnimator.Width(SWButtonSep, 20, 0, SwZoomSpeed, widthEasing);

            await Task.Delay(SwZoomDelayMs);

            if (hadLaps)
            {
                UiAnimator.ReleaseHeight(StopwatchLapScrollView);
            }

            StartStopwatchBtn.Visibility = Visibility.Visible;
            PauseStopwatchBtn.Visibility = Visibility.Hidden;
            ResetStopwatchBtn.Visibility = Visibility.Hidden;
            LapStopwatchBtn.Visibility = Visibility.Hidden;
            PauseStopwatchBtn.Opacity = 1; // ensure opacity is reset for the next run

            var fadeEasing = new QuadraticEase { EasingMode = EasingMode.EaseInOut };
            UiAnimator.Fade(PauseStopwatchBtn, 0, 1, SwFadeSpeed, fadeEasing);
            UiAnimator.Fade(LapStopwatchBtn, 0, 1, SwFadeSpeed, fadeEasing);

            _stopwatchResetting = false;
        }

        private async void LapStopwatchBtn_Click(object sender, RoutedEventArgs e)
        {
            bool firstLap = !ViewModel.Stopwatch.IsLapListVisible;

            // The lap's time is fixed at the instant of the click (this also flips IsLapListVisible
            // synchronously, so a fast double-click can't run the deploy animation twice).
            var lap = ViewModel.Stopwatch.CaptureLap();

            if (firstLap)
            {
                _stopwatchLapDeployTask = DeployStopwatchLapAreaAsync();
            }

            // Laps clicked while the area is still deploying wait for it, in click order.
            if (_stopwatchLapDeployTask is { IsCompleted: false } deploying)
            {
                await deploying;
            }

            // The card's own "slide in" (slot grows 0 -> 58, then fades in) is in the lap DataTemplate.
            ViewModel.Stopwatch.CommitLap(lap);
        }

        private async Task DeployStopwatchLapAreaAsync()
        {
            double expandedHeight = StopwatchSubGrid.ActualHeight * 0.6;
            UiAnimator.Height(StopwatchLapScrollView, 0, expandedHeight, SwZoomSpeed, new QuinticEase { EasingMode = EasingMode.EaseOut });
            await Task.Delay(SwZoomDelayMs);
            StopwatchLapRow.Height = new GridLength(0.6, GridUnitType.Star);
            UiAnimator.ReleaseHeight(StopwatchLapScrollView);
        }

        #endregion

        #region Timer

        private void WireTimerDigitAnimations()
        {
            ViewModel.Timer.PropertyChanged += (_, e) =>
            {
                var vm = ViewModel.Timer;
                switch (e.PropertyName)
                {
                    case nameof(TimerViewModel.H1): AnimateTimerDigit(TimerH1Text, vm.H1); break;
                    case nameof(TimerViewModel.H2): AnimateTimerDigit(TimerH2Text, vm.H2); break;
                    case nameof(TimerViewModel.M1): AnimateTimerDigit(TimerM1Text, vm.M1); break;
                    case nameof(TimerViewModel.M2): AnimateTimerDigit(TimerM2Text, vm.M2); break;
                    case nameof(TimerViewModel.S1): AnimateTimerDigit(TimerS1Text, vm.S1); break;
                    case nameof(TimerViewModel.S2): AnimateTimerDigit(TimerS2Text, vm.S2); break;
                    case nameof(TimerViewModel.ProgressPercent):
                        TimerProgressRing_Update(vm.ProgressPercent);
                        break;
                }
            };
        }

        /// <summary>
        /// Slides a timer digit the same way the original did: "up" (entering from below) when the
        /// value increased or wrapped forward past its max back to 0; "down" otherwise. The
        /// direction is inferred from what's currently displayed, since that's exactly the previous
        /// value until this call replaces it.
        /// </summary>
        private static void AnimateTimerDigit(TextBlock target, string newText)
        {
            string previous = target.Text;
            if (previous == newText) return;

            bool up = true;
            if (int.TryParse(previous, out int p) && int.TryParse(newText, out int n))
            {
                up = n > p || (newText == "0" && previous is "9" or "5" or "3" or "2");
            }

            double outTo = up ? -50 : 50;
            _ = UiAnimator.SlideReplaceAsync(target, SlideAxis.Y, outTo, -outTo, () => target.Text = newText,
                TTextSlideSpeed, TTextSlideSpeed, TTextDelayMs, new QuadraticEase { EasingMode = EasingMode.EaseIn }, new QuadraticEase { EasingMode = EasingMode.EaseOut });
        }

        private void WireTimerOverlayReactions()
        {
            ViewModel.Timer.PropertyChanged += async (_, e) =>
            {
                if (e.PropertyName != nameof(TimerViewModel.IsTimeUp)) return;

                if (ViewModel.Timer.IsTimeUp)
                {
                    await TurnScreenOnAsync();
                    TimeUpGrid.Visibility = Visibility.Visible;
                    UiAnimator.Scale(TimeUpGrid, 1.1, 1, TZoomSpeed, new QuadraticEase { EasingMode = EasingMode.EaseOut });
                    UiAnimator.Fade(TimeUpGrid, 0, 1, TFadeSpeed, new QuadraticEase { EasingMode = EasingMode.EaseIn });
                }
                else
                {
                    UiAnimator.Scale(TimeUpGrid, 1, 1.1, TZoomSpeed, new QuadraticEase { EasingMode = EasingMode.EaseIn });
                    UiAnimator.Fade(TimeUpGrid, 1, 0, TFadeSpeed, new QuadraticEase { EasingMode = EasingMode.EaseOut });
                    await Task.Delay(Math.Max(TFadeDelayMs, TZoomDelayMs));
                    TimeUpGrid.Visibility = Visibility.Hidden;
                    await CollapseTimerVisualsAsync();
                }
            };
        }

        /// <summary>The original eased the ring with QuadraticEase ease-out; the port had dropped the
        /// easing function, which made every step of the ring animate linearly.</summary>
        private void TimerProgressRing_Update(int percent)
        {
            TimerProgressRing.BeginAnimation(Wpf.Ui.Controls.ProgressRing.ProgressProperty,
                new System.Windows.Media.Animation.DoubleAnimation
                {
                    From = TimerProgressRing.Progress,
                    To = percent,
                    Duration = TimeSpan.FromSeconds(0.5),
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                });
        }

        private async void StartTimerBtn_Click(object sender, RoutedEventArgs e)
        {
            bool firstStart = !ViewModel.Timer.IsRunning;
            var before = ViewModel.Timer.IsRunning;
            ViewModel.Timer.Start();
            if (!before && !ViewModel.Timer.IsRunning)
            {
                return; // duration was zero, Start() was a no-op
            }

            if (firstStart)
            {
                StartTimerBtn.Visibility = Visibility.Hidden;
                PauseTimerBtn.Visibility = Visibility.Visible;
                ResetTimerBtn.Visibility = Visibility.Visible;
                PauseTimerBtn.Opacity = 1;

                var widthEasing = new QuarticEase { EasingMode = EasingMode.EaseOut };
                UiAnimator.Width(StartTimerBtn, 200, 100, TZoomSpeed, widthEasing);
                UiAnimator.Width(PauseTimerBtn, 200, 100, TZoomSpeed, widthEasing);
                UiAnimator.Width(ResetTimerBtn, 0, 100, TZoomSpeed, widthEasing);
                UiAnimator.Width(TButtonSep, 0, 20, TZoomSpeed, widthEasing);

                UiAnimator.Scale(TimerProgressRing, 0, 1, TZoomSpeed, new QuadraticEase { EasingMode = EasingMode.EaseOut });
                UiAnimator.Fade(TimerProgressRing, 0, 1, TFadeSpeed, new QuadraticEase { EasingMode = EasingMode.EaseIn });
                TimerProgressRing_Update(100);

                foreach (var button in TimerBtnGrid.Children.OfType<Button>())
                {
                    UiAnimator.Height(button, 31, 0, TZoomSpeed, new QuadraticEase { EasingMode = EasingMode.EaseOut });
                }
            }
            else
            {
                var fadeEasing = new QuadraticEase { EasingMode = EasingMode.EaseInOut };
                UiAnimator.Fade(StartTimerBtn, 1, 0, TFadeSpeed, fadeEasing);
                UiAnimator.Fade(ResetTimerBtn, 1, 0, TFadeSpeed, fadeEasing);
                await Task.Delay(TFadeDelayMs);
                StartTimerBtn.Visibility = Visibility.Hidden;
                PauseTimerBtn.Visibility = Visibility.Visible;
                ResetTimerBtn.Visibility = Visibility.Visible;
                UiAnimator.Fade(PauseTimerBtn, 0, 1, TFadeSpeed, fadeEasing);
                UiAnimator.Fade(ResetTimerBtn, 0, 1, TFadeSpeed, fadeEasing);
                UiAnimator.Fade(StartTimerBtn, 0, 1, TFadeSpeed, fadeEasing);
            }
        }

        private async void PauseTimerBtn_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.Timer.Pause();

            var fadeEasing = new QuadraticEase { EasingMode = EasingMode.EaseInOut };
            UiAnimator.Fade(PauseTimerBtn, 1, 0, TFadeSpeed, fadeEasing);
            UiAnimator.Fade(ResetTimerBtn, 1, 0, TFadeSpeed, fadeEasing);
            await Task.Delay(TFadeDelayMs);
            StartTimerBtn.Visibility = Visibility.Visible;
            PauseTimerBtn.Visibility = Visibility.Hidden;
            ResetTimerBtn.Visibility = Visibility.Visible;
            UiAnimator.Fade(StartTimerBtn, 0, 1, TFadeSpeed, fadeEasing);
            UiAnimator.Fade(PauseTimerBtn, 0, 1, TFadeSpeed, fadeEasing);
            UiAnimator.Fade(ResetTimerBtn, 0, 1, TFadeSpeed, fadeEasing);
        }

        private async void ResetTimerBtn_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.Timer.Reset();
            await CollapseTimerVisualsAsync();
        }

        private async Task CollapseTimerVisualsAsync()
        {
            StartTimerBtn.Visibility = Visibility.Visible;

            var widthEasing = new QuarticEase { EasingMode = EasingMode.EaseOut };
            UiAnimator.Width(StartTimerBtn, 100, 200, TZoomSpeed, widthEasing);
            UiAnimator.Width(PauseTimerBtn, 100, 200, TZoomSpeed, widthEasing);
            UiAnimator.Width(ResetTimerBtn, 100, 0, TZoomSpeed, widthEasing);
            UiAnimator.Width(TButtonSep, 20, 0, TZoomSpeed, widthEasing);

            await Task.Delay(TZoomDelayMs);

            PauseTimerBtn.Visibility = Visibility.Hidden;
            ResetTimerBtn.Visibility = Visibility.Hidden;

            UiAnimator.Scale(TimerProgressRing, 1, 0, TZoomSpeed, new QuadraticEase { EasingMode = EasingMode.EaseIn });
            UiAnimator.Fade(TimerProgressRing, 1, 0, TFadeSpeed, new QuadraticEase { EasingMode = EasingMode.EaseOut });

            foreach (var button in TimerBtnGrid.Children.OfType<Button>())
            {
                UiAnimator.Height(button, 0, 31, TZoomSpeed, new QuadraticEase { EasingMode = EasingMode.EaseOut });
            }
        }

        private void TimeUpStopBtn_Click(object sender, EventArgs e) => ViewModel.Timer.AcknowledgeTimeUp();

        private void TimerHourUpBtn_Click(object sender, RoutedEventArgs e) => ViewModel.Timer.AdjustHour(up: true);
        private void TimerHourDownBtn_Click(object sender, RoutedEventArgs e) => ViewModel.Timer.AdjustHour(up: false);
        private void TimerMinuteUpBtn_Click(object sender, RoutedEventArgs e) => ViewModel.Timer.AdjustMinute(up: true);
        private void TimerMinuteDownBtn_Click(object sender, RoutedEventArgs e) => ViewModel.Timer.AdjustMinute(up: false);
        private void TimerSecondUpBtn_Click(object sender, RoutedEventArgs e) => ViewModel.Timer.AdjustSecond(up: true);
        private void TimerSecondDownBtn_Click(object sender, RoutedEventArgs e) => ViewModel.Timer.AdjustSecond(up: false);

        #endregion

        #region Alarm

        /// <summary>
        /// Mirrors the original's AddAlarmBtn_Click sequencing: the new (unsorted) card first grows into
        /// the list, and only once that grow animation has finished does it expand into the editor.
        /// Expanding immediately (the previous behaviour) started the editor's height animation while
        /// the card was still mid-grow, so the two animations fought each other.
        /// </summary>
        private async void AddAlarmBtn_Click(object sender, RoutedEventArgs e)
        {
            var item = await ViewModel.Alarms.AddAlarmAsync();
            if (item == null)
            {
                return;
            }

            await Task.Delay(300);
            ViewModel.Alarms.ExpandNewlyAdded(item);
        }

        private void WireAlarmAlertReactions()
        {
            ViewModel.Alarms.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName == nameof(AlarmsViewModel.IsAlertActive))
                {
                    OnAlarmAlertChanged(ViewModel.Alarms.IsAlertActive);
                }
            };
            ViewModel.Alarms.ExpansionChanged += (_, expandedItem) => OnAlarmExpansionChanged(expandedItem);
        }

        private async void OnAlarmAlertChanged(bool active)
        {
            if (active)
            {
                AlarmAlertGrid.Visibility = Visibility.Visible;
                UiAnimator.Fade(AlarmAlertGrid, 0, 1, TFadeSpeed, new QuadraticEase { EasingMode = EasingMode.EaseIn });
                UiAnimator.Scale(AlarmAlertGrid, 1.1, 1, TZoomSpeed, new QuarticEase { EasingMode = EasingMode.EaseOut });
            }
            else
            {
                UiAnimator.Fade(AlarmAlertGrid, 1, 0, TFadeSpeed, new QuadraticEase { EasingMode = EasingMode.EaseOut });
                UiAnimator.Scale(AlarmAlertGrid, 1, 1.1, TZoomSpeed, new QuadraticEase { EasingMode = EasingMode.EaseIn });
                await Task.Delay(500);
                AlarmAlertGrid.Visibility = Visibility.Hidden;
            }
        }

        private void AlarmAlertStopBtn_Click(object sender, RoutedEventArgs e) => ViewModel.Alarms.StopAlert();

        /// <summary>Shrinks/hides every card but the one being expanded, or restores them all when collapsing.</summary>
        private async void OnAlarmExpansionChanged(AlarmItemViewModel? expandedItem)
        {
            if (expandedItem != null)
            {
                _expandedAlarmItem = expandedItem;
                foreach (var pair in _alarmCardsByViewModel)
                {
                    if (pair.Key != expandedItem)
                    {
                        UiAnimator.Height(pair.Value, pair.Value.ActualHeight, 0, 0.3, new QuarticEase { EasingMode = EasingMode.EaseOut });
                    }
                }
                await Task.Delay(150);
                foreach (var pair in _alarmCardsByViewModel)
                {
                    if (pair.Key != expandedItem)
                    {
                        pair.Value.Visibility = Visibility.Collapsed;
                    }
                }
                await Task.Delay(150);
            }
            else
            {
                var closingItem = _expandedAlarmItem;
                _expandedAlarmItem = null;

                foreach (var card in _alarmCardsByViewModel.Values)
                {
                    card.Visibility = Visibility.Visible;
                }
                await Task.Delay(150);
                foreach (var pair in _alarmCardsByViewModel)
                {
                    // The card that was just being edited keeps its own height animation (owned by
                    // AlarmCardView); only the siblings that were shrunk to 0 grow back here.
                    if (pair.Key != closingItem)
                    {
                        UiAnimator.Height(pair.Value, 0, 64, 0.3, new QuarticEase { EasingMode = EasingMode.EaseOut });
                    }
                }
                await Task.Delay(300);
                foreach (var card in _alarmCardsByViewModel.Values)
                {
                    UiAnimator.ReleaseHeight(card);
                }
                await Task.Delay(500);

                // Everything has finished animating: only now sort, refresh the ETA texts and (for a
                // Delete) remove the card - each strictly after the previous one has fully played out,
                // exactly like AlarmCardClose -> SortAlarmCards -> AlarmCardDelete in the original.
                var pendingDelete = ViewModel.Alarms.FinishCollapse();
                await ReorderAlarmCardsIfNeededAsync();
                if (pendingDelete != null)
                {
                    await ViewModel.Alarms.RemovePendingDeleteAsync(pendingDelete);
                }
            }
        }

        /// <summary>
        /// Reconciles the AlarmStack panel with ViewModel.Alarms.Alarms: creates a card for every new
        /// alarm and removes cards for deleted ones, each with the matching grow/shrink animation from
        /// the original. Pure re-ordering is intentionally *not* handled reactively (see the
        /// CollectionChanged subscription): it is driven explicitly, once the close animation that
        /// triggered it has fully finished (see <see cref="OnAlarmExpansionChanged"/>).
        /// </summary>
        private async void SyncAlarmCards()
        {
            var alarms = ViewModel.Alarms.Alarms;

            if (alarms.Count > 0)
            {
                NoAlarmGrid.Visibility = Visibility.Hidden;
            }
            else if (AlarmStack.Children.Count == 0)
            {
                NoAlarmGrid.Visibility = Visibility.Visible;
            }

            bool removedAny = false;
            foreach (var removedVm in _alarmCardsByViewModel.Keys.Except(alarms).ToList())
            {
                var card = _alarmCardsByViewModel[removedVm];
                _alarmCardsByViewModel.Remove(removedVm);
                removedAny = true;
                _ = RemoveAlarmCardAsync(card);
            }

            bool addedAny = false;
            foreach (var vm in alarms)
            {
                if (!_alarmCardsByViewModel.ContainsKey(vm))
                {
                    var card = new AlarmCardView(vm, AlarmScroll);
                    _alarmCardsByViewModel[vm] = card;
                    int index = Math.Min(alarms.IndexOf(vm), AlarmStack.Children.Count);
                    AlarmStack.Children.Insert(index, card);
                    addedAny = true;
                    _ = GrowAlarmCardAsync(card);
                }
            }

            // A plain re-sync (nothing added or removed), e.g. the explicit call right after the
            // initial load. Never reorder while a card is mid-removal: the panel still contains the
            // card that is shrinking away, so the two lists would differ for the wrong reason.
            if (!addedAny && !removedAny)
            {
                await ReorderAlarmCardsIfNeededAsync();
            }
        }

        /// <summary>The original's AlarmCardDelete: a quick 0.2s shrink, then removal, then the
        /// "No alarms" placeholder if that was the last card.</summary>
        private async Task RemoveAlarmCardAsync(AlarmCardView card)
        {
            UiAnimator.Height(card, card.ActualHeight, 0, 0.2, new QuarticEase { EasingMode = EasingMode.EaseOut });
            await Task.Delay(200);
            AlarmStack.Children.Remove(card);
            if (AlarmStack.Children.Count == 0)
            {
                NoAlarmGrid.Visibility = Visibility.Visible;
            }
        }

        private async Task GrowAlarmCardAsync(AlarmCardView card)
        {
            UiAnimator.Height(card, 0, 64, 0.3, new QuarticEase { EasingMode = EasingMode.EaseOut });
            await Task.Delay(300);
            UiAnimator.ReleaseHeight(card);
        }

        /// <summary>
        /// Port of the original's SortAlarmCards: only the cards whose position actually changes shrink
        /// away, the panel is rebuilt in the new order, and only those cards grow back. Untouched cards
        /// never animate (the previous port shrank *every* card as soon as any single one moved).
        /// </summary>
        private async Task ReorderAlarmCardsIfNeededAsync()
        {
            var desiredOrder = ViewModel.Alarms.Alarms
                .Where(vm => _alarmCardsByViewModel.ContainsKey(vm))
                .Select(vm => _alarmCardsByViewModel[vm])
                .ToList();
            var currentOrder = AlarmStack.Children.OfType<AlarmCardView>().ToList();

            var tasks = new List<Task>();
            for (int i = 0; i < currentOrder.Count; i++)
            {
                var card = currentOrder[i];
                if (desiredOrder.IndexOf(card) == i)
                {
                    continue;
                }

                if (double.IsNaN(card.Height)) card.Height = card.ActualHeight;
                var tcs = new TaskCompletionSource();
                var shrink = new DoubleAnimation { To = 0, Duration = TimeSpan.FromMilliseconds(300), EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn } };
                shrink.Completed += (_, _) => tcs.TrySetResult();
                card.BeginAnimation(HeightProperty, shrink);
                tasks.Add(tcs.Task);
            }

            if (tasks.Count == 0)
            {
                return;
            }

            await Task.WhenAll(tasks);
            await Task.Delay(150);

            AlarmStack.Children.Clear();
            foreach (var card in desiredOrder)
            {
                AlarmStack.Children.Add(card);
            }

            tasks.Clear();
            foreach (var card in desiredOrder)
            {
                if (double.IsNaN(card.Height)) continue;

                var tcs = new TaskCompletionSource();
                var grow = new DoubleAnimation { From = 0, To = 64, Duration = TimeSpan.FromMilliseconds(500), EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut } };
                grow.Completed += (_, _) => tcs.TrySetResult();
                card.BeginAnimation(HeightProperty, grow);
                tasks.Add(tcs.Task);
            }
            await Task.WhenAll(tasks);

            foreach (var card in desiredOrder)
            {
                UiAnimator.ReleaseHeight(card);
            }
        }

        #endregion

        #region Global menu

        private async void GlobalMenuBtn_MouseDown(object sender, MouseButtonEventArgs e)
        {
            _globalMenuBlocked = true;
            ViewModel.IsGlobalMenuOpen = true;
            await Task.Delay(GlobalAnimSpeedMs + 150);
            _globalMenuBlocked = false;
        }

        private void GlobalMenuGrid_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (e.OriginalSource == GlobalMenuGrid)
            {
                CloseGlobalMenu();
            }
        }

        private void CloseGlobalMenu() => ViewModel.IsGlobalMenuOpen = false;

        private void BackBtn_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (_globalMenuBlocked) return;
            CloseGlobalMenu();
        }

        /// <summary>
        /// Port of the original's GlobalMenuBtn_MouseDown/BackBtn_MouseUp: the round menu button and
        /// the menu panel take turns - one zooms/fades away *before* the other appears and zooms/fades
        /// in, rather than both animating at once. The previous port only ever animated the panel and
        /// never touched the button at all, so it just sat there full-size, unmoving, the whole time.
        /// </summary>
        private async void OnGlobalMenuOpenChanged(bool isOpen)
        {
            double durationSeconds = GlobalAnimSpeedMs / 1000.0;
            var shrinkEase = new QuarticEase { EasingMode = EasingMode.EaseIn };
            var growEase = new QuarticEase { EasingMode = EasingMode.EaseOut };

            if (isOpen)
            {
                UiAnimator.Fade(GlobalMenuBtn, 1, 0, durationSeconds, new QuadraticEase { EasingMode = EasingMode.EaseOut });
                UiAnimator.Scale(GlobalMenuBtn, 1, 2, durationSeconds, shrinkEase);
                await Task.Delay(GlobalAnimSpeedMs);

                GlobalMenuGrid.Visibility = Visibility.Visible;
                GlobalMenuBtn.Visibility = Visibility.Collapsed;

                UiAnimator.Fade(GlobalMenuGrid, 0, 1, durationSeconds, new QuadraticEase { EasingMode = EasingMode.EaseIn });
                UiAnimator.Scale(GlobalMenuBorder, 0, 1, durationSeconds, growEase);
            }
            else
            {
                UiAnimator.Fade(GlobalMenuGrid, 1, 0, durationSeconds, new QuadraticEase { EasingMode = EasingMode.EaseOut });
                UiAnimator.Scale(GlobalMenuBorder, 1, 0, durationSeconds, shrinkEase);
                await Task.Delay(GlobalAnimSpeedMs);

                GlobalMenuGrid.Visibility = Visibility.Hidden;
                GlobalMenuBtn.Visibility = Visibility.Visible;

                UiAnimator.Fade(GlobalMenuBtn, 0, 1, durationSeconds, new QuadraticEase { EasingMode = EasingMode.EaseOut });
                UiAnimator.Scale(GlobalMenuBtn, 2, 1, durationSeconds, growEase);
                await Task.Delay(300);
            }
        }

        private static async Task HideAfterAsync(UIElement element, double delaySeconds)
        {
            await Task.Delay(TimeSpan.FromSeconds(delaySeconds));
            element.Visibility = Visibility.Hidden;
        }

        private async void MirrorBtn_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (_globalMenuBlocked) return;
            CloseGlobalMenu();
            await Task.Delay(GlobalAnimSpeedMs);
            ViewModel.IsMirrorOpen = true;
        }

        private async void SettingsBtn_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (_globalMenuBlocked) return;
            CloseGlobalMenu();
            await Task.Delay(GlobalAnimSpeedMs);
            ViewModel.IsSettingsOpen = true;
        }

        private async void ExitAppBtn_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (_globalMenuBlocked) return;
            _ = Log.Info("Exiting the application...");
            await Task.Delay(150);
            Application.Current.Shutdown();
        }

        #endregion

        #region Settings

        private void WireSettingsReactions()
        {
            ViewModel.Settings.RestartRequired += async (_, _) => await App.RestartApp();
        }

        private void SettingsBackBtn_Click(object sender, RoutedEventArgs e) => ViewModel.IsSettingsOpen = false;

        private async void SettingsSaveBtn_Click(object sender, RoutedEventArgs e)
        {
            await ViewModel.Settings.SaveAsync();
            ViewModel.IsSettingsOpen = false;
        }

        /// <summary>
        /// The original zoomed this panel the opposite way round from the global/kiosk menus: it
        /// starts slightly *oversized* (1.1x) and settles down to 1x on open, then grows back past
        /// 1x (to 1.1x) as it fades out on close. The previous port reused the "grows from smaller"
        /// 0.9x/1x pattern of the other panels here too, which plays the scale backwards.
        /// </summary>
        private async void OnSettingsOpenChanged(bool isOpen)
        {
            var fade = new QuadraticEase { EasingMode = isOpen ? EasingMode.EaseIn : EasingMode.EaseOut };
            var zoom = new QuarticEase { EasingMode = isOpen ? EasingMode.EaseOut : EasingMode.EaseIn };
            double durationSeconds = GlobalAnimSpeedMs / 1000.0;

            if (isOpen)
            {
                await ViewModel.Settings.LoadAsync();
                GeneralFrame.Navigate(_settingsTabs["General"]);
                PluginManagerFrame.Navigate(_settingsTabs["PluginManager"]);
                AboutFrame.Navigate(_settingsTabs["About"]);

                SettingsGrid.Visibility = Visibility.Visible;
                UiAnimator.Fade(SettingsGrid, 0, 1, durationSeconds, fade);
                UiAnimator.Scale(SettingsBorder, 1.1, 1, durationSeconds, zoom);
            }
            else
            {
                UiAnimator.Fade(SettingsGrid, 1, 0, durationSeconds, fade);
                UiAnimator.Scale(SettingsBorder, 1, 1.1, durationSeconds, zoom);
                _ = HideAfterAsync(SettingsGrid, durationSeconds);
            }
        }

        private void SettingsTabFrame_Loaded(object sender, RoutedEventArgs e)
        {
            if (SettingsGrid.Visibility != Visibility.Visible)
            {
                return;
            }

            var frame = (Frame)sender;
            UiAnimator.Fade(frame, 0, 1, SettingsTabAnimSpeed / 2, new QuadraticEase { EasingMode = EasingMode.EaseIn });
            UiAnimator.Scale(frame, 0.9, 1, SettingsTabAnimSpeed, new QuarticEase { EasingMode = EasingMode.EaseOut });
        }

        #endregion

        #region Mirror

        private void MirrorBackBtn_Click(object sender, RoutedEventArgs e) => ViewModel.IsMirrorOpen = false;

        private async void OnMirrorOpenChanged(bool isOpen)
        {
            var fade = new QuadraticEase { EasingMode = isOpen ? EasingMode.EaseIn : EasingMode.EaseOut };
            var zoom = isOpen ? (IEasingFunction)new QuinticEase { EasingMode = EasingMode.EaseOut } : new QuarticEase { EasingMode = EasingMode.EaseIn };
            double durationSeconds = GlobalAnimSpeedMs / 1000.0;

            if (isOpen)
            {
                MirrorFrame.Navigate(new Mirror(ViewModel.Mirror, _localization));
                MirrorGrid.Visibility = Visibility.Visible;
                UiAnimator.Fade(MirrorGrid, 0, 1, durationSeconds, fade);
                UiAnimator.Scale(MirrorGrid, 1.1, 1, durationSeconds, new QuarticEase { EasingMode = EasingMode.EaseOut });
            }
            else
            {
                UiAnimator.Fade(MirrorGrid, 1, 0, durationSeconds, fade);
                UiAnimator.Scale(MirrorGrid, 1, 1.1, durationSeconds, zoom);
                await Task.Delay(GlobalAnimSpeedMs);
                MirrorFrame.Navigate(null);
                MirrorGrid.Visibility = Visibility.Hidden;
                await ViewModel.Mirror.StopAsync();
            }
        }

        #endregion

        #region Plugin menu

        private void RefreshPluginCarouselPlaceholder()
        {
            if (!ViewModel.PluginCarousel.HasAnyPlugin)
            {
                _ = Log.Warning("No plugin loaded");
                CarouselPluginFrame.Navigate(BuildPluginPlaceholder("Resources/Icons/plugin.png", _localization.GetString("plugin.noload")));
                EnterDownMenuPluginBtn.IsEnabled = false;
                return;
            }

            EnterDownMenuPluginBtn.IsEnabled = true;

            if (!ViewModel.PluginCarousel.HasPinnedPlugin)
            {
                _ = Log.Warning("No pinned plugin in the carousel list");
                CarouselPluginFrame.Navigate(BuildPluginPlaceholder("Resources/Icons/carousel.png", _localization.GetString("plugin.nopin")));
                return;
            }

            CarouselPluginFrame.Navigate(ViewModel.PluginCarousel.GetMainPage(ViewModel.PluginCarousel.CurrentCarouselPluginId!));
        }

        private static Grid BuildPluginPlaceholder(string iconResourcePath, string message)
        {
            var grid = new Grid
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
            var image = new System.Windows.Controls.Image
            {
                Source = new System.Windows.Media.Imaging.BitmapImage(new Uri($"pack://application:,,,/{iconResourcePath}")),
                Width = 72,
                Height = 72
            };
            image.SetValue(Grid.RowProperty, 0);
            RenderOptions.SetBitmapScalingMode(image, BitmapScalingMode.HighQuality);

            var text = new Wpf.Ui.Controls.TextBlock
            {
                Text = message,
                FontSize = 20,
                Foreground = (Brush)Application.Current.Resources["TextFillColorPrimaryBrush"]
            };
            text.SetValue(Grid.RowProperty, 2);

            grid.Children.Add(image);
            grid.Children.Add(text);
            return grid;
        }

        private async Task AdvanceCarouselAsync()
        {
            if (!ViewModel.PluginCarousel.HasPinnedPlugin || ViewModel.IsPluginMenuOpen)
            {
                return;
            }

            UiAnimator.Fade(CarouselPluginFrame, 1, 0, 0.3, new QuadraticEase { EasingMode = EasingMode.EaseOut });
            await Task.Delay(300);

            string? nextId = ViewModel.PluginCarousel.AdvanceCarousel();
            if (nextId != null)
            {
                CarouselPluginFrame.Navigate(ViewModel.PluginCarousel.GetMainPage(nextId));
            }

            UiAnimator.Fade(CarouselPluginFrame, 0, 1, 0.3, new QuadraticEase { EasingMode = EasingMode.EaseIn });
        }

        private void EnterDownMenuPluginBtn_Click(object sender, RoutedEventArgs e) => OpenPluginMenuFromCarousel();

        private void CarouselFrameClickGrid_PreviewMouseUp(object sender, MouseButtonEventArgs e) => OpenPluginMenuFromCarousel();

        private void OpenPluginMenuFromCarousel()
        {
            if (!ViewModel.PluginCarousel.HasAnyPlugin) return;
            ViewModel.PluginCarousel.OpenMenuAtCurrentCarouselPlugin();
            MenuPluginFrame.Navigate(ViewModel.PluginCarousel.GetMainPage(ViewModel.PluginCarousel.CurrentMenuPluginId!));
            LeftMenuPluginBtn.IsEnabled = ViewModel.PluginCarousel.CanGoLeftInMenu;
            RightMenuPluginBtn.IsEnabled = ViewModel.PluginCarousel.CanGoRightInMenu;
            ViewModel.IsPluginMenuOpen = true;
        }

        private void ExitDownMenuPluginBtn_Click(object sender, RoutedEventArgs e) => ViewModel.IsPluginMenuOpen = false;

        private async void OnPluginMenuOpenChanged(bool isOpen)
        {
            double durationSeconds = GlobalAnimSpeedMs / 1000.0;
            var fadeOut = new QuadraticEase { EasingMode = EasingMode.EaseOut };
            var fadeIn = new QuadraticEase { EasingMode = EasingMode.EaseIn };

            if (isOpen)
            {
                UiAnimator.Translate(PluginGrid, SlideAxis.Y, 0, -100, durationSeconds, new QuarticEase { EasingMode = EasingMode.EaseIn });
                UiAnimator.Fade(PluginGrid, 1, 0, durationSeconds, fadeOut);

                await Task.Delay(GlobalAnimSpeedMs);

                PluginGrid.Visibility = Visibility.Hidden;
                MenuPluginGrid.Visibility = Visibility.Visible;

                UiAnimator.Translate(MenuPluginGrid, SlideAxis.Y, 100, 0, durationSeconds, new QuarticEase { EasingMode = EasingMode.EaseOut });
                UiAnimator.Fade(MenuPluginGrid, 0, 1, durationSeconds, fadeIn);
            }
            else
            {
                UiAnimator.Translate(MenuPluginGrid, SlideAxis.Y, 0, 100, durationSeconds, new QuarticEase { EasingMode = EasingMode.EaseIn });
                UiAnimator.Fade(MenuPluginGrid, 1, 0, durationSeconds, fadeOut);

                await Task.Delay(GlobalAnimSpeedMs);

                MenuPluginGrid.Visibility = Visibility.Hidden;
                PluginGrid.Visibility = Visibility.Visible;
                MenuPluginFrame.Navigate(null);

                UiAnimator.Translate(PluginGrid, SlideAxis.Y, -100, 0, durationSeconds, new QuarticEase { EasingMode = EasingMode.EaseOut });
                UiAnimator.Fade(PluginGrid, 0, 1, durationSeconds, fadeIn);

                RefreshPluginCarouselPlaceholder();
            }
        }

        private async void LeftMenuPluginBtn_Click(object sender, RoutedEventArgs e)
        {
            string? id = ViewModel.PluginCarousel.MoveMenuLeft();
            RightMenuPluginBtn.IsEnabled = true;
            LeftMenuPluginBtn.IsEnabled = ViewModel.PluginCarousel.CanGoLeftInMenu;
            await SlidePluginMenuAsync(id, goingLeft: true);
        }

        private async void RightMenuPluginBtn_Click(object sender, RoutedEventArgs e)
        {
            string? id = ViewModel.PluginCarousel.MoveMenuRight();
            LeftMenuPluginBtn.IsEnabled = true;
            RightMenuPluginBtn.IsEnabled = ViewModel.PluginCarousel.CanGoRightInMenu;
            await SlidePluginMenuAsync(id, goingLeft: false);
        }

        private async Task SlidePluginMenuAsync(string? nextId, bool goingLeft)
        {
            if (nextId == null) return;

            double outDirection = goingLeft ? 100 : -100;
            UiAnimator.Fade(MenuPluginFrame, 1, 0, FadeSpeed, new QuadraticEase { EasingMode = EasingMode.EaseOut }, beginTimeSeconds: ZoomSpeed - FadeSpeed);
            UiAnimator.Translate(MenuPluginFrame, SlideAxis.X, 0, outDirection, ZoomSpeed, new QuinticEase { EasingMode = EasingMode.EaseIn });

            await Task.Delay(AnimDelayMs);

            MenuPluginFrame.Navigate(ViewModel.PluginCarousel.GetMainPage(nextId));
            UiAnimator.Fade(MenuPluginFrame, 0, 1, FadeSpeed, new QuadraticEase { EasingMode = EasingMode.EaseIn });
            UiAnimator.Translate(MenuPluginFrame, SlideAxis.X, -outDirection, 0, ZoomSpeed, new QuinticEase { EasingMode = EasingMode.EaseOut });
        }

        #endregion
    }
}
