using Microsoft.Win32;

namespace WinDeskClock.Services
{
    /// <summary>
    /// Wraps the WPF/Win32 file picker so ViewModels can ask for a file without depending on
    /// Microsoft.Win32.OpenFileDialog directly (which makes them easier to unit test later).
    /// </summary>
    public interface IFileDialogService
    {
        /// <summary>Shows a "pick a .wav file" dialog. Returns null if the user cancelled.</summary>
        string? PickWavFile(string title);
    }

    public class FileDialogService : IFileDialogService
    {
        public string? PickWavFile(string title)
        {
            var dialog = new OpenFileDialog
            {
                DefaultExt = ".wav",
                Filter = "WAV Audio File (*.wav)|*.wav",
                FilterIndex = 1,
                Title = title
            };

            return dialog.ShowDialog() == true ? dialog.FileName : null;
        }
    }
}
