using System.Windows.Media.Imaging;
using WinDeskClock.Interfaces;
using WinDeskClock.Mvvm;
using WinDeskClock.Services;

namespace WinDeskClock.ViewModels
{
    /// <summary>One row in the Plugin Manager list.</summary>
    public class PluginCardViewModel : ObservableObject
    {
        private readonly IPluginCatalogService _pluginCatalog;
        private readonly IProcessLauncherService _processLauncher;
        private readonly IConfigService _config;

        public PluginCardViewModel(IPluginInfo info, IPluginCatalogService pluginCatalog, IProcessLauncherService processLauncher, IConfigService config)
        {
            Info = info;
            _pluginCatalog = pluginCatalog;
            _processLauncher = processLauncher;
            _config = config;
        }

        public IPluginInfo Info { get; }
        public string Id => Info.ID;
        public string Name => Info.Name;
        public string Author => Info.Author;
        public string Version => Info.Version;
        public string Description => Info.Description;
        public BitmapImage Icon => Info.Icon;

        public bool HasWebsite => Info.ProjectWebsiteURL != "none";
        public bool HasSource => Info.ProjectSourceURL != "none";
        public void OpenWebsite() => _processLauncher.OpenUrl(Info.ProjectWebsiteURL);
        public void OpenSource() => _processLauncher.OpenUrl(Info.ProjectSourceURL);

        private bool _isCompatible = true;
        public bool IsCompatible { get => _isCompatible; private set => SetProperty(ref _isCompatible, value); }

        private bool _isEnabled;
        public bool IsEnabled { get => _isEnabled; private set => SetProperty(ref _isEnabled, value); }

        public bool CanToggleEnabled => IsCompatible;
        public bool CanOpenSettings => IsCompatible && IsEnabled;

        private string _enabledLabelKey = "settings.pluginmanager.disabled";
        public string EnabledLabelKey { get => _enabledLabelKey; private set => SetProperty(ref _enabledLabelKey, value); }

        public string EnabledIconResource => IsEnabled ? "checkmark" : "cross";

        public async Task LoadAsync()
        {
            bool disabled = await _pluginCatalog.IsDisabledAsync(Info.ID);
            IsCompatible = await _pluginCatalog.IsCompatibleAsync(Info.ID);
            IsEnabled = !disabled && IsCompatible;
            EnabledLabelKey = IsEnabled ? "settings.pluginmanager.enabled" : "settings.pluginmanager.disabled";
            OnPropertiesChanged(nameof(CanToggleEnabled), nameof(CanOpenSettings), nameof(EnabledIconResource));

            HasUpdateUrl = Info.UpdateURL != "none";
            if (HasUpdateUrl)
            {
                IsUpdateButtonEnabled = await _pluginCatalog.ValidateUpdateAsync(Info.ID);
                IsUpdateAvailable = await _pluginCatalog.IsUpdateAvailableAsync(Info.ID);
            }
        }

        public async Task ToggleEnabledAsync()
        {
            IsEnabled = !IsEnabled;
            EnabledLabelKey = IsEnabled ? "settings.pluginmanager.enabled" : "settings.pluginmanager.disabled";
            OnPropertiesChanged(nameof(CanOpenSettings), nameof(EnabledIconResource));
            await _pluginCatalog.SetDisabledAsync(Info.ID, !IsEnabled);
            _config.MarkRestartNeeded();
        }

        // ----- Update button -----
        public bool HasUpdateUrl { get; private set; }

        private bool _isUpdateButtonEnabled;
        public bool IsUpdateButtonEnabled { get => _isUpdateButtonEnabled; private set => SetProperty(ref _isUpdateButtonEnabled, value); }

        private bool _isUpdateAvailable;
        /// <summary>True while a newer version is known to be available (drives the highlighted button style).</summary>
        public bool IsUpdateAvailable { get => _isUpdateAvailable; private set => SetProperty(ref _isUpdateAvailable, value); }

        private string _updateLabelKey = "settings.pluginmanager.update";
        public string UpdateLabelKey { get => _updateLabelKey; private set => SetProperty(ref _updateLabelKey, value); }

        public async Task UpdateAsync()
        {
            bool wasHighlighted = IsUpdateAvailable;
            IsUpdateAvailable = false;
            UpdateLabelKey = "settings.pluginmanager.updating";

            if (await _pluginCatalog.UpdateAsync(Info.ID))
            {
                UpdateLabelKey = "settings.pluginmanager.updated";
            }
            else
            {
                UpdateLabelKey = "settings.pluginmanager.update";
                IsUpdateAvailable = wasHighlighted;
            }
        }

        // ----- Delete -----
        public event EventHandler? DeleteRequested;
        public void RequestDelete()
        {
            _config.MarkRestartNeeded();
            DeleteRequested?.Invoke(this, EventArgs.Empty);
        }

        // ----- Settings sub-page -----
        public event EventHandler? SettingsRequested;
        public void RequestOpenSettings() => SettingsRequested?.Invoke(this, EventArgs.Empty);
    }
}
