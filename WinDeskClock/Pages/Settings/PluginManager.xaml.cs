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
using Wpf.Ui.Controls;
using WinDeskClock.Utils;
using System.Diagnostics;
using System.Windows.Controls.Primitives;
using System.IO;
using System.Windows.Media.Animation;

namespace WinDeskClock.Pages.Settings
{
    /// <summary>
    /// Interaction logic for PluginManager.xaml
    /// </summary>
    public partial class PluginManager : Page
    {
        public PluginManager()
        {
            InitializeComponent();
            Loaded += async (s, e) => await Load();
        }

        private async Task Load()
        {
            PluginCardStack.Children.Clear();
            foreach (var id in PluginLoader.Plugins.Keys)
            {
                Debug.WriteLine(id);
                await CreatePluginCard(id);
            }
        }

        private double animzoomspeeds = 0.25;
        private async Task ShowPluginSettings(string id)
        {
            var mainWindow = (MainWindow)Application.Current.MainWindow;
            mainWindow.SettingsSaveBtn.IsEnabled = false;
            mainWindow.SettingsBackBtn.IsEnabled = false;
            mainWindow.GeneralTabItem.IsEnabled = false;
            mainWindow.AboutTabItem.IsEnabled = false;
            PluginSettingsGrid.Visibility = Visibility.Visible;
            {
                var fadeAnimation = new DoubleAnimation
                {
                    From = 0,
                    To = 1,
                    Duration = TimeSpan.FromSeconds(animzoomspeeds),
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn }
                };
                var sizeAnimation1 = new DoubleAnimation
                {
                    From = 0.9,
                    To = 1,
                    Duration = TimeSpan.FromSeconds(animzoomspeeds),
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                };
                var sizeAnimation2 = new DoubleAnimation
                {
                    From = 0.9,
                    To = 1,
                    Duration = TimeSpan.FromSeconds(animzoomspeeds),
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                };
                Storyboard.SetTarget(fadeAnimation, PluginSettingsGrid);
                Storyboard.SetTargetProperty(fadeAnimation, new PropertyPath(OpacityProperty));
                Storyboard.SetTarget(sizeAnimation1, PluginSettingsBorder);
                Storyboard.SetTargetProperty(sizeAnimation1, new PropertyPath("(UIElement.RenderTransform).(ScaleTransform.ScaleX)"));
                Storyboard.SetTarget(sizeAnimation2, PluginSettingsBorder);
                Storyboard.SetTargetProperty(sizeAnimation2, new PropertyPath("(UIElement.RenderTransform).(ScaleTransform.ScaleY)"));
                var storyboard = new Storyboard();
                storyboard.Children.Add(fadeAnimation);
                storyboard.Children.Add(sizeAnimation1);
                storyboard.Children.Add(sizeAnimation2);
                storyboard.Begin();
            }
            SettingsPluginID = id;
            PluginSettingsFrame.Content = PluginLoader.Plugins[id].GetSettings();
        }

        private async Task HidePluginSettings()
        {
            var mainWindow = (MainWindow)Application.Current.MainWindow;
            mainWindow.SettingsSaveBtn.IsEnabled = true;
            mainWindow.SettingsBackBtn.IsEnabled = true;
            mainWindow.GeneralTabItem.IsEnabled = true;
            mainWindow.AboutTabItem.IsEnabled = true;
            {
                var fadeAnimation = new DoubleAnimation
                {
                    From = 1,
                    To = 0,
                    Duration = TimeSpan.FromSeconds(animzoomspeeds),
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut },
                    BeginTime = TimeSpan.FromSeconds(0.05)
                };
                var sizeAnimation1 = new DoubleAnimation
                {
                    From = 1,
                    To = 0.9,
                    Duration = TimeSpan.FromSeconds(animzoomspeeds),
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn }
                };
                var sizeAnimation2 = new DoubleAnimation
                {
                    From = 1,
                    To = 0.9,
                    Duration = TimeSpan.FromSeconds(animzoomspeeds),
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn }
                };
                Storyboard.SetTarget(fadeAnimation, PluginSettingsGrid);
                Storyboard.SetTargetProperty(fadeAnimation, new PropertyPath(OpacityProperty));
                Storyboard.SetTarget(sizeAnimation1, PluginSettingsBorder);
                Storyboard.SetTargetProperty(sizeAnimation1, new PropertyPath("(UIElement.RenderTransform).(ScaleTransform.ScaleX)"));
                Storyboard.SetTarget(sizeAnimation2, PluginSettingsBorder);
                Storyboard.SetTargetProperty(sizeAnimation2, new PropertyPath("(UIElement.RenderTransform).(ScaleTransform.ScaleY)"));
                var storyboard = new Storyboard();
                storyboard.Children.Add(fadeAnimation);
                storyboard.Children.Add(sizeAnimation1);
                storyboard.Children.Add(sizeAnimation2);
                storyboard.Completed += (s, e) =>
                {
                    PluginSettingsGrid.Visibility = Visibility.Hidden;
                    PluginSettingsFrame.Content = null;
                    SettingsPluginID = "";
                };
                storyboard.Begin();
            }
        }

        private async Task CreatePluginCard(string id)
        {
            Card PluginCard = new Card();
            PluginCard.Margin = new Thickness(5);
            PluginCard.Tag = $"{id}_PluginCard";

            Grid PluginCardMainGrid = new Grid();
            PluginCardMainGrid.Height = 150;
            PluginCardMainGrid.Tag = $"{id}_PluginCardMainGrid";
            PluginCardMainGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(150) });
            PluginCardMainGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) });
            PluginCardMainGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Auto) });

            System.Windows.Controls.Image PluginIcon = new System.Windows.Controls.Image();
            PluginIcon.Source = PluginLoader.Plugins[id].Icon;
            PluginIcon.Width = 150;
            PluginIcon.Height = 150;
            PluginIcon.Margin = new Thickness(5, 0, 0, 0);
            PluginIcon.Tag = $"{id}_PluginIcon";
            PluginIcon.SetValue(RenderOptions.BitmapScalingModeProperty, BitmapScalingMode.HighQuality);

            Grid PluginCardInfoGrid = new Grid();
            PluginCardInfoGrid.SetValue(Grid.ColumnProperty, 1);
            PluginCardInfoGrid.Margin = new Thickness(25, 0, 20, 0);
            PluginCardInfoGrid.Tag = $"{id}_PluginCardInfoGrid";
            PluginCardInfoGrid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(1, GridUnitType.Auto) });
            PluginCardInfoGrid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(5) });
            PluginCardInfoGrid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(1, GridUnitType.Star) });
            PluginCardInfoGrid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(10) });
            PluginCardInfoGrid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(1, GridUnitType.Auto) });

            Grid PluginTitleGrid = new Grid();
            PluginTitleGrid.SetValue(Grid.RowProperty, 0);
            PluginTitleGrid.Tag = $"{id}_PluginTitleGrid";
            PluginTitleGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Auto) });
            PluginTitleGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(15) });
            PluginTitleGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Auto) });
            PluginTitleGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(15) });
            PluginTitleGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Auto) });
            PluginTitleGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(15) });
            PluginTitleGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Auto) });

            System.Windows.Controls.TextBlock PluginNameTextBlock = new System.Windows.Controls.TextBlock();
            PluginNameTextBlock.Text = PluginLoader.Plugins[id].Name;
            PluginNameTextBlock.FontSize = 18;
            PluginNameTextBlock.Foreground = (Brush)FindResource("TextFillColorPrimaryBrush");
            PluginNameTextBlock.FontFamily = new FontFamily("Segoe UI SemiBold");
            PluginNameTextBlock.SetValue(Grid.ColumnProperty, 0);
            PluginNameTextBlock.VerticalAlignment = VerticalAlignment.Center;
            PluginNameTextBlock.Tag = $"{id}_PluginNameTextBlock";

            System.Windows.Controls.TextBlock PluginSepTextBlock1 = new System.Windows.Controls.TextBlock();
            PluginSepTextBlock1.Text = "|";
            PluginSepTextBlock1.FontSize = 18;
            PluginSepTextBlock1.Foreground = (Brush)FindResource("TextFillColorPrimaryBrush");
            PluginSepTextBlock1.FontFamily = new FontFamily("Segoe UI SemiBold");
            PluginSepTextBlock1.SetValue(Grid.ColumnProperty, 1);
            PluginSepTextBlock1.HorizontalAlignment = HorizontalAlignment.Center;
            PluginSepTextBlock1.VerticalAlignment = VerticalAlignment.Center;
            PluginSepTextBlock1.Tag = $"{id}_PluginSepTextBlock1";

            System.Windows.Controls.TextBlock PluginIDTextBlock = new System.Windows.Controls.TextBlock();
            PluginIDTextBlock.Text = PluginLoader.Plugins[id].ID;
            PluginIDTextBlock.FontSize = 18;
            PluginIDTextBlock.Foreground = (Brush)FindResource("TextFillColorSecondaryBrush");
            PluginIDTextBlock.FontFamily = new FontFamily("Segoe UI");
            PluginIDTextBlock.SetValue(Grid.ColumnProperty, 2);
            PluginIDTextBlock.VerticalAlignment = VerticalAlignment.Center;
            PluginIDTextBlock.Tag = $"{id}_PluginIDTextBlock";

            System.Windows.Controls.TextBlock PluginSepTextBlock2 = new System.Windows.Controls.TextBlock();
            PluginSepTextBlock2.Text = "•";
            PluginSepTextBlock2.FontSize = 18;
            PluginSepTextBlock2.Foreground = (Brush)FindResource("TextFillColorSecondaryBrush");
            PluginSepTextBlock2.FontFamily = new FontFamily("Segoe UI");
            PluginSepTextBlock2.SetValue(Grid.ColumnProperty, 3);
            PluginSepTextBlock2.HorizontalAlignment = HorizontalAlignment.Center;
            PluginSepTextBlock2.VerticalAlignment = VerticalAlignment.Center;
            PluginSepTextBlock2.Tag = $"{id}_PluginSepTextBlock2";

            System.Windows.Controls.TextBlock PluginAuthorTextBlock = new System.Windows.Controls.TextBlock();
            PluginAuthorTextBlock.Text = PluginLoader.Plugins[id].Author;
            PluginAuthorTextBlock.FontSize = 18;
            PluginAuthorTextBlock.Foreground = (Brush)FindResource("TextFillColorSecondaryBrush");
            PluginAuthorTextBlock.FontFamily = new FontFamily("Segoe UI");
            PluginAuthorTextBlock.SetValue(Grid.ColumnProperty, 4);
            PluginAuthorTextBlock.VerticalAlignment = VerticalAlignment.Center;
            PluginAuthorTextBlock.Tag = $"{id}_PluginAuthorTextBlock";

            System.Windows.Controls.TextBlock PluginSepTextBlock3 = new System.Windows.Controls.TextBlock();
            PluginSepTextBlock3.Text = "•";
            PluginSepTextBlock3.FontSize = 18;
            PluginSepTextBlock3.Foreground = (Brush)FindResource("TextFillColorSecondaryBrush");
            PluginSepTextBlock3.FontFamily = new FontFamily("Segoe UI");
            PluginSepTextBlock3.SetValue(Grid.ColumnProperty, 5);
            PluginSepTextBlock3.HorizontalAlignment = HorizontalAlignment.Center;
            PluginSepTextBlock3.VerticalAlignment = VerticalAlignment.Center;
            PluginSepTextBlock3.Tag = $"{id}_PluginSepTextBlock3";

            System.Windows.Controls.TextBlock PluginVersionTextBlock = new System.Windows.Controls.TextBlock();
            PluginVersionTextBlock.Text = PluginLoader.Plugins[id].Version;
            PluginVersionTextBlock.FontSize = 18;
            PluginVersionTextBlock.Foreground = (Brush)FindResource("TextFillColorSecondaryBrush");
            PluginVersionTextBlock.FontFamily = new FontFamily("Segoe UI");
            PluginVersionTextBlock.SetValue(Grid.ColumnProperty, 6);
            PluginVersionTextBlock.VerticalAlignment = VerticalAlignment.Center;
            PluginVersionTextBlock.Tag = $"{id}_PluginVersionTextBlock";

            System.Windows.Controls.TextBlock PluginDescTextBlock = new System.Windows.Controls.TextBlock();
            PluginDescTextBlock.Text = PluginLoader.Plugins[id].Description;
            PluginDescTextBlock.FontSize = 12;
            PluginDescTextBlock.Foreground = (Brush)FindResource("TextFillColorPrimaryBrush");
            PluginDescTextBlock.FontFamily = new FontFamily("Segoe UI");
            PluginDescTextBlock.SetValue(Grid.RowProperty, 2);
            PluginDescTextBlock.TextWrapping = TextWrapping.Wrap;
            PluginDescTextBlock.Tag = $"{id}_PluginDescTextBlock";

            Grid PluginInfoButtonGrid = new Grid();
            PluginInfoButtonGrid.SetValue(Grid.RowProperty, 4);
            PluginInfoButtonGrid.HorizontalAlignment = HorizontalAlignment.Left;
            PluginInfoButtonGrid.Tag = $"{id}_PluginInfoButtonGrid";
            PluginInfoButtonGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Auto) });
            PluginInfoButtonGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(5) });
            PluginInfoButtonGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Auto) });

            Wpf.Ui.Controls.Button PluginInfoWebsiteButton = new Wpf.Ui.Controls.Button();
            PluginInfoWebsiteButton.Content = "Website";
            PluginInfoWebsiteButton.SetValue(Grid.ColumnProperty, 0);
            PluginInfoWebsiteButton.Tag = $"{id}_PluginInfoWebsiteButton";
            PluginInfoWebsiteButton.Click += async (s, e) =>
            {
                Process.Start(new ProcessStartInfo(PluginLoader.Plugins[id].ProjectWebsiteURL) { UseShellExecute = true });
            };

            Wpf.Ui.Controls.Button PluginInfoSourceButton = new Wpf.Ui.Controls.Button();
            PluginInfoSourceButton.Content = "Source";
            PluginInfoSourceButton.SetValue(Grid.ColumnProperty, 2);
            PluginInfoSourceButton.Tag = $"{id}_PluginInfoSourceButton";
            PluginInfoSourceButton.Click += async (s, e) =>
            {
                Process.Start(new ProcessStartInfo(PluginLoader.Plugins[id].ProjectSourceURL) { UseShellExecute = true });
            };

            Grid PluginActionButtonGrid = new Grid();
            PluginActionButtonGrid.SetValue(Grid.ColumnProperty, 2);
            PluginActionButtonGrid.Margin = new Thickness(0, 0, 10, 0);
            PluginActionButtonGrid.Tag = $"{id}_PluginActionButtonGrid";
            PluginActionButtonGrid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(1, GridUnitType.Star) });
            PluginActionButtonGrid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(1, GridUnitType.Star) });
            PluginActionButtonGrid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(1, GridUnitType.Star) });
            PluginActionButtonGrid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(1, GridUnitType.Star) });

            ToggleButton PluginEnableToggleButton = new ToggleButton();
            PluginEnableToggleButton.Content = "Enabled";
            PluginEnableToggleButton.SetValue(Grid.RowProperty, 0);
            PluginEnableToggleButton.IsChecked = true;
            PluginEnableToggleButton.HorizontalAlignment = HorizontalAlignment.Stretch;
            PluginEnableToggleButton.Tag = $"{id}_PluginEnableToggleButton";
            PluginEnableToggleButton.Click += async (s, e) =>
            {
                ConfigManager.NewVariable.RestartNeeded = true;

                if (PluginEnableToggleButton.IsChecked == true)
                {
                    PluginEnableToggleButton.Content = "Enabled";
                    PluginLoader.DelDisabledPlugin(PluginLoader.Plugins[id].ID);
                }
                else
                {
                    PluginEnableToggleButton.Content = "Disabled";
                    PluginLoader.AddDisabledPlugin(PluginLoader.Plugins[id].ID);
                }
            };

            Wpf.Ui.Controls.Button PluginUpdateButton = new Wpf.Ui.Controls.Button();
            PluginUpdateButton.Content = "Update";
            PluginUpdateButton.SetValue(Grid.RowProperty, 1);
            PluginUpdateButton.HorizontalAlignment = HorizontalAlignment.Stretch;
            PluginUpdateButton.IsEnabled = false;
            PluginUpdateButton.Tag = $"{id}_PluginUpdateButton";

            Wpf.Ui.Controls.Button PluginSettingsButton = new Wpf.Ui.Controls.Button();
            PluginSettingsButton.Content = "Settings";
            PluginSettingsButton.SetValue(Grid.RowProperty, 2);
            PluginSettingsButton.HorizontalAlignment = HorizontalAlignment.Stretch;
            PluginSettingsButton.Tag = $"{id}_PluginSettingsButton";
            PluginSettingsButton.Click += async (s, e) =>
            {
                await ShowPluginSettings(PluginLoader.Plugins[id].ID);
            };

            Wpf.Ui.Controls.Button PluginDeleteButton = new Wpf.Ui.Controls.Button();
            PluginDeleteButton.Content = "Delete";
            PluginDeleteButton.SetValue(Grid.RowProperty, 3);
            PluginDeleteButton.HorizontalAlignment = HorizontalAlignment.Stretch;
            PluginDeleteButton.Tag = $"{id}_PluginDeleteButton";
            PluginDeleteButton.Click += async (s, e) =>
            {
                ConfigManager.NewVariable.RestartNeeded = true;
                File.Delete(System.IO.Path.Combine(PluginLoader.PluginPath, $"{id}.dll"));
                PluginCardStack.Children.Remove(PluginCard);
            };

            PluginActionButtonGrid.Children.Add(PluginEnableToggleButton);
            PluginActionButtonGrid.Children.Add(PluginUpdateButton);
            PluginActionButtonGrid.Children.Add(PluginSettingsButton);
            PluginActionButtonGrid.Children.Add(PluginDeleteButton);

            PluginInfoButtonGrid.Children.Add(PluginInfoWebsiteButton);
            PluginInfoButtonGrid.Children.Add(PluginInfoSourceButton);

            PluginTitleGrid.Children.Add(PluginNameTextBlock);
            PluginTitleGrid.Children.Add(PluginSepTextBlock1);
            PluginTitleGrid.Children.Add(PluginIDTextBlock);
            PluginTitleGrid.Children.Add(PluginSepTextBlock2);
            PluginTitleGrid.Children.Add(PluginAuthorTextBlock);
            PluginTitleGrid.Children.Add(PluginSepTextBlock3);
            PluginTitleGrid.Children.Add(PluginVersionTextBlock);

            PluginCardInfoGrid.Children.Add(PluginTitleGrid);
            PluginCardInfoGrid.Children.Add(PluginDescTextBlock);
            PluginCardInfoGrid.Children.Add(PluginInfoButtonGrid);

            PluginCardMainGrid.Children.Add(PluginIcon);
            PluginCardMainGrid.Children.Add(PluginCardInfoGrid);
            PluginCardMainGrid.Children.Add(PluginActionButtonGrid);

            PluginCard.Content = PluginCardMainGrid;

            PluginCardStack.Children.Add(PluginCard);
        }

        private string SettingsPluginID = "";
        private async void PluginSettingsCancelBtn_Click(object sender, RoutedEventArgs e)
        {
            await HidePluginSettings();
        }

        private async void PluginSettingsSaveBtn_Click(object sender, RoutedEventArgs e)
        {
            ConfigManager.NewVariable.RestartNeeded = true;
            await PluginLoader.Plugins[SettingsPluginID].SaveConfig();
            await HidePluginSettings();
        }
    }
}
