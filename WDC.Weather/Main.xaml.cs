using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using WDC.Weather.Utils;
using WinDeskClock.Utils;

namespace WDC.Weather
{
    /// <summary>
    /// Interaction logic for Main.xaml
    /// </summary>
    public partial class Main : Page
    {
        public WeatherData.WeatherDataHour weatherNow;
        public WeatherData.WeatherDataWeek weatherAll;
        public DispatcherTimer RefreshTimer;
        public DispatcherTimer UIRestore;

        public Main()
        {
            InitializeComponent();
            Loaded += async (s, e) => await Load();
        }

        private bool Init = false;
        private async Task Load()
        {
            if (!Init)
            {
                // This part is called only once (equivalent to Initialized event)
                await Config.CheckConf();
                await WMO.LoadJson();
                await Lang.InitLang();
                await LoadLang();
                await TimerInit();
                await LoadConfig();
                await RefreshData();
                Init = true;
            }
            // await RefreshData();  Avoid unnecessary data refresh on every load
            DayListView.SelectedIndex = 0;
            DailyScroll.ScrollToVerticalOffset(0);
            HourlyScroll.ScrollToHorizontalOffset(0);
            await UIUpdate();
        }

        public async Task LoadLang()
        {
            // Place your code here to apply the language to the controls in the main page
            // - You can get the language from the main program with:
            //   Plugin plugin = new Plugin();
            //   plugin.Language;
        }

        public async Task TimerInit()
        {
            RefreshTimer = new DispatcherTimer();
            RefreshTimer.Interval = TimeSpan.FromMinutes(1);
            RefreshTimer.Tick += async (s, e) =>
            {
                if (DayListView.SelectedIndex == 0 && HourlyScroll.HorizontalOffset == 0 && DailyScroll.VerticalOffset == 0)
                {
                    await RefreshData();
                    await UIUpdate();
                }

                // Time variation fix
                int nextminute = 60 - DateTime.Now.Second;
                RefreshTimer.Interval = TimeSpan.FromSeconds(nextminute);
            };
            RefreshTimer.Start();
        }

        public async Task LoadConfig()
        {
            // Place your code here to load the configuration, and set the values to the controls in the main page
            await Config.LoadConfig();
        }

        private string RefreshErrorReason = "";
        public async Task RefreshData()
        {
            if (Config.Variables.Location == null)
            {
                weatherNow = null;
                weatherAll = null;
            }
            else
            {
                try
                {
                    weatherNow = await OpenMeteo.GetWeatherNow(Config.Variables.Location);
                    weatherAll = await OpenMeteo.GetWeatherAll(Config.Variables.Location);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"{ex.Message}");
                    RefreshErrorReason = ex.Message;
                    weatherNow = null;
                    weatherAll = null;
                }
            }
        }

        private string UIStatus = "init";
        private bool IsExecuting = false;
        public async Task UIUpdate()
        {
            if (!IsExecuting)
            {
                IsExecuting = true;

                if (Config.Variables.Location == null)
                {
                    WeatherGrid.Visibility = System.Windows.Visibility.Hidden;
                    NoDataGrid.Visibility = System.Windows.Visibility.Hidden;
                    LocationGrid.Visibility = System.Windows.Visibility.Visible;
                    UIStatus = "noloc";
                }
                else if (weatherNow == null && weatherAll == null)
                {
                    WeatherGrid.Visibility = System.Windows.Visibility.Hidden;
                    LocationGrid.Visibility = System.Windows.Visibility.Hidden;
                    NoDataGrid.Visibility = System.Windows.Visibility.Visible;
                    NoDataReason.Text = RefreshErrorReason;
                    UIStatus = "nodata";
                }
                else
                {
                    CityText.Text = Config.Variables.Location.Name;

                    WeatherGrid.Visibility = System.Windows.Visibility.Visible;
                    NoDataGrid.Visibility = System.Windows.Visibility.Hidden;
                    LocationGrid.Visibility = System.Windows.Visibility.Hidden;

                    // Update the daily forecast labels
                    DailyText1.Text = "Now";
                    DailyText2.Text = "Tomorrow";
                    DailyText3.Text = weatherAll.DateTimeStart.AddDays(2).DayOfWeek.ToString();
                    DailyText4.Text = weatherAll.DateTimeStart.AddDays(3).DayOfWeek.ToString();
                    DailyText5.Text = weatherAll.DateTimeStart.AddDays(4).DayOfWeek.ToString();
                    DailyText6.Text = weatherAll.DateTimeStart.AddDays(5).DayOfWeek.ToString();
                    DailyText7.Text = weatherAll.DateTimeStart.AddDays(6).DayOfWeek.ToString();

                    TempText.Text = await weatherNow.Temperature.GetFormatedText();
                    FeelsTempText.Text = await Lang.GetLang("weather.feelslike", await weatherNow.FeelsLike.GetFormatedText());
                    MinTempText.Text = await weatherAll.Daily[0].TemperatureMin.GetFormatedText();
                    MaxTempText.Text = await weatherAll.Daily[0].TemperatureMax.GetFormatedText();
                    HumidityText.Text = await Lang.GetLang("units.percent.percent", weatherNow.Humidity.ToString());
                    WindText.Text = $"{await weatherNow.WindSpeed.GetFormatedText()}\n{await weatherNow.WindDirection.GetCardinal()} {weatherNow.WindDirection.Deg}°";
                    UVText.Text = $"{weatherNow.UVIndex.GetValue().ToString("0.#")}\n{await weatherNow.UVIndex.GetUVISafety()}";
                    AQText.Text = $"{weatherNow.AirQuality.GetValue()}\n{await weatherNow.AirQuality.GetSafety()}";
                    PrecipitationText.Text = await weatherNow.Precipitation.GetFormatedText();
                    PressureText.Text = await weatherNow.Pressure.GetFormatedText();
                    CloudText.Text = await Lang.GetLang("units.percent.percent", weatherNow.CloudCover.ToString());
                    VisibilityText.Text = await weatherNow.Visibility.GetFormatedText();
                    DateTime now = DateTime.Now;
                    bool isday;
                    if (now > weatherAll.Daily[0].Sunrise && now < weatherAll.Daily[0].Sunset)
                    {
                        isday = true;
                    }
                    else
                    {
                        isday = false;
                    }
                    UpdateWMOUI(weatherNow.WMO, isday);

                    if (UIStatus != "now") // If it's not in Now mode, clear the HourlyStack and rebuild it
                    {
                        foreach (Border card in HourlyStack.Children)
                        {
                            var fadeout = new DoubleAnimation
                            {
                                From = 1,
                                To = 0,
                                Duration = TimeSpan.FromSeconds(0.1),
                                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn }
                            };
                            card.SetValue(OpacityProperty, 1.0);
                            Storyboard.SetTarget(fadeout, card);
                            Storyboard.SetTargetProperty(fadeout, new PropertyPath("Opacity"));
                            var storyboard = new Storyboard();
                            storyboard.Children.Add(fadeout);
                            storyboard.Begin();
                            await Task.Delay(10);
                        }
                        HourlyStack.Children.Clear();
                        for (int hour = 0; hour < 24; hour++)
                        {
                            string time = hour == 0 ? "Now" : weatherAll.Daily[(now.Hour + hour) / 24].Hourly[(now.Hour + hour) % 24].DateTime.ToString("HH:mm");
                            string temp = hour == 0 ? weatherNow.Temperature.GetValue().ToString("0.#") : weatherAll.Daily[(now.Hour + hour) / 24].Hourly[(now.Hour + hour) % 24].Temperature.GetValue().ToString("0.#");
                            bool isday2;
                            if (weatherAll.Daily[(now.Hour + hour) / 24].Hourly[(now.Hour + hour) % 24].DateTime > weatherAll.Daily[(now.Hour + hour) / 24].Sunrise && weatherAll.Daily[(now.Hour + hour) / 24].Hourly[(now.Hour + hour) % 24].DateTime < weatherAll.Daily[(now.Hour + hour) / 24].Sunset)
                            {
                                isday2 = true;
                            }
                            else
                            {
                                isday2 = false;
                            }
                            string icon = hour == 0 ? await WMO.GetWMOIconName(weatherNow.WMO, isday) : await WMO.GetWMOIconName(weatherAll.Daily[(now.Hour + hour) / 24].Hourly[(now.Hour + hour) % 24].WMO, isday2);
                            double progressvalue = hour == 0 ? (((weatherNow.Temperature.Kelvin - weatherAll.Daily[0].TemperatureMin.Kelvin) * 80) / (weatherAll.Daily[0].TemperatureMax.Kelvin - weatherAll.Daily[0].TemperatureMin.Kelvin)) + 10 : (((weatherAll.Daily[(now.Hour + hour) / 24].Hourly[(now.Hour + hour) % 24].Temperature.Kelvin - weatherAll.Daily[(now.Hour + hour) / 24].TemperatureMin.Kelvin) * 80) / (weatherAll.Daily[(now.Hour + hour) / 24].TemperatureMax.Kelvin - weatherAll.Daily[(now.Hour + hour) / 24].TemperatureMin.Kelvin)) + 10;
                            Border hourcard = await CreateHourlyCard($"Hour{(hour == 0 ? "Now" : weatherAll.Daily[(now.Hour + hour) / 24].Hourly[(now.Hour + hour) % 24].DateTime.ToString("HH"))}", time, temp, icon, progressvalue);
                            {

                                var fadein = new DoubleAnimation
                                {
                                    From = 0,
                                    To = 1,
                                    Duration = TimeSpan.FromSeconds(0.3),
                                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                                };
                                var translate = new DoubleAnimation
                                {
                                    From = 30,
                                    To = 0,
                                    Duration = TimeSpan.FromSeconds(0.3),
                                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                                };
                                hourcard.Visibility = Visibility.Visible;
                                hourcard.RenderTransformOrigin = new Point(0.5, 0.5);
                                hourcard.SetValue(RenderTransformProperty, new TranslateTransform(0, 0));
                                Storyboard.SetTarget(fadein, hourcard);
                                Storyboard.SetTargetProperty(fadein, new PropertyPath("Opacity"));
                                Storyboard.SetTarget(translate, hourcard);
                                Storyboard.SetTargetProperty(translate, new PropertyPath("(UIElement.RenderTransform).(TranslateTransform.X)"));
                                var storyboard = new Storyboard();
                                storyboard.Children.Add(fadein);
                                storyboard.Children.Add(translate);
                                storyboard.Begin();
                                RowDefinition ProgressRowDef = ((Grid)await TagSearch.FindFrameworkElementwithTag(hourcard, $"{hourcard.Tag}ProgressGrid")).RowDefinitions[1];
                                var gridup = new GridLengthAnimation
                                {
                                    From = new System.Windows.GridLength(0, System.Windows.GridUnitType.Star),
                                    To = new System.Windows.GridLength(progressvalue / 100, System.Windows.GridUnitType.Star),
                                    Duration = TimeSpan.FromSeconds(0.3),
                                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                                };
                                Storyboard.SetTarget(gridup, ProgressRowDef);
                                Storyboard.SetTargetProperty(gridup, new PropertyPath("Height"));
                                storyboard = new Storyboard();
                                storyboard.Children.Add(gridup);
                                storyboard.Begin();
                                await Task.Delay(30);
                            }
                            HourlyStack.Children.Add(hourcard);

                            if (((now.Hour + hour) % 24) + 1 < 24) // Check if the next hour is the same day
                            {
                                // Check if the next hour is sunrise or sunset 
                                if (weatherAll.Daily[(now.Hour + hour) / 24].Hourly[(now.Hour + hour) % 24].DateTime.Hour == weatherAll.Daily[(now.Hour + hour) / 24].Sunrise.Hour && weatherAll.Daily[(now.Hour + hour) / 24].Hourly[((now.Hour + hour) % 24) + 1].DateTime.Hour > weatherAll.Daily[(now.Hour + hour) / 24].Sunrise.Hour && now <= weatherAll.Daily[(now.Hour + hour) / 24].Sunrise)
                                {
                                    Border suncard = await CreateHourlySunCard("Sunrise", weatherAll.Daily[(now.Hour + hour) / 24].Sunrise.ToString("HH:mm"), "sunrise.png", "Sunrise");
                                    {
                                        var fadein = new DoubleAnimation
                                        {
                                            From = 0,
                                            To = 1,
                                            Duration = TimeSpan.FromSeconds(0.3),
                                            EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                                        };
                                        var translate = new DoubleAnimation
                                        {
                                            From = 30,
                                            To = 0,
                                            Duration = TimeSpan.FromSeconds(0.3),
                                            EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                                        };
                                        suncard.Visibility = Visibility.Visible;
                                        suncard.RenderTransformOrigin = new Point(0.5, 0.5);
                                        suncard.SetValue(RenderTransformProperty, new TranslateTransform(0, 0));
                                        Storyboard.SetTarget(fadein, suncard);
                                        Storyboard.SetTargetProperty(fadein, new PropertyPath("Opacity"));
                                        Storyboard.SetTarget(translate, suncard);
                                        Storyboard.SetTargetProperty(translate, new PropertyPath("(UIElement.RenderTransform).(TranslateTransform.X)"));
                                        var storyboard = new Storyboard();
                                        storyboard.Children.Add(fadein);
                                        storyboard.Children.Add(translate);
                                        storyboard.Begin();
                                        await Task.Delay(30);
                                    }
                                    HourlyStack.Children.Add(suncard);
                                }
                                if (weatherAll.Daily[(now.Hour + hour) / 24].Hourly[(now.Hour + hour) % 24].DateTime.Hour == weatherAll.Daily[(now.Hour + hour) / 24].Sunset.Hour && weatherAll.Daily[(now.Hour + hour) / 24].Hourly[((now.Hour + hour) % 24) + 1].DateTime.Hour > weatherAll.Daily[(now.Hour + hour) / 24].Sunset.Hour && now <= weatherAll.Daily[(now.Hour + hour) / 24].Sunset)
                                {
                                    Border suncard = await CreateHourlySunCard("Sunset", weatherAll.Daily[(now.Hour + hour) / 24].Sunset.ToString("HH:mm"), "sunset.png", "Sunset");
                                    {
                                        var fadein = new DoubleAnimation
                                        {
                                            From = 0,
                                            To = 1,
                                            Duration = TimeSpan.FromSeconds(0.3),
                                            EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                                        };
                                        var translate = new DoubleAnimation
                                        {
                                            From = 30,
                                            To = 0,
                                            Duration = TimeSpan.FromSeconds(0.3),
                                            EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                                        };
                                        suncard.Visibility = Visibility.Visible;
                                        suncard.RenderTransformOrigin = new Point(0.5, 0.5);
                                        suncard.SetValue(RenderTransformProperty, new TranslateTransform(0, 0));
                                        Storyboard.SetTarget(fadein, suncard);
                                        Storyboard.SetTargetProperty(fadein, new PropertyPath("Opacity"));
                                        Storyboard.SetTarget(translate, suncard);
                                        Storyboard.SetTargetProperty(translate, new PropertyPath("(UIElement.RenderTransform).(TranslateTransform.X)"));
                                        var storyboard = new Storyboard();
                                        storyboard.Children.Add(fadein);
                                        storyboard.Children.Add(translate);
                                        storyboard.Begin();
                                        await Task.Delay(30);
                                    }
                                    HourlyStack.Children.Add(suncard);
                                }
                            }

                            if (((now.Hour + hour) % 24) + 1 == 24)
                            {
                                Border nextcard = await CreateHourlySunCard("Nextday", weatherAll.Daily[((now.Hour + hour) / 24) + 1].DateTime.ToString("dd/MM"), "nextday.png", "Tomorrow");
                                {
                                    var fadein = new DoubleAnimation
                                    {
                                        From = 0,
                                        To = 1,
                                        Duration = TimeSpan.FromSeconds(0.3),
                                        EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                                    };
                                    var translate = new DoubleAnimation
                                    {
                                        From = 30,
                                        To = 0,
                                        Duration = TimeSpan.FromSeconds(0.3),
                                        EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                                    };
                                    nextcard.Visibility = Visibility.Visible;
                                    nextcard.RenderTransformOrigin = new Point(0.5, 0.5);
                                    nextcard.SetValue(RenderTransformProperty, new TranslateTransform(0, 0));
                                    Storyboard.SetTarget(fadein, nextcard);
                                    Storyboard.SetTargetProperty(fadein, new PropertyPath("Opacity"));
                                    Storyboard.SetTarget(translate, nextcard);
                                    Storyboard.SetTargetProperty(translate, new PropertyPath("(UIElement.RenderTransform).(TranslateTransform.X)"));
                                    var storyboard = new Storyboard();
                                    storyboard.Children.Add(fadein);
                                    storyboard.Children.Add(translate);
                                    storyboard.Begin();
                                    await Task.Delay(30);
                                }
                                HourlyStack.Children.Add(nextcard);
                            }
                        }
                        UIStatus = "now";
                    }
                    else
                    {
                        bool NeedToAdd = false; // If it's true, new cards will be added to the stack
                                                // Check if the [1] is the current hour to shift [1] to [0] and convert to "Now" and create a new card at the end of the stack
                        if (((Border)HourlyStack.Children[1]).Tag.ToString() == $"Hour{now.ToString("HH")}")
                        {
                            var fadeout = new DoubleAnimation
                            {
                                From = 1,
                                To = 0,
                                Duration = TimeSpan.FromSeconds(0.3),
                                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                            };
                            var zoomout = new DoubleAnimation
                            {
                                From = ((Border)HourlyStack.Children[0]).ActualWidth,
                                To = 0,
                                Duration = TimeSpan.FromSeconds(0.3),
                                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                            };
                            ((Border)HourlyStack.Children[0]).SetValue(OpacityProperty, 1.0);
                            ((Border)HourlyStack.Children[0]).SetValue(WidthProperty, ((Border)HourlyStack.Children[0]).ActualWidth);
                            Storyboard.SetTarget(fadeout, HourlyStack.Children[0]);
                            Storyboard.SetTargetProperty(fadeout, new PropertyPath("Opacity"));
                            Storyboard.SetTarget(zoomout, HourlyStack.Children[0]);
                            Storyboard.SetTargetProperty(zoomout, new PropertyPath("Width"));
                            var storyboard = new Storyboard();
                            storyboard.Children.Add(fadeout);
                            storyboard.Children.Add(zoomout);
                            storyboard.Begin();
                            await Task.Delay(300);
                            HourlyStack.Children.RemoveAt(0);
                            Border card = (Border)HourlyStack.Children[0];
                            string oldtag = card.Tag.ToString();
                            card.Tag = "HourNow";
                            ((Image)await TagSearch.FindFrameworkElementwithTag(card, $"{oldtag}Icon")).Tag = $"{card.Tag}Icon";
                            ((Grid)await TagSearch.FindFrameworkElementwithTag(card, $"{oldtag}ProgressGrid")).Tag = $"{card.Tag}ProgressGrid";
                            ((TextBlock)await TagSearch.FindFrameworkElementwithTag(card, $"{oldtag}TimeText")).Tag = $"{card.Tag}TimeText";
                            ((TextBlock)await TagSearch.FindFrameworkElementwithTag(card, $"{oldtag}TempText")).Tag = $"{card.Tag}TempText";

                            ((TextBlock)await TagSearch.FindFrameworkElementwithTag(card, $"{card.Tag}TimeText")).Text = "Now";

                            NeedToAdd = true;
                        }
                        // Check if the [0] is a Sunrise or Sunset card and if the time is not the same as the current time to fade it out and create a new regular Now card
                        if ((((Border)HourlyStack.Children[0]).Tag.ToString() == "Sunrise" && !(now.Hour == weatherAll.Daily[0].Sunrise.Hour && now.Minute == weatherAll.Daily[0].Sunrise.Minute)) || (((Border)HourlyStack.Children[0]).Tag.ToString() == "Sunset" && !(now.Hour == weatherAll.Daily[0].Sunset.Hour && now.Minute == weatherAll.Daily[0].Sunset.Minute)))
                        {
                            var fadein = new DoubleAnimation
                            {
                                From = 1,
                                To = 0,
                                Duration = TimeSpan.FromSeconds(0.1),
                                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn }
                            };
                            ((Border)HourlyStack.Children[0]).SetValue(OpacityProperty, 1.0);
                            Storyboard.SetTarget(fadein, HourlyStack.Children[0]);
                            Storyboard.SetTargetProperty(fadein, new PropertyPath("Opacity"));
                            var storyboard = new Storyboard();
                            storyboard.Children.Add(fadein);
                            storyboard.Begin();
                            await Task.Delay(100);
                            HourlyStack.Children.RemoveAt(0);
                            string time = "Now";
                            string temp = weatherNow.Temperature.GetValue().ToString("0.#");
                            string icon = await WMO.GetWMOIconName(weatherNow.WMO, isday);
                            double progressvalue = (((weatherNow.Temperature.Kelvin - weatherAll.Daily[0].TemperatureMin.Kelvin) * 80) / (weatherAll.Daily[0].TemperatureMax.Kelvin - weatherAll.Daily[0].TemperatureMin.Kelvin)) + 10;
                            Border hourcard = await CreateHourlyCard($"HourNow", time, temp, icon, progressvalue);
                            {

                                var fadeout = new DoubleAnimation
                                {
                                    From = 0,
                                    To = 1,
                                    Duration = TimeSpan.FromSeconds(0.3),
                                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                                };
                                hourcard.Visibility = Visibility.Visible;
                                hourcard.RenderTransformOrigin = new Point(0.5, 0.5);
                                hourcard.SetValue(RenderTransformProperty, new TranslateTransform(0, 0));
                                Storyboard.SetTarget(fadeout, hourcard);
                                Storyboard.SetTargetProperty(fadeout, new PropertyPath("Opacity"));
                                storyboard = new Storyboard();
                                storyboard.Children.Add(fadeout);
                                storyboard.Begin();
                                RowDefinition ProgressRowDef = ((Grid)await TagSearch.FindFrameworkElementwithTag(hourcard, $"{hourcard.Tag}ProgressGrid")).RowDefinitions[1];
                                var gridup = new GridLengthAnimation
                                {
                                    From = new System.Windows.GridLength(0, System.Windows.GridUnitType.Star),
                                    To = new System.Windows.GridLength(progressvalue / 100, System.Windows.GridUnitType.Star),
                                    Duration = TimeSpan.FromSeconds(0.3),
                                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                                };
                                Storyboard.SetTarget(gridup, ProgressRowDef);
                                Storyboard.SetTargetProperty(gridup, new PropertyPath("Height"));
                                storyboard = new Storyboard();
                                storyboard.Children.Add(gridup);
                                storyboard.Begin();
                            }
                            HourlyStack.Children.Insert(0, hourcard);
                            await Task.Delay(300);
                        }
                        // Check if the [1] is a Sunrise, Sunset or Nextday card and if the time is the same as the current time to shift it to [0] and convert to "Now" and create a new card at the end of the stack
                        if ((((Border)HourlyStack.Children[1]).Tag.ToString() == "Nextday" && now.Hour == 0 && now.Minute == 0) || (((Border)HourlyStack.Children[1]).Tag.ToString() == "Sunrise" && now.Hour == weatherAll.Daily[0].Sunrise.Hour && now.Minute == weatherAll.Daily[0].Sunrise.Minute) || (((Border)HourlyStack.Children[1]).Tag.ToString() == "Sunset" && now.Hour == weatherAll.Daily[0].Sunset.Hour && now.Minute == weatherAll.Daily[0].Sunset.Minute))
                        {
                            var fadeout = new DoubleAnimation
                            {
                                From = 1,
                                To = 0,
                                Duration = TimeSpan.FromSeconds(0.3),
                                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                            };
                            var zoomout = new DoubleAnimation
                            {
                                From = ((Border)HourlyStack.Children[0]).ActualWidth,
                                To = 0,
                                Duration = TimeSpan.FromSeconds(0.3),
                                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                            };
                            ((Border)HourlyStack.Children[0]).SetValue(OpacityProperty, 1.0);
                            ((Border)HourlyStack.Children[0]).SetValue(WidthProperty, ((Border)HourlyStack.Children[0]).ActualWidth);
                            Storyboard.SetTarget(fadeout, HourlyStack.Children[0]);
                            Storyboard.SetTargetProperty(fadeout, new PropertyPath("Opacity"));
                            Storyboard.SetTarget(zoomout, HourlyStack.Children[0]);
                            Storyboard.SetTargetProperty(zoomout, new PropertyPath("Width"));
                            var storyboard = new Storyboard();
                            storyboard.Children.Add(fadeout);
                            storyboard.Children.Add(zoomout);
                            storyboard.Begin();
                            await Task.Delay(300);
                            HourlyStack.Children.RemoveAt(0);
                            Border card = (Border)HourlyStack.Children[0];

                            ((TextBlock)await TagSearch.FindFrameworkElementwithTag(card, $"{card.Tag}TimeText")).Text = "Now";
                        }
                        // Create a new card at the end of the stack if needed
                        if (NeedToAdd)
                        {
                            int hour = 23;
                            string time = weatherAll.Daily[(now.Hour + hour) / 24].Hourly[(now.Hour + hour) % 24].DateTime.ToString("HH:mm");
                            string temp = weatherAll.Daily[(now.Hour + hour) / 24].Hourly[(now.Hour + hour) % 24].Temperature.GetValue().ToString("0.#");
                            bool isday2;
                            if (weatherAll.Daily[(now.Hour + hour) / 24].Hourly[(now.Hour + hour) % 24].DateTime > weatherAll.Daily[(now.Hour + hour) / 24].Sunrise && weatherAll.Daily[(now.Hour + hour) / 24].Hourly[(now.Hour + hour) % 24].DateTime < weatherAll.Daily[(now.Hour + hour) / 24].Sunset)
                            {
                                isday2 = true;
                            }
                            else
                            {
                                isday2 = false;
                            }
                            string icon = await WMO.GetWMOIconName(weatherAll.Daily[(now.Hour + hour) / 24].Hourly[(now.Hour + hour) % 24].WMO, isday2);
                            double progressvalue = (((weatherAll.Daily[(now.Hour + hour) / 24].Hourly[(now.Hour + hour) % 24].Temperature.Kelvin - weatherAll.Daily[(now.Hour + hour) / 24].TemperatureMin.Kelvin) * 80) / (weatherAll.Daily[(now.Hour + hour) / 24].TemperatureMax.Kelvin - weatherAll.Daily[(now.Hour + hour) / 24].TemperatureMin.Kelvin)) + 10;
                            Border hourcard = await CreateHourlyCard($"Hour{(weatherAll.Daily[(now.Hour + hour) / 24].Hourly[(now.Hour + hour) % 24].DateTime.ToString("HH"))}", time, temp, icon, progressvalue);
                            {

                                var fadein = new DoubleAnimation
                                {
                                    From = 0,
                                    To = 1,
                                    Duration = TimeSpan.FromSeconds(0.3),
                                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                                };
                                var translate = new DoubleAnimation
                                {
                                    From = 30,
                                    To = 0,
                                    Duration = TimeSpan.FromSeconds(0.3),
                                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                                };
                                hourcard.Visibility = Visibility.Visible;
                                hourcard.RenderTransformOrigin = new Point(0.5, 0.5);
                                hourcard.SetValue(RenderTransformProperty, new TranslateTransform(0, 0));
                                Storyboard.SetTarget(fadein, hourcard);
                                Storyboard.SetTargetProperty(fadein, new PropertyPath("Opacity"));
                                Storyboard.SetTarget(translate, hourcard);
                                Storyboard.SetTargetProperty(translate, new PropertyPath("(UIElement.RenderTransform).(TranslateTransform.X)"));
                                var storyboard = new Storyboard();
                                storyboard.Children.Add(fadein);
                                storyboard.Children.Add(translate);
                                storyboard.Begin();
                                RowDefinition ProgressRowDef = ((Grid)await TagSearch.FindFrameworkElementwithTag(hourcard, $"{hourcard.Tag}ProgressGrid")).RowDefinitions[1];
                                var gridup = new GridLengthAnimation
                                {
                                    From = new System.Windows.GridLength(0, System.Windows.GridUnitType.Star),
                                    To = new System.Windows.GridLength(progressvalue / 100, System.Windows.GridUnitType.Star),
                                    Duration = TimeSpan.FromSeconds(0.3),
                                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                                };
                                Storyboard.SetTarget(gridup, ProgressRowDef);
                                Storyboard.SetTargetProperty(gridup, new PropertyPath("Height"));
                                storyboard = new Storyboard();
                                storyboard.Children.Add(gridup);
                                storyboard.Begin();
                                await Task.Delay(30);
                            }
                            HourlyStack.Children.Add(hourcard);

                            if (((now.Hour + hour) % 24) + 1 < 24) // Check if the next hour is the same day
                            {
                                // Check if the next hour is sunrise or sunset 
                                if (weatherAll.Daily[(now.Hour + hour) / 24].Hourly[(now.Hour + hour) % 24].DateTime.Hour == weatherAll.Daily[(now.Hour + hour) / 24].Sunrise.Hour && weatherAll.Daily[(now.Hour + hour) / 24].Hourly[((now.Hour + hour) % 24) + 1].DateTime.Hour > weatherAll.Daily[(now.Hour + hour) / 24].Sunrise.Hour && now <= weatherAll.Daily[(now.Hour + hour) / 24].Sunrise)
                                {
                                    Border suncard = await CreateHourlySunCard("Sunrise", weatherAll.Daily[(now.Hour + hour) / 24].Sunrise.ToString("HH:mm"), "sunrise.png", "Sunrise");
                                    {
                                        var fadein = new DoubleAnimation
                                        {
                                            From = 0,
                                            To = 1,
                                            Duration = TimeSpan.FromSeconds(0.3),
                                            EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                                        };
                                        var translate = new DoubleAnimation
                                        {
                                            From = 30,
                                            To = 0,
                                            Duration = TimeSpan.FromSeconds(0.3),
                                            EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                                        };
                                        suncard.Visibility = Visibility.Visible;
                                        suncard.RenderTransformOrigin = new Point(0.5, 0.5);
                                        suncard.SetValue(RenderTransformProperty, new TranslateTransform(0, 0));
                                        Storyboard.SetTarget(fadein, suncard);
                                        Storyboard.SetTargetProperty(fadein, new PropertyPath("Opacity"));
                                        Storyboard.SetTarget(translate, suncard);
                                        Storyboard.SetTargetProperty(translate, new PropertyPath("(UIElement.RenderTransform).(TranslateTransform.X)"));
                                        var storyboard = new Storyboard();
                                        storyboard.Children.Add(fadein);
                                        storyboard.Children.Add(translate);
                                        storyboard.Begin();
                                        await Task.Delay(30);
                                    }
                                    HourlyStack.Children.Add(suncard);
                                }
                                if (weatherAll.Daily[(now.Hour + hour) / 24].Hourly[(now.Hour + hour) % 24].DateTime.Hour == weatherAll.Daily[(now.Hour + hour) / 24].Sunset.Hour && weatherAll.Daily[(now.Hour + hour) / 24].Hourly[((now.Hour + hour) % 24) + 1].DateTime.Hour > weatherAll.Daily[(now.Hour + hour) / 24].Sunset.Hour && now <= weatherAll.Daily[(now.Hour + hour) / 24].Sunset)
                                {
                                    Border suncard = await CreateHourlySunCard("Sunset", weatherAll.Daily[(now.Hour + hour) / 24].Sunset.ToString("HH:mm"), "sunset.png", "Sunset");
                                    {
                                        var fadein = new DoubleAnimation
                                        {
                                            From = 0,
                                            To = 1,
                                            Duration = TimeSpan.FromSeconds(0.3),
                                            EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                                        };
                                        var translate = new DoubleAnimation
                                        {
                                            From = 30,
                                            To = 0,
                                            Duration = TimeSpan.FromSeconds(0.3),
                                            EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                                        };
                                        suncard.Visibility = Visibility.Visible;
                                        suncard.RenderTransformOrigin = new Point(0.5, 0.5);
                                        suncard.SetValue(RenderTransformProperty, new TranslateTransform(0, 0));
                                        Storyboard.SetTarget(fadein, suncard);
                                        Storyboard.SetTargetProperty(fadein, new PropertyPath("Opacity"));
                                        Storyboard.SetTarget(translate, suncard);
                                        Storyboard.SetTargetProperty(translate, new PropertyPath("(UIElement.RenderTransform).(TranslateTransform.X)"));
                                        var storyboard = new Storyboard();
                                        storyboard.Children.Add(fadein);
                                        storyboard.Children.Add(translate);
                                        storyboard.Begin();
                                        await Task.Delay(30);
                                    }
                                    HourlyStack.Children.Add(suncard);
                                }
                            }

                            if (((now.Hour + hour) % 24) + 1 == 24)
                            {
                                Border nextcard = await CreateHourlySunCard("Nextday", weatherAll.Daily[((now.Hour + hour) / 24) + 1].DateTime.ToString("dd/MM"), "nextday.png", "Tomorrow");
                                {
                                    var fadein = new DoubleAnimation
                                    {
                                        From = 0,
                                        To = 1,
                                        Duration = TimeSpan.FromSeconds(0.3),
                                        EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                                    };
                                    var translate = new DoubleAnimation
                                    {
                                        From = 30,
                                        To = 0,
                                        Duration = TimeSpan.FromSeconds(0.3),
                                        EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                                    };
                                    nextcard.Visibility = Visibility.Visible;
                                    nextcard.RenderTransformOrigin = new Point(0.5, 0.5);
                                    nextcard.SetValue(RenderTransformProperty, new TranslateTransform(0, 0));
                                    Storyboard.SetTarget(fadein, nextcard);
                                    Storyboard.SetTargetProperty(fadein, new PropertyPath("Opacity"));
                                    Storyboard.SetTarget(translate, nextcard);
                                    Storyboard.SetTargetProperty(translate, new PropertyPath("(UIElement.RenderTransform).(TranslateTransform.X)"));
                                    var storyboard = new Storyboard();
                                    storyboard.Children.Add(fadein);
                                    storyboard.Children.Add(translate);
                                    storyboard.Begin();
                                    await Task.Delay(30);
                                }
                                HourlyStack.Children.Add(nextcard);
                            }
                        }

                        // Updating all cards with new data
                        Border nowcard = (Border)await TagSearch.FindFrameworkElementwithTag(HourlyStack, "HourNow");
                        if (nowcard != null)
                        {
                            ((Image)await TagSearch.FindFrameworkElementwithTag(nowcard, $"{nowcard.Tag}Icon")).Source = new BitmapImage(new Uri($"pack://application:,,,/WDC.Weather;component/Resources/wmo/icons/{await WMO.GetWMOIconName(weatherNow.WMO, isday)}"));
                            ((TextBlock)await TagSearch.FindFrameworkElementwithTag(nowcard, $"{nowcard.Tag}TempText")).Text = $"{weatherNow.Temperature.GetValue().ToString("0.#")}°";
                            double newprogressvalue = (((weatherNow.Temperature.Kelvin - weatherAll.Daily[0].TemperatureMin.Kelvin) * 80) / (weatherAll.Daily[0].TemperatureMax.Kelvin - weatherAll.Daily[0].TemperatureMin.Kelvin)) + 10;
                            RowDefinition ProgressRowDef1 = ((Grid)await TagSearch.FindFrameworkElementwithTag(nowcard, $"{nowcard.Tag}ProgressGrid")).RowDefinitions[1];
                            {
                                var gridup = new GridLengthAnimation
                                {
                                    From = ProgressRowDef1.Height,
                                    To = new System.Windows.GridLength(newprogressvalue / 100, System.Windows.GridUnitType.Star),
                                    Duration = TimeSpan.FromSeconds(0.3),
                                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                                };
                                ProgressRowDef1.Height = new System.Windows.GridLength(newprogressvalue / 100, System.Windows.GridUnitType.Star);
                                Storyboard.SetTarget(gridup, ProgressRowDef1);
                                Storyboard.SetTargetProperty(gridup, new PropertyPath("Height"));
                                var storyboard = new Storyboard();
                                storyboard.Children.Add(gridup);
                                storyboard.Begin();
                            }
                        }
                        for (int hour = 1; hour < 24; hour++)
                        {
                            string hourtag = $"Hour{weatherAll.Daily[(now.Hour + hour) / 24].Hourly[(now.Hour + hour) % 24].DateTime.ToString("HH")}";
                            Border hourcard = (Border)await TagSearch.FindFrameworkElementwithTag(HourlyStack, hourtag);
                            if (hourcard != null)
                            {
                                bool isday2;
                                if (weatherAll.Daily[(now.Hour + hour) / 24].Hourly[(now.Hour + hour) % 24].DateTime > weatherAll.Daily[(now.Hour + hour) / 24].Sunrise && weatherAll.Daily[(now.Hour + hour) / 24].Hourly[(now.Hour + hour) % 24].DateTime < weatherAll.Daily[(now.Hour + hour) / 24].Sunset)
                                {
                                    isday2 = true;
                                }
                                else
                                {
                                    isday2 = false;
                                }
                                ((Image)await TagSearch.FindFrameworkElementwithTag(hourcard, $"{hourcard.Tag}Icon")).Source = new BitmapImage(new Uri($"pack://application:,,,/WDC.Weather;component/Resources/wmo/icons/{await WMO.GetWMOIconName(weatherAll.Daily[(now.Hour + hour) / 24].Hourly[(now.Hour + hour) % 24].WMO, isday2)}"));
                                ((TextBlock)await TagSearch.FindFrameworkElementwithTag(hourcard, $"{hourcard.Tag}TempText")).Text = $"{weatherAll.Daily[(now.Hour + hour) / 24].Hourly[(now.Hour + hour) % 24].Temperature.GetValue().ToString("0.#")}°";
                                double progressvalue = (((weatherAll.Daily[(now.Hour + hour) / 24].Hourly[(now.Hour + hour) % 24].Temperature.Kelvin - weatherAll.Daily[(now.Hour + hour) / 24].TemperatureMin.Kelvin) * 80) / (weatherAll.Daily[(now.Hour + hour) / 24].TemperatureMax.Kelvin - weatherAll.Daily[(now.Hour + hour) / 24].TemperatureMin.Kelvin)) + 10;
                                RowDefinition ProgressRowDef2 = ((Grid)await TagSearch.FindFrameworkElementwithTag(hourcard, $"{hourcard.Tag}ProgressGrid")).RowDefinitions[1];
                                {
                                    var gridup = new GridLengthAnimation
                                    {
                                        From = ProgressRowDef2.Height,
                                        To = new System.Windows.GridLength(progressvalue / 100, System.Windows.GridUnitType.Star),
                                        Duration = TimeSpan.FromSeconds(0.3),
                                        EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                                    };
                                    ProgressRowDef2.Height = new System.Windows.GridLength(progressvalue / 100, System.Windows.GridUnitType.Star);
                                    Storyboard.SetTarget(gridup, ProgressRowDef2);
                                    Storyboard.SetTargetProperty(gridup, new PropertyPath("Height"));
                                    var storyboard = new Storyboard();
                                    storyboard.Children.Add(gridup);
                                    storyboard.Begin();
                                }
                            }
                        }
                    }
                }

                IsExecuting = false;
            }
        }

        private async void DayList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            int index = DayListView.SelectedIndex;

            if (index == 0)
            {
                if (weatherNow != null)
                {
                    /*TempText.Text = $"{weatherNow.Temperature.Celsius}°C";
                    FeelsTempText.Text = $"Feels like {weatherNow.FeelsLike.Celsius}°C";
                    MinTempText.Text = $"{weatherAll.Daily[0].TemperatureMin.Celsius}°C";
                    MaxTempText.Text = $"{weatherAll.Daily[0].TemperatureMax.Celsius}°C";
                    HumidityText.Text = $"{weatherNow.Humidity}%";
                    WindText.Text = $"{weatherNow.WindSpeed.Kph} km/h\n{weatherNow.WindDirection.GetCardinal()} {weatherNow.WindDirection.Deg}°";
                    UVText.Text = $"{weatherNow.UVIndex.Index}\n{await weatherNow.UVIndex.GetUVISafety()}";
                    AQText.Text = $"{weatherNow.AirQuality.EUAQI}\n{await weatherNow.AirQuality.GetEUAQISafety()}";
                    PrecipitationText.Text = $"{weatherNow.Precipitation.Mm} mm";
                    PressureText.Text = $"{weatherNow.Pressure.Hpambar} hPa";
                    CloudText.Text = $"{weatherNow.CloudCover}%";
                    VisibilityText.Text = $"{weatherNow.Visibility.Km} km";
                    DateTime now = DateTime.Now;
                    bool isday;
                    if (now > weatherAll.Daily[0].Sunrise && now < weatherAll.Daily[0].Sunset)
                    {
                        isday = true;
                    }
                    else
                    {
                        isday = false;
                    }
                    WMOIcon.Source = await WMO.GetWMOIcon(weatherNow.WMO, isday);
                    WMOBack.ImageSource = await WMO.GetWMOBack(weatherNow.WMO, isday);
                    WMOText.Text = await WMO.GetWMODesc(weatherNow.WMO, isday);

                    HourlyStack.Children.Clear();
                    for (int hour = 0; hour < 24; hour++)
                    {
                        string time = hour == 0 ? "Now" : weatherAll.Daily[(now.Hour + hour) / 24].Hourly[(now.Hour + hour) % 24].DateTime.ToString("HH:mm");
                        string temp = hour == 0 ? weatherNow.Temperature.Celsius.ToString() : weatherAll.Daily[(now.Hour + hour) / 24].Hourly[(now.Hour + hour) % 24].Temperature.Celsius.ToString();
                        bool isday2;
                        if (weatherAll.Daily[(now.Hour + hour) / 24].Hourly[(now.Hour + hour) % 24].DateTime > weatherAll.Daily[(now.Hour + hour) / 24].Sunrise && weatherAll.Daily[(now.Hour + hour) / 24].Hourly[(now.Hour + hour) % 24].DateTime < weatherAll.Daily[(now.Hour + hour) / 24].Sunset)
                        {
                            isday2 = true;
                        }
                        else
                        {
                            isday2 = false;
                        }
                        string icon = await WMO.GetWMOIconName(weatherAll.Daily[(now.Hour + hour) / 24].Hourly[(now.Hour + hour) % 24].WMO, isday2);
                        double progressvalue = (((weatherAll.Daily[(now.Hour + hour) / 24].Hourly[(now.Hour + hour) % 24].Temperature.Kelvin - weatherAll.Daily[(now.Hour + hour) / 24].TemperatureMin.Kelvin) * 80) / (weatherAll.Daily[(now.Hour + hour) / 24].TemperatureMax.Kelvin - weatherAll.Daily[(now.Hour + hour) / 24].TemperatureMin.Kelvin)) + 10;
                        await CreateHourlyCard(time, temp, icon, progressvalue);

                        if (((now.Hour + hour) % 24) + 1 < 24)
                        {
                            if (weatherAll.Daily[(now.Hour + hour) / 24].Hourly[(now.Hour + hour) % 24].DateTime.Hour == weatherAll.Daily[(now.Hour + hour) / 24].Sunrise.Hour && weatherAll.Daily[(now.Hour + hour) / 24].Hourly[((now.Hour + hour) % 24) + 1].DateTime.Hour > weatherAll.Daily[(now.Hour + hour) / 24].Sunrise.Hour)
                            {
                                await CreateHourlySunCard(weatherAll.Daily[(now.Hour + hour) / 24].Sunrise.ToString("HH:mm"), "sunrise.png", "Sunrise");
                            }
                            if (weatherAll.Daily[(now.Hour + hour) / 24].Hourly[(now.Hour + hour) % 24].DateTime.Hour == weatherAll.Daily[(now.Hour + hour) / 24].Sunset.Hour && weatherAll.Daily[(now.Hour + hour) / 24].Hourly[((now.Hour + hour) % 24) + 1].DateTime.Hour > weatherAll.Daily[(now.Hour + hour) / 24].Sunset.Hour)
                            {
                                await CreateHourlySunCard(weatherAll.Daily[(now.Hour + hour) / 24].Sunset.ToString("HH:mm"), "sunset.png", "Sunset");
                            }
                        }

                        if (((now.Hour + hour) % 24) + 1 == 24)
                        {
                            await CreateHourlySunCard(weatherAll.Daily[((now.Hour + hour) / 24) + 1].DateTime.ToString("dd/MM"), "nextday.png", "Tomorrow");
                        }
                    }*/
                    await UIUpdate();
                }
            }
            else
            {
                UIStatus = "daily";
                if (weatherAll != null)
                {
                    DayListView.IsEnabled = false;

                    TempText.Text = await weatherAll.Daily[index].GlobalDay.Temperature.GetFormatedText();
                    FeelsTempText.Text = await Lang.GetLang("weather.feelslike", await weatherAll.Daily[index].GlobalDay.FeelsLike.GetFormatedText());
                    MinTempText.Text = await weatherAll.Daily[index].TemperatureMin.GetFormatedText();
                    MaxTempText.Text = await weatherAll.Daily[index].TemperatureMax.GetFormatedText();
                    HumidityText.Text = await Lang.GetLang("units.percent.percent", weatherAll.Daily[index].GlobalDay.Humidity.ToString());
                    WindText.Text = $"{await weatherAll.Daily[index].GlobalDay.WindSpeed.GetFormatedText()}\n{await weatherAll.Daily[index].GlobalDay.WindDirection.GetCardinal()} {weatherAll.Daily[index].GlobalDay.WindDirection.Deg}°";
                    UVText.Text = $"{weatherAll.Daily[index].GlobalDay.UVIndex.GetValue().ToString("0.#")}\n{await weatherAll.Daily[index].GlobalDay.UVIndex.GetUVISafety()}";
                    if (weatherAll.Daily[index].GlobalDay.AirQuality != null)
                    {
                        AQText.Text = $"{weatherAll.Daily[index].GlobalDay.AirQuality.GetValue()}\n{await weatherAll.Daily[index].GlobalDay.AirQuality.GetSafety()}";
                    }
                    else
                    {
                        AQText.Text = "N/A";
                    }
                    PrecipitationText.Text = await weatherAll.Daily[index].GlobalDay.Precipitation.GetFormatedText();
                    PressureText.Text = await weatherAll.Daily[index].GlobalDay.Pressure.GetFormatedText();
                    CloudText.Text = await Lang.GetLang("units.percent.percent", weatherAll.Daily[index].GlobalDay.CloudCover.ToString());
                    VisibilityText.Text = await weatherAll.Daily[index].GlobalDay.Visibility.GetFormatedText();
                    UpdateWMOUI(weatherAll.Daily[index].GlobalDay.WMO, true);

                    foreach (Border card in HourlyStack.Children)
                    {
                        var fadeout = new DoubleAnimation
                        {
                            From = 1,
                            To = 0,
                            Duration = TimeSpan.FromSeconds(0.1),
                            EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn }
                        };
                        card.SetValue(OpacityProperty, 1.0);
                        Storyboard.SetTarget(fadeout, card);
                        Storyboard.SetTargetProperty(fadeout, new PropertyPath("Opacity"));
                        var storyboard = new Storyboard();
                        storyboard.Children.Add(fadeout);
                        storyboard.Begin();
                        await Task.Delay(10);
                    }
                    HourlyStack.Children.Clear();
                    for (int hour = 0; hour < 24; hour++)
                    {
                        string time = weatherAll.Daily[index].Hourly[hour].DateTime.ToString("HH:mm");
                        string temp = weatherAll.Daily[index].Hourly[hour].Temperature.GetValue().ToString("0.#");
                        bool isday2;
                        if (weatherAll.Daily[index].Hourly[hour].DateTime > weatherAll.Daily[index].Sunrise && weatherAll.Daily[index].Hourly[hour].DateTime < weatherAll.Daily[index].Sunset)
                        {
                            isday2 = true;
                        }
                        else
                        {
                            isday2 = false;
                        }
                        string icon = await WMO.GetWMOIconName(weatherAll.Daily[index].Hourly[hour].WMO, isday2);
                        double progressvalue = (((weatherAll.Daily[index].Hourly[hour].Temperature.Kelvin - weatherAll.Daily[index].TemperatureMin.Kelvin) * 80) / (weatherAll.Daily[index].TemperatureMax.Kelvin - weatherAll.Daily[index].TemperatureMin.Kelvin)) + 10;
                        Border hourcard = await CreateHourlyCard($"Hour{weatherAll.Daily[index].Hourly[hour].DateTime.ToString("HH")}", time, temp, icon, progressvalue);
                        {
                            var fadein = new DoubleAnimation
                            {
                                From = 0,
                                To = 1,
                                Duration = TimeSpan.FromSeconds(0.3),
                                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                            };
                            var translate = new DoubleAnimation
                            {
                                From = 30,
                                To = 0,
                                Duration = TimeSpan.FromSeconds(0.3),
                                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                            };
                            hourcard.Visibility = Visibility.Visible;
                            hourcard.RenderTransformOrigin = new Point(0.5, 0.5);
                            hourcard.SetValue(RenderTransformProperty, new TranslateTransform(0, 0));
                            Storyboard.SetTarget(fadein, hourcard);
                            Storyboard.SetTargetProperty(fadein, new PropertyPath("Opacity"));
                            Storyboard.SetTarget(translate, hourcard);
                            Storyboard.SetTargetProperty(translate, new PropertyPath("(UIElement.RenderTransform).(TranslateTransform.X)"));
                            var storyboard = new Storyboard();
                            storyboard.Children.Add(fadein);
                            storyboard.Children.Add(translate);
                            storyboard.Begin();
                            RowDefinition ProgressRowDef = ((Grid)await TagSearch.FindFrameworkElementwithTag(hourcard, $"{hourcard.Tag}ProgressGrid")).RowDefinitions[1];
                            var gridup = new GridLengthAnimation
                            {
                                From = new System.Windows.GridLength(0, System.Windows.GridUnitType.Star),
                                To = new System.Windows.GridLength(progressvalue / 100, System.Windows.GridUnitType.Star),
                                Duration = TimeSpan.FromSeconds(0.3),
                                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                            };
                            Storyboard.SetTarget(gridup, ProgressRowDef);
                            Storyboard.SetTargetProperty(gridup, new PropertyPath("Height"));
                            storyboard = new Storyboard();
                            storyboard.Children.Add(gridup);
                            storyboard.Begin();
                            await Task.Delay(30);
                        }
                        HourlyStack.Children.Add(hourcard);

                        if (hour + 1 < 24)
                        {
                            if (weatherAll.Daily[index].Hourly[hour].DateTime.Hour == weatherAll.Daily[index].Sunrise.Hour && weatherAll.Daily[index].Hourly[hour + 1].DateTime.Hour > weatherAll.Daily[index].Sunrise.Hour)
                            {
                                Border suncard = await CreateHourlySunCard("Sunrise", weatherAll.Daily[index].Sunrise.ToString("HH:mm"), "sunrise.png", "Sunrise");
                                {
                                    var fadein = new DoubleAnimation
                                    {
                                        From = 0,
                                        To = 1,
                                        Duration = TimeSpan.FromSeconds(0.3),
                                        EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                                    };
                                    var translate = new DoubleAnimation
                                    {
                                        From = 30,
                                        To = 0,
                                        Duration = TimeSpan.FromSeconds(0.3),
                                        EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                                    };
                                    suncard.Visibility = Visibility.Visible;
                                    suncard.RenderTransformOrigin = new Point(0.5, 0.5);
                                    suncard.SetValue(RenderTransformProperty, new TranslateTransform(0, 0));
                                    Storyboard.SetTarget(fadein, suncard);
                                    Storyboard.SetTargetProperty(fadein, new PropertyPath("Opacity"));
                                    Storyboard.SetTarget(translate, suncard);
                                    Storyboard.SetTargetProperty(translate, new PropertyPath("(UIElement.RenderTransform).(TranslateTransform.X)"));
                                    var storyboard = new Storyboard();
                                    storyboard.Children.Add(fadein);
                                    storyboard.Children.Add(translate);
                                    storyboard.Begin();
                                    await Task.Delay(30);
                                }
                                HourlyStack.Children.Add(suncard);
                            }
                            if (weatherAll.Daily[index].Hourly[hour].DateTime.Hour == weatherAll.Daily[index].Sunset.Hour && weatherAll.Daily[index].Hourly[hour + 1].DateTime.Hour > weatherAll.Daily[index].Sunset.Hour)
                            {
                                Border suncard = await CreateHourlySunCard("Sunset", weatherAll.Daily[index].Sunset.ToString("HH:mm"), "sunset.png", "Sunset");
                                {
                                    var fadein = new DoubleAnimation
                                    {
                                        From = 0,
                                        To = 1,
                                        Duration = TimeSpan.FromSeconds(0.3),
                                        EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                                    };
                                    var translate = new DoubleAnimation
                                    {
                                        From = 30,
                                        To = 0,
                                        Duration = TimeSpan.FromSeconds(0.3),
                                        EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                                    };
                                    suncard.Visibility = Visibility.Visible;
                                    suncard.RenderTransformOrigin = new Point(0.5, 0.5);
                                    suncard.SetValue(RenderTransformProperty, new TranslateTransform(0, 0));
                                    Storyboard.SetTarget(fadein, suncard);
                                    Storyboard.SetTargetProperty(fadein, new PropertyPath("Opacity"));
                                    Storyboard.SetTarget(translate, suncard);
                                    Storyboard.SetTargetProperty(translate, new PropertyPath("(UIElement.RenderTransform).(TranslateTransform.X)"));
                                    var storyboard = new Storyboard();
                                    storyboard.Children.Add(fadein);
                                    storyboard.Children.Add(translate);
                                    storyboard.Begin();
                                    await Task.Delay(30);
                                }
                                HourlyStack.Children.Add(suncard);
                            }
                        }
                    }

                    DayListView.IsEnabled = true;
                }
            }
        }

        public async Task<Border> CreateHourlyCard(string tagid, string time, string temp, string icon, double progressvalue)
        {
            Border MainCard = new Border();
            MainCard.Width = 40;
            MainCard.CornerRadius = new System.Windows.CornerRadius(8);
            MainCard.Margin = new System.Windows.Thickness(2.5);
            MainCard.ClipToBounds = true;
            MainCard.Background = (Brush)new BrushConverter().ConvertFrom("#0affffff");
            MainCard.Tag = $"{tagid}";

            Grid MainGrid = new Grid();
            MainGrid.RowDefinitions.Add(new RowDefinition() { Height = new System.Windows.GridLength(32) });
            MainGrid.RowDefinitions.Add(new RowDefinition() { Height = new System.Windows.GridLength(1, System.Windows.GridUnitType.Star) });
            MainGrid.RowDefinitions.Add(new RowDefinition() { Height = new System.Windows.GridLength(20) });

            Image Icon = new Image();
            Icon.Source = new BitmapImage(new Uri($"pack://application:,,,/WDC.Weather;component/Resources/wmo/icons/{icon}"));
            Icon.SetValue(RenderOptions.BitmapScalingModeProperty, BitmapScalingMode.HighQuality);
            Icon.SetValue(Grid.RowProperty, 0);
            Icon.Margin = new System.Windows.Thickness(0, 5, 0, 5);
            Icon.Tag = $"{tagid}Icon";

            Border ProgressBack = new Border();
            ProgressBack.Background = (Brush)new BrushConverter().ConvertFrom("#0affffff");
            ProgressBack.SetValue(Grid.RowProperty, 1);
            ProgressBack.VerticalAlignment = System.Windows.VerticalAlignment.Stretch;
            ProgressBack.HorizontalAlignment = System.Windows.HorizontalAlignment.Stretch;
            ProgressBack.CornerRadius = new System.Windows.CornerRadius(8);
            ProgressBack.Margin = new System.Windows.Thickness(3);
            ProgressBack.ClipToBounds = true;

            Grid ProgressGrid = new Grid();
            if (progressvalue < 0) progressvalue = 0;
            if (progressvalue > 100) progressvalue = 100;
            ProgressGrid.RowDefinitions.Add(new RowDefinition() { Height = new System.Windows.GridLength(1 - progressvalue / 100, System.Windows.GridUnitType.Star) });
            ProgressGrid.RowDefinitions.Add(new RowDefinition() { Height = new System.Windows.GridLength(progressvalue / 100, System.Windows.GridUnitType.Star) });
            ProgressGrid.VerticalAlignment = System.Windows.VerticalAlignment.Stretch;
            ProgressGrid.Tag = $"{tagid}ProgressGrid";
            /*
            MainCard.IsVisibleChanged += (s, e) =>
            {
                if (MainCard.IsVisible)
                {
                    var gridup = new GridLengthAnimation
                    {
                        From = new System.Windows.GridLength(0, System.Windows.GridUnitType.Star),
                        To = new System.Windows.GridLength(progressvalue / 100, System.Windows.GridUnitType.Star),
                        Duration = TimeSpan.FromSeconds(0.3),
                        EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                    };
                    Storyboard.SetTarget(gridup, ProgressRowDef);
                    Storyboard.SetTargetProperty(gridup, new PropertyPath("Height"));
                    var storyboard = new Storyboard();
                    storyboard.Children.Add(gridup);
                    storyboard.Begin();
                }
            };*/

            TextBlock Temp = new TextBlock();
            Temp.Text = $"{temp}°";
            Temp.FontSize = 13;
            Temp.Foreground = (Brush)this.FindResource("TextFillColorPrimaryBrush");
            Temp.FontFamily = new System.Windows.Media.FontFamily("Segoe UI Variable");
            Temp.VerticalAlignment = System.Windows.VerticalAlignment.Bottom;
            Temp.HorizontalAlignment = System.Windows.HorizontalAlignment.Center;
            Temp.SetValue(Grid.RowProperty, 0);
            Temp.Tag = $"{tagid}TempText";

            Border ProgressFore = new Border();
            ProgressFore.Background = (Brush)this.FindResource("AccentTextFillColorTertiaryBrush");
            ProgressFore.CornerRadius = new System.Windows.CornerRadius(8);
            ProgressFore.SetValue(Grid.RowProperty, 1);
            ProgressFore.Margin = new System.Windows.Thickness(3);
            //ProgressFore.Height = progressvalue;
            ProgressFore.VerticalAlignment = System.Windows.VerticalAlignment.Stretch;

            TextBlock Time = new TextBlock();
            Time.Text = time;
            Time.FontSize = 13;
            Time.Foreground = (Brush)this.FindResource("TextFillColorDisabledBrush");
            Time.FontFamily = new System.Windows.Media.FontFamily("Segoe UI Variable");
            Time.VerticalAlignment = System.Windows.VerticalAlignment.Center;
            Time.HorizontalAlignment = System.Windows.HorizontalAlignment.Center;
            Time.SetValue(Grid.RowProperty, 2);
            Time.Tag = $"{tagid}TimeText";

            ProgressGrid.Children.Add(Temp);
            ProgressGrid.Children.Add(ProgressFore);
            ProgressBack.Child = ProgressGrid;
            MainGrid.Children.Add(Icon);
            MainGrid.Children.Add(ProgressBack);
            MainGrid.Children.Add(Time);
            MainCard.Child = MainGrid;

            return MainCard;
        }

        public async Task<Border> CreateHourlySunCard(string tagid, string time, string icon, string name)
        {
            Border MainCard = new Border();
            MainCard.Width = 40;
            MainCard.CornerRadius = new System.Windows.CornerRadius(8);
            MainCard.Margin = new System.Windows.Thickness(2.5);
            MainCard.ClipToBounds = true;
            MainCard.Background = (Brush)new BrushConverter().ConvertFrom("#0affffff");
            MainCard.Tag = $"{tagid}";

            Grid MainGrid = new Grid();
            MainGrid.RowDefinitions.Add(new RowDefinition() { Height = new System.Windows.GridLength(32) });
            MainGrid.RowDefinitions.Add(new RowDefinition() { Height = new System.Windows.GridLength(1, System.Windows.GridUnitType.Star) });
            MainGrid.RowDefinitions.Add(new RowDefinition() { Height = new System.Windows.GridLength(20) });

            Image Icon = new Image();
            Icon.Source = new BitmapImage(new Uri($"pack://application:,,,/WDC.Weather;component/Resources/icons/{icon}"));
            Icon.SetValue(RenderOptions.BitmapScalingModeProperty, BitmapScalingMode.HighQuality);
            Icon.SetValue(Grid.RowProperty, 0);
            Icon.Margin = new System.Windows.Thickness(0, 5, 0, 5);
            Icon.Tag = $"{tagid}Icon";

            Border ProgressBack = new Border();
            ProgressBack.Background = (Brush)new BrushConverter().ConvertFrom("#0affffff");
            ProgressBack.SetValue(Grid.RowProperty, 1);
            ProgressBack.VerticalAlignment = System.Windows.VerticalAlignment.Stretch;
            ProgressBack.HorizontalAlignment = System.Windows.HorizontalAlignment.Stretch;
            ProgressBack.CornerRadius = new System.Windows.CornerRadius(8);
            ProgressBack.Margin = new System.Windows.Thickness(3);
            ProgressBack.ClipToBounds = true;

            Grid ProgressGrid = new Grid();
            ProgressGrid.VerticalAlignment = System.Windows.VerticalAlignment.Center;

            TextBlock Sun = new TextBlock();
            Sun.Text = name;
            Sun.FontSize = 13;
            Sun.Foreground = (Brush)this.FindResource("TextFillColorPrimaryBrush");
            Sun.FontFamily = new System.Windows.Media.FontFamily("Segoe UI Variable");
            Sun.VerticalAlignment = System.Windows.VerticalAlignment.Center;
            Sun.HorizontalAlignment = System.Windows.HorizontalAlignment.Center;
            RotateTransform rotate = new RotateTransform(-90);
            Sun.LayoutTransform = rotate;
            Sun.Tag = $"{tagid}SunText";

            TextBlock Time = new TextBlock();
            Time.Text = time;
            Time.FontSize = 13;
            Time.Foreground = (Brush)this.FindResource("TextFillColorDisabledBrush");
            Time.FontFamily = new System.Windows.Media.FontFamily("Segoe UI Variable");
            Time.VerticalAlignment = System.Windows.VerticalAlignment.Center;
            Time.HorizontalAlignment = System.Windows.HorizontalAlignment.Center;
            Time.SetValue(Grid.RowProperty, 2);
            Time.Tag = $"{tagid}TimeText";

            ProgressGrid.Children.Add(Sun);
            ProgressBack.Child = ProgressGrid;
            MainGrid.Children.Add(Icon);
            MainGrid.Children.Add(ProgressBack);
            MainGrid.Children.Add(Time);
            MainCard.Child = MainGrid;

            return MainCard;
        }

        private void HourlyScroll_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            HourlyScroll.HorizontalScrollBarVisibility = ScrollBarVisibility.Visible;
        }

        private void HourlyScroll_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            HourlyScroll.HorizontalScrollBarVisibility = ScrollBarVisibility.Hidden;
        }

        private int actualwmo = -1;
        private bool actualisday = true;
        private async Task UpdateWMOUI(int wmo, bool isday)
        {
            if (wmo != actualwmo || isday != actualisday)
            {
                actualwmo = wmo;
                actualisday = isday;

                {
                    var iconzoomout1 = new DoubleAnimation
                    {
                        From = 1,
                        To = 0,
                        Duration = TimeSpan.FromSeconds(0.15),
                        EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn }
                    };
                    var iconzoomout2 = new DoubleAnimation
                    {
                        From = 1,
                        To = 0,
                        Duration = TimeSpan.FromSeconds(0.15),
                        EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn }
                    };
                    var fadeout = new DoubleAnimation
                    {
                        From = 1,
                        To = 0,
                        Duration = TimeSpan.FromSeconds(0.15),
                        EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn }
                    };
                    Storyboard.SetTarget(iconzoomout1, WMOIcon);
                    Storyboard.SetTargetProperty(iconzoomout1, new PropertyPath("RenderTransform.(ScaleTransform.ScaleX)"));
                    Storyboard.SetTarget(iconzoomout2, WMOIcon);
                    Storyboard.SetTargetProperty(iconzoomout2, new PropertyPath("RenderTransform.(ScaleTransform.ScaleY)"));
                    Storyboard.SetTarget(fadeout, WMOBackBorder);
                    Storyboard.SetTargetProperty(fadeout, new PropertyPath("Opacity"));
                    var storyboard = new Storyboard();
                    storyboard.Children.Add(iconzoomout1);
                    storyboard.Children.Add(iconzoomout2);
                    storyboard.Children.Add(fadeout);
                    storyboard.Begin();
                }

                await Task.Delay(150);

                WMOIcon.Source = await WMO.GetWMOIcon(wmo, isday);
                WMOBack.ImageSource = await WMO.GetWMOBack(wmo, isday);
                WMOText.Text = await WMO.GetWMODesc(wmo, isday);

                {
                    var iconzoomin1 = new DoubleAnimation
                    {
                        From = 0,
                        To = 1,
                        Duration = TimeSpan.FromSeconds(0.5),
                        EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                    };
                    var iconzoomin2 = new DoubleAnimation
                    {
                        From = 0,
                        To = 1,
                        Duration = TimeSpan.FromSeconds(0.5),
                        EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                    };
                    var fadein = new DoubleAnimation
                    {
                        From = 0,
                        To = 1,
                        Duration = TimeSpan.FromSeconds(0.5),
                        EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                    };
                    Storyboard.SetTarget(iconzoomin1, WMOIcon);
                    Storyboard.SetTargetProperty(iconzoomin1, new PropertyPath("RenderTransform.(ScaleTransform.ScaleX)"));
                    Storyboard.SetTarget(iconzoomin2, WMOIcon);
                    Storyboard.SetTargetProperty(iconzoomin2, new PropertyPath("RenderTransform.(ScaleTransform.ScaleY)"));
                    Storyboard.SetTarget(fadein, WMOBackBorder);
                    Storyboard.SetTargetProperty(fadein, new PropertyPath("Opacity"));
                    var storyboard = new Storyboard();
                    storyboard.Children.Add(iconzoomin1);
                    storyboard.Children.Add(iconzoomin2);
                    storyboard.Children.Add(fadein);
                    storyboard.Begin();
                }
            }
        }
    }
}
