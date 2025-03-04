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

        public static async Task<bool> CheckCompatiblePlugin(string id)
        {
            foreach (string plugin in PluginModules.Keys)
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

                Type[] types;
                try
                {
                    types = assembly.GetTypes();
                }
                catch (ReflectionTypeLoadException ex)
                {
                    types = ex.Types.Where(t => t != null).ToArray();
                }

                var pluginInfoType = types
                    .FirstOrDefault(t => typeof(IPluginInfo).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract);

                IPluginInfo pluginInfo = null;
                try
                {
                    if (pluginInfoType != null)
                    {
                        pluginInfo = Activator.CreateInstance(pluginInfoType) as IPluginInfo;
                    }
                }
                catch 
                {

                }

                if (pluginInfo == null || !pluginInfo.ID.StartsWith("WDC.") || Path.GetFileNameWithoutExtension(file) != pluginInfo.ID)
                {
                    context.Unload();
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                    continue;
                }

                PluginInfos.Add(pluginInfo.ID, pluginInfo);

                var pluginDataDir = Path.Combine(PluginDataPath, pluginInfo.ID);
                if (!Directory.Exists(pluginDataDir))
                {
                    Directory.CreateDirectory(pluginDataDir);
                }

                try
                {
                    var moduleType = types
                        .FirstOrDefault(t => typeof(IPluginModule).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract);

                    if (moduleType != null)
                    {
                        if (Activator.CreateInstance(moduleType) is IPluginModule pluginModule)
                        {
                            PluginModules.Add(pluginInfo.ID, pluginModule);
                        }
                    }
                }
                catch
                {

                }

                context.Unload();
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }
        }
    }
}
