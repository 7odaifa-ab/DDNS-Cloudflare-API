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


            // Subscribe the event handler to fire regardless of UI initialization
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

        public void StartTimer(string profileName, int intervalMinutes)
        {
            Debug.WriteLine($"Starting timer for {profileName}");

            if (profileTimers.ContainsKey(profileName))
            {
                profileTimers[profileName].Stop();
                profileTimers.Remove(profileName);
            }

            DispatcherTimer timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMinutes(intervalMinutes)
            };

            timer.Tag = DateTime.Now;
            timer.Tick += async (sender, e) =>
            {
                timer.Tag = DateTime.Now;
                await UpdateDnsRecordsForProfile(profileName);

                // Trigger the ProfileTimerUpdated event with real status
                ProfileTimerUpdated?.Invoke(profileName, "Running");
            };

            timer.Start();
            profileTimers[profileName] = timer;

            // Immediately update the profile status
            ProfileTimerUpdated?.Invoke(profileName, "Running");
        }


        public void StopTimer(string profileName)
        {
            if (profileTimers.ContainsKey(profileName))
            {
                profileTimers[profileName].Stop();
                profileTimers.Remove(profileName);

                // Trigger the ProfileTimerUpdated event with "Stopped" status
                ProfileTimerUpdated?.Invoke(profileName, "Stopped");
            }
        }



        public async Task UpdateDnsRecordForProfile(string apiKey, string zoneId, object record, string dnsRecordId)
        {
            string json = JsonSerializer.Serialize(record);

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
                    string name = record["Name"]?.ToString();
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