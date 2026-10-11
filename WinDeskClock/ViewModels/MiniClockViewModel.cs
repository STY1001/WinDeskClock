using System.Windows.Threading;
using WinDeskClock.Mvvm;

namespace WinDeskClock.ViewModels
{
    /// <summary>
    /// Drives the small "hh:mm" clock shown at the top of the down-menu (above Alarm/Stopwatch/Timer).
    /// Updates once a minute, independently from the main <see cref="ClockViewModel"/>.
    /// </summary>
    public class MiniClockViewModel : ObservableObject
    {
        private DispatcherTimer? _timer;

        private string _hour = "17";
        public string Hour { get => _hour; private set => SetProperty(ref _hour, value); }

        private string _minute = "20";
        public string Minute { get => _minute; private set => SetProperty(ref _minute, value); }

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
            Hour = now.Hour.ToString("0");
            Minute = now.Minute.ToString("00");

            // Re-align to the next minute boundary instead of polling every second forever.
            int secondsUntilNextMinute = 60 - DateTime.Now.Second;
            _timer!.Interval = TimeSpan.FromSeconds(secondsUntilNextMinute);
        }
    }
}
