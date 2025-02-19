using System.IO;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace WinDeskClockPlugin
{
    /// <summary>
    /// Interaction logic for Main.xaml
    /// </summary>
    public partial class Main : Page
    {
        public Main()
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
            // - You can get the language from the main program with:
            //   Plugin plugin = new Plugin();
            //   plugin.Language;
        }

        public async Task LoadConfig()
        {
            // Place your code here to load the configuration, and set the values to the controls in the main page
        }
    }
}
