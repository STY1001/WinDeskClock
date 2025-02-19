using System.IO;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using WinDeskClock.Interfaces;
using WinDeskClock.Utils;

namespace WinDeskClockPlugin
{
    public class Plugin : IPlugin
    {
        // Information of your plugin
        // - For the ID, don't use spaces or special characters and start with "WDC." (The main program will use this ID to identify the plugin)
        // - For the Icon, you need to use a square image (recommended 1024x1024 pixels) and with transparent background (recommended PNG format)
        // Remember to change these values, the icon and the namespace (WinDeskClockPlugin (in Plugin.cs,Main.xaml(.cs),Settings.xaml(.cs)) and the AssemblyName (WDC.WinDeskClockPlugin (in Project file))
        // - For the namespace and AssemblyName, use the same as the Plugin ID
        // Remember also to change the Company (STY Inc. (STY1001) (in Project file)), the Copyright (STY101 (in Project file)) and the Description (A plugin for WinDeskClock (in Project file))
        // - For the Company and the Copyright, use the same as the Plugin Author
        // - For the Description, use the same as the Plugin Description
        // You can add some URL for the AuthorURL, the VersionUpdateCheckURL, the VersionUpdateDownloadURL, the ProjectWebsiteURL and the ProjectSourceURL
        // Theses URLs are optional, if you don't want to use them, just set "none" (as string, not null object), VersionUpdateCheckURL and VersionUpdateDownloadURL are paired, if you set one, you need to set the other
        // - For the AuthorURL, you can use the URL of your website or your GitHub profile for example
        // - For the VersionUpdateCheckURL, the URL need to return just a raw text of the latest version of your plugin (the main program will compare the version with the Version variable)
        // - For the VersionUpdateDownloadURL, the URL need to return the download link of the latest version of your plugin as a raw text
        // - For the ProjectWebsiteURL, you can use the URL of your project website
        // - For the ProjectSourceURL, you can use the URL of your project source code repo (GitHub, GitLab, etc.)

        public string ID => "WDC.PluginTemplate";
        public string Name => "Plugin Template";
        public string Description => "A plugins template to make plugin for WinDeskClock";
        public string Author => "STY1001";
        public string AuthorURL => "https://sty1001.com";
        public string Version => "1.0";
        public string VersionUpdateCheckURL => "none";
        public string VersionUpdateDownloadURL => "none";
        public BitmapImage Icon => new BitmapImage(new Uri($"pack://application:,,,/{GetType().Assembly.GetName().Name};component/Resources/icon.png", UriKind.Absolute));
        public string ProjectWebsiteURL => "none";
        public string ProjectSourceURL => "https://github.com/STY1001/WinDeskClock";

        // Plugin variables
        // - You must use PluginDataPath (folder) to store the plugin data, you need to manage only the folder content, the folder is created automatically by the main program
        public readonly string PluginDataPath;
        // - You need to use Language for your plugin language system, this variable is the same as the main program language. If your plugin don't have the language that set in the main program, you need to use the default language (en-us)
        public readonly string Language;

        // Don't touch this, this is for the Plugin variables
        public Plugin()
        {
            // Plugin pages (Don't touch this)
            _main = new Main();
            _settings = new Settings();

            // Plugin data path
            PluginDataPath = Path.Combine((string)AppDomain.CurrentDomain.GetData("PluginFolderPath"), "plugins_data", ID);
            // Plugin language
            Language = ConfigManager.Variable.Language;
        }

        // Don't touch this, this is for the Plugin Interface
        Main _main;
        Settings _settings;
        public Page GetMain()
        {
            return _main;
        }
        public Page GetSettings()
        {
            return _settings;
        }
        public async Task SaveConfig()
        {
            await _settings.SaveConfig();
        }
    }
}
