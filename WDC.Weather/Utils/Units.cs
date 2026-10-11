using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WDC.Weather.Utils
{
    public class Units
    {
        public enum TemperatureUnit
        {
            Celsius,
            Fahrenheit
        }
        public enum SpeedUnit
        {
            Kph,
            Mph,
            Ms
        }
        public enum PressureUnit
        {
            Hpambar
        }
        public enum DistanceUnit
        {
            M,
            Km,
            Miles
        }
        public enum PrecipitationUnit
        {
            Mm,
            Inches
        }
        public enum AirQualityUnit
        {
            USAQI,
            EUAQI
        }

        public class Temperature
        {
            public float Celsius { get; set; } // Celsius (Input)
            public float Farenheit { get { return Celsius * 1.8f + 32; } } // Fahrenheit
            public float Kelvin { get { return Celsius + 273.15f; } } // Kelvin
            public float GetValue()
            {
                if (Config.Variables.TemperatureUnit == TemperatureUnit.Celsius)
                {
                    if (Config.Variables.FloatMode)
                    {
                        return Celsius;
                    }
                    else
                    {
                        return (float)Math.Round(Celsius, 0);
                    }
                }
                else if (Config.Variables.TemperatureUnit == TemperatureUnit.Fahrenheit)
                {
                    if (Config.Variables.FloatMode)
                    {
                        return Farenheit;
                    }
                    else
                    {
                        return (float)Math.Round(Farenheit, 0);
                    }
                }
                else
                {
                    return -1;
                }
            }
            public async Task<string> GetFormatedText()
            {
                float value = GetValue();
                if (Config.Variables.TemperatureUnit == TemperatureUnit.Celsius)
                {
                    return await Lang.GetLang("units.temperature.c", value.ToString("0.#"));
                }
                else if (Config.Variables.TemperatureUnit == TemperatureUnit.Fahrenheit)
                {
                    return await Lang.GetLang("units.temperature.f", value.ToString("0.#"));
                }
                else
                {
                    return "";
                }
            }
        }

        public class Speed
        {
            public float Kph { get; set; } // Kilometers per hour (Input)
            public float Mph { get { return Kph / 1.60934f; } } // Miles per hour
            public float Ms { get { return Kph / 3.6f; } } // Meters per second
            public float GetValue()
            {
                if (Config.Variables.SpeedUnit == SpeedUnit.Kph)
                {
                    if (Config.Variables.FloatMode)
                    {
                        return Kph;
                    }
                    else
                    {
                        return (float)Math.Round(Kph, 0);
                    }
                }
                else if (Config.Variables.SpeedUnit == SpeedUnit.Mph)
                {
                    if (Config.Variables.FloatMode)
                    {
                        return Mph;
                    }
                    else
                    {
                        return (float)Math.Round(Mph, 0);
                    }
                }
                else if (Config.Variables.SpeedUnit == SpeedUnit.Ms)
                {
                    if (Config.Variables.FloatMode)
                    {
                        return Ms;
                    }
                    else
                    {
                        return (float)Math.Round(Ms, 0);
                    }
                }
                else
                {
                    return -1;
                }
            }
            public async Task<string> GetFormatedText()
            {
                float value = GetValue();
                if (Config.Variables.SpeedUnit == SpeedUnit.Kph)
                {
                    return await Lang.GetLang("units.speed.kph", value.ToString("0.#"));
                }
                else if (Config.Variables.SpeedUnit == SpeedUnit.Mph)
                {
                    return await Lang.GetLang("units.speed.mph", value.ToString("0.#"));
                }
                else if (Config.Variables.SpeedUnit == SpeedUnit.Ms)
                {
                    return await Lang.GetLang("units.speed.ms", value.ToString("0.#"));
                }
                else
                {
                    return "";
                }
            }
        }

        public class Orientation
        {
            public int Deg { get; set; } // Degrees (Input)
            public async Task<string> GetCardinal()
            {
                if (Deg >= 337.5 || Deg < 22.5) return await Lang.GetLang("direction.short.n");
                if (Deg >= 22.5 && Deg < 67.5) return await Lang.GetLang("direction.short.ne");
                if (Deg >= 67.5 && Deg < 112.5) return await Lang.GetLang("direction.short.e");
                if (Deg >= 112.5 && Deg < 157.5) return await Lang.GetLang("direction.short.se");
                if (Deg >= 157.5 && Deg < 202.5) return await Lang.GetLang("direction.short.s");
                if (Deg >= 202.5 && Deg < 247.5) return await Lang.GetLang("direction.short.sw");
                if (Deg >= 247.5 && Deg < 292.5) return await Lang.GetLang("direction.short.w");
                if (Deg >= 292.5 && Deg < 337.5) return await Lang.GetLang("direction.short.nw");
                return null;
            }
        }

        public class Pressure
        {
            public float Hpambar { get; set; } // Hectopascals/millibars
            public float GetValue()
            {
                if (Config.Variables.PressureUnit == PressureUnit.Hpambar)
                {
                    if (Config.Variables.FloatMode)
                    {
                        return Hpambar;
                    }
                    else
                    {
                        return (float)Math.Round(Hpambar, 0);
                    }
                }
                else
                {
                    return -1;
                }
            }
            public async Task<string> GetFormatedText()
            {
                float value = GetValue();
                if (Config.Variables.PressureUnit == PressureUnit.Hpambar)
                {
                    return await Lang.GetLang("units.pressure.hpa", value.ToString("0.#"));
                }
                else
                {
                    return "";
                }
            }
        }

        public class Distance
        {
            public int M { get; set; } // Meters (Input)
            public float Km { get { return M / 1000f; } } // Kilometers
            public float Miles { get { return M / 1609.34f; } } // Miles
            public float GetValue()
            {
                if (Config.Variables.DistanceUnit == DistanceUnit.M)
                {
                    return M;
                }
                else if (Config.Variables.DistanceUnit == DistanceUnit.Km)
                {
                    if (Config.Variables.FloatMode)
                    {
                        return Km;
                    }
                    else
                    {
                        return (float)Math.Round(Km, 0);
                    }
                }
                else if (Config.Variables.DistanceUnit == DistanceUnit.Miles)
                {
                    if (Config.Variables.FloatMode)
                    {
                        return Miles;
                    }
                    else
                    {
                        return (float)Math.Round(Miles, 0);
                    }
                }
                else
                {
                    return -1;
                }
            }
            public async Task<string> GetFormatedText()
            {
                float value = GetValue();
                if (Config.Variables.DistanceUnit == DistanceUnit.M)
                {
                    return await Lang.GetLang("units.distance.m", value.ToString("0.#"));
                }
                else if (Config.Variables.DistanceUnit == DistanceUnit.Km)
                {
                    return await Lang.GetLang("units.distance.km", value.ToString("0.#"));
                }
                else if (Config.Variables.DistanceUnit == DistanceUnit.Miles)
                {
                    return await Lang.GetLang("units.distance.mi", value.ToString("0.#"));
                }
                else
                {
                    return "";
                }
            }
        }

        public class Precipitation
        {
            public float Mm { get; set; } // Millimeters (Input)
            public float Inches { get { return Mm / 25.4f; } } // Inches
            public float GetValue()
            {
                if (Config.Variables.PrecipitationUnit == PrecipitationUnit.Mm)
                {
                    if (Config.Variables.FloatMode)
                    {
                        return Mm;
                    }
                    else
                    {
                        return (float)Math.Round(Mm, 0);
                    }
                }
                else if (Config.Variables.PrecipitationUnit == PrecipitationUnit.Inches)
                {
                    if (Config.Variables.FloatMode)
                    {
                        return Inches;
                    }
                    else
                    {
                        return (float)Math.Round(Inches, 0);
                    }
                }
                else
                {
                    return -1;
                }
            }
            public async Task<string> GetFormatedText()
            {
                float value = GetValue();
                if (Config.Variables.PrecipitationUnit == PrecipitationUnit.Mm)
                {
                    return await Lang.GetLang("units.precipitation.mm", value.ToString("0.#"));
                }
                else if (Config.Variables.PrecipitationUnit == PrecipitationUnit.Inches)
                {
                    return await Lang.GetLang("units.precipitation.in", value.ToString("0.#"));
                }
                else
                {
                    return "";
                }
            }
        }

        public class UVIndex
        {
            public float Index { get; set; } // UV Index (Input)
            public float GetValue()
            {
                if (Config.Variables.FloatMode)
                {
                    return Index;
                }
                else
                {
                    return (float)Math.Round(Index, 0);
                }
            }
            public async Task<string> GetUVISafety()
            {
                switch (Index)
                {
                    case < 3:
                        return await Lang.GetLang("safety.good");
                    case < 5:
                        return await Lang.GetLang("safety.moderate");
                    case < 7:
                        return await Lang.GetLang("safety.bad");
                    case < 9:
                        return await Lang.GetLang("safety.warning");
                    case < 11:
                        return await Lang.GetLang("safety.danger");
                    default:
                        return await Lang.GetLang("safety.hazardous");

                }
            }
        }

        public class AirQuality
        {
            public int USAQI { get; set; } // United States AQI
            public int EUAQI { get; set; } // European AQI
            public int GetValue()
            {
                if (Config.Variables.AirQualityUnit == AirQualityUnit.USAQI)
                {
                    return USAQI;
                }
                else if (Config.Variables.AirQualityUnit == AirQualityUnit.EUAQI)
                {
                    return EUAQI;
                }
                else
                {
                    return -1;
                }
            }
            public async Task<string> GetSafety()
            {
                if (Config.Variables.AirQualityUnit == AirQualityUnit.USAQI)
                {
                    return await GetUSAQISafety();
                }
                else if (Config.Variables.AirQualityUnit == AirQualityUnit.EUAQI)
                {
                    return await GetEUAQISafety();
                }
                else
                {
                    return null;
                }
            }
            public async Task<string> GetUSAQISafety()
            {
                switch (USAQI)
                {
                    case < 50:
                        return await Lang.GetLang("safety.good");
                    case < 100:
                        return await Lang.GetLang("safety.moderate");
                    case < 150:
                        return await Lang.GetLang("safety.bad");
                    case < 200:
                        return await Lang.GetLang("safety.warning");
                    case < 300:
                        return await Lang.GetLang("safety.danger");
                    default:
                        return await Lang.GetLang("safety.hazardous");
                }
            }

            public async Task<string> GetEUAQISafety()
            {
                switch (EUAQI)
                {
                    case < 50:
                        return await Lang.GetLang("safety.good");
                    case < 100:
                        return await Lang.GetLang("safety.moderate");
                    case < 150:
                        return await Lang.GetLang("safety.bad");
                    case < 200:
                        return await Lang.GetLang("safety.warning");
                    case < 300:
                        return await Lang.GetLang("safety.danger");
                    default:
                        return await Lang.GetLang("safety.hazardous");
                }
            }
        }
    }
}
