using Newtonsoft.Json.Linq;
using System.IO;
using System.Reflection;

namespace WinDeskClock.Utils
{
    public static class ConfigManager
    {
        public static string ConfigPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "data", "config.json");
        public static string AlarmPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "data", "alarms.json");

        public static async Task CheckAndCreateConfigs()
        {
            Dictionary<string, string> DefaultConfigList = new Dictionary<string, string>
        {
            { "Language", "en-us" },
            { "DefaultTimeUpSound", "C:\\Windows\\Media\\Alarm08.wav" },
            { "DefaultAlarmSound", "C:\\Windows\\Media\\Alarm05.wav" },
            { "AlarmTimeoutDelay", "1" },
            { "ClockShowSecond", "true" },
            { "ClockFbxStyle", "false" },
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
            Variable.Language = await GetConfig("Language");
            Variable.DefaultTimeUpSound = await GetConfig("DefaultTimeUpSound");
            Variable.DefaultAlarmSound = await GetConfig("DefaultAlarmSound");
            Variable.AlarmTimeoutDelay = await GetConfig("AlarmTimeoutDelay");
            Variable.ClockShowSecond = bool.Parse(await GetConfig("ClockShowSecond"));
            Variable.ClockFbxStyle = bool.Parse(await GetConfig("ClockFbxStyle"));

            if (await GetConfig("PinnedPlugin") != "")
            {
                Variable.PinnedPlugin = (await GetConfig("PinnedPlugin")).Split(',').ToList();
            }
            else
            {
                Variable.PinnedPlugin = new List<string>();
            }

            if (await GetConfig("PluginOrder") != "")
            {
                Variable.PluginOrder = (await GetConfig("PluginOrder")).Split(',').ToList();
            }
            else
            {
                Variable.PluginOrder = new List<string>();
            }

            if (await GetConfig("DisabledPlugin") != "")
            {
                Variable.DisabledPlugin = (await GetConfig("DisabledPlugin")).Split(',').ToList();
            }
            else
            {
                Variable.DisabledPlugin = new List<string>();
            }

            NewVariable.Language = Variable.Language;
            NewVariable.DefaultTimeUpSound = Variable.DefaultTimeUpSound;
            NewVariable.DefaultAlarmSound = Variable.DefaultAlarmSound;
            NewVariable.AlarmTimeoutDelay = Variable.AlarmTimeoutDelay;
            NewVariable.ClockShowSecond = Variable.ClockShowSecond;
            NewVariable.PinnedPlugin = Variable.PinnedPlugin;
            NewVariable.PluginOrder = Variable.PluginOrder;
            NewVariable.DisabledPlugin = Variable.DisabledPlugin;
        }

        public static async Task SaveNewSettings()
        {
            await SetConfig("Language", NewVariable.Language);
            await SetConfig("DefaultTimeUpSound", NewVariable.DefaultTimeUpSound);
            await SetConfig("DefaultAlarmSound", NewVariable.DefaultAlarmSound);
            await SetConfig("AlarmTimeoutDelay", NewVariable.AlarmTimeoutDelay);
            await SetConfig("ClockShowSecond", NewVariable.ClockShowSecond.ToString());
            await SetConfig("ClockFbxStyle", NewVariable.ClockFbxStyle.ToString());
            await SetConfig("PinnedPlugin", string.Join(',', NewVariable.PinnedPlugin));
            await SetConfig("PluginOrder", string.Join(',', NewVariable.PluginOrder));
            await SetConfig("DisabledPlugin", string.Join(',', NewVariable.DisabledPlugin));

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

        public static class Variable
        {
            public static string DefaultTimeUpSound;
            public static string DefaultAlarmSound;
            public static string AlarmTimeoutDelay;
            public static string Language;
            public static bool ClockShowSecond;
            public static bool ClockFbxStyle;
            public static List<string> PinnedPlugin;
            public static List<string> PluginOrder;
            public static List<string> DisabledPlugin;
        }

        public static class NewVariable
        {
            public static bool RestartNeeded = false;
            public static string DefaultTimeUpSound;
            public static string DefaultAlarmSound;
            public static string AlarmTimeoutDelay;
            public static string Language;
            public static bool ClockShowSecond;
            public static bool ClockFbxStyle;
            public static List<string> PinnedPlugin;
            public static List<string> PluginOrder;
            public static List<string> DisabledPlugin;
        }
    }
}