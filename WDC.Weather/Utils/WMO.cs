using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;

namespace WDC.Weather.Utils
{
    public class WMO
    {
        private static JObject wmoJson;

        public static async Task LoadJson()
        {
            using (Stream stream = Application.GetResourceStream(new Uri($"pack://application:,,,/WDC.Weather;component/Resources/wmo/codes.json")).Stream)
            {
                using (StreamReader reader = new StreamReader(stream))
                {
                    string json = await reader.ReadToEndAsync();
                    wmoJson = JObject.Parse(json);
                }
            }
        }

        public static async Task<BitmapImage> GetWMOIcon(int wmo, bool isday)
        {
            BitmapImage icon = new BitmapImage(new Uri($"pack://application:,,,/WDC.Weather;component/Resources/wmo/icons/{wmoJson[wmo.ToString()][isday ? "day" : "night"]["icon"].ToString()}"));
            return icon;
        }

        public static async Task<string> GetWMOIconName(int wmo, bool isday)
        {
            return wmoJson[wmo.ToString()][isday ? "day" : "night"]["icon"].ToString();
        }

        public static async Task<BitmapImage> GetWMOBack(int wmo, bool isday)
        {
            BitmapImage back = new BitmapImage(new Uri($"pack://application:,,,/WDC.Weather;component/Resources/wmo/backs/{wmoJson[wmo.ToString()][isday ? "day" : "night"]["back"].ToString()}"));
            return back;
        }

        public static async Task<string> GetWMODesc(int wmo, bool isday)
        {
            return await Lang.GetLang("weather.wmo." + wmoJson[wmo.ToString()][isday ? "day" : "night"]["desc"].ToString());
        }
    }
}
