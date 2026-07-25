using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

namespace FoxCord
{
    internal static class Program
    {
        private const string MutexName = "FoxCord_SingleInstance_Mutex";
        private const string WindowMessage = "FoxCord_ShowWindow";

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern bool PostMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

        [STAThread]
        static void Main()
        {
            using Mutex mutex = new Mutex(true, MutexName, out bool createdNew);

            uint message = NativeMethods.RegisterWindowMessage(WindowMessage);

            if (!createdNew)
            {
                IntPtr hwnd = NativeMethods.FindWindow(null, "FoxCord");

                if (hwnd != IntPtr.Zero)
                    PostMessage(hwnd, message, IntPtr.Zero, IntPtr.Zero);

                return;
            }

            ApplicationConfiguration.Initialize();
            Application.Run(new FoxCord());
        }
    }

    internal static class NativeMethods
    {
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern uint RegisterWindowMessage(string lpString);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern IntPtr FindWindow(string? lpClassName, string? lpWindowName);
    }
}