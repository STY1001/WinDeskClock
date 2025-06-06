using NAudio.CoreAudioApi;
using OpenCvSharp;
using OpenCvSharp.WpfExtensions;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using DirectShowLib;

namespace WinDeskClock.Pages
{
    public partial class Mirror : Page
    {
        private List<int> cameraIndices = new();
        private VideoCapture? capture;
        private CancellationTokenSource? cts;

        public Mirror()
        {
            InitializeComponent();
            LoadCameras();
            this.Unloaded += Mirror_Unloaded;
        }

        private async void Mirror_Unloaded(object sender, RoutedEventArgs e)
        {
            StopCamera();
        }

        private void LoadCameras()
        {
            CameraComboBox.Items.Clear();
            cameraIndices.Clear();

            var systemCameras = DsDevice.GetDevicesOfCat(FilterCategory.VideoInputDevice);

            for (int i = 0; i < systemCameras.Length; i++)
            {
                var cam = systemCameras[i];
                cameraIndices.Add(i);
                CameraComboBox.Items.Add($"{i} - {cam.Name}");
            }

            if (CameraComboBox.Items.Count > 0)
                CameraComboBox.SelectedIndex = 0;
            else
                CameraComboBox.Text = "No camera found";
        }

        private async void CameraComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (CameraComboBox.SelectedIndex < 0)
                return;

            CameraComboBox.IsEnabled = false;
            LoadingIndicator.Visibility = Visibility.Visible;

            try
            {
                await StartCamera(cameraIndices[CameraComboBox.SelectedIndex]);
            }
            catch (Exception ex)
            {
                throw new Exception("Error starting camera: " + ex.Message, ex);
            }
            finally
            {
                CameraComboBox.IsEnabled = true;
                LoadingIndicator.Visibility = Visibility.Collapsed;
            }
        }


        private async Task StartCamera(int cameraIndex)
        {
            await StopCamera();

            await Task.Delay(1000);

            cts = new CancellationTokenSource();

            Task.Run(async () =>
            {
                capture = new VideoCapture(cameraIndex);
                if (!capture.IsOpened())
                {
                    Dispatcher.Invoke(() => throw new Exception("Failed to open camera. Please check if the camera is connected and accessible."));
                    return;
                }

                Task.Run(async () => CaptureLoop(cts.Token));
            });
        }

        private async Task StopCamera()
        {
            if (cts != null)
            {
                cts.Cancel();
                await Task.Delay(100);
                cts.Dispose();
                cts = null;
            }

            capture?.Release();
            capture?.Dispose();
            capture = null;
        }

        private void CaptureLoop(CancellationToken token)
        {
            using var frame = new Mat();

            while (!token.IsCancellationRequested)
            {
                if (capture == null || !capture.Read(frame) || frame.Empty())
                    continue;

                var image = frame.Clone();

                if (Dispatcher.Invoke(() => MirrorCheckBox.IsChecked == true))
                {
                    Cv2.Flip(image, image, FlipMode.Y);
                }

                var bitmap = image.ToBitmapSource();
                bitmap.Freeze();

                Dispatcher.Invoke(() => CameraImage.Source = bitmap);
            }
        }
    }
}
