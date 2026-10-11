using System.Collections.ObjectModel;
using System.IO;
using System.Windows.Controls;
using WinDeskClock.Services;
using WinDeskClock.Mvvm;

namespace WinDeskClock.ViewModels
{
    /// <summary>
    /// The plugin list in Settings, plus the "open a single plugin's settings" overlay. While that
    /// overlay is open, the rest of the Settings dialog (Save/Back, General/About tabs) is disabled -
    /// SettingsViewModel exposes <see cref="IsPluginSettingsOpen"/> for that.
    /// </summary>
    public class PluginManagerViewModel : ObservableObject
    {
        private readonly IPluginCatalogService _pluginCatalog;
        private readonly IProcessLauncherService _processLauncher;
        private readonly IConfigService _config;

        public PluginManagerViewModel(IPluginCatalogService pluginCatalog, IProcessLauncherService processLauncher, IConfigService config)
        {
            _pluginCatalog = pluginCatalog;
            _processLauncher = processLauncher;
            _config = config;
        }

        public ObservableCollection<PluginCardViewModel> Plugins { get; } = new();
        public bool HasPlugins => Plugins.Count > 0;

        private bool _isPluginSettingsOpen;
        public bool IsPluginSettingsOpen { get => _isPluginSettingsOpen; private set => SetProperty(ref _isPluginSettingsOpen, value); }

        private Page? _currentPluginSettingsPage;
        public Page? CurrentPluginSettingsPage { get => _currentPluginSettingsPage; private set => SetProperty(ref _currentPluginSettingsPage, value); }

        private string _currentPluginId = string.Empty;

        public async Task LoadAsync()
        {
            Plugins.Clear();
            foreach (var id in _pluginCatalog.PluginInfos.Keys)
            {
                var card = new PluginCardViewModel(_pluginCatalog.PluginInfos[id], _pluginCatalog, _processLauncher, _config);
                card.DeleteRequested += async (_, _) => await DeletePluginAsync(card);
                card.SettingsRequested += (_, _) => OpenPluginSettings(card);
                await card.LoadAsync();
                Plugins.Add(card);
            }
            OnPropertyChanged(nameof(HasPlugins));
        }

        private void OpenPluginSettings(PluginCardViewModel card)
        {
            _currentPluginId = card.Id;
            CurrentPluginSettingsPage = _pluginCatalog.PluginModules[card.Id].GetSettings();
            IsPluginSettingsOpen = true;
        }

        public async Task SavePluginSettingsAsync()
        {
            _config.MarkRestartNeeded();
            await _pluginCatalog.PluginModules[_currentPluginId].SaveConfig();
            ClosePluginSettings();
        }

        public void CancelPluginSettings() => ClosePluginSettings();

        private void ClosePluginSettings()
        {
            IsPluginSettingsOpen = false;
            CurrentPluginSettingsPage = null;
            _currentPluginId = string.Empty;
        }

        private async Task DeletePluginAsync(PluginCardViewModel card)
        {
            string dllPath = Path.Combine(_pluginCatalog.PluginFolderPath, $"{card.Id}.dll");
            if (File.Exists(dllPath))
            {
                File.Delete(dllPath);
            }
            Plugins.Remove(card);
            OnPropertyChanged(nameof(HasPlugins));
            await Task.CompletedTask;
        }
    }
}
