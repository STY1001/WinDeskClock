using WinDeskClock.Interfaces;
using WinDeskClock.Utils;

namespace WinDeskClock.Services
{
    /// <summary>
    /// Injectable facade over WinDeskClock.Utils.PluginLoader. PluginLoader itself stays a static
    /// class: the loaded plugin assemblies are genuinely process-global, one-time-at-startup state,
    /// so making it an instance service wouldn't add real value. This wrapper exists purely so
    /// ViewModels depend on an interface rather than the static class directly.
    /// </summary>
    public interface IPluginCatalogService
    {
        Task LoadPluginsAsync();

        string PluginFolderPath { get; }
        IReadOnlyDictionary<string, IPluginInfo> PluginInfos { get; }
        IReadOnlyDictionary<string, IPluginModule> PluginModules { get; }

        Task<bool> IsDisabledAsync(string id);
        Task<bool> IsCompatibleAsync(string id);
        Task SetDisabledAsync(string id, bool disabled);

        Task<bool> ValidateUpdateAsync(string id);
        Task<bool> IsUpdateAvailableAsync(string id);
        Task<bool> UpdateAsync(string id);
    }

    public class PluginCatalogService : IPluginCatalogService
    {
        public Task LoadPluginsAsync() => PluginLoader.LoadPlugins();

        public string PluginFolderPath => PluginLoader.PluginPath;
        public IReadOnlyDictionary<string, IPluginInfo> PluginInfos => PluginLoader.PluginInfos;
        public IReadOnlyDictionary<string, IPluginModule> PluginModules => PluginLoader.PluginModules;

        public Task<bool> IsDisabledAsync(string id) => PluginLoader.CheckDisabledPlugin(id);
        public Task<bool> IsCompatibleAsync(string id) => PluginLoader.CheckCompatiblePlugin(id);

        public async Task SetDisabledAsync(string id, bool disabled)
        {
            if (disabled)
            {
                await PluginLoader.AddDisabledPlugin(id);
            }
            else
            {
                await PluginLoader.DelDisabledPlugin(id);
            }
        }

        public Task<bool> ValidateUpdateAsync(string id) => PluginLoader.UpdateValidate(id);
        public Task<bool> IsUpdateAvailableAsync(string id) => PluginLoader.UpdateCheck(id);
        public Task<bool> UpdateAsync(string id) => PluginLoader.UpdatePlugin(id);
    }
}
