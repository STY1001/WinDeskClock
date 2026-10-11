using System.Windows.Controls;
using System.Windows.Media.Animation;
using WinDeskClock.ViewModels;
using WinDeskClock.Views.Animation;

namespace WinDeskClock.Clocks
{
    /// <summary>
    /// The default (Fluent-style) clock face. All state and timing now live in
    /// <see cref="ClockViewModel"/> (shared with FbxClock); this class only decides how to
    /// animate each digit/field when the ViewModel says it changed.
    /// </summary>
    public partial class FluentClock : Page
    {
        private const double SlideSpeed = 0.25;
        private const int SlideDelayMs = 250;

        public FluentClock(ClockViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
            viewModel.Start();

            viewModel.PropertyChanged += (_, e) =>
            {
                switch (e.PropertyName)
                {
                    case nameof(ClockViewModel.H1): Slide(H1Text, viewModel.H1, SlideAxis.Y, -180, 180); break;
                    case nameof(ClockViewModel.H2): Slide(H2Text, viewModel.H2, SlideAxis.Y, -180, 180); break;
                    case nameof(ClockViewModel.M1): Slide(M1Text, viewModel.M1, SlideAxis.Y, 180, -180); break;
                    case nameof(ClockViewModel.M2): Slide(M2Text, viewModel.M2, SlideAxis.Y, 180, -180); break;
                    case nameof(ClockViewModel.S1): Slide(S1Text, viewModel.S1, SlideAxis.X, 110, -110); break;
                    case nameof(ClockViewModel.S2): Slide(S2Text, viewModel.S2, SlideAxis.X, 110, -110); break;
                    case nameof(ClockViewModel.DName): Slide(DNameText, viewModel.DName, SlideAxis.Y, -55, 55); break;
                    case nameof(ClockViewModel.DDay): Slide(DDayText, viewModel.DDay, SlideAxis.Y, -55, 55); break;
                    case nameof(ClockViewModel.DMonth): Slide(DMonthText, viewModel.DMonth, SlideAxis.Y, -55, 55); break;
                    case nameof(ClockViewModel.DYear): Slide(DYearText, viewModel.DYear, SlideAxis.Y, -55, 55); break;
                }
            };

            if (!viewModel.ShowSeconds)
            {
                ClockGridSecondCol1.Width = new System.Windows.GridLength(0);
                ClockGridSecondCol2.Width = new System.Windows.GridLength(0);
            }
        }

        private static void Slide(TextBlock target, string newText, SlideAxis axis, double outTo, double inFrom) =>
            _ = UiAnimator.SlideReplaceAsync(target, axis, outTo, inFrom, () => target.Text = newText,
                SlideSpeed, SlideSpeed * 2, SlideDelayMs,
                new QuinticEase { EasingMode = EasingMode.EaseIn }, new QuinticEase { EasingMode = EasingMode.EaseOut });
    }
}
