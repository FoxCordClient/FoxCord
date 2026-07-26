using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace FoxCord
{
    public static class UpdateChecker
    {
        public static readonly string CurrentVersionStr = "0.7.2";

        private const string GitHubApiUrl = "https://api.github.com/repos/PshNsDev/FoxCord/releases/latest";

        /// <summary>
        /// </summary>
        public static async Task CheckForUpdatesAsync()
        {
            try
            {
                using var client = new HttpClient();
                client.DefaultRequestHeaders.Add("User-Agent", "FoxCord-UpdateChecker");

                var response = await client.GetAsync(GitHubApiUrl);

                if (!response.IsSuccessStatusCode)
                {
                    return;
                }

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
                    "Unable to check for updates. Please check your Wi-Fi or internet connection.",
                    "FoxCord - Connection Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Update check error: {ex.Message}");
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
                MessageBox.Show(
                    $"Hey, Updates out you are on {localVerStr} and the latest version is {onlineTag}",
                    "FoxCord - Update Available",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
            else if (result > 0)
            {
                MessageBox.Show(
                    "Hey, you recompiled the FoxCord right? or not? hm idk",
                    "FoxCord",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Question);
            }
            
        }
    }
}