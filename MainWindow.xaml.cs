using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;
using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows;

namespace RobloxShortcutGenerator
{
    public partial class MainWindow : Window
    {
        private readonly HttpClient _httpClient = new HttpClient();

        public MainWindow()
        {
            InitializeComponent();
        }

        private async void BtnCreate_Click(object sender, RoutedEventArgs e)
        {
            // 1. Validation
            if (!long.TryParse(TxtPlaceId.Text.Trim(), out long placeId))
            {
                MessageBox.Show("Enter a valid Place ID.");
                return;
            }

            string launcherName = string.IsNullOrWhiteSpace(TxtLauncherName.Text) ? "Roblox Game" : TxtLauncherName.Text.Trim();

            // Remove invalid characters to prevent file saving errors
            string safeName = string.Join("_", launcherName.Split(Path.GetInvalidFileNameChars())).Trim();

            try
            {
                BtnCreate.IsEnabled = false;
                StatusLabel.Text = "Downloading Icon...";

                // 2. Setup Icon Storage
                // We save icons in LocalAppData so the shortcut doesn't lose its icon later
                string iconDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "RobCutIcons");
                if (!Directory.Exists(iconDir)) Directory.CreateDirectory(iconDir);
                string iconPath = Path.Combine(iconDir, $"{placeId}.ico");

                await DownloadIconAsIco(placeId, iconPath);

                StatusLabel.Text = "Creating Launcher...";

                // 3. Create the .url Launcher
                // Using SpecialFolder.Desktop handles OneDrive and Non-English paths (No more ??? errors)
                string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                string finalPath = Path.Combine(desktopPath, $"{safeName}.url");

                string linkCode = string.IsNullOrWhiteSpace(TxtLinkCode.Text) ? "" : $"&linkCode={TxtLinkCode.Text.Trim()}";
                string robloxUri = $"roblox://placeId={placeId}{linkCode}";

                // This text format creates a functional Windows launcher with an icon
                string[] fileContent = {
                    "[InternetShortcut]",
                    $"URL={robloxUri}",
                    "IconIndex=0",
                    $"IconFile={iconPath}"
                };

                // Standard .NET saving supports all languages (Unicode)
                await File.WriteAllLinesAsync(finalPath, fileContent);

                StatusLabel.Text = "Done!";
                MessageBox.Show("Launcher created successfully on your Desktop!");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message);
                StatusLabel.Text = "Error";
            }
            finally
            {
                BtnCreate.IsEnabled = true;
            }
        }

        private async Task DownloadIconAsIco(long id, string path)
        {
            // Get the thumbnail URL from Roblox API
            string json = await _httpClient.GetStringAsync($"https://thumbnails.roblox.com/v1/places/gameicons?placeIds={id}&size=512x512&format=Png&isCircular=false");
            int start = json.IndexOf("https://");
            string url = json.Substring(start, json.IndexOf("\"", start) - start);

            // Download the PNG image
            byte[] data = await _httpClient.GetByteArrayAsync(url);

            // Process image using ImageSharp
            using var img = Image.Load(new MemoryStream(data));
            using var ms = new MemoryStream();
            img.Save(ms, new PngEncoder());
            byte[] png = ms.ToArray();

            // Manually build the .ico file structure
            using var fs = new FileStream(path, FileMode.Create);
            using var w = new BinaryWriter(fs);
            w.Write((short)0); // Reserved
            w.Write((short)1); // Type (Icon)
            w.Write((short)1); // Number of images
            w.Write((byte)0);  // Width (0 = 256+)
            w.Write((byte)0);  // Height (0 = 256+)
            w.Write((byte)0);  // Color Palette
            w.Write((byte)0);  // Reserved
            w.Write((short)1); // Color Planes
            w.Write((short)32);// Bits per pixel
            w.Write(png.Length); // Image Size
            w.Write(22);         // Offset to image data
            w.Write(png);        // The PNG data itself
        }
    }
}