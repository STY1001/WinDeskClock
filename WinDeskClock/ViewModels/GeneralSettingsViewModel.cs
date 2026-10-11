using System.Collections.ObjectModel;
using WinDeskClock.Mvvm;
using WinDeskClock.Services;

namespace WinDeskClock.ViewModels
{
    /// <summary>A single "pin this plugin to the carousel" checkbox row.</summary>
    public class PluginPinItemViewModel : ObservableObject
    {
        private readonly IConfigService _config;
        public string PluginId { get; }
        public string DisplayName { get; }

        public PluginPinItemViewModel(string pluginId, string displayName, bool isPinned, IConfigService config)
        {
            PluginId = pluginId;
            DisplayName = displayName;
            _config = config;
            _isPinned = isPinned;
        }

        private bool _isPinned;
        public bool IsPinned
        {
            get => _isPinned;
            set
            {
                if (SetProperty(ref _isPinned, value))
                {
                    // Pinning/unpinning, like in the original, does not require a restart.
                    if (value)
                    {
                        _ = _config.AddPinnedPluginAsync(PluginId);
                    }
                    else
                    {
                        _ = _config.RemovePinnedPluginAsync(PluginId);
                    }
                }
            }
        }
    }

    /// <summary>
    /// The General settings page: everything here edits IConfigService's "Draft" values, applied
    /// only when SettingsViewModel.Save runs. Every field marks a restart as needed except Volume
    /// (applied live to the OS, never persisted) and plugin pinning (also applied live) - matching
    /// the original exactly.
    /// </summary>
    public class GeneralSettingsViewModel : ObservableObject
    {
        private readonly IConfigService _config;
        private readonly ILocalizationService _localization;
        private readonly IFileDialogService _fileDialogService;
        private readonly IAudioVolumeService _volumeService;
        private readonly IPluginCatalogService _pluginCatalog;

        public GeneralSettingsViewModel(
            IConfigService config,
            ILocalizationService localization,
            IFileDialogService fileDialogService,
            IAudioVolumeService volumeService,
            IPluginCatalogService pluginCatalog)
        {
            _config = config;
            _localization = localization;
            _fileDialogService = fileDialogService;
            _volumeService = volumeService;
            _pluginCatalog = pluginCatalog;
        }

        public IReadOnlyDictionary<string, string> AvailableLanguages => _localization.AvailableLanguages;

        public string DraftLanguage
        {
            get => _config.DraftLanguage;
            set { _config.DraftLanguage = value; _config.MarkRestartNeeded(); OnPropertyChanged(); }
        }

        private int _volumePercent;
        public int VolumePercent
        {
            get => _volumePercent;
            set
            {
                if (SetProperty(ref _volumePercent, value))
                {
                    _volumeService.SetVolumePercent(value);
                }
            }
        }

        public bool DraftBlurEffectEnabled
        {
            get => _config.DraftBlurEffectEnabled;
            set { _config.DraftBlurEffectEnabled = value; _config.MarkRestartNeeded(); OnPropertyChanged(); }
        }

        /// <summary>"none" | "fade" | "crt".</summary>
        public string DraftScreenOnOffEffect
        {
            get => _config.DraftScreenOnOffEffect;
            set { _config.DraftScreenOnOffEffect = value; _config.MarkRestartNeeded(); OnPropertyChanged(); }
        }

        public bool DraftScreenAutoWakeUp
        {
            get => _config.DraftScreenAutoWakeUp;
            set { _config.DraftScreenAutoWakeUp = value; _config.MarkRestartNeeded(); OnPropertiesChanged(nameof(DraftScreenAutoWakeUp)); }
        }

        public int DraftScreenAutoWakeUpHour
        {
            get => _config.DraftScreenAutoWakeUpTime.Hour;
            set { SetWakeUpTime(value, DraftScreenAutoWakeUpMinute); }
        }

        public int DraftScreenAutoWakeUpMinute
        {
            get => _config.DraftScreenAutoWakeUpTime.Minute;
            set { SetWakeUpTime(DraftScreenAutoWakeUpHour, value); }
        }

        private void SetWakeUpTime(int hour, int minute)
        {
            _config.DraftScreenAutoWakeUpTime = new TimeOnly(hour, minute);
            _config.MarkRestartNeeded();
            OnPropertiesChanged(nameof(DraftScreenAutoWakeUpHour), nameof(DraftScreenAutoWakeUpMinute));
        }

        public int DraftMenuCloseDelaySeconds
        {
            get => _config.DraftMenuCloseDelaySeconds;
            set { _config.DraftMenuCloseDelaySeconds = value; _config.MarkRestartNeeded(); OnPropertyChanged(); }
        }

        public bool DraftClockShowSeconds
        {
            get => _config.DraftClockShowSeconds;
            set { _config.DraftClockShowSeconds = value; _config.MarkRestartNeeded(); OnPropertyChanged(); }
        }

        /// <summary>The Show-seconds toggle is disabled while the Freebox clock style is selected.</summary>
        public bool IsShowSecondsEditable => !DraftClockFbxStyle;

        public bool DraftClockFbxStyle
        {
            get => _config.DraftClockFbxStyle;
            set
            {
                _config.DraftClockFbxStyle = value;
                _config.MarkRestartNeeded();
                // Mirrors the original: switching to the Freebox style forces seconds off,
                // switching back to the Fluent style restores them.
                _config.DraftClockShowSeconds = !value;
                OnPropertiesChanged(nameof(DraftClockFbxStyle), nameof(IsShowSecondsEditable), nameof(DraftClockShowSeconds));
            }
        }

        private string _defaultAlarmSoundLabel = string.Empty;
        public string DefaultAlarmSoundLabel { get => _defaultAlarmSoundLabel; private set => SetProperty(ref _defaultAlarmSoundLabel, value); }

        public void BrowseDefaultAlarmSound()
        {
            string? path = _fileDialogService.PickWavFile("WinDeskClock Default Alarm Sound");
            if (path == null)
            {
                return;
            }
            _config.DraftDefaultAlarmSound = path;
            _config.MarkRestartNeeded();
            DefaultAlarmSoundLabel = _localization.GetString("settings.general.alarm.defaultsound.desc", path);
        }

        public int DraftAlarmTimeoutDelayMinutes
        {
            get => _config.DraftAlarmTimeoutDelayMinutes;
            set { _config.DraftAlarmTimeoutDelayMinutes = value; _config.MarkRestartNeeded(); OnPropertiesChanged(nameof(DraftAlarmTimeoutDelayMinutes), nameof(AlarmTimeoutLabel)); }
        }

        public string AlarmTimeoutLabel => DraftAlarmTimeoutDelayMinutes switch
        {
            0 => _localization.GetString("settings.general.alarm.timeout.never"),
            1 => _localization.GetString("settings.general.alarm.timeout.min", "1"),
            _ => _localization.GetString("settings.general.alarm.timeout.mins", DraftAlarmTimeoutDelayMinutes.ToString())
        };

        private string _timerSoundLabel = string.Empty;
        public string TimerSoundLabel { get => _timerSoundLabel; private set => SetProperty(ref _timerSoundLabel, value); }

        public void BrowseTimerSound()
        {
            string? path = _fileDialogService.PickWavFile("WinDeskClock Timer Sound");
            if (path == null)
            {
                return;
            }
            _config.DraftDefaultTimeUpSound = path;
            _config.MarkRestartNeeded();
            TimerSoundLabel = _localization.GetString("settings.general.timer.defaultsound.desc", path);
        }

        public int DraftCarouselDelaySeconds
        {
            get => _config.DraftCarouselDelaySeconds;
            set { _config.DraftCarouselDelaySeconds = value; _config.MarkRestartNeeded(); OnPropertiesChanged(nameof(DraftCarouselDelaySeconds), nameof(CarouselDelayLabel)); }
        }

        public string CarouselDelayLabel => _localization.GetString("settings.general.plugin.carouseldelay.secs", DraftCarouselDelaySeconds.ToString());

        public string MenuAutoCloseLabel => DraftMenuCloseDelaySeconds == 0
            ? _localization.GetString("settings.general.menuautoclose.never")
            : _localization.GetString("settings.general.menuautoclose.secs", DraftMenuCloseDelaySeconds.ToString());

        public ObservableCollection<PluginPinItemViewModel> PinnablePlugins { get; } = new();

        /// <summary>Refreshes every field from the current effective settings. Call each time the page is shown.</summary>
        public async Task LoadAsync()
        {
            _volumePercent = _volumeService.GetVolumePercent();
            OnPropertyChanged(nameof(VolumePercent));
            DefaultAlarmSoundLabel = _localization.GetString("settings.general.alarm.defaultsound.desc", _config.DraftDefaultAlarmSound);
            TimerSoundLabel = _localization.GetString("settings.general.timer.defaultsound.desc", _config.DraftDefaultTimeUpSound);

            PinnablePlugins.Clear();
            foreach (var id in _pluginCatalog.PluginModules.Keys)
            {
                bool isPinned = await _config.IsPluginPinnedAsync(id);
                PinnablePlugins.Add(new PluginPinItemViewModel(id, _pluginCatalog.PluginInfos[id].Name, isPinned, _config));
            }

            OnPropertiesChanged(
                nameof(DraftLanguage), nameof(DraftBlurEffectEnabled), nameof(DraftScreenOnOffEffect),
                nameof(DraftScreenAutoWakeUp), nameof(DraftScreenAutoWakeUpHour), nameof(DraftScreenAutoWakeUpMinute),
                nameof(DraftMenuCloseDelaySeconds), nameof(MenuAutoCloseLabel), nameof(DraftClockShowSeconds),
                nameof(DraftClockFbxStyle), nameof(IsShowSecondsEditable), nameof(DraftAlarmTimeoutDelayMinutes),
                nameof(AlarmTimeoutLabel), nameof(DraftCarouselDelaySeconds), nameof(CarouselDelayLabel));
        }
    }
}
