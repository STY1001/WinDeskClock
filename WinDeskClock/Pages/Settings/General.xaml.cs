using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using WinDeskClock.Utils;

namespace WinDeskClock.Pages.Settings
{
    /// <summary>
    /// Interaction logic for General.xaml
    /// </summary>
    public partial class General : Page
    {
        public General()
        {
            InitializeComponent();

            // Load settings
            Loaded += async (s, e) => await Load();
        }

        private async Task Load()
        {
            // Load language
            LangComboBox.Items.Clear();
            foreach (var lang in LangSystem.LangList)
            {
                ComboBoxItem item = new ComboBoxItem();
                item.Content = lang.Value;
                item.Selected += async (s, e) =>
                {
                    ConfigManager.NewVariable.RestartNeeded = true;
                    ConfigManager.NewVariable.Language = lang.Key;
                };
                LangComboBox.Items.Add(item);
            }
            LangComboBox.Text = LangSystem.LangList[ConfigManager.Variable.Language];

            ShowSecondsToggleSwitch.IsChecked = ConfigManager.Variable.ClockShowSecond;
            if (ConfigManager.Variable.ClockFbxStyle)
            {
                ShowSecondsToggleSwitch.IsEnabled = false;
                ShowSecondsToggleSwitch.IsChecked = false;
            }   
            ShowSecondsToggleSwitch.Checked += async (s, e) =>
            {
                ConfigManager.NewVariable.RestartNeeded = true;
                ConfigManager.NewVariable.ClockShowSecond = true;
            };
            ShowSecondsToggleSwitch.Unchecked += async (s, e) =>
            {
                ConfigManager.NewVariable.RestartNeeded = true;
                ConfigManager.NewVariable.ClockShowSecond = false;
            };

            FbxStyleToggleSwitch.IsChecked = ConfigManager.Variable.ClockFbxStyle;
            FbxStyleToggleSwitch.Checked += async (s, e) =>
            {
                ConfigManager.NewVariable.RestartNeeded = true;
                ConfigManager.NewVariable.ClockFbxStyle = true;
                ShowSecondsToggleSwitch.IsChecked = false;
                ShowSecondsToggleSwitch.IsEnabled = false;
            };
            FbxStyleToggleSwitch.Unchecked += async (s, e) =>
            {
                ConfigManager.NewVariable.RestartNeeded = true;
                ConfigManager.NewVariable.ClockFbxStyle = false;
                ShowSecondsToggleSwitch.IsEnabled = true;
                ShowSecondsToggleSwitch.IsChecked = true;
            };
        }
    }
}
