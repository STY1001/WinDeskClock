using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WDC.Weather.Utils
{
    public class WeatherData
    {
        public class WeatherDataHour
        {
            public DateTime DateTime { get; set; }
            public Units.Temperature Temperature { get; set; }
            public Units.Temperature FeelsLike { get; set; }
            public int WMO { get; set; }
            public int Humidity { get; set; }
            public Units.Speed WindSpeed { get; set; }
            public Units.Orientation WindDirection { get; set; }
            public Units.UVIndex UVIndex { get; set; }
            public Units.AirQuality AirQuality { get; set; }
            public Units.Precipitation Precipitation { get; set; }
            public Units.Pressure Pressure { get; set; }
            public int CloudCover { get; set; }
            public Units.Distance Visibility { get; set; }
        }

        public class WeatherDataDay
        {
            public DateTime DateTime { get; set; }
            public WeatherDataHour GlobalDay { get; set; }
            public DateTime Sunrise { get; set; }
            public DateTime Sunset { get; set; }
            public Utils.Units.Temperature TemperatureMin { get; set; }
            public Utils.Units.Temperature TemperatureMax { get; set; }
            public WeatherDataHour[] Hourly { get; set; }
        }

        public class WeatherDataWeek
        {
            public DateTime DateTimeStart { get; set; }
            public DateTime DateTimeEnd { get; set; }
            public WeatherDataDay[] Daily { get; set; }
        }
    }
}
