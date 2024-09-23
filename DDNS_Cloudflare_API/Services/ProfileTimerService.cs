/*
 * Author: Hudaifa Abdullah
 * @7odaifa_ab
 * info@huimangtech.com
 *
 * This class handles the logic for managing DNS profiles and timers in the DDNS Cloudflare API application.
 * It includes functions for starting/stopping timers, updating DNS records, fetching IP addresses, and managing profile status.
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Threading;
using System.Diagnostics;
using static System.Net.Mime.MediaTypeNames;
using System.Drawing;
using System.Numerics;
using System.Xml.Linq;

namespace DDNS_Cloudflare_API.Services
{
    public class ProfileTimerService
    {
        private readonly Dictionary<string, DispatcherTimer> profileTimers = new Dictionary<string, DispatcherTimer>();
        private readonly Dictionary<string, DispatcherTimer> uiTimers = new Dictionary<string, DispatcherTimer>(); // UI Timers

        private readonly Dictionary<string, Dictionary<string, object>> profileData;
        private readonly string profilesFolderPath;
        private readonly string logFilePath;

        // Event to notify when profile timer status changes
        public event Action<string, string> ProfileTimerUpdated;

        // Delegate to invoke the method that updates the last API call log
        public Action UpdateLastApiCallLogAction { get; set; }

        public ProfileTimerService()
        {
            profileTimers = new Dictionary<string, DispatcherTimer>();
            profileData = new Dictionary<string, Dictionary<string, object>>();
            profilesFolderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "DDNS_Cloudflare_API", "Profiles");
            logFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "DDNS_Cloudflare_API", "Logs.txt");
            LoadProfiles();
        }

        #region Profile Loading

        // Loads all profiles from the saved JSON files
        private void LoadProfiles()
        {
            if (Directory.Exists(profilesFolderPath))
            {
                var profileFiles = Directory.GetFiles(profilesFolderPath, "*.json");
                foreach (var file in profileFiles)
                {
                    string profileName = Path.GetFileNameWithoutExtension(file);
                    if (!profileData.ContainsKey(profileName))
                    {
                        var json = File.ReadAllText(file);
                        var profile = JsonSerializer.Deserialize<Dictionary<string, object>>(json);
                        profileData[profileName] = profile;
                    }
                }
            }
        }

        public Dictionary<string, Dictionary<string, object>> GetProfileData() => profileData;

        #endregion

        #region Timer Management

        // Starts a DNS update timer for a profile
        public async void StartTimer(string profileName, int intervalMinutes)
        {
            Debug.WriteLine($"Starting timer for {profileName}");

            // Stop any existing DNS update timer for this profile
            if (profileTimers.ContainsKey(profileName))
            {
                profileTimers[profileName].Stop();
                profileTimers.Remove(profileName);
            }

            // Stop any existing UI timer for this profile
            if (uiTimers.ContainsKey(profileName))
            {
                uiTimers[profileName].Stop();
                uiTimers.Remove(profileName);
            }

            // Start a new DNS update timer
            DispatcherTimer dnsTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMinutes(intervalMinutes)
            };

            dnsTimer.Tag = DateTime.Now;
            dnsTimer.Tick += async (sender, e) =>
            {
                dnsTimer.Tag = DateTime.Now;
                await UpdateDnsRecordsForProfile(profileName);
                UpdateLastApiCallLogAction?.Invoke();
                ProfileTimerUpdated?.Invoke(profileName, "Running");
            };

            dnsTimer.Start();
            profileTimers[profileName] = dnsTimer;

            // Start a new UI timer
            var uiTimer = CreateUiTimer(profileName, intervalMinutes);
            uiTimer.Start();
            uiTimers[profileName] = uiTimer;

            // Notify the UI about the timer status
            ProfileTimerUpdated?.Invoke(profileName, "Running");

            // Save the profile's timer status
            await SaveProfileStatusToSettings(profileName, true, intervalMinutes);
        }

        // Stops the timer for a profile
        public async void StopTimer(string profileName)
        {
            // Stop and remove the DNS update timer
            if (profileTimers.ContainsKey(profileName))
            {
                profileTimers[profileName].Stop();
                profileTimers.Remove(profileName);
            }

            // Stop and remove the UI timer
            if (uiTimers.ContainsKey(profileName))
            {
                uiTimers[profileName].Stop();
                uiTimers.Remove(profileName);
            }

            // Notify the UI that the profile's timer has been stopped
            ProfileTimerUpdated?.Invoke(profileName, "Stopped");

            // Save the profile's timer status
            await SaveProfileStatusToSettings(profileName, false, 0);
        }

        // Returns a dictionary of currently active profile timers
        public Dictionary<string, DispatcherTimer> GetProfileTimers() => profileTimers;

        #endregion

        #region UI Timer

        // Creates a UI timer that updates the remaining time for a profile
        private DispatcherTimer CreateUiTimer(string profileName, int intervalMinutes)
        {
            DispatcherTimer uiTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };

            uiTimer.Tick += (sender, e) =>
            {
                if (profileTimers.ContainsKey(profileName))
                {
                    var timer = profileTimers[profileName];
                    var lastRun = (DateTime)timer.Tag;
                    var remainingTime = TimeSpan.FromMinutes(intervalMinutes) - (DateTime.Now - lastRun);
                    RemainingTimeUpdated?.Invoke(this, (profileName, remainingTime));
                }
            };

            return uiTimer;
        }

        public event EventHandler<(string profileName, TimeSpan remainingTime)> RemainingTimeUpdated;

        #endregion

        #region DNS Records

        // Updates DNS records for a profile
        private async Task UpdateDnsRecordsForProfile(string profileName)
        {
            if (profileData.ContainsKey(profileName))
            {
                var profile = profileData[profileName];
                string apiKey = EncryptionHelper.DecryptString(profile["ApiKey"].ToString());
                string zoneId = EncryptionHelper.DecryptString(profile["ZoneId"].ToString());
                var dnsRecords = JsonSerializer.Deserialize<List<Dictionary<string, object>>>(profile["DnsRecords"].ToString());

                foreach (var record in dnsRecords)
                {
                    string dnsRecordId = record["RecordID"]?.ToString();
                    string mainDomain = profile["mainDomain"]?.ToString();
                    string name = record["Name"]?.ToString() + "." + mainDomain;
                    string content = await GetIpContent(new ComboBoxItem { Content = record["Content"]?.ToString() });

                    var recordData = new
                    {
                        content,
                        name,
                        proxied = bool.Parse(record["Proxied"]?.ToString()),
                        type = record["Type"]?.ToString(),
                        ttl = int.Parse(record["TTL"]?.ToString()),
                        comment = "DDNS updated from WPF"
                    };

                    await UpdateDnsRecordForProfile(apiKey, zoneId, recordData, dnsRecordId, profileName, mainDomain, content);
                }
            }
        }

        // Updates a specific DNS record for a profile
        public async Task<string> UpdateDnsRecordForProfile(string apiKey, string zoneId, object record, string dnsRecordId, string profileName, string domain, string ipAddress)
        {
            string json = JsonSerializer.Serialize(record);
            Debug.WriteLine($"Update Request for: {json}");

            using HttpClient client = new HttpClient
            {
                DefaultRequestHeaders =
                {
                    Accept = { new MediaTypeWithQualityHeaderValue("application/json") },
                    Authorization = new AuthenticationHeaderValue("Bearer", apiKey)
                }
            };

            HttpResponseMessage response = await client.PutAsync(
                $"https://api.cloudflare.com/client/v4/zones/{zoneId}/dns_records/{dnsRecordId}",
                new StringContent(json, Encoding.UTF8, "application/json"));

            string responseContent = await response.Content.ReadAsStringAsync();

            string logMessage = $"Profile: {profileName}, Domain: {domain}, IP: {ipAddress}, Response: {responseContent}";
            Log(logMessage);

            return responseContent;
        }

        #endregion

        #region IP Fetching

        // Fetches the public IP address based on the selected content (IPv4 or IPv6)
        public async Task<string> GetIpContent(ComboBoxItem selectedContent)
        {
            return selectedContent.Content.ToString() switch
            {
                "IPv4" => await GetWanIpv4(),
                "IPv6" => await GetWanIpv6(),
                _ => string.Empty
            };
        }

        private async Task<string> GetWanIpv4() => await FetchIpAddress("https://api.ipify.org?format=json");
        private async Task<string> GetWanIpv6() => await FetchIpAddress("https://api6.ipify.org?format=json");

        private async Task<string> FetchIpAddress(string url)
        {
            using HttpClient client = new HttpClient();
            var response = await client.GetStringAsync(url);
            var ipData = JsonSerializer.Deserialize<Dictionary<string, string>>(response);
            return ipData["ip"];
        }

        #endregion

        #region Profile Settings Management

        // Saves the status of the profile to the startup settings file
        private async Task SaveProfileStatusToSettings(string profileName, bool isRunning, int intervalMinutes)
        {
            var settingsFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "DDNS_Cloudflare_API", "startupSettings.json");

            Dictionary<string, object> startupSettings = new Dictionary<string, object>();
            if (File.Exists(settingsFilePath))
            {
                string json = await File.ReadAllTextAsync(settingsFilePath);
                startupSettings = JsonSerializer.Deserialize<Dictionary<string, object>>(json);
            }

            var profileSettings = new Dictionary<string, object>
            {
                { "IsRunning", isRunning },
                { "Interval", intervalMinutes }
            };

            startupSettings[profileName] = profileSettings;

            string updatedJson = JsonSerializer.Serialize(startupSettings, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(settingsFilePath, updatedJson);
            Debug.WriteLine($"Saved profile {profileName} with status {(isRunning ? "Running" : "Stopped")} and interval {intervalMinutes} minutes.");
        }

        // Loads the startup settings and starts timers if necessary
        public async Task LoadStartupSettings(string settingsFilePath)
        {
            if (File.Exists(settingsFilePath))
            {
                string json = await File.ReadAllTextAsync(settingsFilePath);
                var startupSettings = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json);

                if (startupSettings.ContainsKey("LoadProfilesOnStartup") && startupSettings["LoadProfilesOnStartup"].GetBoolean())
                {
                    foreach (var kvp in startupSettings)
                    {
                        if (kvp.Key == "RunOnStartup" || kvp.Key == "LoadProfilesOnStartup")
                            continue;

                        string profileName = kvp.Key;

                        // Load the profile settings (status and interval)
                        var profileSettings = kvp.Value;
                        bool wasRunning = profileSettings.GetProperty("IsRunning").GetBoolean();
                        int interval = profileSettings.GetProperty("Interval").GetInt32();  // Load the saved interval

                        if (wasRunning && !GetProfileTimers().ContainsKey(profileName))
                        {
                            // Start the timer with the loaded interval
                            StartTimer(profileName, interval);
                            Debug.WriteLine($"Starting profile {profileName} on startup with interval {interval} minutes.");
                        }
                    }
                }
            }
        }

        #endregion

        #region Timer Status

        // Checks if a profile's timer is currently running
        public bool IsProfileTimerRunning(string profileName)
        {
            return profileTimers.ContainsKey(profileName) && profileTimers[profileName].IsEnabled;
        }

        #endregion


        #region Logging

        // Logs any message to the log file
        private void Log(string message)
        {
            try
            {
                using (StreamWriter writer = new StreamWriter(logFilePath, true) { AutoFlush = true })
                {
                    writer.WriteLine($"{DateTime.Now}: {message}");
                }
            }
            catch (Exception ex)
            {
                // Handle logging exceptions, if needed
                Debug.WriteLine($"Error writing log: {ex.Message}");
            }
        }

        #endregion
    }
}
