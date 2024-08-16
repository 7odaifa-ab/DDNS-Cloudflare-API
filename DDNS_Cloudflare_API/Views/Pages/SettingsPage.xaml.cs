using DDNS_Cloudflare_API.ViewModels.Pages;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using Wpf.Ui.Controls;
using Application = System.Windows.Application;
using MessageBox = System.Windows.MessageBox;
using MessageBoxButton = System.Windows.MessageBoxButton;
using MessageBoxResult = System.Windows.MessageBoxResult;

namespace DDNS_Cloudflare_API.Views.Pages
{
    public partial class SettingsPage : INavigableView<SettingsViewModel>
    {
        public SettingsViewModel ViewModel { get; }

        public SettingsPage(SettingsViewModel viewModel)
        {
            ViewModel = viewModel;
            DataContext = this;

            InitializeComponent();
        }

        private async void BtnCheckForUpdate_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var latestVersion = await GetLatestVersionAsync();
                var currentVersion = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? String.Empty;

                // Parse the versions
                Version latest = new Version(latestVersion.TrimStart('v')); // Trim 'v' if it's in the version string from GitHub
                Version current = new Version(currentVersion);

                // Compare versions
                if (latest > current)
                {
                    var result = MessageBox.Show($"A new version ({latestVersion}) is available. Do you want to update?", "Update Available", MessageBoxButton.YesNo);
                    if (result == MessageBoxResult.Yes)
                    {
                        await DownloadAndUpdateAsync(latestVersion);
                    }
                }
                else
                {
                    MessageBox.Show("You are already using the latest version.", "No Updates Found");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error checking for updates: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }


        private async Task<string> GetLatestVersionAsync()
        {
            using HttpClient client = new HttpClient();
            client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("DDNS-Cloudflare-API", "1.0"));
            var response = await client.GetStringAsync("https://api.github.com/repos/7odaifa-ab/DDNS-Cloudflare-API/releases/latest");
            var releaseInfo = JsonSerializer.Deserialize<Dictionary<string, object>>(response);

            string latestVersionTag = releaseInfo["tag_name"].ToString(); // Assumes the release tag is the version number.
            string latestVersion = latestVersionTag.TrimStart('v'); // Remove 'v' prefix if present.

            return latestVersion;
        }


        private async Task DownloadAndUpdateAsync(string version)
        {
            try
            {
                var downloadUrl = $"https://github.com/7odaifa-ab/DDNS-Cloudflare-API/releases/download/{version}/DDNS.Cloudflare.API-Win-x64.zip";
                var tempPath = Path.GetTempPath();
                var zipFilePath = Path.Combine(tempPath, "DDNS-Cloudflare-API.zip");
                var extractPath = AppDomain.CurrentDomain.BaseDirectory;

                using (HttpClient client = new HttpClient())
                {
                    var data = await client.GetByteArrayAsync(downloadUrl);
                    await File.WriteAllBytesAsync(zipFilePath, data);
                }

                // Extract the ZIP file and overwrite existing files
                ZipFile.ExtractToDirectory(zipFilePath, extractPath, true);

                MessageBox.Show("Update completed. The application will now restart.", "Update Completed");

                // Restart the application
                System.Diagnostics.Process.Start(Application.ResourceAssembly.Location);
                Application.Current.Shutdown();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error during update: {ex.Message}", "Update Failed", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
