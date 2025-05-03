using System.IO.Compression;
using System.Runtime.InteropServices;
using System.Text.Json;

namespace AfterlifeModsInstaller
{
    public partial class Form1 : Form
    {
        private string installationPath = null;
        [DllImport("user32.dll")]
        public static extern bool ReleaseCapture();

        [DllImport("user32.dll")]
        public static extern IntPtr SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);

        private const int WM_NCLBUTTONDOWN = 0xA1;
        private const int HTCAPTION = 0x2;
        public Form1()
        {
            InitializeComponent();
        }
        private async void CoreWebView2_WebMessageReceived(object sender, Microsoft.Web.WebView2.Core.CoreWebView2WebMessageReceivedEventArgs e)
        {
            string message = null;

            try
            {
                // Attempt to get the message as string
                message = e.TryGetWebMessageAsString();
            }
            catch (Exception ex)
            {
                UpdateDebugMessage("Error parsing WebMessage: " + ex.Message);
                return;
            }

            if (string.IsNullOrEmpty(message))
            {
                UpdateDebugMessage("Received empty or null message.");
                return;
            }

            // Handle the specific message cases
            if (message == "exit")
            {
                Application.Exit();
            }
            if (message.Contains("www.") || message.Contains("http"))
            {
                // Use the message itself as the custom URL
                string customUrl = message.Trim();

                if (string.IsNullOrWhiteSpace(installationPath))
                {
                    UpdateDebugMessage("Please select an installation path before downloading the mod.");
                    return;
                }

                UpdateDebugMessage("");
                await DownloadModFromUrl(customUrl);
            }
            else if (message == "downloadMod")
            {
                if (string.IsNullOrWhiteSpace(installationPath))
                {
                    UpdateDebugMessage("Please select an installation path before downloading the mod.");
                    return;
                }

                UpdateDebugMessage("Starting mod download...");
                string defaultUrl = "https://www.dropbox.com/scl/fi/7ditv85uzenm8q950vva2/AfterlifeModInstaller.zip?rlkey=o8h04h738qiphrrv4xjppcgao&st=hq0ge3m5&dl=1";
                await DownloadModFromUrl(defaultUrl);
            }
            else if (message == "selectPath")
            {
                using (FolderBrowserDialog folderDialog = new FolderBrowserDialog())
                {
                    folderDialog.Description = "Select the installation path";
                    if (folderDialog.ShowDialog() == DialogResult.OK)
                    {
                        installationPath = folderDialog.SelectedPath;

                        if (!installationPath.EndsWith(Path.DirectorySeparatorChar.ToString()))
                        {
                            installationPath += Path.DirectorySeparatorChar;
                        }

                        // Update debug message with the selected path
                        UpdateDebugMessage("Selected installation path: " + installationPath);

                        // Serialize and send the path as an object
                        var messageObject = new { path = installationPath };
                        string jsonMessage = JsonSerializer.Serialize(messageObject);

                        // Escape the JSON string for safe JavaScript injection
                        string escapedJsonMessage = jsonMessage.Replace("'", "\\'").Replace("\n", "\\n").Replace("\r", "\\r");

                        // Send the escaped JSON message to JavaScript
                        string script = $"window.chrome.webview.postMessage('{escapedJsonMessage}');";
                        await webView21.ExecuteScriptAsync(script);
                    }
                    else
                    {
                        UpdateDebugMessage("Path selection cancelled.");
                    }
                }
            }
        }
        private async Task DownloadModFromUrl(string downloadUrl)
        {
            string targetFile = Path.Combine(installationPath, "AfterlifeMod.zip");

            try
            {
                using (HttpClient client = new HttpClient())
                {
                    var response = await client.GetAsync(downloadUrl);
                    if (!response.IsSuccessStatusCode || !response.Content.Headers.ContentType.MediaType.Contains("application"))
                    {
                        UpdateDebugMessage("The download URL did not return a valid file.");
                        return;
                    }

                    byte[] fileBytes = await response.Content.ReadAsByteArrayAsync();
                    await File.WriteAllBytesAsync(targetFile, fileBytes);
                }

                Directory.CreateDirectory(installationPath);

                using (ZipArchive archive = ZipFile.OpenRead(targetFile))
                {
                    foreach (var entry in archive.Entries)
                    {
                        string destinationPath = Path.Combine(installationPath, entry.FullName);
                        if (string.IsNullOrEmpty(entry.Name))
                        {
                            Directory.CreateDirectory(destinationPath);
                        }
                        else
                        {
                            Directory.CreateDirectory(Path.GetDirectoryName(destinationPath));
                            entry.ExtractToFile(destinationPath, overwrite: true);
                        }
                    }
                }

                File.Delete(targetFile);
                UpdateDebugMessage("Mod extracted and cleaned up successfully.");
            }
            catch (Exception ex)
            {
                UpdateDebugMessage("Error downloading or extracting mod: " + ex.Message);
            }
        }
        // Method to send messages to the debug area in the HTML
        private async void UpdateDebugMessage(string message)
        {
            string partial = "";
            foreach (char c in message)
            {
                partial += c;
                string jsSafe = System.Text.Json.JsonSerializer.Serialize(partial);
                string js = $"postDebugMessage({jsSafe});";
                await webView21.ExecuteScriptAsync(js);
                await Task.Delay(30); // Typing speed
            }
        }

        private string EscapeJsString(string input)
        {
            return input.Replace("\\", "\\\\")
                        .Replace("'", "\\'")
                        .Replace("\r", "")
                        .Replace("\n", "\\n");
        }

        private async void Form1_Load(object sender, EventArgs e)
        {
            await webView21.EnsureCoreWebView2Async();
            webView21.CoreWebView2.WebMessageReceived += CoreWebView2_WebMessageReceived;

            string htmlFolderPath = Path.Combine(Application.StartupPath, "html_files");
            Directory.CreateDirectory(htmlFolderPath);

            string htmlFilePath = Path.Combine(htmlFolderPath, "AfterlifeModsInstaller.html");

            // URL to the raw HTML file in your GitHub repo
            string githubRawUrl = "https://raw.githubusercontent.com/yourusername/yourrepo/main/html_files/AfterlifeModsInstaller.html";

            try
            {
                using (HttpClient client = new HttpClient())
                {
                    string htmlContent = await client.GetStringAsync(githubRawUrl);
                    await File.WriteAllTextAsync(htmlFilePath, htmlContent);
                }

                // Load the HTML file into WebView2
                string htmlFileUri = new Uri(htmlFilePath).AbsoluteUri;
                webView21.Source = new Uri(htmlFileUri);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to fetch HTML from GitHub:\n" + ex.Message);
            }
        }

        private void Form1_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                ReleaseCapture();
                SendMessage(this.Handle, WM_NCLBUTTONDOWN, HTCAPTION, 0);
            }
        }
    }
}
