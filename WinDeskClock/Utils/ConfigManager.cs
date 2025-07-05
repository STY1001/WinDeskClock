using Newtonsoft.Json.Linq;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Windows.Input;

namespace WinDeskClock.Utils
{
    public static class ConfigManager
    {
        public static readonly string ConfigPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "data", "config.json");
        public static readonly string AlarmPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "data", "alarms.json");

        public static async Task CheckAndCreateConfigs()
        {
            Dictionary<string, string> DefaultConfigList = new Dictionary<string, string>
            {
                { "Language", "en-us" },
                { "ScreenOnOff", "crt" },
                { "DefaultTimeUpSound", "C:\\Windows\\Media\\Alarm08.wav" },
                { "DefaultAlarmSound", "C:\\Windows\\Media\\Alarm05.wav" },
                { "AlarmTimeoutDelay", "1" },
                { "CarouselDelay", "5" },
                { "MenuCloseDelay", "10" },
                { "ClockShowSecond", "true" },
                { "ClockFbxStyle", "false" },
                { "BlurEffect", "true" },
                { "ScreenAutoWakeUp", "false" },
                { "ScreenAutoWakeUpTime", "10:56" },
                { "PinnedPlugin", "" },
                { "PluginOrder", "" },
                { "DisabledPlugin", "" }
            };

            if (!File.Exists(ConfigPath))
            {
                Log.Warning("Config file not found, creating default config file...");
                Directory.CreateDirectory(Path.GetDirectoryName(ConfigPath));
                await File.WriteAllTextAsync(ConfigPath, "{}");
                foreach (var defaultconfig in DefaultConfigList)
                {
                    await SetConfig(defaultconfig.Key, defaultconfig.Value);
                }
            }
            if (!File.Exists(AlarmPath))
            {
                Log.Warning("Alarm file not found, creating default alarm file...");
                Directory.CreateDirectory(Path.GetDirectoryName(AlarmPath));
                await File.WriteAllTextAsync(AlarmPath, "{}");
            }

            foreach (var defaultconfig in DefaultConfigList)
            {
                if (await GetConfig(defaultconfig.Key) == null)
                {
                    Log.Warning($"Config key '{defaultconfig.Key}' not found, setting default value '{defaultconfig.Value}'...");
                    await SetConfig(defaultconfig.Key, defaultconfig.Value);
                }
            }
        }

        public static async Task LoadSettings()
        {
            Variables.Language = await GetConfig("Language");
            Log.Info($"Language: {Variables.Language}");
            Variables.ScreenOnOff = await GetConfig("ScreenOnOff");
            Log.Info($"ScreenOnOff: {Variables.ScreenOnOff}");
            Variables.DefaultTimeUpSound = await GetConfig("DefaultTimeUpSound");
            Log.Info($"DefaultTimeUpSound: {Variables.DefaultTimeUpSound}");
            Variables.DefaultAlarmSound = await GetConfig("DefaultAlarmSound");
            Log.Info($"DefaultAlarmSound: {Variables.DefaultAlarmSound}");
            Variables.AlarmTimeoutDelay = int.Parse(await GetConfig("AlarmTimeoutDelay"));
            Log.Info($"AlarmTimeoutDelay: {Variables.AlarmTimeoutDelay}");
            Variables.CarouselDelay = int.Parse(await GetConfig("CarouselDelay"));
            Log.Info($"CarouselDelay: {Variables.CarouselDelay}");
            Variables.MenuCloseDelay = int.Parse(await GetConfig("MenuCloseDelay"));
            Log.Info($"MenuCloseDelay: {Variables.MenuCloseDelay}");
            Variables.ClockShowSecond = bool.Parse(await GetConfig("ClockShowSecond"));
            Log.Info($"ClockShowSecond: {Variables.ClockShowSecond}");
            Variables.ClockFbxStyle = bool.Parse(await GetConfig("ClockFbxStyle"));
            Log.Info($"ClockFbxStyle: {Variables.ClockFbxStyle}");
            Variables.BlurEffect = bool.Parse(await GetConfig("BlurEffect"));
            Log.Info($"BlurEffect: {Variables.BlurEffect}");
            Variables.ScreenAutoWakeUp = bool.Parse(await GetConfig("ScreenAutoWakeUp"));
            Log.Info($"ScreenAutoWakeUp: {Variables.ScreenAutoWakeUp}");
            Variables.ScreenAutoWakeUpTime = TimeOnly.Parse(await GetConfig("ScreenAutoWakeUpTime"));
            Log.Info($"ScreenAutoWakeUpTime: {Variables.ScreenAutoWakeUpTime}");
            if (await GetConfig("PinnedPlugin") != "")
            {
                Variables.PinnedPlugin = (await GetConfig("PinnedPlugin")).Split(',').ToList();
            }
            else
            {
                Variables.PinnedPlugin = new List<string>();
            }
            Log.Info($"PinnedPlugin: {string.Join(", ", Variables.PinnedPlugin)}");
            if (await GetConfig("PluginOrder") != "")
            {
                Variables.PluginOrder = (await GetConfig("PluginOrder")).Split(',').ToList();
            }
            else
            {
                Variables.PluginOrder = new List<string>();
            }
            Log.Info($"PluginOrder: {string.Join(", ", Variables.PluginOrder)}");
            if (await GetConfig("DisabledPlugin") != "")
            {
                Variables.DisabledPlugin = (await GetConfig("DisabledPlugin")).Split(',').ToList();
            }
            else
            {
                Variables.DisabledPlugin = new List<string>();
            }
            Log.Info($"DisabledPlugin: {string.Join(", ", Variables.DisabledPlugin)}");

            Log.Info("Settings loaded successfully.");

            // Copy current settings to new variables
            Log.Info("Copying current settings to new variables...");
            NewVariables.Language = Variables.Language;
            NewVariables.ScreenOnOff = Variables.ScreenOnOff;
            NewVariables.DefaultTimeUpSound = Variables.DefaultTimeUpSound;
            NewVariables.DefaultAlarmSound = Variables.DefaultAlarmSound;
            NewVariables.ClockShowSecond = Variables.ClockShowSecond;
            NewVariables.AlarmTimeoutDelay = Variables.AlarmTimeoutDelay;
            NewVariables.CarouselDelay = Variables.CarouselDelay;
            NewVariables.MenuCloseDelay = Variables.MenuCloseDelay;
            NewVariables.ClockFbxStyle = Variables.ClockFbxStyle;
            NewVariables.BlurEffect = Variables.BlurEffect;
            NewVariables.ScreenAutoWakeUp = Variables.ScreenAutoWakeUp;
            NewVariables.ScreenAutoWakeUpTime = new TimeOnly(Variables.ScreenAutoWakeUpTime.Hour, Variables.ScreenAutoWakeUpTime.Minute);
            NewVariables.PinnedPlugin = new List<string>();
            foreach (string plugin in Variables.PinnedPlugin)
            {
                NewVariables.PinnedPlugin.Add(plugin);
            }
            NewVariables.PluginOrder = new List<string>();
            foreach (string plugin in Variables.PluginOrder)
            {
                NewVariables.PluginOrder.Add(plugin);
            }
            NewVariables.DisabledPlugin = new List<string>();
            foreach (string plugin in Variables.DisabledPlugin)
            {
                NewVariables.DisabledPlugin.Add(plugin);
            }
        }

        public static async Task SaveNewSettings()
        {
            Log.Info("Saving new settings...");
            Log.Info($"Language: {NewVariables.Language}");
            await SetConfig("Language", NewVariables.Language);
            Log.Info($"ScreenOnOff: {NewVariables.ScreenOnOff}");
            await SetConfig("ScreenOnOff", NewVariables.ScreenOnOff);
            Log.Info($"DefaultTimeUpSound: {NewVariables.DefaultTimeUpSound}");
            await SetConfig("DefaultTimeUpSound", NewVariables.DefaultTimeUpSound);
            Log.Info($"DefaultAlarmSound: {NewVariables.DefaultAlarmSound}");
            await SetConfig("DefaultAlarmSound", NewVariables.DefaultAlarmSound);
            Log.Info($"ClockShowSecond: {NewVariables.ClockShowSecond}");
            await SetConfig("ClockShowSecond", NewVariables.ClockShowSecond.ToString());
            Log.Info($"ClockFbxStyle: {NewVariables.ClockFbxStyle}");
            await SetConfig("ClockFbxStyle", NewVariables.ClockFbxStyle.ToString());
            Log.Info($"AlarmTimeoutDelay: {NewVariables.AlarmTimeoutDelay}");
            await SetConfig("AlarmTimeoutDelay", NewVariables.AlarmTimeoutDelay.ToString());
            Log.Info($"CarouselDelay: {NewVariables.CarouselDelay}");
            await SetConfig("CarouselDelay", NewVariables.CarouselDelay.ToString());
            Log.Info($"MenuCloseDelay: {NewVariables.MenuCloseDelay}");
            await SetConfig("MenuCloseDelay", NewVariables.MenuCloseDelay.ToString());
            Log.Info($"BlurEffect: {NewVariables.BlurEffect}");
            await SetConfig("BlurEffect", NewVariables.BlurEffect.ToString());
            Log.Info($"ScreenAutoWakeUp: {NewVariables.ScreenAutoWakeUp}");
            await SetConfig("ScreenAutoWakeUp", NewVariables.ScreenAutoWakeUp.ToString());
            Log.Info($"ScreenAutoWakeUpTime: {NewVariables.ScreenAutoWakeUpTime}");
            await SetConfig("ScreenAutoWakeUpTime", NewVariables.ScreenAutoWakeUpTime.ToString("HH:mm"));
            Log.Info($"PinnedPlugin: {string.Join(", ", NewVariables.PinnedPlugin)}");
            await SetConfig("PinnedPlugin", string.Join(',', NewVariables.PinnedPlugin));
            Log.Info($"PluginOrder: {string.Join(", ", NewVariables.PluginOrder)}");
            await SetConfig("PluginOrder", string.Join(',', NewVariables.PluginOrder));
            Log.Info($"DisabledPlugin: {string.Join(", ", NewVariables.DisabledPlugin)}");
            await SetConfig("DisabledPlugin", string.Join(',', NewVariables.DisabledPlugin));
            Log.Info("New settings saved successfully. Reloading settings...");
            await LoadSettings();
        }

        public static async Task<string> GetConfig(string key)
        {
            string json = await File.ReadAllTextAsync(ConfigPath);
            JObject data = JObject.Parse(json);
            return GetNestedValue(data, key.Split('.'));
        }

        public static async Task SetConfig(string key, string value)
        {
            string json = await File.ReadAllTextAsync(ConfigPath);
            JObject data = JObject.Parse(json);

            SetNestedValue(data, key.Split('.'), value);

            string newJson = data.ToString();
            await File.WriteAllTextAsync(ConfigPath, newJson);
        }

        public static async Task<string> GetAlarm(string key)
        {
            string json = await File.ReadAllTextAsync(AlarmPath);
            JObject data = JObject.Parse(json);

            return GetNestedValue(data, key.Split('.'));
        }

        public static async Task SetAlarm(string key, string value)
        {
            string json = await File.ReadAllTextAsync(AlarmPath);
            JObject data = JObject.Parse(json);

            SetNestedValue(data, key.Split('.'), value);

            string newJson = data.ToString();
            await File.WriteAllTextAsync(AlarmPath, newJson);
        }

        public static async Task DelAlarm(string key)
        {
            string json = await File.ReadAllTextAsync(AlarmPath);
            JObject data = JObject.Parse(json);

            DeleteNestedValue(data, key.Split('.'));

            string newJson = data.ToString();
            await File.WriteAllTextAsync(AlarmPath, newJson);
        }

        private static void SetNestedValue(JObject obj, string[] keys, string value)
        {
            JObject current = obj;
            for (int i = 0; i < keys.Length - 1; i++)
            {
                if (current[keys[i]] == null || current[keys[i]].Type != JTokenType.Object)
                {
                    current[keys[i]] = new JObject();
                }
                current = (JObject)current[keys[i]];
            }
            current[keys[^1]] = value;
        }

        private static string GetNestedValue(JObject obj, string[] keys)
        {
            JObject current = obj;
            for (int i = 0; i < keys.Length - 1; i++)
            {
                if (current[keys[i]] == null)
                {
                    throw new KeyNotFoundException($"Key '{keys[i]}' not found.");
                }
                current = (JObject)current[keys[i]];
            }
            return current[keys[^1]]?.ToString();
        }

        private static void DeleteNestedValue(JObject obj, string[] keys)
        {
            JObject current = obj;
            for (int i = 0; i < keys.Length - 1; i++)
            {
                if (current[keys[i]] == null)
                {
                    throw new KeyNotFoundException($"Key '{keys[i]}' not found.");
                }
                current = (JObject)current[keys[i]];
            }
            current.Remove(keys[^1]);
        }

        public static async Task<bool> CheckPinnedPlugin(string id)
        {
            foreach (string plugin in Variables.PinnedPlugin)
            {
                if (plugin == id) { return true; }
            }
            return false;
        }

        public static async Task AddPinnedPlugin(string id)
        {
            foreach (string plugin in NewVariables.PinnedPlugin)
            {
                if (plugin == id) { return; }
            }
            NewVariables.PinnedPlugin.Add(id);
        }

        public static async Task DelPinnedPlugin(string id)
        {
            foreach (string plugin in NewVariables.PinnedPlugin)
            {
                if (plugin == id)
                {
                    NewVariables.PinnedPlugin.Remove(id);
                    return;
                }
            }
        }

        public static class Variables
        {
            public static string DefaultTimeUpSound;
            public static string DefaultAlarmSound;
            public static string Language;
            public static string ScreenOnOff;
            public static int AlarmTimeoutDelay;
            public static int CarouselDelay;
            public static int MenuCloseDelay;
            public static bool ClockShowSecond;
            public static bool ClockFbxStyle;
            public static bool BlurEffect;
            public static bool ScreenAutoWakeUp;
            public static TimeOnly ScreenAutoWakeUpTime;
            public static List<string> PinnedPlugin;
            public static List<string> PluginOrder;
            public static List<string> DisabledPlugin;
        }

        public static class NewVariables
        {
            public static bool RestartNeeded = false;
            public static string DefaultTimeUpSound;
            public static string DefaultAlarmSound;
            public static string Language;
            public static string ScreenOnOff;
            public static int AlarmTimeoutDelay;
            public static int CarouselDelay;
            public static int MenuCloseDelay;
            public static bool ClockShowSecond;
            public static bool ClockFbxStyle;
            public static bool BlurEffect;
            public static bool ScreenAutoWakeUp;
            public static TimeOnly ScreenAutoWakeUpTime;
            public static List<string> PinnedPlugin;
            public static List<string> PluginOrder;
            public static List<string> DisabledPlugin;
        }
    }
}