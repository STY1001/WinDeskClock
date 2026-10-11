using WinDeskClock.Utils;

namespace WinDeskClock.Services
{
    /// <summary>
    /// Injectable facade over the app's persisted settings. ViewModels depend on this interface
    /// instead of the static WinDeskClock.Utils.ConfigManager class directly, which keeps them
    /// testable and framework-agnostic.
    ///
    /// Internally this still delegates to ConfigManager (unchanged) so the on-disk config.json
    /// format, and the "edit a draft, then Save/RestartIfNeeded" workflow the Settings page relies
    /// on, stay exactly the same as before.
    /// </summary>
    public interface IConfigService
    {
        Task InitializeAsync();

        // Effective (saved) settings, as currently applied to the running app.
        string CurrentLanguage { get; }
        string CurrentScreenOnOffEffect { get; }
        string CurrentDefaultTimeUpSound { get; }
        string CurrentDefaultAlarmSound { get; }
        int CurrentAlarmTimeoutDelayMinutes { get; }
        int CurrentCarouselDelaySeconds { get; }
        int CurrentMenuCloseDelaySeconds { get; }
        bool CurrentClockShowSeconds { get; }
        bool CurrentClockFbxStyle { get; }
        bool CurrentBlurEffectEnabled { get; }
        bool CurrentScreenAutoWakeUp { get; }
        TimeOnly CurrentScreenAutoWakeUpTime { get; }
        IReadOnlyList<string> CurrentPinnedPluginIds { get; }
        IReadOnlyList<string> CurrentDisabledPluginIds { get; }

        // Draft settings, edited by the Settings page and only applied on SaveAsync().
        string DraftLanguage { get; set; }
        string DraftScreenOnOffEffect { get; set; }
        string DraftDefaultTimeUpSound { get; set; }
        string DraftDefaultAlarmSound { get; set; }
        int DraftAlarmTimeoutDelayMinutes { get; set; }
        int DraftCarouselDelaySeconds { get; set; }
        int DraftMenuCloseDelaySeconds { get; set; }
        bool DraftClockShowSeconds { get; set; }
        bool DraftClockFbxStyle { get; set; }
        bool DraftBlurEffectEnabled { get; set; }
        bool DraftScreenAutoWakeUp { get; set; }
        TimeOnly DraftScreenAutoWakeUpTime { get; set; }

        /// <summary>Marks that applying the draft requires an app restart (e.g. language, blur effect...).</summary>
        void MarkRestartNeeded();

        Task<bool> IsPluginPinnedAsync(string pluginId);
        Task AddPinnedPluginAsync(string pluginId);
        Task RemovePinnedPluginAsync(string pluginId);

        /// <summary>Persists the draft to disk, reloads the effective settings, and returns whether a restart is needed.</summary>
        Task<bool> SaveDraftAsync();
    }

    public class ConfigService : IConfigService
    {
        public Task InitializeAsync() => ConfigManager.CheckAndCreateConfigs().ContinueWith(_ => ConfigManager.LoadSettings()).Unwrap();

        public string CurrentLanguage => ConfigManager.Variables.Language;
        public string CurrentScreenOnOffEffect => ConfigManager.Variables.ScreenOnOff;
        public string CurrentDefaultTimeUpSound => ConfigManager.Variables.DefaultTimeUpSound;
        public string CurrentDefaultAlarmSound => ConfigManager.Variables.DefaultAlarmSound;
        public int CurrentAlarmTimeoutDelayMinutes => ConfigManager.Variables.AlarmTimeoutDelay;
        public int CurrentCarouselDelaySeconds => ConfigManager.Variables.CarouselDelay;
        public int CurrentMenuCloseDelaySeconds => ConfigManager.Variables.MenuCloseDelay;
        public bool CurrentClockShowSeconds => ConfigManager.Variables.ClockShowSecond;
        public bool CurrentClockFbxStyle => ConfigManager.Variables.ClockFbxStyle;
        public bool CurrentBlurEffectEnabled => ConfigManager.Variables.BlurEffect;
        public bool CurrentScreenAutoWakeUp => ConfigManager.Variables.ScreenAutoWakeUp;
        public TimeOnly CurrentScreenAutoWakeUpTime => ConfigManager.Variables.ScreenAutoWakeUpTime;
        public IReadOnlyList<string> CurrentPinnedPluginIds => ConfigManager.Variables.PinnedPlugin;
        public IReadOnlyList<string> CurrentDisabledPluginIds => ConfigManager.Variables.DisabledPlugin;

        public string DraftLanguage
        {
            get => ConfigManager.NewVariables.Language;
            set => ConfigManager.NewVariables.Language = value;
        }
        public string DraftScreenOnOffEffect
        {
            get => ConfigManager.NewVariables.ScreenOnOff;
            set => ConfigManager.NewVariables.ScreenOnOff = value;
        }
        public string DraftDefaultTimeUpSound
        {
            get => ConfigManager.NewVariables.DefaultTimeUpSound;
            set => ConfigManager.NewVariables.DefaultTimeUpSound = value;
        }
        public string DraftDefaultAlarmSound
        {
            get => ConfigManager.NewVariables.DefaultAlarmSound;
            set => ConfigManager.NewVariables.DefaultAlarmSound = value;
        }
        public int DraftAlarmTimeoutDelayMinutes
        {
            get => ConfigManager.NewVariables.AlarmTimeoutDelay;
            set => ConfigManager.NewVariables.AlarmTimeoutDelay = value;
        }
        public int DraftCarouselDelaySeconds
        {
            get => ConfigManager.NewVariables.CarouselDelay;
            set => ConfigManager.NewVariables.CarouselDelay = value;
        }
        public int DraftMenuCloseDelaySeconds
        {
            get => ConfigManager.NewVariables.MenuCloseDelay;
            set => ConfigManager.NewVariables.MenuCloseDelay = value;
        }
        public bool DraftClockShowSeconds
        {
            get => ConfigManager.NewVariables.ClockShowSecond;
            set => ConfigManager.NewVariables.ClockShowSecond = value;
        }
        public bool DraftClockFbxStyle
        {
            get => ConfigManager.NewVariables.ClockFbxStyle;
            set => ConfigManager.NewVariables.ClockFbxStyle = value;
        }
        public bool DraftBlurEffectEnabled
        {
            get => ConfigManager.NewVariables.BlurEffect;
            set => ConfigManager.NewVariables.BlurEffect = value;
        }
        public bool DraftScreenAutoWakeUp
        {
            get => ConfigManager.NewVariables.ScreenAutoWakeUp;
            set => ConfigManager.NewVariables.ScreenAutoWakeUp = value;
        }
        public TimeOnly DraftScreenAutoWakeUpTime
        {
            get => ConfigManager.NewVariables.ScreenAutoWakeUpTime;
            set => ConfigManager.NewVariables.ScreenAutoWakeUpTime = value;
        }

        public void MarkRestartNeeded() => ConfigManager.NewVariables.RestartNeeded = true;

        public async Task<bool> IsPluginPinnedAsync(string pluginId) => await ConfigManager.CheckPinnedPlugin(pluginId);
        public async Task AddPinnedPluginAsync(string pluginId) => await ConfigManager.AddPinnedPlugin(pluginId);
        public async Task RemovePinnedPluginAsync(string pluginId) => await ConfigManager.DelPinnedPlugin(pluginId);

        public async Task<bool> SaveDraftAsync()
        {
            await ConfigManager.SaveNewSettings();
            bool restartNeeded = ConfigManager.NewVariables.RestartNeeded;
            ConfigManager.NewVariables.RestartNeeded = false;
            return restartNeeded;
        }
    }
}
