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

namespace DDNS_Cloudflare_API.Services
{
    public class ProfileTimerService
    {
        private readonly Dictionary<string, DispatcherTimer> profileTimers;
        private readonly Dictionary<string, Dictionary<string, object>> profileData;
        private readonly string profilesFolderPath;
        private readonly string logFilePath;

        // Define the event to notify status updates
        public event Action<string> StatusUpdated;

        public ProfileTimerService()
        {
            profileTimers = new Dictionary<string, DispatcherTimer>();
            profileData = new Dictionary<string, Dictionary<string, object>>();
            profilesFolderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "DDNS_Cloudflare_API", "Profiles");
            logFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "DDNS_Cloudflare_API", "Logs.txt");

            // Initialize data
            LoadProfiles();
        }

        public Dictionary<string, DispatcherTimer> GetProfileTimers() => profileTimers;

        public Dictionary<string, Dictionary<string, object>> GetProfileData() => profileData;

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
            // Stop and remove the old timer if it exists
            if (profileTimers.ContainsKey(profileName))
            {
                profileTimers[profileName].Stop();
                profileTimers.Remove(profileName);
            }

            if (!profileData.ContainsKey(profileName))
                return;

            // Create and start a new timer
            DispatcherTimer timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMinutes(intervalMinutes)
            };

            // Set the initial run time
            timer.Tag = DateTime.Now;

            // Update the timer's Tick event
            timer.Tick += async (sender, e) =>
            {
                timer.Tag = DateTime.Now; // Update the last run time
                await UpdateDnsRecordsForProfile(profileName);
            };

            timer.Start();
            profileTimers[profileName] = timer; // Store the timer

            // Trigger an immediate update
            Task.Run(async () => await UpdateDnsRecordsForProfile(profileName));
        }

        public void StopTimer(string profileName)
        {
            if (profileTimers.ContainsKey(profileName))
            {
                profileTimers[profileName].Stop();
                profileTimers.Remove(profileName);
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
            string logMessage = $"Last update: {DateTime.Now}\nResponse: {responseContent}";

            // Raise the event to update status in UI
            StatusUpdated?.Invoke(logMessage);

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

            // Raise the event to update status in UI
            StatusUpdated?.Invoke($"Fetched IP: {ipContent}");

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
                // Use StreamWriter with AutoFlush to ensure data is saved immediately
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
