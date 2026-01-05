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
            try
            {
                if (Directory.Exists(profilesFolderPath))
                {
                    var profileFiles = Directory.GetFiles(profilesFolderPath, "*.json");
                    foreach (var file in profileFiles)
                    {
                        try
                        {
                            string profileName = Path.GetFileNameWithoutExtension(file);
                            if (!profileData.ContainsKey(profileName))
                            {
                                var json = File.ReadAllText(file);
                                var profile = JsonSerializer.Deserialize<Dictionary<string, object>>(json);
                                
                                if (profile != null)
                                {
                                    profileData[profileName] = profile;
                                }
                                else
                                {
                                    Debug.WriteLine($"Failed to deserialize profile: {profileName}");
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine($"Error loading profile from {file}: {ex.Message}");
                        }
                    }
                }
                else
                {
                    Debug.WriteLine($"Profiles folder does not exist: {profilesFolderPath}");
                    Directory.CreateDirectory(profilesFolderPath);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error loading profiles: {ex.Message}");
            }
        }

        public Dictionary<string, Dictionary<string, object>> GetProfileData() => profileData;

        #endregion

        #region Timer Management

        // Starts a DNS update timer for a profile
        public async void StartTimer(string profileName, int intervalMinutes)
        {
            try
            {
                if (string.IsNullOrEmpty(profileName))
                {
                    Debug.WriteLine("Cannot start timer: profile name is null or empty");
                    return;
                }

                if (intervalMinutes <= 0)
                {
                    Debug.WriteLine($"Invalid interval for {profileName}: {intervalMinutes}");
                    return;
                }

                Debug.WriteLine($"Starting timer for {profileName} with interval {intervalMinutes} minutes");

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
                    try
                    {
                        dnsTimer.Tag = DateTime.Now;
                        await UpdateDnsRecordsForProfile(profileName);
                        UpdateLastApiCallLogAction?.Invoke();
                        ProfileTimerUpdated?.Invoke(profileName, "Running");
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Error in timer tick for {profileName}: {ex.Message}");
                        Log($"Timer error for {profileName}: {ex.Message}");
                    }
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
            catch (Exception ex)
            {
                Debug.WriteLine($"Error starting timer for {profileName}: {ex.Message}");
                Log($"Failed to start timer for {profileName}: {ex.Message}");
            }
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
            try
            {
                if (!profileData.ContainsKey(profileName))
                {
                    Debug.WriteLine($"Profile not found: {profileName}");
                    return;
                }

                var profile = profileData[profileName];
                
                if (!profile.ContainsKey("ApiKey") || !profile.ContainsKey("ZoneId") || !profile.ContainsKey("DnsRecords"))
                {
                    Debug.WriteLine($"Profile {profileName} is missing required fields");
                    return;
                }

                string apiKey = EncryptionHelper.DecryptString(profile["ApiKey"].ToString());
                string zoneId = EncryptionHelper.DecryptString(profile["ZoneId"].ToString());
                var dnsRecords = JsonSerializer.Deserialize<List<Dictionary<string, object>>>(profile["DnsRecords"].ToString());

                if (dnsRecords == null || dnsRecords.Count == 0)
                {
                    Debug.WriteLine($"No DNS records found for profile: {profileName}");
                    return;
                }

                foreach (var record in dnsRecords)
                {
                    try
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
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Error updating DNS record for {profileName}: {ex.Message}");
                        Log($"Error updating DNS record for {profileName}: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error updating DNS records for profile {profileName}: {ex.Message}");
                Log($"Error updating DNS records for profile {profileName}: {ex.Message}");
            }
        }

        // Updates a specific DNS record for a profile
        public async Task<string> UpdateDnsRecordForProfile(string apiKey, string zoneId, object record, string dnsRecordId, string profileName, string domain, string ipAddress)
        {
            try
            {
                string json = JsonSerializer.Serialize(record);
                Debug.WriteLine($"Update Request for: {json}");

                using HttpClient client = new HttpClient
                {
                    Timeout = TimeSpan.FromSeconds(30),
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

                string logMessage = $"Profile: {profileName}, Domain: {domain}, IP: {ipAddress}, Status: {response.StatusCode}, Response: {responseContent}";
                Log(logMessage);

                if (!response.IsSuccessStatusCode)
                {
                    Debug.WriteLine($"DNS update failed for {profileName}: {response.StatusCode} - {responseContent}");
                }

                return responseContent;
            }
            catch (Exception ex)
            {
                string errorMessage = $"Profile: {profileName}, Domain: {domain}, IP: {ipAddress}, Error: {ex.Message}";
                Log(errorMessage);
                Debug.WriteLine($"Exception updating DNS record for {profileName}: {ex.Message}");
                return $"{{\"success\":false,\"error\":\"{ex.Message}\"}}";
            }
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
            try
            {
                using HttpClient client = new HttpClient();
                client.Timeout = TimeSpan.FromSeconds(10);
                var response = await client.GetStringAsync(url);
                var ipData = JsonSerializer.Deserialize<Dictionary<string, string>>(response);
                
                if (ipData != null && ipData.ContainsKey("ip"))
                {
                    return ipData["ip"];
                }
                
                Debug.WriteLine($"Invalid response from IP service: {url}");
                return string.Empty;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error fetching IP from {url}: {ex.Message}");
                return string.Empty;
            }
        }

        #endregion

        #region Profile Settings Management

        // Saves the status of the profile to the startup settings file
        private async Task SaveProfileStatusToSettings(string profileName, bool isRunning, int intervalMinutes)
        {
            try
            {
                var settingsDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "DDNS_Cloudflare_API");
                var settingsFilePath = Path.Combine(settingsDirectory, "startupSettings.json");

                // Ensure directory exists
                Directory.CreateDirectory(settingsDirectory);

                Dictionary<string, object> startupSettings = new Dictionary<string, object>();
                if (File.Exists(settingsFilePath))
                {
                    try
                    {
                        string json = await File.ReadAllTextAsync(settingsFilePath);
                        startupSettings = JsonSerializer.Deserialize<Dictionary<string, object>>(json) ?? new Dictionary<string, object>();
                    }
                    catch (JsonException ex)
                    {
                        Debug.WriteLine($"Error deserializing settings, creating new: {ex.Message}");
                        startupSettings = new Dictionary<string, object>();
                    }
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
            catch (Exception ex)
            {
                Debug.WriteLine($"Error saving profile status for {profileName}: {ex.Message}");
            }
        }

        // Loads the startup settings and starts timers if necessary
        public async Task LoadStartupSettings(string settingsFilePath)
        {
            try
            {
                if (!File.Exists(settingsFilePath))
                {
                    Debug.WriteLine($"Startup settings file not found: {settingsFilePath}");
                    return;
                }

                string json = await File.ReadAllTextAsync(settingsFilePath);
                var startupSettings = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json);

                if (startupSettings == null)
                {
                    Debug.WriteLine("Failed to deserialize startup settings");
                    return;
                }

                if (startupSettings.ContainsKey("LoadProfilesOnStartup") && startupSettings["LoadProfilesOnStartup"].GetBoolean())
                {
                    foreach (var kvp in startupSettings)
                    {
                        if (kvp.Key == "RunOnStartup" || kvp.Key == "LoadProfilesOnStartup")
                            continue;

                        try
                        {
                            string profileName = kvp.Key;

                            // Load the profile settings (status and interval)
                            var profileSettings = kvp.Value;
                            
                            if (profileSettings.ValueKind != JsonValueKind.Object)
                                continue;

                            bool wasRunning = profileSettings.GetProperty("IsRunning").GetBoolean();
                            int interval = profileSettings.GetProperty("Interval").GetInt32();

                            if (wasRunning && !GetProfileTimers().ContainsKey(profileName))
                            {
                                // Start the timer with the loaded interval
                                StartTimer(profileName, interval);
                                Debug.WriteLine($"Starting profile {profileName} on startup with interval {interval} minutes.");
                            }
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine($"Error loading startup settings for profile {kvp.Key}: {ex.Message}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error loading startup settings: {ex.Message}");
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
                // Ensure log directory exists
                var logDirectory = Path.GetDirectoryName(logFilePath);
                if (!string.IsNullOrEmpty(logDirectory))
                {
                    Directory.CreateDirectory(logDirectory);
                }

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
