using System.Windows.Threading;
using WinDeskClock.Mvvm;
using WinDeskClock.Services;

namespace WinDeskClock.ViewModels
{
    public enum ClockMenuTab { Alarm = 0, Stopwatch = 1, Timer = 2 }

    /// <summary>
    /// Composition root: hosts every feature ViewModel and the small amount of shell-level state
    /// (which overlay/panel is currently open, fullscreen/kiosk flags) that several parts of the
    /// View need to agree on. Window-chrome operations that require an actual Window/HWND
    /// (entering fullscreen, turning the monitor off, hiding the taskbar...) are intentionally left
    /// to MainWindow's code-behind; this ViewModel just remembers the resulting state so the rest
    /// of the app (and the auto-close/idle logic below) can react to it.
    /// </summary>
    public class MainViewModel : ObservableObject
    {
        private readonly IConfigService _config;
        private readonly DispatcherTimer _menuAutoCloseTimer;

        public MainViewModel(
            IConfigService config,
            ClockViewModel clock,
            MiniClockViewModel miniClock,
            StopwatchViewModel stopwatch,
            TimerViewModel timer,
            AlarmsViewModel alarms,
            SettingsViewModel settings,
            MirrorViewModel mirror,
            PluginCarouselViewModel pluginCarousel)
        {
            _config = config;
            Clock = clock;
            MiniClock = miniClock;
            Stopwatch = stopwatch;
            Timer = timer;
            Alarms = alarms;
            Settings = settings;
            Mirror = mirror;
            PluginCarousel = pluginCarousel;

            _menuAutoCloseTimer = new DispatcherTimer();
            _menuAutoCloseTimer.Tick += (_, _) => CloseMenusIfIdle();
        }

        public ClockViewModel Clock { get; }
        public MiniClockViewModel MiniClock { get; }
        public StopwatchViewModel Stopwatch { get; }
        public TimerViewModel Timer { get; }
        public AlarmsViewModel Alarms { get; }
        public SettingsViewModel Settings { get; }
        public MirrorViewModel Mirror { get; }
        public PluginCarouselViewModel PluginCarousel { get; }

        // ----- Fullscreen / kiosk : the View performs the actual Window changes, then reports
        // the resulting state back here through these setters. -----
        private bool _isFullScreen;
        public bool IsFullScreen
        {
            get => _isFullScreen;
            set
            {
                if (SetProperty(ref _isFullScreen, value))
                {
                    OnPropertyChanged(nameof(IsFullScreenIndicatorChecked));
                }
            }
        }

        private bool _isKioskMode;
        public bool IsKioskMode
        {
            get => _isKioskMode;
            set
            {
                if (SetProperty(ref _isKioskMode, value))
                {
                    OnPropertyChanged(nameof(IsFullScreenIndicatorChecked));
                }
            }
        }

        /// <summary>The FullScreen toggle shows checked (and is disabled) while Kiosk mode is on,
        /// since kiosk implies fullscreen - matching the original's visual behaviour.</summary>
        public bool IsFullScreenIndicatorChecked => IsFullScreen || IsKioskMode;

        private bool _isScreenOff;
        public bool IsScreenOff { get => _isScreenOff; set => SetProperty(ref _isScreenOff, value); }

        // ----- Which overlay/panel is currently shown -----
        private bool _isGlobalMenuOpen;
        public bool IsGlobalMenuOpen { get => _isGlobalMenuOpen; set => SetProperty(ref _isGlobalMenuOpen, value); }

        private bool _isClockMenuOpen;
        public bool IsClockMenuOpen { get => _isClockMenuOpen; set => SetProperty(ref _isClockMenuOpen, value); }

        private ClockMenuTab _currentClockTab = ClockMenuTab.Alarm;
        public ClockMenuTab CurrentClockTab { get => _currentClockTab; set => SetProperty(ref _currentClockTab, value); }

        private bool _isSettingsOpen;
        public bool IsSettingsOpen { get => _isSettingsOpen; set => SetProperty(ref _isSettingsOpen, value); }

        private bool _isMirrorOpen;
        public bool IsMirrorOpen { get => _isMirrorOpen; set => SetProperty(ref _isMirrorOpen, value); }

        private bool _isPluginMenuOpen;
        public bool IsPluginMenuOpen { get => _isPluginMenuOpen; set => SetProperty(ref _isPluginMenuOpen, value); }

        /// <summary>
        /// Call once at startup, after settings/plugins/alarms have been loaded.
        /// </summary>
        public void StartClocks()
        {
            Clock.Start();
            MiniClock.Start();
        }

        /// <summary>The View calls this on every mouse move / touch, to reset the "close menus after idle" timer.</summary>
        public void NotifyUserActivity()
        {
            int delay = _config.CurrentMenuCloseDelaySeconds;
            if (delay == 0)
            {
                _menuAutoCloseTimer.Stop();
                return;
            }

            _menuAutoCloseTimer.Interval = TimeSpan.FromSeconds(delay);
            _menuAutoCloseTimer.Start();
        }

        /// <summary>
        /// Mirrors the original MenuAutoClose_Tick: closes whichever panels are open, except a
        /// clock-menu tab that's actively running a stopwatch or timer.
        /// </summary>
        private void CloseMenusIfIdle()
        {
            if (IsGlobalMenuOpen)
            {
                IsGlobalMenuOpen = false;
            }

            bool clockTabIsBusy = (CurrentClockTab == ClockMenuTab.Stopwatch && Stopwatch.IsCounting)
                                || (CurrentClockTab == ClockMenuTab.Timer && Timer.IsCounting);
            if (IsClockMenuOpen && !clockTabIsBusy)
            {
                IsClockMenuOpen = false;
            }

            if (IsPluginMenuOpen)
            {
                IsPluginMenuOpen = false;
            }
        }
    }
}
