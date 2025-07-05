using System.IO;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using WinDeskClock.Interfaces;
using WinDeskClock.Utils;

namespace WinDeskClockPlugin
{
    public static class Plugin
    {
        private static readonly PluginInfo pluginInfo = new PluginInfo();

        static Plugin()
        {
            //Intialize logging for the plugin
            IPluginLog.Init(pluginInfo.ID);
            // If you want to use logging (I recommend it), you can use the IPluginLog class to log messages
            // Example: IPluginLog.Error("This is an error message");
            // There is also other methods, IPluginLog.Info and IPluginLog.Warning
            // Plugin specific logging will be saved in the "plugins_log" folder in the "plugins" folder
        }

        // Plugin variables
        // - You must use PluginDataPath (folder) to store the plugin data, you need to manage only the folder content, the folder is created automatically by the main program
        public static string PluginDataPath => Path.Combine((string)AppDomain.CurrentDomain.GetData("PluginFolderPath"), "plugins_data", pluginInfo.ID);
    }

    public class PluginInfo : IPluginInfo
    {
        // Information of your plugin (IPuginInfo)
        // - For the ID, don't use spaces or special characters and start with "WDC." (The main program will use this ID to identify the plugin)
        // - For the Icon, you need to use a square image (recommended 1024x1024 pixels) and with transparent background (recommended PNG format)
        // Remember to change these values, the icon and the namespace ("WinDeskClockPlugin" in Plugin.cs,Main.xaml(.cs),Settings.xaml(.cs)) and the AssemblyName ("WDC.WinDeskClockPlugin" in Project file)
        // - For the namespace and AssemblyName, use the same as the Plugin ID
        // Remember also to change the Company ("STY Inc. (STY1001)" in Project file), the Copyright ("STY101" in Project file) and the Description ("A plugin for WinDeskClock" in Project file)
        // - For the Company and the Copyright, use the same as the Plugin Author
        // - For the Description, use the same as the Plugin Description
        // You can add some URL for the AuthorURL, the UpdateURL, the ProjectWebsiteURL and the ProjectSourceURL
        // Theses URLs are optional, if you don't want to use them, just set "none" (as string, not null object)
        // - For the AuthorURL, you can use the URL of your website or your GitHub profile for example
        // - For the UpdateURL, the URL need to return a raw json that contains specific fields (needed for the main program to check and download updates). For more information, see the example file (update.json) in the plugin template
        // - For the ProjectWebsiteURL, you can use the URL of your project website
        // - For the ProjectSourceURL, you can use the URL of your project source code repo (GitHub, GitLab, etc.)
        // You need to use the Version and VersionCode to manage the plugin version, the Version is the version name (like "1.0") and the VersionCode is the version code (like 100)
        // - The VersionCode is used to compare the versions, if the VersionCode is higher than the current version code, it's considered as an update available

        public string ID => "WDC.PluginTemplate";
        public string Name => "Plugin Template";
        public string Description => "A plugins template to make plugin for WinDeskClock";
        public string Author => "STY1001";
        public string AuthorURL => "https://sty1001.com";
        public string Version => "1.0";
        public int VersionCode => 100;
        public string UpdateURL => "none";
        public BitmapImage Icon => new BitmapImage(new Uri($"pack://application:,,,/{GetType().Assembly.GetName().Name};component/Resources/icon.png", UriKind.Absolute));
        public string ProjectWebsiteURL => "none";
        public string ProjectSourceURL => "https://github.com/STY1001/WinDeskClock";
    }

    public class PluginModule : IPluginModule
    {
        // Don't touch this, this is for the Plugin Interface (IPluginModule)
        Main _main = new Main();
        Settings _settings = new Settings();
        public Page GetMain() => _main;
        public Page GetSettings() =>  _settings;
        public async Task SaveConfig() => await _settings.SaveConfig();
        public async Task OnEvent(string eventName, object? data = null)
        {
            // Handle events here if needed
            // - You can use the eventName to identify the event and the data to get the data passed with the event
            // See the documentation for more information about the events
            await Task.CompletedTask;
        }
    }
}
