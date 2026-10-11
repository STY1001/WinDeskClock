using System.IO;
using Newtonsoft.Json.Linq;
using WinDeskClock.Models;
using WinDeskClock.Utils;

namespace WinDeskClock.Services
{
    /// <summary>
    /// Loads and saves alarms as a whole list, in memory. This replaces the original approach of
    /// re-reading/re-writing alarms.json on every single field access (every tick, every card
    /// redraw...), which was the biggest performance sink in the old code: a full JSON file
    /// round-trip per property per alarm, dozens of times a second while the clock menu was open.
    /// </summary>
    public interface IAlarmRepository
    {
        Task<List<AlarmModel>> LoadAllAsync();
        Task SaveAllAsync(IEnumerable<AlarmModel> alarms);
    }

    public class AlarmRepository : IAlarmRepository
    {
        public async Task<List<AlarmModel>> LoadAllAsync()
        {
            if (!File.Exists(ConfigManager.AlarmPath))
            {
                return new List<AlarmModel>();
            }

            string json = await File.ReadAllTextAsync(ConfigManager.AlarmPath);
            if (string.IsNullOrWhiteSpace(json))
            {
                return new List<AlarmModel>();
            }

            JObject root = JObject.Parse(json);
            var result = new List<AlarmModel>();

            foreach (var property in root.Properties())
            {
                JObject entry = (JObject)property.Value;
                var (soundMode, customPath) = AlarmModel.ParseSoundFromStorageString(entry.Value<string>("sound"));
                string time = entry.Value<string>("time") ?? "00:00";
                string[] timeParts = time.Split(':');

                var alarm = new AlarmModel
                {
                    Id = property.Name,
                    Name = entry.Value<string>("name") ?? string.Empty,
                    Hour = int.Parse(timeParts[0]),
                    Minute = int.Parse(timeParts[1]),
                    Days = AlarmModel.ParseDaysFromStorageString(entry.Value<string>("days")),
                    SoundMode = soundMode,
                    CustomSoundPath = customPath,
                    Enabled = entry.Value<string>("enabled") == "true"
                };
                result.Add(alarm);
            }

            return result;
        }

        public async Task SaveAllAsync(IEnumerable<AlarmModel> alarms)
        {
            var root = new JObject();
            foreach (var alarm in alarms)
            {
                root[alarm.Id] = new JObject
                {
                    ["time"] = $"{alarm.Hour:00}:{alarm.Minute:00}",
                    ["name"] = alarm.Name,
                    ["days"] = alarm.DaysToStorageString(),
                    ["sound"] = alarm.SoundToStorageString(),
                    ["enabled"] = alarm.Enabled ? "true" : "false"
                };
            }

            Directory.CreateDirectory(Path.GetDirectoryName(ConfigManager.AlarmPath)!);
            await File.WriteAllTextAsync(ConfigManager.AlarmPath, root.ToString());
        }
    }
}
