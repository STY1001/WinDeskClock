using System.Diagnostics;
using System.IO;
using System.Media;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using Wpf.Ui.Controls;
using Newtonsoft.Json;
using System.Windows.Controls.Primitives;
using Microsoft.Win32;
using Newtonsoft.Json.Linq;

namespace WinDeskClock
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>

    public class Configs
    {
        public string ConfigPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "config.json");
        public string AlarmPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "alarms.json");

        public string DefaultTimeUpSound = "C:\\Windows\\Media\\Ring01.wav";
        public string DefaultAlarmSound = "C:\\Windows\\Media\\Alarm03.wav";
        public string SnoozeDelay = "1";

        public async Task SetConfig(string key, string value)
        {
            string json = await File.ReadAllTextAsync(ConfigPath);
            JObject data = JObject.Parse(json);

            SetNestedValue(data, key.Split('.'), value);

            string newJson = data.ToString();
            await File.WriteAllTextAsync(ConfigPath, newJson);
        }

        public async Task<string> GetAlarm(string key)
        {
            string json = await File.ReadAllTextAsync(AlarmPath);
            JObject data = JObject.Parse(json);

            return GetNestedValue(data, key.Split('.'));
        }

        public async Task SetAlarm(string key, string value)
        {
            string json = await File.ReadAllTextAsync(AlarmPath);
            JObject data = JObject.Parse(json);

            SetNestedValue(data, key.Split('.'), value);

            string newJson = data.ToString();
            await File.WriteAllTextAsync(AlarmPath, newJson);
        }

        public async Task DelAlarm(string key)
        {
            string json = await File.ReadAllTextAsync(AlarmPath);
            JObject data = JObject.Parse(json);

            DeleteNestedValue(data, key.Split('.'));

            string newJson = data.ToString();
            await File.WriteAllTextAsync(AlarmPath, newJson);
        }

        private void SetNestedValue(JObject obj, string[] keys, string value)
        {
            JObject current = obj;
            for (int i = 0; i < keys.Length - 1; i++)
            {
                if (current[keys[i]] == null || current[keys[i]].Type != JTokenType.Object)
                {
                    current[keys[i]] = new JObject();
                }
                current = (JObject)current[keys[i]];
            }
            current[keys[^1]] = value;
        }

        private string GetNestedValue(JObject obj, string[] keys)
        {
            JObject current = obj;
            for (int i = 0; i < keys.Length - 1; i++)
            {
                if (current[keys[i]] == null)
                {
                    throw new KeyNotFoundException($"Key '{keys[i]}' not found.");
                }
                current = (JObject)current[keys[i]];
            }
            return current[keys[^1]]?.ToString();
        }

        private void DeleteNestedValue(JObject obj, string[] keys)
        {
            JObject current = obj;
            for (int i = 0; i < keys.Length - 1; i++)
            {
                if (current[keys[i]] == null)
                {
                    throw new KeyNotFoundException($"Key '{keys[i]}' not found.");
                }
                current = (JObject)current[keys[i]];
            }
            current.Remove(keys[^1]);
        }
    }

    public partial class MainWindow : FluentWindow
    {
        public Configs configs = new Configs();

        private List<Grid> MenuClockGrids = new List<Grid>();
        private Stopwatch stopwatch;
        private DispatcherTimer stopwatchTimer;
        private DispatcherTimer timerTimer;
        private TimeSpan timer;

        public MainWindow()
        {
            InitializeComponent();

            //Create config files if they don't exist
            if (!File.Exists(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "config.json")))
            {
                File.WriteAllText(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "config.json"), "{}");
            }
            if (!File.Exists(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "alarms.json")))
            {
                File.WriteAllText(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "alarms.json"), "{}");
            }

            // Add the grids to the list
            MenuClockGrids.Add(AlarmGrid);
            MenuClockGrids.Add(StopwatchGrid);
            MenuClockGrids.Add(TimerGrid);

            // Load the alarms
            AlarmCardRestore();

            Loaded += MainWindow_Loaded;
        }

        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // Create a timer to update the clock every second
            DispatcherTimer time = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            time.Tick += Time_Tick;
            time.Start();

            // Create the stopwatch for... the stopwatch of the app (lol)
            stopwatch = new Stopwatch();
            stopwatchTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(10)
            };
            stopwatchTimer.Tick += Stopwatch_Tick;

            // Create the timer for the... timer of the app (lol... Ok I'll stop)
            timerTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            timerTimer.Tick += Timer_Tick;

            // Create the timer for the alarm
            DispatcherTimer alarm = new DispatcherTimer
            {
                Interval = TimeSpan.FromMinutes(1)
            };
            alarm.Tick += Alarm_Tick;
            alarm.Start();
        }

        private async Task<FrameworkElement> FindFrameworkElementwithTag(DependencyObject parent, object tag)
        {
            int count = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < count; i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);

                if (child is FrameworkElement element && element.Tag?.Equals(tag) == true)
                {
                    return element;
                }

                var result = await FindFrameworkElementwithTag(child, tag);
                if (result != null)
                {
                    return result;
                }
            }
            return null;
        }

        private async Task<FrameworkElement> FindItemwithTag(ItemCollection items, object tag)
        {
            foreach (FrameworkElement item in items)
            {
                if (item.Tag?.Equals(tag) == true)
                {
                    return item;
                }
            }
            return null;
        }

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
            MiniClockHour.Text = now.Hour.ToString("00");  // Hour of MiniClock
            MiniClockMinute.Text = now.Minute.ToString("00");  // Minute of MiniClock
            DNameText.Text = now.DayOfWeek.ToString().Substring(0, 3).ToUpper();  // Day of the week
            DDayText.Text = now.Day.ToString();  // Day of the month
            DMonthText.Text = now.ToString("MMM").ToUpper();  // Month
            DYearText.Text = now.Year.ToString("0000");  // Year
        }

        private string ActualH1 = "1";
        private string ActualH2 = "7";
        private string ActualM1 = "2";
        private string ActualM2 = "0";
        private string ActualS1 = "0";
        private string ActualS2 = "0";
        private double txtslidespeed = 0.15;
        private int txtdelay = 150;
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

        private double fadespeed = 0.30;
        private double zoomspeed = 0.35;
        private int MenuClockIndex = 0;
        private async void EnterDownMenuClockBtn_Click(object sender, RoutedEventArgs e)
        {
            {
                var fadeAnimation = new DoubleAnimation
                {
                    From = 1,
                    To = 0,
                    Duration = TimeSpan.FromSeconds(fadespeed),
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                };
                var zoomAnimation1 = new DoubleAnimation
                {
                    From = 1,
                    To = 0,
                    Duration = TimeSpan.FromSeconds(zoomspeed),
                    EasingFunction = new QuarticEase { EasingMode = EasingMode.EaseIn }
                };
                var zoomAnimation2 = new DoubleAnimation
                {
                    From = 1,
                    To = 0,
                    Duration = TimeSpan.FromSeconds(zoomspeed),
                    EasingFunction = new QuarticEase { EasingMode = EasingMode.EaseIn }
                };
                Storyboard.SetTarget(fadeAnimation, ClockGrid);
                Storyboard.SetTargetProperty(fadeAnimation, new PropertyPath(UIElement.OpacityProperty));
                Storyboard.SetTarget(zoomAnimation1, ClockGrid);
                Storyboard.SetTarget(zoomAnimation2, ClockGrid);
                Storyboard.SetTargetProperty(zoomAnimation1, new PropertyPath("(UIElement.RenderTransform).(ScaleTransform.ScaleX)"));
                Storyboard.SetTargetProperty(zoomAnimation2, new PropertyPath("(UIElement.RenderTransform).(ScaleTransform.ScaleY)"));
                var storyboard = new Storyboard();
                storyboard.Children.Add(fadeAnimation);
                storyboard.Children.Add(zoomAnimation1);
                storyboard.Children.Add(zoomAnimation2);
                storyboard.Begin();
            }

            await Task.Delay(400);

            ClockGrid.Visibility = Visibility.Hidden;
            MenuClockGrid.Visibility = Visibility.Visible;

            {
                var fadeAnimation = new DoubleAnimation
                {
                    From = 0,
                    To = 1,
                    Duration = TimeSpan.FromSeconds(fadespeed),
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn }
                };
                var translateAnimation = new DoubleAnimation
                {
                    From = 100,
                    To = 0,
                    Duration = TimeSpan.FromSeconds(zoomspeed),
                    EasingFunction = new QuarticEase { EasingMode = EasingMode.EaseOut }
                };
                Storyboard.SetTarget(fadeAnimation, MenuClockGrid);
                Storyboard.SetTargetProperty(fadeAnimation, new PropertyPath(UIElement.OpacityProperty));
                Storyboard.SetTarget(translateAnimation, MenuClockGrid);
                Storyboard.SetTargetProperty(translateAnimation, new PropertyPath("(UIElement.RenderTransform).(TranslateTransform.Y)"));
                var storyboard = new Storyboard();
                storyboard.Children.Add(fadeAnimation);
                storyboard.Children.Add(translateAnimation);
                storyboard.Begin();
            }

            MiniClockBorder.Visibility = Visibility.Visible;
            ExitDownMenuClockBtn.Visibility = Visibility.Visible;

            {
                var fadeAnimation = new DoubleAnimation
                {
                    From = 0,
                    To = 1,
                    Duration = TimeSpan.FromSeconds(fadespeed),
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn }
                };
                var fadeAnimation2 = new DoubleAnimation
                {
                    From = 0,
                    To = 1,
                    Duration = TimeSpan.FromSeconds(fadespeed),
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn }
                };
                var zoomAnimation1 = new DoubleAnimation
                {
                    From = 0,
                    To = 1,
                    Duration = TimeSpan.FromSeconds(zoomspeed),
                    EasingFunction = new QuarticEase { EasingMode = EasingMode.EaseOut }
                };
                var zoomAnimation2 = new DoubleAnimation
                {
                    From = 0,
                    To = 1,
                    Duration = TimeSpan.FromSeconds(zoomspeed),
                    EasingFunction = new QuarticEase { EasingMode = EasingMode.EaseOut }
                };
                Storyboard.SetTarget(fadeAnimation, MiniClockBorder);
                Storyboard.SetTarget(fadeAnimation2, ExitDownMenuClockBtn);
                Storyboard.SetTargetProperty(fadeAnimation, new PropertyPath(UIElement.OpacityProperty));
                Storyboard.SetTargetProperty(fadeAnimation2, new PropertyPath(UIElement.OpacityProperty));
                Storyboard.SetTarget(zoomAnimation1, MiniClockBorder);
                Storyboard.SetTarget(zoomAnimation2, MiniClockBorder);
                Storyboard.SetTargetProperty(zoomAnimation1, new PropertyPath("(UIElement.RenderTransform).(ScaleTransform.ScaleX)"));
                Storyboard.SetTargetProperty(zoomAnimation2, new PropertyPath("(UIElement.RenderTransform).(ScaleTransform.ScaleY)"));
                var storyboard = new Storyboard();
                storyboard.Children.Add(fadeAnimation);
                storyboard.Children.Add(fadeAnimation2);
                storyboard.Children.Add(zoomAnimation1);
                storyboard.Children.Add(zoomAnimation2);
                storyboard.Begin();
            }
        }
        private async void ExitDownMenuClockBtn_Click(object sender, RoutedEventArgs e)
        {
            {
                var fadeAnimation = new DoubleAnimation
                {
                    From = 1,
                    To = 0,
                    Duration = TimeSpan.FromSeconds(fadespeed),
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                };
                var translateAnimation = new DoubleAnimation
                {
                    From = 0,
                    To = 100,
                    Duration = TimeSpan.FromSeconds(zoomspeed),
                    EasingFunction = new QuarticEase { EasingMode = EasingMode.EaseIn }
                };
                Storyboard.SetTarget(fadeAnimation, MenuClockGrid);
                Storyboard.SetTargetProperty(fadeAnimation, new PropertyPath(UIElement.OpacityProperty));
                Storyboard.SetTarget(translateAnimation, MenuClockGrid);
                Storyboard.SetTargetProperty(translateAnimation, new PropertyPath("(UIElement.RenderTransform).(TranslateTransform.Y)"));
                var storyboard = new Storyboard();
                storyboard.Children.Add(fadeAnimation);
                storyboard.Children.Add(translateAnimation);
                storyboard.Begin();
            }

            {
                var fadeAnimation = new DoubleAnimation
                {
                    From = 1,
                    To = 0,
                    Duration = TimeSpan.FromSeconds(fadespeed),
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                };
                var fadeAnimation2 = new DoubleAnimation
                {
                    From = 1,
                    To = 0,
                    Duration = TimeSpan.FromSeconds(fadespeed),
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                };
                var zoomAnimation1 = new DoubleAnimation
                {
                    From = 1,
                    To = 0,
                    Duration = TimeSpan.FromSeconds(zoomspeed),
                    EasingFunction = new QuarticEase { EasingMode = EasingMode.EaseIn }
                };
                var zoomAnimation2 = new DoubleAnimation
                {
                    From = 1,
                    To = 0,
                    Duration = TimeSpan.FromSeconds(zoomspeed),
                    EasingFunction = new QuarticEase { EasingMode = EasingMode.EaseIn }
                };
                Storyboard.SetTarget(fadeAnimation, MiniClockBorder);
                Storyboard.SetTarget(fadeAnimation2, ExitDownMenuClockBtn);
                Storyboard.SetTargetProperty(fadeAnimation, new PropertyPath(UIElement.OpacityProperty));
                Storyboard.SetTargetProperty(fadeAnimation2, new PropertyPath(UIElement.OpacityProperty));
                Storyboard.SetTarget(zoomAnimation1, MiniClockBorder);
                Storyboard.SetTarget(zoomAnimation2, MiniClockBorder);
                Storyboard.SetTargetProperty(zoomAnimation1, new PropertyPath("(UIElement.RenderTransform).(ScaleTransform.ScaleX)"));
                Storyboard.SetTargetProperty(zoomAnimation2, new PropertyPath("(UIElement.RenderTransform).(ScaleTransform.ScaleY)"));
                var storyboard = new Storyboard();
                storyboard.Children.Add(fadeAnimation);
                storyboard.Children.Add(fadeAnimation2);
                storyboard.Children.Add(zoomAnimation1);
                storyboard.Children.Add(zoomAnimation2);
                storyboard.Begin();
            }

            await Task.Delay(400);

            MenuClockGrid.Visibility = Visibility.Hidden;
            MiniClockBorder.Visibility = Visibility.Hidden;
            ExitDownMenuClockBtn.Visibility = Visibility.Hidden;
            ClockGrid.Visibility = Visibility.Visible;

            {
                var fadeAnimation = new DoubleAnimation
                {
                    From = 0,
                    To = 1,
                    Duration = TimeSpan.FromSeconds(fadespeed),
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn }
                };
                var zoomAnimation1 = new DoubleAnimation
                {
                    From = 0,
                    To = 1,
                    Duration = TimeSpan.FromSeconds(zoomspeed),
                    EasingFunction = new QuarticEase { EasingMode = EasingMode.EaseOut }
                };
                var zoomAnimation2 = new DoubleAnimation
                {
                    From = 0,
                    To = 1,
                    Duration = TimeSpan.FromSeconds(zoomspeed),
                    EasingFunction = new QuarticEase { EasingMode = EasingMode.EaseOut }
                };
                Storyboard.SetTarget(fadeAnimation, ClockGrid);
                Storyboard.SetTargetProperty(fadeAnimation, new PropertyPath(UIElement.OpacityProperty));
                Storyboard.SetTarget(zoomAnimation1, ClockGrid);
                Storyboard.SetTarget(zoomAnimation2, ClockGrid);
                Storyboard.SetTargetProperty(zoomAnimation1, new PropertyPath("(UIElement.RenderTransform).(ScaleTransform.ScaleX)"));
                Storyboard.SetTargetProperty(zoomAnimation2, new PropertyPath("(UIElement.RenderTransform).(ScaleTransform.ScaleY)"));
                var storyboard = new Storyboard();
                storyboard.Children.Add(fadeAnimation);
                storyboard.Children.Add(zoomAnimation1);
                storyboard.Children.Add(zoomAnimation2);
                storyboard.Begin();
            }
        }
        private async void LeftMenuClockBtn_Click(object sender, RoutedEventArgs e)
        {
            int ActualIndex = MenuClockIndex;
            MenuClockIndex--;
            RightMenuClockBtn.IsEnabled = true;
            if (MenuClockIndex == 0)
            {
                LeftMenuClockBtn.IsEnabled = false;
            }

            Grid PrevGrid = MenuClockGrids[ActualIndex];
            {
                var fadeAnimation = new DoubleAnimation
                {
                    From = 1,
                    To = 0,
                    Duration = TimeSpan.FromSeconds(fadespeed),
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                };
                var translateAnimation = new DoubleAnimation
                {
                    From = 0,
                    To = 100,
                    Duration = TimeSpan.FromSeconds(zoomspeed),
                    EasingFunction = new QuarticEase { EasingMode = EasingMode.EaseIn }
                };
                Storyboard.SetTarget(fadeAnimation, PrevGrid);
                Storyboard.SetTargetProperty(fadeAnimation, new PropertyPath(UIElement.OpacityProperty));
                Storyboard.SetTarget(translateAnimation, PrevGrid);
                Storyboard.SetTargetProperty(translateAnimation, new PropertyPath("(UIElement.RenderTransform).(TranslateTransform.X)"));
                var storyboard = new Storyboard();
                storyboard.Children.Add(fadeAnimation);
                storyboard.Children.Add(translateAnimation);
                storyboard.Begin();
            }

            await Task.Delay(300);
            PrevGrid.Visibility = Visibility.Hidden;

            Grid NextGrid = MenuClockGrids[MenuClockIndex];
            NextGrid.Visibility = Visibility.Visible;
            {
                var fadeAnimation = new DoubleAnimation
                {
                    From = 0,
                    To = 1,
                    Duration = TimeSpan.FromSeconds(fadespeed),
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn }
                };
                var translateAnimation = new DoubleAnimation
                {
                    From = -100,
                    To = 0,
                    Duration = TimeSpan.FromSeconds(zoomspeed),
                    EasingFunction = new QuarticEase { EasingMode = EasingMode.EaseOut }
                };
                Storyboard.SetTarget(fadeAnimation, NextGrid);
                Storyboard.SetTargetProperty(fadeAnimation, new PropertyPath(UIElement.OpacityProperty));
                Storyboard.SetTarget(translateAnimation, NextGrid);
                Storyboard.SetTargetProperty(translateAnimation, new PropertyPath("(UIElement.RenderTransform).(TranslateTransform.X)"));
                var storyboard = new Storyboard();
                storyboard.Children.Add(fadeAnimation);
                storyboard.Children.Add(translateAnimation);
                storyboard.Begin();
            }
        }
        private async void RightMenuClockBtn_Click(object sender, RoutedEventArgs e)
        {
            int ActualIndex = MenuClockIndex;
            MenuClockIndex++;
            LeftMenuClockBtn.IsEnabled = true;
            if (MenuClockIndex == MenuClockGrids.Count - 1)
            {
                RightMenuClockBtn.IsEnabled = false;
            }

            Grid PrevGrid = MenuClockGrids[ActualIndex];
            {
                var fadeAnimation = new DoubleAnimation
                {
                    From = 1,
                    To = 0,
                    Duration = TimeSpan.FromSeconds(fadespeed),
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                };
                var translateAnimation = new DoubleAnimation
                {
                    From = 0,
                    To = -100,
                    Duration = TimeSpan.FromSeconds(zoomspeed),
                    EasingFunction = new QuarticEase { EasingMode = EasingMode.EaseIn }
                };
                Storyboard.SetTarget(fadeAnimation, PrevGrid);
                Storyboard.SetTargetProperty(fadeAnimation, new PropertyPath(UIElement.OpacityProperty));
                Storyboard.SetTarget(translateAnimation, PrevGrid);
                Storyboard.SetTargetProperty(translateAnimation, new PropertyPath("(UIElement.RenderTransform).(TranslateTransform.X)"));
                var storyboard = new Storyboard();
                storyboard.Children.Add(fadeAnimation);
                storyboard.Children.Add(translateAnimation);
                storyboard.Begin();
            }

            await Task.Delay(300);
            PrevGrid.Visibility = Visibility.Hidden;

            Grid NextGrid = MenuClockGrids[MenuClockIndex];
            NextGrid.Visibility = Visibility.Visible;
            {
                var fadeAnimation = new DoubleAnimation
                {
                    From = 0,
                    To = 1,
                    Duration = TimeSpan.FromSeconds(fadespeed),
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn }
                };
                var translateAnimation = new DoubleAnimation
                {
                    From = 100,
                    To = 0,
                    Duration = TimeSpan.FromSeconds(zoomspeed),
                    EasingFunction = new QuarticEase { EasingMode = EasingMode.EaseOut }
                };
                Storyboard.SetTarget(fadeAnimation, NextGrid);
                Storyboard.SetTargetProperty(fadeAnimation, new PropertyPath(UIElement.OpacityProperty));
                Storyboard.SetTarget(translateAnimation, NextGrid);
                Storyboard.SetTargetProperty(translateAnimation, new PropertyPath("(UIElement.RenderTransform).(TranslateTransform.X)"));
                var storyboard = new Storyboard();
                storyboard.Children.Add(fadeAnimation);
                storyboard.Children.Add(translateAnimation);
                storyboard.Begin();
            }
        }

        private bool StopwatchIsRunning = false;
        private bool StopwatchLapDeployed = false;
        private double swfadespeed = 0.15;
        private double swzoomspeed = 0.30;
        private bool StopwatchProgressFlipped = false;
        private string ActualStopwatchH1 = "0";
        private string ActualStopwatchH2 = "0";
        private string ActualStopwatchM1 = "0";
        private string ActualStopwatchM2 = "0";
        private string ActualStopwatchS1 = "0";
        private string ActualStopwatchS2 = "0";
        private string ActualStopwatchMS1 = "0";
        private string ActualStopwatchMS2 = "0";
        private double swtxtslidespeed = 0.15;
        private int swtxtdelay = 150;
        private TimeSpan LastLapTime;
        private async void Stopwatch_Tick(object sender, EventArgs e)
        {
            UpdateStopwatchTextH1(stopwatch.Elapsed.Hours.ToString("00")[0].ToString());
            UpdateStopwatchTextH2(stopwatch.Elapsed.Hours.ToString("00")[1].ToString());
            UpdateStopwatchTextM1(stopwatch.Elapsed.Minutes.ToString("00")[0].ToString());
            UpdateStopwatchTextM2(stopwatch.Elapsed.Minutes.ToString("00")[1].ToString());
            UpdateStopwatchTextS1(stopwatch.Elapsed.Seconds.ToString("00")[0].ToString());
            UpdateStopwatchTextS2(stopwatch.Elapsed.Seconds.ToString("00")[1].ToString());
            UpdateStopwatchTextMS1(stopwatch.Elapsed.Milliseconds.ToString("000")[0].ToString(), false);
            UpdateStopwatchTextMS2(stopwatch.Elapsed.Milliseconds.ToString("000")[1].ToString(), false);

            if (stopwatch.Elapsed.Seconds % 2 == 0 && StopwatchProgressFlipped)
            {
                StopwatchProgressFlipped = false;
                StopwatchProgressBar.RenderTransform = new ScaleTransform(1, 1);

            }
            else if (stopwatch.Elapsed.Seconds % 2 != 0 && !StopwatchProgressFlipped)
            {
                StopwatchProgressFlipped = true;
                StopwatchProgressBar.RenderTransform = new ScaleTransform(-1, 1);
            }

            if (StopwatchProgressFlipped)
            {
                StopwatchProgressBar.Value = 1000 - stopwatch.Elapsed.Milliseconds;
            }
            else
            {
                StopwatchProgressBar.Value = stopwatch.Elapsed.Milliseconds;
            }
        }
        private async void StartStopwatchBtn_Click(object sender, RoutedEventArgs e)
        {
            stopwatch.Start();
            stopwatchTimer.Start();

            if (!StopwatchIsRunning)
            {
                StartStopwatchBtn.Visibility = Visibility.Hidden;
                PauseStopwatchBtn.Visibility = Visibility.Visible;
                ResetStopwatchBtn.Visibility = Visibility.Hidden;
                LapStopwatchBtn.Visibility = Visibility.Visible;

                {
                    var changeAnimation1 = new DoubleAnimation
                    {
                        From = 200,
                        To = 100,
                        Duration = TimeSpan.FromSeconds(swzoomspeed),
                        EasingFunction = new QuarticEase { EasingMode = EasingMode.EaseOut }
                    };
                    var changeAnimation2 = new DoubleAnimation
                    {
                        From = 200,
                        To = 100,
                        Duration = TimeSpan.FromSeconds(swzoomspeed),
                        EasingFunction = new QuarticEase { EasingMode = EasingMode.EaseOut }
                    };
                    Storyboard.SetTarget(changeAnimation1, StartStopwatchBtn);
                    Storyboard.SetTargetProperty(changeAnimation1, new PropertyPath(WidthProperty));
                    Storyboard.SetTarget(changeAnimation2, PauseStopwatchBtn);
                    Storyboard.SetTargetProperty(changeAnimation2, new PropertyPath(WidthProperty));
                    var storyboard = new Storyboard();
                    storyboard.Children.Add(changeAnimation1);
                    storyboard.Children.Add(changeAnimation2);
                    storyboard.Begin();
                }

                {
                    var changeAnimation1 = new DoubleAnimation
                    {
                        From = 0,
                        To = 100,
                        Duration = TimeSpan.FromSeconds(swzoomspeed),
                        EasingFunction = new QuarticEase { EasingMode = EasingMode.EaseOut }
                    };
                    var changeAnimation2 = new DoubleAnimation
                    {
                        From = 0,
                        To = 100,
                        Duration = TimeSpan.FromSeconds(swzoomspeed),
                        EasingFunction = new QuarticEase { EasingMode = EasingMode.EaseOut }
                    };
                    Storyboard.SetTarget(changeAnimation1, ResetStopwatchBtn);
                    Storyboard.SetTargetProperty(changeAnimation1, new PropertyPath(WidthProperty));
                    Storyboard.SetTarget(changeAnimation2, LapStopwatchBtn);
                    Storyboard.SetTargetProperty(changeAnimation2, new PropertyPath(WidthProperty));
                    var storyboard = new Storyboard();
                    storyboard.Children.Add(changeAnimation1);
                    storyboard.Children.Add(changeAnimation2);
                    storyboard.Begin();
                }

                {
                    var changeAnimation1 = new DoubleAnimation
                    {
                        From = 0,
                        To = 20,
                        Duration = TimeSpan.FromSeconds(swzoomspeed),
                        EasingFunction = new QuarticEase { EasingMode = EasingMode.EaseOut }
                    };
                    Storyboard.SetTarget(changeAnimation1, SWButtonSep);
                    Storyboard.SetTargetProperty(changeAnimation1, new PropertyPath(WidthProperty));
                    var storyboard = new Storyboard();
                    storyboard.Children.Add(changeAnimation1);
                    storyboard.Begin();
                }
            }
            else
            {
                {
                    var fadeAnimation1 = new DoubleAnimation
                    {
                        From = 1,
                        To = 0,
                        Duration = TimeSpan.FromSeconds(swfadespeed),
                        EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseInOut }
                    };
                    var fadeAnimation2 = new DoubleAnimation
                    {
                        From = 1,
                        To = 0,
                        Duration = TimeSpan.FromSeconds(swfadespeed),
                        EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseInOut }
                    };
                    Storyboard.SetTarget(fadeAnimation1, StartStopwatchBtn);
                    Storyboard.SetTargetProperty(fadeAnimation1, new PropertyPath(UIElement.OpacityProperty));
                    Storyboard.SetTarget(fadeAnimation2, ResetStopwatchBtn);
                    Storyboard.SetTargetProperty(fadeAnimation2, new PropertyPath(UIElement.OpacityProperty));
                    var storyboard = new Storyboard();
                    storyboard.Children.Add(fadeAnimation1);
                    storyboard.Children.Add(fadeAnimation2);
                    storyboard.Begin();
                }

                await Task.Delay(150);

                StartStopwatchBtn.Visibility = Visibility.Hidden;
                PauseStopwatchBtn.Visibility = Visibility.Visible;
                ResetStopwatchBtn.Visibility = Visibility.Hidden;
                LapStopwatchBtn.Visibility = Visibility.Visible;

                {
                    var fadeAnimation1 = new DoubleAnimation
                    {
                        From = 0,
                        To = 1,
                        Duration = TimeSpan.FromSeconds(swfadespeed),
                        EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseInOut }
                    };
                    var fadeAnimation2 = new DoubleAnimation
                    {
                        From = 0,
                        To = 1,
                        Duration = TimeSpan.FromSeconds(swfadespeed),
                        EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseInOut }
                    };
                    Storyboard.SetTarget(fadeAnimation1, PauseStopwatchBtn);
                    Storyboard.SetTargetProperty(fadeAnimation1, new PropertyPath(UIElement.OpacityProperty));
                    Storyboard.SetTarget(fadeAnimation2, LapStopwatchBtn);
                    Storyboard.SetTargetProperty(fadeAnimation2, new PropertyPath(UIElement.OpacityProperty));
                    var storyboard = new Storyboard();
                    storyboard.Children.Add(fadeAnimation1);
                    storyboard.Children.Add(fadeAnimation2);
                    storyboard.Begin();
                }
            }

            StopwatchIsRunning = true;
        }
        private async void PauseStopwatchBtn_Click(object sender, RoutedEventArgs e)
        {
            stopwatch.Stop();
            stopwatchTimer.Stop();

            {
                var fadeAnimation1 = new DoubleAnimation
                {
                    From = 1,
                    To = 0,
                    Duration = TimeSpan.FromSeconds(swfadespeed),
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseInOut }
                };
                var fadeAnimation2 = new DoubleAnimation
                {
                    From = 1,
                    To = 0,
                    Duration = TimeSpan.FromSeconds(swfadespeed),
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseInOut }
                };
                Storyboard.SetTarget(fadeAnimation1, PauseStopwatchBtn);
                Storyboard.SetTargetProperty(fadeAnimation1, new PropertyPath(UIElement.OpacityProperty));
                Storyboard.SetTarget(fadeAnimation2, LapStopwatchBtn);
                Storyboard.SetTargetProperty(fadeAnimation2, new PropertyPath(UIElement.OpacityProperty));
                var storyboard = new Storyboard();
                storyboard.Children.Add(fadeAnimation1);
                storyboard.Children.Add(fadeAnimation2);
                storyboard.Begin();
            }

            await Task.Delay(150);

            StartStopwatchBtn.Visibility = Visibility.Visible;
            PauseStopwatchBtn.Visibility = Visibility.Hidden;
            ResetStopwatchBtn.Visibility = Visibility.Visible;
            LapStopwatchBtn.Visibility = Visibility.Hidden;

            {
                var fadeAnimation1 = new DoubleAnimation
                {
                    From = 0,
                    To = 1,
                    Duration = TimeSpan.FromSeconds(swfadespeed),
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseInOut }
                };
                var fadeAnimation2 = new DoubleAnimation
                {
                    From = 0,
                    To = 1,
                    Duration = TimeSpan.FromSeconds(swfadespeed),
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseInOut }
                };
                Storyboard.SetTarget(fadeAnimation1, StartStopwatchBtn);
                Storyboard.SetTargetProperty(fadeAnimation1, new PropertyPath(UIElement.OpacityProperty));
                Storyboard.SetTarget(fadeAnimation2, ResetStopwatchBtn);
                Storyboard.SetTargetProperty(fadeAnimation2, new PropertyPath(UIElement.OpacityProperty));
                var storyboard = new Storyboard();
                storyboard.Children.Add(fadeAnimation1);
                storyboard.Children.Add(fadeAnimation2);
                storyboard.Begin();
            }
        }
        private int LapIndex = 0;
        private async void ResetStopwatchBtn_Click(object sender, RoutedEventArgs e)
        {
            stopwatch.Reset();
            stopwatchTimer.Stop();

            foreach (Card lap in LapStopwatchStack.Children)
            {
                var fadeAnimation = new DoubleAnimation
                {
                    From = 1,
                    To = 0,
                    Duration = TimeSpan.FromSeconds(0.3),
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                };
                Storyboard.SetTarget(fadeAnimation, lap);
                Storyboard.SetTargetProperty(fadeAnimation, new PropertyPath(UIElement.OpacityProperty));
                var storyboard = new Storyboard();
                storyboard.Children.Add(fadeAnimation);
                storyboard.Begin();
            }

            await Task.Delay(300);

            LapStopwatchStack.Children.Clear();
            LapIndex = 0;

            if (StopwatchLapDeployed)
            {
                StopwatchLapDeployed = false;
                StopwatchLapRow.Height = new GridLength(1, GridUnitType.Auto);

                double oldRowLapHeight = StopwatchSubGrid.ActualHeight;
                oldRowLapHeight *= 0.6;
                {
                    var sizeAnimation = new DoubleAnimation
                    {
                        From = oldRowLapHeight,
                        To = 0,
                        Duration = TimeSpan.FromSeconds(0.3),
                        EasingFunction = new QuarticEase { EasingMode = EasingMode.EaseOut }
                    };
                    Storyboard.SetTarget(sizeAnimation, StopwatchLapScrollView);
                    Storyboard.SetTargetProperty(sizeAnimation, new PropertyPath(HeightProperty));
                    var storyboard = new Storyboard();
                    storyboard.Children.Add(sizeAnimation);
                    storyboard.Begin();
                }

                await Task.Delay(300);

                StopwatchLapScrollView.Height = double.NaN;
            }

            UpdateStopwatchTextH1("0");
            UpdateStopwatchTextH2("0");
            UpdateStopwatchTextM1("0");
            UpdateStopwatchTextM2("0");
            UpdateStopwatchTextS1("0");
            UpdateStopwatchTextS2("0");
            UpdateStopwatchTextMS1("0", true);
            UpdateStopwatchTextMS2("0", true);
            StopwatchProgressBar.Value = 0;

            {
                var changeAnimation1 = new DoubleAnimation
                {
                    From = 100,
                    To = 200,
                    Duration = TimeSpan.FromSeconds(swzoomspeed),
                    EasingFunction = new QuarticEase { EasingMode = EasingMode.EaseOut }
                };
                var changeAnimation2 = new DoubleAnimation
                {
                    From = 100,
                    To = 200,
                    Duration = TimeSpan.FromSeconds(swzoomspeed),
                    EasingFunction = new QuarticEase { EasingMode = EasingMode.EaseOut }
                };
                Storyboard.SetTarget(changeAnimation1, StartStopwatchBtn);
                Storyboard.SetTargetProperty(changeAnimation1, new PropertyPath(WidthProperty));
                Storyboard.SetTarget(changeAnimation2, PauseStopwatchBtn);
                Storyboard.SetTargetProperty(changeAnimation2, new PropertyPath(WidthProperty));
                var storyboard = new Storyboard();
                storyboard.Children.Add(changeAnimation1);
                storyboard.Children.Add(changeAnimation2);
                storyboard.Begin();
            }
            {
                var changeAnimation1 = new DoubleAnimation
                {
                    From = 100,
                    To = 0,
                    Duration = TimeSpan.FromSeconds(swzoomspeed),
                    EasingFunction = new QuarticEase { EasingMode = EasingMode.EaseOut }
                };
                var changeAnimation2 = new DoubleAnimation
                {
                    From = 100,
                    To = 0,
                    Duration = TimeSpan.FromSeconds(swzoomspeed),
                    EasingFunction = new QuarticEase { EasingMode = EasingMode.EaseOut }
                };
                Storyboard.SetTarget(changeAnimation1, ResetStopwatchBtn);
                Storyboard.SetTargetProperty(changeAnimation1, new PropertyPath(WidthProperty));
                Storyboard.SetTarget(changeAnimation2, LapStopwatchBtn);
                Storyboard.SetTargetProperty(changeAnimation2, new PropertyPath(WidthProperty));
                var storyboard = new Storyboard();
                storyboard.Children.Add(changeAnimation1);
                storyboard.Children.Add(changeAnimation2);
                storyboard.Begin();
            }
            {
                var changeAnimation1 = new DoubleAnimation
                {
                    From = 20,
                    To = 0,
                    Duration = TimeSpan.FromSeconds(swzoomspeed),
                    EasingFunction = new QuarticEase { EasingMode = EasingMode.EaseOut }
                };
                Storyboard.SetTarget(changeAnimation1, SWButtonSep);
                Storyboard.SetTargetProperty(changeAnimation1, new PropertyPath(WidthProperty));
                var storyboard = new Storyboard();
                storyboard.Children.Add(changeAnimation1);
                storyboard.Begin();
            }

            await Task.Delay(150);

            StartStopwatchBtn.Visibility = Visibility.Visible;
            PauseStopwatchBtn.Visibility = Visibility.Hidden;
            ResetStopwatchBtn.Visibility = Visibility.Hidden;
            LapStopwatchBtn.Visibility = Visibility.Hidden;

            {
                var fadeAnimation1 = new DoubleAnimation
                {
                    From = 0,
                    To = 1,
                    Duration = TimeSpan.FromSeconds(swfadespeed),
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseInOut }
                };
                var fadeAnimation2 = new DoubleAnimation
                {
                    From = 0,
                    To = 1,
                    Duration = TimeSpan.FromSeconds(swfadespeed),
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseInOut }
                };
                Storyboard.SetTarget(fadeAnimation1, PauseStopwatchBtn);
                Storyboard.SetTargetProperty(fadeAnimation1, new PropertyPath(UIElement.OpacityProperty));
                Storyboard.SetTarget(fadeAnimation2, LapStopwatchBtn);
                Storyboard.SetTargetProperty(fadeAnimation2, new PropertyPath(UIElement.OpacityProperty));
                var storyboard = new Storyboard();
                storyboard.Children.Add(fadeAnimation1);
                storyboard.Children.Add(fadeAnimation2);
                storyboard.Begin();
            }

            StopwatchIsRunning = false;
        }
        private async void LapStopwatchBtn_Click(object sender, RoutedEventArgs e)
        {
            LapIndex++;

            if (!StopwatchLapDeployed)
            {
                StopwatchLapDeployed = true;

                double newRowLapHeight = StopwatchSubGrid.ActualHeight;
                newRowLapHeight *= 0.6;
                {
                    var sizeAnimation = new DoubleAnimation
                    {
                        From = 0,
                        To = newRowLapHeight,
                        Duration = TimeSpan.FromSeconds(0.3),
                        EasingFunction = new QuarticEase { EasingMode = EasingMode.EaseOut }
                    };
                    Storyboard.SetTarget(sizeAnimation, StopwatchLapScrollView);
                    Storyboard.SetTargetProperty(sizeAnimation, new PropertyPath(HeightProperty));
                    var storyboard = new Storyboard();
                    storyboard.Children.Add(sizeAnimation);
                    storyboard.Begin();
                }

                await Task.Delay(300);

                StopwatchLapRow.Height = new GridLength(0.6, GridUnitType.Star);
                StopwatchLapScrollView.Height = double.NaN;
            }

            TimeSpan LapTimeGap = stopwatch.Elapsed - LastLapTime;
            LastLapTime = stopwatch.Elapsed;

            Card LapCard = new Card();
            LapCard.Margin = new Thickness(20, 0, 20, 10);

            Grid LapGrid = new Grid();

            System.Windows.Controls.TextBlock LapTextIndex = new System.Windows.Controls.TextBlock();
            LapTextIndex.Text = LapIndex.ToString();
            LapTextIndex.FontSize = 18;
            LapTextIndex.FontFamily = new FontFamily("Segoe UI Variable Display SemiBold");
            LapTextIndex.Foreground = (Brush)FindResource("TextFillColorPrimaryBrush");
            LapTextIndex.HorizontalAlignment = HorizontalAlignment.Left;
            LapTextIndex.VerticalAlignment = VerticalAlignment.Center;

            System.Windows.Controls.TextBlock LapTextGap = new System.Windows.Controls.TextBlock();
            LapTextGap.Text = "+" + LapTimeGap.ToString("hh\\:mm\\:ss\\.ff");
            LapTextGap.FontSize = 18;
            LapTextGap.FontFamily = new FontFamily("Segoe UI Variable Display Regular");
            LapTextGap.Foreground = (Brush)FindResource("TextFillColorSecondaryBrush");
            LapTextGap.HorizontalAlignment = HorizontalAlignment.Center;
            LapTextGap.VerticalAlignment = VerticalAlignment.Center;

            System.Windows.Controls.TextBlock LapTextTotal = new System.Windows.Controls.TextBlock();
            LapTextTotal.Text = stopwatch.Elapsed.ToString("hh\\:mm\\:ss\\.ff");
            LapTextTotal.FontSize = 18;
            LapTextTotal.FontFamily = new FontFamily("Segoe UI Variable Display Regular");
            LapTextTotal.Foreground = (Brush)FindResource("TextFillColorPrimaryBrush");
            LapTextTotal.HorizontalAlignment = HorizontalAlignment.Right;
            LapTextTotal.VerticalAlignment = VerticalAlignment.Center;

            LapGrid.Children.Add(LapTextIndex);
            LapGrid.Children.Add(LapTextGap);
            LapGrid.Children.Add(LapTextTotal);

            LapCard.Content = LapGrid;

            LapStopwatchStack.Children.Insert(0, LapCard);

            LapCard = LapStopwatchStack.Children[0] as Card;
            double newCardHeight = 58;
            LapCard.Opacity = 0;

            {
                var sizeAnimation = new DoubleAnimation
                {
                    From = 0,
                    To = newCardHeight,
                    Duration = TimeSpan.FromSeconds(0.3),
                    EasingFunction = new QuarticEase { EasingMode = EasingMode.EaseOut }
                };
                Storyboard.SetTarget(sizeAnimation, LapCard);
                Storyboard.SetTargetProperty(sizeAnimation, new PropertyPath(HeightProperty));
                var storyboard = new Storyboard();
                storyboard.Children.Add(sizeAnimation);
                storyboard.Begin();
            }

            await Task.Delay(300);

            {
                var fadeAnimation = new DoubleAnimation
                {
                    From = 0,
                    To = 1,
                    Duration = TimeSpan.FromSeconds(0.3),
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                };
                Storyboard.SetTarget(fadeAnimation, LapCard);
                Storyboard.SetTargetProperty(fadeAnimation, new PropertyPath(OpacityProperty));
                var storyboard = new Storyboard();
                storyboard.Children.Add(fadeAnimation);
                storyboard.Begin();
            }
        }
        private async Task UpdateStopwatchTextH1(string text)
        {
            if (ActualStopwatchH1 != text)
            {
                StopwatchTextH1.Text = ActualStopwatchH1;
                ActualStopwatchH1 = text;
                {
                    var translateAnimation = new DoubleAnimation
                    {
                        From = 0,
                        To = -50,
                        Duration = TimeSpan.FromSeconds(swtxtslidespeed),
                        EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn }
                    };
                    Storyboard.SetTarget(translateAnimation, StopwatchTextH1);
                    Storyboard.SetTargetProperty(translateAnimation, new PropertyPath("(UIElement.RenderTransform).(TranslateTransform.Y)"));
                    var storyboard = new Storyboard();
                    storyboard.Children.Add(translateAnimation);
                    storyboard.Begin();
                }
                await Task.Delay(swtxtdelay);
                StopwatchTextH1.Text = text;
                {
                    var translateAnimation = new DoubleAnimation
                    {
                        From = 50,
                        To = 0,
                        Duration = TimeSpan.FromSeconds(swtxtslidespeed),
                        EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                    };
                    Storyboard.SetTarget(translateAnimation, StopwatchTextH1);
                    Storyboard.SetTargetProperty(translateAnimation, new PropertyPath("(UIElement.RenderTransform).(TranslateTransform.Y)"));
                    var storyboard = new Storyboard();
                    storyboard.Children.Add(translateAnimation);
                    storyboard.Begin();
                }
            }
        }
        private async Task UpdateStopwatchTextH2(string text)
        {
            if (ActualStopwatchH2 != text)
            {
                StopwatchTextH2.Text = ActualStopwatchH2;
                ActualStopwatchH2 = text;
                {
                    var translateAnimation = new DoubleAnimation
                    {
                        From = 0,
                        To = -50,
                        Duration = TimeSpan.FromSeconds(swtxtslidespeed),
                        EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn }
                    };
                    Storyboard.SetTarget(translateAnimation, StopwatchTextH2);
                    Storyboard.SetTargetProperty(translateAnimation, new PropertyPath("(UIElement.RenderTransform).(TranslateTransform.Y)"));
                    var storyboard = new Storyboard();
                    storyboard.Children.Add(translateAnimation);
                    storyboard.Begin();
                }
                await Task.Delay(swtxtdelay);
                StopwatchTextH2.Text = text;
                {
                    var translateAnimation = new DoubleAnimation
                    {
                        From = 50,
                        To = 0,
                        Duration = TimeSpan.FromSeconds(swtxtslidespeed),
                        EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                    };
                    Storyboard.SetTarget(translateAnimation, StopwatchTextH2);
                    Storyboard.SetTargetProperty(translateAnimation, new PropertyPath("(UIElement.RenderTransform).(TranslateTransform.Y)"));
                    var storyboard = new Storyboard();
                    storyboard.Children.Add(translateAnimation);
                    storyboard.Begin();
                }
            }
        }
        private async Task UpdateStopwatchTextM1(string text)
        {
            if (ActualStopwatchM1 != text)
            {
                StopwatchTextM1.Text = ActualStopwatchM1;
                ActualStopwatchM1 = text;
                {
                    var translateAnimation = new DoubleAnimation
                    {
                        From = 0,
                        To = -50,
                        Duration = TimeSpan.FromSeconds(swtxtslidespeed),
                        EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn }
                    };
                    Storyboard.SetTarget(translateAnimation, StopwatchTextM1);
                    Storyboard.SetTargetProperty(translateAnimation, new PropertyPath("(UIElement.RenderTransform).(TranslateTransform.Y)"));
                    var storyboard = new Storyboard();
                    storyboard.Children.Add(translateAnimation);
                    storyboard.Begin();
                }
                await Task.Delay(swtxtdelay);
                StopwatchTextM1.Text = text;
                {
                    var translateAnimation = new DoubleAnimation
                    {
                        From = 50,
                        To = 0,
                        Duration = TimeSpan.FromSeconds(swtxtslidespeed),
                        EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                    };
                    Storyboard.SetTarget(translateAnimation, StopwatchTextM1);
                    Storyboard.SetTargetProperty(translateAnimation, new PropertyPath("(UIElement.RenderTransform).(TranslateTransform.Y)"));
                    var storyboard = new Storyboard();
                    storyboard.Children.Add(translateAnimation);
                    storyboard.Begin();
                }
            }
        }
        private async Task UpdateStopwatchTextM2(string text)
        {
            if (ActualStopwatchM2 != text)
            {
                StopwatchTextM2.Text = ActualStopwatchM2;
                ActualStopwatchM2 = text;
                {
                    var translateAnimation = new DoubleAnimation
                    {
                        From = 0,
                        To = -50,
                        Duration = TimeSpan.FromSeconds(swtxtslidespeed),
                        EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn }
                    };
                    Storyboard.SetTarget(translateAnimation, StopwatchTextM2);
                    Storyboard.SetTargetProperty(translateAnimation, new PropertyPath("(UIElement.RenderTransform).(TranslateTransform.Y)"));
                    var storyboard = new Storyboard();
                    storyboard.Children.Add(translateAnimation);
                    storyboard.Begin();
                }
                await Task.Delay(swtxtdelay);
                StopwatchTextM2.Text = text;
                {
                    var translateAnimation = new DoubleAnimation
                    {
                        From = 50,
                        To = 0,
                        Duration = TimeSpan.FromSeconds(swtxtslidespeed),
                        EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                    };
                    Storyboard.SetTarget(translateAnimation, StopwatchTextM2);
                    Storyboard.SetTargetProperty(translateAnimation, new PropertyPath("(UIElement.RenderTransform).(TranslateTransform.Y)"));
                    var storyboard = new Storyboard();
                    storyboard.Children.Add(translateAnimation);
                    storyboard.Begin();
                }
            }
        }
        private async Task UpdateStopwatchTextS1(string text)
        {
            if (ActualStopwatchS1 != text)
            {
                StopwatchTextS1.Text = ActualStopwatchS1;
                ActualStopwatchS1 = text;
                {
                    var translateAnimation = new DoubleAnimation
                    {
                        From = 0,
                        To = -50,
                        Duration = TimeSpan.FromSeconds(swtxtslidespeed),
                        EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn }
                    };
                    Storyboard.SetTarget(translateAnimation, StopwatchTextS1);
                    Storyboard.SetTargetProperty(translateAnimation, new PropertyPath("(UIElement.RenderTransform).(TranslateTransform.Y)"));
                    var storyboard = new Storyboard();
                    storyboard.Children.Add(translateAnimation);
                    storyboard.Begin();
                }
                await Task.Delay(swtxtdelay);
                StopwatchTextS1.Text = text;
                {
                    var translateAnimation = new DoubleAnimation
                    {
                        From = 50,
                        To = 0,
                        Duration = TimeSpan.FromSeconds(swtxtslidespeed),
                        EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                    };
                    Storyboard.SetTarget(translateAnimation, StopwatchTextS1);
                    Storyboard.SetTargetProperty(translateAnimation, new PropertyPath("(UIElement.RenderTransform).(TranslateTransform.Y)"));
                    var storyboard = new Storyboard();
                    storyboard.Children.Add(translateAnimation);
                    storyboard.Begin();
                }
            }
        }
        private async Task UpdateStopwatchTextS2(string text)
        {
            if (ActualStopwatchS2 != text)
            {
                StopwatchTextS2.Text = ActualStopwatchS2;
                ActualStopwatchS2 = text;
                {
                    var translateAnimation = new DoubleAnimation
                    {
                        From = 0,
                        To = -50,
                        Duration = TimeSpan.FromSeconds(swtxtslidespeed),
                        EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn }
                    };
                    Storyboard.SetTarget(translateAnimation, StopwatchTextS2);
                    Storyboard.SetTargetProperty(translateAnimation, new PropertyPath("(UIElement.RenderTransform).(TranslateTransform.Y)"));
                    var storyboard = new Storyboard();
                    storyboard.Children.Add(translateAnimation);
                    storyboard.Begin();
                }
                await Task.Delay(swtxtdelay);
                StopwatchTextS2.Text = text;
                {
                    var translateAnimation = new DoubleAnimation
                    {
                        From = 50,
                        To = 0,
                        Duration = TimeSpan.FromSeconds(swtxtslidespeed),
                        EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                    };
                    Storyboard.SetTarget(translateAnimation, StopwatchTextS2);
                    Storyboard.SetTargetProperty(translateAnimation, new PropertyPath("(UIElement.RenderTransform).(TranslateTransform.Y)"));
                    var storyboard = new Storyboard();
                    storyboard.Children.Add(translateAnimation);
                    storyboard.Begin();
                }
            }
        }
        private async Task UpdateStopwatchTextMS1(string text, bool reset)
        {
            if (ActualStopwatchMS1 != text)
            {
                StopwatchTextMS1.Text = ActualStopwatchMS1;
                if (reset)
                {
                    {
                        var translateAnimation = new DoubleAnimation
                        {
                            From = 0,
                            To = -50,
                            Duration = TimeSpan.FromSeconds(swtxtslidespeed),
                            EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn }
                        };
                        Storyboard.SetTarget(translateAnimation, StopwatchTextMS1);
                        Storyboard.SetTargetProperty(translateAnimation, new PropertyPath("(UIElement.RenderTransform).(TranslateTransform.Y)"));
                        var storyboard = new Storyboard();
                        storyboard.Children.Add(translateAnimation);
                        storyboard.Begin();
                    }
                    await Task.Delay(swtxtdelay);
                    StopwatchTextMS1.Text = text;
                    {
                        var translateAnimation = new DoubleAnimation
                        {
                            From = 50,
                            To = 0,
                            Duration = TimeSpan.FromSeconds(swtxtslidespeed),
                            EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                        };
                        Storyboard.SetTarget(translateAnimation, StopwatchTextMS1);
                        Storyboard.SetTargetProperty(translateAnimation, new PropertyPath("(UIElement.RenderTransform).(TranslateTransform.Y)"));
                        var storyboard = new Storyboard();
                        storyboard.Children.Add(translateAnimation);
                        storyboard.Begin();
                    }
                }
                StopwatchTextMS1.Text = text;
                ActualStopwatchMS1 = text;
            }
        }
        private async Task UpdateStopwatchTextMS2(string text, bool reset)
        {
            if (ActualStopwatchMS2 != text)
            {
                StopwatchTextMS2.Text = ActualStopwatchMS2;
                if (reset)
                {
                    {
                        var translateAnimation = new DoubleAnimation
                        {
                            From = 0,
                            To = -50,
                            Duration = TimeSpan.FromSeconds(swtxtslidespeed),
                            EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn }
                        };
                        Storyboard.SetTarget(translateAnimation, StopwatchTextMS2);
                        Storyboard.SetTargetProperty(translateAnimation, new PropertyPath("(UIElement.RenderTransform).(TranslateTransform.Y)"));
                        var storyboard = new Storyboard();
                        storyboard.Children.Add(translateAnimation);
                        storyboard.Begin();
                    }
                    await Task.Delay(swtxtdelay);
                    StopwatchTextMS2.Text = text;
                    {
                        var translateAnimation = new DoubleAnimation
                        {
                            From = 50,
                            To = 0,
                            Duration = TimeSpan.FromSeconds(swtxtslidespeed),
                            EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                        };
                        Storyboard.SetTarget(translateAnimation, StopwatchTextMS2);
                        Storyboard.SetTargetProperty(translateAnimation, new PropertyPath("(UIElement.RenderTransform).(TranslateTransform.Y)"));
                        var storyboard = new Storyboard();
                        storyboard.Children.Add(translateAnimation);
                        storyboard.Begin();
                    }
                }
                StopwatchTextMS2.Text = text;
                ActualStopwatchMS2 = text;
            }
        }


        private string ActualTimerH1 = "0";
        private string ActualTimerH2 = "0";
        private string ActualTimerM1 = "0";
        private string ActualTimerM2 = "0";
        private string ActualTimerS1 = "0";
        private string ActualTimerS2 = "0";
        private int ActualTimeProgress = 100;
        private TimeSpan timerSet = TimeSpan.Zero;
        private bool TimerIsRunning = false;
        private double tfadespeed = 0.30;
        private double tzoomspeed = 0.30;
        private bool TimeUp = false;
        private SoundPlayer TimeUpSoundPlayer;
        private async void StartTimerBtn_Click(object sender, RoutedEventArgs e)
        {
            if (!TimerIsRunning)
            {
                TimerProgressRing.Progress = 100;
                timer = TimeSpan.Zero;
                timerSet = TimeSpan.Zero;
                timerSet = timerSet.Add(TimeSpan.FromHours(int.Parse(ActualTimerH1) * 10 + int.Parse(ActualTimerH2)));
                timerSet = timerSet.Add(TimeSpan.FromMinutes(int.Parse(ActualTimerM1) * 10 + int.Parse(ActualTimerM2)));
                timerSet = timerSet.Add(TimeSpan.FromSeconds(int.Parse(ActualTimerS1) * 10 + int.Parse(ActualTimerS2)));
                if (timerSet != TimeSpan.Zero)
                {
                    timer = timer.Add(timerSet);
                    timerTimer.Start();
                    StartTimerBtn.Visibility = Visibility.Hidden;
                    PauseTimerBtn.Visibility = Visibility.Visible;
                    ResetTimerBtn.Visibility = Visibility.Visible;
                    PauseTimerBtn.Opacity = 1;
                    {
                        var changeAnimation1 = new DoubleAnimation
                        {
                            From = 200,
                            To = 100,
                            Duration = TimeSpan.FromSeconds(tzoomspeed),
                            EasingFunction = new QuarticEase { EasingMode = EasingMode.EaseOut }
                        };
                        var changeAnimation2 = new DoubleAnimation
                        {
                            From = 200,
                            To = 100,
                            Duration = TimeSpan.FromSeconds(tzoomspeed),
                            EasingFunction = new QuarticEase { EasingMode = EasingMode.EaseOut }
                        };
                        Storyboard.SetTarget(changeAnimation1, StartTimerBtn);
                        Storyboard.SetTargetProperty(changeAnimation1, new PropertyPath(WidthProperty));
                        Storyboard.SetTarget(changeAnimation2, PauseTimerBtn);
                        Storyboard.SetTargetProperty(changeAnimation2, new PropertyPath(WidthProperty));
                        var storyboard = new Storyboard();
                        storyboard.Children.Add(changeAnimation1);
                        storyboard.Children.Add(changeAnimation2);
                        storyboard.Begin();
                    }
                    {
                        var changeAnimation1 = new DoubleAnimation
                        {
                            From = 0,
                            To = 100,
                            Duration = TimeSpan.FromSeconds(tzoomspeed),
                            EasingFunction = new QuarticEase { EasingMode = EasingMode.EaseOut }
                        };
                        Storyboard.SetTarget(changeAnimation1, ResetTimerBtn);
                        Storyboard.SetTargetProperty(changeAnimation1, new PropertyPath(WidthProperty));
                        var storyboard = new Storyboard();
                        storyboard.Children.Add(changeAnimation1);
                        storyboard.Begin();
                    }
                    {
                        var changeAnimation1 = new DoubleAnimation
                        {
                            From = 0,
                            To = 20,
                            Duration = TimeSpan.FromSeconds(tzoomspeed),
                            EasingFunction = new QuarticEase { EasingMode = EasingMode.EaseOut }
                        };
                        Storyboard.SetTarget(changeAnimation1, TButtonSep);
                        Storyboard.SetTargetProperty(changeAnimation1, new PropertyPath(WidthProperty));
                        var storyboard = new Storyboard();
                        storyboard.Children.Add(changeAnimation1);
                        storyboard.Begin();
                    }
                    {
                        var zommAnimation1 = new DoubleAnimation
                        {
                            From = 0,
                            To = 1,
                            Duration = TimeSpan.FromSeconds(tzoomspeed),
                            EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                        };
                        var zommAnimation2 = new DoubleAnimation
                        {
                            From = 0,
                            To = 1,
                            Duration = TimeSpan.FromSeconds(tzoomspeed),
                            EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                        };
                        var fadeAnimation1 = new DoubleAnimation
                        {
                            From = 0,
                            To = 1,
                            Duration = TimeSpan.FromSeconds(tfadespeed),
                            EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn }
                        };
                        Storyboard.SetTarget(zommAnimation1, TimerProgressRing);
                        Storyboard.SetTargetProperty(zommAnimation1, new PropertyPath("(UIElement.RenderTransform).(ScaleTransform.ScaleX)"));
                        Storyboard.SetTarget(zommAnimation2, TimerProgressRing);
                        Storyboard.SetTargetProperty(zommAnimation2, new PropertyPath("(UIElement.RenderTransform).(ScaleTransform.ScaleY)"));
                        Storyboard.SetTarget(fadeAnimation1, TimerProgressRing);
                        Storyboard.SetTargetProperty(fadeAnimation1, new PropertyPath(OpacityProperty));
                        var storyboard = new Storyboard();
                        storyboard.Children.Add(zommAnimation1);
                        storyboard.Children.Add(zommAnimation2);
                        storyboard.Children.Add(fadeAnimation1);
                        storyboard.Begin();
                    }
                    foreach (UIElement element in TimerwBtnGrid.Children)
                    {
                        if (element is System.Windows.Controls.Button)
                        {
                            var zoomAnimation1 = new DoubleAnimation
                            {
                                From = 31,
                                To = 0,
                                Duration = TimeSpan.FromSeconds(tzoomspeed),
                                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                            };
                            Storyboard.SetTarget(zoomAnimation1, element);
                            Storyboard.SetTargetProperty(zoomAnimation1, new PropertyPath(HeightProperty));
                            var storyboard = new Storyboard();
                            storyboard.Children.Add(zoomAnimation1);
                            storyboard.Begin();
                        }
                    }
                    TimerIsRunning = true;
                }
            }
            else
            {
                timerTimer.Start();
                {
                    var fadeAnimation1 = new DoubleAnimation
                    {
                        From = 1,
                        To = 0,
                        Duration = TimeSpan.FromSeconds(tfadespeed),
                        EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseInOut }
                    };
                    var fadeAnimation2 = new DoubleAnimation
                    {
                        From = 1,
                        To = 0,
                        Duration = TimeSpan.FromSeconds(tfadespeed),
                        EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseInOut }
                    };
                    Storyboard.SetTarget(fadeAnimation1, StartTimerBtn);
                    Storyboard.SetTargetProperty(fadeAnimation1, new PropertyPath(UIElement.OpacityProperty));
                    Storyboard.SetTarget(fadeAnimation2, ResetTimerBtn);
                    Storyboard.SetTargetProperty(fadeAnimation2, new PropertyPath(UIElement.OpacityProperty));
                    var storyboard = new Storyboard();
                    storyboard.Children.Add(fadeAnimation1);
                    storyboard.Children.Add(fadeAnimation2);
                    storyboard.Begin();
                }
                await Task.Delay(150);
                StartTimerBtn.Visibility = Visibility.Hidden;
                PauseTimerBtn.Visibility = Visibility.Visible;
                ResetTimerBtn.Visibility = Visibility.Visible;
                {
                    var fadeAnimation1 = new DoubleAnimation
                    {
                        From = 0,
                        To = 1,
                        Duration = TimeSpan.FromSeconds(tfadespeed),
                        EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseInOut }
                    };
                    var fadeAnimation2 = new DoubleAnimation
                    {
                        From = 0,
                        To = 1,
                        Duration = TimeSpan.FromSeconds(tfadespeed),
                        EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseInOut }
                    };
                    var fadeAnimation3 = new DoubleAnimation
                    {
                        From = 0,
                        To = 1,
                        Duration = TimeSpan.FromSeconds(tfadespeed),
                        EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseInOut }
                    };
                    Storyboard.SetTarget(fadeAnimation1, PauseTimerBtn);
                    Storyboard.SetTargetProperty(fadeAnimation1, new PropertyPath(UIElement.OpacityProperty));
                    Storyboard.SetTarget(fadeAnimation2, ResetTimerBtn);
                    Storyboard.SetTargetProperty(fadeAnimation2, new PropertyPath(UIElement.OpacityProperty));
                    Storyboard.SetTarget(fadeAnimation3, StartTimerBtn);
                    Storyboard.SetTargetProperty(fadeAnimation3, new PropertyPath(UIElement.OpacityProperty));
                    var storyboard = new Storyboard();
                    storyboard.Children.Add(fadeAnimation1);
                    storyboard.Children.Add(fadeAnimation2);
                    storyboard.Children.Add(fadeAnimation3);
                    storyboard.Begin();
                }
                TimerIsRunning = true;
            }
        }
        private async void PauseTimerBtn_Click(object sender, RoutedEventArgs e)
        {
            timerTimer.Stop();
            {
                var fadeAnimation1 = new DoubleAnimation
                {
                    From = 1,
                    To = 0,
                    Duration = TimeSpan.FromSeconds(tfadespeed),
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseInOut }
                };
                var fadeAnimation2 = new DoubleAnimation
                {
                    From = 1,
                    To = 0,
                    Duration = TimeSpan.FromSeconds(tfadespeed),
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseInOut }
                };
                Storyboard.SetTarget(fadeAnimation1, PauseTimerBtn);
                Storyboard.SetTargetProperty(fadeAnimation1, new PropertyPath(UIElement.OpacityProperty));
                Storyboard.SetTarget(fadeAnimation2, ResetTimerBtn);
                Storyboard.SetTargetProperty(fadeAnimation2, new PropertyPath(UIElement.OpacityProperty));
                var storyboard = new Storyboard();
                storyboard.Children.Add(fadeAnimation1);
                storyboard.Children.Add(fadeAnimation2);
                storyboard.Begin();
            }
            await Task.Delay(150);
            StartTimerBtn.Visibility = Visibility.Visible;
            PauseTimerBtn.Visibility = Visibility.Hidden;
            ResetTimerBtn.Visibility = Visibility.Visible;
            {
                var fadeAnimation1 = new DoubleAnimation
                {
                    From = 0,
                    To = 1,
                    Duration = TimeSpan.FromSeconds(tfadespeed),
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseInOut }
                };
                var fadeAnimation2 = new DoubleAnimation
                {
                    From = 0,
                    To = 1,
                    Duration = TimeSpan.FromSeconds(tfadespeed),
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseInOut }
                };
                var fadeAnimation3 = new DoubleAnimation
                {
                    From = 0,
                    To = 1,
                    Duration = TimeSpan.FromSeconds(tfadespeed),
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseInOut }
                };
                Storyboard.SetTarget(fadeAnimation1, StartTimerBtn);
                Storyboard.SetTargetProperty(fadeAnimation1, new PropertyPath(UIElement.OpacityProperty));
                Storyboard.SetTarget(fadeAnimation2, ResetTimerBtn);
                Storyboard.SetTargetProperty(fadeAnimation2, new PropertyPath(UIElement.OpacityProperty));
                Storyboard.SetTarget(fadeAnimation3, PauseTimerBtn);
                Storyboard.SetTargetProperty(fadeAnimation3, new PropertyPath(UIElement.OpacityProperty));
                var storyboard = new Storyboard();
                storyboard.Children.Add(fadeAnimation1);
                storyboard.Children.Add(fadeAnimation2);
                storyboard.Children.Add(fadeAnimation3);
                storyboard.Begin();
            }
        }
        private async void ResetTimerBtn_Click(object sender, RoutedEventArgs e)
        {
            timerTimer.Stop();
            timer = TimeSpan.Zero;
            UpdateTimerTextH1((timerSet.Hours / 10).ToString(), true);
            UpdateTimerTextH2((timerSet.Hours % 10).ToString(), true);
            UpdateTimerTextM1((timerSet.Minutes / 10).ToString(), true);
            UpdateTimerTextM2((timerSet.Minutes % 10).ToString(), true);
            UpdateTimerTextS1((timerSet.Seconds / 10).ToString(), true);
            UpdateTimerTextS2((timerSet.Seconds % 10).ToString(), true);
            UpdateTimerProgressRing(100);
            StartTimerBtn.Visibility = Visibility.Visible;
            TimerIsRunning = false;
            {
                var changeAnimation1 = new DoubleAnimation
                {
                    From = 100,
                    To = 200,
                    Duration = TimeSpan.FromSeconds(tzoomspeed),
                    EasingFunction = new QuarticEase { EasingMode = EasingMode.EaseOut }
                };
                var changeAnimation2 = new DoubleAnimation
                {
                    From = 100,
                    To = 200,
                    Duration = TimeSpan.FromSeconds(tzoomspeed),
                    EasingFunction = new QuarticEase { EasingMode = EasingMode.EaseOut }
                };
                Storyboard.SetTarget(changeAnimation1, StartTimerBtn);
                Storyboard.SetTargetProperty(changeAnimation1, new PropertyPath(WidthProperty));
                Storyboard.SetTarget(changeAnimation2, PauseTimerBtn);
                Storyboard.SetTargetProperty(changeAnimation2, new PropertyPath(WidthProperty));
                var storyboard = new Storyboard();
                storyboard.Children.Add(changeAnimation1);
                storyboard.Children.Add(changeAnimation2);
                storyboard.Begin();
            }
            {
                var changeAnimation1 = new DoubleAnimation
                {
                    From = 100,
                    To = 0,
                    Duration = TimeSpan.FromSeconds(tzoomspeed),
                    EasingFunction = new QuarticEase { EasingMode = EasingMode.EaseOut }
                };
                Storyboard.SetTarget(changeAnimation1, ResetTimerBtn);
                Storyboard.SetTargetProperty(changeAnimation1, new PropertyPath(WidthProperty));
                var storyboard = new Storyboard();
                storyboard.Children.Add(changeAnimation1);
                storyboard.Begin();
            }
            {
                var changeAnimation1 = new DoubleAnimation
                {
                    From = 20,
                    To = 0,
                    Duration = TimeSpan.FromSeconds(tzoomspeed),
                    EasingFunction = new QuarticEase { EasingMode = EasingMode.EaseOut }
                };
                Storyboard.SetTarget(changeAnimation1, TButtonSep);
                Storyboard.SetTargetProperty(changeAnimation1, new PropertyPath(WidthProperty));
                var storyboard = new Storyboard();
                storyboard.Children.Add(changeAnimation1);
                storyboard.Begin();
            }

            await Task.Delay(150);

            PauseTimerBtn.Visibility = Visibility.Hidden;
            ResetTimerBtn.Visibility = Visibility.Hidden;

            {
                var zommAnimation1 = new DoubleAnimation
                {
                    From = 1,
                    To = 0,
                    Duration = TimeSpan.FromSeconds(tzoomspeed),
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn }
                };
                var zommAnimation2 = new DoubleAnimation
                {
                    From = 1,
                    To = 0,
                    Duration = TimeSpan.FromSeconds(tzoomspeed),
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn }
                };
                var fadeAnimation1 = new DoubleAnimation
                {
                    From = 1,
                    To = 0,
                    Duration = TimeSpan.FromSeconds(tfadespeed),
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                };
                Storyboard.SetTarget(zommAnimation1, TimerProgressRing);
                Storyboard.SetTargetProperty(zommAnimation1, new PropertyPath("(UIElement.RenderTransform).(ScaleTransform.ScaleX)"));
                Storyboard.SetTarget(zommAnimation2, TimerProgressRing);
                Storyboard.SetTargetProperty(zommAnimation2, new PropertyPath("(UIElement.RenderTransform).(ScaleTransform.ScaleY)"));
                Storyboard.SetTarget(fadeAnimation1, TimerProgressRing);
                Storyboard.SetTargetProperty(fadeAnimation1, new PropertyPath(OpacityProperty));
                var storyboard = new Storyboard();
                storyboard.Children.Add(zommAnimation1);
                storyboard.Children.Add(zommAnimation2);
                storyboard.Children.Add(fadeAnimation1);
                storyboard.Begin();
            }
            foreach (UIElement element in TimerwBtnGrid.Children)
            {
                if (element is System.Windows.Controls.Button)
                {
                    var zoomAnimation1 = new DoubleAnimation
                    {
                        From = 0,
                        To = 31,
                        Duration = TimeSpan.FromSeconds(tzoomspeed),
                        EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                    };
                    Storyboard.SetTarget(zoomAnimation1, element);
                    Storyboard.SetTargetProperty(zoomAnimation1, new PropertyPath(HeightProperty));
                    var storyboard = new Storyboard();
                    storyboard.Children.Add(zoomAnimation1);
                    storyboard.Begin();
                }
            }
        }
        private async void Timer_Tick(object sender, EventArgs e)
        {
            if (timer > TimeSpan.Zero)
            {
                timer = timer.Subtract(TimeSpan.FromSeconds(1));
                UpdateTimerTextH1((timer.Hours / 10).ToString(), false);
                UpdateTimerTextH2((timer.Hours % 10).ToString(), false);
                UpdateTimerTextM1((timer.Minutes / 10).ToString(), false);
                UpdateTimerTextM2((timer.Minutes % 10).ToString(), false);
                UpdateTimerTextS1((timer.Seconds / 10).ToString(), false);
                UpdateTimerTextS2((timer.Seconds % 10).ToString(), false);
                UpdateTimerProgressRing((int)(100 * (timerSet.TotalSeconds - (timerSet.TotalSeconds - timer.TotalSeconds)) / timerSet.TotalSeconds));
            }
            else
            {
                timerTimer.Stop();
                TimeUp = true;
                TimeUpShow();
            }
        }
        private async Task TimeUpShow()
        {
            TimeUpSoundPlayer = new SoundPlayer(configs.DefaultTimeUpSound);
            TimeUpSoundPlayer.Load();
            TimeUpSoundPlayer.PlayLooping();
            TimeUpGrid.Visibility = Visibility.Visible;
            {
                var fadeAnimation1 = new DoubleAnimation
                {
                    From = 0,
                    To = 1,
                    Duration = TimeSpan.FromSeconds(tfadespeed),
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn }
                };
                var zoomAnimation1 = new DoubleAnimation
                {
                    From = 1.1,
                    To = 1,
                    Duration = TimeSpan.FromSeconds(tzoomspeed),
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                };
                var zoomAnimation2 = new DoubleAnimation
                {
                    From = 1.1,
                    To = 1,
                    Duration = TimeSpan.FromSeconds(tzoomspeed),
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                };
                Storyboard.SetTarget(fadeAnimation1, TimeUpGrid);
                Storyboard.SetTargetProperty(fadeAnimation1, new PropertyPath(OpacityProperty));
                Storyboard.SetTarget(zoomAnimation1, TimeUpGrid);
                Storyboard.SetTargetProperty(zoomAnimation1, new PropertyPath("(UIElement.RenderTransform).(ScaleTransform.ScaleX)"));
                Storyboard.SetTarget(zoomAnimation2, TimeUpGrid);
                Storyboard.SetTargetProperty(zoomAnimation2, new PropertyPath("(UIElement.RenderTransform).(ScaleTransform.ScaleY)"));
                var storyboard = new Storyboard();
                storyboard.Children.Add(fadeAnimation1);
                storyboard.Children.Add(zoomAnimation1);
                storyboard.Children.Add(zoomAnimation2);
                storyboard.Begin();
            }
            while (TimeUp)
            {
                await Task.Delay(100);
            }
            {
                var fadeAnimation1 = new DoubleAnimation
                {
                    From = 1,
                    To = 0,
                    Duration = TimeSpan.FromSeconds(tfadespeed),
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                };
                var zoomAnimation1 = new DoubleAnimation
                {
                    From = 1,
                    To = 1.1,
                    Duration = TimeSpan.FromSeconds(tzoomspeed),
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn }
                };
                var zoomAnimation2 = new DoubleAnimation
                {
                    From = 1,
                    To = 1.1,
                    Duration = TimeSpan.FromSeconds(tzoomspeed),
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn }
                };
                Storyboard.SetTarget(fadeAnimation1, TimeUpGrid);
                Storyboard.SetTargetProperty(fadeAnimation1, new PropertyPath(OpacityProperty));
                Storyboard.SetTarget(zoomAnimation1, TimeUpGrid);
                Storyboard.SetTargetProperty(zoomAnimation1, new PropertyPath("(UIElement.RenderTransform).(ScaleTransform.ScaleX)"));
                Storyboard.SetTarget(zoomAnimation2, TimeUpGrid);
                Storyboard.SetTargetProperty(zoomAnimation2, new PropertyPath("(UIElement.RenderTransform).(ScaleTransform.ScaleY)"));
                var storyboard = new Storyboard();
                storyboard.Children.Add(fadeAnimation1);
                storyboard.Children.Add(zoomAnimation1);
                storyboard.Children.Add(zoomAnimation2);
                storyboard.Begin();
            }
            TimeUpSoundPlayer.Stop();
            TimeUpSoundPlayer.Dispose();
            timer = TimeSpan.Zero;
            UpdateTimerTextH1((timerSet.Hours / 10).ToString(), true);
            UpdateTimerTextH2((timerSet.Hours % 10).ToString(), true);
            UpdateTimerTextM1((timerSet.Minutes / 10).ToString(), true);
            UpdateTimerTextM2((timerSet.Minutes % 10).ToString(), true);
            UpdateTimerTextS1((timerSet.Seconds / 10).ToString(), true);
            UpdateTimerTextS2((timerSet.Seconds % 10).ToString(), true);
            UpdateTimerProgressRing(100);
            StartTimerBtn.Visibility = Visibility.Visible;
            TimerIsRunning = false;
            {
                var changeAnimation1 = new DoubleAnimation
                {
                    From = 100,
                    To = 200,
                    Duration = TimeSpan.FromSeconds(tzoomspeed),
                    EasingFunction = new QuarticEase { EasingMode = EasingMode.EaseOut }
                };
                var changeAnimation2 = new DoubleAnimation
                {
                    From = 100,
                    To = 200,
                    Duration = TimeSpan.FromSeconds(tzoomspeed),
                    EasingFunction = new QuarticEase { EasingMode = EasingMode.EaseOut }
                };
                Storyboard.SetTarget(changeAnimation1, StartTimerBtn);
                Storyboard.SetTargetProperty(changeAnimation1, new PropertyPath(WidthProperty));
                Storyboard.SetTarget(changeAnimation2, PauseTimerBtn);
                Storyboard.SetTargetProperty(changeAnimation2, new PropertyPath(WidthProperty));
                var storyboard = new Storyboard();
                storyboard.Children.Add(changeAnimation1);
                storyboard.Children.Add(changeAnimation2);
                storyboard.Begin();
            }
            {
                var changeAnimation1 = new DoubleAnimation
                {
                    From = 100,
                    To = 0,
                    Duration = TimeSpan.FromSeconds(tzoomspeed),
                    EasingFunction = new QuarticEase { EasingMode = EasingMode.EaseOut }
                };
                Storyboard.SetTarget(changeAnimation1, ResetTimerBtn);
                Storyboard.SetTargetProperty(changeAnimation1, new PropertyPath(WidthProperty));
                var storyboard = new Storyboard();
                storyboard.Children.Add(changeAnimation1);
                storyboard.Begin();
            }
            {
                var changeAnimation1 = new DoubleAnimation
                {
                    From = 20,
                    To = 0,
                    Duration = TimeSpan.FromSeconds(tzoomspeed),
                    EasingFunction = new QuarticEase { EasingMode = EasingMode.EaseOut }
                };
                Storyboard.SetTarget(changeAnimation1, TButtonSep);
                Storyboard.SetTargetProperty(changeAnimation1, new PropertyPath(WidthProperty));
                var storyboard = new Storyboard();
                storyboard.Children.Add(changeAnimation1);
                storyboard.Begin();
            }

            await Task.Delay(300);

            PauseTimerBtn.Visibility = Visibility.Hidden;
            ResetTimerBtn.Visibility = Visibility.Hidden;

            {
                var zommAnimation1 = new DoubleAnimation
                {
                    From = 1,
                    To = 0,
                    Duration = TimeSpan.FromSeconds(tzoomspeed),
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn }
                };
                var zommAnimation2 = new DoubleAnimation
                {
                    From = 1,
                    To = 0,
                    Duration = TimeSpan.FromSeconds(tzoomspeed),
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn }
                };
                var fadeAnimation1 = new DoubleAnimation
                {
                    From = 1,
                    To = 0,
                    Duration = TimeSpan.FromSeconds(tfadespeed),
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                };
                Storyboard.SetTarget(zommAnimation1, TimerProgressRing);
                Storyboard.SetTargetProperty(zommAnimation1, new PropertyPath("(UIElement.RenderTransform).(ScaleTransform.ScaleX)"));
                Storyboard.SetTarget(zommAnimation2, TimerProgressRing);
                Storyboard.SetTargetProperty(zommAnimation2, new PropertyPath("(UIElement.RenderTransform).(ScaleTransform.ScaleY)"));
                Storyboard.SetTarget(fadeAnimation1, TimerProgressRing);
                Storyboard.SetTargetProperty(fadeAnimation1, new PropertyPath(OpacityProperty));
                var storyboard = new Storyboard();
                storyboard.Children.Add(zommAnimation1);
                storyboard.Children.Add(zommAnimation2);
                storyboard.Children.Add(fadeAnimation1);
                storyboard.Begin();
            }
            foreach (UIElement element in TimerwBtnGrid.Children)
            {
                if (element is System.Windows.Controls.Button)
                {
                    var zoomAnimation1 = new DoubleAnimation
                    {
                        From = 0,
                        To = 31,
                        Duration = TimeSpan.FromSeconds(tzoomspeed),
                        EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                    };
                    Storyboard.SetTarget(zoomAnimation1, element);
                    Storyboard.SetTargetProperty(zoomAnimation1, new PropertyPath(HeightProperty));
                    var storyboard = new Storyboard();
                    storyboard.Children.Add(zoomAnimation1);
                    storyboard.Begin();
                }
            }
            TimeUpGrid.Visibility = Visibility.Hidden;
        }
        private async void TimeUpStopBtn_Click(object sender, EventArgs e)
        {
            TimeUp = false;
        }
        private async void TimerHourUpBtn_Click(object sender, RoutedEventArgs e)
        {
            if (ActualTimerH1 == "2" && ActualTimerH2 == "3")
            {
                UpdateTimerTextH1("0", false);
                UpdateTimerTextH2("0", false);
            }
            else if (ActualTimerH2 == "9")
            {
                UpdateTimerTextH2("0", false);
                UpdateTimerTextH1((int.Parse(ActualTimerH1) + 1).ToString(), false);
            }
            else
            {
                UpdateTimerTextH2((int.Parse(ActualTimerH2) + 1).ToString(), false);
            }
        }
        private async void TimerHourDownBtn_Click(object sender, RoutedEventArgs e)
        {
            if (ActualTimerH1 == "0" && ActualTimerH2 == "0")
            {
                UpdateTimerTextH1("2", false);
                UpdateTimerTextH2("3", false);
            }
            else if (ActualTimerH2 == "0")
            {
                UpdateTimerTextH2("9", false);
                UpdateTimerTextH1((int.Parse(ActualTimerH1) - 1).ToString(), false);
            }
            else
            {
                UpdateTimerTextH2((int.Parse(ActualTimerH2) - 1).ToString(), false);
            }
        }
        private async void TimerMinuteUpBtn_Click(object sender, RoutedEventArgs e)
        {
            if (ActualTimerM1 == "5" && ActualTimerM2 == "9")
            {
                UpdateTimerTextM1("0", false);
                UpdateTimerTextM2("0", false);
            }
            else if (ActualTimerM2 == "9")
            {
                UpdateTimerTextM2("0", false);
                UpdateTimerTextM1((int.Parse(ActualTimerM1) + 1).ToString(), false);
            }
            else
            {
                UpdateTimerTextM2((int.Parse(ActualTimerM2) + 1).ToString(), false);
            }
        }
        private async void TimerMinuteDownBtn_Click(object sender, RoutedEventArgs e)
        {
            if (ActualTimerM1 == "0" && ActualTimerM2 == "0")
            {
                UpdateTimerTextM1("5", false);
                UpdateTimerTextM2("9", false);
            }
            else if (ActualTimerM2 == "0")
            {
                UpdateTimerTextM2("9", false);
                UpdateTimerTextM1((int.Parse(ActualTimerM1) - 1).ToString(), false);
            }
            else
            {
                UpdateTimerTextM2((int.Parse(ActualTimerM2) - 1).ToString(), false);
            }
        }
        private async void TimerSecondUpBtn_Click(object sender, RoutedEventArgs e)
        {
            if (ActualTimerS1 == "5" && ActualTimerS2 == "9")
            {
                UpdateTimerTextS1("0", false);
                UpdateTimerTextS2("0", false);
            }
            else if (ActualTimerS2 == "9")
            {
                UpdateTimerTextS2("0", false);
                UpdateTimerTextS1((int.Parse(ActualTimerS1) + 1).ToString(), false);
            }
            else
            {
                UpdateTimerTextS2((int.Parse(ActualTimerS2) + 1).ToString(), false);
            }
        }
        private async void TimerSecondDownBtn_Click(object sender, RoutedEventArgs e)
        {
            if (ActualTimerS1 == "0" && ActualTimerS2 == "0")
            {
                UpdateTimerTextS1("5", false);
                UpdateTimerTextS2("9", false);
            }
            else if (ActualTimerS2 == "0")
            {
                UpdateTimerTextS2("9", false);
                UpdateTimerTextS1((int.Parse(ActualTimerS1) - 1).ToString(), false);
            }
            else
            {
                UpdateTimerTextS2((int.Parse(ActualTimerS2) - 1).ToString(), false);
            }
        }
        private async Task UpdateTimerTextH1(string text, bool reset)
        {
            string direction = "none";
            if ((text == "0" && ActualTimerH1 == "2") && !reset)
            {
                direction = "up";
            }
            else if ((text == "2" && ActualTimerH1 == "0") && !reset)
            {
                direction = "down";
            }
            else if ((text != ActualTimerH1) && reset)
            {
                direction = "up";
            }
            else if (int.Parse(text) > int.Parse(ActualTimerH1))
            {
                direction = "up";
            }
            else if (int.Parse(text) < int.Parse(ActualTimerH1))
            {
                direction = "down";
            }
            else if (text == ActualTimerH1)
            {
                direction = "none";
            }

            if (direction == "up")
            {
                {
                    var translateAnimation = new DoubleAnimation
                    {
                        From = 0,
                        To = -50,
                        Duration = TimeSpan.FromSeconds(0.15),
                        EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn }
                    };
                    Storyboard.SetTarget(translateAnimation, TimerH1Text);
                    Storyboard.SetTargetProperty(translateAnimation, new PropertyPath("(UIElement.RenderTransform).(TranslateTransform.Y)"));
                    var storyboard = new Storyboard();
                    storyboard.Children.Add(translateAnimation);
                    storyboard.Begin();
                }
                await Task.Delay(150);
                TimerH1Text.Text = text;
                {
                    var translateAnimation = new DoubleAnimation
                    {
                        From = 50,
                        To = 0,
                        Duration = TimeSpan.FromSeconds(0.15),
                        EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                    };
                    Storyboard.SetTarget(translateAnimation, TimerH1Text);
                    Storyboard.SetTargetProperty(translateAnimation, new PropertyPath("(UIElement.RenderTransform).(TranslateTransform.Y)"));
                    var storyboard = new Storyboard();
                    storyboard.Children.Add(translateAnimation);
                    storyboard.Begin();
                }
            }
            else if (direction == "down")
            {
                {
                    var translateAnimation = new DoubleAnimation
                    {
                        From = 0,
                        To = 50,
                        Duration = TimeSpan.FromSeconds(0.15),
                        EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn }
                    };
                    Storyboard.SetTarget(translateAnimation, TimerH1Text);
                    Storyboard.SetTargetProperty(translateAnimation, new PropertyPath("(UIElement.RenderTransform).(TranslateTransform.Y)"));
                    var storyboard = new Storyboard();
                    storyboard.Children.Add(translateAnimation);
                    storyboard.Begin();
                }
                await Task.Delay(150);
                TimerH1Text.Text = text;
                {
                    var translateAnimation = new DoubleAnimation
                    {
                        From = -50,
                        To = 0,
                        Duration = TimeSpan.FromSeconds(0.15),
                        EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                    };
                    Storyboard.SetTarget(translateAnimation, TimerH1Text);
                    Storyboard.SetTargetProperty(translateAnimation, new PropertyPath("(UIElement.RenderTransform).(TranslateTransform.Y)"));
                    var storyboard = new Storyboard();
                    storyboard.Children.Add(translateAnimation);
                    storyboard.Begin();
                }
            }
            ActualTimerH1 = text;
        }
        private async Task UpdateTimerTextH2(string text, bool reset)
        {
            string direction = "none";
            if ((text == "0" && ActualTimerH2 == "9") && !reset)
            {
                direction = "up";
            }
            else if ((text == "9" && ActualTimerH2 == "0") && !reset)
            {
                direction = "down";
            }
            else if ((text == "0" && ActualTimerH2 == "3") && !reset)
            {
                direction = "up";
            }
            else if ((text == "3" && ActualTimerH2 == "0") && !reset)
            {
                direction = "down";
            }
            else if ((text != ActualTimerH2) && reset)
            {
                direction = "up";
            }
            else if (int.Parse(text) > int.Parse(ActualTimerH2))
            {
                direction = "up";
            }
            else if (int.Parse(text) < int.Parse(ActualTimerH2))
            {
                direction = "down";
            }
            else if (text == ActualTimerH2)
            {
                direction = "none";
            }

            if (direction == "up")
            {
                {
                    var translateAnimation = new DoubleAnimation
                    {
                        From = 0,
                        To = -50,
                        Duration = TimeSpan.FromSeconds(0.15),
                        EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn }
                    };
                    Storyboard.SetTarget(translateAnimation, TimerH2Text);
                    Storyboard.SetTargetProperty(translateAnimation, new PropertyPath("(UIElement.RenderTransform).(TranslateTransform.Y)"));
                    var storyboard = new Storyboard();
                    storyboard.Children.Add(translateAnimation);
                    storyboard.Begin();
                }
                await Task.Delay(150);
                TimerH2Text.Text = text;
                {
                    var translateAnimation = new DoubleAnimation
                    {
                        From = 50,
                        To = 0,
                        Duration = TimeSpan.FromSeconds(0.15),
                        EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                    };
                    Storyboard.SetTarget(translateAnimation, TimerH2Text);
                    Storyboard.SetTargetProperty(translateAnimation, new PropertyPath("(UIElement.RenderTransform).(TranslateTransform.Y)"));
                    var storyboard = new Storyboard();
                    storyboard.Children.Add(translateAnimation);
                    storyboard.Begin();
                }
            }
            else if (direction == "down")
            {
                {
                    var translateAnimation = new DoubleAnimation
                    {
                        From = 0,
                        To = 50,
                        Duration = TimeSpan.FromSeconds(0.15),
                        EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn }
                    };
                    Storyboard.SetTarget(translateAnimation, TimerH2Text);
                    Storyboard.SetTargetProperty(translateAnimation, new PropertyPath("(UIElement.RenderTransform).(TranslateTransform.Y)"));
                    var storyboard = new Storyboard();
                    storyboard.Children.Add(translateAnimation);
                    storyboard.Begin();
                }
                await Task.Delay(150);
                TimerH2Text.Text = text;
                {
                    var translateAnimation = new DoubleAnimation
                    {
                        From = -50,
                        To = 0,
                        Duration = TimeSpan.FromSeconds(0.15),
                        EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                    };
                    Storyboard.SetTarget(translateAnimation, TimerH2Text);
                    Storyboard.SetTargetProperty(translateAnimation, new PropertyPath("(UIElement.RenderTransform).(TranslateTransform.Y)"));
                    var storyboard = new Storyboard();
                    storyboard.Children.Add(translateAnimation);
                    storyboard.Begin();
                }
            }
            ActualTimerH2 = text;
        }
        private async Task UpdateTimerTextM1(string text, bool reset)
        {
            string direction = "none";
            if ((text == "0" && ActualTimerM1 == "5") && !reset)
            {
                direction = "up";
            }
            else if ((text == "5" && ActualTimerM1 == "0") && !reset)
            {
                direction = "down";
            }
            else if (((text != ActualTimerM1) && reset) && reset)
            {
                direction = "up";
            }
            else if (int.Parse(text) > int.Parse(ActualTimerM1))
            {
                direction = "up";
            }
            else if (int.Parse(text) < int.Parse(ActualTimerM1))
            {
                direction = "down";
            }
            else if (text == ActualTimerM1)
            {
                direction = "none";
            }

            if (direction == "up")
            {
                {
                    var translateAnimation = new DoubleAnimation
                    {
                        From = 0,
                        To = -50,
                        Duration = TimeSpan.FromSeconds(0.15),
                        EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn }
                    };
                    Storyboard.SetTarget(translateAnimation, TimerM1Text);
                    Storyboard.SetTargetProperty(translateAnimation, new PropertyPath("(UIElement.RenderTransform).(TranslateTransform.Y)"));
                    var storyboard = new Storyboard();
                    storyboard.Children.Add(translateAnimation);
                    storyboard.Begin();
                }
                await Task.Delay(150);
                TimerM1Text.Text = text;
                {
                    var translateAnimation = new DoubleAnimation
                    {
                        From = 50,
                        To = 0,
                        Duration = TimeSpan.FromSeconds(0.15),
                        EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                    };
                    Storyboard.SetTarget(translateAnimation, TimerM1Text);
                    Storyboard.SetTargetProperty(translateAnimation, new PropertyPath("(UIElement.RenderTransform).(TranslateTransform.Y)"));
                    var storyboard = new Storyboard();
                    storyboard.Children.Add(translateAnimation);
                    storyboard.Begin();
                }
            }
            else if (direction == "down")
            {
                {
                    var translateAnimation = new DoubleAnimation
                    {
                        From = 0,
                        To = 50,
                        Duration = TimeSpan.FromSeconds(0.15),
                        EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn }
                    };
                    Storyboard.SetTarget(translateAnimation, TimerM1Text);
                    Storyboard.SetTargetProperty(translateAnimation, new PropertyPath("(UIElement.RenderTransform).(TranslateTransform.Y)"));
                    var storyboard = new Storyboard();
                    storyboard.Children.Add(translateAnimation);
                    storyboard.Begin();
                }
                await Task.Delay(150);
                TimerM1Text.Text = text;
                {
                    var translateAnimation = new DoubleAnimation
                    {
                        From = -50,
                        To = 0,
                        Duration = TimeSpan.FromSeconds(0.15),
                        EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                    };
                    Storyboard.SetTarget(translateAnimation, TimerM1Text);
                    Storyboard.SetTargetProperty(translateAnimation, new PropertyPath("(UIElement.RenderTransform).(TranslateTransform.Y)"));
                    var storyboard = new Storyboard();
                    storyboard.Children.Add(translateAnimation);
                    storyboard.Begin();
                }
            }
            ActualTimerM1 = text;
        }
        private async Task UpdateTimerTextM2(string text, bool reset)
        {
            string direction = "none";
            if ((text == "0" && ActualTimerM2 == "9") && !reset)
            {
                direction = "up";
            }
            else if ((text == "9" && ActualTimerM2 == "0") && !reset)
            {
                direction = "down";
            }
            else if (((text != ActualTimerM2) && reset) && reset)
            {
                direction = "up";
            }
            else if (int.Parse(text) > int.Parse(ActualTimerM2))
            {
                direction = "up";
            }
            else if (int.Parse(text) < int.Parse(ActualTimerM2))
            {
                direction = "down";
            }
            else if (text == ActualTimerM2)
            {
                direction = "none";
            }

            if (direction == "up")
            {
                {
                    var translateAnimation = new DoubleAnimation
                    {
                        From = 0,
                        To = -50,
                        Duration = TimeSpan.FromSeconds(0.15),
                        EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn }
                    };
                    Storyboard.SetTarget(translateAnimation, TimerM2Text);
                    Storyboard.SetTargetProperty(translateAnimation, new PropertyPath("(UIElement.RenderTransform).(TranslateTransform.Y)"));
                    var storyboard = new Storyboard();
                    storyboard.Children.Add(translateAnimation);
                    storyboard.Begin();
                }
                await Task.Delay(150);
                TimerM2Text.Text = text;
                {
                    var translateAnimation = new DoubleAnimation
                    {
                        From = 50,
                        To = 0,
                        Duration = TimeSpan.FromSeconds(0.15),
                        EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                    };
                    Storyboard.SetTarget(translateAnimation, TimerM2Text);
                    Storyboard.SetTargetProperty(translateAnimation, new PropertyPath("(UIElement.RenderTransform).(TranslateTransform.Y)"));
                    var storyboard = new Storyboard();
                    storyboard.Children.Add(translateAnimation);
                    storyboard.Begin();
                }
            }
            else if (direction == "down")
            {
                {
                    var translateAnimation = new DoubleAnimation
                    {
                        From = 0,
                        To = 50,
                        Duration = TimeSpan.FromSeconds(0.15),
                        EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn }
                    };
                    Storyboard.SetTarget(translateAnimation, TimerM2Text);
                    Storyboard.SetTargetProperty(translateAnimation, new PropertyPath("(UIElement.RenderTransform).(TranslateTransform.Y)"));
                    var storyboard = new Storyboard();
                    storyboard.Children.Add(translateAnimation);
                    storyboard.Begin();
                }
                await Task.Delay(150);
                TimerM2Text.Text = text;
                {
                    var translateAnimation = new DoubleAnimation
                    {
                        From = -50,
                        To = 0,
                        Duration = TimeSpan.FromSeconds(0.15),
                        EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                    };
                    Storyboard.SetTarget(translateAnimation, TimerM2Text);
                    Storyboard.SetTargetProperty(translateAnimation, new PropertyPath("(UIElement.RenderTransform).(TranslateTransform.Y)"));
                    var storyboard = new Storyboard();
                    storyboard.Children.Add(translateAnimation);
                    storyboard.Begin();
                }
            }
            ActualTimerM2 = text;
        }
        private async Task UpdateTimerTextS1(string text, bool reset)
        {
            string direction = "none";
            if ((text == "0" && ActualTimerS1 == "5") && !reset)
            {
                direction = "up";
            }
            else if ((text == "5" && ActualTimerS1 == "0") && !reset)
            {
                direction = "down";
            }
            else if (((text != ActualTimerS1) && reset) && reset)
            {
                direction = "up";
            }
            else if (int.Parse(text) > int.Parse(ActualTimerS1))
            {
                direction = "up";
            }
            else if (int.Parse(text) < int.Parse(ActualTimerS1))
            {
                direction = "down";
            }
            else if (text == ActualTimerS1)
            {
                direction = "none";
            }

            if (direction == "up")
            {
                {
                    var translateAnimation = new DoubleAnimation
                    {
                        From = 0,
                        To = -50,
                        Duration = TimeSpan.FromSeconds(0.15),
                        EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn }
                    };
                    Storyboard.SetTarget(translateAnimation, TimerS1Text);
                    Storyboard.SetTargetProperty(translateAnimation, new PropertyPath("(UIElement.RenderTransform).(TranslateTransform.Y)"));
                    var storyboard = new Storyboard();
                    storyboard.Children.Add(translateAnimation);
                    storyboard.Begin();
                }
                await Task.Delay(150);
                TimerS1Text.Text = text;
                {
                    var translateAnimation = new DoubleAnimation
                    {
                        From = 50,
                        To = 0,
                        Duration = TimeSpan.FromSeconds(0.15),
                        EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                    };
                    Storyboard.SetTarget(translateAnimation, TimerS1Text);
                    Storyboard.SetTargetProperty(translateAnimation, new PropertyPath("(UIElement.RenderTransform).(TranslateTransform.Y)"));
                    var storyboard = new Storyboard();
                    storyboard.Children.Add(translateAnimation);
                    storyboard.Begin();
                }
            }
            else if (direction == "down")
            {
                {
                    var translateAnimation = new DoubleAnimation
                    {
                        From = 0,
                        To = 50,
                        Duration = TimeSpan.FromSeconds(0.15),
                        EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn }
                    };
                    Storyboard.SetTarget(translateAnimation, TimerS1Text);
                    Storyboard.SetTargetProperty(translateAnimation, new PropertyPath("(UIElement.RenderTransform).(TranslateTransform.Y)"));
                    var storyboard = new Storyboard();
                    storyboard.Children.Add(translateAnimation);
                    storyboard.Begin();
                }
                await Task.Delay(150);
                TimerS1Text.Text = text;
                {
                    var translateAnimation = new DoubleAnimation
                    {
                        From = -50,
                        To = 0,
                        Duration = TimeSpan.FromSeconds(0.15),
                        EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                    };
                    Storyboard.SetTarget(translateAnimation, TimerS1Text);
                    Storyboard.SetTargetProperty(translateAnimation, new PropertyPath("(UIElement.RenderTransform).(TranslateTransform.Y)"));
                    var storyboard = new Storyboard();
                    storyboard.Children.Add(translateAnimation);
                    storyboard.Begin();
                }
            }
            ActualTimerS1 = text;
        }
        private async Task UpdateTimerTextS2(string text, bool reset)
        {
            string direction = "none";
            if ((text == "0" && ActualTimerS2 == "9") && !reset)
            {
                direction = "up";
            }
            else if ((text == "9" && ActualTimerS2 == "0") && !reset)
            {
                direction = "down";
            }
            else if (((text != ActualTimerS2) && reset) && reset)
            {
                direction = "up";
            }
            else if (int.Parse(text) > int.Parse(ActualTimerS2))
            {
                direction = "up";
            }
            else if (int.Parse(text) < int.Parse(ActualTimerS2))
            {
                direction = "down";
            }
            else if (text == ActualTimerS2)
            {
                direction = "none";
            }

            if (direction == "up")
            {
                {
                    var translateAnimation = new DoubleAnimation
                    {
                        From = 0,
                        To = -50,
                        Duration = TimeSpan.FromSeconds(0.15),
                        EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn }
                    };
                    Storyboard.SetTarget(translateAnimation, TimerS2Text);
                    Storyboard.SetTargetProperty(translateAnimation, new PropertyPath("(UIElement.RenderTransform).(TranslateTransform.Y)"));
                    var storyboard = new Storyboard();
                    storyboard.Children.Add(translateAnimation);
                    storyboard.Begin();
                }
                await Task.Delay(150);
                TimerS2Text.Text = text;
                {
                    var translateAnimation = new DoubleAnimation
                    {
                        From = 50,
                        To = 0,
                        Duration = TimeSpan.FromSeconds(0.15),
                        EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                    };
                    Storyboard.SetTarget(translateAnimation, TimerS2Text);
                    Storyboard.SetTargetProperty(translateAnimation, new PropertyPath("(UIElement.RenderTransform).(TranslateTransform.Y)"));
                    var storyboard = new Storyboard();
                    storyboard.Children.Add(translateAnimation);
                    storyboard.Begin();
                }
            }
            else if (direction == "down")
            {
                {
                    var translateAnimation = new DoubleAnimation
                    {
                        From = 0,
                        To = 50,
                        Duration = TimeSpan.FromSeconds(0.15),
                        EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn }
                    };
                    Storyboard.SetTarget(translateAnimation, TimerS2Text);
                    Storyboard.SetTargetProperty(translateAnimation, new PropertyPath("(UIElement.RenderTransform).(TranslateTransform.Y)"));
                    var storyboard = new Storyboard();
                    storyboard.Children.Add(translateAnimation);
                    storyboard.Begin();
                }
                await Task.Delay(150);
                TimerS2Text.Text = text;
                {
                    var translateAnimation = new DoubleAnimation
                    {
                        From = -50,
                        To = 0,
                        Duration = TimeSpan.FromSeconds(0.15),
                        EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                    };
                    Storyboard.SetTarget(translateAnimation, TimerS2Text);
                    Storyboard.SetTargetProperty(translateAnimation, new PropertyPath("(UIElement.RenderTransform).(TranslateTransform.Y)"));
                    var storyboard = new Storyboard();
                    storyboard.Children.Add(translateAnimation);
                    storyboard.Begin();
                }
            }
            ActualTimerS2 = text;
        }
        private async Task UpdateTimerProgressRing(int value)
        {
            var transtion = new DoubleAnimation
            {
                From = TimerProgressRing.Progress,
                To = value,
                Duration = TimeSpan.FromSeconds(0.5),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
            };
            Storyboard.SetTarget(transtion, TimerProgressRing);
            Storyboard.SetTargetProperty(transtion, new PropertyPath(ProgressRing.ProgressProperty));
            var storyboard = new Storyboard();
            storyboard.Children.Add(transtion);
            storyboard.Begin();
        }

        private async void Alarm_Tick(object sender, EventArgs e)
        { 
            Debug.WriteLine("Alarm_Tick");

            AlarmCardETAUpdate();

            string nowdaynumber = "0";
            DateTime now = DateTime.Now;
            string nowday = now.DayOfWeek.ToString();

            switch (nowday) 
            {
                case "Monday":
                    nowdaynumber = "1";
                    break;
                case "Tuesday":
                    nowdaynumber = "2";
                    break;
                case "Wednesday":
                    nowdaynumber = "3";
                    break;
                case "Thursday":
                    nowdaynumber = "4";
                    break;
                case "Friday":
                    nowdaynumber = "5";
                    break;
                case "Saturday":
                    nowdaynumber = "6";
                    break;
                case "Sunday":
                    nowdaynumber = "7";
                    break;
            }

            foreach (CardAction card in AlarmStack.Children)
            {
                string uid = card.Tag.ToString().Replace("_AlarmCard", "");
                string time = await configs.GetAlarm($"{uid}.time");
                string days = await configs.GetAlarm($"{uid}.days");
                string enabled = await configs.GetAlarm($"{uid}.enabled");
                if (time == now.ToString("HH:mm") && (days.Contains(nowdaynumber) || days == "0") && enabled.Contains("true"))
                {
                    AlarmAlertShow(uid);
                }
            }
        }
        private bool AlarmAlert = false;
        private string AlarmAlertUID = "";
        private SoundPlayer AlarmSoundPlayer;
        private async Task AlarmAlertShow(string uid)
        {
            if (!AlarmAlert)
            {
                AlarmAlert = true;
                AlarmAlertUID = uid;
                string name = await configs.GetAlarm($"{uid}.name");
                if (name == "") name = "Alarm";
                AlarmAlertText.Text = name;
                string time = await configs.GetAlarm($"{uid}.time");
                AlarmAlertTime.Text = time;
                string sound = await configs.GetAlarm($"{uid}.sound");
                if (!sound.Contains("none"))
                {
                    if (sound.Contains("default")) sound = configs.DefaultAlarmSound;
                    AlarmSoundPlayer = new SoundPlayer(sound);
                    AlarmSoundPlayer.Load();
                    AlarmSoundPlayer.PlayLooping();
                }
                AlarmAlertGrid.Visibility = Visibility.Visible;
                {
                    var fadeAnimation1 = new DoubleAnimation
                    {
                        From = 0,
                        To = 1,
                        Duration = TimeSpan.FromSeconds(tfadespeed),
                        EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn }
                    };
                    var zoomAnimation1 = new DoubleAnimation
                    {
                        From = 1.1,
                        To = 1,
                        Duration = TimeSpan.FromSeconds(tzoomspeed),
                        EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                    };
                    var zoomAnimation2 = new DoubleAnimation
                    {
                        From = 1.1,
                        To = 1,
                        Duration = TimeSpan.FromSeconds(tzoomspeed),
                        EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                    };
                    Storyboard.SetTarget(fadeAnimation1, AlarmAlertGrid);
                    Storyboard.SetTargetProperty(fadeAnimation1, new PropertyPath(OpacityProperty));
                    Storyboard.SetTarget(zoomAnimation1, AlarmAlertGrid);
                    Storyboard.SetTargetProperty(zoomAnimation1, new PropertyPath("(UIElement.RenderTransform).(ScaleTransform.ScaleX)"));
                    Storyboard.SetTarget(zoomAnimation2, AlarmAlertGrid);
                    Storyboard.SetTargetProperty(zoomAnimation2, new PropertyPath("(UIElement.RenderTransform).(ScaleTransform.ScaleY)"));
                    var storyboard = new Storyboard();
                    storyboard.Children.Add(fadeAnimation1);
                    storyboard.Children.Add(zoomAnimation1);
                    storyboard.Children.Add(zoomAnimation2);
                    storyboard.Begin();
                }

                double snooze = double.Parse(configs.SnoozeDelay);
                TimeSpan alerttime = new TimeSpan(0, 0, 0, 0, 0);

                while (AlarmAlert)
                {
                    await Task.Delay(100);
                    alerttime = alerttime.Add(TimeSpan.FromMilliseconds(100));

                    if (alerttime.TotalMinutes >= snooze)
                    {
                        break;
                    }
                }
                {
                    var fadeAnimation1 = new DoubleAnimation
                    {
                        From = 1,
                        To = 0,
                        Duration = TimeSpan.FromSeconds(tfadespeed),
                        EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                    };
                    var zoomAnimation1 = new DoubleAnimation
                    {
                        From = 1,
                        To = 1.1,
                        Duration = TimeSpan.FromSeconds(tzoomspeed),
                        EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn }
                    };
                    var zoomAnimation2 = new DoubleAnimation
                    {
                        From = 1,
                        To = 1.1,
                        Duration = TimeSpan.FromSeconds(tzoomspeed),
                        EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn }
                    };
                    Storyboard.SetTarget(fadeAnimation1, AlarmAlertGrid);
                    Storyboard.SetTargetProperty(fadeAnimation1, new PropertyPath(OpacityProperty));
                    Storyboard.SetTarget(zoomAnimation1, AlarmAlertGrid);
                    Storyboard.SetTargetProperty(zoomAnimation1, new PropertyPath("(UIElement.RenderTransform).(ScaleTransform.ScaleX)"));
                    Storyboard.SetTarget(zoomAnimation2, AlarmAlertGrid);
                    Storyboard.SetTargetProperty(zoomAnimation2, new PropertyPath("(UIElement.RenderTransform).(ScaleTransform.ScaleY)"));
                    var storyboard = new Storyboard();
                    storyboard.Children.Add(fadeAnimation1);
                    storyboard.Children.Add(zoomAnimation1);
                    storyboard.Children.Add(zoomAnimation2);
                    storyboard.Begin();
                }
                if (sound != "none")
                {
                    AlarmSoundPlayer.Stop();
                    AlarmSoundPlayer.Dispose();
                }

                string days = await configs.GetAlarm($"{uid}.days");
                if (days == "0")
                {
                    FrameworkElement AlarmCard = await FindFrameworkElementwithTag(AlarmStack, $"{uid}_AlarmCard");
                    FrameworkElement AlarmCardToggleSmall = await FindFrameworkElementwithTag(AlarmCard, $"{uid}_AlarmCardToggleSmall");
                    FrameworkElement AlarmCardToggleBig = await FindFrameworkElementwithTag(AlarmCard, $"{uid}_AlarmCardToggleBig");
                    ((ToggleSwitch)AlarmCardToggleBig).IsChecked = false;
                    ((ToggleSwitch)AlarmCardToggleSmall).IsChecked = false;

                    await configs.SetAlarm($"{uid}.enabled", "false");
                }

                AlarmAlert = false;
                AlarmAlertUID = "";

                await Task.Delay(500);

                AlarmAlertGrid.Visibility = Visibility.Hidden;
            }
        }
        private async Task AlarmCardETAUpdate()
        {
            foreach (CardAction card in AlarmStack.Children)
            {
                string uid = card.Tag.ToString().Replace("_AlarmCard", "");
                FrameworkElement AlarmCardDescText = await FindFrameworkElementwithTag(card, $"{uid}_AlarmCardDescText");
                string days = "";
                string daysArray = await configs.GetAlarm($"{uid}.days");
                if (daysArray == "0")
                {
                    days = "Once";
                }
                else
                {
                    if (daysArray == "12345")
                    {
                        days = "Working days";
                    }
                    else if (daysArray == "67")
                    {
                        days = "Weekend";
                    }
                    else if (daysArray == "1234567")
                    {
                        days = "Everyday";
                    }
                    else
                    {
                        foreach (char day in daysArray)
                        {
                            switch (day)
                            {
                                case '1':
                                    days += "Monday, ";
                                    break;
                                case '2':
                                    days += "Tuesday, ";
                                    break;
                                case '3':
                                    days += "Wednesday, ";
                                    break;
                                case '4':
                                    days += "Thursday, ";
                                    break;
                                case '5':
                                    days += "Friday, ";
                                    break;
                                case '6':
                                    days += "Saturday, ";
                                    break;
                                case '7':
                                    days += "Sunday, ";
                                    break;
                            }
                        }
                        days = days.Remove(days.Length - 2);
                    }
                }
                string remaningtime = await GetETAAlarmString(uid);
                ((System.Windows.Controls.TextBlock)AlarmCardDescText).Text = days + " • " + remaningtime;
            }
        }
        private async Task AlarmCardRestore()
        {
            string alarmsJSON = await File.ReadAllTextAsync(configs.AlarmPath);
            JObject alarms = JObject.Parse(alarmsJSON);
            List<string> alarmsUID = new List<string>();
            foreach (var alarm in alarms)
            {
                alarmsUID.Add(alarm.Key);
            }
            foreach (string uid in alarmsUID)
            {
                await CreateAlarmCard(uid);
            }
            await SortAlarmCards();
        }
        private async Task<DateTime> GetAlarmTime(string uid)
        {
            string time = await configs.GetAlarm($"{uid}.time");
            string[] timeArray = time.Split(':');
            return new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, int.Parse(timeArray[0]), int.Parse(timeArray[1]), 0);
        }
        private async Task<TimeSpan> GetETAAlarm(string uid)
        {
            string time = await configs.GetAlarm($"{uid}.time");
            string days = await configs.GetAlarm($"{uid}.days");

            string[] timeArray = time.Split(":");
            int hours = int.Parse(timeArray[0]);
            int minutes = int.Parse(timeArray[1]);

            DateTime now = DateTime.Now;
            DateTime alarm = new DateTime(now.Year, now.Month, now.Day, hours, minutes, 0);

            if (days == "0")
            {
                if (alarm < now)
                {
                    alarm = alarm.AddDays(-1);
                }
            }
            else
            {
                List<int> alarmDays = days.Select(c => int.Parse(c.ToString())).ToList();

                int today = ((int)now.DayOfWeek == 0) ? 7 : (int)now.DayOfWeek;

                int nextDay = alarmDays
                    .Where(d => (d > today || (d == today && alarm >= now)))
                    .DefaultIfEmpty(alarmDays.Min())
                    .First();

                int daysToAdd = (nextDay >= today) ? nextDay - today : (7 - today + nextDay);

                alarm = alarm.AddDays(daysToAdd);
            }

            return alarm - now;
        }
        public async Task SortAlarmCards()
        {
            StackPanel stackPanel = AlarmStack;

            var items = stackPanel.Children.OfType<CardAction>().ToList();

            var cardData = new Dictionary<CardAction, (DateTime time, TimeSpan eta)>();
            foreach (var card in items)
            {
                var tag = card.Tag.ToString().Replace("_AlarmCard", "");
                cardData[card] = (
                    time: await GetAlarmTime(tag),
                    eta: await GetETAAlarm(tag)
                );
            }

            var sortedItems = items.OrderBy(card => cardData[card].time)
                                   .ThenBy(card => cardData[card].eta)
                                   .ToList();

            var tasks = new List<Task>();
            for (int i = 0; i < items.Count; i++)
            {
                var card = items[i];
                var newIndex = sortedItems.IndexOf(card);

                if (newIndex != i)
                {
                    if (double.IsNaN(card.Height)) card.Height = card.ActualHeight;

                    var shrinkAnimation = new DoubleAnimation
                    {
                        To = 0,
                        Duration = TimeSpan.FromMilliseconds(300),
                        EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn }
                    };

                    var tcs = new TaskCompletionSource();
                    shrinkAnimation.Completed += (s, e) => tcs.SetResult();
                    card.BeginAnimation(HeightProperty, shrinkAnimation);
                    tasks.Add(tcs.Task);
                }
            }

            await Task.WhenAll(tasks);
            await Task.Delay(150);

            stackPanel.Children.Clear();
            foreach (var card in sortedItems)
            {
                stackPanel.Children.Add(card);
            }

            tasks.Clear();
            foreach (var card in sortedItems)
            {
                if (double.IsNaN(card.Height)) continue;

                var growAnimation = new DoubleAnimation
                {
                    From = 0,
                    To = 64,
                    Duration = TimeSpan.FromMilliseconds(500),
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                };

                var tcs = new TaskCompletionSource();
                growAnimation.Completed += (s, e) => tcs.SetResult();
                card.BeginAnimation(HeightProperty, growAnimation);
                tasks.Add(tcs.Task);
            }

            await Task.WhenAll(tasks);

            foreach (CardAction card in AlarmStack.Children)
            {
                card.BeginAnimation(HeightProperty, null);
                card.Height = double.NaN;
            }
        }
        private async Task CreateAlarmCard(string uid)
        {
            CardAction AlarmCard = new CardAction();
            AlarmCard.Tag = $"{uid}_AlarmCard";
            AlarmCard.IsChevronVisible = false;
            AlarmCard.Margin = new Thickness(0, 0, 0, 10);
            AlarmCard.Padding = new Thickness(10);
            AlarmCard.PreviewMouseDown += async (sender, e) =>
            {
                await AlarmCardOpen(sender, e, uid);
            };

            Grid AlarmCardMainGrid = new Grid();
            AlarmCardMainGrid.Tag = $"{uid}_AlarmCardMainGrid";

            Grid AlarmCardSmallGrid = new Grid();
            AlarmCardSmallGrid.Tag = $"{uid}_AlarmCardSmallGrid";
            AlarmCardSmallGrid.Visibility = Visibility.Visible;
            AlarmCardSmallGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Auto) });
            AlarmCardSmallGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Auto) });
            AlarmCardSmallGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Auto) });
            AlarmCardSmallGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            System.Windows.Controls.TextBlock AlarmCardTimeText = new System.Windows.Controls.TextBlock();
            AlarmCardTimeText.Tag = $"{uid}_AlarmCardTimeText";
            AlarmCardTimeText.Text = await configs.GetAlarm($"{uid}.time");
            AlarmCardTimeText.FontSize = 32;
            AlarmCardTimeText.FontFamily = new FontFamily("Segoe UI Variable Display SemiBold");
            AlarmCardTimeText.Foreground = (Brush)FindResource("TextFillColorPrimaryBrush");
            AlarmCardTimeText.Margin = new Thickness(10, 0, 20, 0);
            AlarmCardTimeText.HorizontalAlignment = HorizontalAlignment.Left;
            AlarmCardTimeText.VerticalAlignment = VerticalAlignment.Center;

            System.Windows.Controls.TextBlock AlarmCardNameText = new System.Windows.Controls.TextBlock();
            AlarmCardNameText.Tag = $"{uid}_AlarmCardNameText";
            AlarmCardNameText.Text = await configs.GetAlarm($"{uid}.name");
            AlarmCardNameText.FontSize = 18;
            AlarmCardNameText.FontFamily = new FontFamily("Segoe UI SemiBold");
            AlarmCardNameText.Foreground = (Brush)FindResource("TextFillColorPrimaryBrush");
            AlarmCardNameText.SetValue(Grid.ColumnProperty, 1);
            AlarmCardNameText.Margin = new Thickness(0, 0, 10, 0);
            AlarmCardNameText.VerticalAlignment = VerticalAlignment.Center;

            string days = "";
            string daysArray = await configs.GetAlarm($"{uid}.days");
            if (daysArray == "0")
            {
                days = "Once";
            }
            else
            {
                foreach (char day in daysArray)
                {
                    switch (day)
                    {
                        case '1':
                            days += "Monday, ";
                            break;
                        case '2':
                            days += "Tuesday, ";
                            break;
                        case '3':
                            days += "Wednesday, ";
                            break;
                        case '4':
                            days += "Thursday, ";
                            break;
                        case '5':
                            days += "Friday, ";
                            break;
                        case '6':
                            days += "Saturday, ";
                            break;
                        case '7':
                            days += "Sunday, ";
                            break;
                    }
                }
                days = days.Remove(days.Length - 2);
            }
            string remaningtime = await GetETAAlarmString(uid);

            System.Windows.Controls.TextBlock AlarmCardDescText = new System.Windows.Controls.TextBlock();
            AlarmCardDescText.Tag = $"{uid}_AlarmCardDescText";
            AlarmCardDescText.Text = days + " • " + remaningtime;
            AlarmCardDescText.FontSize = 14;
            AlarmCardDescText.FontFamily = new FontFamily("Segoe UI");
            AlarmCardDescText.Foreground = (Brush)FindResource("TextFillColorSecondaryBrush");
            AlarmCardDescText.SetValue(Grid.ColumnProperty, 2);
            AlarmCardDescText.Margin = new Thickness(0, 0, 10, 0);
            AlarmCardDescText.VerticalAlignment = VerticalAlignment.Center;

            ToggleSwitch AlarmCardToggleSmall = new ToggleSwitch();
            AlarmCardToggleSmall.Tag = $"{uid}_AlarmCardToggleSmall";
            AlarmCardToggleSmall.HorizontalAlignment = HorizontalAlignment.Right;
            AlarmCardToggleSmall.VerticalAlignment = VerticalAlignment.Center;
            AlarmCardToggleSmall.Margin = new Thickness(0, 0, 10, 0);
            AlarmCardToggleSmall.SetValue(Grid.ColumnProperty, 3);
            if ((await configs.GetAlarm($"{uid}.enabled")).Contains("true"))
            {
                AlarmCardToggleSmall.IsChecked = true;
            }
            else
            {
                AlarmCardToggleSmall.IsChecked = false;
            }
            AlarmCardToggleSmall.Click += async (sender, e) =>
            {
                await AlarmCardToggle(uid);
            };

            Grid AlarmCardBigGrid = new Grid();
            AlarmCardBigGrid.Tag = $"{uid}_AlarmCardBigGrid";
            AlarmCardBigGrid.Visibility = Visibility.Collapsed;
            AlarmCardBigGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) });
            AlarmCardBigGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            AlarmCardBigGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) });

            Grid AlarmCardBigGridTop = new Grid();
            AlarmCardBigGridTop.Tag = $"{uid}_AlarmCardBigGridTop";
            AlarmCardBigGridTop.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            AlarmCardBigGridTop.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            AlarmCardBigGridTop.SetValue(Grid.RowProperty, 0);

            Wpf.Ui.Controls.TextBox AlarmCardNameTextBox = new Wpf.Ui.Controls.TextBox();
            AlarmCardNameTextBox.Tag = $"{uid}_AlarmCardNameTextBox";
            AlarmCardNameTextBox.Text = await configs.GetAlarm($"{uid}.name");
            AlarmCardNameTextBox.SetValue(Grid.ColumnProperty, 0);
            AlarmCardNameTextBox.PlaceholderEnabled = true;
            AlarmCardNameTextBox.PlaceholderText = "Name";
            AlarmCardNameTextBox.VerticalAlignment = VerticalAlignment.Center;
            AlarmCardNameTextBox.Margin = new Thickness(0, 0, 10, 0);

            ToggleSwitch AlarmCardToggleBig = new ToggleSwitch();
            AlarmCardToggleBig.Tag = $"{uid}_AlarmCardToggleBig";
            AlarmCardToggleBig.HorizontalAlignment = HorizontalAlignment.Right;
            AlarmCardToggleBig.Margin = new Thickness(0, 0, 10, 0);
            AlarmCardToggleBig.SetValue(Grid.ColumnProperty, 1);
            if ((await configs.GetAlarm($"{uid}.enabled")).Contains("true"))
            {
                AlarmCardToggleBig.IsChecked = true;
            }
            else
            {
                AlarmCardToggleBig.IsChecked = false;
            }

            Grid AlarmCardBigGridMiddle = new Grid();
            AlarmCardBigGridMiddle.Tag = $"{uid}_AlarmCardBigGridMiddle";
            AlarmCardBigGridMiddle.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(0.4, GridUnitType.Star) });
            AlarmCardBigGridMiddle.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(0.6, GridUnitType.Star) });
            AlarmCardBigGridMiddle.SetValue(Grid.RowProperty, 1);

            Grid AlarmCardBigGridMiddleLeft = new Grid();
            AlarmCardBigGridMiddleLeft.Tag = $"{uid}_AlarmCardBigGridMiddleLeft";
            AlarmCardBigGridMiddleLeft.SetValue(Grid.ColumnProperty, 0);

            Border AlarmCardEditBorder = new Border();
            AlarmCardEditBorder.Tag = $"{uid}_AlarmCardEditBorder";
            AlarmCardEditBorder.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#0aFFFFFF"));
            AlarmCardEditBorder.CornerRadius = new CornerRadius(8);
            AlarmCardEditBorder.Padding = new Thickness(10);
            AlarmCardEditBorder.VerticalAlignment = VerticalAlignment.Center;
            AlarmCardEditBorder.HorizontalAlignment = HorizontalAlignment.Center;

            Grid AlarmCardEditGrid = new Grid();
            AlarmCardEditGrid.Tag = $"{uid}_AlarmCardEditGrid";
            AlarmCardEditGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            AlarmCardEditGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Auto) });
            AlarmCardEditGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            AlarmCardEditGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) });
            AlarmCardEditGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            AlarmCardEditGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) });

            Wpf.Ui.Controls.Button AlarmCardEditHourUpBtn = new Wpf.Ui.Controls.Button();
            AlarmCardEditHourUpBtn.Tag = $"{uid}_AlarmCardEditHourUpBtn";
            AlarmCardEditHourUpBtn.SetValue(Grid.ColumnProperty, 0);
            AlarmCardEditHourUpBtn.SetValue(Grid.RowProperty, 0);
            AlarmCardEditHourUpBtn.Content = "▲";
            AlarmCardEditHourUpBtn.Foreground = (Brush)FindResource("TextFillColorPrimaryBrush");
            AlarmCardEditHourUpBtn.HorizontalAlignment = HorizontalAlignment.Stretch;
            AlarmCardEditHourUpBtn.Appearance = ControlAppearance.Secondary;
            AlarmCardEditHourUpBtn.Click += async (sender, e) =>
            {
                await AlarmCardEditHour(uid, "up");
            };

            Wpf.Ui.Controls.Button AlarmCardEditHourDownBtn = new Wpf.Ui.Controls.Button();
            AlarmCardEditHourDownBtn.Tag = $"{uid}_AlarmCardEditHourDownBtn";
            AlarmCardEditHourDownBtn.SetValue(Grid.ColumnProperty, 0);
            AlarmCardEditHourDownBtn.SetValue(Grid.RowProperty, 2);
            AlarmCardEditHourDownBtn.Content = "▼";
            AlarmCardEditHourDownBtn.Foreground = (Brush)FindResource("TextFillColorPrimaryBrush");
            AlarmCardEditHourDownBtn.HorizontalAlignment = HorizontalAlignment.Stretch;
            AlarmCardEditHourDownBtn.Appearance = ControlAppearance.Secondary;
            AlarmCardEditHourDownBtn.Click += async (sender, e) =>
            {
                await AlarmCardEditHour(uid, "down");
            };

            Wpf.Ui.Controls.Button AlarmCardEditMinuteUpBtn = new Wpf.Ui.Controls.Button();
            AlarmCardEditMinuteUpBtn.Tag = $"{uid}_AlarmCardEditMinuteUpBtn";
            AlarmCardEditMinuteUpBtn.SetValue(Grid.ColumnProperty, 2);
            AlarmCardEditMinuteUpBtn.SetValue(Grid.RowProperty, 0);
            AlarmCardEditMinuteUpBtn.Content = "▲";
            AlarmCardEditMinuteUpBtn.Foreground = (Brush)FindResource("TextFillColorPrimaryBrush");
            AlarmCardEditMinuteUpBtn.HorizontalAlignment = HorizontalAlignment.Stretch;
            AlarmCardEditMinuteUpBtn.Appearance = ControlAppearance.Secondary;
            AlarmCardEditMinuteUpBtn.Click += async (sender, e) =>
            {
                await AlarmCardEditMinute(uid, "up");
            };

            Wpf.Ui.Controls.Button AlarmCardEditMinuteDownBtn = new Wpf.Ui.Controls.Button();
            AlarmCardEditMinuteDownBtn.Tag = $"{uid}_AlarmCardEditMinuteDownBtn";
            AlarmCardEditMinuteDownBtn.SetValue(Grid.ColumnProperty, 2);
            AlarmCardEditMinuteDownBtn.SetValue(Grid.RowProperty, 2);
            AlarmCardEditMinuteDownBtn.Content = "▼";
            AlarmCardEditMinuteDownBtn.Foreground = (Brush)FindResource("TextFillColorPrimaryBrush");
            AlarmCardEditMinuteDownBtn.HorizontalAlignment = HorizontalAlignment.Stretch;
            AlarmCardEditMinuteDownBtn.Appearance = ControlAppearance.Secondary;
            AlarmCardEditMinuteDownBtn.Click += async (sender, e) =>
            {
                await AlarmCardEditMinute(uid, "down");
            };

            Grid AlarmCardEditHourGrid = new Grid();
            AlarmCardEditHourGrid.Tag = $"{uid}_AlarmCardEditHourGrid";
            AlarmCardEditHourGrid.SetValue(Grid.ColumnProperty, 0);
            AlarmCardEditHourGrid.SetValue(Grid.RowProperty, 1);
            AlarmCardEditHourGrid.VerticalAlignment = VerticalAlignment.Center;
            AlarmCardEditHourGrid.HorizontalAlignment = HorizontalAlignment.Center;
            AlarmCardEditHourGrid.ClipToBounds = true;
            AlarmCardEditHourGrid.Height = 50;
            AlarmCardEditHourGrid.Width = 50;

            System.Windows.Controls.TextBlock AlarmCardEditHourText = new System.Windows.Controls.TextBlock();
            AlarmCardEditHourText.Tag = $"{uid}_AlarmCardEditHourText";
            AlarmCardEditHourText.Text = (await configs.GetAlarm($"{uid}.time")).Split(":")[0];
            AlarmCardEditHourText.FontSize = 48;
            AlarmCardEditHourText.LineHeight = 50;
            AlarmCardEditHourText.LineStackingStrategy = LineStackingStrategy.BlockLineHeight;
            AlarmCardEditHourText.FontFamily = new FontFamily("Segoe UI Variable Display SemiBold");
            AlarmCardEditHourText.Foreground = (Brush)FindResource("TextFillColorPrimaryBrush");
            AlarmCardEditHourText.HorizontalAlignment = HorizontalAlignment.Center;
            AlarmCardEditHourText.VerticalAlignment = VerticalAlignment.Center;
            AlarmCardEditHourText.RenderTransformOrigin = new Point(0.5, 0.5);
            AlarmCardEditHourText.RenderTransform = new TranslateTransform(0, 0);

            Grid AlarmCardEditMinuteGrid = new Grid();
            AlarmCardEditMinuteGrid.Tag = $"{uid}_AlarmCardEditMinuteGrid";
            AlarmCardEditMinuteGrid.SetValue(Grid.ColumnProperty, 2);
            AlarmCardEditMinuteGrid.SetValue(Grid.RowProperty, 1);
            AlarmCardEditMinuteGrid.VerticalAlignment = VerticalAlignment.Center;
            AlarmCardEditMinuteGrid.HorizontalAlignment = HorizontalAlignment.Center;
            AlarmCardEditMinuteGrid.ClipToBounds = true;
            AlarmCardEditMinuteGrid.Height = 50;
            AlarmCardEditMinuteGrid.Width = 50;

            System.Windows.Controls.TextBlock AlarmCardEditMinuteText = new System.Windows.Controls.TextBlock();
            AlarmCardEditMinuteText.Tag = $"{uid}_AlarmCardEditMinuteText";
            AlarmCardEditMinuteText.Text = (await configs.GetAlarm($"{uid}.time")).Split(":")[1];
            AlarmCardEditMinuteText.FontSize = 48;
            AlarmCardEditMinuteText.LineHeight = 50;
            AlarmCardEditMinuteText.LineStackingStrategy = LineStackingStrategy.BlockLineHeight;
            AlarmCardEditMinuteText.FontFamily = new FontFamily("Segoe UI Variable Display SemiBold");
            AlarmCardEditMinuteText.Foreground = (Brush)FindResource("TextFillColorPrimaryBrush");
            AlarmCardEditMinuteText.HorizontalAlignment = HorizontalAlignment.Center;
            AlarmCardEditMinuteText.VerticalAlignment = VerticalAlignment.Center;
            AlarmCardEditMinuteText.RenderTransformOrigin = new Point(0.5, 0.5);
            AlarmCardEditMinuteText.RenderTransform = new TranslateTransform(0, 0);

            System.Windows.Controls.TextBlock AlarmCardEditColonText = new System.Windows.Controls.TextBlock();
            AlarmCardEditColonText.Tag = $"{uid}_AlarmCardEditColonText";
            AlarmCardEditColonText.Text = ":";
            AlarmCardEditColonText.FontSize = 48;
            AlarmCardEditColonText.LineHeight = 50;
            AlarmCardEditColonText.LineStackingStrategy = LineStackingStrategy.BlockLineHeight;
            AlarmCardEditColonText.FontFamily = new FontFamily("Segoe UI Variable Display SemiBold");
            AlarmCardEditColonText.Foreground = (Brush)FindResource("TextFillColorPrimaryBrush");
            AlarmCardEditColonText.HorizontalAlignment = HorizontalAlignment.Center;
            AlarmCardEditColonText.VerticalAlignment = VerticalAlignment.Center;
            AlarmCardEditColonText.SetValue(Grid.ColumnProperty, 1);
            AlarmCardEditColonText.SetValue(Grid.RowProperty, 1);
            AlarmCardEditColonText.Margin = new Thickness(3);

            Grid AlarmCardBigGridMiddleRight = new Grid();
            AlarmCardBigGridMiddleRight.Tag = $"{uid}_AlarmCardBigGridMiddleRight";
            AlarmCardBigGridMiddleRight.SetValue(Grid.ColumnProperty, 1);
            AlarmCardBigGridMiddleRight.VerticalAlignment = VerticalAlignment.Center;
            AlarmCardBigGridMiddleRight.HorizontalAlignment = HorizontalAlignment.Center;


            StackPanel AlarmCardBigGridMiddleRightStack = new StackPanel();
            AlarmCardBigGridMiddleRightStack.Tag = $"{uid}_AlarmCardBigGridMiddleRightStack";
            AlarmCardBigGridMiddleRightStack.Orientation = Orientation.Vertical;

            StackPanel AlarmCardDaysStack = new StackPanel();
            AlarmCardDaysStack.Tag = $"{uid}_AlarmCardDaysStack";
            AlarmCardDaysStack.Margin = new Thickness(0, 0, 0, 10);
            AlarmCardDaysStack.Orientation = Orientation.Horizontal;

            string daysstr = await configs.GetAlarm($"{uid}.days");
            ToggleButton AlarmCardDayOnce = new ToggleButton();
            AlarmCardDayOnce.Tag = $"{uid}_AlarmCardDayOnce";
            AlarmCardDayOnce.Content = "1";
            AlarmCardDayOnce.Margin = new Thickness(0, 0, 5, 0);
            AlarmCardDayOnce.Foreground = (Brush)FindResource("TextFillColorPrimaryBrush");
            AlarmCardDayOnce.FontFamily = new FontFamily("Segoe UI Variable Display Regular");
            if (daysstr.Contains("0"))
            {
                AlarmCardDayOnce.IsChecked = true;
            }
            AlarmCardDayOnce.Click += async (sender, e) =>
            {
                await AlarmCardDaysToggleButton(uid, "once");
            };

            ToggleButton AlarmCardDayMonday = new ToggleButton();
            AlarmCardDayMonday.Tag = $"{uid}_AlarmCardDayMonday";
            AlarmCardDayMonday.Content = "M";
            AlarmCardDayMonday.Margin = new Thickness(0, 0, 3, 0);
            AlarmCardDayMonday.Foreground = (Brush)FindResource("TextFillColorPrimaryBrush");
            AlarmCardDayMonday.FontFamily = new FontFamily("Segoe UI Variable Display Regular");
            if (daysstr.Contains("1"))
            {
                AlarmCardDayMonday.IsChecked = true;
            }
            AlarmCardDayMonday.Click += async (sender, e) =>
            {
                await AlarmCardDaysToggleButton(uid, "days");
            };

            ToggleButton AlarmCardDayTuesday = new ToggleButton();
            AlarmCardDayTuesday.Tag = $"{uid}_AlarmCardDayTuesday";
            AlarmCardDayTuesday.Content = "T";
            AlarmCardDayTuesday.Margin = new Thickness(0, 0, 3, 0);
            AlarmCardDayTuesday.Foreground = (Brush)FindResource("TextFillColorPrimaryBrush");
            AlarmCardDayTuesday.FontFamily = new FontFamily("Segoe UI Variable Display Regular");
            if (daysstr.Contains("2"))
            {
                AlarmCardDayTuesday.IsChecked = true;
            }
            AlarmCardDayTuesday.Click += async (sender, e) =>
            {
                await AlarmCardDaysToggleButton(uid, "days");
            };

            ToggleButton AlarmCardDayWednesday = new ToggleButton();
            AlarmCardDayWednesday.Tag = $"{uid}_AlarmCardDayWednesday";
            AlarmCardDayWednesday.Content = "W";
            AlarmCardDayWednesday.Margin = new Thickness(0, 0, 3, 0);
            AlarmCardDayWednesday.Foreground = (Brush)FindResource("TextFillColorPrimaryBrush");
            AlarmCardDayWednesday.FontFamily = new FontFamily("Segoe UI Variable Display Regular");
            if (daysstr.Contains("3"))
            {
                AlarmCardDayWednesday.IsChecked = true;
            }
            AlarmCardDayWednesday.Click += async (sender, e) =>
            {
                await AlarmCardDaysToggleButton(uid, "days");
            };

            ToggleButton AlarmCardDayThursday = new ToggleButton();
            AlarmCardDayThursday.Tag = $"{uid}_AlarmCardDayThursday";
            AlarmCardDayThursday.Content = "T";
            AlarmCardDayThursday.Margin = new Thickness(0, 0, 3, 0);
            AlarmCardDayThursday.Foreground = (Brush)FindResource("TextFillColorPrimaryBrush");
            AlarmCardDayThursday.FontFamily = new FontFamily("Segoe UI Variable Display Regular");
            if (daysstr.Contains("4"))
            {
                AlarmCardDayThursday.IsChecked = true;
            }
            AlarmCardDayThursday.Click += async (sender, e) =>
            {
                await AlarmCardDaysToggleButton(uid, "days");
            };

            ToggleButton AlarmCardDayFriday = new ToggleButton();
            AlarmCardDayFriday.Tag = $"{uid}_AlarmCardDayFriday";
            AlarmCardDayFriday.Content = "F";
            AlarmCardDayFriday.Margin = new Thickness(0, 0, 3, 0);
            AlarmCardDayFriday.Foreground = (Brush)FindResource("TextFillColorPrimaryBrush");
            AlarmCardDayFriday.FontFamily = new FontFamily("Segoe UI Variable Display Regular");
            if (daysstr.Contains("5"))
            {
                AlarmCardDayFriday.IsChecked = true;
            }
            AlarmCardDayFriday.Click += async (sender, e) =>
            {
                await AlarmCardDaysToggleButton(uid, "days");
            };

            ToggleButton AlarmCardDaySaturday = new ToggleButton();
            AlarmCardDaySaturday.Tag = $"{uid}_AlarmCardDaySaturday";
            AlarmCardDaySaturday.Content = "S";
            AlarmCardDaySaturday.Margin = new Thickness(0, 0, 3, 0);
            AlarmCardDaySaturday.Foreground = (Brush)FindResource("TextFillColorPrimaryBrush");
            AlarmCardDaySaturday.FontFamily = new FontFamily("Segoe UI Variable Display Regular");
            if (daysstr.Contains("6"))
            {
                AlarmCardDaySaturday.IsChecked = true;
            }
            AlarmCardDaySaturday.Click += async (sender, e) =>
            {
                await AlarmCardDaysToggleButton(uid, "days");
            };

            ToggleButton AlarmCardDaySunday = new ToggleButton();
            AlarmCardDaySunday.Tag = $"{uid}_AlarmCardDaySunday";
            AlarmCardDaySunday.Content = "S";
            AlarmCardDaySunday.Margin = new Thickness(0, 0, 3, 0);
            AlarmCardDaySunday.Foreground = (Brush)FindResource("TextFillColorPrimaryBrush");
            AlarmCardDaySunday.FontFamily = new FontFamily("Segoe UI Variable Display Regular");
            if (daysstr.Contains("7"))
            {
                AlarmCardDaySunday.IsChecked = true;
            }
            AlarmCardDaySunday.Click += async (sender, e) =>
            {
                await AlarmCardDaysToggleButton(uid, "days");
            };

            Grid AlarmCardSoundGrid = new Grid();
            AlarmCardSoundGrid.Tag = $"{uid}_AlarmCardSoundGrid";
            AlarmCardSoundGrid.Margin = new Thickness(0, 0, 0, 10);
            AlarmCardSoundGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Auto) });
            AlarmCardSoundGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            ToggleSwitch AlarmCardSoundToggle = new ToggleSwitch();
            AlarmCardSoundToggle.Tag = $"{uid}_AlarmCardSoundToggle";
            AlarmCardSoundToggle.HorizontalAlignment = HorizontalAlignment.Center;
            AlarmCardSoundToggle.VerticalAlignment = VerticalAlignment.Center;
            AlarmCardSoundToggle.Margin = new Thickness(0, 0, 10, 0);
            AlarmCardSoundToggle.SetValue(Grid.ColumnProperty, 0);
            if ((await configs.GetAlarm($"{uid}.sound")).Contains("none"))
            {
                AlarmCardSoundToggle.IsChecked = false;
            }
            else
            {
                AlarmCardSoundToggle.IsChecked = true;
            }
            AlarmCardSoundToggle.Click += async (sender, e) =>
            {
                await AlarmCardSoundToggleClick(uid);
            };

            ComboBox AlarmCardSoundComboBox = new ComboBox();
            AlarmCardSoundComboBox.Tag = $"{uid}_AlarmCardSoundComboBox";
            AlarmCardSoundComboBox.SetValue(Grid.ColumnProperty, 1);
            if (AlarmCardSoundToggle.IsChecked == false)
            {
                AlarmCardSoundComboBox.IsEnabled = false;
            }
            else
            {
                AlarmCardSoundComboBox.IsEnabled = true;
            }
            AlarmCardSoundComboBox.DropDownOpened += async (sender, e) =>
            {
                await AlarmCardSoundComboBoxOpen(uid);
            };
            AlarmCardSoundComboBox.DropDownClosed += async (sender, e) =>
            {
                await AlarmCardSoundComboBoxClose(uid);
            };


            ComboBoxItem AlarmCardSoundComboBoxItemDefault = new ComboBoxItem();
            AlarmCardSoundComboBoxItemDefault.Tag = $"{uid}_AlarmCardSoundComboBoxItemDefault";
            AlarmCardSoundComboBoxItemDefault.Content = "Default sound";
            if (!(await configs.GetAlarm($"{uid}.sound")).Contains("default"))
            {
                AlarmCardSoundComboBoxItemDefault.IsSelected = false;
            }
            else
            {
                AlarmCardSoundComboBoxItemDefault.IsSelected = true;
            }

            ComboBoxItem AlarmCardSoundComboBoxItemCustom = new ComboBoxItem();
            AlarmCardSoundComboBoxItemCustom.Tag = $"{uid}_AlarmCardSoundComboBoxItemCustom";
            AlarmCardSoundComboBoxItemCustom.Content = "Custom sound";
            if (AlarmCardSoundComboBox.IsEnabled && !AlarmCardSoundComboBoxItemDefault.IsSelected)
            {
                AlarmCardSoundComboBoxItemCustom.IsSelected = true;
                AlarmCardSoundComboBoxItemCustom.Content = await configs.GetAlarm($"{uid}.sound");
            }
            else
            {
                AlarmCardSoundComboBoxItemCustom.IsSelected = false;
            }

            Grid AlarmCardBigGridBottom = new Grid();
            AlarmCardBigGridBottom.Tag = $"{uid}_AlarmCardBigGridBottom";
            AlarmCardBigGridBottom.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            AlarmCardBigGridBottom.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            AlarmCardBigGridBottom.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            AlarmCardBigGridBottom.SetValue(Grid.RowProperty, 2);

            Wpf.Ui.Controls.Button AlarmCardBigGridBottomBtnDelete = new Wpf.Ui.Controls.Button();
            AlarmCardBigGridBottomBtnDelete.Tag = $"{uid}_AlarmCardBigGridBottomBtnDelete";
            AlarmCardBigGridBottomBtnDelete.Content = "Delete";
            AlarmCardBigGridBottomBtnDelete.Foreground = (Brush)FindResource("TextFillColorPrimaryBrush");
            AlarmCardBigGridBottomBtnDelete.HorizontalAlignment = HorizontalAlignment.Stretch;
            AlarmCardBigGridBottomBtnDelete.Appearance = ControlAppearance.Secondary;
            AlarmCardBigGridBottomBtnDelete.SetValue(Grid.ColumnProperty, 0);
            AlarmCardBigGridBottomBtnDelete.Click += async (sender, e) =>
            {
                await AlarmCardClose(uid);
                await AlarmCardDelete(uid);
            };

            Wpf.Ui.Controls.Button AlarmCardBigGridBottomBtnCancel = new Wpf.Ui.Controls.Button();
            AlarmCardBigGridBottomBtnCancel.Tag = $"{uid}_AlarmCardBigGridBottomBtnCancel";
            AlarmCardBigGridBottomBtnCancel.Content = "Cancel";
            AlarmCardBigGridBottomBtnCancel.Foreground = (Brush)FindResource("TextFillColorPrimaryBrush");
            AlarmCardBigGridBottomBtnCancel.HorizontalAlignment = HorizontalAlignment.Stretch;
            AlarmCardBigGridBottomBtnCancel.Appearance = ControlAppearance.Secondary;
            AlarmCardBigGridBottomBtnCancel.SetValue(Grid.ColumnProperty, 1);
            AlarmCardBigGridBottomBtnCancel.Margin = new Thickness(10, 0, 10, 0);
            AlarmCardBigGridBottomBtnCancel.Click += async (sender, e) =>
            {
                await AlarmCardCancel(uid);
                await AlarmCardClose(uid);
            };

            Wpf.Ui.Controls.Button AlarmCardBigGridBottomBtnSave = new Wpf.Ui.Controls.Button();
            AlarmCardBigGridBottomBtnSave.Tag = $"{uid}_AlarmCardBigGridBottomBtnSave";
            AlarmCardBigGridBottomBtnSave.Content = "Save";
            AlarmCardBigGridBottomBtnSave.Foreground = (Brush)FindResource("TextFillColorPrimaryBrush");
            AlarmCardBigGridBottomBtnSave.HorizontalAlignment = HorizontalAlignment.Stretch;
            AlarmCardBigGridBottomBtnSave.Appearance = ControlAppearance.Primary;
            AlarmCardBigGridBottomBtnSave.SetValue(Grid.ColumnProperty, 2);
            AlarmCardBigGridBottomBtnSave.Click += async (sender, e) =>
            {
                await AlarmCardSave(uid);
                await AlarmCardClose(uid);
            };

            AlarmCardSmallGrid.Children.Add(AlarmCardTimeText);
            AlarmCardSmallGrid.Children.Add(AlarmCardNameText);
            AlarmCardSmallGrid.Children.Add(AlarmCardDescText);
            AlarmCardSmallGrid.Children.Add(AlarmCardToggleSmall);

            AlarmCardBigGridTop.Children.Add(AlarmCardNameTextBox);
            AlarmCardBigGridTop.Children.Add(AlarmCardToggleBig);

            AlarmCardEditHourGrid.Children.Add(AlarmCardEditHourText);
            AlarmCardEditMinuteGrid.Children.Add(AlarmCardEditMinuteText);

            AlarmCardEditGrid.Children.Add(AlarmCardEditHourUpBtn);
            AlarmCardEditGrid.Children.Add(AlarmCardEditHourGrid);
            AlarmCardEditGrid.Children.Add(AlarmCardEditHourDownBtn);
            AlarmCardEditGrid.Children.Add(AlarmCardEditMinuteUpBtn);
            AlarmCardEditGrid.Children.Add(AlarmCardEditMinuteGrid);
            AlarmCardEditGrid.Children.Add(AlarmCardEditMinuteDownBtn);
            AlarmCardEditGrid.Children.Add(AlarmCardEditColonText);

            AlarmCardEditBorder.Child = AlarmCardEditGrid;

            AlarmCardBigGridMiddleLeft.Children.Add(AlarmCardEditBorder);

            AlarmCardDaysStack.Children.Add(AlarmCardDayOnce);
            AlarmCardDaysStack.Children.Add(AlarmCardDayMonday);
            AlarmCardDaysStack.Children.Add(AlarmCardDayTuesday);
            AlarmCardDaysStack.Children.Add(AlarmCardDayWednesday);
            AlarmCardDaysStack.Children.Add(AlarmCardDayThursday);
            AlarmCardDaysStack.Children.Add(AlarmCardDayFriday);
            AlarmCardDaysStack.Children.Add(AlarmCardDaySaturday);
            AlarmCardDaysStack.Children.Add(AlarmCardDaySunday);

            AlarmCardSoundComboBox.Items.Add(AlarmCardSoundComboBoxItemDefault);
            AlarmCardSoundComboBox.Items.Add(AlarmCardSoundComboBoxItemCustom);

            AlarmCardSoundGrid.Children.Add(AlarmCardSoundToggle);
            AlarmCardSoundGrid.Children.Add(AlarmCardSoundComboBox);

            AlarmCardBigGridMiddleRightStack.Children.Add(AlarmCardDaysStack);
            AlarmCardBigGridMiddleRightStack.Children.Add(AlarmCardSoundGrid);

            AlarmCardBigGridMiddleRight.Children.Add(AlarmCardBigGridMiddleRightStack);

            AlarmCardBigGridMiddle.Children.Add(AlarmCardBigGridMiddleLeft);
            AlarmCardBigGridMiddle.Children.Add(AlarmCardBigGridMiddleRight);

            AlarmCardBigGridBottom.Children.Add(AlarmCardBigGridBottomBtnDelete);
            AlarmCardBigGridBottom.Children.Add(AlarmCardBigGridBottomBtnCancel);
            AlarmCardBigGridBottom.Children.Add(AlarmCardBigGridBottomBtnSave);

            AlarmCardBigGrid.Children.Add(AlarmCardBigGridTop);
            AlarmCardBigGrid.Children.Add(AlarmCardBigGridMiddle);
            AlarmCardBigGrid.Children.Add(AlarmCardBigGridBottom);

            AlarmCardMainGrid.Children.Add(AlarmCardSmallGrid);
            AlarmCardMainGrid.Children.Add(AlarmCardBigGrid);

            AlarmCard.Content = AlarmCardMainGrid;

            AlarmStack.Children.Add(AlarmCard);

            NoAlarmText.Visibility = Visibility.Collapsed;
        }
        private async Task<string> GetETAAlarmString(string uid)
        {
            string time = await configs.GetAlarm($"{uid}.time");
            string days = await configs.GetAlarm($"{uid}.days");

            string[] timeArray = time.Split(":");
            int hours = int.Parse(timeArray[0]);
            int minutes = int.Parse(timeArray[1]);

            DateTime now = DateTime.Now;
            DateTime alarm = new DateTime(now.Year, now.Month, now.Day, hours, minutes, 0);

            if (days == "0")
            {
                if (alarm < now)
                {
                    alarm = alarm.AddDays(1);
                }
            }
            else
            {
                List<int> alarmDays = days.Select(c => int.Parse(c.ToString())).ToList();

                int today = ((int)now.DayOfWeek == 0) ? 7 : (int)now.DayOfWeek;

                int nextDay = alarmDays
                    .Where(d => (d > today || (d == today && alarm >= now)))
                    .DefaultIfEmpty(alarmDays.Min())
                    .First();

                int daysToAdd = (nextDay >= today) ? nextDay - today : (7 - today + nextDay);

                alarm = alarm.AddDays(daysToAdd);
            }

            TimeSpan diff = alarm - now;

            if (diff.Days == 0)
            {
                return $"in {diff.Hours} h and {diff.Minutes} m";
            }
            else
            {
                return $"in {diff.Days} day{(diff.Days > 1 ? "s" : "")}";
            }
        }
        private async Task AlarmCardSoundToggleClick(string uid)
        {
            FrameworkElement AlarmCard = await FindFrameworkElementwithTag(AlarmStack, $"{uid}_AlarmCard");
            FrameworkElement AlarmCardSoundToggle = await FindFrameworkElementwithTag(AlarmCard, $"{uid}_AlarmCardSoundToggle");
            FrameworkElement AlarmCardSoundComboBox = await FindFrameworkElementwithTag(AlarmCard, $"{uid}_AlarmCardSoundComboBox");
            FrameworkElement AlarmCardSoundComboBoxItemDefault = await FindItemwithTag(((ComboBox)AlarmCardSoundComboBox).Items, $"{uid}_AlarmCardSoundComboBoxItemDefault");
            FrameworkElement AlarmCardSoundComboBoxItemCustom = await FindItemwithTag(((ComboBox)AlarmCardSoundComboBox).Items, $"{uid}_AlarmCardSoundComboBoxItemCustom");
            if (((ToggleButton)AlarmCardSoundToggle).IsChecked == true)
            {
                ((ComboBox)AlarmCardSoundComboBox).IsEnabled = true;
            }
            else
            {
                ((ComboBox)AlarmCardSoundComboBox).IsEnabled = false;
            }

            ((ComboBoxItem)AlarmCardSoundComboBoxItemDefault).IsSelected = true;
            ((ComboBoxItem)AlarmCardSoundComboBoxItemCustom).IsSelected = false;
            ((ComboBoxItem)AlarmCardSoundComboBoxItemCustom).Content = "Custom";

        }
        private async Task AlarmCardDaysToggleButton(string uid, string source)
        {
            FrameworkElement AlarmCard = await FindFrameworkElementwithTag(AlarmStack, $"{uid}_AlarmCard");
            FrameworkElement AlarmCardDayOnce = await FindFrameworkElementwithTag(AlarmCard, $"{uid}_AlarmCardDayOnce");
            FrameworkElement AlarmCardDayMonday = await FindFrameworkElementwithTag(AlarmCard, $"{uid}_AlarmCardDayMonday");
            FrameworkElement AlarmCardDayTuesday = await FindFrameworkElementwithTag(AlarmCard, $"{uid}_AlarmCardDayTuesday");
            FrameworkElement AlarmCardDayWednesday = await FindFrameworkElementwithTag(AlarmCard, $"{uid}_AlarmCardDayWednesday");
            FrameworkElement AlarmCardDayThursday = await FindFrameworkElementwithTag(AlarmCard, $"{uid}_AlarmCardDayThursday");
            FrameworkElement AlarmCardDayFriday = await FindFrameworkElementwithTag(AlarmCard, $"{uid}_AlarmCardDayFriday");
            FrameworkElement AlarmCardDaySaturday = await FindFrameworkElementwithTag(AlarmCard, $"{uid}_AlarmCardDaySaturday");
            FrameworkElement AlarmCardDaySunday = await FindFrameworkElementwithTag(AlarmCard, $"{uid}_AlarmCardDaySunday");

            if (((ToggleButton)AlarmCardDayOnce).IsChecked == true && source == "once")
            {
                ((ToggleButton)AlarmCardDayMonday).IsChecked = false;
                ((ToggleButton)AlarmCardDayTuesday).IsChecked = false;
                ((ToggleButton)AlarmCardDayWednesday).IsChecked = false;
                ((ToggleButton)AlarmCardDayThursday).IsChecked = false;
                ((ToggleButton)AlarmCardDayFriday).IsChecked = false;
                ((ToggleButton)AlarmCardDaySaturday).IsChecked = false;
                ((ToggleButton)AlarmCardDaySunday).IsChecked = false;
            }
            else if (((ToggleButton)AlarmCardDayOnce).IsChecked == false && source == "once")
            {
                ((ToggleButton)AlarmCardDayMonday).IsChecked = true;
                ((ToggleButton)AlarmCardDayTuesday).IsChecked = true;
                ((ToggleButton)AlarmCardDayWednesday).IsChecked = true;
                ((ToggleButton)AlarmCardDayThursday).IsChecked = true;
                ((ToggleButton)AlarmCardDayFriday).IsChecked = true;
                ((ToggleButton)AlarmCardDaySaturday).IsChecked = true;
                ((ToggleButton)AlarmCardDaySunday).IsChecked = true;
            }


            if (((ToggleButton)AlarmCardDayMonday).IsChecked == false && ((ToggleButton)AlarmCardDayTuesday).IsChecked == false && ((ToggleButton)AlarmCardDayWednesday).IsChecked == false && ((ToggleButton)AlarmCardDayThursday).IsChecked == false && ((ToggleButton)AlarmCardDayFriday).IsChecked == false && ((ToggleButton)AlarmCardDaySaturday).IsChecked == false && ((ToggleButton)AlarmCardDaySunday).IsChecked == false && source == "days")
            {
                ((ToggleButton)AlarmCardDayOnce).IsChecked = true;
            }
            else if ((((ToggleButton)AlarmCardDayMonday).IsChecked == true || ((ToggleButton)AlarmCardDayTuesday).IsChecked == true || ((ToggleButton)AlarmCardDayWednesday).IsChecked == true || ((ToggleButton)AlarmCardDayThursday).IsChecked == true || ((ToggleButton)AlarmCardDayFriday).IsChecked == true || ((ToggleButton)AlarmCardDaySaturday).IsChecked == true || ((ToggleButton)AlarmCardDaySunday).IsChecked == true) && source == "days")
            {
                ((ToggleButton)AlarmCardDayOnce).IsChecked = false;
            }
        }
        private async Task AlarmCardSoundComboBoxClose(string uid)
        {
            FrameworkElement AlarmCard = await FindFrameworkElementwithTag(AlarmStack, $"{uid}_AlarmCard");
            FrameworkElement AlarmCardSoundComboBox = await FindFrameworkElementwithTag(AlarmCard, $"{uid}_AlarmCardSoundComboBox");
            FrameworkElement AlarmCardSoundComboBoxItemDefault = await FindItemwithTag(((ComboBox)AlarmCardSoundComboBox).Items, $"{uid}_AlarmCardSoundComboBoxItemDefault");
            FrameworkElement AlarmCardSoundComboBoxItemCustom = await FindItemwithTag(((ComboBox)AlarmCardSoundComboBox).Items, $"{uid}_AlarmCardSoundComboBoxItemCustom");

            if (((ComboBoxItem)AlarmCardSoundComboBoxItemCustom).IsSelected)
            {
                var ofd = new OpenFileDialog();
                ofd.DefaultExt = "wav";
                ofd.Filter = "WAV Audio File (*.wav)|*.wav";
                ofd.FilterIndex = 1;
                ofd.Title = "WinDeskClock Custom Alarm Sound Select";
                var result = ofd.ShowDialog();
                if (result == true)
                {
                    string filename = ofd.FileName;
                    ((ComboBoxItem)AlarmCardSoundComboBoxItemCustom).Content = filename;
                }
                else
                {
                    ((ComboBoxItem)AlarmCardSoundComboBoxItemDefault).IsSelected = true;
                    ((ComboBoxItem)AlarmCardSoundComboBoxItemCustom).IsSelected = false;
                }
            }
        }
        private async Task AlarmCardSoundComboBoxOpen(string uid)
        {
            FrameworkElement AlarmCard = await FindFrameworkElementwithTag(AlarmStack, $"{uid}_AlarmCard");
            FrameworkElement AlarmCardSoundComboBox = await FindFrameworkElementwithTag(AlarmCard, $"{uid}_AlarmCardSoundComboBox");
            FrameworkElement AlarmCardSoundComboBoxItemDefault = await FindItemwithTag(((ComboBox)AlarmCardSoundComboBox).Items, $"{uid}_AlarmCardSoundComboBoxItemDefault");
            FrameworkElement AlarmCardSoundComboBoxItemCustom = await FindItemwithTag(((ComboBox)AlarmCardSoundComboBox).Items, $"{uid}_AlarmCardSoundComboBoxItemCustom");

            ((ComboBoxItem)AlarmCardSoundComboBoxItemCustom).Content = "Custom";
        }
        private async Task AlarmCardToggle(string uid)
        {
            FrameworkElement AlarmCard = await FindFrameworkElementwithTag(AlarmStack, $"{uid}_AlarmCard");
            FrameworkElement AlarmCardToggleSmall = await FindFrameworkElementwithTag(AlarmCard, $"{uid}_AlarmCardToggleSmall");
            FrameworkElement AlarmCardToggleBig = await FindFrameworkElementwithTag(AlarmCard, $"{uid}_AlarmCardToggleBig");

            if (((ToggleSwitch)AlarmCardToggleSmall).IsChecked == true)
            {
                ((ToggleSwitch)AlarmCardToggleBig).IsChecked = true;
                await configs.SetAlarm($"{uid}.enabled", "true");
            }
            else
            {
                ((ToggleSwitch)AlarmCardToggleBig).IsChecked = false;
                await configs.SetAlarm($"{uid}.enabled", "false");
            }
        }
        private async Task AlarmCardSave(string uid)
        {
            FrameworkElement AlarmCard = await FindFrameworkElementwithTag(AlarmStack, $"{uid}_AlarmCard");
            FrameworkElement AlarmCardEditHourText = await FindFrameworkElementwithTag(AlarmCard, $"{uid}_AlarmCardEditHourText");
            FrameworkElement AlarmCardEditMinuteText = await FindFrameworkElementwithTag(AlarmCard, $"{uid}_AlarmCardEditMinuteText");
            FrameworkElement AlarmCardNameTextBox = await FindFrameworkElementwithTag(AlarmCard, $"{uid}_AlarmCardNameTextBox");
            FrameworkElement AlarmCardDayOnce = await FindFrameworkElementwithTag(AlarmCard, $"{uid}_AlarmCardDayOnce");
            FrameworkElement AlarmCardDayMonday = await FindFrameworkElementwithTag(AlarmCard, $"{uid}_AlarmCardDayMonday");
            FrameworkElement AlarmCardDayTuesday = await FindFrameworkElementwithTag(AlarmCard, $"{uid}_AlarmCardDayTuesday");
            FrameworkElement AlarmCardDayWednesday = await FindFrameworkElementwithTag(AlarmCard, $"{uid}_AlarmCardDayWednesday");
            FrameworkElement AlarmCardDayThursday = await FindFrameworkElementwithTag(AlarmCard, $"{uid}_AlarmCardDayThursday");
            FrameworkElement AlarmCardDayFriday = await FindFrameworkElementwithTag(AlarmCard, $"{uid}_AlarmCardDayFriday");
            FrameworkElement AlarmCardDaySaturday = await FindFrameworkElementwithTag(AlarmCard, $"{uid}_AlarmCardDaySaturday");
            FrameworkElement AlarmCardDaySunday = await FindFrameworkElementwithTag(AlarmCard, $"{uid}_AlarmCardDaySunday");
            FrameworkElement AlarmCardSoundToggle = await FindFrameworkElementwithTag(AlarmCard, $"{uid}_AlarmCardSoundToggle");
            FrameworkElement AlarmCardSoundComboBox = await FindFrameworkElementwithTag(AlarmCard, $"{uid}_AlarmCardSoundComboBox");
            FrameworkElement AlarmCardSoundComboBoxItemDefault = await FindItemwithTag(((ComboBox)AlarmCardSoundComboBox).Items, $"{uid}_AlarmCardSoundComboBoxItemDefault");
            FrameworkElement AlarmCardSoundComboBoxItemCustom = await FindItemwithTag(((ComboBox)AlarmCardSoundComboBox).Items, $"{uid}_AlarmCardSoundComboBoxItemCustom");

            FrameworkElement AlarmCardToggleBig = await FindFrameworkElementwithTag(AlarmCard, $"{uid}_AlarmCardToggleBig");
            FrameworkElement AlarmCardToggleSmall = await FindFrameworkElementwithTag(AlarmCard, $"{uid}_AlarmCardToggleSmall");

            FrameworkElement AlarmCardTimeText = await FindFrameworkElementwithTag(AlarmCard, $"{uid}_AlarmCardTimeText");
            FrameworkElement AlarmCardNameText = await FindFrameworkElementwithTag(AlarmCard, $"{uid}_AlarmCardNameText");
            FrameworkElement AlarmCardDescText = await FindFrameworkElementwithTag(AlarmCard, $"{uid}_AlarmCardDescText");

            string time = $"{((System.Windows.Controls.TextBlock)AlarmCardEditHourText).Text}:{((System.Windows.Controls.TextBlock)AlarmCardEditMinuteText).Text}";
            ((System.Windows.Controls.TextBlock)AlarmCardTimeText).Text = time;

            string name = ((Wpf.Ui.Controls.TextBox)AlarmCardNameTextBox).Text;
            ((System.Windows.Controls.TextBlock)AlarmCardNameText).Text = name;

            string daysstr = "";
            if (((ToggleButton)AlarmCardDayOnce).IsChecked == true)
            {
                daysstr += "0";
            }
            else
            {
                if (((ToggleButton)AlarmCardDayMonday).IsChecked == true)
                {
                    daysstr += "1";
                }
                if (((ToggleButton)AlarmCardDayTuesday).IsChecked == true)
                {
                    daysstr += "2";
                }
                if (((ToggleButton)AlarmCardDayWednesday).IsChecked == true)
                {
                    daysstr += "3";
                }
                if (((ToggleButton)AlarmCardDayThursday).IsChecked == true)
                {
                    daysstr += "4";
                }
                if (((ToggleButton)AlarmCardDayFriday).IsChecked == true)
                {
                    daysstr += "5";
                }
                if (((ToggleButton)AlarmCardDaySaturday).IsChecked == true)
                {
                    daysstr += "6";
                }
                if (((ToggleButton)AlarmCardDaySunday).IsChecked == true)
                {
                    daysstr += "7";
                }
            }

            string sound = "";
            if (((ToggleSwitch)AlarmCardSoundToggle).IsChecked == false)
            {
                sound = "none";
            }
            else
            {
                if (((ComboBoxItem)AlarmCardSoundComboBoxItemDefault).IsSelected == true)
                {
                    sound = "default";
                }
                else
                {
                    sound = ((ComboBoxItem)AlarmCardSoundComboBoxItemCustom).Content.ToString();
                }
            }

            string enabled = "false";
            if (((ToggleSwitch)AlarmCardToggleBig).IsChecked == true)
            {
                ((ToggleSwitch)AlarmCardToggleSmall).IsChecked = true;
                enabled = "true";
            }
            else
            {
                ((ToggleSwitch)AlarmCardToggleSmall).IsChecked = false;
                enabled = "false";
            }

            await configs.SetAlarm($"{uid}.time", time);
            await configs.SetAlarm($"{uid}.name", name);
            await configs.SetAlarm($"{uid}.days", daysstr);
            await configs.SetAlarm($"{uid}.sound", sound);
            await configs.SetAlarm($"{uid}.enabled", enabled);
        }
        private async Task AlarmCardCancel(string uid)
        {
            FrameworkElement AlarmCard = await FindFrameworkElementwithTag(AlarmStack, $"{uid}_AlarmCard");
            FrameworkElement AlarmCardEditHourText = await FindFrameworkElementwithTag(AlarmCard, $"{uid}_AlarmCardEditHourText");
            FrameworkElement AlarmCardEditMinuteText = await FindFrameworkElementwithTag(AlarmCard, $"{uid}_AlarmCardEditMinuteText");
            FrameworkElement AlarmCardNameTextBox = await FindFrameworkElementwithTag(AlarmCard, $"{uid}_AlarmCardNameTextBox");
            FrameworkElement AlarmCardDayOnce = await FindFrameworkElementwithTag(AlarmCard, $"{uid}_AlarmCardDayOnce");
            FrameworkElement AlarmCardDayMonday = await FindFrameworkElementwithTag(AlarmCard, $"{uid}_AlarmCardDayMonday");
            FrameworkElement AlarmCardDayTuesday = await FindFrameworkElementwithTag(AlarmCard, $"{uid}_AlarmCardDayTuesday");
            FrameworkElement AlarmCardDayWednesday = await FindFrameworkElementwithTag(AlarmCard, $"{uid}_AlarmCardDayWednesday");
            FrameworkElement AlarmCardDayThursday = await FindFrameworkElementwithTag(AlarmCard, $"{uid}_AlarmCardDayThursday");
            FrameworkElement AlarmCardDayFriday = await FindFrameworkElementwithTag(AlarmCard, $"{uid}_AlarmCardDayFriday");
            FrameworkElement AlarmCardDaySaturday = await FindFrameworkElementwithTag(AlarmCard, $"{uid}_AlarmCardDaySaturday");
            FrameworkElement AlarmCardDaySunday = await FindFrameworkElementwithTag(AlarmCard, $"{uid}_AlarmCardDaySunday");
            FrameworkElement AlarmCardSoundToggle = await FindFrameworkElementwithTag(AlarmCard, $"{uid}_AlarmCardSoundToggle");
            FrameworkElement AlarmCardSoundComboBox = await FindFrameworkElementwithTag(AlarmCard, $"{uid}_AlarmCardSoundComboBox");
            FrameworkElement AlarmCardSoundComboBoxItemDefault = await FindItemwithTag(((ComboBox)AlarmCardSoundComboBox).Items, $"{uid}_AlarmCardSoundComboBoxItemDefault");
            FrameworkElement AlarmCardSoundComboBoxItemCustom = await FindItemwithTag(((ComboBox)AlarmCardSoundComboBox).Items, $"{uid}_AlarmCardSoundComboBoxItemCustom");

            FrameworkElement AlarmCardToggleBig = await FindFrameworkElementwithTag(AlarmCard, $"{uid}_AlarmCardToggleBig");

            string name = await configs.GetAlarm($"{uid}.name");
            ((Wpf.Ui.Controls.TextBox)AlarmCardNameTextBox).Text = name;

            string time = await configs.GetAlarm($"{uid}.time");
            string[] timeArray = time.Split(":");
            ((System.Windows.Controls.TextBlock)AlarmCardEditHourText).Text = timeArray[0];
            ((System.Windows.Controls.TextBlock)AlarmCardEditMinuteText).Text = timeArray[1];
            ((Wpf.Ui.Controls.TextBox)AlarmCardNameTextBox).Text = await configs.GetAlarm($"{uid}.name");

            string daysstr = await configs.GetAlarm($"{uid}.days");
            if (daysstr.Contains("0"))
            {
                ((ToggleButton)AlarmCardDayOnce).IsChecked = true;
            }
            else
            {
                ((ToggleButton)AlarmCardDayOnce).IsChecked = false;
                if (daysstr.Contains("1"))
                {
                    ((ToggleButton)AlarmCardDayMonday).IsChecked = true;
                }
                if (daysstr.Contains("2"))
                {
                    ((ToggleButton)AlarmCardDayTuesday).IsChecked = true;
                }
                if (daysstr.Contains("3"))
                {
                    ((ToggleButton)AlarmCardDayWednesday).IsChecked = true;
                }
                if (daysstr.Contains("4"))
                {
                    ((ToggleButton)AlarmCardDayThursday).IsChecked = true;
                }
                if (daysstr.Contains("5"))
                {
                    ((ToggleButton)AlarmCardDayFriday).IsChecked = true;
                }
                if (daysstr.Contains("6"))
                {
                    ((ToggleButton)AlarmCardDaySaturday).IsChecked = true;
                }
                if (daysstr.Contains("7"))
                {
                    ((ToggleButton)AlarmCardDaySunday).IsChecked = true;
                }
            }

            string sound = await configs.GetAlarm($"{uid}.sound");
            if (sound.Contains("none"))
            {
                ((ToggleSwitch)AlarmCardSoundToggle).IsChecked = false;
            }
            else
            {
                ((ToggleSwitch)AlarmCardSoundToggle).IsChecked = true;
                if (sound.Contains("default"))
                {
                    ((ComboBoxItem)AlarmCardSoundComboBoxItemDefault).IsSelected = true;
                }
                else
                {
                    ((ComboBoxItem)AlarmCardSoundComboBoxItemCustom).IsSelected = true;
                    ((ComboBoxItem)AlarmCardSoundComboBoxItemCustom).Content = sound;
                }
            }

            string enabled = await configs.GetAlarm($"{uid}.enabled");
            if (enabled.Contains("true"))
            {
                ((ToggleSwitch)AlarmCardToggleBig).IsChecked = true;
            }
            else
            {
                ((ToggleSwitch)AlarmCardToggleBig).IsChecked = false;
            }
        }
        private async Task AlarmCardDelete(string uid)
        {
            await configs.DelAlarm(uid);
            FrameworkElement AlarmCard = await FindFrameworkElementwithTag(AlarmStack, $"{uid}_AlarmCard");
            {
                var sizeAnimation = new DoubleAnimation
                {
                    From = AlarmCard.ActualHeight,
                    To = 0,
                    Duration = TimeSpan.FromSeconds(0.2),
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                };
                Storyboard.SetTarget(sizeAnimation, AlarmCard);
                Storyboard.SetTargetProperty(sizeAnimation, new PropertyPath(HeightProperty));
                var storyboard = new Storyboard();
                storyboard.Children.Add(sizeAnimation);
                storyboard.Begin();
            }
            await Task.Delay(200);
            AlarmStack.Children.Remove((CardAction)AlarmCard);
            if (AlarmStack.Children.Count == 0)
            {
                NoAlarmText.Visibility = Visibility.Visible;
            }
        }
        private async Task AlarmCardEditHour(string uid, string direction)
        {
            FrameworkElement AlarmCard = await FindFrameworkElementwithTag(AlarmStack, $"{uid}_AlarmCard");
            FrameworkElement AlarmCardEditHourText = await FindFrameworkElementwithTag(AlarmCard, $"{uid}_AlarmCardEditHourText");
            int hour = int.Parse(((System.Windows.Controls.TextBlock)AlarmCardEditHourText).Text);
            if (direction == "up")
            {
                if (hour == 23)
                {
                    hour = 0;
                }
                else
                {
                    hour++;
                }

                {
                    var translateAnimation = new DoubleAnimation
                    {
                        From = 0,
                        To = 50,
                        Duration = TimeSpan.FromSeconds(0.1),
                        EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                    };
                    Storyboard.SetTarget(translateAnimation, AlarmCardEditHourText);
                    Storyboard.SetTargetProperty(translateAnimation, new PropertyPath("(UIElement.RenderTransform).(TranslateTransform.Y)"));
                    var storyboard = new Storyboard();
                    storyboard.Children.Add(translateAnimation);
                    storyboard.Begin();
                }

                await Task.Delay(100);

                {
                    var translateAnimation = new DoubleAnimation
                    {
                        From = -50,
                        To = 0,
                        Duration = TimeSpan.FromSeconds(0.1),
                        EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                    };
                    Storyboard.SetTarget(translateAnimation, AlarmCardEditHourText);
                    Storyboard.SetTargetProperty(translateAnimation, new PropertyPath("(UIElement.RenderTransform).(TranslateTransform.Y)"));
                    var storyboard = new Storyboard();
                    storyboard.Children.Add(translateAnimation);
                    storyboard.Begin();
                }
            }
            else if (direction == "down")
            {
                if (hour == 0)
                {
                    hour = 23;
                }
                else
                {
                    hour--;
                }

                {
                    var translateAnimation = new DoubleAnimation
                    {
                        From = 0,
                        To = -50,
                        Duration = TimeSpan.FromSeconds(0.1),
                        EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                    };
                    Storyboard.SetTarget(translateAnimation, AlarmCardEditHourText);
                    Storyboard.SetTargetProperty(translateAnimation, new PropertyPath("(UIElement.RenderTransform).(TranslateTransform.Y)"));
                    var storyboard = new Storyboard();
                    storyboard.Children.Add(translateAnimation);
                    storyboard.Begin();
                }

                await Task.Delay(100);

                {
                    var translateAnimation = new DoubleAnimation
                    {
                        From = 50,
                        To = 0,
                        Duration = TimeSpan.FromSeconds(0.1),
                        EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                    };
                    Storyboard.SetTarget(translateAnimation, AlarmCardEditHourText);
                    Storyboard.SetTargetProperty(translateAnimation, new PropertyPath("(UIElement.RenderTransform).(TranslateTransform.Y)"));
                    var storyboard = new Storyboard();
                    storyboard.Children.Add(translateAnimation);
                    storyboard.Begin();
                }
            }
            ((System.Windows.Controls.TextBlock)AlarmCardEditHourText).Text = hour.ToString("00");
        }
        private async Task AlarmCardEditMinute(string uid, string direction)
        {
            FrameworkElement AlarmCard = await FindFrameworkElementwithTag(AlarmStack, $"{uid}_AlarmCard");
            FrameworkElement AlarmCardEditMinuteText = await FindFrameworkElementwithTag(AlarmCard, $"{uid}_AlarmCardEditMinuteText");
            int minute = int.Parse(((System.Windows.Controls.TextBlock)AlarmCardEditMinuteText).Text);
            if (direction == "up")
            {
                if (minute == 59)
                {
                    minute = 0;
                }
                else
                {
                    minute++;
                }

                {
                    var translateAnimation = new DoubleAnimation
                    {
                        From = 0,
                        To = 50,
                        Duration = TimeSpan.FromSeconds(0.1),
                        EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                    };
                    Storyboard.SetTarget(translateAnimation, AlarmCardEditMinuteText);
                    Storyboard.SetTargetProperty(translateAnimation, new PropertyPath("(UIElement.RenderTransform).(TranslateTransform.Y)"));
                    var storyboard = new Storyboard();
                    storyboard.Children.Add(translateAnimation);
                    storyboard.Begin();
                }

                await Task.Delay(100);

                {
                    var translateAnimation = new DoubleAnimation
                    {
                        From = -50,
                        To = 0,
                        Duration = TimeSpan.FromSeconds(0.1),
                        EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                    };
                    Storyboard.SetTarget(translateAnimation, AlarmCardEditMinuteText);
                    Storyboard.SetTargetProperty(translateAnimation, new PropertyPath("(UIElement.RenderTransform).(TranslateTransform.Y)"));
                    var storyboard = new Storyboard();
                    storyboard.Children.Add(translateAnimation);
                    storyboard.Begin();
                }
            }
            else if (direction == "down")
            {
                if (minute == 0)
                {
                    minute = 59;
                }
                else
                {
                    minute--;
                }

                {
                    var translateAnimation = new DoubleAnimation
                    {
                        From = 0,
                        To = -50,
                        Duration = TimeSpan.FromSeconds(0.1),
                        EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                    };
                    Storyboard.SetTarget(translateAnimation, AlarmCardEditMinuteText);
                    Storyboard.SetTargetProperty(translateAnimation, new PropertyPath("(UIElement.RenderTransform).(TranslateTransform.Y)"));
                    var storyboard = new Storyboard();
                    storyboard.Children.Add(translateAnimation);
                    storyboard.Begin();
                }

                await Task.Delay(100);

                {
                    var translateAnimation = new DoubleAnimation
                    {
                        From = 50,
                        To = 0,
                        Duration = TimeSpan.FromSeconds(0.1),
                        EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                    };
                    Storyboard.SetTarget(translateAnimation, AlarmCardEditMinuteText);
                    Storyboard.SetTargetProperty(translateAnimation, new PropertyPath("(UIElement.RenderTransform).(TranslateTransform.Y)"));
                    var storyboard = new Storyboard();
                    storyboard.Children.Add(translateAnimation);
                    storyboard.Begin();
                }
            }
            ((System.Windows.Controls.TextBlock)AlarmCardEditMinuteText).Text = minute.ToString("00");
        }
        private bool CardDeployed = false;
        private async Task AlarmCardClose(string uid)
        {

            foreach (CardAction card in AlarmStack.Children)
            {
                card.Visibility = Visibility.Visible;
            }

            await Task.Delay(150);

            FrameworkElement AlarmCard = await FindFrameworkElementwithTag(AlarmStack, $"{uid}_AlarmCard");
            FrameworkElement AlarmCardMainGrid = await FindFrameworkElementwithTag(AlarmCard, $"{uid}_AlarmCardMainGrid");
            FrameworkElement AlarmCardBigGrid = await FindFrameworkElementwithTag(AlarmCard, $"{uid}_AlarmCardBigGrid");
            FrameworkElement AlarmCardSmallGrid = await FindFrameworkElementwithTag(AlarmCard, $"{uid}_AlarmCardSmallGrid");

            AlarmCardBigGrid.Visibility = Visibility.Collapsed;
            AlarmCardSmallGrid.Visibility = Visibility.Visible;

            {
                var sizeAnimation = new DoubleAnimation
                {
                    From = AlarmScroll.ActualHeight - 23,
                    To = AlarmCardSmallGrid.ActualHeight,
                    Duration = TimeSpan.FromSeconds(0.3),
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                };
                Storyboard.SetTarget(sizeAnimation, AlarmCardMainGrid);
                Storyboard.SetTargetProperty(sizeAnimation, new PropertyPath(HeightProperty));
                var storyboard = new Storyboard();
                storyboard.Children.Add(sizeAnimation);
                storyboard.Begin();

                var fadeAnimation = new DoubleAnimation
                {
                    From = 0,
                    To = 1,
                    Duration = TimeSpan.FromSeconds(0.5),
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn }
                };
                Storyboard.SetTarget(fadeAnimation, AlarmCardSmallGrid);
                Storyboard.SetTargetProperty(fadeAnimation, new PropertyPath(OpacityProperty));
                var storyboard2 = new Storyboard();
                storyboard2.Children.Add(fadeAnimation);
                storyboard2.Begin();
            }

            foreach (CardAction card in AlarmStack.Children)
            {
                if (card.Tag.ToString() != $"{uid}_AlarmCard")
                {
                    var sizeAnimation = new DoubleAnimation
                    {
                        From = 0,
                        To = 64,
                        Duration = TimeSpan.FromSeconds(0.3),
                        EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                    };
                    Storyboard.SetTarget(sizeAnimation, card);
                    Storyboard.SetTargetProperty(sizeAnimation, new PropertyPath(HeightProperty));
                    var storyboard = new Storyboard();
                    storyboard.Children.Add(sizeAnimation);
                    storyboard.Begin();
                }
            }

            await Task.Delay(300);

            AlarmCardMainGrid.Height = double.NaN;
            foreach (CardAction card in AlarmStack.Children)
            {
                card.BeginAnimation(HeightProperty, null);
                card.Height = double.NaN;
            }

            CardDeployed = false;

            await Task.Delay(500);

            await SortAlarmCards();
            await AlarmCardETAUpdate();
        }
        private async Task AlarmCardOpen(object sender, MouseButtonEventArgs e, string uid)
        {
            if ((e.Source is CardAction || e.Source is System.Windows.Controls.TextBlock) && !CardDeployed)
            {
                CardDeployed = true;

                foreach (CardAction card in AlarmStack.Children)
                {
                    if (card.Tag.ToString() != $"{uid}_AlarmCard")
                    {
                        var sizeAnimation = new DoubleAnimation
                        {
                            From = card.ActualHeight,
                            To = 0,
                            Duration = TimeSpan.FromSeconds(0.3),
                            EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                        };
                        Storyboard.SetTarget(sizeAnimation, card);
                        Storyboard.SetTargetProperty(sizeAnimation, new PropertyPath(HeightProperty));
                        var storyboard = new Storyboard();
                        storyboard.Children.Add(sizeAnimation);
                        storyboard.Begin();
                    }
                }

                FrameworkElement AlarmCard = await FindFrameworkElementwithTag(AlarmStack, $"{uid}_AlarmCard");
                FrameworkElement AlarmCardMainGrid = await FindFrameworkElementwithTag(AlarmCard, $"{uid}_AlarmCardMainGrid");
                FrameworkElement AlarmCardBigGrid = await FindFrameworkElementwithTag(AlarmCard, $"{uid}_AlarmCardBigGrid");
                FrameworkElement AlarmCardSmallGrid = await FindFrameworkElementwithTag(AlarmCard, $"{uid}_AlarmCardSmallGrid");

                AlarmCardSmallGrid.Visibility = Visibility.Collapsed;
                AlarmCardBigGrid.Visibility = Visibility.Visible;

                {
                    var sizeAnimation = new DoubleAnimation
                    {
                        From = AlarmCard.ActualHeight,
                        To = AlarmScroll.ActualHeight - 23,
                        Duration = TimeSpan.FromSeconds(0.3),
                        EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                    };
                    Storyboard.SetTarget(sizeAnimation, AlarmCardMainGrid);
                    Storyboard.SetTargetProperty(sizeAnimation, new PropertyPath(HeightProperty));
                    var storyboard = new Storyboard();
                    storyboard.Children.Add(sizeAnimation);
                    storyboard.Begin();

                    var fadeAnimation = new DoubleAnimation
                    {
                        From = 0,
                        To = 1,
                        Duration = TimeSpan.FromSeconds(0.5),
                        EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn }
                    };
                    Storyboard.SetTarget(fadeAnimation, AlarmCardBigGrid);
                    Storyboard.SetTargetProperty(fadeAnimation, new PropertyPath(OpacityProperty));
                    var storyboard2 = new Storyboard();
                    storyboard2.Children.Add(fadeAnimation);
                    storyboard2.Begin();
                }

                await Task.Delay(150);

                foreach (CardAction card in AlarmStack.Children)
                {
                    if (card.Tag.ToString() != $"{uid}_AlarmCard")
                    {
                        card.Visibility = Visibility.Collapsed;
                    }
                }

                await Task.Delay(150);
            }
        }
        private async void AddAlarmBtn_Click(object sender, RoutedEventArgs e)
        {
            if (!CardDeployed)
            {

                CardDeployed = true;
                string uid = Guid.NewGuid().ToString();

                DateTime now = DateTime.Now;
                string time = $"{now.Hour.ToString("00")}:{now.Minute.ToString("00")}";
                string name = "";
                string days = "0";
                string sound = "default";
                string enabled = "true";
                await configs.SetAlarm($"{uid}.time", time);
                await configs.SetAlarm($"{uid}.name", name);
                await configs.SetAlarm($"{uid}.days", days);
                await configs.SetAlarm($"{uid}.sound", sound);
                await configs.SetAlarm($"{uid}.enabled", enabled);

                await CreateAlarmCard(uid);

                FrameworkElement AlarmCard = await FindFrameworkElementwithTag(AlarmStack, $"{uid}_AlarmCard");

                {
                    var sizeAnimation = new DoubleAnimation
                    {
                        From = 0,
                        To = 64,
                        Duration = TimeSpan.FromSeconds(0.3),
                        EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                    };
                    Storyboard.SetTarget(sizeAnimation, AlarmCard);
                    Storyboard.SetTargetProperty(sizeAnimation, new PropertyPath(HeightProperty));
                    var storyboard = new Storyboard();
                    storyboard.Children.Add(sizeAnimation);
                    storyboard.Begin();
                }

                await Task.Delay(300);

                AlarmCard.BeginAnimation(HeightProperty, null);
                AlarmCard.Height = double.NaN;

                foreach (CardAction card in AlarmStack.Children)
                {
                    if (card.Tag.ToString() != $"{uid}_AlarmCard")
                    {
                        var sizeAnimation = new DoubleAnimation
                        {
                            From = card.ActualHeight,
                            To = 0,
                            Duration = TimeSpan.FromSeconds(0.3),
                            EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                        };
                        Storyboard.SetTarget(sizeAnimation, card);
                        Storyboard.SetTargetProperty(sizeAnimation, new PropertyPath(HeightProperty));
                        var storyboard = new Storyboard();
                        storyboard.Children.Add(sizeAnimation);
                        storyboard.Begin();
                    }
                }

                FrameworkElement AlarmCardMainGrid = await FindFrameworkElementwithTag(AlarmCard, $"{uid}_AlarmCardMainGrid");
                FrameworkElement AlarmCardBigGrid = await FindFrameworkElementwithTag(AlarmCard, $"{uid}_AlarmCardBigGrid");
                FrameworkElement AlarmCardSmallGrid = await FindFrameworkElementwithTag(AlarmCard, $"{uid}_AlarmCardSmallGrid");

                AlarmCardSmallGrid.Visibility = Visibility.Collapsed;
                AlarmCardBigGrid.Visibility = Visibility.Visible;

                {
                    var sizeAnimation = new DoubleAnimation
                    {
                        From = AlarmCard.ActualHeight,
                        To = AlarmScroll.ActualHeight - 23,
                        Duration = TimeSpan.FromSeconds(0.3),
                        EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                    };
                    Storyboard.SetTarget(sizeAnimation, AlarmCardMainGrid);
                    Storyboard.SetTargetProperty(sizeAnimation, new PropertyPath(HeightProperty));
                    var storyboard = new Storyboard();
                    storyboard.Children.Add(sizeAnimation);
                    storyboard.Begin();

                    var fadeAnimation = new DoubleAnimation
                    {
                        From = 0,
                        To = 1,
                        Duration = TimeSpan.FromSeconds(0.5),
                        EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn }
                    };
                    Storyboard.SetTarget(fadeAnimation, AlarmCardBigGrid);
                    Storyboard.SetTargetProperty(fadeAnimation, new PropertyPath(OpacityProperty));
                    var storyboard2 = new Storyboard();
                    storyboard2.Children.Add(fadeAnimation);
                    storyboard2.Begin();
                }

                await Task.Delay(150);
                foreach (CardAction card in AlarmStack.Children)
                {
                    if (card.Tag.ToString() != $"{uid}_AlarmCard")
                    {
                        card.Visibility = Visibility.Collapsed;
                    }
                }
                await Task.Delay(150);
            }
        }
        private void AlarmAlertStopBtn_Click(object sender, RoutedEventArgs e)
        {
            AlarmAlert = false;
        }
    }
}