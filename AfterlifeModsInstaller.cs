using System.Diagnostics;
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
            if (message.Contains("updateConfig"))
            {
                try
                {
                    if (string.IsNullOrWhiteSpace(installationPath))
                    {
                        UpdateDebugMessage("Please select an installation path before updating the config file.");
                        return;
                    }

                    // Extract asset name
                    string assetName = message.Replace("updateConfig", "").Trim();

                    string configPath = Path.Combine(installationPath, "Mods", "AssetBundles", "AfterlifeAssetBundles.txt");
                    string assetBundlesFolder = Path.Combine(installationPath, "Mods", "AssetBundles");

                    // Ensure directory exists for the asset bundles
                    Directory.CreateDirectory(assetBundlesFolder);

                    // Search for any file that matches the asset name in the installation path
                    string[] assetFiles = Directory.GetFiles(installationPath, assetName + ".*", SearchOption.TopDirectoryOnly);

                    if (assetFiles.Length == 0)
                    {
                        // Notify user to manually move the asset
                        webView21.CoreWebView2.PostWebMessageAsJson(
                            JsonSerializer.Serialize(new
                            {
                                status = "error",
                                message = $"No file found matching \"{assetName}\" in the installation path. Please move the asset there manually."
                            })
                        );
                        return;
                    }

                    // Move found files to AssetBundles folder
                    foreach (var file in assetFiles)
                    {
                        string destFile = Path.Combine(assetBundlesFolder, Path.GetFileName(file));
                        File.Move(file, destFile, overwrite: true);
                    }

                    // Write asset name to config file if it's not already there
                    if (!File.Exists(configPath))
                    {
                        File.WriteAllText(configPath, assetName + Environment.NewLine);
                    }
                    else
                    {
                        var lines = new HashSet<string>(File.ReadAllLines(configPath), StringComparer.OrdinalIgnoreCase);
                        if (!lines.Contains(assetName))
                        {
                            File.AppendAllText(configPath, assetName + Environment.NewLine);
                        }
                    }

                    // Notify JavaScript about success
                    webView21.CoreWebView2.PostWebMessageAsJson(
                        JsonSerializer.Serialize(new { status = "assetAdded", asset = assetName })
                    );
                }
                catch (Exception ex)
                {
                    // Notify JavaScript about error
                    webView21.CoreWebView2.PostWebMessageAsJson(
                        JsonSerializer.Serialize(new { status = "error", message = ex.Message })
                    );
                }
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

                // Download and extract latest MelonLoader
                await DownloadLatestMelonLoaderAsync();
            }
            catch (Exception ex)
            {
                UpdateDebugMessage("Error downloading or extracting mod: " + ex.Message);
            }
        }

        private async Task DownloadLatestMelonLoaderAsync()
        {
            try
            {
                string architecture = Environment.Is64BitOperatingSystem ? "x64" : "x86";
                using HttpClient client = new HttpClient();

                client.DefaultRequestHeaders.UserAgent.ParseAdd("request"); // Required for GitHub API

                var response = await client.GetAsync("https://api.github.com/repos/LavaGang/MelonLoader/releases/latest");
                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync();
                using JsonDocument doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                string tagName = root.GetProperty("tag_name").GetString();
                var assets = root.GetProperty("assets").EnumerateArray();

                string downloadUrl = null;

                foreach (var asset in assets)
                {
                    var name = asset.GetProperty("name").GetString();
                    if (name.Contains($"MelonLoader.{architecture}.zip"))
                    {
                        downloadUrl = asset.GetProperty("browser_download_url").GetString();
                        break;
                    }
                }

                if (downloadUrl == null)
                {
                    UpdateDebugMessage($"Could not find a suitable MelonLoader.{architecture}.zip in the latest release.");
                    return;
                }

                string melonZipPath = Path.Combine(installationPath, $"MelonLoader.{architecture}.zip");
                byte[] zipData = await client.GetByteArrayAsync(downloadUrl);
                await File.WriteAllBytesAsync(melonZipPath, zipData);

                ZipFile.ExtractToDirectory(melonZipPath, installationPath, overwriteFiles: true);
                File.Delete(melonZipPath);

                UpdateDebugMessage($"Installed MelonLoader {tagName} ({architecture}).");
            }
            catch (Exception ex)
            {
                UpdateDebugMessage($"Failed to install MelonLoader: {ex.Message}");
            }
        }

        private static readonly HttpClient client = new HttpClient();

        public async Task DownloadLatestMelonLoaderAsync(string architecture)
        {
            try
            {
                // Fetch the latest release information
                var response = await client.GetAsync("https://api.github.com/repos/LavaGang/MelonLoader/releases/latest");
                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync();

                // Parse the JSON to find the appropriate asset
                using (JsonDocument doc = JsonDocument.Parse(json))
                {
                    var root = doc.RootElement;
                    var tagName = root.GetProperty("tag_name").GetString();
                    var assets = root.GetProperty("assets").EnumerateArray();

                    string downloadUrl = null;

                    foreach (var asset in assets)
                    {
                        var name = asset.GetProperty("name").GetString();
                        if (name.Contains($"MelonLoader.{architecture}.zip"))
                        {
                            downloadUrl = asset.GetProperty("browser_download_url").GetString();
                            break;
                        }
                    }

                    if (downloadUrl != null)
                    {
                        Console.WriteLine($"Latest MelonLoader version: {tagName}");
                        Console.WriteLine($"Downloading: {downloadUrl}");

                        // Proceed to download the file
                        var fileBytes = await client.GetByteArrayAsync(downloadUrl);
                        System.IO.File.WriteAllBytes($"MelonLoader.{architecture}.zip", fileBytes);

                        Console.WriteLine("Download completed successfully.");
                    }
                    else
                    {
                        Console.WriteLine($"No asset found for architecture: {architecture}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred: {ex.Message}");
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

        private async void Form1_Load(object sender, EventArgs e)
        {
            await webView21.EnsureCoreWebView2Async();
            webView21.CoreWebView2.WebMessageReceived += CoreWebView2_WebMessageReceived;

            // Define the folder path where the HTML file will be stored
            string htmlFolderPath = Path.Combine(Application.StartupPath, "html_files");
            Directory.CreateDirectory(htmlFolderPath);  // Creates folder if it doesn't exist

            // Path to the HTML file
            string htmlFilePath = Path.Combine(htmlFolderPath, "AfterlifeModsInstaller.html");

            // URL to the raw HTML file in your GitHub repo
            string githubRawUrl = "https://raw.githubusercontent.com/JayCoderr/AfterlifeModsInstaller/master/AfterlifeModsInstaller.html";

            try
            {
                // Fetch the HTML content from the GitHub raw URL
                using (HttpClient client = new HttpClient())
                {
                    string htmlContent = await client.GetStringAsync(githubRawUrl);

                    // Check if the file already exists and only update if content differs (optional)
                    if (File.Exists(htmlFilePath))
                    {
                        string existingContent = await File.ReadAllTextAsync(htmlFilePath);
                        if (existingContent != htmlContent)
                        {
                            await File.WriteAllTextAsync(htmlFilePath, htmlContent);
                            UpdateDebugMessage("Afterlife Mods Installer updated.");
                        }
                    }
                    else
                    {
                        await File.WriteAllTextAsync(htmlFilePath, htmlContent);
                        UpdateDebugMessage("Afterlife Mods Installer Latest Update Downloaded.");
                    }
                }

                // Load the HTML file into WebView2
                string htmlFileUri = new Uri(htmlFilePath).AbsoluteUri;
                webView21.Source = new Uri(htmlFileUri);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to fetch or update HTML from GitHub:\n{ex.Message}");
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
