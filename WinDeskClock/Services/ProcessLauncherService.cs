using System.Diagnostics;

namespace WinDeskClock.Services
{
    /// <summary>
    /// Opens a URL in the user's default browser. Wraps Process.Start so ViewModels (e.g. the
    /// plugin manager's "Website" / "Source code" buttons) don't depend on System.Diagnostics directly.
    /// </summary>
    public interface IProcessLauncherService
    {
        void OpenUrl(string url);
    }

    public class ProcessLauncherService : IProcessLauncherService
    {
        public void OpenUrl(string url)
        {
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
        }
    }
}
