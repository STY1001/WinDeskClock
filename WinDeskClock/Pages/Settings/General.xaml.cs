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
using Microsoft.Win32;

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

            ShowSecondsToggleSwitch.IsChecked = ConfigManager.Variables.ClockShowSecond;
            if (ConfigManager.Variables.ClockFbxStyle)
            {
                ShowSecondsToggleSwitch.IsEnabled = false;
                ShowSecondsToggleSwitch.IsChecked = false;
            }
            ShowSecondsToggleSwitch.Checked += async (s, e) =>
            {
                ConfigManager.NewVariables.RestartNeeded = true;
                ConfigManager.NewVariables.ClockShowSecond = true;
            };
            ShowSecondsToggleSwitch.Unchecked += async (s, e) =>
            {
                ConfigManager.NewVariables.RestartNeeded = true;
                ConfigManager.NewVariables.ClockShowSecond = false;
            };

            FbxStyleToggleSwitch.IsChecked = ConfigManager.Variables.ClockFbxStyle;
            FbxStyleToggleSwitch.Checked += async (s, e) =>
            {
                ConfigManager.NewVariables.RestartNeeded = true;
                ConfigManager.NewVariables.ClockFbxStyle = true;
                ShowSecondsToggleSwitch.IsChecked = false;
                ShowSecondsToggleSwitch.IsEnabled = false;
            };
            FbxStyleToggleSwitch.Unchecked += async (s, e) =>
            {
                ConfigManager.NewVariables.RestartNeeded = true;
                ConfigManager.NewVariables.ClockFbxStyle = false;
                ShowSecondsToggleSwitch.IsEnabled = true;
                ShowSecondsToggleSwitch.IsChecked = true;
            };

            DefaultAlarmSoundDesc.Text = LangSystem.GetLang("settings.general.alarm.defaultsound.desc", ConfigManager.Variables.DefaultAlarmSound).Result;
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
                    ConfigManager.NewVariables.RestartNeeded = true;
                    ConfigManager.NewVariables.DefaultAlarmSound = ofd.FileName;
                    DefaultAlarmSoundDesc.Text = await LangSystem.GetLang("settings.general.alarm.defaultsound.desc", ofd.FileName);
                }
            };

            AlarmTimeoutSlider.Value = ConfigManager.Variables.AlarmTimeoutDelay;
            if (AlarmTimeoutSlider.Value == 0)
            {
                AlarmTimeoutText.Text = LangSystem.GetLang("settings.general.alarm.timeout.never").Result;
            }
            else if (AlarmTimeoutSlider.Value == 1)
            {
                AlarmTimeoutText.Text = LangSystem.GetLang("settings.general.alarm.timeout.min", AlarmTimeoutSlider.Value.ToString("0")).Result;
            }
            else
            {
                AlarmTimeoutText.Text = LangSystem.GetLang("settings.general.alarm.timeout.mins", AlarmTimeoutSlider.Value.ToString("0")).Result;
            }
            AlarmTimeoutSlider.ValueChanged += async (s, e) =>
            {
                ConfigManager.NewVariables.RestartNeeded = true;
                ConfigManager.NewVariables.AlarmTimeoutDelay = (int)AlarmTimeoutSlider.Value;
                if (AlarmTimeoutSlider.Value == 0)
                {
                    AlarmTimeoutText.Text = await LangSystem.GetLang("settings.general.alarm.timeout.never");
                }
                else if (AlarmTimeoutSlider.Value == 1)
                {
                    AlarmTimeoutText.Text = await LangSystem.GetLang("settings.general.alarm.timeout.min", AlarmTimeoutSlider.Value.ToString("0"));
                }
                else
                {
                    AlarmTimeoutText.Text = await LangSystem.GetLang("settings.general.alarm.timeout.mins", AlarmTimeoutSlider.Value.ToString("0"));
                }
            };

            TimerSoundDesc.Text= LangSystem.GetLang("settings.general.timer.defaultsound.desc", ConfigManager.Variables.DefaultTimeUpSound).Result;
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
                    ConfigManager.NewVariables.RestartNeeded = true;
                    ConfigManager.NewVariables.DefaultTimeUpSound = ofd.FileName;
                    TimerSoundDesc.Text = await LangSystem.GetLang("settings.general.timer.defaultsound.desc", ofd.FileName);
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

            CarouselDelaySlider.Value = ConfigManager.Variables.CarouselDelay;
            CarouselDelayText.Text = LangSystem.GetLang("settings.general.plugin.carouseldelay.secs", CarouselDelaySlider.Value.ToString("0")).Result;
            CarouselDelaySlider.ValueChanged += async (s, e) =>
            {
                ConfigManager.NewVariables.RestartNeeded = true;
                ConfigManager.NewVariables.CarouselDelay = (int)CarouselDelaySlider.Value;
                CarouselDelayText.Text = await LangSystem.GetLang("settings.general.plugin.carouseldelay.secs", CarouselDelaySlider.Value.ToString("0"));
            };

            // Load settings
            Loaded += async (s, e) => await Load();
        }

        private bool Init = false;
        private async Task Load()
        {
            if (!Init)
            {
                GeneralText.Text = await LangSystem.GetLang("settings.general.titlename");
                ClockText.Text = await LangSystem.GetLang("settings.general.clock.titlename");
                AlarmText.Text = await LangSystem.GetLang("settings.general.alarm.titlename");
                TimerText.Text = await LangSystem.GetLang("settings.general.timer.titlename");
                PluginText.Text = await LangSystem.GetLang("settings.general.plugin.titlename");

                LangTitle.Text = await LangSystem.GetLang("settings.general.lang.name");
                VolumeTitle.Text = await LangSystem.GetLang("settings.general.volume.name");

                ShowSecondsTitle.Text = await LangSystem.GetLang("settings.general.clock.showsec.name");
                ShowSecondsDesc.Text = await LangSystem.GetLang("settings.general.clock.showsec.desc");
                FbxStyleTitle.Text = await LangSystem.GetLang("settings.general.clock.fbxstyle.name");
                FbxStyleDesc.Text = await LangSystem.GetLang("settings.general.clock.fbxstyle.desc");

                DefaultAlarmSoundTitle.Text = await LangSystem.GetLang("settings.general.alarm.defaultsound.name");
                DefaultAlarmSoundDesc.Text = await LangSystem.GetLang("settings.general.alarm.defaultsound.desc", ConfigManager.Variables.DefaultAlarmSound);
                DefaultAlarmSoundBtn.Content = await LangSystem.GetLang("settings.general.alarm.defaultsound.choose");
                AlarmTimeoutTitle.Text = await LangSystem.GetLang("settings.general.alarm.timeout.name");
                AlarmTimeoutDesc.Text = await LangSystem.GetLang("settings.general.alarm.timeout.desc");

                TimerSoundText.Text = await LangSystem.GetLang("settings.general.timer.defaultsound.name");
                TimerSoundDesc.Text = await LangSystem.GetLang("settings.general.timer.defaultsound.desc", ConfigManager.Variables.DefaultTimeUpSound);
                TimerSoundBtn.Content = await LangSystem.GetLang("settings.general.timer.defaultsound.choose");

                CarouselSelectTitle.Text = await LangSystem.GetLang("settings.general.plugin.pinned.name");
                CarouselSelectDesc.Text = await LangSystem.GetLang("settings.general.plugin.pinned.desc");

                CarouselDelayTitle.Text = await LangSystem.GetLang("settings.general.plugin.carouseldelay.name");
                CarouselDelayDesc.Text = await LangSystem.GetLang("settings.general.plugin.carouseldelay.desc");


                Init = true;
            }

            // Load language
            LangComboBox.Items.Clear();
            foreach (var lang in LangSystem.LangList)
            {
                ComboBoxItem item = new ComboBoxItem();
                item.Content = lang.Value;
                item.Selected += async (s, e) =>
                {
                    ConfigManager.NewVariables.RestartNeeded = true;
                    ConfigManager.NewVariables.Language = lang.Key;
                };
                LangComboBox.Items.Add(item);
            }
            LangComboBox.Text = LangSystem.LangList[ConfigManager.Variables.Language];

            ShowSecondsToggleSwitch.IsChecked = ConfigManager.Variables.ClockShowSecond;
            FbxStyleToggleSwitch.IsChecked = ConfigManager.Variables.ClockFbxStyle;
            DefaultAlarmSoundDesc.Text = await LangSystem.GetLang("settings.general.alarm.defaultsound.desc", ConfigManager.Variables.DefaultAlarmSound);
            AlarmTimeoutSlider.Value = ConfigManager.Variables.AlarmTimeoutDelay;
            if (AlarmTimeoutSlider.Value == 0)
            {
                AlarmTimeoutText.Text = await LangSystem.GetLang("settings.general.alarm.timeout.never");
            }
            else if (AlarmTimeoutSlider.Value == 1)
            {
                AlarmTimeoutText.Text = await LangSystem.GetLang("settings.general.alarm.timeout.min", AlarmTimeoutSlider.Value.ToString("0"));
            }
            else
            {
                AlarmTimeoutText.Text = await LangSystem.GetLang("settings.general.alarm.timeout.mins", AlarmTimeoutSlider.Value.ToString("0"));
            }
            TimerSoundDesc.Text = await LangSystem.GetLang("settings.general.timer.defaultsound.desc", ConfigManager.Variables.DefaultTimeUpSound);

            MMDeviceEnumerator enumerator = new MMDeviceEnumerator();
            MMDevice device = enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Console);
            VolumeSlider.Value = (int)(device.AudioEndpointVolume.MasterVolumeLevelScalar * 100);
            VolumeText.Text = VolumeSlider.Value.ToString() + "%";

            CarouselSelectStack.Children.Clear();
            foreach (string id in PluginLoader.PluginModules.Keys)
            {
                CheckBox checkBox = new CheckBox();
                checkBox.Content = PluginLoader.PluginInfos[id].Name;
                checkBox.Checked += async (s, e) =>
                {
                    await ConfigManager.AddPinnedPlugin(id);
                };
                checkBox.Unchecked += async (s, e) =>
                {
                    await ConfigManager.DelPinnedPlugin(id);
                };
                checkBox.IsChecked = await ConfigManager.CheckPinnedPlugin(id);
                CarouselSelectStack.Children.Add(checkBox);
            }

            if (CarouselSelectStack.Children.Count == 0)
            {
                TextBlock textBlock = new TextBlock();
                textBlock.Text = await LangSystem.GetLang("settings.general.plugin.pinned.noplugin");
                textBlock.Foreground = Brushes.Gray;
                CarouselSelectStack.Children.Add(textBlock);
            }

            CarouselDelaySlider.Value = ConfigManager.Variables.CarouselDelay;
            CarouselDelayText.Text = await LangSystem.GetLang("settings.general.plugin.carouseldelay.secs", CarouselDelaySlider.Value.ToString("0"));
        }
    }
}
