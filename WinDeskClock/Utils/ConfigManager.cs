using Newtonsoft.Json.Linq;
using System.Diagnostics;
using System.IO;
using System.Reflection;

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
                { "DefaultTimeUpSound", "C:\\Windows\\Media\\Alarm08.wav" },
                { "DefaultAlarmSound", "C:\\Windows\\Media\\Alarm05.wav" },
                { "AlarmTimeoutDelay", "1" },
                { "CarouselDelay", "5" },
                { "ClockShowSecond", "true" },
                { "ClockFbxStyle", "false" },
                { "BlurEffect", "true" },
                { "PinnedPlugin", "" },
                { "PluginOrder", "" },
                { "DisabledPlugin", "" }
            };

            if (!File.Exists(ConfigPath))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(ConfigPath));
                await File.WriteAllTextAsync(ConfigPath, "{}");
                foreach (var defaultconfig in DefaultConfigList)
                {
                    await SetConfig(defaultconfig.Key, defaultconfig.Value);
                }
            }
            if (!File.Exists(AlarmPath))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(AlarmPath));
                await File.WriteAllTextAsync(AlarmPath, "{}");
            }

            foreach (var defaultconfig in DefaultConfigList)
            {
                if (await GetConfig(defaultconfig.Key) == null)
                {
                    await SetConfig(defaultconfig.Key, defaultconfig.Value);
                }
            }
        }

        public static async Task LoadSettings()
        {
            Variables.Language = await GetConfig("Language");
            Variables.DefaultTimeUpSound = await GetConfig("DefaultTimeUpSound");
            Variables.DefaultAlarmSound = await GetConfig("DefaultAlarmSound");
            Variables.AlarmTimeoutDelay = int.Parse(await GetConfig("AlarmTimeoutDelay"));
            Variables.CarouselDelay = int.Parse(await GetConfig("CarouselDelay"));
            Variables.ClockShowSecond = bool.Parse(await GetConfig("ClockShowSecond"));
            Variables.ClockFbxStyle = bool.Parse(await GetConfig("ClockFbxStyle"));
            Variables.BlurEffect = bool.Parse(await GetConfig("BlurEffect"));

            if (await GetConfig("PinnedPlugin") != "")
            {
                Variables.PinnedPlugin = (await GetConfig("PinnedPlugin")).Split(',').ToList();
            }
            else
            {
                Variables.PinnedPlugin = new List<string>();
            }

            if (await GetConfig("PluginOrder") != "")
            {
                Variables.PluginOrder = (await GetConfig("PluginOrder")).Split(',').ToList();
            }
            else
            {
                Variables.PluginOrder = new List<string>();
            }

            if (await GetConfig("DisabledPlugin") != "")
            {
                Variables.DisabledPlugin = (await GetConfig("DisabledPlugin")).Split(',').ToList();
            }
            else
            {
                Variables.DisabledPlugin = new List<string>();
            }

            NewVariables.Language = Variables.Language;
            NewVariables.DefaultTimeUpSound = Variables.DefaultTimeUpSound;
            NewVariables.DefaultAlarmSound = Variables.DefaultAlarmSound;
            NewVariables.ClockShowSecond = Variables.ClockShowSecond;
            NewVariables.AlarmTimeoutDelay = Variables.AlarmTimeoutDelay;
            NewVariables.CarouselDelay = Variables.CarouselDelay;
            NewVariables.ClockFbxStyle = Variables.ClockFbxStyle;
            NewVariables.BlurEffect = Variables.BlurEffect;
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
            await SetConfig("Language", NewVariables.Language);
            await SetConfig("DefaultTimeUpSound", NewVariables.DefaultTimeUpSound);
            await SetConfig("DefaultAlarmSound", NewVariables.DefaultAlarmSound);
            await SetConfig("ClockShowSecond", NewVariables.ClockShowSecond.ToString());
            await SetConfig("ClockFbxStyle", NewVariables.ClockFbxStyle.ToString());
            await SetConfig("AlarmTimeoutDelay", NewVariables.AlarmTimeoutDelay.ToString());
            await SetConfig("CarouselDelay", NewVariables.CarouselDelay.ToString());
            await SetConfig("BlurEffect", NewVariables.BlurEffect.ToString());
            await SetConfig("PinnedPlugin", string.Join(',', NewVariables.PinnedPlugin));
            await SetConfig("PluginOrder", string.Join(',', NewVariables.PluginOrder));
            await SetConfig("DisabledPlugin", string.Join(',', NewVariables.DisabledPlugin));

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
            public static int AlarmTimeoutDelay;
            public static int CarouselDelay;
            public static bool ClockShowSecond;
            public static bool ClockFbxStyle;
            public static bool BlurEffect;
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
            public static int AlarmTimeoutDelay;
            public static int CarouselDelay;
            public static bool ClockShowSecond;
            public static bool ClockFbxStyle;
            public static bool BlurEffect;
            public static List<string> PinnedPlugin;
            public static List<string> PluginOrder;
            public static List<string> DisabledPlugin;
        }
    }
}