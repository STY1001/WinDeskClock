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
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using WinDeskClock.Utils;

namespace WinDeskClock.Clocks
{
    /// <summary>
    /// Interaction logic for FluentClock.xaml
    /// </summary>
    public partial class FluentClock : Page
    {
        DispatcherTimer time;

        public FluentClock()
        {
            InitializeComponent();

            // Load the clock
            Loaded += async (s, e) => await Load();
        }

        private bool Init = false;
        public async Task Load()
        {
            if (!Init)
            {
                // Create a timer to update the clock every second
                time = new DispatcherTimer
                {
                    Interval = TimeSpan.FromSeconds(1)
                };
                time.Tick += Time_Tick;
                time.Start();
                Init = true;
            }

            // Update the UI
            await UIUpdate();
        }

        public async Task UIUpdate()
        {
            // - Show seconds
            if (ConfigManager.Variable.ClockShowSecond)
            {
                ClockGridSecondCol1.Width = new GridLength(20);
                ClockGridSecondCol2.Width = new GridLength(1, GridUnitType.Star);
                DateGridYearCol1.Width = new GridLength(10);
                DateGridYearCol2.Width = new GridLength(1, GridUnitType.Auto);
            }
            else
            {
                ClockGridSecondCol1.Width = new GridLength(0);
                ClockGridSecondCol2.Width = new GridLength(0);
                DateGridYearCol1.Width = new GridLength(0);
                DateGridYearCol2.Width = new GridLength(0);
            }
        }

        /*
         * Time Digit Legend:
         * H1 = [0]0:00:00
         * H2 = 0[0]:00:00
         * M1 = 00:[0]0:00
         * M2 = 00:0[0]:00
         * S1 = 00:00:[0]0
         * S2 = 00:00:0[0]
         */

        // Current char of the clock
        private string ActualH1 = "1";
        private string ActualH2 = "7";
        private string ActualM1 = "2";
        private string ActualM2 = "0";
        private string ActualS1 = "0";
        private string ActualS2 = "0";
        private string ActualDName = "MON";
        private string ActualDDay = "7";
        private string ActualDMonth = "AUG";
        private string ActualDYear = "2006";

        // Animation variables
        // - Slide speed
        private double txtslidespeed = 0.15;
        // - Delay between slides
        private int txtdelay = 150;

        // Tick event
        private async void Time_Tick(object sender, EventArgs e)
        {
            // Update the clock
            DateTime now = DateTime.Now;
            UpdateH1(now.Hour.ToString("00")[0].ToString()); // First digit of the hour
            UpdateH2(now.Hour.ToString("00")[1].ToString()); // Second digit of the hour
            UpdateM1(now.Minute.ToString("00")[0].ToString()); // First digit of the minute
            UpdateM2(now.Minute.ToString("00")[1].ToString()); // Second digit of the minute
            UpdateS1(now.Second.ToString("00")[0].ToString()); // First digit of the second
            UpdateS2(now.Second.ToString("00")[1].ToString()); // Second digit of the second
            UpdateDName(now.DayOfWeek.ToString().Substring(0, 3).ToUpper());  // Day of the week
            UpdateDDay(now.Day.ToString());  // Day of the month
            UpdateDMonth(now.ToString("MMM").ToUpper());  // Month
            UpdateDYear(now.Year.ToString("0000"));  // Year
        }

        // Update the clock with animation
        private async Task UpdateH1(string text)
        {
            if (ActualH1 != text)
            {
                H1Text.Text = ActualH1;
                ActualH1 = text;
                {
                    var translateAnimation = new DoubleAnimation
                    {
                        From = 0,
                        To = -180,
                        Duration = TimeSpan.FromSeconds(txtslidespeed),
                        EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn }
                    };
                    Storyboard.SetTarget(translateAnimation, H1Text);
                    Storyboard.SetTargetProperty(translateAnimation, new PropertyPath("(UIElement.RenderTransform).(TranslateTransform.Y)"));
                    var storyboard = new Storyboard();
                    storyboard.Children.Add(translateAnimation);
                    storyboard.Begin();
                }
                await Task.Delay(txtdelay);
                H1Text.Text = text;
                {
                    var translateAnimation = new DoubleAnimation
                    {
                        From = 180,
                        To = 0,
                        Duration = TimeSpan.FromSeconds(txtslidespeed),
                        EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                    };
                    Storyboard.SetTarget(translateAnimation, H1Text);
                    Storyboard.SetTargetProperty(translateAnimation, new PropertyPath("(UIElement.RenderTransform).(TranslateTransform.Y)"));
                    var storyboard = new Storyboard();
                    storyboard.Children.Add(translateAnimation);
                    storyboard.Begin();
                }
            }
        }
        private async Task UpdateH2(string text)
        {
            if (ActualH2 != text)
            {
                H2Text.Text = ActualH2;
                ActualH2 = text;
                {
                    var translateAnimation = new DoubleAnimation
                    {
                        From = 0,
                        To = -180,
                        Duration = TimeSpan.FromSeconds(txtslidespeed),
                        EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn }
                    };
                    Storyboard.SetTarget(translateAnimation, H2Text);
                    Storyboard.SetTargetProperty(translateAnimation, new PropertyPath("(UIElement.RenderTransform).(TranslateTransform.Y)"));
                    var storyboard = new Storyboard();
                    storyboard.Children.Add(translateAnimation);
                    storyboard.Begin();
                }
                await Task.Delay(txtdelay);
                H2Text.Text = text;
                {
                    var translateAnimation = new DoubleAnimation
                    {
                        From = 180,
                        To = 0,
                        Duration = TimeSpan.FromSeconds(txtslidespeed),
                        EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                    };
                    Storyboard.SetTarget(translateAnimation, H2Text);
                    Storyboard.SetTargetProperty(translateAnimation, new PropertyPath("(UIElement.RenderTransform).(TranslateTransform.Y)"));
                    var storyboard = new Storyboard();
                    storyboard.Children.Add(translateAnimation);
                    storyboard.Begin();
                }
            }
        }
        private async Task UpdateM1(string text)
        {
            if (ActualM1 != text)
            {
                M1Text.Text = ActualM1;
                ActualM1 = text;
                {
                    var translateAnimation = new DoubleAnimation
                    {
                        From = 0,
                        To = 180,
                        Duration = TimeSpan.FromSeconds(txtslidespeed),
                        EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn }
                    };
                    Storyboard.SetTarget(translateAnimation, M1Text);
                    Storyboard.SetTargetProperty(translateAnimation, new PropertyPath("(UIElement.RenderTransform).(TranslateTransform.Y)"));
                    var storyboard = new Storyboard();
                    storyboard.Children.Add(translateAnimation);
                    storyboard.Begin();
                }
                await Task.Delay(txtdelay);
                M1Text.Text = text;
                {
                    var translateAnimation = new DoubleAnimation
                    {
                        From = -180,
                        To = 0,
                        Duration = TimeSpan.FromSeconds(txtslidespeed),
                        EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                    };
                    Storyboard.SetTarget(translateAnimation, M1Text);
                    Storyboard.SetTargetProperty(translateAnimation, new PropertyPath("(UIElement.RenderTransform).(TranslateTransform.Y)"));
                    var storyboard = new Storyboard();
                    storyboard.Children.Add(translateAnimation);
                    storyboard.Begin();
                }
            }
        }
        private async Task UpdateM2(string text)
        {
            if (ActualM2 != text)
            {
                M2Text.Text = ActualM2;
                ActualM2 = text;
                {
                    var translateAnimation = new DoubleAnimation
                    {
                        From = 0,
                        To = 180,
                        Duration = TimeSpan.FromSeconds(txtslidespeed),
                        EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn }
                    };
                    Storyboard.SetTarget(translateAnimation, M2Text);
                    Storyboard.SetTargetProperty(translateAnimation, new PropertyPath("(UIElement.RenderTransform).(TranslateTransform.Y)"));
                    var storyboard = new Storyboard();
                    storyboard.Children.Add(translateAnimation);
                    storyboard.Begin();
                }
                await Task.Delay(txtdelay);
                M2Text.Text = text;
                {
                    var translateAnimation = new DoubleAnimation
                    {
                        From = -180,
                        To = 0,
                        Duration = TimeSpan.FromSeconds(txtslidespeed),
                        EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                    };
                    Storyboard.SetTarget(translateAnimation, M2Text);
                    Storyboard.SetTargetProperty(translateAnimation, new PropertyPath("(UIElement.RenderTransform).(TranslateTransform.Y)"));
                    var storyboard = new Storyboard();
                    storyboard.Children.Add(translateAnimation);
                    storyboard.Begin();
                }
            }
        }
        private async Task UpdateS1(string text)
        {
            if (ActualS1 != text)
            {
                S1Text.Text = ActualS1;
                ActualS1 = text;
                {
                    var translateAnimation = new DoubleAnimation
                    {
                        From = 0,
                        To = 110,
                        Duration = TimeSpan.FromSeconds(txtslidespeed),
                        EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn }
                    };
                    Storyboard.SetTarget(translateAnimation, S1Text);
                    Storyboard.SetTargetProperty(translateAnimation, new PropertyPath("(UIElement.RenderTransform).(TranslateTransform.X)"));
                    var storyboard = new Storyboard();
                    storyboard.Children.Add(translateAnimation);
                    storyboard.Begin();
                }
                await Task.Delay(txtdelay);
                S1Text.Text = text;
                {
                    var translateAnimation = new DoubleAnimation
                    {
                        From = -110,
                        To = 0,
                        Duration = TimeSpan.FromSeconds(txtslidespeed),
                        EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                    };
                    Storyboard.SetTarget(translateAnimation, S1Text);
                    Storyboard.SetTargetProperty(translateAnimation, new PropertyPath("(UIElement.RenderTransform).(TranslateTransform.X)"));
                    var storyboard = new Storyboard();
                    storyboard.Children.Add(translateAnimation);
                    storyboard.Begin();
                }
            }
        }
        private async Task UpdateS2(string text)
        {
            if (ActualS2 != text)
            {
                S2Text.Text = ActualS2;
                ActualS2 = text;
                {
                    var translateAnimation = new DoubleAnimation
                    {
                        From = 0,
                        To = 110,
                        Duration = TimeSpan.FromSeconds(txtslidespeed),
                        EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn }
                    };
                    Storyboard.SetTarget(translateAnimation, S2Text);
                    Storyboard.SetTargetProperty(translateAnimation, new PropertyPath("(UIElement.RenderTransform).(TranslateTransform.X)"));
                    var storyboard = new Storyboard();
                    storyboard.Children.Add(translateAnimation);
                    storyboard.Begin();
                }
                await Task.Delay(txtdelay);
                S2Text.Text = text;
                {
                    var translateAnimation = new DoubleAnimation
                    {
                        From = -110,
                        To = 0,
                        Duration = TimeSpan.FromSeconds(txtslidespeed),
                        EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                    };
                    Storyboard.SetTarget(translateAnimation, S2Text);
                    Storyboard.SetTargetProperty(translateAnimation, new PropertyPath("(UIElement.RenderTransform).(TranslateTransform.X)"));
                    var storyboard = new Storyboard();
                    storyboard.Children.Add(translateAnimation);
                    storyboard.Begin();
                }
            }
        }

        private async Task UpdateDName(string text)
        {
            if (ActualDName != text)
            {
                DNameText.Text = ActualDName;
                ActualDName = text;
                {
                    var translateAnimation = new DoubleAnimation
                    {
                        From = 0,
                        To = -55,
                        Duration = TimeSpan.FromSeconds(txtslidespeed),
                        EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn }
                    };
                    Storyboard.SetTarget(translateAnimation, DNameText);
                    Storyboard.SetTargetProperty(translateAnimation, new PropertyPath("(UIElement.RenderTransform).(TranslateTransform.Y)"));
                    var storyboard = new Storyboard();
                    storyboard.Children.Add(translateAnimation);
                    storyboard.Begin();
                }
                await Task.Delay(txtdelay);
                DNameText.Text = text;
                {
                    var translateAnimation = new DoubleAnimation
                    {
                        From = 55,
                        To = 0,
                        Duration = TimeSpan.FromSeconds(txtslidespeed),
                        EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                    };
                    Storyboard.SetTarget(translateAnimation, DNameText);
                    Storyboard.SetTargetProperty(translateAnimation, new PropertyPath("(UIElement.RenderTransform).(TranslateTransform.Y)"));
                    var storyboard = new Storyboard();
                    storyboard.Children.Add(translateAnimation);
                    storyboard.Begin();
                }
            }
        }

        private async Task UpdateDDay(string text)
        {
            if (ActualDDay != text)
            {
                DDayText.Text = ActualDDay;
                ActualDDay = text;
                {
                    var translateAnimation = new DoubleAnimation
                    {
                        From = 0,
                        To = -55,
                        Duration = TimeSpan.FromSeconds(txtslidespeed),
                        EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn }
                    };
                    Storyboard.SetTarget(translateAnimation, DDayText);
                    Storyboard.SetTargetProperty(translateAnimation, new PropertyPath("(UIElement.RenderTransform).(TranslateTransform.Y)"));
                    var storyboard = new Storyboard();
                    storyboard.Children.Add(translateAnimation);
                    storyboard.Begin();
                }
                await Task.Delay(txtdelay);
                DDayText.Text = text;
                {
                    var translateAnimation = new DoubleAnimation
                    {
                        From = 55,
                        To = 0,
                        Duration = TimeSpan.FromSeconds(txtslidespeed),
                        EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                    };
                    Storyboard.SetTarget(translateAnimation, DDayText);
                    Storyboard.SetTargetProperty(translateAnimation, new PropertyPath("(UIElement.RenderTransform).(TranslateTransform.Y)"));
                    var storyboard = new Storyboard();
                    storyboard.Children.Add(translateAnimation);
                    storyboard.Begin();
                }
            }
        }

        private async Task UpdateDMonth(string text)
        {
            if (ActualDMonth != text)
            {
                DMonthText.Text = ActualDMonth;
                ActualDMonth = text;
                {
                    var translateAnimation = new DoubleAnimation
                    {
                        From = 0,
                        To = -55,
                        Duration = TimeSpan.FromSeconds(txtslidespeed),
                        EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn }
                    };
                    Storyboard.SetTarget(translateAnimation, DMonthText);
                    Storyboard.SetTargetProperty(translateAnimation, new PropertyPath("(UIElement.RenderTransform).(TranslateTransform.Y)"));
                    var storyboard = new Storyboard();
                    storyboard.Children.Add(translateAnimation);
                    storyboard.Begin();
                }
                await Task.Delay(txtdelay);
                DMonthText.Text = text;
                {
                    var translateAnimation = new DoubleAnimation
                    {
                        From = 55,
                        To = 0,
                        Duration = TimeSpan.FromSeconds(txtslidespeed),
                        EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                    };
                    Storyboard.SetTarget(translateAnimation, DMonthText);
                    Storyboard.SetTargetProperty(translateAnimation, new PropertyPath("(UIElement.RenderTransform).(TranslateTransform.Y)"));
                    var storyboard = new Storyboard();
                    storyboard.Children.Add(translateAnimation);
                    storyboard.Begin();
                }
            }
        }

        private async Task UpdateDYear(string text)
        {
            if (ActualDYear != text)
            {
                DYearText.Text = ActualDYear;
                ActualDYear = text;
                {
                    var translateAnimation = new DoubleAnimation
                    {
                        From = 0,
                        To = -55,
                        Duration = TimeSpan.FromSeconds(txtslidespeed),
                        EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn }
                    };
                    Storyboard.SetTarget(translateAnimation, DYearText);
                    Storyboard.SetTargetProperty(translateAnimation, new PropertyPath("(UIElement.RenderTransform).(TranslateTransform.Y)"));
                    var storyboard = new Storyboard();
                    storyboard.Children.Add(translateAnimation);
                    storyboard.Begin();
                }
                await Task.Delay(txtdelay);
                DYearText.Text = text;
                {
                    var translateAnimation = new DoubleAnimation
                    {
                        From = 55,
                        To = 0,
                        Duration = TimeSpan.FromSeconds(txtslidespeed),
                        EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                    };
                    Storyboard.SetTarget(translateAnimation, DYearText);
                    Storyboard.SetTargetProperty(translateAnimation, new PropertyPath("(UIElement.RenderTransform).(TranslateTransform.Y)"));
                    var storyboard = new Storyboard();
                    storyboard.Children.Add(translateAnimation);
                    storyboard.Begin();
                }
            }
        }
    }
}
