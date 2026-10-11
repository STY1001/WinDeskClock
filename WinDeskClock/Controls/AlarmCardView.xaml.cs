using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Animation;
using WinDeskClock.ViewModels;
using WinDeskClock.Views.Animation;

namespace WinDeskClock.Controls
{
    /// <summary>
    /// One alarm card. Replaces the original's ~600-line imperative CreateAlarmCard: everything
    /// that's data (time, name, days, sound...) is now bound to <see cref="AlarmItemViewModel"/> in
    /// XAML; this code-behind only handles the card's own expand/collapse animation and forwards
    /// button clicks to the ViewModel.
    /// </summary>
    public partial class AlarmCardView : UserControl
    {
        private readonly FrameworkElement _expandedHeightReference;
        private AlarmItemViewModel ViewModel => (AlarmItemViewModel)DataContext;

        public AlarmCardView(AlarmItemViewModel viewModel, FrameworkElement expandedHeightReference)
        {
            InitializeComponent();
            DataContext = viewModel;
            _expandedHeightReference = expandedHeightReference;

            SyncSoundComboBoxSelection();

            viewModel.PropertyChanged += (_, e) =>
            {
                switch (e.PropertyName)
                {
                    case nameof(AlarmItemViewModel.IsExpanded): OnExpandedChanged(viewModel.IsExpanded); break;
                    case nameof(AlarmItemViewModel.DraftHourDisplay): AnimateDigit(HourText, viewModel.DraftHourDisplay); break;
                    case nameof(AlarmItemViewModel.DraftMinuteDisplay): AnimateDigit(MinuteText, viewModel.DraftMinuteDisplay); break;
                    case nameof(AlarmItemViewModel.DraftSoundIsCustom): SyncSoundComboBoxSelection(); break;
                }
            };
        }

        private static void AnimateDigit(TextBlock target, string newText) =>
            _ = UiAnimator.SlideReplaceAsync(target, SlideAxis.Y, -50, 50, () => target.Text = newText,
                0.15, 0.15, 150, new QuinticEase { EasingMode = EasingMode.EaseIn }, new QuinticEase { EasingMode = EasingMode.EaseOut });

        private void SyncSoundComboBoxSelection()
        {
            SoundComboBox.SelectionChanged -= SoundComboBox_SelectionChanged;
            SoundComboBox.SelectedItem = ViewModel.DraftSoundIsCustom ? CustomSoundItem : DefaultSoundItem;
            SoundComboBox.SelectionChanged += SoundComboBox_SelectionChanged;
        }

        private async void OnExpandedChanged(bool expanded)
        {
            if (expanded)
            {
                SmallGrid.Visibility = Visibility.Collapsed;
                BigGrid.Visibility = Visibility.Visible;

                UiAnimator.Height(MainGrid, MainGrid.ActualHeight, _expandedHeightReference.ActualHeight - 23, 0.3, new QuarticEase { EasingMode = EasingMode.EaseOut });
                UiAnimator.Fade(BigGrid, 0, 1, 0.5, new QuadraticEase { EasingMode = EasingMode.EaseIn });
            }
            else
            {
                // Swap the two views *immediately* (like the original's AlarmCardClose) so the small
                // view can actually fade in during the shrink. Doing this only after the delay left
                // SmallGrid collapsed for the whole animation - its fade was invisible and the card
                // then popped in half-faded at the end.
                BigGrid.Visibility = Visibility.Collapsed;
                SmallGrid.Visibility = Visibility.Visible;

                double collapsedHeight = SmallGrid.ActualHeight > 0 ? SmallGrid.ActualHeight : 64;
                UiAnimator.Height(MainGrid, _expandedHeightReference.ActualHeight - 23, collapsedHeight, 0.3, new QuarticEase { EasingMode = EasingMode.EaseOut });
                UiAnimator.Fade(SmallGrid, 0, 1, 0.5, new QuadraticEase { EasingMode = EasingMode.EaseIn });

                await Task.Delay(300);
                UiAnimator.ReleaseHeight(MainGrid);
            }
        }

        private void RootCard_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.Source is Wpf.Ui.Controls.CardAction or TextBlock)
            {
                ViewModel.RequestOpen();
            }
        }

        private void HourUpBtn_Click(object sender, RoutedEventArgs e) => ViewModel.IncrementHour();
        private void HourDownBtn_Click(object sender, RoutedEventArgs e) => ViewModel.DecrementHour();
        private void MinuteUpBtn_Click(object sender, RoutedEventArgs e) => ViewModel.IncrementMinute();
        private void MinuteDownBtn_Click(object sender, RoutedEventArgs e) => ViewModel.DecrementMinute();

        private void OnceToggle_Click(object sender, RoutedEventArgs e) => ViewModel.ToggleOnce();
        private void DayToggle_Click(object sender, RoutedEventArgs e) => ViewModel.ToggleDay(int.Parse((string)((FrameworkElement)sender).Tag));

        private void SoundComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (SoundComboBox.SelectedItem == CustomSoundItem)
            {
                ViewModel.SelectCustomSound();
                SyncSoundComboBoxSelection();
            }
            else
            {
                ViewModel.SelectDefaultSound();
            }
        }

        private void SaveBtn_Click(object sender, RoutedEventArgs e) => ViewModel.RequestSave();
        private void CancelBtn_Click(object sender, RoutedEventArgs e) => ViewModel.RequestCancel();
        private void DeleteBtn_Click(object sender, RoutedEventArgs e) => ViewModel.RequestDelete();
    }
}
