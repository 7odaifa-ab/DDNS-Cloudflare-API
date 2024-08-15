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


namespace DDNS_Cloudflare_API.Views.Pages
{
    public partial class DashboardPage : INavigableView<DashboardViewModel>
    {
        public DashboardViewModel ViewModel { get; }

        private DispatcherTimer timer;
        private readonly string profilesFolderPath;

        public DashboardPage(DashboardViewModel viewModel)
        {
            ViewModel = viewModel;
            DataContext = this;

            InitializeComponent();
            profilesFolderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "DDNS_Cloudflare_API", "Profiles");
            Directory.CreateDirectory(profilesFolderPath);

            LoadProfiles();
        }

        private void BtnStart_Click(object sender, RoutedEventArgs e)
        {
            int interval = GetInterval();
            StartTimer(interval);
            txtStatus.Text = "Started";
            _ = UpdateDnsRecords();
        }

        private void BtnStop_Click(object sender, RoutedEventArgs e)
        {
            StopTimer();
        }

        private void BtnOneTime_Click(object sender, RoutedEventArgs e)
        {
            _ = UpdateDnsRecords();
        }

        private int GetInterval() =>
            cmbInterval.SelectedIndex switch
            {
                0 => 15,
                1 => 30,
                2 => 60,
                3 => 360,
                4 => 720,
                5 => 1440,
                _ => 60
            };

        private int GetTtlInSeconds(int index) =>
            index switch
            {
                0 => 1,
                1 => 60,
                2 => 120,
                3 => 300,
                4 => 600,
                5 => 900,
                6 => 1800,
                7 => 3600,
                8 => 7200,
                9 => 18000,
                10 => 43200,
                11 => 86400,
                _ => 3600
            };

        private async Task UpdateDnsRecords()
        {
            foreach (var item in itemsControlDnsRecords.Items)
            {
                if (item is StackPanel dnsRecordPanel)
                {
                    var (dnsRecordId, name, content, type, proxied, ttl) = GetDnsRecordFields(dnsRecordPanel);

                    if (IsDnsRecordValid(dnsRecordId, name, content, type, proxied, ttl))
                    {
                        string ipContent = await GetIpContent((ComboBoxItem)content.SelectedItem);

                        var record = new
                        {
                            content = ipContent,
                            name = name.Text,
                            proxied = ((ComboBoxItem)proxied.SelectedItem).Content.ToString() == "True",
                            type = ((ComboBoxItem)type.SelectedItem).Content.ToString(),
                            ttl = GetTtlInSeconds(ttl.SelectedIndex),
                            comment = "DDNS updated from WPF"
                        };

                        await UpdateDnsRecord(record, dnsRecordId.Text);
                    }
                    else
                    {
                        await ShowErrorMessage("All parameters for API call are not complete.");
                    }
                }
            }
        }

        private async Task<string> GetIpContent(ComboBoxItem selectedContent) =>
            selectedContent.Content.ToString() switch
            {
                "IPv4" => await GetWanIpv4(),
                "IPv6" => await GetWanIpv6(),
                _ => string.Empty
            };

        private async Task UpdateDnsRecord(object record, string dnsRecordId)
        {
            string json = JsonSerializer.Serialize(record);

            using HttpClient client = new HttpClient
            {
                DefaultRequestHeaders =
                {
                    Accept = { new MediaTypeWithQualityHeaderValue("application/json") },
                    Authorization = new AuthenticationHeaderValue("Bearer", txtApiKey.Text)
                }
            };

            HttpResponseMessage response = await client.PutAsync(
                $"https://api.cloudflare.com/client/v4/zones/{txtZoneId.Text}/dns_records/{dnsRecordId}",
                new StringContent(json, Encoding.UTF8, "application/json"));

            txtStatus.Text = $"Last update: {DateTime.Now}\nResponse: {await response.Content.ReadAsStringAsync()}";
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

        private void BtnAddDnsRecord_Click(object sender, RoutedEventArgs e)
        {
            var dnsRecordPanel = CreateDnsRecordPanel();
            itemsControlDnsRecords.Items.Add(dnsRecordPanel);
        }

        private StackPanel CreateDnsRecordPanel()
        {
            var dnsRecordPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(0, 0, 0, 10)
            };

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

            return dnsRecordPanel;
        }

        private TextBox CreateTextBox(string name, string text = "") =>
            new TextBox
            {
                Name = name,
                Margin = new Thickness(5),
                Width = 100,
                Text = text
            };

        private ComboBox CreateComboBox(string name, string[] items, string selectedItem = null)
        {
            var comboBox = new ComboBox { Name = name, Margin = new Thickness(5) };
            foreach (var item in items)
            {
                comboBox.Items.Add(new ComboBoxItem { Content = item });
            }
            if (selectedItem != null)
            {
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
                    cmbProfiles.Items.Add(Path.GetFileNameWithoutExtension(file));
                }
                cmbProfiles.IsEnabled = cmbProfiles.Items.Count > 0;
            }
        }

        private void CmbProfiles_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cmbProfiles.SelectedItem is string profileName)
            {
                var profilePath = Path.Combine(profilesFolderPath, $"{profileName}.json");
                if (File.Exists(profilePath))
                {
                    LoadProfile(profilePath);
                }
            }
        }

        private void LoadProfile(string profilePath)
        {
            var json = File.ReadAllText(profilePath);
            var profile = JsonSerializer.Deserialize<Dictionary<string, object>>(json);

            txtApiKey.Text = EncryptionHelper.DecryptString(profile["ApiKey"].ToString());
            txtZoneId.Text = EncryptionHelper.DecryptString(profile["ZoneId"].ToString());

            itemsControlDnsRecords.Items.Clear();

            var dnsRecords = JsonSerializer.Deserialize<List<Dictionary<string, object>>>(profile["DnsRecords"].ToString());
            foreach (var record in dnsRecords)
            {
                var dnsRecordPanel = CreateDnsRecordPanel();
                UpdateDnsRecordPanel(dnsRecordPanel, record);
                itemsControlDnsRecords.Items.Add(dnsRecordPanel);
            }
        }
        private int GetTtlIndex(int ttlInSeconds)
        {
            return ttlInSeconds switch
            {
                1 => 0, // Auto
                60 => 1, // 1 min
                120 => 2, // 2 min
                300 => 3, // 5 min
                600 => 4, // 10 min
                900 => 5, // 15 min
                1800 => 6, // 30 min
                3600 => 7, // 1 hr
                7200 => 8, // 2 hr
                18000 => 9, // 5 hr
                43200 => 10, // 12 hr
                86400 => 11, // 1 day
                _ => 0 // Default to Auto
            };
        }


        private void UpdateDnsRecordPanel(StackPanel dnsRecordPanel, Dictionary<string, object> record)
        {
            var (dnsRecordId, name, content, type, proxied, ttl) = GetDnsRecordFields(dnsRecordPanel);

            dnsRecordId.Text = record["RecordID"]?.ToString();
            name.Text = record["Name"]?.ToString();
            content.SelectedItem = FindComboBoxItem(content, record["Content"]?.ToString());
            type.SelectedItem = FindComboBoxItem(type, record["Type"]?.ToString());
            proxied.SelectedItem = FindComboBoxItem(proxied, bool.Parse(record["Proxied"]?.ToString()) ? "True" : "False");
            ttl.SelectedIndex = GetTtlIndex(int.Parse(record["TTL"]?.ToString()));
        }

        private (TextBox dnsRecordId, TextBox name, ComboBox content, ComboBox type, ComboBox proxied, ComboBox ttl) GetDnsRecordFields(StackPanel dnsRecordPanel)
        {
            return (
                (TextBox)dnsRecordPanel.Children[0],
                (TextBox)dnsRecordPanel.Children[1],
                (ComboBox)dnsRecordPanel.Children[2],
                (ComboBox)dnsRecordPanel.Children[3],
                (ComboBox)dnsRecordPanel.Children[4],
                (ComboBox)dnsRecordPanel.Children[5]
            );
        }

        private ComboBoxItem FindComboBoxItem(ComboBox comboBox, string content) =>
            comboBox.Items.OfType<ComboBoxItem>().FirstOrDefault(item => item.Content.ToString() == content);

        private void BtnSaveProfile_Click(object sender, RoutedEventArgs e)
        {

            var profileName = $"newProfile_{DateTime.Now:yyyyMMddHHmmss}";            
            var profilePath = Path.Combine(profilesFolderPath, $"{profileName}.json");

            var profile = new Dictionary<string, object>
            {
                { "ApiKey", EncryptionHelper.EncryptString(txtApiKey.Text) },
                { "ZoneId", EncryptionHelper.EncryptString(txtZoneId.Text) },
                { "DnsRecords", SerializeDnsRecords() }
            };

            var json = JsonSerializer.Serialize(profile);
            File.WriteAllText(profilePath, json);

            LoadProfiles();
            _ = ShowSuccessMessage("Profile saved successfully.");
        }

        private string SerializeDnsRecords()
        {
            var dnsRecords = new List<Dictionary<string, object>>();

            foreach (var item in itemsControlDnsRecords.Items)
            {
                if (item is StackPanel dnsRecordPanel)
                {
                    var (dnsRecordId, name, content, type, proxied, ttl) = GetDnsRecordFields(dnsRecordPanel);

                    dnsRecords.Add(new Dictionary<string, object>
                    {
                        { "RecordID", dnsRecordId.Text },
                        { "Name", name.Text },
                        { "Content", ((ComboBoxItem)content.SelectedItem).Content.ToString() },
                        { "Type", ((ComboBoxItem)type.SelectedItem).Content.ToString() },
                        { "Proxied", ((ComboBoxItem)proxied.SelectedItem).Content.ToString() == "True" },
                        { "TTL", GetTtlInSeconds(ttl.SelectedIndex) }
                    });
                }
            }

            return JsonSerializer.Serialize(dnsRecords);
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
                    ClearInputFields();
                    _ = ShowSuccessMessage("Profile deleted successfully.");
                }
            }
            else
            {
                _ = ShowErrorMessage("No profile selected to delete.");
            }
        }

        private void ClearInputFields()
        {
            txtApiKey.Text = string.Empty;
            txtZoneId.Text = string.Empty;
            itemsControlDnsRecords.Items.Clear();
        }

        private async Task ShowErrorMessage(string message)
        {
            var dialog = new ContentDialog
            {
                Title = "Error",
                Content = message,
                PrimaryButtonText = "OK"
            };
            await dialog.ShowAsync();
        }

        private async Task ShowSuccessMessage(string message)
        {
            var dialog = new ContentDialog
            {
                Title = "Success",
                Content = message,
                PrimaryButtonText = "OK"
            };
            await dialog.ShowAsync();
        }

        private void StartTimer(int intervalMinutes)
        {
            timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMinutes(intervalMinutes)
            };
            timer.Tick += async (sender, e) => await UpdateDnsRecords();
            timer.Start();
        }

        private void StopTimer()
        {
            timer?.Stop();
            timer = null;
            txtStatus.Text = "Stopped";
        }

        private bool IsDnsRecordValid(params object[] fields) => fields.All(field => field != null && !string.IsNullOrEmpty(field.ToString()));


    }
}
