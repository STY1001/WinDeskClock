using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace WinDeskClock.Interfaces
{
    public interface IPlugin
    {
        // Plugin interface to retrieve plugin information and Pages

        // Plugin information
        string Name { get; }  // Name of the plugin
        string ID { get; }  // Unique ID of the plugin
        string Description { get; }  // Description of the plugin
        string Author { get; }  // Author of the plugin
        string AuthorURL { get; }  // Author's URL
        string Version { get; }  // Version of the plugin
        string VersionUpdateCheckURL { get; }  // URL to check for updates
        string VersionUpdateDownloadURL { get; }  // URL to download the latest version
        BitmapImage Icon { get; }  // Icon of the plugin
        string ProjectWebsiteURL { get; }  // URL to the project
        string ProjectSourceURL { get; }  // URL to the project source code

        // Pages of the plugin
        Page GetMain();  // Main page of the plugin
        Page GetSettings();  // Settings page of the plugin
        Task SaveConfig();  // Save configuration of the plugin
    }
}
