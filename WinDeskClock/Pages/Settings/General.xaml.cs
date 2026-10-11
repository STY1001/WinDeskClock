using System.Windows;
using System.Windows.Controls;
using WinDeskClock.Services;
using WinDeskClock.ViewModels;

namespace WinDeskClock.Pages.Settings
{
    /// <summary>
    /// The General settings tab. Every field's *value* is bound to GeneralSettingsViewModel in
    /// XAML; this code-behind only applies the (static, never-changing-at-runtime) section and
    /// field labels, and forwards the two "choose a file" buttons to the ViewModel.
    /// </summary>
    public partial class General : Page
    {
        private readonly GeneralSettingsViewModel _viewModel;
        private readonly ILocalizationService _localization;

        public General(GeneralSettingsViewModel viewModel, ILocalizationService localization)
        {
            InitializeComponent();
            _viewModel = viewModel;
            _localization = localization;
            DataContext = viewModel;

            ApplyStaticTexts();
            Loaded += async (_, _) => await viewModel.LoadAsync();
        }

        private void ApplyStaticTexts()
        {
            var loc = _localization;

            GeneralText.Text = loc.GetString("settings.general.titlename");
            LangTitle.Text = loc.GetString("settings.general.lang.name");
            VolumeTitle.Text = loc.GetString("settings.general.volume.name");
            BlurEffectTitle.Text = loc.GetString("settings.general.blur.name");
            ScreenOnOffTitle.Text = loc.GetString("settings.general.screenonoff.name");
            ScreenOnOffNone.Content = loc.GetString("settings.general.screenonoff.type.none");
            ScreenOnOffFade.Content = loc.GetString("settings.general.screenonoff.type.fade");
            ScreenOnOffCRT.Content = loc.GetString("settings.general.screenonoff.type.crt");
            ScreenWakeUpTitle.Text = loc.GetString("settings.general.screenwakeup.name");
            MenuAutoCloseTitle.Text = loc.GetString("settings.general.menuautoclose.name");

            ClockText.Text = loc.GetString("settings.general.clock.titlename");
            ShowSecondsTitle.Text = loc.GetString("settings.general.clock.showsec.name");
            ShowSecondsDesc.Text = loc.GetString("settings.general.clock.showsec.desc");
            FbxStyleTitle.Text = loc.GetString("settings.general.clock.fbxstyle.name");
            FbxStyleDesc.Text = loc.GetString("settings.general.clock.fbxstyle.desc");

            AlarmText.Text = loc.GetString("settings.general.alarm.titlename");
            DefaultAlarmSoundTitle.Text = loc.GetString("settings.general.alarm.defaultsound.name");
            DefaultAlarmSoundBtn.Content = loc.GetString("settings.general.alarm.defaultsound.choose");
            AlarmTimeoutTitle.Text = loc.GetString("settings.general.alarm.timeout.name");
            AlarmTimeoutDesc.Text = loc.GetString("settings.general.alarm.timeout.desc");

            TimerText.Text = loc.GetString("settings.general.timer.titlename");
            TimerSoundText.Text = loc.GetString("settings.general.timer.defaultsound.name");
            TimerSoundBtn.Content = loc.GetString("settings.general.timer.defaultsound.choose");

            PluginText.Text = loc.GetString("settings.general.plugin.titlename");
            CarouselSelectTitle.Text = loc.GetString("settings.general.plugin.pinned.name");
            CarouselSelectDesc.Text = loc.GetString("settings.general.plugin.pinned.desc");
            CarouselDelayTitle.Text = loc.GetString("settings.general.plugin.carouseldelay.name");
            CarouselDelayDesc.Text = loc.GetString("settings.general.plugin.carouseldelay.desc");
        }

        private void DefaultAlarmSoundBtn_Click(object sender, RoutedEventArgs e) => _viewModel.BrowseDefaultAlarmSound();
        private void TimerSoundBtn_Click(object sender, RoutedEventArgs e) => _viewModel.BrowseTimerSound();
    }
}
