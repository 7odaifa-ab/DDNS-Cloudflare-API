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

        private async Task UpdateDnsRecord()
{
    string wanIp = await GetWanIp();
    var record = new
    {
        content = wanIp,
        name = txtName.Text,
        proxied =false, // Adjust if using a different control
        type = ((ComboBoxItem)cmbType.SelectedItem).Content.ToString(),
        ttl = int.Parse(txtTtl.Text),
        comment = "DDNS updated" // You can customize this comment if needed
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
