using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
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
using NAudio.CoreAudioApi;

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

            DefaultAlarmSoundBtn.Content = ConfigManager.Variable.DefaultAlarmSound;
            DefaultAlarmSoundBtn.Click += async (s, e) =>
            {
                var ofd = new Microsoft.Win32.OpenFileDialog();
                ofd.DefaultExt = ".wav";
                ofd.Filter = "WAV Audio File (*.wav)|*.wav";
                ofd.FilterIndex = 1;
                ofd.Title = "WinDeskClock Default Alarm Sound";
                var result = ofd.ShowDialog();
                if (result == true)
                {
                    ConfigManager.NewVariable.RestartNeeded = true;
                    ConfigManager.NewVariable.DefaultAlarmSound = ofd.FileName;
                    DefaultAlarmSoundBtn.Content = ofd.FileName;
                }
            };

            AlarmTimeoutSlider.Value = double.Parse(ConfigManager.Variable.AlarmTimeoutDelay);
            if (AlarmTimeoutSlider.Value == 0)
            {
                AlarmTimeoutText.Text = "Never";
            }
            else
            {
                AlarmTimeoutText.Text = AlarmTimeoutSlider.Value.ToString("0") + " minute(s)";
            }
            AlarmTimeoutSlider.ValueChanged += async (s, e) =>
            {
                ConfigManager.NewVariable.RestartNeeded = true;
                ConfigManager.NewVariable.AlarmTimeoutDelay = AlarmTimeoutSlider.Value.ToString("0");
                if (AlarmTimeoutSlider.Value == 0)
                {
                    AlarmTimeoutText.Text = "Never";
                }
                else
                {
                    AlarmTimeoutText.Text = AlarmTimeoutSlider.Value.ToString("0") + " minute(s)";
                }
            };

            TimerSoundBtn.Content = ConfigManager.Variable.DefaultTimeUpSound;
            TimerSoundBtn.Click += async (s, e) =>
            {
                var ofd = new Microsoft.Win32.OpenFileDialog();
                ofd.DefaultExt = ".wav";
                ofd.Filter = "WAV Audio File (*.wav)|*.wav";
                ofd.FilterIndex = 1;
                ofd.Title = "WinDeskClock Timer Sound";
                var result = ofd.ShowDialog();
                if (result == true)
                {
                    ConfigManager.NewVariable.RestartNeeded = true;
                    ConfigManager.NewVariable.DefaultTimeUpSound = ofd.FileName;
                    TimerSoundBtn.Content = ofd.FileName;
                }
            };

            MMDeviceEnumerator enumerator = new MMDeviceEnumerator();
            MMDevice device = enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Console);
            VolumeSlider.Value = (int)(device.AudioEndpointVolume.MasterVolumeLevelScalar * 100);
            VolumeText.Text = VolumeSlider.Value.ToString() + "%";
            VolumeSlider.ValueChanged += async (s, e) =>
            {
                device.AudioEndpointVolume.MasterVolumeLevelScalar = (float)VolumeSlider.Value / 100;
                VolumeText.Text = VolumeSlider.Value.ToString("0") + "%";
            };


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
            FbxStyleToggleSwitch.IsChecked = ConfigManager.Variable.ClockFbxStyle;
            DefaultAlarmSoundBtn.Content = ConfigManager.Variable.DefaultAlarmSound;
            AlarmTimeoutSlider.Value = double.Parse(ConfigManager.Variable.AlarmTimeoutDelay);
            if (AlarmTimeoutSlider.Value == 0)
            {
                AlarmTimeoutText.Text = "Never";
            }
            else
            {
                AlarmTimeoutText.Text = AlarmTimeoutSlider.Value.ToString("0") + " minute(s)";
            }
            TimerSoundBtn.Content = ConfigManager.Variable.DefaultTimeUpSound;

            MMDeviceEnumerator enumerator = new MMDeviceEnumerator();
            MMDevice device = enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Console);
            VolumeSlider.Value = (int)(device.AudioEndpointVolume.MasterVolumeLevelScalar * 100);
            VolumeText.Text = VolumeSlider.Value.ToString() + "%";
        }
    }
}
