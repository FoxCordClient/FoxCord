using Microsoft.Web.WebView2.Core;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace FoxCord
{
    public partial class FoxCord : Form
    {
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
            InitializeComponent();

            trayManager = new SysTrayManager(this);

            int enabled = 1;
            DwmSetWindowAttribute(
                this.Handle,
                DWMWA_USE_IMMERSIVE_DARK_MODE,
                ref enabled,
                sizeof(int));
        }

        private async void FoxCord_Load(object sender, EventArgs e)
        {
            // Inicializa o WebView2
            await webView21.EnsureCoreWebView2Async();

            // Qualquer janela nova abre no navegador padrão
            webView21.CoreWebView2.NewWindowRequested += CoreWebView2_NewWindowRequested;

            // Carrega o Discord
            webView21.CoreWebView2.Navigate("https://discord.com/app");
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