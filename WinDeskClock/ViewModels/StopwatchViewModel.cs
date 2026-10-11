using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows.Threading;
using WinDeskClock.Mvvm;
using WinDeskClock.Utils;

namespace WinDeskClock.ViewModels
{
    /// <summary>A single recorded lap, ready to display (already formatted).</summary>
    public class StopwatchLapViewModel : ObservableObject
    {
        public int Index { get; }
        public string GapDisplay { get; }
        public string TotalDisplay { get; }

        public StopwatchLapViewModel(int index, TimeSpan gap, TimeSpan total)
        {
            Index = index;
            GapDisplay = "+" + gap.ToString(@"hh\:mm\:ss\.ff");
            TotalDisplay = total.ToString(@"hh\:mm\:ss\.ff");
        }
    }

    /// <summary>
    /// Stopwatch state and logic. Digit property names (H1/H2/M1/M2/S1/S2/MS1/MS2) follow the
    /// app's "tens digit / units digit" convention, matching the original field names.
    ///
    /// Two flags drive the View's animation choice, mirroring the original code exactly:
    /// - IsRunning: true from the *first* Start until Reset. The View only plays the big
    ///   "morph the button row" animation the first time this becomes true.
    /// - IsCounting: true while time is actively accumulating (false while paused). The View
    ///   plays a simple cross-fade between Start/Pause whenever this changes.
    /// </summary>
    public class StopwatchViewModel : ObservableObject
    {
        private readonly Stopwatch _stopwatch = new();
        private readonly DispatcherTimer _tickTimer;

        public StopwatchViewModel()
        {
            _tickTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(10) };
            _tickTimer.Tick += (_, _) => Tick();
        }

        public ObservableCollection<StopwatchLapViewModel> Laps { get; } = new();

        private bool _isRunning;
        public bool IsRunning { get => _isRunning; private set => SetProperty(ref _isRunning, value); }

        private bool _isCounting;
        public bool IsCounting { get => _isCounting; private set => SetProperty(ref _isCounting, value); }

        private bool _isLapListVisible;
        public bool IsLapListVisible { get => _isLapListVisible; private set => SetProperty(ref _isLapListVisible, value); }

        private bool _isProgressFlipped;
        public bool IsProgressFlipped { get => _isProgressFlipped; private set => SetProperty(ref _isProgressFlipped, value); }

        private double _progressValue;
        public double ProgressValue { get => _progressValue; private set => SetProperty(ref _progressValue, value); }

        private string _h1 = "0"; public string H1 { get => _h1; private set => SetProperty(ref _h1, value); }
        private string _h2 = "0"; public string H2 { get => _h2; private set => SetProperty(ref _h2, value); }
        private string _m1 = "0"; public string M1 { get => _m1; private set => SetProperty(ref _m1, value); }
        private string _m2 = "0"; public string M2 { get => _m2; private set => SetProperty(ref _m2, value); }
        private string _s1 = "0"; public string S1 { get => _s1; private set => SetProperty(ref _s1, value); }
        private string _s2 = "0"; public string S2 { get => _s2; private set => SetProperty(ref _s2, value); }
        private string _ms1 = "0"; public string MS1 { get => _ms1; private set => SetProperty(ref _ms1, value); }
        private string _ms2 = "0"; public string MS2 { get => _ms2; private set => SetProperty(ref _ms2, value); }

        private int _lapIndex;
        private TimeSpan _lastLapTime;

        /// <summary>Starts (first time) or resumes (after Pause) the stopwatch.</summary>
        public void Start()
        {
            _ = Log.Info("Stopwatch start");
            _stopwatch.Start();
            _tickTimer.Start();
            IsRunning = true;
            IsCounting = true;
        }

        public void Pause()
        {
            _ = Log.Info("Stopwatch pause");
            _stopwatch.Stop();
            _tickTimer.Stop();
            IsCounting = false;
        }

        /// <summary>Freezes the displayed time immediately, without touching it or anything else yet.
        /// The original stopped ticking the instant Reset was clicked, before any animation (the lap
        /// fade-out, if there were laps) had even started, so the display doesn't keep counting up
        /// during that delay.</summary>
        public void StopTicking() => _tickTimer.Stop();

        public void Reset()
        {
            _ = Log.Info("Stopwatch reset");
            _stopwatch.Reset();
            _tickTimer.Stop();

            Laps.Clear();
            _lapIndex = 0;
            _lastLapTime = TimeSpan.Zero; // the original forgot this, so the first lap after a reset got a bogus split
            IsLapListVisible = false;

            H1 = "0"; H2 = "0"; M1 = "0"; M2 = "0"; S1 = "0"; S2 = "0"; MS1 = "0"; MS2 = "0";
            ProgressValue = 0;
            IsProgressFlipped = false;

            IsCounting = false;
            IsRunning = false;
        }

        /// <summary>
        /// Records a lap *right now* (its number, split and total are fixed at this instant) without
        /// showing it yet, and marks the lap list as deployed. Splitting "record" from "show" lets the
        /// View play the lap area's deploy animation on the very first lap before the card appears
        /// (like the original) without the recorded time drifting by the length of that animation.
        /// </summary>
        public StopwatchLapViewModel CaptureLap()
        {
            _lapIndex++;
            _ = Log.Info("Stopwatch lap: " + _lapIndex);

            IsLapListVisible = true;

            TimeSpan total = _stopwatch.Elapsed;
            TimeSpan gap = total - _lastLapTime;
            _lastLapTime = total;
            return new StopwatchLapViewModel(_lapIndex, gap, total);
        }

        /// <summary>Shows a lap previously returned by <see cref="CaptureLap"/> (newest on top).</summary>
        public void CommitLap(StopwatchLapViewModel lap) => Laps.Insert(0, lap);

        public void AddLap() => CommitLap(CaptureLap());

        private void Tick()
        {
            H1 = _stopwatch.Elapsed.Hours.ToString("00")[0].ToString();
            H2 = _stopwatch.Elapsed.Hours.ToString("00")[1].ToString();
            M1 = _stopwatch.Elapsed.Minutes.ToString("00")[0].ToString();
            M2 = _stopwatch.Elapsed.Minutes.ToString("00")[1].ToString();
            S1 = _stopwatch.Elapsed.Seconds.ToString("00")[0].ToString();
            S2 = _stopwatch.Elapsed.Seconds.ToString("00")[1].ToString();
            MS1 = _stopwatch.Elapsed.Milliseconds.ToString("000")[0].ToString();
            MS2 = _stopwatch.Elapsed.Milliseconds.ToString("000")[1].ToString();

            // Ping-pong the progress bar direction every whole second, exactly like the original.
            if (_stopwatch.Elapsed.Seconds % 2 == 0 && IsProgressFlipped)
            {
                IsProgressFlipped = false;
            }
            else if (_stopwatch.Elapsed.Seconds % 2 != 0 && !IsProgressFlipped)
            {
                IsProgressFlipped = true;
            }

            ProgressValue = IsProgressFlipped
                ? 1000 - _stopwatch.Elapsed.Milliseconds
                : _stopwatch.Elapsed.Milliseconds;
        }
    }
}
