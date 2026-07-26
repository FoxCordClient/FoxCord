using Microsoft.Web.WebView2.Core;
using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
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
            DwmSetWindowAttribute(this.Handle, DWMWA_USE_IMMERSIVE_DARK_MODE, ref enabled, sizeof(int));
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


        private void NavigateToMessage(string url)
        {
            WindowState = FormWindowState.Normal;
            Show();
            Activate();
            webView21.CoreWebView2?.Navigate(url);
        }

        private async void FoxCord_Load(object sender, EventArgs e)
        {
            await InitializeWebViewAsync();
            _ = UpdateChecker.CheckForUpdatesAsync();
        }

        private async Task InitializeWebViewAsync(bool isRetry = false)
        {
            try
            {
                string userDataFolder = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "FoxCord", "WebView2Data");

                Directory.CreateDirectory(userDataFolder);

                string runtimeFolder = Path.Combine(AppContext.BaseDirectory, "WebView2Runtime");

                if (!Directory.Exists(runtimeFolder) || !File.Exists(Path.Combine(runtimeFolder, "msedgewebview2.exe")))
                {
                    throw new DirectoryNotFoundException(
                        $"WebView2 Fixed Version Runtime was not found at: {runtimeFolder}. Please reinstall FoxCord.");
                }

                var env = await CoreWebView2Environment.CreateAsync(
                    browserExecutableFolder: runtimeFolder,
                    userDataFolder: userDataFolder);

                await webView21.EnsureCoreWebView2Async(env);

                webView21.CoreWebView2.Settings.AreDefaultContextMenusEnabled = false;
                webView21.CoreWebView2.Settings.AreDevToolsEnabled = false;
                webView21.CoreWebView2.Settings.AreBrowserAcceleratorKeysEnabled = false;
                webView21.CoreWebView2.Settings.IsStatusBarEnabled = false;
                webView21.CoreWebView2.NewWindowRequested += CoreWebView2_NewWindowRequested;
                webView21.CoreWebView2.ProcessFailed += CoreWebView2_ProcessFailed;

                webView21.CoreWebView2.Navigate("https://discord.com/app");
            }
            catch (Exception ex) when (!isRetry)
            {
                Debug.WriteLine($"WebView2 failed, retrying: {ex}");

                try
                {
                    string userDataFolder = Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                        "FoxCord", "WebView2Data");

                    if (Directory.Exists(userDataFolder))
                        Directory.Delete(userDataFolder, recursive: true);
                }
                catch { }

                await InitializeWebViewAsync(isRetry: true);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Unable to initialize the navigation component (WebView2).\n\n" +
                    "Please reinstall FoxCord to fix this issue.\n\n" +
                    $"Technical Details: {ex.Message}",
                    "FoxCord - Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        private void CoreWebView2_ProcessFailed(object? sender, CoreWebView2ProcessFailedEventArgs e)
        {
            Debug.WriteLine($"WebView2 process failed: {e.ProcessFailedKind}");
            BeginInvoke(new Action(async () => await InitializeWebViewAsync(isRetry: true)));
        }

        private void CoreWebView2_AcceleratorKeyPressed(
            object? sender,
            CoreWebView2AcceleratorKeyPressedEventArgs e)
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

        private void webView21_Click(object sender, EventArgs e) { }
    }
}