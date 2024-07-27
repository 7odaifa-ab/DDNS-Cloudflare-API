using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Controls;
using System.Windows;
using System.Windows.Threading;
using System.Text.Json;
using System.Threading.Tasks;
using System.Net.Http.Headers;
using System.Net.Http;
using System.Text;
using Wpf.Ui.Controls;
using DDNS_Cloudflare_API.ViewModels.Pages;

namespace DDNS_Cloudflare_API.Views.Pages
{
    public partial class DashboardPage : INavigableView<DashboardViewModel>
    {
        public DashboardViewModel ViewModel { get; }

        private DispatcherTimer timer; // Class-level variable
        private string profilesFolderPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "DDNS_Cloudflare_API", "Profiles");

        public DashboardPage(DashboardViewModel viewModel)
        {
            ViewModel = viewModel;
            DataContext = this;

            InitializeComponent();
            Directory.CreateDirectory(profilesFolderPath); // Ensure the profiles folder exists
            LoadProfiles();
        }

        private async void BtnStart_Click(object sender, RoutedEventArgs e)
        {
            int interval = GetInterval();
            timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMinutes(interval)
            };
            timer.Tick += async (s, args) => await UpdateDnsRecord();
            timer.Start();
            txtStatus.Text = "Started";
            await UpdateDnsRecord(); // Immediate update
        }

        private void BtnStop_Click(object sender, RoutedEventArgs e)
        {
            if (timer != null)
            {
                timer.Stop();
                txtStatus.Text = "Stopped";
            }
        }

        private async void BtnOneTime_Click(object sender, RoutedEventArgs e)
        {
            await UpdateDnsRecord(); // Perform a one-time update
        }

        private int GetInterval()
        {
            switch (cmbInterval.SelectedIndex)
            {
                case 0: return 15;
                case 1: return 30;
                case 2: return 60;
                case 3: return 360;
                case 4: return 720;
                case 5: return 1440;
                default: return 60;
            }
        }

        private int GetTtlInSeconds()
        {
            switch (cmbTtl.SelectedIndex)
            {
                case 0: return 1; // Auto
                case 1: return 60; // 1 min
                case 2: return 120; // 2 min
                case 3: return 300; // 5 min
                case 4: return 600; // 10 min
                case 5: return 900; // 15 min
                case 6: return 1800; // 30 min
                case 7: return 3600; // 1 hr
                case 8: return 7200; // 2 hr
                case 9: return 18000; // 5 hr
                case 10: return 43200; // 12 hr
                case 11: return 86400; // 1 day
                default: return 3600; // Default to 1 hour
            }
        }

        private async Task UpdateDnsRecord()
        {
            string wanIp = await GetWanIp();
            var record = new
            {
                content = wanIp,
                name = txtName.Text,
                proxied = ((ComboBoxItem)cmbProxied.SelectedItem).Content.ToString() == "True",
                type = ((ComboBoxItem)cmbType.SelectedItem).Content.ToString(),
                ttl = GetTtlInSeconds(),
                comment = "DDNS updated from WPF" // You can customize this comment if needed
            };

            string json = JsonSerializer.Serialize(record);
            using HttpClient client = new HttpClient();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", txtApiKey.Text); // Decrypt API key

            HttpResponseMessage response = await client.PutAsync(
                $"https://api.cloudflare.com/client/v4/zones/{txtZoneId.Text}/dns_records/{txtDnsRecordId.Text}",
                new StringContent(json, Encoding.UTF8, "application/json"));

            string result = await response.Content.ReadAsStringAsync();
            txtStatus.Text = $"Last update: {DateTime.Now}\nResponse: {result}";
        }

        private async Task<string> GetWanIp()
        {
            using HttpClient client = new HttpClient();
            return await client.GetStringAsync("https://api.ipify.org");
        }

        private void BtnSaveProfile_Click(object sender, RoutedEventArgs e)
        {
            string profileName = $"{txtName.Text}_{((ComboBoxItem)cmbType.SelectedItem).Content}";
            string filePath = Path.Combine(profilesFolderPath, $"{profileName}.json");

            var profileData = new
            {
                ApiKey = EncryptionHelper.EncryptString(txtApiKey.Text), // Encrypt API key
                ZoneId = txtZoneId.Text,
                DnsRecordId = txtDnsRecordId.Text,
                Name = txtName.Text,
                Type = ((ComboBoxItem)cmbType.SelectedItem).Content.ToString(),
                Proxied = ((ComboBoxItem)cmbProxied.SelectedItem).Content.ToString() == "True",
                Ttl = GetTtlInSeconds(),
                Interval = GetInterval()
            };

            string json = JsonSerializer.Serialize(profileData);
            File.WriteAllText(filePath, json);

            LoadProfiles();
        }

        private void BtnDeleteProfile_Click(object sender, RoutedEventArgs e)
        {
            if (cmbProfiles.SelectedItem != null)
            {
                string profileName = (string)((ComboBoxItem)cmbProfiles.SelectedItem).Content;
                string filePath = Path.Combine(profilesFolderPath, $"{profileName}.json");

                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                    LoadProfiles();
                }
            }
        }

        private void LoadProfiles()
        {
            cmbProfiles.Items.Clear();
            var files = Directory.GetFiles(profilesFolderPath, "*.json");

            if (files.Length > 0)
            {
                foreach (var file in files)
                {
                    string fileName = Path.GetFileNameWithoutExtension(file);
                    cmbProfiles.Items.Add(new ComboBoxItem { Content = fileName });
                }
                cmbProfiles.IsEnabled = true;
            }
            else
            {
                cmbProfiles.IsEnabled = false;
            }
        }

        private void CmbProfiles_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cmbProfiles.SelectedItem != null)
            {
                string profileName = (string)((ComboBoxItem)cmbProfiles.SelectedItem).Content;
                string filePath = Path.Combine(profilesFolderPath, $"{profileName}.json");

                if (File.Exists(filePath))
                {
                    var json = File.ReadAllText(filePath);
                    var profileData = JsonSerializer.Deserialize<ProfileData>(json);

                    txtApiKey.Text = EncryptionHelper.DecryptString(profileData.ApiKey); // Decrypt API key
                    txtZoneId.Text = profileData.ZoneId;
                    txtDnsRecordId.Text = profileData.DnsRecordId;
                    txtName.Text = profileData.Name;
                    cmbType.SelectedItem = cmbType.Items.OfType<ComboBoxItem>().FirstOrDefault(item => item.Content.ToString() == profileData.Type);
                    cmbProxied.SelectedItem = profileData.Proxied ? cmbProxied.Items.OfType<ComboBoxItem>().FirstOrDefault(item => item.Content.ToString() == "True") : cmbProxied.Items.OfType<ComboBoxItem>().FirstOrDefault(item => item.Content.ToString() == "False");
                    cmbTtl.SelectedIndex = GetTtlIndex(profileData.Ttl);
                    cmbInterval.SelectedIndex = GetIntervalIndex(profileData.Interval);
                }
            }
        }

        private int GetTtlIndex(int ttl)
        {
            switch (ttl)
            {
                case 1: return 0; // Auto
                case 60: return 1; // 1 min
                case 120: return 2; // 2 min
                case 300: return 3; // 5 min
                case 600: return 4; // 10 min
                case 900: return 5; // 15 min
                case 1800: return 6; // 30 min
                case 3600: return 7; // 1 hr
                case 7200: return 8; // 2 hr
                case 18000: return 9; // 5 hr
                case 43200: return 10; // 12 hr
                case 86400: return 11; // 1 day
                default: return 7; // Default to 1 hour
            }
        }

        private int GetIntervalIndex(int interval)
        {
            switch (interval)
            {
                case 15: return 0;
                case 30: return 1;
                case 60: return 2;
                case 360: return 3;
                case 720: return 4;
                case 1440: return 5;
                default: return 2; // Default to 1 hour
            }
        }

        private class ProfileData
        {
            public string ApiKey { get; set; }
            public string ZoneId { get; set; }
            public string DnsRecordId { get; set; }
            public string Name { get; set; }
            public string Type { get; set; }
            public bool Proxied { get; set; }
            public int Ttl { get; set; }
            public int Interval { get; set; }
        }
    }
}
