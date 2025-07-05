using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace WinDeskClock.Utils
{
    public static class Log
    {
        [DllImport("kernel32.dll")]
        public static extern IntPtr GetConsoleWindow();

        public static string LogFilePath = "log.txt";
        public static string GetTimeStamp()
        {
            return DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        }

        public static async Task Error(string message)
        {
            string errorMessage = $"[{GetTimeStamp()}][E][WinDeskClock]: {message}";
            if (GetConsoleWindow() != IntPtr.Zero)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(errorMessage);
                Console.ResetColor();
            }
            if (Debugger.IsAttached)
            {
                Debug.WriteLine(errorMessage);
            }
            await Write2File(errorMessage.Replace("[WinDeskClock]", ""));
        }
        public static async Task Warning(string message)
        {
            string warningMessage = $"[{GetTimeStamp()}][W][WinDeskClock]: {message}";
            if (GetConsoleWindow() != IntPtr.Zero)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine(warningMessage);
                Console.ResetColor();
            }
            if (Debugger.IsAttached)
            {
                Debug.WriteLine(warningMessage);
            }
            await Write2File(warningMessage.Replace("[WinDeskClock]", ""));
        }
        public static async Task Info(string message)
        {
            string infoMessage = $"[{GetTimeStamp()}][I][WinDeskClock]: {message}";
            if (GetConsoleWindow() != IntPtr.Zero)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine(infoMessage);
                Console.ResetColor();
            }
            if (Debugger.IsAttached)
            {
                Debug.WriteLine(infoMessage);
            }
            await Write2File(infoMessage.Replace("[WinDeskClock]", ""));
        }

        public static async Task Write2File(string message)
        {
            try
            {
                if (!File.Exists(LogFilePath))
                {
                    using (System.IO.StreamWriter file = new System.IO.StreamWriter(LogFilePath, false))
                    {
                        await file.WriteLineAsync($"=== WinDeskClock by STY1001 ===\nLog file created at: {GetTimeStamp()}\n");
                        Console.WriteLine($"Log file does not exist, creating new log file");
                        Debug.WriteLine($"Log file does not exist, creating new log file");
                    }
                }

                using (System.IO.StreamWriter file = new System.IO.StreamWriter(LogFilePath, true))
                {
                    await file.WriteLineAsync(message);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"!!! Failed to write to log file: {ex.Message} !!!");
                Debug.WriteLine($"!!! Failed to write to log file: {ex.Message} !!!");
            }
        }
    }
}
