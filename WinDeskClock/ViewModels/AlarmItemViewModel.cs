using WinDeskClock.Models;
using WinDeskClock.Mvvm;
using WinDeskClock.Services;

namespace WinDeskClock.ViewModels
{
    /// <summary>
    /// One alarm card. Holds the *saved* alarm (<see cref="Saved"/>) plus a set of editable
    /// "Draft*" properties bound to the expanded edit UI - mirroring the original app's edit
    /// controls, which held pending edits until Save/Cancel. Save writes the draft back into
    /// Saved (and asks the repository to persist); Cancel reloads the draft from Saved.
    /// </summary>
    public class AlarmItemViewModel : ObservableObject
    {
        private readonly ILocalizationService _localization;
        private readonly IFileDialogService _fileDialogService;

        public AlarmItemViewModel(AlarmModel saved, ILocalizationService localization, IFileDialogService fileDialogService)
        {
            _localization = localization;
            _fileDialogService = fileDialogService;
            Saved = saved;
            LoadDraftFromSaved();
        }

        public AlarmModel Saved { get; private set; }

        // ----- Collapsed-card display -----
        public string TimeDisplay => $"{Saved.Hour:00}:{Saved.Minute:00}";
        public string NameDisplay => Saved.Name;

        private string _etaSummary = string.Empty;
        /// <summary>"Mon, Tue... - in 3 hours" style summary, refreshed by AlarmsViewModel.</summary>
        public string EtaSummary { get => _etaSummary; set => SetProperty(ref _etaSummary, value); }

        // ----- Shared enabled toggle (both the small and big switch bind to this one property,
        // which keeps them in sync automatically - the original had to sync two separate controls
        // by hand every time either one changed). -----
        private bool _isEnabled;
        public bool IsEnabled
        {
            get => _isEnabled;
            set
            {
                if (SetProperty(ref _isEnabled, value))
                {
                    Saved.Enabled = value;
                    EnabledChanged?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        // ----- Expansion / visibility -----
        private bool _isExpanded;
        /// <summary>Set by AlarmsViewModel; true while this card's edit view is open.</summary>
        public bool IsExpanded { get => _isExpanded; set => SetProperty(ref _isExpanded, value); }

        private bool _isHiddenBySibling;
        /// <summary>Set by AlarmsViewModel; true while a *different* card is expanded.</summary>
        public bool IsHiddenBySibling { get => _isHiddenBySibling; set => SetProperty(ref _isHiddenBySibling, value); }

        // ----- Draft fields (the expanded edit view) -----
        private string _draftName = string.Empty;
        public string DraftName { get => _draftName; set => SetProperty(ref _draftName, value); }

        private int _draftHour;
        public int DraftHour
        {
            get => _draftHour;
            private set
            {
                if (SetProperty(ref _draftHour, value))
                {
                    OnPropertyChanged(nameof(DraftHourDisplay));
                }
            }
        }
        public string DraftHourDisplay => _draftHour.ToString("00");

        private int _draftMinute;
        public int DraftMinute
        {
            get => _draftMinute;
            private set
            {
                if (SetProperty(ref _draftMinute, value))
                {
                    OnPropertyChanged(nameof(DraftMinuteDisplay));
                }
            }
        }
        public string DraftMinuteDisplay => _draftMinute.ToString("00");

        public void IncrementHour() => DraftHour = DraftHour == 23 ? 0 : DraftHour + 1;
        public void DecrementHour() => DraftHour = DraftHour == 0 ? 23 : DraftHour - 1;
        public void IncrementMinute() => DraftMinute = DraftMinute == 59 ? 0 : DraftMinute + 1;
        public void DecrementMinute() => DraftMinute = DraftMinute == 0 ? 59 : DraftMinute - 1;

        private HashSet<int> _draftDays = new();

        /// <summary>True when no specific day is selected (a one-shot alarm).</summary>
        public bool DraftIsOnce => _draftDays.Count == 0;
        public bool DraftMonday => _draftDays.Contains(1);
        public bool DraftTuesday => _draftDays.Contains(2);
        public bool DraftWednesday => _draftDays.Contains(3);
        public bool DraftThursday => _draftDays.Contains(4);
        public bool DraftFriday => _draftDays.Contains(5);
        public bool DraftSaturday => _draftDays.Contains(6);
        public bool DraftSunday => _draftDays.Contains(7);

        /// <summary>
        /// Toggling "Once": switching it ON clears every day; switching it OFF (because the user
        /// wants specific days instead) selects every day as a starting point - exactly like the
        /// original ToggleButton coordination logic.
        /// </summary>
        public void ToggleOnce()
        {
            _draftDays = DraftIsOnce ? new HashSet<int> { 1, 2, 3, 4, 5, 6, 7 } : new HashSet<int>();
            RaiseDayPropertiesChanged();
        }

        /// <summary>Toggles a single day (1 = Monday ... 7 = Sunday). Falls back to "Once" if that empties the set.</summary>
        public void ToggleDay(int isoDay)
        {
            if (!_draftDays.Remove(isoDay))
            {
                _draftDays.Add(isoDay);
            }
            RaiseDayPropertiesChanged();
        }

        private void RaiseDayPropertiesChanged() => OnPropertiesChanged(
            nameof(DraftIsOnce), nameof(DraftMonday), nameof(DraftTuesday), nameof(DraftWednesday),
            nameof(DraftThursday), nameof(DraftFriday), nameof(DraftSaturday), nameof(DraftSunday));

        public string MondayAbbreviation => _localization.GetString("clock.days.abbr.monday");
        public string TuesdayAbbreviation => _localization.GetString("clock.days.abbr.tuesday");
        public string WednesdayAbbreviation => _localization.GetString("clock.days.abbr.wednesday");
        public string ThursdayAbbreviation => _localization.GetString("clock.days.abbr.thursday");
        public string FridayAbbreviation => _localization.GetString("clock.days.abbr.friday");
        public string SaturdayAbbreviation => _localization.GetString("clock.days.abbr.saturday");
        public string SundayAbbreviation => _localization.GetString("clock.days.abbr.sunday");
        public string NamePlaceholder => _localization.GetString("alarm.name");
        public string DefaultSoundLabel => _localization.GetString("alarm.defsounds");
        public string DeleteLabel => _localization.GetString("alarm.delete");
        public string CancelLabel => _localization.GetString("alarm.cancel");
        public string SaveLabel => _localization.GetString("alarm.save");

        private bool _draftSoundEnabled;
        public bool DraftSoundEnabled { get => _draftSoundEnabled; set => SetProperty(ref _draftSoundEnabled, value); }

        private bool _draftSoundIsCustom;
        public bool DraftSoundIsCustom { get => _draftSoundIsCustom; private set => SetProperty(ref _draftSoundIsCustom, value); }

        private string _draftCustomSoundPath = string.Empty;
        public string DraftCustomSoundPath { get => _draftCustomSoundPath; private set => SetProperty(ref _draftCustomSoundPath, value, () => OnPropertyChanged(nameof(DraftCustomSoundLabel))); }

        /// <summary>What the "Custom" combo box item should display: the chosen file name, or a placeholder.</summary>
        public string DraftCustomSoundLabel => string.IsNullOrEmpty(DraftCustomSoundPath)
            ? _localization.GetString("alarm.custsounds")
            : System.IO.Path.GetFileName(DraftCustomSoundPath);

        public void SelectDefaultSound()
        {
            DraftSoundIsCustom = false;
        }

        /// <summary>Opens a file picker for a custom .wav; falls back to "Default" if cancelled - like the original.</summary>
        public void SelectCustomSound()
        {
            string? path = _fileDialogService.PickWavFile("WinDeskClock Custom Alarm Sound");
            if (path != null)
            {
                DraftCustomSoundPath = path;
                DraftSoundIsCustom = true;
            }
            else
            {
                DraftSoundIsCustom = false;
            }
        }

        public string DaySummaryText()
        {
            if (Saved.IsOnce)
            {
                return _localization.GetString("alarm.repeat.once");
            }

            string daysKey = Saved.DaysToStorageString();
            if (daysKey == "12345") return _localization.GetString("alarm.repeat.workdays");
            if (daysKey == "67") return _localization.GetString("alarm.repeat.weekend");
            if (daysKey == "1234567") return _localization.GetString("alarm.repeat.everyday");

            var names = new List<string>();
            foreach (int day in Saved.Days.OrderBy(d => d))
            {
                names.Add(_localization.GetString(DayLongNameKey(day)));
            }
            return string.Join(", ", names);
        }

        private static string DayLongNameKey(int isoDay) => isoDay switch
        {
            1 => "clock.days.long.monday",
            2 => "clock.days.long.tuesday",
            3 => "clock.days.long.wednesday",
            4 => "clock.days.long.thursday",
            5 => "clock.days.long.friday",
            6 => "clock.days.long.saturday",
            _ => "clock.days.long.sunday"
        };

        /// <summary>Copies Saved -> Draft. Used both when opening a card and when cancelling an edit.</summary>
        public void LoadDraftFromSaved()
        {
            DraftName = Saved.Name;
            DraftHour = Saved.Hour;
            DraftMinute = Saved.Minute;
            _draftDays = new HashSet<int>(Saved.Days);
            RaiseDayPropertiesChanged();
            DraftSoundEnabled = Saved.SoundMode != AlarmSoundMode.None;
            DraftSoundIsCustom = Saved.SoundMode == AlarmSoundMode.Custom;
            DraftCustomSoundPath = Saved.CustomSoundPath;
            _isEnabled = Saved.Enabled;
            OnPropertyChanged(nameof(IsEnabled));
        }

        /// <summary>Copies Draft -> Saved. Persisting to disk is the caller's (AlarmsViewModel's) job.</summary>
        public void CommitDraftToSaved()
        {
            Saved.Name = DraftName;
            Saved.Hour = DraftHour;
            Saved.Minute = DraftMinute;
            Saved.Days = new List<int>(_draftDays);
            Saved.SoundMode = !DraftSoundEnabled
                ? AlarmSoundMode.None
                : DraftSoundIsCustom ? AlarmSoundMode.Custom : AlarmSoundMode.Default;
            Saved.CustomSoundPath = DraftCustomSoundPath;
            // Note: Saved.Enabled is already kept in sync by the IsEnabled property setter.

            OnPropertiesChanged(nameof(TimeDisplay), nameof(NameDisplay));
        }

        /// <summary>Raised whenever IsEnabled changes (including via the collapsed-card switch), so
        /// AlarmsViewModel can persist the change immediately, like the original did.</summary>
        public event EventHandler? EnabledChanged;

        // ----- Requests handled by the parent AlarmsViewModel (keeps this class free of any
        // reference to the collection it lives in). -----
        public event EventHandler? OpenRequested;
        public event EventHandler? SaveRequested;
        public event EventHandler? CancelRequested;
        public event EventHandler? DeleteRequested;

        public void RequestOpen() => OpenRequested?.Invoke(this, EventArgs.Empty);
        public void RequestSave() => SaveRequested?.Invoke(this, EventArgs.Empty);
        public void RequestCancel() => CancelRequested?.Invoke(this, EventArgs.Empty);
        public void RequestDelete() => DeleteRequested?.Invoke(this, EventArgs.Empty);
    }
}
