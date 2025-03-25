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

        public static async Task<string> GetLang(string id, params string[] replace)
        {
            id = ConfigManager.Variables.Language + "." + "content" + "." + id;
            var json = LangData;
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
