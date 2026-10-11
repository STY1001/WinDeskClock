using System.Windows.Controls;
using WinDeskClock.Services;
using WinDeskClock.Mvvm;

namespace WinDeskClock.ViewModels
{
    /// <summary>
    /// Tracks *which* plugin page should currently be shown in the small home-screen carousel
    /// (pinned plugins only, auto-advancing) and in the full-screen plugin menu (every loaded
    /// plugin, paged manually). Actually retrieving the plugin's page (a WPF Page, per the stable
    /// IPluginModule contract) and navigating a Frame to it, plus all transition animations,
    /// stays in the View - this ViewModel only tracks indices and exposes the plugin id to show.
    /// </summary>
    public class PluginCarouselViewModel : ObservableObject
    {
        private readonly IPluginCatalogService _pluginCatalog;
        private readonly IConfigService _config;

        public PluginCarouselViewModel(IPluginCatalogService pluginCatalog, IConfigService config)
        {
            _pluginCatalog = pluginCatalog;
            _config = config;
        }

        public List<string> AllPluginIds { get; private set; } = new();
        public List<string> PinnedPluginIds { get; private set; } = new();

        public bool HasAnyPlugin => AllPluginIds.Count > 0;
        public bool HasPinnedPlugin => PinnedPluginIds.Count > 0;

        private int _carouselIndex;
        private int _menuIndex;

        public string? CurrentCarouselPluginId => HasPinnedPlugin ? PinnedPluginIds[_carouselIndex] : null;
        public string? CurrentMenuPluginId => HasAnyPlugin ? AllPluginIds[_menuIndex] : null;

        public bool CanGoLeftInMenu => _menuIndex > 0;
        public bool CanGoRightInMenu => _menuIndex < AllPluginIds.Count - 1;

        public void Load()
        {
            AllPluginIds = _pluginCatalog.PluginModules.Keys.ToList();
            PinnedPluginIds = _config.CurrentPinnedPluginIds.Where(AllPluginIds.Contains).ToList();
            _carouselIndex = 0;
            _menuIndex = 0;
            OnPropertiesChanged(nameof(HasAnyPlugin), nameof(HasPinnedPlugin), nameof(CurrentCarouselPluginId),
                nameof(CurrentMenuPluginId), nameof(CanGoLeftInMenu), nameof(CanGoRightInMenu));
        }

        public Page? GetMainPage(string id) => _pluginCatalog.PluginModules.TryGetValue(id, out var module) ? module.GetMain() : null;

        /// <summary>Advances to the next pinned plugin (wrapping around). Returns the new id to display.</summary>
        public string? AdvanceCarousel()
        {
            if (PinnedPluginIds.Count <= 1)
            {
                return CurrentCarouselPluginId;
            }

            _carouselIndex = _carouselIndex == PinnedPluginIds.Count - 1 ? 0 : _carouselIndex + 1;
            OnPropertyChanged(nameof(CurrentCarouselPluginId));
            return CurrentCarouselPluginId;
        }

        /// <summary>Resets the full menu to whichever plugin is currently shown in the carousel.</summary>
        public void OpenMenuAtCurrentCarouselPlugin()
        {
            _menuIndex = HasPinnedPlugin ? AllPluginIds.IndexOf(PinnedPluginIds[_carouselIndex]) : 0;
            if (_menuIndex < 0) _menuIndex = 0;
            OnPropertiesChanged(nameof(CurrentMenuPluginId), nameof(CanGoLeftInMenu), nameof(CanGoRightInMenu));
        }

        public string? MoveMenuLeft()
        {
            _menuIndex--;
            OnPropertiesChanged(nameof(CurrentMenuPluginId), nameof(CanGoLeftInMenu), nameof(CanGoRightInMenu));
            return CurrentMenuPluginId;
        }

        public string? MoveMenuRight()
        {
            _menuIndex++;
            OnPropertiesChanged(nameof(CurrentMenuPluginId), nameof(CanGoLeftInMenu), nameof(CanGoRightInMenu));
            return CurrentMenuPluginId;
        }
    }
}
