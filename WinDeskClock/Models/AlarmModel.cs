namespace WinDeskClock.Models
{
    /// <summary>
    /// How an alarm plays (or doesn't play) a sound when it triggers.
    /// </summary>
    public enum AlarmSoundMode
    {
        None,
        Default,
        Custom
    }

    /// <summary>
    /// Plain data representation of a single alarm, as persisted in alarms.json.
    /// This is the "Model" in MVVM: no WPF types, no notification, just data + (de)serialization
    /// helpers so it stays compatible with the existing alarms.json file format used by earlier
    /// versions of the app.
    /// </summary>
    public class AlarmModel
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = string.Empty;
        public int Hour { get; set; }
        public int Minute { get; set; }

        /// <summary>
        /// Days the alarm repeats on: 1 = Monday ... 7 = Sunday (ISO order), matching the
        /// original file format. An empty set means a one-shot ("once") alarm.
        /// </summary>
        public List<int> Days { get; set; } = new();

        public AlarmSoundMode SoundMode { get; set; } = AlarmSoundMode.Default;
        public string CustomSoundPath { get; set; } = string.Empty;
        public bool Enabled { get; set; } = true;

        public bool IsOnce => Days.Count == 0;

        public TimeOnly Time
        {
            get => new(Hour, Minute);
            set { Hour = value.Hour; Minute = value.Minute; }
        }

        /// <summary>
        /// Creates a deep copy, used to snapshot the saved state before editing (for Cancel support).
        /// </summary>
        public AlarmModel Clone() => new()
        {
            Id = Id,
            Name = Name,
            Hour = Hour,
            Minute = Minute,
            Days = new List<int>(Days),
            SoundMode = SoundMode,
            CustomSoundPath = CustomSoundPath,
            Enabled = Enabled
        };

        /// <summary>
        /// Serializes the "days" field the same way the original app did: "0" for a one-shot
        /// alarm, otherwise the selected day numbers concatenated in ascending order (e.g. "1234567",
        /// "12345", "67", ...). Kept identical so language strings that special-case "12345"/"67"/
        /// "1234567" (workdays / weekend / every day) keep matching.
        /// </summary>
        public string DaysToStorageString()
        {
            return IsOnce ? "0" : string.Concat(Days.OrderBy(d => d));
        }

        public static List<int> ParseDaysFromStorageString(string? raw)
        {
            if (string.IsNullOrEmpty(raw) || raw == "0")
            {
                return new List<int>();
            }

            return raw.Where(char.IsDigit)
                       .Select(c => c - '0')
                       .Where(d => d is >= 1 and <= 7)
                       .Distinct()
                       .OrderBy(d => d)
                       .ToList();
        }

        public string SoundToStorageString() => SoundMode switch
        {
            AlarmSoundMode.None => "none",
            AlarmSoundMode.Default => "default",
            AlarmSoundMode.Custom => CustomSoundPath,
            _ => "default"
        };

        public static (AlarmSoundMode mode, string customPath) ParseSoundFromStorageString(string? raw)
        {
            if (string.IsNullOrEmpty(raw) || raw == "none")
            {
                return (AlarmSoundMode.None, string.Empty);
            }

            if (raw == "default")
            {
                return (AlarmSoundMode.Default, string.Empty);
            }

            return (AlarmSoundMode.Custom, raw);
        }
    }
}
