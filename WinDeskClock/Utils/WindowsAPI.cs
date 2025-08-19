using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace WinDeskClock.Utils
{
    public static class WindowsAPI
    {
        public delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern IntPtr FindWindow(string lpClassName, string? lpWindowName);
        [DllImport("user32.dll")]
        private static extern IntPtr FindWindowEx(IntPtr parentHandle, IntPtr childAfter, string? className, string? windowTitle);
        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
        [DllImport("user32.dll")]
        private static extern int SendMessage(int hWnd, int hMsg, int wParam, int lParam);
        [DllImport("user32.dll")]
        private static extern IntPtr RegisterPowerSettingNotification(IntPtr hRecipient, ref Guid PowerSettingGuid, uint Flags);
        [DllImport("user32.dll")]
        private static extern bool UnregisterPowerSettingNotification(IntPtr Handle);
        [DllImport("user32.dll")]
        private static extern void mouse_event(uint dwFlags, int dx, int dy, uint dwData, IntPtr dwExtraInfo);
        [DllImport("user32.dll")]
        private static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);
        [DllImport("user32.dll")]
        private static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);
        [DllImport("user32.dll")]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out int lpdwProcessId);
        [DllImport("user32.dll")]
        private static extern bool IsWindowVisible(IntPtr hWnd);
        [DllImport("user32.dll")]
        private static extern uint GetWindowLong(IntPtr hWnd, int nIndex);
        [DllImport("user32.dll")]
        private static extern uint SetWindowLong(IntPtr hWnd, int nIndex, uint dwNewLong);

        private const int HWND_BROADCAST = 0xFFFF;
        private const uint WM_SYSCOMMAND = 0x0112;
        private const uint SC_MONITORPOWER = 0xF170;
        private const int MONITOR_OFF = 2;
        private const int MONITOR_ON = -1;
        private const int WM_POWERBROADCAST = 0x0218;
        private const int PBT_POWERSETTINGCHANGE = 0x8013;
        private const uint MOUSE_EVENTF_MOVE = 0x0001;
        private static Guid CONSOLE_DISPLAY_STATE = new Guid("6FE69556-704A-47A0-8F24-C28D936FDA47");
        private const int SW_HIDE = 0;
        private const int SW_SHOW = 5;
        private const int GWL_STYLE = -16;
        private const uint WS_VISIBLE = 0x10000000;
        public static class User32
        {
            

            public static int HWND_BROADCAST { get { return WindowsAPI.HWND_BROADCAST; } }
            public static uint WM_SYSCOMMAND { get { return WindowsAPI.WM_SYSCOMMAND; } }
            public static uint SC_MONITORPOWER { get { return WindowsAPI.SC_MONITORPOWER; } }
            public static int MONITOR_OFF { get { return WindowsAPI.MONITOR_OFF; } }
            public static int MONITOR_ON { get { return WindowsAPI.MONITOR_ON; } }
            public static int WM_POWERBROADCAST { get { return WindowsAPI.WM_POWERBROADCAST; } }
            public static int PBT_POWERSETTINGCHANGE { get { return WindowsAPI.PBT_POWERSETTINGCHANGE; } }
            public static uint MOUSE_EVENTF_MOVE { get { return WindowsAPI.MOUSE_EVENTF_MOVE; } }
            public static Guid CONSOLE_DISPLAY_STATE { get { return WindowsAPI.CONSOLE_DISPLAY_STATE; } }
            public static int SW_HIDE { get { return WindowsAPI.SW_HIDE; } }
            public static int SW_SHOW { get { return WindowsAPI.SW_SHOW; } }
            public static int GWL_STYLE { get { return WindowsAPI.GWL_STYLE; } }
            public static uint WS_VISIBLE { get { return WindowsAPI.WS_VISIBLE; } }

            [StructLayout(LayoutKind.Sequential, Pack = 4)]
            public struct PowerBroadcastSetting
            {
                public Guid PowerSetting;
                public uint DataLength;
                public byte Data;
            }
            
            public static IntPtr FindWindow(string lpClassName, string? lpWindowName)
            {
                return WindowsAPI.FindWindow(lpClassName, lpWindowName);
            }
            public static IntPtr FindWindowEx(IntPtr parentHandle, IntPtr childAfter, string? className, string? windowTitle)
            {
                return WindowsAPI.FindWindowEx(parentHandle, childAfter, className, windowTitle);
            }
            public static bool ShowWindow(IntPtr hWnd, int nCmdShow)
            {
                return WindowsAPI.ShowWindow(hWnd, nCmdShow);
            }
            public static int SendMessage(int hWnd, int hMsg, int wParam, int lParam)
            {
                return WindowsAPI.SendMessage(hWnd, hMsg, wParam, lParam);
            }
            public static IntPtr RegisterPowerSettingNotification(IntPtr hRecipient, ref Guid PowerSettingGuid, uint Flags)
            {
                return WindowsAPI.RegisterPowerSettingNotification(hRecipient, ref PowerSettingGuid, Flags);
            }
            public static bool UnregisterPowerSettingNotification(IntPtr Handle)
            {
                return WindowsAPI.UnregisterPowerSettingNotification(Handle);
            }
            public static void MouseEvent(uint dwFlags, int dx, int dy, uint dwData, IntPtr dwExtraInfo)
            {
                WindowsAPI.mouse_event(dwFlags, dx, dy, dwData, dwExtraInfo);
            }
            public static bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam)
            {
                return WindowsAPI.EnumWindows(lpEnumFunc, lParam);
            }
            public static int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount)
            {
                return WindowsAPI.GetClassName(hWnd, lpClassName, nMaxCount);
            }
            public static uint GetWindowThreadProcessId(IntPtr hWnd, out int lpdwProcessId)
            {
                return WindowsAPI.GetWindowThreadProcessId(hWnd, out lpdwProcessId);
            }
            public static bool IsWindowVisible(IntPtr hWnd)
            {
                return WindowsAPI.IsWindowVisible(hWnd);
            }
            public static uint GetWindowLong(IntPtr hWnd, int nIndex)
            {
                return WindowsAPI.GetWindowLong(hWnd, nIndex);
            }
            public static uint SetWindowLong(IntPtr hWnd, int nIndex, uint dwNewLong)
            {
                return WindowsAPI.SetWindowLong(hWnd, nIndex, dwNewLong);
            }
        }
    }
}
