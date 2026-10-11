using System.Collections.ObjectModel;
using System.Windows.Threading;
using WinDeskClock.Models;
using WinDeskClock.Services;
using WinDeskClock.Mvvm;
using WinDeskClock.Utils;

namespace WinDeskClock.ViewModels
{
    /// <summary>
    /// Owns the alarm list: loading/saving (through <see cref="IAlarmRepository"/>, once for the
    /// whole list instead of per-field-per-disk-access like the original), sorting, per-minute
    /// firing, and the "only one card expanded at a time" coordination.
    /// </summary>
    public class AlarmsViewModel : ObservableObject
    {
        private readonly IAlarmRepository _repository;
        private readonly ILocalizationService _localization;
        private readonly IFileDialogService _fileDialogService;
        private readonly IConfigService _config;
        private readonly ISoundService _soundService;
        private readonly DispatcherTimer _minuteTimer;

        private bool _cardEditorBusy;
        private AlarmItemViewModel? _pendingDelete;

        public AlarmsViewModel(
            IAlarmRepository repository,
            ILocalizationService localization,
            IFileDialogService fileDialogService,
            IConfigService config,
            ISoundService soundService)
        {
            _repository = repository;
            _localization = localization;
            _fileDialogService = fileDialogService;
            _config = config;
            _soundService = soundService;

            _minuteTimer = new DispatcherTimer { Interval = TimeSpan.FromMinutes(1) };
            _minuteTimer.Tick += (_, _) => Tick();
        }

        public ObservableCollection<AlarmItemViewModel> Alarms { get; } = new();

        public bool HasAlarms => Alarms.Count > 0;

        // ----- Ringing alert overlay -----
        private bool _isAlertActive;
        public bool IsAlertActive { get => _isAlertActive; private set => SetProperty(ref _isAlertActive, value); }

        private string _alertName = string.Empty;
        public string AlertName { get => _alertName; private set => SetProperty(ref _alertName, value); }

        private string _alertTime = string.Empty;
        public string AlertTime { get => _alertTime; private set => SetProperty(ref _alertTime, value); }

        public async Task LoadAsync()
        {
            var models = await _repository.LoadAllAsync();
            Alarms.Clear();
            foreach (var model in models)
            {
                AddItem(model);
            }
            SortInPlace();
            RefreshEtaTexts();
            _minuteTimer.Start();
        }

        private void AddItem(AlarmModel model)
        {
            var item = new AlarmItemViewModel(model, _localization, _fileDialogService);
            item.EnabledChanged += async (_, _) => await PersistAsync();
            item.OpenRequested += (sender, _) => RequestExpand((AlarmItemViewModel)sender!);
            item.SaveRequested += async (sender, _) => await SaveAsync((AlarmItemViewModel)sender!);
            item.CancelRequested += (sender, _) => CancelEdit((AlarmItemViewModel)sender!);
            item.DeleteRequested += async (sender, _) => await DeleteAsync((AlarmItemViewModel)sender!);
            Alarms.Add(item);
            OnPropertyChanged(nameof(HasAlarms));
        }

        /// <summary>
        /// Creates a new alarm and persists it, but does *not* sort or expand it yet - the original's
        /// AddAlarmBtn_Click grew the new (still unsorted) card into the list first and only *then*
        /// ran its own copy of the open animation once that grow finished. The View (which knows how
        /// long that grow animation takes) is responsible for waiting before calling
        /// <see cref="ExpandNewlyAdded"/>. Returns null (without doing anything) if a card is already
        /// being edited, exactly like the original's CardDeployed guard.
        /// </summary>
        public async Task<AlarmItemViewModel?> AddAlarmAsync()
        {
            if (_cardEditorBusy)
            {
                return null;
            }
            _cardEditorBusy = true;

            DateTime now = DateTime.Now;
            var model = new AlarmModel
            {
                Hour = now.Hour,
                Minute = now.Minute,
                Name = string.Empty,
                Days = new List<int>(),
                SoundMode = AlarmSoundMode.Default,
                Enabled = true
            };

            AddItem(model);
            await PersistAsync();

            return Alarms.First(a => a.Saved == model);
        }

        /// <summary>Raised when the expanded card changes: the new card (or null when collapsing
        /// back to the list). The View uses this to shrink/hide the *other* cards while one is
        /// expanded, and restore them afterwards.</summary>
        public event EventHandler<AlarmItemViewModel?>? ExpansionChanged;

        /// <summary>Collapses every other card and expands the given one. No-ops if a card is
        /// already being edited (matches the original's `!CardDeployed` guard on AlarmCardOpen) -
        /// the original only allowed switching to a different card by first closing the open one.</summary>
        public void RequestExpand(AlarmItemViewModel item)
        {
            if (_cardEditorBusy)
            {
                return;
            }
            _cardEditorBusy = true;
            ExpandCore(item);
        }

        /// <summary>Expands the given card without the busy re-check - used once, right after
        /// AddAlarmAsync, which already claimed the busy flag itself.</summary>
        public void ExpandNewlyAdded(AlarmItemViewModel item) => ExpandCore(item);

        private void ExpandCore(AlarmItemViewModel item)
        {
            item.LoadDraftFromSaved();
            foreach (var alarm in Alarms)
            {
                alarm.IsHiddenBySibling = alarm != item;
                alarm.IsExpanded = alarm == item;
            }
            ExpansionChanged?.Invoke(this, item);
        }

        public void CancelEdit(AlarmItemViewModel item)
        {
            item.LoadDraftFromSaved();
            CollapseAll();
        }

        /// <summary>
        /// Commits and persists the edit, then starts the close animation - matching the original's
        /// Save button, which wrote to disk (AlarmCardSave) *before* calling AlarmCardClose. Sorting
        /// and refreshing the ETA text are deferred to <see cref="FinishCollapse"/>: the original only
        /// did those once AlarmCardClose's own shrink/grow animation had fully finished, not the
        /// instant Save was pressed - doing it earlier is what made the list re-sort itself out from
        /// under the still-running close animation.
        /// </summary>
        public async Task SaveAsync(AlarmItemViewModel item)
        {
            item.CommitDraftToSaved();
            await PersistAsync();
            CollapseAll();
        }

        /// <summary>
        /// Starts the close animation; the actual removal is deferred until the View reports that
        /// animation finished (<see cref="FinishCollapse"/>) - matching the original's Delete button,
        /// which ran AlarmCardClose (collapsing the editor back down) *before* AlarmCardDelete (which
        /// then did its own separate shrink-and-remove). Removing immediately instead made the
        /// in-progress close animation fight over a card that was simultaneously being torn down.
        /// </summary>
        public Task DeleteAsync(AlarmItemViewModel item)
        {
            _pendingDelete = item;
            CollapseAll();
            return Task.CompletedTask;
        }

        private void CollapseAll()
        {
            foreach (var alarm in Alarms)
            {
                alarm.IsExpanded = false;
                alarm.IsHiddenBySibling = false;
            }
            ExpansionChanged?.Invoke(this, null);
        }

        /// <summary>
        /// Called by the View once its "restore the other cards" close animation has fully played out.
        /// This is where the original's AlarmCardClose called SortAlarmCards()/AlarmCardETAUpdate() -
        /// only after everything had finished animating, never the instant Save/Cancel/Delete was
        /// clicked. Returns the alarm to delete, if this close followed a Delete request, so the View
        /// can run the original's separate delete-shrink animation strictly afterwards (matching the
        /// original awaiting AlarmCardClose fully before starting AlarmCardDelete).
        /// </summary>
        public AlarmItemViewModel? FinishCollapse()
        {
            _cardEditorBusy = false;
            SortInPlace();
            RefreshEtaTexts();

            var pending = _pendingDelete;
            _pendingDelete = null;
            return pending;
        }

        public async Task RemovePendingDeleteAsync(AlarmItemViewModel item)
        {
            Alarms.Remove(item);
            OnPropertyChanged(nameof(HasAlarms));
            await PersistAsync();
        }

        private Task PersistAsync() => _repository.SaveAllAsync(Alarms.Select(a => a.Saved));

        /// <summary>
        /// Primary key: today's clock time (so alarms with the same ring time group together
        /// regardless of which days they repeat on). Secondary key: the true next-occurrence ETA.
        /// Matches the original GetAlarmTime/GetETAAlarm sort exactly.
        /// </summary>
        private void SortInPlace()
        {
            DateTime now = DateTime.Now;
            var ordered = Alarms
                .OrderBy(a => new DateTime(now.Year, now.Month, now.Day, a.Saved.Hour, a.Saved.Minute, 0))
                .ThenBy(a => ComputeNextOccurrence(a.Saved, now, pushPastOnceForward: false) - now)
                .ToList();

            for (int i = 0; i < ordered.Count; i++)
            {
                int currentIndex = Alarms.IndexOf(ordered[i]);
                if (currentIndex != i)
                {
                    Alarms.Move(currentIndex, i);
                }
            }
        }

        public void RefreshEtaTexts()
        {
            foreach (var alarm in Alarms)
            {
                alarm.EtaSummary = alarm.DaySummaryText() + " • " + FormatEta(alarm.Saved);
            }
        }

        private string FormatEta(AlarmModel alarm)
        {
            DateTime now = DateTime.Now;
            TimeSpan diff = ComputeNextOccurrence(alarm, now, pushPastOnceForward: true) - now;

            if (diff.Days == 0)
            {
                return diff.Hours == 0
                    ? _localization.GetString("alarm.remaining.inmin", diff.Minutes.ToString())
                    : _localization.GetString("alarm.remaining.inhour", diff.Hours.ToString(), diff.Minutes.ToString());
            }

            return diff.Days == 1
                ? _localization.GetString("alarm.remaining.inday", diff.Days.ToString())
                : _localization.GetString("alarm.remaining.indays", diff.Days.ToString());
        }

        /// <summary>
        /// Next DateTime this alarm rings at, from "now". For a one-shot ("once") alarm whose time
        /// has already passed today, the original app used two different conventions depending on
        /// purpose: sorting treats it as if it had rung *yesterday* (so an overdue one-shot alarm
        /// sorts to the top), while the displayed countdown treats it as ringing *tomorrow*. Both
        /// are preserved here via <paramref name="pushPastOnceForward"/>.
        /// </summary>
        private static DateTime ComputeNextOccurrence(AlarmModel alarm, DateTime now, bool pushPastOnceForward)
        {
            DateTime target = new(now.Year, now.Month, now.Day, alarm.Hour, alarm.Minute, 0);

            if (alarm.IsOnce)
            {
                if (target < now)
                {
                    target = target.AddDays(pushPastOnceForward ? 1 : -1);
                }
                return target;
            }

            int today = now.DayOfWeek == DayOfWeek.Sunday ? 7 : (int)now.DayOfWeek;
            int nextDay = alarm.Days
                .Where(d => d > today || (d == today && target >= now))
                .DefaultIfEmpty(alarm.Days.Min())
                .First();
            int daysToAdd = nextDay >= today ? nextDay - today : 7 - today + nextDay;
            return target.AddDays(daysToAdd);
        }

        private void Tick()
        {
            DateTime now = DateTime.Now;
            int isoToday = now.DayOfWeek == DayOfWeek.Sunday ? 7 : (int)now.DayOfWeek;

            foreach (var alarm in Alarms.ToList())
            {
                var model = alarm.Saved;
                bool timeMatches = model.Hour == now.Hour && model.Minute == now.Minute;
                bool dayMatches = model.IsOnce || model.Days.Contains(isoToday);
                if (timeMatches && dayMatches && model.Enabled)
                {
                    _ = TriggerAlertAsync(alarm);
                }
            }

            RefreshEtaTexts();

            // Re-align to the next minute boundary, exactly like the original.
            _minuteTimer.Interval = TimeSpan.FromSeconds(60 - DateTime.Now.Second);
        }

        public event EventHandler? ScreenWakeRequested;

        private async Task TriggerAlertAsync(AlarmItemViewModel alarm)
        {
            if (IsAlertActive)
            {
                return;
            }

            _ = Log.Info($"Alarm alert: {alarm.Saved.Id}");
            IsAlertActive = true;
            ScreenWakeRequested?.Invoke(this, EventArgs.Empty);

            AlertName = string.IsNullOrEmpty(alarm.Saved.Name) ? "Alarm" : alarm.Saved.Name;
            AlertTime = alarm.TimeDisplay;

            if (alarm.Saved.SoundMode != AlarmSoundMode.None)
            {
                string soundPath = alarm.Saved.SoundMode == AlarmSoundMode.Default
                    ? _config.CurrentDefaultAlarmSound
                    : alarm.Saved.CustomSoundPath;
                try
                {
                    _soundService.PlayLooping(soundPath);
                }
                catch (Exception ex)
                {
                    // A rethrow here would fault this fire-and-forget Task before IsAlertActive is ever
                    // set back to false (that happens in AcknowledgeAlertAsync, below the loop this skips
                    // straight past). Since RequestExpand-style re-entry is guarded by "if (IsAlertActive)
                    // return", a single missing/corrupt sound file would silently disable every alarm from
                    // then on. Log it and keep going without sound instead.
                    _ = Log.Warning($"Failed to play alarm sound: {ex.Message}");
                }
            }

            double timeoutMinutes = _config.CurrentAlarmTimeoutDelayMinutes;
            TimeSpan elapsed = TimeSpan.Zero;
            while (IsAlertActive)
            {
                await Task.Delay(100);
                elapsed = elapsed.Add(TimeSpan.FromMilliseconds(100));
                if (timeoutMinutes != 0 && elapsed.TotalMinutes >= timeoutMinutes)
                {
                    _ = Log.Warning($"Alarm alert timeout reached: {alarm.Saved.Id}");
                    break;
                }
            }

            await AcknowledgeAlertAsync(alarm);
        }

        /// <summary>Called by the View when the user presses "Stop" on the ringing alert.</summary>
        public void StopAlert() => IsAlertActive = false;

        private async Task AcknowledgeAlertAsync(AlarmItemViewModel alarm)
        {
            _soundService.Stop();

            if (alarm.Saved.IsOnce)
            {
                alarm.IsEnabled = false;
                await PersistAsync();
            }

            IsAlertActive = false;
        }
    }
}
