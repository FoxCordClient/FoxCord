using System;
using System.Diagnostics;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace FoxCord
{
    public static class UpdateChecker
    {
        public static readonly string CurrentVersionStr = "1.0.0";

        private const string GitHubApiUrl = "https://api.github.com/repos/FoxCordClient/FoxCord/releases/latest";
        private const string ReleasesUrl = "https://github.com/FoxCordClient/FoxCord/releases/latest";

        public static async Task CheckForUpdatesAsync()
        {
            try
            {
                using var client = new HttpClient();
                client.DefaultRequestHeaders.Add("User-Agent", "FoxCord-UpdateChecker");

                var response = await client.GetAsync(GitHubApiUrl);

                if (!response.IsSuccessStatusCode)
                    return;

                string jsonContent = await response.Content.ReadAsStringAsync();

                using JsonDocument doc = JsonDocument.Parse(jsonContent);

                if (doc.RootElement.TryGetProperty("tag_name", out JsonElement tagElement))
                {
                    string latestTag = tagElement.GetString() ?? "";
                    CompareVersions(CurrentVersionStr, latestTag);
                }
            }
            catch (HttpRequestException)
            {
                MessageBox.Show(
                    "Unable to check for updates.\nPlease check your internet connection.",
                    "FoxCord - Connection Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }
        }

        private static void CompareVersions(string localVerStr, string onlineTag)
        {
            string cleanLocal = localVerStr.TrimStart('v', 'V');
            string cleanOnline = onlineTag.TrimStart('v', 'V');

            if (!Version.TryParse(cleanLocal, out Version? localVersion) ||
                !Version.TryParse(cleanOnline, out Version? onlineVersion))
            {
                return;
            }

            int result = localVersion.CompareTo(onlineVersion);

            if (result < 0)
            {
                bool forceUpdate = IsForceUpdate(localVersion, onlineVersion);

                if (forceUpdate)
                {
                    while (true)
                    {
                        DialogResult dialog = MessageBox.Show(
                            $"Your FoxCord version ({localVerStr}) is too old.\n\n" +
                            $"Latest version: {onlineTag}\n\n" +
                            "OK = Update Now\n" +
                            "Cancel = Not Allowed",
                            "FoxCord - Update Required",
                            MessageBoxButtons.OKCancel,
                            MessageBoxIcon.Warning);

                        if (dialog == DialogResult.OK)
                        {
                            Process.Start(new ProcessStartInfo
                            {
                                FileName = ReleasesUrl,
                                UseShellExecute = true
                            });

                            Application.Exit();
                            return;
                        }
                    }
                }
                else
                {
                    DialogResult dialog = MessageBox.Show(
                        $"A new version of FoxCord is available!\n\n" +
                        $"Current Version: {localVerStr}\n" +
                        $"Latest Version: {onlineTag}\n\n" +
                        "OK = Update Now\n" +
                        "Cancel = Update Later",
                        "FoxCord - Update Available",
                        MessageBoxButtons.OKCancel,
                        MessageBoxIcon.Information);

                    if (dialog == DialogResult.OK)
                    {
                        Process.Start(new ProcessStartInfo
                        {
                            FileName = ReleasesUrl,
                            UseShellExecute = true
                        });
                    }
                }
            }
            else if (result > 0)
            {
                MessageBox.Show(
                    "You're using a newer/development build of FoxCord.",
                    "FoxCord",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
        }

        private static bool IsForceUpdate(Version current, Version latest)
        {
            if (latest.Major > current.Major)
                return true;

            if (latest.Minor - current.Minor >= 2)
                return true;

            return false;
        }
    }
}