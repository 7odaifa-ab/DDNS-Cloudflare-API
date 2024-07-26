using DDNS_Cloudflare_API.ViewModels.Pages;
using System.Net.Http.Headers;
using System.Net.Http;
using System.Text.Json;
using System.Text;
using System.Windows.Controls;
using System.Threading.Tasks;
using System.Windows;
using System;
using System.Windows.Threading;
using Wpf.Ui.Controls;

namespace DDNS_Cloudflare_API.Views.Pages
{
    public partial class DashboardPage : INavigableView<DashboardViewModel>
    {
        public DashboardViewModel ViewModel { get; }

        private DispatcherTimer timer; // Class-level variable

        public DashboardPage(DashboardViewModel viewModel)
        {
            ViewModel = viewModel;
            DataContext = this;

            InitializeComponent();
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
                case 0: return 1;
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
                proxied = false, // Adjust if using a different control
                type = ((ComboBoxItem)cmbType.SelectedItem).Content.ToString(),
                ttl = GetTtlInSeconds(),
                comment = "DDNS updated from WPF" // You can customize this comment if needed
            };

            string json = JsonSerializer.Serialize(record);
            using HttpClient client = new HttpClient();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", txtApiKey.Text); // Bearer token

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
    }
}
