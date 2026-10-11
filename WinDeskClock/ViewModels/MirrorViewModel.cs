using System.Collections.ObjectModel;
using System.Windows.Media.Imaging;
using WinDeskClock.Mvvm;
using WinDeskClock.Services;
using WinDeskClock.Utils;

namespace WinDeskClock.ViewModels
{
    public class MirrorViewModel : ObservableObject
    {
        private readonly ICameraService _cameraService;

        public MirrorViewModel(ICameraService cameraService)
        {
            _cameraService = cameraService;
            _cameraService.FrameReady += (_, frame) => CurrentFrame = frame;
        }

        public ObservableCollection<CameraDescription> Cameras { get; } = new();

        private CameraDescription? _selectedCamera;
        public CameraDescription? SelectedCamera
        {
            get => _selectedCamera;
            set
            {
                if (SetProperty(ref _selectedCamera, value) && value != null)
                {
                    _ = RestartCaptureAsync(value.Index);
                }
            }
        }

        private bool _isMirrored = true;
        public bool IsMirrored
        {
            get => _isMirrored;
            set
            {
                if (SetProperty(ref _isMirrored, value))
                {
                    _cameraService.FlipHorizontally = value;
                }
            }
        }

        private bool _hasNoCamera;
        public bool HasNoCamera { get => _hasNoCamera; private set => SetProperty(ref _hasNoCamera, value); }

        private BitmapSource? _currentFrame;
        public BitmapSource? CurrentFrame { get => _currentFrame; private set => SetProperty(ref _currentFrame, value); }

        public void LoadCameras()
        {
            Cameras.Clear();
            foreach (var camera in _cameraService.ListCameras())
            {
                Cameras.Add(camera);
            }

            HasNoCamera = Cameras.Count == 0;
            if (Cameras.Count > 0)
            {
                _cameraService.FlipHorizontally = IsMirrored;
                SelectedCamera = Cameras[0];
            }
        }

        private async Task RestartCaptureAsync(int cameraIndex)
        {
            try
            {
                await _cameraService.StartAsync(cameraIndex);
            }
            catch (Exception ex)
            {
                _ = Log.Warning($"Failed to start camera {cameraIndex}: {ex.Message}");
                HasNoCamera = true;
            }
        }

        public async Task StopAsync() => await _cameraService.StopAsync();
    }
}
