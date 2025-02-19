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

        public static Dictionary<string, IPlugin> Plugins = new Dictionary<string, IPlugin>();

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
                if (productAttribute == null || productAttribute.Product != "WinDeskClock")
                {
                    context.Unload();
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                    continue;
                }

                if (!assembly.GetName().Name.StartsWith("WDC."))
                {
                    context.Unload();
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                    continue;
                }

                var types = assembly.GetTypes()
                    .Where(t => typeof(IPlugin).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract);

                foreach (var type in types)
                {
                    if (Activator.CreateInstance(type) is IPlugin plugin)
                    {
                        if (!plugin.ID.StartsWith("WDC."))
                        {
                            continue;
                        }

                        if (Path.GetFileNameWithoutExtension(file) != plugin.ID)
                        {
                            continue;
                        }

                        Plugins.Add(plugin.ID, plugin);

                        if (!Directory.Exists(Path.Combine(PluginDataPath, plugin.ID)))
                        {
                            Directory.CreateDirectory(Path.Combine(PluginDataPath, plugin.ID));
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
