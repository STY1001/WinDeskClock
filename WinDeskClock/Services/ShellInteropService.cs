using WinDeskClock.Utils;

namespace WinDeskClock.Services
{
    /// <summary>
    /// Injectable facade over the low-level Win32 calls in WinDeskClock.Utils.WindowsAPI:
    /// turning the monitor on/off, showing/hiding desktop icons (used when entering/leaving
    /// kiosk mode) and nudging the mouse cursor (used to wake the display).
    /// These are pure OS interop calls with no UI-tree/animation logic, so they're a natural
    /// service boundary: ViewModels (and the View) can call them without depending on
    /// WinDeskClock.Utils.WindowsAPI directly.
    /// </summary>
    public interface IShellInteropService
    {
        void RequestMonitorPower(bool turnOn);
        void NudgeMouseCursor();
        void ShowDesktopIcons();
        void HideDesktopIcons();
        void ShowTaskbar();
        void HideTaskbar();
        IntPtr RegisterPowerNotifications(IntPtr windowHandle);
        void UnregisterPowerNotifications(IntPtr notificationHandle);
    }

    public class ShellInteropService : IShellInteropService
    {
        public void RequestMonitorPower(bool turnOn)
        {
            // PostMessage, not SendMessage: SendMessage blocks this thread until every top-level
            // window (HWND_BROADCAST) has handled WM_SYSCOMMAND, and on some Windows builds that
            // wait doesn't return promptly - which is what was freezing the whole app on screen
            // off/on. Nothing here ever used a return value, so there's no downside to not waiting.
            WindowsAPI.User32.PostMessage(
                0xFFFF, // HWND_BROADCAST
                (int)WindowsAPI.User32.WM_SYSCOMMAND,
                (int)WindowsAPI.User32.SC_MONITORPOWER,
                turnOn ? WindowsAPI.User32.MONITOR_ON : WindowsAPI.User32.MONITOR_OFF);
        }

        public void NudgeMouseCursor()
        {
            WindowsAPI.User32.MouseEvent(WindowsAPI.User32.MOUSE_EVENTF_MOVE, 1, 1, 0, IntPtr.Zero);
            WindowsAPI.User32.MouseEvent(WindowsAPI.User32.MOUSE_EVENTF_MOVE, -1, -1, 0, IntPtr.Zero);
        }

        public IntPtr RegisterPowerNotifications(IntPtr windowHandle)
        {
            Guid powerSettingGuid = WindowsAPI.User32.CONSOLE_DISPLAY_STATE;
            return WindowsAPI.User32.RegisterPowerSettingNotification(windowHandle, ref powerSettingGuid, 0);
        }

        public void UnregisterPowerNotifications(IntPtr notificationHandle)
        {
            if (notificationHandle != IntPtr.Zero)
            {
                WindowsAPI.User32.UnregisterPowerSettingNotification(notificationHandle);
            }
        }

        public void ShowDesktopIcons() => SetDesktopIconsVisible(true);
        public void HideDesktopIcons() => SetDesktopIconsVisible(false);

        public void ShowTaskbar() => SetTaskbarVisible(true);
        public void HideTaskbar() => SetTaskbarVisible(false);

        private static void SetTaskbarVisible(bool visible)
        {
            IntPtr taskbarWnd = WindowsAPI.User32.FindWindow("Shell_TrayWnd", null);
            if (taskbarWnd == IntPtr.Zero)
            {
                _ = Log.Warning("Taskbar not found, cannot change its visibility");
                return;
            }

            _ = Log.Info(visible ? "Showing taskbar" : "Hiding taskbar");
            WindowsAPI.User32.ShowWindow(taskbarWnd, visible ? WindowsAPI.User32.SW_SHOW : WindowsAPI.User32.SW_HIDE);
        }

        private static void SetDesktopIconsVisible(bool visible)
        {
            IntPtr desktopWnd = WindowsAPI.User32.FindWindow("Progman", null);
            if (desktopWnd == IntPtr.Zero)
            {
                _ = Log.Warning("Desktop not found, cannot change desktop icons visibility");
                return;
            }

            IntPtr shellViewWnd = WindowsAPI.User32.FindWindowEx(desktopWnd, IntPtr.Zero, "SHELLDLL_DefView", null);
            if (shellViewWnd == IntPtr.Zero)
            {
                _ = Log.Warning("Shell view not found, cannot change desktop icons visibility");
                return;
            }

            IntPtr sysListView32Wnd = WindowsAPI.User32.FindWindowEx(shellViewWnd, IntPtr.Zero, "SysListView32", null);
            if (sysListView32Wnd == IntPtr.Zero)
            {
                _ = Log.Warning("SysListView32 not found, cannot change desktop icons visibility");
                return;
            }

            WindowsAPI.User32.ShowWindow(sysListView32Wnd, visible ? WindowsAPI.User32.SW_SHOW : WindowsAPI.User32.SW_HIDE);
        }
    }
}
