using Newtonsoft.Json.Linq;
using System.Globalization;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows.Controls;
using WDC.Weather.Utils;

namespace WDC.Weather
{
    /// <summary>
    /// Interaction logic for Main.xaml
    /// </summary>
    public partial class Settings : Page
    {
        public Settings()
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
                await LoadLang();
                Init = true;

                DecimalSwitch.Checked += (s, e) => Config.NewVariables.FloatMode = true;
                DecimalSwitch.Unchecked += (s, e) => Config.NewVariables.FloatMode = false;
            }
            await LoadConfig();
        }

        public async Task LoadLang()
        {
            // Place your code here to apply the language to the controls in the settings page
            // - You can get the language from the main program with:
            //   Plugin plugin = new Plugin();
            //   plugin.Language;
        }

        public async Task LoadConfig()
        {
            // Place your code here to load the configuration, and set the values to the controls in the settings page

            Location location = Config.Variables.Location;
            if (location.Latitude != 123456 && location.Longitude != 654321)
            {
                LocationDesc.Text = "Current location: " + location.ToStringAutosuggest();
            }

            if (Config.Variables.FloatMode)
            {
                DecimalSwitch.IsChecked = true;
            }
            else
            {
                DecimalSwitch.IsChecked = false;
            }

            switch (Config.Variables.DistanceUnit)
            {
                case Units.DistanceUnit.M:
                    DistanceBox.SelectedIndex = 0;
                    break;
                case Units.DistanceUnit.Km:
                    DistanceBox.SelectedIndex = 1;
                    break;
                case Units.DistanceUnit.Miles:
                    DistanceBox.SelectedIndex = 2;
                    break;
            }

            switch (Config.Variables.SpeedUnit)
            {
                case Units.SpeedUnit.Ms:
                    SpeedBox.SelectedIndex = 0;
                    break;
                case Units.SpeedUnit.Kph:
                    SpeedBox.SelectedIndex = 1;
                    break;
                case Units.SpeedUnit.Mph:
                    SpeedBox.SelectedIndex = 2;
                    break;
            }

            switch (Config.Variables.TemperatureUnit)
            {
                case Units.TemperatureUnit.Celsius:
                    TemperatureBox.SelectedIndex = 0;
                    break;
                case Units.TemperatureUnit.Fahrenheit:
                    TemperatureBox.SelectedIndex = 1;
                    break;
            }

            switch (Config.Variables.PrecipitationUnit)
            {
                case Units.PrecipitationUnit.Mm:
                    PrecipitationBox.SelectedIndex = 0;
                    break;
                case Units.PrecipitationUnit.Inches:
                    PrecipitationBox.SelectedIndex = 1;
                    break;
            }

            switch (Config.Variables.AirQualityUnit)
            {
                case Units.AirQualityUnit.USAQI:
                    AIQBox.SelectedIndex = 0;
                    break;
                case Units.AirQualityUnit.EUAQI:
                    AIQBox.SelectedIndex = 1;
                    break;
            }
        }

        public async Task SaveConfig()
        {
            // Place your code here to save the configuration
            // - You don't need to create "Save" button, the main program contains a button to save the configuration and it will call this method

            // You need to save the configuration in this folder: PluginDataPath
            // - You can get the variable with:
            //   Plugin plugin = new Plugin();
            //   plugin.PluginDataPath;

            await Config.SaveConfig();
        }


        Dictionary<string, Location> results = new Dictionary<string, Location>();
        private async void LocationBox_QuerySubmitted(Wpf.Ui.Controls.AutoSuggestBox sender, Wpf.Ui.Controls.AutoSuggestBoxQuerySubmittedEventArgs args)
        {
            LocationBox.IsSuggestionListOpen = true;
            LocationBox.ItemsSource = new string[] { "Loading..." };

            string apiurl = "https://geocoding-api.open-meteo.com/v1/search?count=100&name=";

            HttpClient client = new HttpClient();
            string query = args.QueryText.Replace(" ", "+");

            string response = await client.GetStringAsync(apiurl + query);
            if (response == null)
            {
                LocationBox.ItemsSource = new string[] { "No internet" };
            }
            else
            {
                JObject json = JObject.Parse(response);
                if (json["results"] == null)
                {
                    LocationBox.ItemsSource = new string[] { "No result" };
                }
                else
                {
                    results.Clear();
                    foreach (var item in json["results"])
                    {
                        Location location = new Location();
                        location.Name = item["name"]?.ToString();
                        location.Country = item["country"]?.ToString();
                        location.Admin1 = item["admin1"]?.ToString();
                        location.Admin2 = item["admin2"]?.ToString();
                        location.Admin3 = item["admin3"]?.ToString();
                        location.Admin4 = item["admin4"]?.ToString();
                        location.Elevation = (item["elevation"] != null) ? double.Parse(item["elevation"].ToString()) : null;
                        location.Latitude = double.Parse(item["latitude"].ToString());
                        location.Longitude = double.Parse(item["longitude"].ToString());
                        results.Add(location.ToStringAutosuggest(), location);
                    }
                    LocationBox.ItemsSource = results.Keys;
                }
            }
        }

        private async void LocationBox_SuggestionChosen(Wpf.Ui.Controls.AutoSuggestBox sender, Wpf.Ui.Controls.AutoSuggestBoxSuggestionChosenEventArgs args)
        {
            if (results.ContainsKey(args.SelectedItem.ToString()))
            {
                Location location = results[args.SelectedItem.ToString()];
                LocationDesc.Text = "New location: " + location.ToStringAutosuggest();
                LocationBox.IsSuggestionListOpen = false;
                Config.NewVariables.Location = location;
                await Task.Delay(100);
                LocationBox.Text = "";
            }
        }

        private void DistanceBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DistanceBox.SelectedIndex == 0)
            {
                Config.NewVariables.DistanceUnit = Units.DistanceUnit.M;
            }
            else if (DistanceBox.SelectedIndex == 1)
            {
                Config.NewVariables.DistanceUnit = Units.DistanceUnit.Km;
            }
            else if (DistanceBox.SelectedIndex == 2)
            {
                Config.NewVariables.DistanceUnit = Units.DistanceUnit.Miles;
            }
        }

        private void SpeedBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (SpeedBox.SelectedIndex == 0)
            {
                Config.NewVariables.SpeedUnit = Units.SpeedUnit.Ms;
            }
            else if (SpeedBox.SelectedIndex == 1)
            {
                Config.NewVariables.SpeedUnit = Units.SpeedUnit.Kph;
            }
            else if (SpeedBox.SelectedIndex == 2)
            {
                Config.NewVariables.SpeedUnit = Units.SpeedUnit.Mph;
            }
        }

        private void TemperatureBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (TemperatureBox.SelectedIndex == 0)
            {
                Config.NewVariables.TemperatureUnit = Units.TemperatureUnit.Celsius;
            }
            else if (TemperatureBox.SelectedIndex == 1)
            {
                Config.NewVariables.TemperatureUnit = Units.TemperatureUnit.Fahrenheit;
            }
        }

        private void PrecipitationBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (PrecipitationBox.SelectedIndex == 0)
            {
                Config.NewVariables.PrecipitationUnit = Units.PrecipitationUnit.Mm;
            }
            else if (PrecipitationBox.SelectedIndex == 1)
            {
                Config.NewVariables.PrecipitationUnit = Units.PrecipitationUnit.Inches;
            }
        }

        private void AIQBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (AIQBox.SelectedIndex == 0)
            {
                Config.NewVariables.AirQualityUnit = Units.AirQualityUnit.USAQI;
            }
            else if (AIQBox.SelectedIndex == 1)
            {
                Config.NewVariables.AirQualityUnit = Units.AirQualityUnit.EUAQI;
            }
        }
    }
}
