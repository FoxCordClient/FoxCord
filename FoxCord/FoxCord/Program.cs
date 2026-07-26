using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using Microsoft.Toolkit.Uwp.Notifications;

namespace FoxCord
{
    internal static class Program
    {
        public const string MutexName = "FoxCord_SingleInstance";
        public const string WindowMessageName = "FoxCord_ShowWindow";

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern bool PostMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

        [STAThread]
        static void Main()
        {
            using Mutex mutex = new(true, MutexName, out bool createdNew);

            uint messageId = NativeMethods.RegisterWindowMessage(WindowMessageName);

            if (!createdNew)
            {
                Process current = Process.GetCurrentProcess();

                foreach (Process process in Process.GetProcessesByName(current.ProcessName))
                {
                    if (process.Id == current.Id)
                        continue;

                    IntPtr hwnd = process.MainWindowHandle;

                    if (hwnd != IntPtr.Zero)
                    {
                        PostMessage(hwnd, messageId, IntPtr.Zero, IntPtr.Zero);
                    }
                }

                return;
            }

            Application.SetHighDpiMode(HighDpiMode.SystemAware);
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            ApplicationConfiguration.Initialize();
            Application.Run(new FoxCord());
        }
    }

    internal static class NativeMethods
    {
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern uint RegisterWindowMessage(string lpString);

        [DllImport("user32.dll")]
        public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll")]
        public static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        public static extern bool BringWindowToTop(IntPtr hWnd);

        [DllImport("user32.dll")]
        public static extern bool IsIconic(IntPtr hWnd);

        public const int SW_RESTORE = 9;
    }
}