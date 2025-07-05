using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using WinDeskClock.Utils;

namespace WinDeskClock.Interfaces
{
    // Plugin interface to retrieve plugin information and Pages

    public interface IPluginInfo
    {
        // Plugin information
        // This interface is a stable interface that will not change in the future (normally)
        string Name { get; }  // Name of the plugin
        string ID { get; }  // Unique ID of the plugin
        string Description { get; }  // Description of the plugin
        string Author { get; }  // Author of the plugin
        string AuthorURL { get; }  // Author's URL
        string Version { get; }  // Version of the plugin
        int VersionCode { get; }  // Version code of the plugin
        string UpdateURL { get; }  // URL to check and download updates
        BitmapImage Icon { get; }  // Icon of the plugin
        string ProjectWebsiteURL { get; }  // URL to the project
        string ProjectSourceURL { get; }  // URL to the project source code
    }

    public interface IPluginModule
    {
        // Pages of the plugin
        // This interface is a not very stable interface that may change in the future to add,update,delete features
        Page GetMain();  // Main page of the plugin
        Page GetSettings();  // Settings page of the plugin
        Task SaveConfig();  // Save configuration of the plugin
        Task OnEvent(string eventName, object? data = null);  // Event handler for the plugin
    }

    public static class IPluginAppStatus
    {
        // Application status for the plugin
        // This is not an interface to ensure there is no crash or error if the plugin does not implement it or if there is update in the future
        public static string Language { get { return ConfigManager.Variables.Language; } } // Language of the application

    }

    public static class IPluginLog
    {
        // Logging for the plugin
        // This is not an interface to ensure there is no crash or error if the plugin does not implement it or if there is update in the future
        private static string PluginID = "null";
        private static string LogFilePath => Path.Combine(
            Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? "",
            "plugins", "plugins_log", $"{PluginID}.txt"
        );

        public static void Init(string pluginID)
        {
            PluginID = pluginID;
        }

        public static async Task Error(string message)
        {
            if (PluginID != "null")
            {
                string errorMessage = $"[{Log.GetTimeStamp()}][E][{PluginID}]: {message}";
                if (Log.GetConsoleWindow() != IntPtr.Zero)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine(errorMessage);
                    Console.ResetColor();
                }
                if (Debugger.IsAttached)
                {
                    Debug.WriteLine(errorMessage);
                }
                await Write2File(errorMessage.Replace($"[{PluginID}]", ""));
            }
        }
        public static async Task Warning(string message)
        {
            if (PluginID != "null")
            {
                string warningMessage = $"[{Log.GetTimeStamp()}][W][{PluginID}]: {message}";
                if (Log.GetConsoleWindow() != IntPtr.Zero)
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine(warningMessage);
                    Console.ResetColor();
                }
                if (Debugger.IsAttached)
                {
                    Debug.WriteLine(warningMessage);
                }
                await Write2File(warningMessage.Replace($"[{PluginID}]", ""));
            }
        }
        public static async Task Info(string message)
        {
            if (PluginID != "null")
            {
                string infoMessage = $"[{Log.GetTimeStamp()}][I][{PluginID}]: {message}";
                if (Log.GetConsoleWindow() != IntPtr.Zero)
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine(infoMessage);
                    Console.ResetColor();
                }
                if (Debugger.IsAttached)
                {
                    Debug.WriteLine(infoMessage);
                }
                await Write2File(infoMessage.Replace($"[{PluginID}]", ""));
            }
        }

        public static async Task Write2File(string message)
        {
            if (PluginID != "null")
            {
                try
                {
                    if (!File.Exists(LogFilePath))
                    {
                        using (System.IO.StreamWriter file = new System.IO.StreamWriter(LogFilePath, false))
                        {
                            await file.WriteLineAsync($"=== WinDeskClock by STY1001 ===\nLog file created at: {Log.GetTimeStamp()}\n");
                        }
                    }

                    using (System.IO.StreamWriter file = new System.IO.StreamWriter(LogFilePath, true))
                    {
                        await file.WriteLineAsync(message);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"!!! Failed to write to log file: {ex.Message} !!!");
                    Debug.WriteLine($"!!! Failed to write to log file: {ex.Message} !!!");
                }
            }
        }
    }
}
