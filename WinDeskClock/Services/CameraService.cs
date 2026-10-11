using System.Threading;
using System.Windows;
using System.Windows.Media.Imaging;
using DirectShowLib;
using OpenCvSharp;
using OpenCvSharp.WpfExtensions;

namespace WinDeskClock.Services
{
    public record CameraDescription(int Index, string Name);

    /// <summary>
    /// Enumerates webcams and streams frames from one of them. Wraps DirectShowLib (device list)
    /// and OpenCvSharp (capture loop), which the Mirror page's code-behind used to do directly.
    /// Frames are delivered through <see cref="FrameReady"/>, already marshalled to the UI thread,
    /// so a ViewModel can simply assign the BitmapSource to a bound property.
    /// </summary>
    public interface ICameraService
    {
        IReadOnlyList<CameraDescription> ListCameras();

        /// <summary>Whether frames are flipped horizontally (mirror effect) before being published.</summary>
        bool FlipHorizontally { get; set; }

        Task StartAsync(int cameraIndex);
        Task StopAsync();

        event EventHandler<BitmapSource>? FrameReady;
    }

    public class CameraService : ICameraService
    {
        private VideoCapture? _capture;
        private CancellationTokenSource? _cts;
        private Task? _captureTask;

        public bool FlipHorizontally { get; set; }

        public event EventHandler<BitmapSource>? FrameReady;

        public IReadOnlyList<CameraDescription> ListCameras()
        {
            var systemCameras = DsDevice.GetDevicesOfCat(FilterCategory.VideoInputDevice);

            var result = new List<CameraDescription>();

            for (int i = 0; i < systemCameras.Length; i++)
            {
                result.Add(new CameraDescription(i, systemCameras[i].Name));
            }

            return result;
        }

        public async Task StartAsync(int cameraIndex)
        {
            await StopAsync();

            var cts = new CancellationTokenSource();
            var capture = new VideoCapture(cameraIndex);

            if (!capture.IsOpened())
            {
                capture.Dispose();
                cts.Dispose();

                throw new Exception("Failed to open camera. Please check if the camera is connected and accessible.");
            }

            _cts = cts;
            _capture = capture;

            _captureTask = Task.Run(() => CaptureLoop(capture, cts.Token), cts.Token);
        }

        public async Task StopAsync()
        {
            var cts = _cts;
            var captureTask = _captureTask;
            var capture = _capture;

            if (cts == null)
                return;

            cts.Cancel();

            try
            {
                if (captureTask != null)
                    await captureTask;
            }
            catch (OperationCanceledException)
            {

            }
            finally
            {
                capture?.Release();
                capture?.Dispose();

                cts.Dispose();

                if (ReferenceEquals(_cts, cts))
                    _cts = null;

                if (ReferenceEquals(_capture, capture))
                    _capture = null;

                if (ReferenceEquals(_captureTask, captureTask))
                    _captureTask = null;
            }
        }

        private void CaptureLoop(VideoCapture capture, CancellationToken token)
        {
            using var frame = new Mat();

            while (!token.IsCancellationRequested)
            {
                if (!capture.Read(frame))
                    break;

                if (frame.Empty())
                    continue;

                using var image = frame.Clone();

                if (FlipHorizontally)
                {
                    Cv2.Flip(image, image, FlipMode.Y);
                }

                var bitmap = image.ToBitmapSource();
                bitmap.Freeze();

                Application.Current?.Dispatcher.Invoke(() => FrameReady?.Invoke(this, bitmap));
            }
        }
    }
}
