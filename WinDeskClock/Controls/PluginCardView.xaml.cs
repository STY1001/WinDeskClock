using System.Windows;
using System.Windows.Controls;
using WinDeskClock.ViewModels;

namespace WinDeskClock.Controls
{
    /// <summary>
    /// One plugin row in the Plugin Manager. Replaces the original's imperative CreatePluginCard.
    /// Used as an ItemsControl DataTemplate, so DataContext is set automatically by WPF to the
    /// corresponding PluginCardViewModel - no manual wiring needed, unlike AlarmCardView (which
    /// is built by hand because it needs the shrink/grow/reorder choreography an ItemsControl
    /// doesn't give us for free).
    /// </summary>
    public partial class PluginCardView : UserControl
    {
        private PluginCardViewModel ViewModel => (PluginCardViewModel)DataContext;

        public PluginCardView()
        {
            InitializeComponent();
        }

        private void WebsiteBtn_Click(object sender, RoutedEventArgs e) => ViewModel.OpenWebsite();
        private void SourceBtn_Click(object sender, RoutedEventArgs e) => ViewModel.OpenSource();
        private async void EnabledToggle_Click(object sender, RoutedEventArgs e) => await ViewModel.ToggleEnabledAsync();
        private async void UpdateBtn_Click(object sender, RoutedEventArgs e) => await ViewModel.UpdateAsync();
        private void SettingsBtn_Click(object sender, RoutedEventArgs e) => ViewModel.RequestOpenSettings();
        private void DeleteBtn_Click(object sender, RoutedEventArgs e) => ViewModel.RequestDelete();
    }
}
