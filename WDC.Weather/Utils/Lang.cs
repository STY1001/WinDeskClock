using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using WinDeskClock.Interfaces;
using WinDeskClock.Utils;

namespace WDC.Weather.Utils
{
    public class Lang
    {
        private static JObject langJson;
        public static string langId;

        public static async Task InitLang()
        {
            using (Stream stream = Application.GetResourceStream(new Uri($"pack://application:,,,/WDC.Weather;component/Resources/langs.json")).Stream)
            {
                using (StreamReader reader = new StreamReader(stream))
                {
                    string json = await reader.ReadToEndAsync();
                    langJson = JObject.Parse(json);
                }
            }

            if (langJson[IPluginAppStatus.Language] == null)
            {
                langId = "en-us"; // Default language
            }
            else
            {
                langId = IPluginAppStatus.Language;
            }
        }

        public static async Task<string> GetLang(string id, params string[] replace)
        {
            id = langId+ "." + "content" + "." + id;
            var json = langJson;
            string[] path = id.Split('.');
            JToken value = json[path[0]];
            for (int i = 1; i < path.Length; i++)
            {
                value = value[path[i]];
            }
            if (value == null)
            {
                throw new Exception($"Lang not found: {id}");
            }
            string valueStr = value.ToString();
            foreach (var item in replace)
            {
                valueStr = valueStr.Replace("%" + replace.ToList().IndexOf(item).ToString(), item);
            }
            return valueStr;
        }
    }
}
