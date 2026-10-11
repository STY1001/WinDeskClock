using System.Windows;
using System.Windows.Controls;
using WinDeskClock.Services;
using WinDeskClock.ViewModels;

namespace WinDeskClock.Pages
{
    /// <summary>
    /// The webcam mirror page. All camera handling now lives in ICameraService/MirrorViewModel;
    /// this class only wires up the controls and starts/stops loading when the page is shown/hidden.
    /// </summary>
    public partial class Mirror : Page
    {
        private readonly MirrorViewModel _viewModel;

        public Mirror(MirrorViewModel viewModel, ILocalizationService localization)
        {
            InitializeComponent();
            _viewModel = viewModel;
            DataContext = viewModel;

            CameraLab.Content = localization.GetString("mirror.camera");
            MirrorCheckBox.Content = localization.GetString("mirror.mirrormode");

            viewModel.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName == nameof(MirrorViewModel.HasNoCamera) && viewModel.HasNoCamera)
                {
                    CameraComboBox.Text = localization.GetString("mirror.nocamera");
                }
            };

            Loaded += (_, _) => viewModel.LoadCameras();
            Unloaded += async (_, _) => await viewModel.StopAsync();
        }

        private void CameraComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (CameraComboBox.SelectedItem is Services.CameraDescription camera)
            {
                _viewModel.SelectedCamera = camera;
            }
        }
    }
}
