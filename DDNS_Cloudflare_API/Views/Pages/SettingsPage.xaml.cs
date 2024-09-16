using DDNS_Cloudflare_API.ViewModels.Pages;
using Microsoft.Win32;
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
            InitializeStartupSettings();
        }

        private void InitializeStartupSettings()
        {
            var (runOnStartup, loadProfilesOnStartup) = LoadStartupSetting();

            RunOnStartupCheckBox.IsChecked = runOnStartup;
            LoadProfilesOnStartupCheckBox.IsChecked = loadProfilesOnStartup;
        }

        private void RunOnStartup_Checked(object sender, RoutedEventArgs e)
        {
            SetStartup(true);
            SaveStartupSetting(true, LoadProfilesOnStartupCheckBox.IsChecked == true);
        }

        private void RunOnStartup_Unchecked(object sender, RoutedEventArgs e)
        {
            SetStartup(false);
            SaveStartupSetting(false, LoadProfilesOnStartupCheckBox.IsChecked == true);
        }

        private void LoadProfilesOnStartup_Checked(object sender, RoutedEventArgs e)
        {
            SaveStartupSetting(RunOnStartupCheckBox.IsChecked == true, true);
        }

        private void LoadProfilesOnStartup_Unchecked(object sender, RoutedEventArgs e)
        {
            SaveStartupSetting(RunOnStartupCheckBox.IsChecked == true, false);
        }

        private void SetStartup(bool enable)
        {
            string appName = "DDNS Cloudflare API";
            string exePath = System.Reflection.Assembly.GetExecutingAssembly().Location;

            using (RegistryKey key = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true))
            {
                if (enable)
                {
                    key.SetValue(appName, $"\"{exePath}\"");
                }
                else
                {
                    key.DeleteValue(appName, false);
                }
            }
        }

        private void SaveStartupSetting(bool runOnStartup, bool loadProfilesOnStartup)
        {
            var settingsFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "DDNS_Cloudflare_API", "startupSettings.json");

            Dictionary<string, object> startupSettings;

            // Check if the settings file exists
            if (File.Exists(settingsFilePath))
            {
                // Load existing settings
                string existingJson = File.ReadAllText(settingsFilePath);
                startupSettings = JsonSerializer.Deserialize<Dictionary<string, object>>(existingJson);
            }
            else
            {
                // Create a new settings dictionary if the file doesn't exist
                startupSettings = new Dictionary<string, object>();
            }

            // Update or add the settings
            startupSettings["RunOnStartup"] = runOnStartup;
            startupSettings["LoadProfilesOnStartup"] = loadProfilesOnStartup;

            // Serialize and save the updated settings
            string json = JsonSerializer.Serialize(startupSettings, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(settingsFilePath, json);
        }



        private (bool runOnStartup, bool loadProfilesOnStartup) LoadStartupSetting()
        {
            var settingsFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "DDNS_Cloudflare_API", "startupSettings.json");

            if (File.Exists(settingsFilePath))
            {
                string json = File.ReadAllText(settingsFilePath);
                var startupSettings = JsonSerializer.Deserialize<Dictionary<string, bool>>(json);

                return (startupSettings["RunOnStartup"], startupSettings["LoadProfilesOnStartup"]);
            }
            else
            {
                CreateStartupSetting();
            }

            // Default values if settings file doesn't exist
            return (false, false);
        }

        private void CreateStartupSetting()
        {
            var settingsFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "DDNS_Cloudflare_API", "startupSettings.json");

            Dictionary<string, object> startupSettings;
            
            // Create a new settings dictionary if the file doesn't exist
            startupSettings = new Dictionary<string, object>();


            // Update or add the settings
            startupSettings["RunOnStartup"] = true;
            startupSettings["LoadProfilesOnStartup"] = false;

            // Serialize and save the updated settings
            string json = JsonSerializer.Serialize(startupSettings, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(settingsFilePath, json);
        }


        // This is the method you mentioned, still present in the class.
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
