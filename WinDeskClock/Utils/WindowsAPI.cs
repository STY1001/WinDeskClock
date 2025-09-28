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
        private static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);

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
        private const byte VK_ESC = 0x1B;
        private const uint KEYEVENTF_KEYUP = 0x0002;
        public static class User32
        {
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
            public static byte VK_ESC { get { return WindowsAPI.VK_ESC; } }
            public static uint KEYEVENTF_KEYUP { get { return WindowsAPI.KEYEVENTF_KEYUP; } }


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
            public static void KeybdEvent(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo)
            {
                WindowsAPI.keybd_event(bVk, bScan, dwFlags, dwExtraInfo);
            }
        }
    }
}
