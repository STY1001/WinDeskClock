using WinDeskClock.Mvvm;
using WinDeskClock.Services;

namespace WinDeskClock.ViewModels
{
    /// <summary>
    /// Composes the three Settings tabs. While a plugin's own settings sub-page is open
    /// (PluginManager.IsPluginSettingsOpen), the View disables Save/Back and the other tabs -
    /// exactly like the original, which reached into MainWindow's controls to do the same thing.
    /// </summary>
    public class SettingsViewModel : ObservableObject
    {
        private readonly IConfigService _config;

        public SettingsViewModel(
            IConfigService config,
            GeneralSettingsViewModel general,
            PluginManagerViewModel pluginManager,
            AboutViewModel about)
        {
            _config = config;
            General = general;
            PluginManager = pluginManager;
            About = about;

            PluginManager.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName == nameof(ViewModels.PluginManagerViewModel.IsPluginSettingsOpen))
                {
                    OnPropertyChanged(nameof(IsPluginSettingsOpen));
                }
            };
        }

        public GeneralSettingsViewModel General { get; }
        public PluginManagerViewModel PluginManager { get; }
        public AboutViewModel About { get; }

        public bool IsPluginSettingsOpen => PluginManager.IsPluginSettingsOpen;

        public async Task LoadAsync()
        {
            await General.LoadAsync();
            await PluginManager.LoadAsync();
        }

        /// <summary>Raised after Save if the app needs restarting to apply everything.</summary>
        public event EventHandler? RestartRequired;

        public async Task SaveAsync()
        {
            bool restartNeeded = await _config.SaveDraftAsync();
            if (restartNeeded)
            {
                RestartRequired?.Invoke(this, EventArgs.Empty);
            }
        }
    }
}
