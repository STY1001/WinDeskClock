using Newtonsoft.Json;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Runtime.Loader;
using System.Windows;
using System.Windows.Controls;
using WinDeskClock.Interfaces;
using WinDeskClock.Utils;

namespace WinDeskClock.Utils
{
    public static class PluginLoader
    {

        public static string PluginPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "plugins");
        public static string PluginDataPath = Path.Combine(PluginPath, "plugins_data");
        public static string PluginLogPath = Path.Combine(PluginPath, "plugins_log");

        public static Dictionary<string, IPluginInfo> PluginInfos = new Dictionary<string, IPluginInfo>();
        public static Dictionary<string, IPluginModule> PluginModules = new Dictionary<string, IPluginModule>();
        public static List<string> IncompatiblePlugin = new List<string>();

        public static async Task<bool> CheckDisabledPlugin(string id)
        {
            foreach (string plugin in ConfigManager.Variables.DisabledPlugin)
            {
                if (plugin == id) { return true; }
            }
            return false;
        }

        public static async Task<bool> CheckCompatiblePlugin(string id)
        {
            foreach (string plugin in IncompatiblePlugin)
            {
                if (plugin == id) { return false; }
            }
            return true;
        }

        public static async Task AddDisabledPlugin(string id)
        {
            foreach (string plugin in ConfigManager.NewVariables.DisabledPlugin)
            {
                if (plugin == id) { return; }
            }
            ConfigManager.NewVariables.DisabledPlugin.Add(id);
        }

        public static async Task DelDisabledPlugin(string id)
        {
            foreach (string plugin in ConfigManager.NewVariables.DisabledPlugin)
            {
                if (plugin == id)
                {
                    ConfigManager.NewVariables.DisabledPlugin.Remove(id);
                    return;
                }
            }
        }

        public static async Task<bool> UpdateValidate(string id)
        {
            Log.Info($"Validating update information for plugin {id}...");
            try
            {
                HttpClient client = new HttpClient();
                Log.Info($"Fetching update information from {PluginInfos[id].UpdateURL}...");
                string json = await client.GetStringAsync(PluginInfos[id].UpdateURL);
                dynamic data = JsonConvert.DeserializeObject(json);
                if (data["version"] != null && data["versioncode"] != null && data["downloadlink"] != null)
                {
                    Log.Info($"Update information for plugin {id} is valid.");
                    return true;
                }
                if (data["version"] == null)
                {
                    Log.Warning($"Update information for plugin {id} is invalid: 'version' is missing");
                }
                if (data["versioncode"] == null)
                {
                    Log.Warning($"Update information for plugin {id} is invalid: 'versioncode' is missing");
                }
                if (data["downloadlink"] == null)
                {
                    Log.Warning($"Update information for plugin {id} is invalid: 'downloadlink' is missing");
                }
                return false;
            }
            catch (Exception ex)
            {
                Log.Warning($"Failed to validate update information for plugin {id}. Exception: {ex.Message}");
                return false;
            }
        }

        public static async Task<bool> UpdateCheck(string id)
        {
            Log.Info($"Checking for updates for plugin {id}...");
            try
            {
                HttpClient client = new HttpClient();
                Log.Info($"Fetching update information from {PluginInfos[id].UpdateURL}...");
                string json = await client.GetStringAsync(PluginInfos[id].UpdateURL);
                dynamic data = JsonConvert.DeserializeObject(json);
                if (data["versioncode"] > PluginInfos[id].VersionCode)
                {
                    Log.Info($"Update available for plugin {id}. Current version: {PluginInfos[id].VersionCode}, New version: {data["versioncode"]}");
                    return true;
                }
            }
            catch (Exception ex)
            {
                Log.Warning($"Failed to check for updates for plugin {id}. Exception: {ex.Message}");
                return false;
            }
            Log.Info($"No updates available for plugin {id}");
            return false;
        }

        public static async Task<bool> UpdatePlugin(string id)
        {
            Log.Info($"Updating plugin {id}...");
            try
            {
                HttpClient client = new HttpClient();
                Log.Info($"Fetching update information from {PluginInfos[id].UpdateURL}...");
                string json = await client.GetStringAsync(PluginInfos[id].UpdateURL);
                dynamic data = JsonConvert.DeserializeObject(json);
                string downloadURL = data["downloadlink"];
                string downloadPath = Path.Combine(PluginPath, id + ".dll");
                if (File.Exists(downloadPath))
                {
                    Log.Info($"Deleting old plugin file {downloadPath}...");
                    File.Delete(downloadPath);
                }
                Log.Info($"Downloading new plugin file from {downloadURL} to {downloadPath}...");
                using (var downloadStream = await client.GetStreamAsync(downloadURL))
                {
                    using (var fileStream = new FileStream(downloadPath, FileMode.Create, FileAccess.Write, FileShare.None))
                    {
                        await downloadStream.CopyToAsync(fileStream);
                    }
                }
                Log.Info($"Plugin {id} updated successfully");
                return true;
            }
            catch (Exception ex)
            {
                Log.Error($"Failed to update plugin {id}. Exception: {ex.Message}");
                return false;
            }
        }

        public static async Task LoadPlugins()
        {
            AppDomain.CurrentDomain.SetData("PluginFolderPath", PluginPath);

            if (!Directory.Exists(PluginPath))
            {
                Log.Warning("Plugin folder does not exist, creating it...");
                Directory.CreateDirectory(PluginPath);
            }
            if (!Directory.Exists(PluginDataPath))
            {
                Log.Warning("Plugin data folder does not exist, creating it...");
                Directory.CreateDirectory(PluginDataPath);
            }
            if (!Directory.Exists(PluginLogPath))
            {
                Log.Warning("Plugin log folder does not exist, creating it...");
                Directory.CreateDirectory(PluginLogPath);
            }

            var dllFiles = Directory.GetFiles(PluginPath, "*.dll");
            foreach (var file in dllFiles)
            {
                Log.Info($"Loading plugin from {Path.GetFileName(file)}");
                if (!Path.GetFileName(file).StartsWith("WDC."))
                {
                    Log.Warning($"Skipping {Path.GetFileName(file)}, file name does not start with 'WDC.'");
                    continue;
                }

                Log.Info($"Loading plugin assembly from {Path.GetFileName(file)}");
                var context = new AssemblyLoadContext(Path.GetFileNameWithoutExtension(file), isCollectible: true);
                using var stream = new FileStream(file, FileMode.Open, FileAccess.Read);
                var assembly = context.LoadFromStream(stream);
                var productAttribute = assembly.GetCustomAttribute<AssemblyProductAttribute>();
                if (productAttribute?.Product != "WinDeskClock" || !assembly.GetName().Name.StartsWith("WDC."))
                {
                    if (productAttribute?.Product != "WinDeskClock")
                    {
                        Log.Warning($"Skipping {Path.GetFileName(file)}, Product attribute does not match 'WinDeskClock'");
                    }
                    if (!assembly.GetName().Name.StartsWith("WDC."))
                    {
                        Log.Warning($"Skipping {Path.GetFileName(file)}, Assembly name does not start with 'WDC.'");
                    }
                    context.Unload();
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                    continue;
                }

                Log.Info($"Assembly loaded successfully, getting types from {Path.GetFileName(file)}");
                Type[] types;
                try
                {
                    types = assembly.GetTypes();
                }
                catch (ReflectionTypeLoadException ex)
                {
                    Log.Warning($"Failed to load types from {Path.GetFileName(file)}, skipping... Exception: {ex.Message}");
                    types = ex.Types.Where(t => t != null).ToArray();
                }

                Log.Info($"Searching for plugin info and module in {Path.GetFileName(file)}");
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
                catch (Exception ex)
                {
                    Log.Warning($"Failed to create instance of plugin info for {Path.GetFileName(file)}, Exception: {ex.Message}");
                }

                if (pluginInfo == null || !pluginInfo.ID.StartsWith("WDC.") || Path.GetFileNameWithoutExtension(file) != pluginInfo.ID)
                {
                    if (pluginInfo == null)
                    {
                        Log.Warning($"Skipping {Path.GetFileName(file)}, plugin info is null");
                    }
                    if (!pluginInfo.ID.StartsWith("WDC."))
                    {
                        Log.Warning($"Skipping {Path.GetFileName(file)}, plugin ID does not start with 'WDC.'");
                    }
                    if (Path.GetFileNameWithoutExtension(file) != pluginInfo.ID)
                    {
                        Log.Warning($"Skipping {Path.GetFileName(file)}, plugin ID does not match file name");
                    }
                    context.Unload();
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                    continue;
                }

                Log.Info($"Plugin found: {pluginInfo.Name} ({pluginInfo.ID}) by {pluginInfo.Author}");
                PluginInfos.Add(pluginInfo.ID, pluginInfo);

                var pluginDataDir = Path.Combine(PluginDataPath, pluginInfo.ID);
                if (!Directory.Exists(pluginDataDir))
                {
                    Log.Warning($"Plugin data directory for {pluginInfo.ID} does not exist, creating it...");
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
                            if (!await CheckDisabledPlugin(pluginInfo.ID))
                            {
                                Log.Info($"Plugin {pluginInfo.ID} loaded successfully.");
                                PluginModules.Add(pluginInfo.ID, pluginModule);
                            }
                            else
                            {
                                Log.Warning($"Plugin {pluginInfo.ID} is disabled, skipping...");
                            }
                        }
                        else
                        {
                            Log.Warning($"Failed to create instance of plugin module for {pluginInfo.ID}, adding to incompatible plugins list.");
                            IncompatiblePlugin.Add(pluginInfo.ID);
                        }
                    }
                    else
                    {
                        Log.Warning($"No valid plugin module found for {pluginInfo.ID}, adding to incompatible plugins list.");
                        IncompatiblePlugin.Add(pluginInfo.ID);
                    }
                }
                catch
                {
                    Log.Warning($"Failed to create instance of plugin module for {pluginInfo.ID}, adding to incompatible plugins list.");
                    IncompatiblePlugin.Add(pluginInfo.ID);
                }

                context.Unload();
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }
        }
    }
}
