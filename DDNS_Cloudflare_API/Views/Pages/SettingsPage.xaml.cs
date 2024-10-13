/*
 * Author: Hudaifa Abdullah
 * @7odaifa_ab
 * info@huimangtech.com
 *
 * This class defines the logic for managing the Settings Page of the DDNS Cloudflare API application.
 * It handles application startup settings, update checks, and allows users to configure the application behavior on startup.
 */

using DDNS_Cloudflare_API.ViewModels.Pages;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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

        #region Constructor

        // Initializes the Settings page and loads the startup settings
        public SettingsPage(SettingsViewModel viewModel)
        {
            ViewModel = viewModel;
            DataContext = this;
            InitializeComponent();
            InitializeStartupSettings();
        }

        #endregion

        #region Startup Settings

        // Initializes startup settings by loading them from a JSON file
        private void InitializeStartupSettings()
        {
            var (runOnStartup, loadProfilesOnStartup) = LoadStartupSetting();
            RunOnStartupCheckBox.IsChecked = runOnStartup;
            LoadProfilesOnStartupCheckBox.IsChecked = loadProfilesOnStartup;
        }

        // Handles checking the "Run On Startup" option and saves the setting
        private void RunOnStartup_Checked(object sender, RoutedEventArgs e)
        {
            SetStartup(true);
            SaveStartupSetting(true, LoadProfilesOnStartupCheckBox.IsChecked == true);
        }

        // Handles unchecking the "Run On Startup" option and saves the setting
        private void RunOnStartup_Unchecked(object sender, RoutedEventArgs e)
        {
            SetStartup(false);
            SaveStartupSetting(false, LoadProfilesOnStartupCheckBox.IsChecked == true);
        }

        // Handles checking the "Load Profiles On Startup" option
        private void LoadProfilesOnStartup_Checked(object sender, RoutedEventArgs e)
        {
            SaveStartupSetting(RunOnStartupCheckBox.IsChecked == true, true);
        }

        // Handles unchecking the "Load Profiles On Startup" option
        private void LoadProfilesOnStartup_Unchecked(object sender, RoutedEventArgs e)
        {
            SaveStartupSetting(RunOnStartupCheckBox.IsChecked == true, false);
        }

        // Enables or disables the application to run at startup by modifying registry settings
        private void SetStartup(bool enable)
        {
            string appName = "DDNS Cloudflare API";

            // Correct the executable path to ensure it's not pointing to a DLL
            string exePath = Process.GetCurrentProcess().MainModule.FileName;

            using (RegistryKey key = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true))
            {
                if (enable)
                {
                    // Add the application path to the registry for startup
                    key.SetValue(appName, $"\"{exePath}\"");
                }
                else
                {
                    // Remove the application from the startup registry
                    key.DeleteValue(appName, false);
                }
            }
        }


        #endregion

        #region Saving and Loading Settings

        // Saves the startup settings to a JSON file
        private void SaveStartupSetting(bool runOnStartup, bool loadProfilesOnStartup)
        {
            var settingsFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "DDNS_Cloudflare_API", "startupSettings.json");
            Dictionary<string, object> startupSettings;

            if (File.Exists(settingsFilePath))
            {
                string existingJson = File.ReadAllText(settingsFilePath);
                startupSettings = JsonSerializer.Deserialize<Dictionary<string, object>>(existingJson);
            }
            else
            {
                startupSettings = new Dictionary<string, object>();
            }

            startupSettings["RunOnStartup"] = runOnStartup;
            startupSettings["LoadProfilesOnStartup"] = loadProfilesOnStartup;

            string json = JsonSerializer.Serialize(startupSettings, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(settingsFilePath, json);
        }

        // Loads startup settings from a JSON file
        private (bool runOnStartup, bool loadProfilesOnStartup) LoadStartupSetting()
        {
            var settingsFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "DDNS_Cloudflare_API", "startupSettings.json");

            if (File.Exists(settingsFilePath))
            {
                string json = File.ReadAllText(settingsFilePath);
                var startupSettings = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json);

                bool runOnStartup = startupSettings.ContainsKey("RunOnStartup") && startupSettings["RunOnStartup"].GetBoolean();
                bool loadProfilesOnStartup = startupSettings.ContainsKey("LoadProfilesOnStartup") && startupSettings["LoadProfilesOnStartup"].GetBoolean();

                foreach (var kvp in startupSettings)
                {
                    if (kvp.Key == "RunOnStartup" || kvp.Key == "LoadProfilesOnStartup")
                        continue;

                    if (kvp.Value.ValueKind == JsonValueKind.Object)
                    {
                        var profileSettings = kvp.Value;
                        bool isRunning = profileSettings.GetProperty("IsRunning").GetBoolean();
                        int interval = profileSettings.GetProperty("Interval").GetInt32();

                        Debug.WriteLine($"Profile: {kvp.Key}, IsRunning: {isRunning}, Interval: {interval}");
                    }
                }

                return (runOnStartup, loadProfilesOnStartup);
            }
            else
            {
                CreateStartupSetting();
            }

            return (false, false);
        }

        // Creates default startup settings
        private void CreateStartupSetting()
        {
            var settingsFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "DDNS_Cloudflare_API", "startupSettings.json");
            Dictionary<string, object> startupSettings = new Dictionary<string, object>
            {
                ["RunOnStartup"] = true,
                ["LoadProfilesOnStartup"] = false
            };

            string json = JsonSerializer.Serialize(startupSettings, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(settingsFilePath, json);
        }

        #endregion

        #region Update Check

        // Button click handler to check for application updates
        private async void BtnCheckForUpdate_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var latestVersion = await GetLatestVersionAsync();
                var currentVersion = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? string.Empty;

                Version latest = new Version(latestVersion.TrimStart('v'));
                Version current = new Version(currentVersion);

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

        // Retrieves the latest version from the GitHub releases page
        private async Task<string> GetLatestVersionAsync()
        {
            using HttpClient client = new HttpClient();
            client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("DDNS-Cloudflare-API", "1.0"));
            var response = await client.GetStringAsync("https://api.github.com/repos/7odaifa-ab/DDNS-Cloudflare-API/releases/latest");
            var releaseInfo = JsonSerializer.Deserialize<Dictionary<string, object>>(response);

            return releaseInfo["tag_name"].ToString().TrimStart('v');
        }

        // Downloads and updates the application
        private async Task DownloadAndUpdateAsync(string version)
        {
            try
            {
                var downloadUrl = $"https://github.com/7odaifa-ab/DDNS-Cloudflare-API/releases/download/{version}/DDNS.Cloudflare.API-Win-x64.zip";
                var tempPath = Path.GetTempPath();
                var zipFilePath = Path.Combine(tempPath, "DDNS-Cloudflare-API.zip");
                var extractPath = AppDomain.CurrentDomain.BaseDirectory;

                using HttpClient client = new HttpClient();
                var data = await client.GetByteArrayAsync(downloadUrl);
                await File.WriteAllBytesAsync(zipFilePath, data);

                ZipFile.ExtractToDirectory(zipFilePath, extractPath, true);

                MessageBox.Show("Update completed. The application will now restart.", "Update Completed");

                Process.Start(Application.ResourceAssembly.Location);
                Application.Current.Shutdown();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error during update: {ex.Message}", "Update Failed", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion
    }
}
