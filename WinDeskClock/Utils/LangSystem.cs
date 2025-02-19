using Newtonsoft.Json.Linq;
using System.IO;
using System.Windows;

namespace WinDeskClock.Utils
{
    public static class LangSystem
    {
        public static Dictionary<string, string> LangList = new Dictionary<string, string>();
        public static JObject LangData;

        public static async Task InitLang()
        {
            var uri = new Uri("pack://application:,,,/Resources/langs.json");
            using (Stream stream = Application.GetResourceStream(uri).Stream)
            using (StreamReader reader = new StreamReader(stream))
            {
                var jsonContent = reader.ReadToEnd();
                LangData = JObject.Parse(jsonContent);
            }

            LangList.Clear();

            foreach (var lang in LangData)
            {
                LangList.Add(lang.Value["id"].ToString(), lang.Value["name"].ToString());
            }
        }

        public static async Task<string> GetLang(string id)
        {
            var json = LangData;
            string[] keys = id.Split('.');
            for (int i = 0; i < keys.Length - 1; i++)
            {
                if (json[keys[i]] == null)
                {
                    throw new KeyNotFoundException($"Key '{keys[i]}' not found.");
                }
                json = (JObject)LangData[keys[i]];
            }
            return json[keys[^1]]?.ToString();
        }
    }
}
