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
using TextBox = Wpf.Ui.Controls.TextBox;
using Orientation = System.Windows.Controls.Orientation;
using ComboBox = System.Windows.Controls.ComboBox;
using MessageBox = System.Windows.MessageBox;
using MessageBoxButton = Wpf.Ui.Controls.MessageBoxButton;
using MessageBoxResult = Wpf.Ui.Controls.MessageBoxResult;

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
            timer.Tick += async (s, args) => await UpdateDnsRecords();
            timer.Start();
            txtStatus.Text = "Started";
            await UpdateDnsRecords(); // Immediate update
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
            await UpdateDnsRecords(); // Perform a one-time update
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

        private int GetTtlInSeconds(int index)
        {
            switch (index)
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

        private async Task UpdateDnsRecords()
        {
            foreach (var item in itemsControlDnsRecords.Items)
            {
                var dnsRecordPanel = item as StackPanel;

                var txtDnsRecordId = dnsRecordPanel.Children.OfType<TextBox>().FirstOrDefault(t => t.Name == "txtDnsRecordId");
                var txtName = dnsRecordPanel.Children.OfType<TextBox>().FirstOrDefault(t => t.Name == "txtName");
                var contentComboBox = dnsRecordPanel.Children.OfType<System.Windows.Controls.ComboBox>().FirstOrDefault(c => c.Name == "content");
                var cmbType = dnsRecordPanel.Children.OfType<System.Windows.Controls.ComboBox>().FirstOrDefault(c => c.Name == "cmbType");
                var cmbProxied = dnsRecordPanel.Children.OfType<System.Windows.Controls.ComboBox>().FirstOrDefault(c => c.Name == "cmbProxied");
                var cmbTtl = dnsRecordPanel.Children.OfType<System.Windows.Controls.ComboBox>().FirstOrDefault(c => c.Name == "cmbTtl");

                if (txtDnsRecordId != null && txtName != null && cmbType != null && cmbProxied != null && cmbTtl != null && contentComboBox != null && contentComboBox.SelectedItem != null)
                {

                    string content = "IPv4";

                    // Determine the IP fetching method based on the selected value in the content ComboBox
                    var selectedContent = ((ComboBoxItem)contentComboBox.SelectedItem).Content.ToString();
                    if (selectedContent == "IPv4")
                    {
                        content = await GetWanIpv4();
                    }
                    else if (selectedContent == "IPv6")
                    {
                        content = await GetWanIpv6();
                    }

                    var record = new
                    {
                        content = content,
                        name = txtName.Text,
                        proxied = ((ComboBoxItem)cmbProxied.SelectedItem).Content.ToString() == "True",
                        type = ((ComboBoxItem)cmbType.SelectedItem).Content.ToString(),
                        ttl = GetTtlInSeconds(cmbTtl.SelectedIndex),
                        comment = "DDNS updated from WPF"
                    };

                    await UpdateDnsRecord(record, txtDnsRecordId.Text);

                }
                else
                {
                    var uiMessageBox = new Wpf.Ui.Controls.MessageBox
                    {
                        Title = "Erorr",
                        Content =
        "all parameter for API Call are not complate",
                    };

                    _ = await uiMessageBox.ShowDialogAsync();
                }
            }
        }

        private async Task UpdateDnsRecord(object record, string dnsRecordId)
        {
            string json = JsonSerializer.Serialize(record);
            using HttpClient client = new HttpClient();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", txtApiKey.Text);

            HttpResponseMessage response = await client.PutAsync(
                $"https://api.cloudflare.com/client/v4/zones/{txtZoneId.Text}/dns_records/{dnsRecordId}",
                new StringContent(json, Encoding.UTF8, "application/json"));

            string result = await response.Content.ReadAsStringAsync();
            txtStatus.Text = $"Last update: {DateTime.Now}\nResponse: {result}";
        }

        private async Task<string> GetWanIpv4()
        {
            using HttpClient client = new HttpClient();
            var response = await client.GetStringAsync("https://api.ipify.org?format=json");
            var ipData = JsonSerializer.Deserialize<Dictionary<string, string>>(response);
            return ipData["ip"];
        }
        private async Task<string> GetWanIpv6()
        {
            using HttpClient client = new HttpClient();
            var response = await client.GetStringAsync("https://api6.ipify.org?format=json");
            var ipData = JsonSerializer.Deserialize<Dictionary<string, string>>(response);
            return ipData["ip"];
        }

        private void BtnAddDnsRecord_Click(object sender, RoutedEventArgs e)
        {
            var dnsRecordPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(0, 0, 0, 10)
            };

            // Create and add new fields
            dnsRecordPanel.Children.Add(CreateTextBox("txtDnsRecordId"));
            dnsRecordPanel.Children.Add(CreateTextBox("txtName"));
            dnsRecordPanel.Children.Add(CreateComboBox("content", new[] { "IPv4", "IPv6" }));
            dnsRecordPanel.Children.Add(CreateComboBox("cmbType", new[] { "A", "AAAA", "CNAME" }));
            dnsRecordPanel.Children.Add(CreateComboBox("cmbProxied", new[] { "True", "False" }));
            dnsRecordPanel.Children.Add(CreateComboBox("cmbTtl", new[]
            {
                "Auto", "1 min", "2 min", "5 min", "10 min", "15 min", "30 min", "1 hr",
                "2 hr", "5 hr", "12 hr", "1 day"
            }));

            itemsControlDnsRecords.Items.Add(dnsRecordPanel);
        }

        private TextBox CreateTextBox(string name, string text = "")
        {
            return new TextBox
            {
                Name = name,
                Margin = new Thickness(5),
                Width = 100,
                Text = text
            };
        }


        private ComboBox CreateComboBox(string name, string[] items, string selectedItem = null)
        {
            var comboBox = new ComboBox { Name = name, Margin = new Thickness(5) };
            foreach (var item in items)
            {
                comboBox.Items.Add(new ComboBoxItem { Content = item });
            }
            if (selectedItem != null)
            {
                // Select the item that matches the provided selectedItem
                comboBox.SelectedItem = comboBox.Items.OfType<ComboBoxItem>().FirstOrDefault(item => item.Content.ToString() == selectedItem);
            }
            return comboBox;
        }


        private void LoadProfiles()
        {
            if (Directory.Exists(profilesFolderPath))
            {
                var profileFiles = Directory.GetFiles(profilesFolderPath, "*.json");
                cmbProfiles.Items.Clear();
                foreach (var file in profileFiles)
                {
                    var profileName = Path.GetFileNameWithoutExtension(file);
                    cmbProfiles.Items.Add(profileName);
                }
                cmbProfiles.IsEnabled = cmbProfiles.Items.Count > 0;
            }
        }
        private static int GetTtlIndex(int ttlInSeconds)
        {
            switch (ttlInSeconds)
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
                default: return 7; // Default to 1 hr
            }
        }


        private void CmbProfiles_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cmbProfiles.SelectedItem is string profileName)
            {
                var profilePath = Path.Combine(profilesFolderPath, $"{profileName}.json");
                if (File.Exists(profilePath))
                {
                    var json = File.ReadAllText(profilePath);
                    var profile = JsonSerializer.Deserialize<Dictionary<string, object>>(json);

                    // Decrypt and set API Key and Zone ID
                    txtApiKey.Text = EncryptionHelper.DecryptString(profile["ApiKey"].ToString());
                    txtZoneId.Text = EncryptionHelper.DecryptString(profile["ZoneId"].ToString());

                    // Clear existing records
                    itemsControlDnsRecords.Items.Clear();

                    // Load DNS records
                    var dnsRecords = JsonSerializer.Deserialize<List<Dictionary<string, object>>>(profile["DnsRecords"].ToString());
                    foreach (var record in dnsRecords)
                    {
                        var dnsRecordPanel = new StackPanel
                        {
                            Orientation = Orientation.Horizontal,
                            Margin = new Thickness(0, 0, 0, 10)
                        };

                        // Create and add new fields with values from profile
                        dnsRecordPanel.Children.Add(CreateTextBox("txtDnsRecordId", record["RecordID"]?.ToString()));
                        dnsRecordPanel.Children.Add(CreateTextBox("txtName", record["Name"]?.ToString()));

                        var content = record["Content"]?.ToString();
                        dnsRecordPanel.Children.Add(CreateComboBox("content", new[] { "IPv4", "IPv6" }, content));

                        var type = record["Type"]?.ToString();
                        dnsRecordPanel.Children.Add(CreateComboBox("cmbType", new[] { "A", "AAAA", "CNAME" }, type));

                        var proxied = bool.Parse(record["Proxied"]?.ToString()) ? "True" : "False";
                        dnsRecordPanel.Children.Add(CreateComboBox("cmbProxied", new[] { "True", "False" }, proxied));

                        var ttlIndex = (record["TTL"]?.ToString());
                        dnsRecordPanel.Children.Add(CreateComboBox("cmbTtl", new[]
                        { 
                            "Auto", "1 min", "2 min", "5 min", "10 min", "15 min", "30 min", "1 hr",
                            "2 hr", "5 hr", "12 hr", "1 day"
                         }, ttlIndex));

                        itemsControlDnsRecords.Items.Add(dnsRecordPanel);
                    }
                }
            }
        }




        private void BtnSaveProfile_Click(object sender, RoutedEventArgs e)
        {
            var profileName = $"Profile_{DateTime.Now:yyyyMMddHHmmss}";
            var profilePath = Path.Combine(profilesFolderPath, $"{profileName}.json");

            var dnsRecords = new List<Dictionary<string, object>>();

            foreach (var item in itemsControlDnsRecords.Items)
            {
                var dnsRecordPanel = item as StackPanel;

                var txtDnsRecordId = dnsRecordPanel.Children.OfType<TextBox>().FirstOrDefault(t => t.Name == "txtDnsRecordId");
                var txtName = dnsRecordPanel.Children.OfType<TextBox>().FirstOrDefault(t => t.Name == "txtName");
                var contentComboBox = dnsRecordPanel.Children.OfType<System.Windows.Controls.ComboBox>().FirstOrDefault(c => c.Name == "content");
                var cmbType = dnsRecordPanel.Children.OfType<System.Windows.Controls.ComboBox>().FirstOrDefault(c => c.Name == "cmbType");
                var cmbProxied = dnsRecordPanel.Children.OfType<System.Windows.Controls.ComboBox>().FirstOrDefault(c => c.Name == "cmbProxied");
                var cmbTtl = dnsRecordPanel.Children.OfType<System.Windows.Controls.ComboBox>().FirstOrDefault(c => c.Name == "cmbTtl");

                if (txtDnsRecordId != null && txtName != null && cmbType != null && cmbProxied != null && cmbTtl != null && contentComboBox != null)
                {
                    var record = new Dictionary<string, object>
            {
                { "RecordID", txtDnsRecordId.Text },
                { "Name", txtName.Text },
                { "Content", ((ComboBoxItem)contentComboBox.SelectedItem)?.Content.ToString() },
                { "Type", ((ComboBoxItem)cmbType.SelectedItem)?.Content.ToString() },
                { "Proxied", ((ComboBoxItem)cmbProxied.SelectedItem)?.Content.ToString() },
                { "TTL", ((ComboBoxItem)cmbTtl.SelectedItem)?.Content.ToString() }
            };

                    dnsRecords.Add(record);
                }
            }

            var profileData = new Dictionary<string, object>
    {
        { "ApiKey", EncryptionHelper.EncryptString(txtApiKey.Text) },
        { "ZoneId", EncryptionHelper.EncryptString(txtZoneId.Text) },
        { "DnsRecords", dnsRecords }
    };

            File.WriteAllText(profilePath, JsonSerializer.Serialize(profileData));
            LoadProfiles();
            MessageBox.Show("Profile saved successfully.");
        }




        private void BtnDeleteProfile_Click(object sender, RoutedEventArgs e)
        {
            if (cmbProfiles.SelectedItem is string profileName)
            {
                var profilePath = Path.Combine(profilesFolderPath, $"{profileName}.json");
                if (File.Exists(profilePath))
                {
                    File.Delete(profilePath);
                    LoadProfiles();
                    MessageBox.Show("Profile deleted successfully.");
                }
            }
        }
    }
}
