using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WDC.Weather.Utils
{
    public static class Config
    {
        public static readonly string ConfigPath;

        static Config()
        {
            ConfigPath = Path.Combine(Plugin.PluginDataPath, "config.json");
        }

        public static async Task CheckConf()
        {
            Dictionary<string, string> DefaultConf = new Dictionary<string, string>
            {
                { "Location.Name", "" },
                { "Location.Country", "" },
                { "Location.Admin1", "" },
                { "Location.Admin2", "" },
                { "Location.Admin3", "" },
                { "Location.Admin4", "" },
                { "Location.Elevation", "" },
                { "Location.Latitude", "123456" },
                { "Location.Longitude", "654321" },
                { "FloatMode", "False" },
                { "TemperatureUnit", "Celsius" },
                { "SpeedUnit", "Kph" },
                { "PressureUnit", "Hpambar" },
                { "DistanceUnit", "Km" },
                { "PrecipitationUnit", "Mm" },
                { "AirQualityUnit", "EUAQI" }
            };

            if (!File.Exists(ConfigPath))
            {
                await File.WriteAllTextAsync(ConfigPath, "{}");
                foreach (var item in DefaultConf)
                {
                    await SetConfig(item.Key, item.Value);
                }
            }
            foreach (var item in DefaultConf)
            {
                if (await GetConfig(item.Key) == null)
                {
                    await SetConfig(item.Key, item.Value);
                }
            }
        }

        public static async Task LoadConfig()
        {
            Variables.Location = new Location
            {
                Name = await GetConfig("Location.Name") != "" ? await GetConfig("Location.Name") : null,
                Country = await GetConfig("Location.Country") != "" ? await GetConfig("Location.Country") : null,
                Admin1 = await GetConfig("Location.Admin1") != "" ? await GetConfig("Location.Admin1") : null,
                Admin2 = await GetConfig("Location.Admin2") != "" ? await GetConfig("Location.Admin2") : null,
                Admin3 = await GetConfig("Location.Admin3") != "" ? await GetConfig("Location.Admin3") : null,
                Admin4 = await GetConfig("Location.Admin4") != "" ? await GetConfig("Location.Admin4") : null,
                Elevation = await GetConfig("Location.Elevation") != "" ? double.Parse(await GetConfig("Location.Elevation"), CultureInfo.GetCultureInfo("en-US")) : null,
                Latitude = double.Parse(await GetConfig("Location.Latitude"), CultureInfo.GetCultureInfo("en-US")),
                Longitude = double.Parse(await GetConfig("Location.Longitude"), CultureInfo.GetCultureInfo("en-US"))
            };

            Variables.FloatMode = bool.Parse(await GetConfig("FloatMode"));

            string tempUnit = await GetConfig("TemperatureUnit");
            Variables.TemperatureUnit = Enum.TryParse(tempUnit, out Units.TemperatureUnit tempUnitEnum) ? tempUnitEnum : Units.TemperatureUnit.Celsius;
            string speedUnit = await GetConfig("SpeedUnit");
            Variables.SpeedUnit = Enum.TryParse(speedUnit, out Units.SpeedUnit speedUnitEnum) ? speedUnitEnum : Units.SpeedUnit.Kph;
            string pressureUnit = await GetConfig("PressureUnit");
            Variables.PressureUnit = Enum.TryParse(pressureUnit, out Units.PressureUnit pressureUnitEnum) ? pressureUnitEnum : Units.PressureUnit.Hpambar;
            string distanceUnit = await GetConfig("DistanceUnit");
            Variables.DistanceUnit = Enum.TryParse(distanceUnit, out Units.DistanceUnit distanceUnitEnum) ? distanceUnitEnum : Units.DistanceUnit.Km;
            string precipitationUnit = await GetConfig("PrecipitationUnit");
            Variables.PrecipitationUnit = Enum.TryParse(precipitationUnit, out Units.PrecipitationUnit precipitationUnitEnum) ? precipitationUnitEnum : Units.PrecipitationUnit.Mm;
            string airQualityUnit = await GetConfig("AirQualityUnit");
            Variables.AirQualityUnit = Enum.TryParse(airQualityUnit, out Units.AirQualityUnit airQualityUnitEnum) ? airQualityUnitEnum : Units.AirQualityUnit.EUAQI;

            NewVariables.Location = new Location
            {
                Name = Variables.Location.Name,
                Country = Variables.Location.Country,
                Admin1 = Variables.Location.Admin1,
                Admin2 = Variables.Location.Admin2,
                Admin3 = Variables.Location.Admin3,
                Admin4 = Variables.Location.Admin4,
                Elevation = Variables.Location.Elevation,
                Latitude = Variables.Location.Latitude,
                Longitude = Variables.Location.Longitude
            };

            NewVariables.FloatMode = Variables.FloatMode;

            NewVariables.TemperatureUnit = Variables.TemperatureUnit;
            NewVariables.SpeedUnit = Variables.SpeedUnit;
            NewVariables.PressureUnit = Variables.PressureUnit;
            NewVariables.DistanceUnit = Variables.DistanceUnit;
            NewVariables.PrecipitationUnit = Variables.PrecipitationUnit;
            NewVariables.AirQualityUnit = Variables.AirQualityUnit;
        }
        public static async Task SaveConfig()
        {
            await SetConfig("Location.Name", NewVariables.Location.Name != null ? NewVariables.Location.Name.ToString() : "");
            await SetConfig("Location.Country", NewVariables.Location.Country != null ? NewVariables.Location.Country.ToString() : "");
            await SetConfig("Location.Admin1", NewVariables.Location.Admin1 != null ? NewVariables.Location.Admin1.ToString() : "");
            await SetConfig("Location.Admin2", NewVariables.Location.Admin2 != null ? NewVariables.Location.Admin2.ToString() : "");
            await SetConfig("Location.Admin3", NewVariables.Location.Admin3 != null ? NewVariables.Location.Admin3.ToString() : "");
            await SetConfig("Location.Admin4", NewVariables.Location.Admin4 != null ? NewVariables.Location.Admin4.ToString() : "");
            await SetConfig("Location.Elevation", NewVariables.Location.Elevation != null ? NewVariables.Location.Elevation?.ToString(CultureInfo.GetCultureInfo("en-US")) : "");
            await SetConfig("Location.Latitude", NewVariables.Location.Latitude.ToString(CultureInfo.GetCultureInfo("en-US")));
            await SetConfig("Location.Longitude", NewVariables.Location.Longitude.ToString(CultureInfo.GetCultureInfo("en-US")));

            await SetConfig("FloatMode", NewVariables.FloatMode.ToString());

            await SetConfig("TemperatureUnit", NewVariables.TemperatureUnit.ToString());
            await SetConfig("SpeedUnit", NewVariables.SpeedUnit.ToString());
            await SetConfig("PressureUnit", NewVariables.PressureUnit.ToString());
            await SetConfig("DistanceUnit", NewVariables.DistanceUnit.ToString());
            await SetConfig("PrecipitationUnit", NewVariables.PrecipitationUnit.ToString());
            await SetConfig("AirQualityUnit", NewVariables.AirQualityUnit.ToString());
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

        public static class Variables
        {
            public static Location Location;
            public static bool FloatMode;
            public static Units.TemperatureUnit TemperatureUnit;
            public static Units.SpeedUnit SpeedUnit;
            public static Units.PressureUnit PressureUnit;
            public static Units.DistanceUnit DistanceUnit;
            public static Units.PrecipitationUnit PrecipitationUnit;
            public static Units.AirQualityUnit AirQualityUnit;
        }

        public static class NewVariables
        {
            public static Location Location;
            public static bool FloatMode;
            public static Units.TemperatureUnit TemperatureUnit;
            public static Units.SpeedUnit SpeedUnit;
            public static Units.PressureUnit PressureUnit;
            public static Units.DistanceUnit DistanceUnit;
            public static Units.PrecipitationUnit PrecipitationUnit;
            public static Units.AirQualityUnit AirQualityUnit;
        }
    }
}
