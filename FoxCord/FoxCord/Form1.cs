using Microsoft.Web.WebView2.Core;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace FoxCord
{
    
    public partial class FoxCord : Form
    {
        private readonly uint _showMessage;

        [DllImport("dwmapi.dll")]
        
        private static extern int DwmSetWindowAttribute(
            IntPtr hwnd,
            int dwAttribute,
            ref int pvAttribute,
            int cbAttribute);

        private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;
        private SysTrayManager trayManager;
        private bool exiting = false;
        public FoxCord()
        {
            _showMessage = NativeMethods.RegisterWindowMessage(Program.WindowMessageName);
            InitializeComponent();

            trayManager = new SysTrayManager(this);

            int enabled = 1;
            DwmSetWindowAttribute(
                this.Handle,
                DWMWA_USE_IMMERSIVE_DARK_MODE,
                ref enabled,
                sizeof(int));
        }


        protected override void WndProc(ref Message m)
        {
            if (m.Msg == _showMessage)
            {
                MessageBox.Show(
                    "Another FoxCord instance tried to start.",
                    "FoxCord",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);

                if (NativeMethods.IsIconic(Handle))
                    NativeMethods.ShowWindow(Handle, NativeMethods.SW_RESTORE);

                WindowState = FormWindowState.Normal;
                Show();
                Activate();
                BringToFront();

                NativeMethods.BringWindowToTop(Handle);
                NativeMethods.SetForegroundWindow(Handle);

                TopMost = true;
                TopMost = false;

                Focus();

                return;
            }

            base.WndProc(ref m);
        }
        private async void FoxCord_Load(object sender, EventArgs e)
        {
            await webView21.EnsureCoreWebView2Async();
            webView21.CoreWebView2.Settings.AreDefaultContextMenusEnabled = false; // Botão direito
            webView21.CoreWebView2.Settings.AreDevToolsEnabled = false;             // F12 / DevTools
            webView21.CoreWebView2.Settings.AreBrowserAcceleratorKeysEnabled = false; // F12, Ctrl+Shift+I, etc.
            webView21.CoreWebView2.Settings.IsStatusBarEnabled = false;             // Barra inferior com links
            webView21.CoreWebView2.NewWindowRequested += CoreWebView2_NewWindowRequested;
            webView21.CoreWebView2.Navigate("https://discord.com/app");

            _ = UpdateChecker.CheckForUpdatesAsync();
        }

        private void CoreWebView2_AcceleratorKeyPressed(
            object? sender,
            Microsoft.Web.WebView2.Core.CoreWebView2AcceleratorKeyPressedEventArgs e)
        {
            e.Handled = true;
        }


        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (!exiting)
            {
                e.Cancel = true;
                Hide();
                return;
            }

            trayManager.Dispose();
            base.OnFormClosing(e);
        }

        public void ExitApplication()
        {
            exiting = true;
            Close();
        }
        private void CoreWebView2_NewWindowRequested(object? sender, CoreWebView2NewWindowRequestedEventArgs e)
        {
            e.Handled = true;

            Process.Start(new ProcessStartInfo
            {
                FileName = e.Uri,
                UseShellExecute = true
            });
        }

        private void webView21_Click(object sender, EventArgs e)
        {

        }
    }
}