using WinDeskClock.Utils;

namespace WinDeskClock.Services
{
    /// <summary>
    /// Injectable facade over WinDeskClock.Utils.LangSystem.
    ///
    /// The original LangSystem.GetLang was declared "async Task&lt;string&gt;" but never actually
    /// awaited anything (the language file is fully loaded into memory at startup by InitLang()),
    /// so every call just paid for a Task allocation for no benefit, and every call site needed
    /// "await". Since ViewModel property getters can't be async, this exposes a plain synchronous
    /// GetString instead - simpler and slightly faster, with identical lookup behaviour.
    /// </summary>
    public interface ILocalizationService
    {
        Task InitializeAsync();

        /// <summary>All available languages as (id, display name) pairs, for the language picker.</summary>
        IReadOnlyDictionary<string, string> AvailableLanguages { get; }

        /// <summary>Looks up "{CurrentLanguage}.content.{id}", replacing %0, %1... with the given values.</summary>
        string GetString(string id, params string[] replacements);
    }

    public class LocalizationService : ILocalizationService
    {
        public Task InitializeAsync() => LangSystem.InitLang();

        public IReadOnlyDictionary<string, string> AvailableLanguages => LangSystem.LangList;

        public string GetString(string id, params string[] replacements)
        {
            string fullPath = ConfigManager.Variables.Language + ".content." + id;
            var json = LangSystem.LangData;
            string[] path = fullPath.Split('.');
            Newtonsoft.Json.Linq.JToken? value = json[path[0]];
            for (int i = 1; i < path.Length && value != null; i++)
            {
                value = value[path[i]];
            }

            if (value == null)
            {
                throw new Exception($"Language string '{fullPath}' not found in '{ConfigManager.Variables.Language}'");
            }

            string result = value.ToString();
            for (int i = 0; i < replacements.Length; i++)
            {
                result = result.Replace("%" + i, replacements[i]);
            }

            return result;
        }
    }
}
