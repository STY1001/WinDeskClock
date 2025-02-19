using System;
using System.Collections.Generic;
using System.IO;
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

namespace WinDeskClock.Clocks
{
    /// <summary>
    /// Interaction logic for FbxClock.xaml
    /// </summary>

    public static class FbxStyle
    {
        // Freebox number style is just a 5x5 matrix of monochrome pixels (#ffff00)
        public static List<byte[][]> FbxNumberStyleMap = new List<byte[][]>()
        {
            new byte[][]
            {
                new byte[] { 1, 1, 1, 1, 1 },
                new byte[] { 1, 0, 0, 0, 1 },
                new byte[] { 1, 0, 0, 0, 1 },
                new byte[] { 1, 0, 0, 0, 1 },
                new byte[] { 1, 1, 1, 1, 1 }
            },
            new byte[][]
            {
                new byte[] { 0, 0, 0, 1, 1 },
                new byte[] { 0, 0, 0, 0, 1 },
                new byte[] { 0, 0, 0, 0, 1 },
                new byte[] { 0, 0, 0, 0, 1 },
                new byte[] { 0, 0, 0, 0, 1 }
            },
            new byte[][]
            {
                new byte[] { 1, 1, 1, 1, 1 },
                new byte[] { 0, 0, 0, 0, 1 },
                new byte[] { 1, 1, 1, 1, 1 },
                new byte[] { 1, 0, 0, 0, 0 },
                new byte[] { 1, 1, 1, 1, 1 }
            },
            new byte[][]
            {
                new byte[] { 1, 1, 1, 1, 1 },
                new byte[] { 0, 0, 0, 0, 1 },
                new byte[] { 0, 0, 1, 1, 1 },
                new byte[] { 0, 0, 0, 0, 1 },
                new byte[] { 1, 1, 1, 1, 1 }
            },
            new byte[][]
            {
                new byte[] { 1, 0, 0, 0, 1 },
                new byte[] { 1, 0, 0, 0, 1 },
                new byte[] { 1, 1, 1, 1, 1 },
                new byte[] { 0, 0, 0, 0, 1 },
                new byte[] { 0, 0, 0, 0, 1 }
            },
            new byte[][]
            {
                new byte[] { 1, 1, 1, 1, 1 },
                new byte[] { 1, 0, 0, 0, 0 },
                new byte[] { 1, 1, 1, 1, 1 },
                new byte[] { 0, 0, 0, 0, 1 },
                new byte[] { 1, 1, 1, 1, 1 }
            },
            new byte[][]
            {
                new byte[] { 1, 1, 1, 1, 1 },
                new byte[] { 1, 0, 0, 0, 0 },
                new byte[] { 1, 1, 1, 1, 1 },
                new byte[] { 1, 0, 0, 0, 1 },
                new byte[] { 1, 1, 1, 1, 1 }
            },
            new byte[][]
            {
                new byte[] { 1, 1, 1, 1, 1 },
                new byte[] { 0, 0, 0, 0, 1 },
                new byte[] { 0, 0, 0, 0, 1 },
                new byte[] { 0, 0, 0, 0, 1 },
                new byte[] { 0, 0, 0, 0, 1 }
            },
            new byte[][]
            {
                new byte[] { 1, 1, 1, 1, 1 },
                new byte[] { 1, 0, 0, 0, 1 },
                new byte[] { 1, 1, 1, 1, 1 },
                new byte[] { 1, 0, 0, 0, 1 },
                new byte[] { 1, 1, 1, 1, 1 }
            },
            new byte[][]
            {
                new byte[] { 1, 1, 1, 1, 1 },
                new byte[] { 1, 0, 0, 0, 1 },
                new byte[] { 1, 1, 1, 1, 1 },
                new byte[] { 0, 0, 0, 0, 1 },
                new byte[] { 1, 1, 1, 1, 1 }
            }
        };
        public static Dictionary<string, byte[][]> FbxLetterStyleMap = new Dictionary<string, byte[][]>()
        {
            { "A", new byte[][]
                {
                    new byte[] { 1, 1, 1, 1, 1 },
                    new byte[] { 1, 0, 0, 0, 1 },
                    new byte[] { 1, 1, 1, 1, 1 },
                    new byte[] { 1, 0, 0, 0, 1 },
                    new byte[] { 1, 0, 0, 0, 1 }
                }
            },
            { "B", new byte[][]
                {
                    new byte[] { 1, 1, 1, 1, 0 },
                    new byte[] { 1, 0, 0, 0, 1 },
                    new byte[] { 1, 1, 1, 1, 0 },
                    new byte[] { 1, 0, 0, 0, 1 },
                    new byte[] { 1, 1, 1, 1, 0 }
                }
            },
            { "C", new byte[][]
                {
                    new byte[] { 1, 1, 1, 1, 1 },
                    new byte[] { 1, 0, 0, 0, 0 },
                    new byte[] { 1, 0, 0, 0, 0 },
                    new byte[] { 1, 0, 0, 0, 0 },
                    new byte[] { 1, 1, 1, 1, 1 }
                }
            },
            { "D", new byte[][]
                {
                    new byte[] { 1, 1, 1, 1, 0 },
                    new byte[] { 1, 0, 0, 0, 1 },
                    new byte[] { 1, 0, 0, 0, 1 },
                    new byte[] { 1, 0, 0, 0, 1 },
                    new byte[] { 1, 1, 1, 1, 0 }
                }
            },
            { "E", new byte[][]
                {
                    new byte[] { 1, 1, 1, 1, 1 },
                    new byte[] { 1, 0, 0, 0, 0 },
                    new byte[] { 1, 1, 1, 0, 0 },
                    new byte[] { 1, 0, 0, 0, 0 },
                    new byte[] { 1, 1, 1, 1, 1 }
                }
            },
            { "F", new byte[][]
                {
                    new byte[] { 1, 1, 1, 1, 1 },
                    new byte[] { 1, 0, 0, 0, 0 },
                    new byte[] { 1, 1, 1, 0, 0 },
                    new byte[] { 1, 0, 0, 0, 0 },
                    new byte[] { 1, 0, 0, 0, 0 }
                }
            },
            { "G", new byte[][]
                {
                    new byte[] { 1, 1, 1, 1, 1 },
                    new byte[] { 1, 0, 0, 0, 0 },
                    new byte[] { 1, 0, 1, 1, 1 },
                    new byte[] { 1, 0, 0, 0, 1 },
                    new byte[] { 1, 1, 1, 1, 1 }
                }
            },
            { "H", new byte[][]
                {
                    new byte[] { 1, 0, 0, 0, 1 },
                    new byte[] { 1, 0, 0, 0, 1 },
                    new byte[] { 1, 1, 1, 1, 1 },
                    new byte[] { 1, 0, 0, 0, 1 },
                    new byte[] { 1, 0, 0, 0, 1 }
                }
            },
            { "I", new byte[][]
                {
                    new byte[] { 0, 0, 1, 0, 0 },
                    new byte[] { 0, 0, 1, 0, 0 },
                    new byte[] { 0, 0, 1, 0, 0 },
                    new byte[] { 0, 0, 1, 0, 0 },
                    new byte[] { 0, 0, 1, 0, 0 }
                }
            },
            { "J", new byte[][]
                {
                    new byte[] { 0, 0, 0, 0, 1 },
                    new byte[] { 0, 0, 0, 0, 1 },
                    new byte[] { 0, 0, 0, 0, 1 },
                    new byte[] { 1, 0, 0, 0, 1 },
                    new byte[] { 1, 1, 1, 1, 1 }
                }
            },
            { "K", new byte[][]
                {
                    new byte[] { 1, 0, 0, 0, 1 },
                    new byte[] { 1, 0, 0, 1, 0 },
                    new byte[] { 1, 1, 1, 0, 0 },
                    new byte[] { 1, 0, 0, 1, 0 },
                    new byte[] { 1, 0, 0, 0, 1 }
                }
            },
            { "L", new byte[][]
                {
                    new byte[] { 1, 0, 0, 0, 0 },
                    new byte[] { 1, 0, 0, 0, 0 },
                    new byte[] { 1, 0, 0, 0, 0 },
                    new byte[] { 1, 0, 0, 0, 0 },
                    new byte[] { 1, 1, 1, 1, 1 }
                }
            },
            { "M", new byte[][]
                {
                    new byte[] { 1, 1, 1, 1, 1 },
                    new byte[] { 1, 0, 1, 0, 1 },
                    new byte[] { 1, 0, 1, 0, 1 },
                    new byte[] { 1, 0, 0, 0, 1 },
                    new byte[] { 1, 0, 0, 0, 1 }
                }
            },
            { "N", new byte[][]
                {
                    new byte[] { 1, 0, 0, 0, 1 },
                    new byte[] { 1, 1, 0, 0, 1 },
                    new byte[] { 1, 0, 1, 0, 1 },
                    new byte[] { 1, 0, 0, 1, 1 },
                    new byte[] { 1, 0, 0, 0, 1 }
                }
            },
            { "O", new byte[][]
                {
                    new byte[] { 1, 1, 1, 1, 1 },
                    new byte[] { 1, 0, 0, 0, 1 },
                    new byte[] { 1, 0, 0, 0, 1 },
                    new byte[] { 1, 0, 0, 0, 1 },
                    new byte[] { 1, 1, 1, 1, 1 }
                }
            },
            { "P", new byte[][]
                {
                    new byte[] { 1, 1, 1, 1, 1 },
                    new byte[] { 1, 0, 0, 0, 1 },
                    new byte[] { 1, 1, 1, 1, 1 },
                    new byte[] { 1, 0, 0, 0, 0 },
                    new byte[] { 1, 0, 0, 0, 0 }
                }
            },
            { "Q", new byte[][]
                {
                    new byte[] { 1, 1, 1, 1, 1 },
                    new byte[] { 1, 0, 0, 0, 1 },
                    new byte[] { 1, 0, 0, 0, 1 },
                    new byte[] { 1, 0, 0, 1, 1 },
                    new byte[] { 1, 1, 1, 1, 1 }
                }
            },
            { "R", new byte[][]
                {
                    new byte[] { 1, 1, 1, 1, 1 },
                    new byte[] { 1, 0, 0, 0, 1 },
                    new byte[] { 1, 1, 1, 1, 1 },
                    new byte[] { 1, 0, 0, 1, 0 },
                    new byte[] { 1, 0, 0, 0, 1 }
                }
            },
            { "S", new byte[][]
                {
                    new byte[] { 1, 1, 1, 1, 1 },
                    new byte[] { 1, 0, 0, 0, 0 },
                    new byte[] { 1, 1, 1, 1, 1 },
                    new byte[] { 0, 0, 0, 0, 1 },
                    new byte[] { 1, 1, 1, 1, 1 }
                }
            },
            { "T", new byte[][]
                {
                    new byte[] { 1, 1, 1, 1, 1 },
                    new byte[] { 0, 0, 1, 0, 0 },
                    new byte[] { 0, 0, 1, 0, 0 },
                    new byte[] { 0, 0, 1, 0, 0 },
                    new byte[] { 0, 0, 1, 0, 0 }
                }
            },
            { "U", new byte[][]
                {
                    new byte[] { 1, 0, 0, 0, 1 },
                    new byte[] { 1, 0, 0, 0, 1 },
                    new byte[] { 1, 0, 0, 0, 1 },
                    new byte[] { 1, 0, 0, 0, 1 },
                    new byte[] { 1, 1, 1, 1, 1 }
                }
            },
            { "V", new byte[][]
                {
                    new byte[] { 1, 0, 0, 0, 1 },
                    new byte[] { 1, 0, 0, 0, 1 },
                    new byte[] { 1, 0, 0, 0, 1 },
                    new byte[] { 1, 0, 0, 0, 1 },
                    new byte[] { 0, 1, 1, 1, 0 }
                }
            },
            { "W", new byte[][]
                {
                    new byte[] { 1, 0, 0, 0, 1 },
                    new byte[] { 1, 0, 0, 0, 1 },
                    new byte[] { 1, 0, 1, 0, 1 },
                    new byte[] { 1, 0, 1, 0, 1 },
                    new byte[] { 1, 1, 1, 1, 1 }
                }
            },
            { "X", new byte[][]
                {
                    new byte[] { 1, 0, 0, 0, 1 },
                    new byte[] { 0, 1, 0, 1, 0 },
                    new byte[] { 0, 0, 1, 0, 0 },
                    new byte[] { 0, 1, 0, 1, 0 },
                    new byte[] { 1, 0, 0, 0, 1 }
                }
            },
            { "Y", new byte[][]
                {
                    new byte[] { 1, 0, 0, 0, 1 },
                    new byte[] { 1, 0, 0, 0, 1 },
                    new byte[] { 1, 1, 1, 1, 1 },
                    new byte[] { 0, 0, 1, 0, 0 },
                    new byte[] { 0, 0, 1, 0, 0 }
                }
            },
            { "Z", new byte[][]
                {
                    new byte[] { 1, 1, 1, 1, 1 },
                    new byte[] { 0, 0, 0, 1, 0 },
                    new byte[] { 0, 0, 1, 0, 0 },
                    new byte[] { 0, 1, 0, 0, 0 },
                    new byte[] { 1, 1, 1, 1, 1 }
                }
            }
        };

        public static List<BitmapImage> FbxNumberBitmaps = new List<BitmapImage>();
        public static Dictionary<string, BitmapImage> FbxLetterBitmaps = new Dictionary<string, BitmapImage>();

        // Load all Freebox style numbers and letter
        public static async Task LoadFbxStyleBitmaps(int size)
        {
            FbxNumberBitmaps.Clear();
            for (int i = 0; i < 10; i++)
            {
                byte[][] matrix = FbxNumberStyleMap[i];
                int gridSize = matrix.Length;
                int pixelSize = size / gridSize;
                WriteableBitmap bitmap = new WriteableBitmap(size, size, 96, 96, PixelFormats.Bgra32, null);
                int stride = size * 4;
                byte[] pixels = new byte[size * stride];
                for (int y = 0; y < gridSize; y++)
                {
                    for (int x = 0; x < gridSize; x++)
                    {
                        Color color = matrix[y][x] == 1 ? Colors.Yellow : Colors.Black;
                        for (int py = 0; py < pixelSize; py++)
                        {
                            for (int px = 0; px < pixelSize; px++)
                            {
                                int pixelX = x * pixelSize + px;
                                int pixelY = y * pixelSize + py;
                                int index = pixelY * stride + pixelX * 4;
                                pixels[index] = color.B;
                                pixels[index + 1] = color.G;
                                pixels[index + 2] = color.R;
                                pixels[index + 3] = 255;
                            }
                        }
                    }
                }
                bitmap.WritePixels(new Int32Rect(0, 0, size, size), pixels, stride, 0);
                BitmapImage image = new BitmapImage();
                using (var stream = new MemoryStream())
                {
                    PngBitmapEncoder encoder = new PngBitmapEncoder();
                    encoder.Frames.Add(BitmapFrame.Create(bitmap));
                    encoder.Save(stream);
                    image.BeginInit();
                    image.StreamSource = stream;
                    image.CacheOption = BitmapCacheOption.OnLoad;
                    image.EndInit();
                }
                FbxNumberBitmaps.Add(image);
            }

            FbxLetterBitmaps.Clear();
            foreach (var pair in FbxLetterStyleMap)
            {
                byte[][] matrix = pair.Value;
                int gridSize = matrix.Length;
                int pixelSize = size / gridSize;
                WriteableBitmap bitmap = new WriteableBitmap(size, size, 96, 96, PixelFormats.Bgra32, null);
                int stride = size * 4;
                byte[] pixels = new byte[size * stride];
                for (int y = 0; y < gridSize; y++)
                {
                    for (int x = 0; x < gridSize; x++)
                    {
                        Color color = matrix[y][x] == 1 ? Colors.Yellow : Colors.Black;
                        for (int py = 0; py < pixelSize; py++)
                        {
                            for (int px = 0; px < pixelSize; px++)
                            {
                                int pixelX = x * pixelSize + px;
                                int pixelY = y * pixelSize + py;
                                int index = pixelY * stride + pixelX * 4;
                                pixels[index] = color.B;
                                pixels[index + 1] = color.G;
                                pixels[index + 2] = color.R;
                                pixels[index + 3] = 255;
                            }
                        }
                    }
                }
                bitmap.WritePixels(new Int32Rect(0, 0, size, size), pixels, stride, 0);
                BitmapImage image = new BitmapImage();
                using (var stream = new MemoryStream())
                {
                    PngBitmapEncoder encoder = new PngBitmapEncoder();
                    encoder.Frames.Add(BitmapFrame.Create(bitmap));
                    encoder.Save(stream);
                    image.BeginInit();
                    image.StreamSource = stream;
                    image.CacheOption = BitmapCacheOption.OnLoad;
                    image.EndInit();
                }
                FbxLetterBitmaps.Add(pair.Key, image);
            }
        }

        // Return a WritableBitmap of the number in Freebox style
        public static async Task<WriteableBitmap> GetFbxNumberBitmapImage(int number)
        {
            return new WriteableBitmap(FbxNumberBitmaps[number]);
        }

        // Return a WritableBitmap of the letter in Freebox style
        public static async Task<WriteableBitmap> GetFbxLetterBitmapImage(string letter)
        {
            return new WriteableBitmap(FbxLetterBitmaps[letter]);
        }
    }

    public partial class FbxClock : Page
    {
        DispatcherTimer time;

        public FbxClock()
        {
            InitializeComponent();

            Loaded += async (s, e) => await Load();
        }

        private bool Init = false;
        public async Task Load()
        {
            if (!Init)
            {
                FbxStyle.LoadFbxStyleBitmaps(180);

                H1Img.Source = await FbxStyle.GetFbxNumberBitmapImage(1);
                H2Img.Source = await FbxStyle.GetFbxNumberBitmapImage(7);
                M1Img.Source = await FbxStyle.GetFbxNumberBitmapImage(2);
                M2Img.Source = await FbxStyle.GetFbxNumberBitmapImage(0);
                DNameStack.Children.Clear();
                foreach (char c in "MON")
                {
                    string letter = c.ToString().ToUpper();
                    Image img = new Image();
                    img.Source = await FbxStyle.GetFbxLetterBitmapImage(letter);
                    img.Margin = new Thickness(5);
                    img.Height = 30;
                    img.Width = 30;
                    DNameStack.Children.Add(img);
                }
                DDayStack.Children.Clear();
                foreach (char c in "7")
                {
                    string letter = c.ToString();
                    Image img = new Image();
                    img.Source = await FbxStyle.GetFbxNumberBitmapImage(int.Parse(letter));
                    img.Margin = new Thickness(5);
                    img.Height = 30;
                    img.Width = 30;
                    DDayStack.Children.Add(img);
                }
                DMonthStack.Children.Clear();
                foreach (char c in "AUG")
                {
                    string letter = c.ToString().ToUpper();
                    Image img = new Image();
                    img.Source = await FbxStyle.GetFbxLetterBitmapImage(letter);
                    img.Margin = new Thickness(5);
                    img.Height = 30;
                    img.Width = 30;
                    DMonthStack.Children.Add(img);
                }

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

        private async Task UIUpdate()
        {

        }


        /*
         * Time Digit Legend:
         * H1 = [0]0:00:00
         * H2 = 0[0]:00:00
         * M1 = 00:[0]0:00
         * M2 = 00:0[0]:00
         */

        // Current char of the clock
        private string ActualH1 = "1";
        private string ActualH2 = "7";
        private string ActualM1 = "2";
        private string ActualM2 = "0";
        private string ActualDName = "MON";
        private string ActualDDay = "7";
        private string ActualDMonth = "AUG";

        // Animation variables
        // - Slide speed
        private double txtslidespeed = 1;
        // - Delay between slides
        private int txtdelay = 1000;

        // Tick event
        private async void Time_Tick(object sender, EventArgs e)
        {
            // Update the clock
            DateTime now = DateTime.Now;
            UpdateH1(now.Hour.ToString("00")[0].ToString()); // First digit of the hour
            UpdateH2(now.Hour.ToString("00")[1].ToString()); // Second digit of the hour
            UpdateM1(now.Minute.ToString("00")[0].ToString()); // First digit of the minute
            UpdateM2(now.Minute.ToString("00")[1].ToString()); // Second digit of the minute
            UpdateDName(now.DayOfWeek.ToString().Substring(0, 3).ToUpper());   // Day of the week
            UpdateDDay(now.Day.ToString());  // Day of the month
            UpdateMonth(now.ToString("MMM").ToUpper());  // Month

        }

        // Update the clock with animation
        private async Task UpdateH1(string text)
        {
            if (ActualH1 != text)
            {
                H1Img.Source = await FbxStyle.GetFbxNumberBitmapImage(int.Parse(ActualH1));
                ActualH1 = text;
                {
                    var translateAnimation = new DoubleAnimation
                    {
                        From = 0,
                        To = -180,
                        Duration = TimeSpan.FromSeconds(txtslidespeed)
                    };
                    Storyboard.SetTarget(translateAnimation, H1Img);
                    Storyboard.SetTargetProperty(translateAnimation, new PropertyPath("(UIElement.RenderTransform).(TranslateTransform.X)"));
                    var storyboard = new Storyboard();
                    storyboard.Children.Add(translateAnimation);
                    storyboard.Begin();
                }
                await Task.Delay(txtdelay);
                H1Img.Source = await FbxStyle.GetFbxNumberBitmapImage(int.Parse(text));
                {
                    var translateAnimation = new DoubleAnimation
                    {
                        From = -180,
                        To = 0,
                        Duration = TimeSpan.FromSeconds(txtslidespeed)
                    };
                    var resetx = new DoubleAnimation
                    {
                        To = 0,
                        Duration = TimeSpan.FromSeconds(0)
                    };
                    Storyboard.SetTarget(translateAnimation, H1Img);
                    Storyboard.SetTargetProperty(translateAnimation, new PropertyPath("(UIElement.RenderTransform).(TranslateTransform.Y)"));
                    Storyboard.SetTarget(resetx, H1Img);
                    Storyboard.SetTargetProperty(resetx, new PropertyPath("(UIElement.RenderTransform).(TranslateTransform.X)"));
                    var storyboard = new Storyboard();
                    storyboard.Children.Add(translateAnimation);
                    storyboard.Children.Add(resetx);
                    storyboard.Begin();
                }
            }
        }
        private async Task UpdateH2(string text)
        {
            if (ActualH2 != text)
            {
                H2Img.Source = await FbxStyle.GetFbxNumberBitmapImage(int.Parse(ActualH2));
                ActualH2 = text;
                {
                    var translateAnimation = new DoubleAnimation
                    {
                        From = 0,
                        To = 180,
                        Duration = TimeSpan.FromSeconds(txtslidespeed)
                    };
                    Storyboard.SetTarget(translateAnimation, H2Img);
                    Storyboard.SetTargetProperty(translateAnimation, new PropertyPath("(UIElement.RenderTransform).(TranslateTransform.X)"));
                    var storyboard = new Storyboard();
                    storyboard.Children.Add(translateAnimation);
                    storyboard.Begin();
                }
                await Task.Delay(txtdelay);
                H2Img.Source = await FbxStyle.GetFbxNumberBitmapImage(int.Parse(text));
                {
                    var translateAnimation = new DoubleAnimation
                    {
                        From = -180,
                        To = 0,
                        Duration = TimeSpan.FromSeconds(txtslidespeed)
                    };
                    var resetx = new DoubleAnimation
                    {
                        To = 0,
                        Duration = TimeSpan.FromSeconds(0)
                    };
                    Storyboard.SetTarget(translateAnimation, H2Img);
                    Storyboard.SetTargetProperty(translateAnimation, new PropertyPath("(UIElement.RenderTransform).(TranslateTransform.Y)"));
                    Storyboard.SetTarget(resetx, H2Img);
                    Storyboard.SetTargetProperty(resetx, new PropertyPath("(UIElement.RenderTransform).(TranslateTransform.X)"));
                    var storyboard = new Storyboard();
                    storyboard.Children.Add(translateAnimation);
                    storyboard.Children.Add(resetx);
                    storyboard.Begin();
                }
            }
        }
        private async Task UpdateM1(string text)
        {
            if (ActualM1 != text)
            {
                M1Img.Source = await FbxStyle.GetFbxNumberBitmapImage(int.Parse(ActualM1));
                ActualM1 = text;
                {
                    var translateAnimation = new DoubleAnimation
                    {
                        From = 0,
                        To = -180,
                        Duration = TimeSpan.FromSeconds(txtslidespeed)
                    };
                    Storyboard.SetTarget(translateAnimation, M1Img);
                    Storyboard.SetTargetProperty(translateAnimation, new PropertyPath("(UIElement.RenderTransform).(TranslateTransform.X)"));
                    var storyboard = new Storyboard();
                    storyboard.Children.Add(translateAnimation);
                    storyboard.Begin();
                }
                await Task.Delay(txtdelay);
                M1Img.Source = await FbxStyle.GetFbxNumberBitmapImage(int.Parse(text));
                {
                    var translateAnimation = new DoubleAnimation
                    {
                        From = 180,
                        To = 0,
                        Duration = TimeSpan.FromSeconds(txtslidespeed)
                    };
                    var resetx = new DoubleAnimation
                    {
                        To = 0,
                        Duration = TimeSpan.FromSeconds(0)
                    };
                    Storyboard.SetTarget(translateAnimation, M1Img);
                    Storyboard.SetTargetProperty(translateAnimation, new PropertyPath("(UIElement.RenderTransform).(TranslateTransform.Y)"));
                    Storyboard.SetTarget(resetx, M1Img);
                    Storyboard.SetTargetProperty(resetx, new PropertyPath("(UIElement.RenderTransform).(TranslateTransform.X)"));
                    var storyboard = new Storyboard();
                    storyboard.Children.Add(translateAnimation);
                    storyboard.Children.Add(resetx);
                    storyboard.Begin();
                }
            }
        }
        private async Task UpdateM2(string text)
        {
            if (ActualM2 != text)
            {
                M2Img.Source = await FbxStyle.GetFbxNumberBitmapImage(int.Parse(ActualM2));
                ActualM2 = text;
                {
                    var translateAnimation = new DoubleAnimation
                    {
                        From = 0,
                        To = 180,
                        Duration = TimeSpan.FromSeconds(txtslidespeed)
                    };
                    Storyboard.SetTarget(translateAnimation, M2Img);
                    Storyboard.SetTargetProperty(translateAnimation, new PropertyPath("(UIElement.RenderTransform).(TranslateTransform.X)"));
                    var storyboard = new Storyboard();
                    storyboard.Children.Add(translateAnimation);
                    storyboard.Begin();
                }
                await Task.Delay(txtdelay);
                M2Img.Source = await FbxStyle.GetFbxNumberBitmapImage(int.Parse(text));
                {
                    var translateAnimation = new DoubleAnimation
                    {
                        From = 180,
                        To = 0,
                        Duration = TimeSpan.FromSeconds(txtslidespeed)
                    };
                    var resetx = new DoubleAnimation
                    {
                        To = 0,
                        Duration = TimeSpan.FromSeconds(0)
                    };
                    Storyboard.SetTarget(translateAnimation, M2Img);
                    Storyboard.SetTargetProperty(translateAnimation, new PropertyPath("(UIElement.RenderTransform).(TranslateTransform.Y)"));
                    Storyboard.SetTarget(resetx, M2Img);
                    Storyboard.SetTargetProperty(resetx, new PropertyPath("(UIElement.RenderTransform).(TranslateTransform.X)"));
                    var storyboard = new Storyboard();
                    storyboard.Children.Add(translateAnimation);
                    storyboard.Children.Add(resetx);
                    storyboard.Begin();
                }
            }
        }

        private async Task UpdateDName(string text)
        {
            if (ActualDName != text)
            {
                ActualDName = text;
                {
                    var translateAnimation = new DoubleAnimation
                    {
                        From = 0,
                        To = -55*3,
                        Duration = TimeSpan.FromSeconds(txtslidespeed),
                    };
                    Storyboard.SetTarget(translateAnimation, DNameStack);
                    Storyboard.SetTargetProperty(translateAnimation, new PropertyPath("(UIElement.RenderTransform).(TranslateTransform.X)"));
                    var storyboard = new Storyboard();
                    storyboard.Children.Add(translateAnimation);
                    storyboard.Begin();
                }
                await Task.Delay(txtdelay);
                DNameStack.Children.Clear();
                foreach (char c in text)
                {
                    string letter = c.ToString().ToUpper();
                    Image img = new Image();
                    img.Source = await FbxStyle.GetFbxLetterBitmapImage(letter);
                    img.Margin = new Thickness(5);
                    img.Height = 30;
                    img.Width = 30;
                    DNameStack.Children.Add(img);
                }
                {
                    var translateAnimation = new DoubleAnimation
                    {
                        From = 55,
                        To = 0,
                        Duration = TimeSpan.FromSeconds(txtslidespeed),
                    };
                    var resetx = new DoubleAnimation
                    {
                        To = 0,
                        Duration = TimeSpan.FromSeconds(0)
                    };
                    Storyboard.SetTarget(translateAnimation, DNameStack);
                    Storyboard.SetTargetProperty(translateAnimation, new PropertyPath("(UIElement.RenderTransform).(TranslateTransform.Y)"));
                    Storyboard.SetTarget(resetx, DNameStack);
                    Storyboard.SetTargetProperty(resetx, new PropertyPath("(UIElement.RenderTransform).(TranslateTransform.X)"));
                    var storyboard = new Storyboard();
                    storyboard.Children.Add(translateAnimation);
                    storyboard.Children.Add(resetx);
                    storyboard.Begin();
                }
            }
        }

        private async Task UpdateDDay(string text)
        {
            if (ActualDDay != text)
            {
                ActualDDay = text;
                {
                    var translateAnimation = new DoubleAnimation
                    {
                        From = 0,
                        To = -55,
                        Duration = TimeSpan.FromSeconds(txtslidespeed),
                    };
                    Storyboard.SetTarget(translateAnimation, DDayStack);
                    Storyboard.SetTargetProperty(translateAnimation, new PropertyPath("(UIElement.RenderTransform).(TranslateTransform.Y)"));
                    var storyboard = new Storyboard();
                    storyboard.Children.Add(translateAnimation);
                    storyboard.Begin();
                }
                await Task.Delay(txtdelay);
                DDayStack.Children.Clear();
                foreach (char c in text)
                {
                    string letter = c.ToString();
                    Image img = new Image();
                    img.Source = await FbxStyle.GetFbxNumberBitmapImage(int.Parse(letter));
                    img.Margin = new Thickness(5);
                    img.Height = 30;
                    img.Width = 30;
                    DDayStack.Children.Add(img);
                }
                {
                    var translateAnimation = new DoubleAnimation
                    {
                        From = 55,
                        To = 0,
                        Duration = TimeSpan.FromSeconds(txtslidespeed),
                    };
                    Storyboard.SetTarget(translateAnimation, DDayStack);
                    Storyboard.SetTargetProperty(translateAnimation, new PropertyPath("(UIElement.RenderTransform).(TranslateTransform.Y)"));
                    var storyboard = new Storyboard();
                    storyboard.Children.Add(translateAnimation);
                    storyboard.Begin();
                }
            }
        }

        private async Task UpdateMonth(string text)
        {
            if (ActualDMonth != text)
            {
                ActualDMonth = text;
                {
                    var translateAnimation = new DoubleAnimation
                    {
                        From = 0,
                        To = 55*3,
                        Duration = TimeSpan.FromSeconds(txtslidespeed),
                    };
                    Storyboard.SetTarget(translateAnimation, DMonthStack);
                    Storyboard.SetTargetProperty(translateAnimation, new PropertyPath("(UIElement.RenderTransform).(TranslateTransform.X)"));
                    var storyboard = new Storyboard();
                    storyboard.Children.Add(translateAnimation);
                    storyboard.Begin();
                }
                await Task.Delay(txtdelay);
                DMonthStack.Children.Clear();
                foreach (char c in text)
                {
                    string letter = c.ToString().ToUpper();
                    Image img = new Image();
                    img.Source = await FbxStyle.GetFbxLetterBitmapImage(letter);
                    img.Margin = new Thickness(5);
                    img.Height = 30;
                    img.Width = 30;
                    DMonthStack.Children.Add(img);
                }
                {
                    var translateAnimation = new DoubleAnimation
                    {
                        From = 55,
                        To = 0,
                        Duration = TimeSpan.FromSeconds(txtslidespeed),
                    };
                    var resetx = new DoubleAnimation
                    {
                        To = 0,
                        Duration = TimeSpan.FromSeconds(0)
                    };
                    Storyboard.SetTarget(translateAnimation, DMonthStack);
                    Storyboard.SetTargetProperty(translateAnimation, new PropertyPath("(UIElement.RenderTransform).(TranslateTransform.Y)"));
                    Storyboard.SetTarget(resetx, DMonthStack);
                    Storyboard.SetTargetProperty(resetx, new PropertyPath("(UIElement.RenderTransform).(TranslateTransform.X)"));
                    var storyboard = new Storyboard();
                    storyboard.Children.Add(translateAnimation);
                    storyboard.Children.Add(resetx);
                    storyboard.Begin();
                }

            }
        }
    }
}
