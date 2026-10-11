using System.Windows.Threading;
using WinDeskClock.Mvvm;
using WinDeskClock.Services;
using WinDeskClock.Utils;

namespace WinDeskClock.ViewModels
{
    /// <summary>
    /// Countdown timer state and logic. Same IsRunning/IsCounting split as StopwatchViewModel:
    /// IsRunning marks "has been started at least once since the last reset" (drives the one-time
    /// button/ring morph animation), IsCounting marks "actively ticking right now" (drives the
    /// simple Start/Pause cross-fade).
    /// </summary>
    public class TimerViewModel : ObservableObject
    {
        private readonly IConfigService _config;
        private readonly ISoundService _soundService;
        private readonly DispatcherTimer _tickTimer;

        private TimeSpan _remaining = TimeSpan.Zero;
        private TimeSpan _configuredDuration = TimeSpan.Zero;

        public TimerViewModel(IConfigService config, ISoundService soundService)
        {
            _config = config;
            _soundService = soundService;
            _tickTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _tickTimer.Tick += (_, _) => Tick();
        }

        private bool _isRunning;
        public bool IsRunning { get => _isRunning; private set => SetProperty(ref _isRunning, value); }

        private bool _isCounting;
        public bool IsCounting { get => _isCounting; private set => SetProperty(ref _isCounting, value); }

        private bool _isTimeUp;
        public bool IsTimeUp { get => _isTimeUp; private set => SetProperty(ref _isTimeUp, value); }

        private int _progressPercent = 100;
        public int ProgressPercent { get => _progressPercent; private set => SetProperty(ref _progressPercent, value); }

        private string _h1 = "0"; public string H1 { get => _h1; private set => SetProperty(ref _h1, value); }
        private string _h2 = "0"; public string H2 { get => _h2; private set => SetProperty(ref _h2, value); }
        private string _m1 = "0"; public string M1 { get => _m1; private set => SetProperty(ref _m1, value); }
        private string _m2 = "0"; public string M2 { get => _m2; private set => SetProperty(ref _m2, value); }
        private string _s1 = "0"; public string S1 { get => _s1; private set => SetProperty(ref _s1, value); }
        private string _s2 = "0"; public string S2 { get => _s2; private set => SetProperty(ref _s2, value); }

        /// <summary>
        /// Adjusts an editable digit by one step, with the same wrap-around rules as the original
        /// (hours wrap at 0/23, minutes and seconds wrap at 0/59). Only meaningful before Start.
        /// </summary>
        public void AdjustHour(bool up) => (H1, H2) = AdjustHourDigits(H1, H2, up);
        public void AdjustMinute(bool up) => (M1, M2) = AdjustSexagesimalDigits(M1, M2, up);
        public void AdjustSecond(bool up) => (S1, S2) = AdjustSexagesimalDigits(S1, S2, up);

        private static (string, string) AdjustHourDigits(string tens, string units, bool up)
        {
            int hour = int.Parse(tens) * 10 + int.Parse(units);
            hour = up ? (hour == 23 ? 0 : hour + 1) : (hour == 0 ? 23 : hour - 1);
            return ((hour / 10).ToString(), (hour % 10).ToString());
        }

        private static (string, string) AdjustSexagesimalDigits(string tens, string units, bool up)
        {
            int value = int.Parse(tens) * 10 + int.Parse(units);
            value = up ? (value == 59 ? 0 : value + 1) : (value == 0 ? 59 : value - 1);
            return ((value / 10).ToString(), (value % 10).ToString());
        }

        public void Start()
        {
            if (!IsRunning)
            {
                _configuredDuration = TimeSpan.Zero
                    .Add(TimeSpan.FromHours(int.Parse(H1) * 10 + int.Parse(H2)))
                    .Add(TimeSpan.FromMinutes(int.Parse(M1) * 10 + int.Parse(M2)))
                    .Add(TimeSpan.FromSeconds(int.Parse(S1) * 10 + int.Parse(S2)));

                if (_configuredDuration == TimeSpan.Zero)
                {
                    return;
                }

                _remaining = _configuredDuration;
                ProgressPercent = 100;
                _ = Log.Info($"Timer start: {H1}{H2}:{M1}{M2}:{S1}{S2}");
            }

            _tickTimer.Start();
            IsRunning = true;
            IsCounting = true;
        }

        public void Pause()
        {
            _ = Log.Info("Timer pause");
            _tickTimer.Stop();
            IsCounting = false;
        }

        public void Reset()
        {
            _ = Log.Info("Timer reset");
            _tickTimer.Stop();
            _remaining = TimeSpan.Zero;
            SetDigitsFromTimeSpan(_configuredDuration);
            ProgressPercent = 100;
            IsCounting = false;
            IsRunning = false;
        }

        /// <summary>Called by the View once the "time's up" alert has been dismissed by the user.</summary>
        public void AcknowledgeTimeUp()
        {
            _ = Log.Info("Timer time up stop");
            _soundService.Stop();
            IsTimeUp = false;
            Reset();
        }

        private void Tick()
        {
            if (_remaining > TimeSpan.Zero)
            {
                _remaining = _remaining.Subtract(TimeSpan.FromSeconds(1));
                SetDigitsFromTimeSpan(_remaining);
                ProgressPercent = (int)(100 * _remaining.TotalSeconds / _configuredDuration.TotalSeconds);
            }
            else
            {
                _tickTimer.Stop();
                _ = Log.Info("Timer time up");
                try
                {
                    _soundService.PlayLooping(_config.CurrentDefaultTimeUpSound);
                }
                catch (Exception ex)
                {
                    // Tick() runs directly on DispatcherTimer.Tick (no async/await boundary to absorb a
                    // throw, unlike the original, whose equivalent call sat inside a fire-and-forget
                    // async Task). Rethrowing here would leave the exception with nowhere to go but
                    // straight up through the dispatcher, crashing/freezing the whole app just because a
                    // sound file failed to load. Log it and keep going so the time's-up screen (and the
                    // Stop button that dismisses it) still shows up even without sound.
                    _ = Log.Warning("Failed to play time up sound: " + ex.Message);
                }
                IsTimeUp = true;
            }
        }

        private void SetDigitsFromTimeSpan(TimeSpan span)
        {
            H1 = (span.Hours / 10).ToString();
            H2 = (span.Hours % 10).ToString();
            M1 = (span.Minutes / 10).ToString();
            M2 = (span.Minutes % 10).ToString();
            S1 = (span.Seconds / 10).ToString();
            S2 = (span.Seconds % 10).ToString();
        }
    }
}
