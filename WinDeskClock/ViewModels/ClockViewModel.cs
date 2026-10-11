using System.Globalization;
using System.Windows.Threading;
using WinDeskClock.Mvvm;
using WinDeskClock.Services;

namespace WinDeskClock.ViewModels
{
    /// <summary>
    /// Drives the current time/date, shared by both clock faces (FluentClock and FbxClock).
    /// Property names follow the app's existing "H1/H2 = tens/units digit of the Hour" convention
    /// (also used for the stopwatch and timer) so they map 1:1 to what the XAML used to be named.
    ///
    /// This ViewModel only exposes the *current logical value* of each digit/field; it has no
    /// knowledge of how a change is displayed (sliding text, sliding bitmaps, ...). Each View
    /// listens to PropertyChanged and decides how to animate its own controls - see UiAnimator.
    /// </summary>
    public class ClockViewModel : ObservableObject
    {
        private readonly ILocalizationService _localization;
        private readonly IConfigService _config;
        private DispatcherTimer? _timer;

        public ClockViewModel(ILocalizationService localization, IConfigService config)
        {
            _localization = localization;
            _config = config;
        }

        public bool ShowSeconds => _config.CurrentClockShowSeconds;

        private string _h1 = "1";
        public string H1 { get => _h1; private set => SetProperty(ref _h1, value); }

        private string _h2 = "7";
        public string H2 { get => _h2; private set => SetProperty(ref _h2, value); }

        private string _m1 = "2";
        public string M1 { get => _m1; private set => SetProperty(ref _m1, value); }

        private string _m2 = "0";
        public string M2 { get => _m2; private set => SetProperty(ref _m2, value); }

        private string _s1 = "0";
        public string S1 { get => _s1; private set => SetProperty(ref _s1, value); }

        private string _s2 = "0";
        public string S2 { get => _s2; private set => SetProperty(ref _s2, value); }

        private string _dName = "MON";
        /// <summary>Localized, upper-cased short day-of-week name (e.g. "MON").</summary>
        public string DName { get => _dName; private set => SetProperty(ref _dName, value); }

        private string _dDay = "7";
        public string DDay { get => _dDay; private set => SetProperty(ref _dDay, value); }

        private string _dMonth = "AUG";
        /// <summary>Localized, upper-cased short month name (e.g. "AUG").</summary>
        public string DMonth { get => _dMonth; private set => SetProperty(ref _dMonth, value); }

        private string _dYear = "2006";
        public string DYear { get => _dYear; private set => SetProperty(ref _dYear, value); }

        /// <summary>Starts the per-second tick. Safe to call more than once (no-op after the first call).</summary>
        public void Start()
        {
            if (_timer != null)
            {
                return;
            }

            _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _timer.Tick += (_, _) => Tick();
            _timer.Start();
        }

        private void Tick()
        {
            DateTime now = DateTime.Now;
            H1 = now.Hour.ToString("00")[0].ToString();
            H2 = now.Hour.ToString("00")[1].ToString();
            M1 = now.Minute.ToString("00")[0].ToString();
            M2 = now.Minute.ToString("00")[1].ToString();
            S1 = now.Second.ToString("00")[0].ToString();
            S2 = now.Second.ToString("00")[1].ToString();
            DName = _localization.GetString($"clock.days.short.{now.DayOfWeek.ToString().ToLower()}").ToUpper();
            DDay = now.Day.ToString();
            DMonth = _localization.GetString($"clock.months.short.{now.ToString("MMMM", CultureInfo.GetCultureInfo("en-US")).ToLower()}").ToUpper();
            DYear = now.Year.ToString("0000");

            // Re-align the timer to the next wall-clock second, so it doesn't slowly drift.
            int msUntilNextSecond = 1000 - DateTime.Now.Millisecond;
            _timer!.Interval = TimeSpan.FromMilliseconds(msUntilNextSecond);
        }
    }
}
