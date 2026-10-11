using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;
using WinDeskClock.ViewModels;
using WinDeskClock.Views.Animation;

namespace WinDeskClock.Pages.Settings
{
    /// <summary>
    /// The Plugin Manager settings tab: a list of plugins (PluginCardView, data-templated from
    /// PluginManagerViewModel.Plugins) plus an overlay for editing one plugin's own settings page.
    /// Disabling the rest of the Settings dialog while that overlay is open is handled by bindings
    /// in MainWindow.xaml (to Settings.IsPluginSettingsOpen); this class only plays the overlay's
    /// show/hide animation and forwards Save/Cancel.
    /// </summary>
    public partial class PluginManager : Page
    {
        private readonly PluginManagerViewModel _viewModel;
        private const double AnimSpeed = 0.25;

        public PluginManager(PluginManagerViewModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel;
            DataContext = viewModel;

            viewModel.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName == nameof(PluginManagerViewModel.IsPluginSettingsOpen))
                {
                    OnPluginSettingsOpenChanged(viewModel.IsPluginSettingsOpen);
                }
                else if (e.PropertyName == nameof(PluginManagerViewModel.CurrentPluginSettingsPage))
                {
                    PluginSettingsFrame.Content = viewModel.CurrentPluginSettingsPage;
                }
            };

            Loaded += async (_, _) => await viewModel.LoadAsync();
        }

        private async void OnPluginSettingsOpenChanged(bool isOpen)
        {
            var easeIn = new QuadraticEase { EasingMode = EasingMode.EaseIn };
            var easeOut = new QuadraticEase { EasingMode = EasingMode.EaseOut };

            if (isOpen)
            {
                PluginSettingsGrid.Visibility = Visibility.Visible;
                UiAnimator.Fade(PluginSettingsGrid, 0, 1, AnimSpeed, easeIn);
                UiAnimator.Scale(PluginSettingsBorder, 0.9, 1, AnimSpeed, easeOut);
            }
            else
            {
                UiAnimator.Fade(PluginSettingsGrid, 1, 0, AnimSpeed, easeOut, beginTimeSeconds: 0.05);
                UiAnimator.Scale(PluginSettingsBorder, 1, 0.9, AnimSpeed, easeIn, beginTimeSeconds: 0.05);
                await Task.Delay(TimeSpan.FromSeconds(AnimSpeed + 0.05));
                PluginSettingsGrid.Visibility = Visibility.Hidden;
            }
        }

        private void PluginSettingsCancelBtn_Click(object sender, RoutedEventArgs e) => _viewModel.CancelPluginSettings();
        private async void PluginSettingsSaveBtn_Click(object sender, RoutedEventArgs e) => await _viewModel.SavePluginSettingsAsync();
    }
}
