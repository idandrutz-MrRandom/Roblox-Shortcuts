using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;
using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace RobloxShortcutGenerator
{
    public partial class MainWindow : Window
    {
        private readonly HttpClient _httpClient = new HttpClient();

        public MainWindow()
        {
            InitializeComponent();
            BtnCreate.MouseEnter += (s, e) => {
                DoubleAnimation wa = new DoubleAnimation { From = -0.5, To = 0.5, Duration = TimeSpan.FromMilliseconds(1400), AutoReverse = true, RepeatBehavior = RepeatBehavior.Forever };
                ((RotateTransform)BtnCreate.RenderTransform).BeginAnimation(RotateTransform.AngleProperty, wa);
            };
            BtnCreate.MouseLeave += (s, e) => ((RotateTransform)BtnCreate.RenderTransform).BeginAnimation(RotateTransform.AngleProperty, null);
        }

        private async void BtnCreate_Click(object sender, RoutedEventArgs e)
        {
            if (!long.TryParse(TxtPlaceId.Text.Trim(), out long placeId)) { MessageBox.Show("Enter a valid Place ID."); return; }
            string safeName = string.Join("_", TxtLauncherName.Text.Split(Path.GetInvalidFileNameChars())).Trim();
            if (string.IsNullOrEmpty(safeName)) safeName = "Roblox Game";

            try
            {
                BtnCreate.IsEnabled = false;
                StatusLabel.Text = "Downloading Icon...";

                string iconDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "RobloxShortcuts");
                if (!Directory.Exists(iconDir)) Directory.CreateDirectory(iconDir);
                string iconPath = Path.Combine(iconDir, $"{placeId}.ico");

                await DownloadIconAsync(placeId, iconPath);

                StatusLabel.Text = "Creating Shortcut...";
                Type shellType = Type.GetTypeFromProgID("WScript.Shell");
                dynamic shell = Activator.CreateInstance(shellType);
                var shortcut = shell.CreateShortcut(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), $"{safeName}.lnk"));

                string linkCode = string.IsNullOrWhiteSpace(TxtLinkCode.Text) ? "" : $"&linkCode={TxtLinkCode.Text.Trim()}";
                shortcut.TargetPath = $"roblox://placeId={placeId}{linkCode}";
                shortcut.IconLocation = iconPath;
                shortcut.Save();

                StatusLabel.Text = "Done!";
                MessageBox.Show("Created on Desktop!");
            }
            catch (Exception ex) { MessageBox.Show("Error: " + ex.Message); StatusLabel.Text = "Error"; }
            finally { BtnCreate.IsEnabled = true; }
        }

        private async Task DownloadIconAsync(long placeId, string savePath)
        {
            string json = await _httpClient.GetStringAsync($"https://thumbnails.roblox.com/v1/places/gameicons?placeIds={placeId}&size=512x512&format=Png&isCircular=false");
            int start = json.IndexOf("https://");
            string url = json.Substring(start, json.IndexOf("\"", start) - start);
            byte[] data = await _httpClient.GetByteArrayAsync(url);
            using var img = Image.Load(new MemoryStream(data));
            using var ms = new MemoryStream();
            img.Save(ms, new PngEncoder());
            byte[] png = ms.ToArray();
            using var fs = new FileStream(savePath, FileMode.Create);
            using var w = new BinaryWriter(fs);
            w.Write((short)0); w.Write((short)1); w.Write((short)1); w.Write((byte)0); w.Write((byte)0);
            w.Write((byte)0); w.Write((byte)0); w.Write((short)1); w.Write((short)32);
            w.Write(png.Length); w.Write(22); w.Write(png);
        }
    }
}