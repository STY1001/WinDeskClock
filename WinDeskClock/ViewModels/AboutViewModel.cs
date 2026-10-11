using WinDeskClock.Mvvm;

namespace WinDeskClock.ViewModels
{
    public class AboutViewModel : ObservableObject
    {
        public string VersionText => "Version " + App.AppVersion;
    }
}
