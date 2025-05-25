using System.Windows.Controls;

namespace WinDeskClockPlugin
{
    /// <summary>
    /// Interaction logic for Main.xaml
    /// </summary>
    public partial class Settings : Page
    {
        public Settings()
        {
            InitializeComponent();
            Loaded += async (s, e) => await Load();
        }

        private bool Init = false;
        private async Task Load()
        {
            if (!Init)
            {
                // This part is called only once (equivalent to Initialized event)
                await LoadLang();
                Init = true;
            }
            await LoadConfig();
        }

        public async Task LoadLang()
        {
            // Place your code here to apply the language to the controls in the main page
            // - You need to use IPluginAppStatus.Language for your plugin language system, this variable is the same as the main program language. If your plugin don't have the language that set in the main program, you need to use the default language (en-us)
            // - You can get the language from the main program with this variable:
            // IPluginAppStatus.Language;
        }

        public async Task LoadConfig()
        {
            // Place your code here to load the configuration, and set the values to the controls in the settings page
        }
        public async Task SaveConfig()
        {
            // Place your code here to save the configuration
            // - You don't need to create "Save" button, the main program contains a button to save the configuration and it will call this method

            // You need to save the configuration in the PluginDataPath folder
            // - You can get the variable with:
            // Plugin.PluginDataPath;
        }
    }
}
