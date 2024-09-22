using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Threading;
using Microsoft.Extensions.Logging;

namespace DDNS_Cloudflare_API.Services
{
    public class ProfileTimerService
    {
        private readonly Dictionary<string, DispatcherTimer> profileTimers;
        private readonly Dictionary<string, Dictionary<string, object>> profileData;
        private readonly string profilesFolderPath;
        private readonly string logFilePath;

        // Define the event to notify status updates
        public event Action<string, string> ProfileTimerUpdated;

        public Dictionary<string, DispatcherTimer> GetProfileTimers() => profileTimers;

        public Dictionary<string, Dictionary<string, object>> GetProfileData() => profileData;

        public ProfileTimerService()
        {
            profileTimers = new Dictionary<string, DispatcherTimer>();
            profileData = new Dictionary<string, Dictionary<string, object>>();
            profilesFolderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "DDNS_Cloudflare_API", "Profiles");
            logFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "DDNS_Cloudflare_API", "Logs.txt");

            // Initialize data
            LoadProfiles();

        }

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

        public async void StartTimer(string profileName, int intervalMinutes)
        {
            Debug.WriteLine($"Starting timer for {profileName}");

            if (profileTimers.ContainsKey(profileName))
            {
                profileTimers[profileName].Stop();
                profileTimers.Remove(profileName);
            }

            DispatcherTimer dnsTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMinutes(intervalMinutes)
            };

            dnsTimer.Tag = DateTime.Now;
            dnsTimer.Tick += async (sender, e) =>
            {
                dnsTimer.Tag = DateTime.Now;
                await UpdateDnsRecordsForProfile(profileName);

                ProfileTimerUpdated?.Invoke(profileName, "Running");
            };
            dnsTimer.Start();
            profileTimers[profileName] = dnsTimer;

            // Start the UI update timer
            var uiTimer = CreateUiTimer(profileName, intervalMinutes);
            uiTimer.Start();

            ProfileTimerUpdated?.Invoke(profileName, "Running");
            await SaveProfileStatusToSettings(profileName, true, intervalMinutes);
        }



        public async void StopTimer(string profileName)
        {
            if (profileTimers.ContainsKey(profileName))
            {
                profileTimers[profileName].Stop();
                profileTimers.Remove(profileName);

                // Trigger the ProfileTimerUpdated event with "Stopped" status
                ProfileTimerUpdated?.Invoke(profileName, "Stopped");

                // Save the status when the timer stops
                await SaveProfileStatusToSettings(profileName, false, 0);  // Stop and reset the interval
            }
        }

        // Declare a new event to notify remaining time updates
        public event EventHandler<(string profileName, TimeSpan remainingTime)> RemainingTimeUpdated;

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

                    // Fire event to notify the remaining time update
                    RemainingTimeUpdated?.Invoke(this, (profileName, remainingTime));
                }
            };

            return uiTimer;
        }


        private async Task SaveProfileStatusToSettings(string profileName, bool isRunning, int intervalMinutes)
        {

            var settingsFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "DDNS_Cloudflare_API", "startupSettings.json");

            // Load existing settings
            Dictionary<string, object> startupSettings = new Dictionary<string, object>();

            if (File.Exists(settingsFilePath))
            {
                string json = await File.ReadAllTextAsync(settingsFilePath);
                startupSettings = JsonSerializer.Deserialize<Dictionary<string, object>>(json);
            }

            // Save the profile's status and interval
            var profileSettings = new Dictionary<string, object>
    {
        { "IsRunning", isRunning },
        { "Interval", intervalMinutes }  // Save the interval here
    };

            startupSettings[profileName] = profileSettings;

            // Serialize and save the updated settings back to the file
            string updatedJson = JsonSerializer.Serialize(startupSettings, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(settingsFilePath, updatedJson);

            Debug.WriteLine($"Saved profile {profileName} with status {(isRunning ? "Running" : "Stopped")} and interval {intervalMinutes} minutes.");
        }




        public async Task<string> UpdateDnsRecordForProfile(string apiKey, string zoneId, object record, string dnsRecordId)
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
            string logMessage = $"Last update: {DateTime.Now} Response: {responseContent}";

            // Log the response
            Log(logMessage);

            return responseContent;  // Return the response content
        }


        public async Task<string> GetIpContent(ComboBoxItem selectedContent)
        {
            string ipContent = selectedContent.Content.ToString() switch
            {
                "IPv4" => await GetWanIpv4(),
                "IPv6" => await GetWanIpv6(),
                _ => string.Empty
            };

            // Log fetched IP
            Log($"Fetched IP: {ipContent}");

            return ipContent;
        }

        private async Task<string> GetWanIpv4()
        {
            return await FetchIpAddress("https://api.ipify.org?format=json");
        }

        private async Task<string> GetWanIpv6()
        {
            return await FetchIpAddress("https://api6.ipify.org?format=json");
        }

        private async Task<string> FetchIpAddress(string url)
        {
            using HttpClient client = new HttpClient();
            var response = await client.GetStringAsync(url);
            var ipData = JsonSerializer.Deserialize<Dictionary<string, string>>(response);
            return ipData["ip"];
        }

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
string mainDomain = profile["mainDomain"]?.ToString();  // Fetch the main domain from the profile
string name = record["Name"]?.ToString() + "." + mainDomain;  // Concatenate Name with Domain
                    string content = record["Content"]?.ToString();
                    string type = record["Type"]?.ToString();
                    bool proxied = bool.Parse(record["Proxied"]?.ToString());
                    int ttl = int.Parse(record["TTL"]?.ToString());

                    var recordData = new
                    {
                        content = await GetIpContent(new ComboBoxItem { Content = content }),
                        name,
                        proxied,
                        type,
                        ttl,
                        comment = "DDNS updated from WPF"
                    };

                    await UpdateDnsRecordForProfile(apiKey, zoneId, recordData, dnsRecordId);
                }
            }
        }


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
                Console.WriteLine($"Error writing log: {ex.Message}");
            }
        }
    }
}