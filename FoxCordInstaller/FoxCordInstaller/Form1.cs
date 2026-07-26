using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Diagnostics;
using Microsoft.Win32;

namespace FoxCordInstaller
{
    public partial class Form1 : Form
    {
        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(
            IntPtr hwnd,
            int dwAttribute,
            ref int pvAttribute,
            int cbAttribute);

        // Windows 11
        private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;
        private const int DWMWA_CAPTION_COLOR = 35;
        private const int DWMWA_TEXT_COLOR = 36;

        private System.Windows.Forms.Timer? finishTimer;
        private int finishTicks = 0;
        private const int FINISH_DURATION_TICKS = 15; // ~3s com intervalo de 200ms

        // Versão e caminho do arquivo zip agora são dinâmicos (FoxCord)
        private string appVersion = "";
        private string tempZipPath = "";

        public Form1()
        {
            InitializeComponent();
        }

        private async void Form1_Load(object sender, EventArgs e)
        {
            AplicarTemaEscuro();
            // Inicia o processo de download (substitui o antigo IniciarPreparacao com timer)
            await IniciarPreparacaoDownloadAsync();
        }

        private void AplicarTemaEscuro()
        {
            int dark = 1;
            DwmSetWindowAttribute(this.Handle, DWMWA_USE_IMMERSIVE_DARK_MODE, ref dark, sizeof(int));

            int captionColor = ColorToCOLORREF(15, 45, 90);
            DwmSetWindowAttribute(this.Handle, DWMWA_CAPTION_COLOR, ref captionColor, sizeof(int));

            int textColor = ColorToCOLORREF(255, 255, 255);
            DwmSetWindowAttribute(this.Handle, DWMWA_TEXT_COLOR, ref textColor, sizeof(int));
        }

        private static int ColorToCOLORREF(byte r, byte g, byte b)
        {
            return r | (g << 8) | (b << 16);
        }

        // ================= ETAPA 1: Preparação (Verificar API GitHub e Baixar) =================
        private async Task IniciarPreparacaoDownloadAsync()
        {
            installlab.Text = "Checking internet connection...";

            progressBar.Style = ProgressBarStyle.Marquee;
            progressBar.MarqueeAnimationSpeed = 30;
            progressBar.Minimum = 0;
            progressBar.Maximum = 100;

            try
            {
                using (HttpClient client = new HttpClient())
                {
                    // O GitHub exige um User-Agent válido para chamadas na API
                    client.DefaultRequestHeaders.Add("User-Agent", "FoxCord-Installer");

                    // 1. Consulta o JSON da última release no GitHub API
                    string apiUrl = "https://api.github.com/repos/PshNsDev/FoxCord/releases/latest";
                    string jsonResponse = await client.GetStringAsync(apiUrl);

                    // Parse do JSON retornado pela API
                    using (JsonDocument doc = JsonDocument.Parse(jsonResponse))
                    {
                        JsonElement root = doc.RootElement;

                        // Obtém a versão a partir do "tag_name" (ex: "v1.0.0" ou "1.0.0")
                        if (root.TryGetProperty("tag_name", out JsonElement tagElement))
                        {
                            appVersion = tagElement.GetString()?.Trim().TrimStart('v') ?? "1.0.0";
                        }
                        else
                        {
                            throw new Exception("Não foi possível identificar a tag da última versão.");
                        }

                        // Localiza a URL de download do 'fxcd.zip' na lista de assets do JSON
                        string downloadUrl = "";
                        if (root.TryGetProperty("assets", out JsonElement assetsElement) && assetsElement.ValueKind == JsonValueKind.Array)
                        {
                            foreach (JsonElement asset in assetsElement.EnumerateArray())
                            {
                                if (asset.TryGetProperty("name", out JsonElement nameElement) &&
                                    nameElement.GetString()?.Equals("fxcd.zip", StringComparison.OrdinalIgnoreCase) == true)
                                {
                                    if (asset.TryGetProperty("browser_download_url", out JsonElement downloadUrlElement))
                                    {
                                        downloadUrl = downloadUrlElement.GetString() ?? "";
                                        break;
                                    }
                                }
                            }
                        }

                        // Caso 'fxcd.zip' não tenha sido encontrado explicitamente no JSON, monta a URL de fallback padrão
                        if (string.IsNullOrEmpty(downloadUrl))
                        {
                            downloadUrl = $"https://github.com/PshNsDev/FoxCord/releases/download/{tagElement.GetString()}/fxcd.zip";
                        }

                        installlab.Text = $"Downloading FoxCord v{appVersion}...";

                        // 2. Baixa o arquivo zip da release
                        tempZipPath = Path.Combine(Path.GetTempPath(), "fxcd.zip");

                        using (var response = await client.GetAsync(downloadUrl, HttpCompletionOption.ResponseHeadersRead))
                        {
                            response.EnsureSuccessStatusCode();
                            using (var fs = new FileStream(tempZipPath, FileMode.Create, FileAccess.Write, FileShare.None))
                            {
                                await response.Content.CopyToAsync(fs);
                            }
                        }
                    }
                }

                // Arquivo baixado com sucesso! Iniciar a instalação.
                IniciarInstalacao();
            }
            catch (HttpRequestException)
            {
                MessageBox.Show("Não foi possível conectar à internet ou obter os dados da release no GitHub. Verifique sua conexão e tente novamente.", "Sem Conexão Online", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Application.Exit();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ocorreu um erro ao verificar/baixar os arquivos: " + ex.Message, "Erro de Download", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Application.Exit();
            }
        }

        // ================= ETAPA 2: Instalação (Backup, Limpeza e Extração) =================
        private void IniciarInstalacao()
        {
            installlab.Text = "Installing FoxCord...";

            progressBar.Style = ProgressBarStyle.Continuous;
            progressBar.Value = 0;

            try
            {
                string appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                string destinoBase = Path.Combine(appData, "FoxCord");
                string destinoApp = Path.Combine(destinoBase, "app-" + appVersion);
                string tempBackupPasta = Path.Combine(Path.GetTempPath(), "FoxCord.exe.WebView2.Backup");

                progressBar.Value = 10;

                // 1. Força o fechamento do aplicativo PRIMEIRO para soltar os arquivos do WebView2
                Process[] processos = Process.GetProcessesByName("FoxCord");
                foreach (Process p in processos)
                {
                    try
                    {
                        p.Kill();
                        p.WaitForExit(1000); // Aguarda até 1 segundo para garantir que fechou
                    }
                    catch { }
                }

                // Dá um tempinho extra para o sistema operacional liberar os bloqueios
                System.Threading.Thread.Sleep(500);
                progressBar.Value = 20;

                // 2. Faz o backup da pasta do WebView2 antes de deletar
                if (Directory.Exists(destinoBase))
                {
                    try
                    {
                        // Procura a pasta do WebView2 dentro da versão antiga
                        string[] pastasWebView2 = Directory.GetDirectories(destinoBase, "FoxCord.exe.WebView2", SearchOption.AllDirectories);

                        if (pastasWebView2.Length > 0)
                        {
                            string pastaAntigaDados = pastasWebView2[0];

                            // Garante que a pasta temporária de backup está limpa
                            if (Directory.Exists(tempBackupPasta))
                                Directory.Delete(tempBackupPasta, true);

                            // Copia tudo para o local temporário
                            CopiarDiretorio(pastaAntigaDados, tempBackupPasta);
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine("Aviso: Falha ao fazer backup da pasta WebView2: " + ex.Message);
                    }
                }

                // 3. Deleta toda a pasta da versão antiga com segurança
                if (Directory.Exists(destinoBase))
                {
                    try
                    {
                        Directory.Delete(destinoBase, true);
                    }
                    catch (Exception ex2)
                    {
                        MessageBox.Show("Não foi possível apagar os arquivos antigos da versão anterior. Você pode precisar reiniciar o computador.\n\n" + ex2.Message, "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                }

                // 4. Cria os novos diretórios
                Directory.CreateDirectory(destinoBase);
                Directory.CreateDirectory(destinoApp);

                progressBar.Value = 30;

                // 5. Extrai a nova versão do arquivo ZIP que acabamos de baixar
                ExtrairZip(tempZipPath, destinoApp);

                // Apaga o zip baixado da pasta temporária
                if (File.Exists(tempZipPath))
                {
                    File.Delete(tempZipPath);
                }

                progressBar.Value = 70;

                // 6. Restaura a pasta de dados do WebView2 para a versão nova (se existir um backup)
                if (Directory.Exists(tempBackupPasta))
                {
                    try
                    {
                        string novoCaminhoWebView2 = Path.Combine(destinoApp, "FoxCord.exe.WebView2");
                        CopiarDiretorio(tempBackupPasta, novoCaminhoWebView2);

                        // Limpa o backup
                        Directory.Delete(tempBackupPasta, true);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine("Aviso: Falha ao restaurar os dados do WebView2: " + ex.Message);
                    }
                }

                progressBar.Value = 85;

                string exePath = Path.Combine(destinoApp, "FoxCord.exe");

                // 7. Recria os Atalhos
                CriarAtalhos(exePath);

                // 8. Atualiza o Registro para mostrar a versão certa no painel de controle
                CriarRegistroDesinstalacao(destinoBase, exePath);

                progressBar.Value = 100;

                // Inicia animação final
                IniciarFinalizacao();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erro durante a instalação: " + ex.Message, "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // FUNÇÃO: Utilizada para copiar pastas e subpastas de forma robusta
        private void CopiarDiretorio(string sourceDir, string destDir)
        {
            Directory.CreateDirectory(destDir);

            foreach (string file in Directory.GetFiles(sourceDir))
            {
                string destFile = Path.Combine(destDir, Path.GetFileName(file));
                File.Copy(file, destFile, true);
            }

            foreach (string subdir in Directory.GetDirectories(sourceDir))
            {
                string destSubdir = Path.Combine(destDir, Path.GetFileName(subdir));
                CopiarDiretorio(subdir, destSubdir);
            }
        }

        private void ExtrairZip(string arquivoZip, string destino)
        {
            using (FileStream fs = new FileStream(arquivoZip, FileMode.Open, FileAccess.Read))
            {
                using (ZipArchive archive = new ZipArchive(fs, ZipArchiveMode.Read))
                {
                    foreach (ZipArchiveEntry entry in archive.Entries)
                    {
                        string destPath = Path.Combine(destino, entry.FullName);
                        string? destDir = Path.GetDirectoryName(destPath);

                        if (!string.IsNullOrEmpty(destDir) && !Directory.Exists(destDir))
                            Directory.CreateDirectory(destDir);

                        if (!string.IsNullOrEmpty(entry.Name))
                        {
                            entry.ExtractToFile(destPath, overwrite: true);
                        }
                    }
                }
            }
        }

        // ================= ETAPA 3: "Initializing" e Tela de Sucesso =================
        private void IniciarFinalizacao()
        {
            progressBar.Style = ProgressBarStyle.Marquee;
            progressBar.MarqueeAnimationSpeed = 20;

            finishTicks = 0;
            finishTimer = new System.Windows.Forms.Timer();
            finishTimer.Interval = 200;
            finishTimer.Tick += FinishTimer_Tick;
            finishTimer.Start();
        }

        private void FinishTimer_Tick(object? sender, EventArgs e)
        {
            finishTicks++;

            // Pontinhos de carregamento
            string dots = new string('.', finishTicks % 4);
            installlab.Text = "Initializing FoxCord" + dots;

            if (finishTicks >= FINISH_DURATION_TICKS)
            {
                finishTimer.Stop();
                finishTimer.Dispose();

                // Quando terminar a animação, atualiza a UI para o estado final de sucesso
                installlab.Text = "FoxCord has been successfully installed";
                cancel.Text = "Done";
                // A barra continua no loop Marquee automaticamente
            }
        }

        // ================= ETAPA 4: Atalhos e Registro =================
        private void CriarAtalhos(string exePath)
        {
            string startMenuPrograms = Environment.GetFolderPath(Environment.SpecialFolder.StartMenu) + @"\Programs";
            string pastaAtalhoStartMenu = Path.Combine(startMenuPrograms, "FoxCord");

            if (!Directory.Exists(pastaAtalhoStartMenu))
                Directory.CreateDirectory(pastaAtalhoStartMenu);

            string atalhoStartMenuPath = Path.Combine(pastaAtalhoStartMenu, "FoxCord.lnk");

            CriarAtalho(
                atalhoStartMenuPath,
                exePath,
                Path.GetDirectoryName(exePath) ?? string.Empty,
                exePath,
                "FoxCord");

            string desktop = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
            string atalhoDesktopPath = Path.Combine(desktop, "FoxCord.lnk");

            CriarAtalho(
                atalhoDesktopPath,
                atalhoStartMenuPath,
                Path.GetDirectoryName(exePath) ?? string.Empty,
                exePath,
                "FoxCord");
        }

        private void CriarAtalho(string atalhoPath, string targetPath, string workingDirectory, string iconPath, string description)
        {
            IShellLinkW link = (IShellLinkW)new ShellLinkCom();

            link.SetPath(targetPath);
            if (!string.IsNullOrEmpty(workingDirectory))
                link.SetWorkingDirectory(workingDirectory);
            link.SetDescription(description);
            link.SetIconLocation(iconPath, 0);

            IPersistFile persistFile = (IPersistFile)link;
            persistFile.Save(atalhoPath, false);
        }

        private void CriarRegistroDesinstalacao(string destinoBase, string exePath)
        {
            try
            {
                string registryPath = @"Software\Microsoft\Windows\CurrentVersion\Uninstall\FoxCord";

                using (RegistryKey key = Registry.CurrentUser.CreateSubKey(registryPath))
                {
                    if (key != null)
                    {
                        key.SetValue("DisplayName", "FoxCord");
                        key.SetValue("DisplayVersion", appVersion);
                        key.SetValue("Publisher", "FoxCord");
                        key.SetValue("DisplayIcon", exePath);
                        key.SetValue("InstallLocation", destinoBase);

                        string uninstallCmd = $"cmd.exe /c rmdir /s /q \"{destinoBase}\"";
                        key.SetValue("UninstallString", uninstallCmd);

                        key.SetValue("NoModify", 1, RegistryValueKind.DWord);
                        key.SetValue("NoRepair", 1, RegistryValueKind.DWord);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Aviso: Falha ao registrar desinstalador: " + ex.Message);
            }
        }

        private void installlab_Click(object sender, EventArgs e) { }
        private void progressBar_Click(object sender, EventArgs e) { }

        private void cancel_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void pictureBox1_Click(object sender, EventArgs e) { }
    }

    // ===== Interfaces COM nativas =====
    [ComImport]
    [Guid("00021401-0000-0000-C000-000000000046")]
    internal class ShellLinkCom { }

    [ComImport]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [Guid("000214F9-0000-0000-C000-000000000046")]
    internal interface IShellLinkW
    {
        void GetPath([Out, MarshalAs(UnmanagedType.LPWStr)] System.Text.StringBuilder pszFile, int cchMaxPath, out IntPtr pfd, int fFlags);
        void GetIDList(out IntPtr ppidl);
        void SetIDList(IntPtr pidl);
        void GetDescription([Out, MarshalAs(UnmanagedType.LPWStr)] System.Text.StringBuilder pszName, int cchMaxName);
        void SetDescription([MarshalAs(UnmanagedType.LPWStr)] string pszName);
        void GetWorkingDirectory([Out, MarshalAs(UnmanagedType.LPWStr)] System.Text.StringBuilder pszDir, int cchMaxPath);
        void SetWorkingDirectory([MarshalAs(UnmanagedType.LPWStr)] string pszDir);
        void GetArguments([Out, MarshalAs(UnmanagedType.LPWStr)] System.Text.StringBuilder pszArgs, int cchMaxPath);
        void SetArguments([MarshalAs(UnmanagedType.LPWStr)] string pszArgs);
        void GetHotkey(out short pwHotkey);
        void SetHotkey(short wHotkey);
        void GetShowCmd(out int piShowCmd);
        void SetShowCmd(int iShowCmd);
        void GetIconLocation([Out, MarshalAs(UnmanagedType.LPWStr)] System.Text.StringBuilder pszIconPath, int cchIconPath, out int piIcon);
        void SetIconLocation([MarshalAs(UnmanagedType.LPWStr)] string pszIconPath, int iIcon);
        void SetRelativePath([MarshalAs(UnmanagedType.LPWStr)] string pszPathRel, int dwReserved);
        void Resolve(IntPtr hwnd, int fFlags);
        void SetPath([MarshalAs(UnmanagedType.LPWStr)] string pszFile);
    }
}