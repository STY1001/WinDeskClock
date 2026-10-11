using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using WinDeskClock.ViewModels;
using WinDeskClock.Views.Animation;

namespace WinDeskClock.Clocks
{
    /// <summary>
    /// Interaction logic for FbxClock.xaml (data below is unchanged: a pure pixel-map lookup table
    /// with no UI/business logic, so it doesn't need any MVVM treatment).
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

        private static bool _loaded;

        /// <summary>Generates every number/letter bitmap once. Must run before any Get*BitmapImage
        /// call - <see cref="FbxNumberBitmaps"/>/<see cref="FbxLetterBitmaps"/> stay empty otherwise,
        /// which is what made every digit/letter image fail to render (nothing visible but the
        /// page's plain black background). Safe to call more than once (no-op after the first).
        /// Synchronous: nothing below actually awaits I/O, it's pure in-memory bitmap generation, so
        /// there's no reason to force every caller through async/await for it.</summary>
        public static void EnsureLoaded(int size)
        {
            if (_loaded)
            {
                return;
            }
            _loaded = true;
            LoadFbxStyleBitmaps(size);
        }

        // Load all Freebox style numbers and letter
        private static void LoadFbxStyleBitmaps(int size)
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
        public static WriteableBitmap GetFbxNumberBitmapImage(int number)
        {
            return new WriteableBitmap(FbxNumberBitmaps[number]);
        }

        // Return a WritableBitmap of the letter in Freebox style
        public static WriteableBitmap GetFbxLetterBitmapImage(string letter)
        {
            return new WriteableBitmap(FbxLetterBitmaps[letter]);
        }
    }


    /// <summary>
    /// The "Freebox"-style LED clock face: renders each digit/letter as a small bitmap from
    /// <see cref="FbxStyle"/> instead of text. Shares <see cref="ClockViewModel"/> with FluentClock;
    /// this class only decides how to animate/rebuild its images when the ViewModel changes.
    /// </summary>
    public partial class FbxClock : Page
    {
        // The original's txtslidespeed (1 second per phase) / txtdelay (1000ms, i.e. the swap
        // happens exactly when the out-phase finishes, with no extra gap).
        private const double SlideSeconds = 1.0;
        private const int SlideDelayMs = 1000;

        public FbxClock(ClockViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;

            // Must happen before anything below touches a Get*BitmapImage call, directly or via a
            // PropertyChanged-triggered slide.
            FbxStyle.EnsureLoaded(180);

            // Render the current value immediately instead of waiting for the ViewModel's first
            // per-second Tick (up to ~1s away) or, worse, for some *later* tick where the digit
            // finally happens to differ from ClockViewModel's own initial value - every image would
            // stay blank until then otherwise, since nothing else sets an initial Source.
            H1Img.Source = FbxStyle.GetFbxNumberBitmapImage(int.Parse(viewModel.H1));
            H2Img.Source = FbxStyle.GetFbxNumberBitmapImage(int.Parse(viewModel.H2));
            M1Img.Source = FbxStyle.GetFbxNumberBitmapImage(int.Parse(viewModel.M1));
            M2Img.Source = FbxStyle.GetFbxNumberBitmapImage(int.Parse(viewModel.M2));
            SetWord(DDayStack, viewModel.DDay, useLetters: false);
            SetWord(DNameStack, viewModel.DName, useLetters: true);
            SetWord(DMonthStack, viewModel.DMonth, useLetters: true);

            viewModel.PropertyChanged += (_, e) =>
            {
                switch (e.PropertyName)
                {
                    // Out axis/distance, then in axis/distance - deliberately mismatched (sideways
                    // out, vertical in) for every field except the day-of-month; see SlideReplace.
                    // Hour digits drop down from above on the way in (Y: -180 -> 0), minute digits
                    // rise up from below (Y: 180 -> 0) - the original's actual, intentional look.
                    case nameof(ClockViewModel.H1):
                        SlideReplace(H1Img, SlideAxis.X, -180, SlideAxis.Y, -180, () => H1Img.Source = FbxStyle.GetFbxNumberBitmapImage(int.Parse(viewModel.H1)));
                        break;
                    case nameof(ClockViewModel.H2):
                        SlideReplace(H2Img, SlideAxis.X, 180, SlideAxis.Y, -180, () => H2Img.Source = FbxStyle.GetFbxNumberBitmapImage(int.Parse(viewModel.H2)));
                        break;
                    case nameof(ClockViewModel.M1):
                        SlideReplace(M1Img, SlideAxis.X, -180, SlideAxis.Y, 180, () => M1Img.Source = FbxStyle.GetFbxNumberBitmapImage(int.Parse(viewModel.M1)));
                        break;
                    case nameof(ClockViewModel.M2):
                        SlideReplace(M2Img, SlideAxis.X, 180, SlideAxis.Y, 180, () => M2Img.Source = FbxStyle.GetFbxNumberBitmapImage(int.Parse(viewModel.M2)));
                        break;
                    case nameof(ClockViewModel.DDay):
                        SlideReplace(DDayStack, SlideAxis.Y, -55, SlideAxis.Y, 55, () => SetWord(DDayStack, viewModel.DDay, useLetters: false));
                        break;
                    case nameof(ClockViewModel.DName):
                        SlideReplace(DNameStack, SlideAxis.X, -165, SlideAxis.Y, 55, () => SetWord(DNameStack, viewModel.DName, useLetters: true));
                        break;
                    case nameof(ClockViewModel.DMonth):
                        SlideReplace(DMonthStack, SlideAxis.X, 165, SlideAxis.Y, 55, () => SetWord(DMonthStack, viewModel.DMonth, useLetters: true));
                        break;
                }
            };

            viewModel.Start();
        }

        private static void SetWord(StackPanel target, string word, bool useLetters)
        {
            target.Children.Clear();
            foreach (char c in word)
            {
                target.Children.Add(new Image
                {
                    Source = useLetters ? FbxStyle.GetFbxLetterBitmapImage(c.ToString()) : FbxStyle.GetFbxNumberBitmapImage(int.Parse(c.ToString())),
                    Margin = new Thickness(5),
                    Height = 30,
                    Width = 30
                });
            }
        }

        /// <summary>
        /// Faithful port of the original's per-field slide (UpdateH1/H2/M1/M2/UpdateDName/UpdateDDay/
        /// UpdateDMonth): translate OUT along outAxis to outTo (1s), swap the content, then translate
        /// IN along inAxis from inFrom back to 0 (1s). The original isn't a simple back-and-forth
        /// slide along one axis: every field but the day-of-month goes out sideways (X) and comes
        /// back in vertically (Y), so when the in-phase uses a different axis than the out-phase, that
        /// out-axis is snapped back to 0 instantly the moment the in-phase starts (the original's
        /// zero-duration "resetx" animation) - otherwise the image would stay sideways-displaced while
        /// sliding in vertically.
        /// </summary>
        private static async void SlideReplace(FrameworkElement target, SlideAxis outAxis, double outTo, SlideAxis inAxis, double inFrom, Action applyChange)
        {
            var transform = (TranslateTransform)target.RenderTransform;

            Animate(transform, outAxis, null, outTo, SlideSeconds);
            await Task.Delay(SlideDelayMs);

            applyChange();

            Animate(transform, inAxis, inFrom, 0, SlideSeconds);
            if (inAxis != outAxis)
            {
                Animate(transform, outAxis, outTo, 0, 0);
            }
        }

        private static void Animate(TranslateTransform transform, SlideAxis axis, double? from, double to, double seconds)
        {
            var animation = new DoubleAnimation { To = to, Duration = TimeSpan.FromSeconds(seconds) };
            if (from.HasValue)
            {
                animation.From = from.Value;
            }
            transform.BeginAnimation(axis == SlideAxis.X ? TranslateTransform.XProperty : TranslateTransform.YProperty, animation);
        }
    }
}
