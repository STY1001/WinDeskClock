using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.Loader;
using System.Windows;
using WinDeskClock.Interfaces;
using WinDeskClock.Utils;

namespace WinDeskClock.Utils
{
    public static class PluginLoader
    {

        public static string PluginPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "plugins");
        public static string PluginDataPath = Path.Combine(PluginPath, "plugins_data");

        public static Dictionary<string, IPluginInfo> PluginInfos = new Dictionary<string, IPluginInfo>();
        public static Dictionary<string, IPluginModule> PluginModules = new Dictionary<string, IPluginModule>();

        public static async Task<bool> CheckDisabledPlugin(string id)
        {
            foreach (string plugin in ConfigManager.Variable.DisabledPlugin)
            {
                if (plugin == id) { return true; }
            }
            return false;
        }

        public static async Task AddDisabledPlugin(string id)
        {
            foreach (string plugin in ConfigManager.NewVariable.DisabledPlugin)
            {
                if (plugin == id) { return; }
            }
            ConfigManager.NewVariable.DisabledPlugin.Add(id);
        }

        public static async Task DelDisabledPlugin(string id)
        {
            foreach (string plugin in ConfigManager.NewVariable.DisabledPlugin)
            {
                if (plugin == id)
                {
                    ConfigManager.NewVariable.DisabledPlugin.Remove(id);
                    return;
                }
            }
        }

        public static async Task LoadPlugins()
        {
            AppDomain.CurrentDomain.SetData("PluginFolderPath", PluginPath);

            if (!Directory.Exists(PluginPath))
            {
                Directory.CreateDirectory(PluginPath);
            }
            if (!Directory.Exists(PluginDataPath))
            {
                Directory.CreateDirectory(PluginDataPath);
            }

            var dllFiles = Directory.GetFiles(PluginPath, "*.dll");
            foreach (var file in dllFiles)
            {
                if (!Path.GetFileName(file).StartsWith("WDC."))
                {
                    continue;
                }

                var context = new AssemblyLoadContext(Path.GetFileNameWithoutExtension(file), isCollectible: true);
                using var stream = new FileStream(file, FileMode.Open, FileAccess.Read);
                var assembly = context.LoadFromStream(stream);

                var productAttribute = assembly.GetCustomAttribute<AssemblyProductAttribute>();
                if (productAttribute?.Product != "WinDeskClock" || !assembly.GetName().Name.StartsWith("WDC."))
                {
                    context.Unload();
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                    continue;
                }

                var pluginInfos = assembly.GetTypes()
                    .Where(t => typeof(IPluginInfo).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract);

                foreach (var infoType in pluginInfos)
                {
                    if (Activator.CreateInstance(infoType) is IPluginInfo pluginInfo)
                    {
                        if (!pluginInfo.ID.StartsWith("WDC.") || Path.GetFileNameWithoutExtension(file) != pluginInfo.ID)
                        {
                            continue;
                        }

                        PluginInfos.Add(pluginInfo.ID, pluginInfo);

                        var moduleType = assembly.GetTypes()
                            .FirstOrDefault(t => typeof(IPluginModule).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract);

                        if (moduleType == null || Activator.CreateInstance(moduleType) is not IPluginModule pluginModule || typeof(IPluginModule).AssemblyQualifiedName != moduleType.GetInterface(nameof(IPluginModule))?.AssemblyQualifiedName)
                        {
                            continue;
                        }

                        PluginModules.Add(pluginInfo.ID, pluginModule);

                        var pluginDataDir = Path.Combine(PluginDataPath, pluginInfo.ID);
                        if (!Directory.Exists(pluginDataDir))
                        {
                            Directory.CreateDirectory(pluginDataDir);
                        }
                    }
                }

                context.Unload();
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }
        }
    }
}
