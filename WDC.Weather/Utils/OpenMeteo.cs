using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace WDC.Weather.Utils
{
    public class OpenMeteo
    {
        public static string WeatherAPI = "https://api.open-meteo.com/v1/forecast";
        public static string AirQualityAPI = "https://air-quality-api.open-meteo.com/v1/air-quality";
        public static string GeocodingAPI = "https://geocoding-api.open-meteo.com/v1/search?count=100&name=";

        public static HttpClient client = new HttpClient();

        public async static Task<WeatherData.WeatherDataHour> GetWeatherNow(Location location)
        {
            string urlweather = $"{WeatherAPI}?latitude={location.Latitude.ToString(CultureInfo.GetCultureInfo("en-US"))}&longitude={location.Longitude.ToString(CultureInfo.GetCultureInfo("en-US"))}&current=temperature_2m,weather_code,cloud_cover,relative_humidity_2m,precipitation,apparent_temperature,is_day,pressure_msl,wind_speed_10m,wind_direction_10m&timezone=auto";
            string urlair = $"{AirQualityAPI}?latitude={location.Latitude.ToString(CultureInfo.GetCultureInfo("en-US"))}&longitude={location.Longitude.ToString(CultureInfo.GetCultureInfo("en-US"))}&current=uv_index,us_aqi,european_aqi&timezone=auto";
            string urlvisibility = $"{WeatherAPI}?latitude={location.Latitude.ToString(CultureInfo.GetCultureInfo("en-US"))}&longitude={location.Longitude.ToString(CultureInfo.GetCultureInfo("en-US"))}&hourly=visibility&forecast_days=1&forecast_hours=1&timezone=auto";
            var responseweather = await client.GetAsync(urlweather);
            var responseair = await client.GetAsync(urlair);
            var responsevisibility = await client.GetAsync(urlvisibility);

            string strweather = await responseweather.Content.ReadAsStringAsync();
            Debug.WriteLine(strweather);
            var jsonweather = Newtonsoft.Json.Linq.JObject.Parse(strweather);
            string strair = await responseair.Content.ReadAsStringAsync();
            var jsonair = Newtonsoft.Json.Linq.JObject.Parse(strair);
            string strvisibility = await responsevisibility.Content.ReadAsStringAsync();
            var jsonvisibility = Newtonsoft.Json.Linq.JObject.Parse(strvisibility);

            WeatherData.WeatherDataHour weather = new WeatherData.WeatherDataHour();
            weather.DateTime = DateTime.Parse(jsonweather["current"]["time"].ToString());
            weather.Temperature = new Units.Temperature();
            weather.Temperature.Celsius = float.Parse(jsonweather["current"]["temperature_2m"].ToString());
            weather.FeelsLike = new Units.Temperature();
            weather.FeelsLike.Celsius = float.Parse(jsonweather["current"]["apparent_temperature"].ToString());
            weather.WMO = int.Parse(jsonweather["current"]["weather_code"].ToString());
            weather.Humidity = int.Parse(jsonweather["current"]["relative_humidity_2m"].ToString());
            weather.WindSpeed = new Units.Speed();
            weather.WindSpeed.Kph = float.Parse(jsonweather["current"]["wind_speed_10m"].ToString());
            weather.WindDirection = new Units.Orientation();
            weather.WindDirection.Deg = int.Parse(jsonweather["current"]["wind_direction_10m"].ToString());
            weather.UVIndex = new Units.UVIndex();
            weather.UVIndex.Index = float.Parse(jsonair["current"]["uv_index"].ToString());
            weather.AirQuality = new Units.AirQuality();
            weather.AirQuality.USAQI = int.Parse(jsonair["current"]["us_aqi"].ToString());
            weather.AirQuality.EUAQI = int.Parse(jsonair["current"]["european_aqi"].ToString());
            weather.Precipitation = new Units.Precipitation();
            weather.Precipitation.Mm = float.Parse(jsonweather["current"]["precipitation"].ToString());
            weather.Pressure = new Units.Pressure();
            weather.Pressure.Hpambar = float.Parse(jsonweather["current"]["pressure_msl"].ToString());
            weather.CloudCover = int.Parse(jsonweather["current"]["cloud_cover"].ToString());
            weather.Visibility = new Units.Distance();
            weather.Visibility.M = int.Parse(jsonvisibility["hourly"]["visibility"][0].ToString());
            return weather;
        }

        public async static Task<WeatherData.WeatherDataWeek> GetWeatherAll(Location location)
        {
            string urlDaysWeather = $"{WeatherAPI}?latitude={location.Latitude.ToString(CultureInfo.GetCultureInfo("en-US"))}&longitude={location.Longitude.ToString(CultureInfo.GetCultureInfo("en-US"))}&daily=sunrise,sunset,weather_code,temperature_2m_max,temperature_2m_min,precipitation_sum,temperature_2m_mean,apparent_temperature_mean,cloud_cover_mean,wind_direction_10m_dominant,wind_speed_10m_mean,pressure_msl_mean,visibility_mean,uv_index_max&timezone=auto";
            string urlHoursWeather = $"{WeatherAPI}?latitude={location.Latitude.ToString(CultureInfo.GetCultureInfo("en-US"))}&longitude={location.Longitude.ToString(CultureInfo.GetCultureInfo("en-US"))}&hourly=temperature_2m,weather_code,cloud_cover,relative_humidity_2m,precipitation,apparent_temperature,is_day,pressure_msl,wind_speed_10m,wind_direction_10m,visibility,uv_index&timezone=auto";
            string urlAir = $"{AirQualityAPI}?latitude={location.Latitude.ToString(CultureInfo.GetCultureInfo("en-US"))}&longitude={location.Longitude.ToString(CultureInfo.GetCultureInfo("en-US"))}&hourly=european_aqi,us_aqi&timezone=auto";
            var responseDaysWeather = await client.GetAsync(urlDaysWeather);
            var responseHoursWeather = await client.GetAsync(urlHoursWeather);
            var responseAir = await client.GetAsync(urlAir);

            string strDaysWeather = await responseDaysWeather.Content.ReadAsStringAsync();
            var jsonDaysWeather = Newtonsoft.Json.Linq.JObject.Parse(strDaysWeather);
            string strHoursWeather = await responseHoursWeather.Content.ReadAsStringAsync();
            var jsonHoursWeather = Newtonsoft.Json.Linq.JObject.Parse(strHoursWeather);
            string strAir = await responseAir.Content.ReadAsStringAsync();
            var jsonAir = Newtonsoft.Json.Linq.JObject.Parse(strAir);

            WeatherData.WeatherDataWeek weather = new WeatherData.WeatherDataWeek();
            weather.DateTimeStart = DateTime.Parse(jsonDaysWeather["daily"]["time"][0].ToString());
            weather.DateTimeEnd = DateTime.Parse(jsonDaysWeather["daily"]["time"][6].ToString());

            int[] usAqiMean = new int[5];
            int[] euAqiMean = new int[5];
            for (int day = 0; day < 5; day++)
            {
                int[] usAqis = new int[24];
                int[] euAqis = new int[24];
                for (int hour = 0; hour < 24; hour++)
                {
                    usAqis[hour] = int.TryParse(jsonAir["hourly"]["us_aqi"]?[day * 24 + hour]?.ToString(), out int usa) ? usa : 0;
                    euAqis[hour] = int.TryParse(jsonAir["hourly"]["european_aqi"]?[day * 24 + hour]?.ToString(), out int eu) ? eu : 0;

                }
                usAqiMean[day] = (int)usAqis.Average();
                euAqiMean[day] = (int)euAqis.Average();
            }

            WeatherData.WeatherDataDay[] weatherDataDays = new WeatherData.WeatherDataDay[7];
            for (int i = 0; i < 7; i++)
            {

                WeatherData.WeatherDataHour[] weatherDataHours = new WeatherData.WeatherDataHour[24];
                for (int j = 0; j < 24; j++)
                {
                    weatherDataHours[j] = new WeatherData.WeatherDataHour();
                    weatherDataHours[j].DateTime = DateTime.Parse(jsonHoursWeather["hourly"]["time"][i * 24 + j].ToString());
                    weatherDataHours[j].Temperature = new Units.Temperature();
                    weatherDataHours[j].Temperature.Celsius = float.Parse(jsonHoursWeather["hourly"]["temperature_2m"][i * 24 + j].ToString());
                    weatherDataHours[j].FeelsLike = new Units.Temperature();
                    weatherDataHours[j].FeelsLike.Celsius = float.Parse(jsonHoursWeather["hourly"]["apparent_temperature"][i * 24 + j].ToString());
                    weatherDataHours[j].WMO = int.Parse(jsonHoursWeather["hourly"]["weather_code"][i * 24 + j].ToString());
                    weatherDataHours[j].Humidity = int.Parse(jsonHoursWeather["hourly"]["relative_humidity_2m"][i * 24 + j].ToString());
                    weatherDataHours[j].WindSpeed = new Units.Speed();
                    weatherDataHours[j].WindSpeed.Kph = float.Parse(jsonHoursWeather["hourly"]["wind_speed_10m"][i * 24 + j].ToString());
                    weatherDataHours[j].WindDirection = new Units.Orientation();
                    weatherDataHours[j].WindDirection.Deg = int.Parse(jsonHoursWeather["hourly"]["wind_direction_10m"][i * 24 + j].ToString());
                    weatherDataHours[j].Pressure = new Units.Pressure();
                    weatherDataHours[j].Pressure.Hpambar = float.Parse(jsonHoursWeather["hourly"]["pressure_msl"][i * 24 + j].ToString());
                    weatherDataHours[j].CloudCover = int.Parse(jsonHoursWeather["hourly"]["cloud_cover"][i * 24 + j].ToString());
                    weatherDataHours[j].Visibility = new Units.Distance();
                    weatherDataHours[j].Visibility.M = (int)float.Parse(jsonHoursWeather["hourly"]["visibility"][i * 24 + j].ToString());
                    weatherDataHours[j].UVIndex = new Units.UVIndex();
                    weatherDataHours[j].UVIndex.Index = float.Parse(jsonHoursWeather["hourly"]["uv_index"][i * 24 + j].ToString());
                    weatherDataHours[j].Precipitation = new Units.Precipitation();
                    weatherDataHours[j].Precipitation.Mm = float.Parse(jsonHoursWeather["hourly"]["precipitation"][i * 24 + j].ToString());
                    if (i < 5)
                    {
                        weatherDataHours[j].AirQuality = new Units.AirQuality();
                        weatherDataHours[j].AirQuality.USAQI = int.TryParse(jsonAir["hourly"]["us_aqi"]?[i * 24 + j]?.ToString(), out int usaAqi) ? usaAqi : -1;
                        weatherDataHours[j].AirQuality.EUAQI = int.TryParse(jsonAir["hourly"]["european_aqi"]?[i * 24 + j]?.ToString(), out int euAqi) ? euAqi : -1;
                    }
                }

                weatherDataDays[i] = new WeatherData.WeatherDataDay();
                weatherDataDays[i].DateTime = DateTime.Parse(jsonDaysWeather["daily"]["time"][i].ToString());
                weatherDataDays[i].Sunrise = DateTime.Parse(jsonDaysWeather["daily"]["sunrise"][i].ToString());
                weatherDataDays[i].Sunset = DateTime.Parse(jsonDaysWeather["daily"]["sunset"][i].ToString());
                weatherDataDays[i].TemperatureMin = new Units.Temperature();
                weatherDataDays[i].TemperatureMin.Celsius = float.Parse(jsonDaysWeather["daily"]["temperature_2m_min"][i].ToString());
                weatherDataDays[i].TemperatureMax = new Units.Temperature();
                weatherDataDays[i].TemperatureMax.Celsius = float.Parse(jsonDaysWeather["daily"]["temperature_2m_max"][i].ToString());
                weatherDataDays[i].GlobalDay = new WeatherData.WeatherDataHour();
                weatherDataDays[i].GlobalDay.DateTime = DateTime.Parse(jsonDaysWeather["daily"]["time"][i].ToString());
                weatherDataDays[i].GlobalDay.Temperature = new Units.Temperature();
                weatherDataDays[i].GlobalDay.Temperature.Celsius = float.Parse(jsonDaysWeather["daily"]["temperature_2m_mean"][i].ToString());
                weatherDataDays[i].GlobalDay.FeelsLike = new Units.Temperature();
                weatherDataDays[i].GlobalDay.FeelsLike.Celsius = float.Parse(jsonDaysWeather["daily"]["apparent_temperature_mean"][i].ToString());
                weatherDataDays[i].GlobalDay.WMO = int.Parse(jsonDaysWeather["daily"]["weather_code"][i].ToString());
                weatherDataDays[i].GlobalDay.Humidity = int.Parse(jsonDaysWeather["daily"]["cloud_cover_mean"][i].ToString());
                weatherDataDays[i].GlobalDay.WindSpeed = new Units.Speed();
                weatherDataDays[i].GlobalDay.WindSpeed.Kph = float.Parse(jsonDaysWeather["daily"]["wind_speed_10m_mean"][i].ToString());
                weatherDataDays[i].GlobalDay.WindDirection = new Units.Orientation();
                weatherDataDays[i].GlobalDay.WindDirection.Deg = int.Parse(jsonDaysWeather["daily"]["wind_direction_10m_dominant"][i].ToString());
                weatherDataDays[i].GlobalDay.Pressure = new Units.Pressure();
                weatherDataDays[i].GlobalDay.Pressure.Hpambar = float.Parse(jsonDaysWeather["daily"]["pressure_msl_mean"][i].ToString());
                weatherDataDays[i].GlobalDay.CloudCover = int.Parse(jsonDaysWeather["daily"]["cloud_cover_mean"][i].ToString());
                weatherDataDays[i].GlobalDay.Visibility = new Units.Distance();
                weatherDataDays[i].GlobalDay.Visibility.M = (int)float.Parse(jsonDaysWeather["daily"]["visibility_mean"][i].ToString());
                weatherDataDays[i].GlobalDay.UVIndex = new Units.UVIndex();
                weatherDataDays[i].GlobalDay.UVIndex.Index = float.Parse(jsonDaysWeather["daily"]["uv_index_max"][i].ToString());
                weatherDataDays[i].GlobalDay.Precipitation = new Units.Precipitation();
                weatherDataDays[i].GlobalDay.Precipitation.Mm = float.Parse(jsonDaysWeather["daily"]["precipitation_sum"][i].ToString());
                if (i < 5)
                {
                    weatherDataDays[i].GlobalDay.AirQuality = new Units.AirQuality();
                    weatherDataDays[i].GlobalDay.AirQuality.USAQI = usAqiMean[i];
                    weatherDataDays[i].GlobalDay.AirQuality.EUAQI = euAqiMean[i];
                }

                weatherDataDays[i].Hourly = weatherDataHours;
            }

            weather.Daily = weatherDataDays;
            return weather;
        }
    }
}
