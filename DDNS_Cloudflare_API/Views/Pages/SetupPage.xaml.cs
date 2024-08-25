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
using MessageBoxButton = Wpf.Ui.Controls.MessageBoxButton;
using System.Collections;
using MessageBox = Wpf.Ui.Controls.MessageBox;
using System.Collections.ObjectModel;


namespace DDNS_Cloudflare_API.Views.Pages
{
    public partial class SetupPage : INavigableView<SetupViewModel>
    {
        public SetupViewModel ViewModel { get; }

        private readonly Dictionary<string, DispatcherTimer> profileTimers; // Dictionary to store timers for each profile
        private readonly Dictionary<string, Dictionary<string, object>> profileData;

        private readonly string profilesFolderPath;
        private readonly string settingsFilePath;

        public SetupPage(SetupViewModel viewModel)
        {
            ViewModel = viewModel;
            DataContext = this;

            InitializeComponent();
            profilesFolderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "DDNS_Cloudflare_API", "Profiles");
            settingsFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "DDNS_Cloudflare_API", "startupSettings.json");
            Directory.CreateDirectory(profilesFolderPath);
            profileData = new Dictionary<string, Dictionary<string, object>>();

            profileTimers = new Dictionary<string, DispatcherTimer>(); // Initialize the Dictionary

            LoadProfiles();
            LoadStartupSettings(); // Load and restore the profile running statuses
        }


        private void BtnStart_Click(object sender, RoutedEventArgs e)
        {
            if (cmbProfiles.SelectedItem != null)
            {
                string profileName = cmbProfiles.SelectedItem.ToString();

                // Save profile's status as running
                SaveStartupSetting(profileName, true);

                int interval = GetInterval();
                StartTimer(profileName, interval);
                txtStatus.Text = $"{profileName} Started";
                _ = UpdateDnsRecords();
            }
        }


        private void BtnStop_Click(object sender, RoutedEventArgs e)
        {
            if (cmbProfiles.SelectedItem != null)
            {
                string profileName = cmbProfiles.SelectedItem.ToString();

                // Stop the timer for the selected profile
                StopTimer(profileName);

                // Save profile's status as stopped
                SaveStartupSetting(profileName, false);
            }
        }



        private void BtnOneTime_Click(object sender, RoutedEventArgs e)
        {
            _ = UpdateDnsRecords();
        }

        private void SaveStartupSetting(string profileName, bool isRunning)
        {
            Dictionary<string, bool> startupSettings;

            if (File.Exists(settingsFilePath))
            {
                string json = File.ReadAllText(settingsFilePath);
                startupSettings = JsonSerializer.Deserialize<Dictionary<string, bool>>(json);
            }
            else
            {
                startupSettings = new Dictionary<string, bool>();
            }

            startupSettings[profileName] = isRunning;

            string updatedJson = JsonSerializer.Serialize(startupSettings, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(settingsFilePath, updatedJson);
        }

        private async Task LoadStartupSettings()
        {
            var settingsFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "DDNS_Cloudflare_API", "startupSettings.json");

            if (File.Exists(settingsFilePath))
            {
                string json = File.ReadAllText(settingsFilePath);
                var startupSettings = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json);

                // Check if LoadProfilesOnStartup is true before proceeding
                if (startupSettings.ContainsKey("LoadProfilesOnStartup") && startupSettings["LoadProfilesOnStartup"].GetBoolean())
                {
                    foreach (var kvp in startupSettings)
                    {
                        // Skip non-profile entries like "RunOnStartup" and "LoadProfilesOnStartup"
                        if (kvp.Key == "RunOnStartup" || kvp.Key == "LoadProfilesOnStartup")
                            continue;

                        // Check if the profile was running and start it if true
                        if (kvp.Value.GetBoolean())
                        {
                            cmbProfiles.SelectedItem = kvp.Key;
                            BtnStart_Click(null, null); // Automatically start the profile

                            // Wait for 5 seconds before proceeding to the next profile
                            await Task.Delay(5000);

                        }
                    }
                }
            }
        }




        private int GetInterval() =>
            cmbInterval.SelectedIndex switch
            {
                0 => 1,
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

        private async Task UpdateDnsRecordForProfile(string apiKey, string zoneId, object record, string dnsRecordId)
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

            // Log and display the response as needed
            txtStatus.Text = $"Last update: {DateTime.Now}\nResponse: {await response.Content.ReadAsStringAsync()}";
            UpdateLogFile(await response.Content.ReadAsStringAsync());
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
            UpdateLogFile(await response.Content.ReadAsStringAsync());
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
                Orientation = Orientation.Vertical,
                Margin = new Thickness(0, 0, 0, 10)
            };
            var dnsInputPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(0, 0, 0, 10)
            };
            var dnsComboPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(0, 0, 0, 10)
            };
            dnsInputPanel.Children.Add(CreateTextBox("txtDnsRecordId","Record ID"));
            dnsInputPanel.Children.Add(CreateTextBox("txtName","Name"));
            dnsComboPanel.Children.Add(CreateComboBox("content", new[] { "IPv4", "IPv6" }));
            dnsComboPanel.Children.Add(CreateComboBox("cmbType", new[] { "A", "AAAA"}));
            dnsComboPanel.Children.Add(CreateComboBox("cmbProxied", new[] { "True", "False" }));
            dnsComboPanel.Children.Add(CreateComboBox("cmbTtl", new[]
            {
               "Auto", "1 min", "2 min", "5 min", "10 min", "15 min", "30 min", "1 hr",
               "2 hr", "5 hr", "12 hr", "1 day"
           }));
            dnsRecordPanel.Children.Add(dnsInputPanel);
            dnsRecordPanel.Children.Add(dnsComboPanel);

            return dnsRecordPanel;
        }

        private TextBox CreateTextBox(string name, string holderText, string text = "") =>
            new TextBox
            {
                Name = name,
                Margin = new Thickness(5),
                Text = text, 
                Width =205 ,
                PlaceholderText =holderText,
                ToolTip = "hint"
            };

        private ComboBox CreateComboBox(string name, string[] items, string selectedItem = null)
        {
            var comboBox = new ComboBox { Name = name, Margin = new Thickness(5) , MinWidth = 80};

            // Add items to the ComboBox
            foreach (var item in items)
            {
                comboBox.Items.Add(new ComboBoxItem { Content = item });
            }

            // Set the selected item if specified
            if (selectedItem != null)
            {
                comboBox.SelectedItem = comboBox.Items.OfType<ComboBoxItem>().FirstOrDefault(item => item.Content.ToString() == selectedItem);
            }

            // Automatically select the first item if no item is specified
            if (comboBox.Items.Count > 0 && selectedItem == null)
            {
                comboBox.SelectedIndex = 0;
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


        private void UpdateDnsRecordPanel(StackPanel dnsRecordPanel, Dictionary<string, object> record)
        {
            var dnsInputPanel = (StackPanel)dnsRecordPanel.Children[0];
            var dnsComboPanel = (StackPanel)dnsRecordPanel.Children[1];

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
            var dnsInputPanel = (StackPanel)dnsRecordPanel.Children[0];
            var dnsComboPanel = (StackPanel)dnsRecordPanel.Children[1];

            return (
                (TextBox)dnsInputPanel.Children[0],
                (TextBox)dnsInputPanel.Children[1],
                (ComboBox)dnsComboPanel.Children[0],
                (ComboBox)dnsComboPanel.Children[1],
                (ComboBox)dnsComboPanel.Children[2],
                (ComboBox)dnsComboPanel.Children[3]
            );
        }


        private ComboBoxItem FindComboBoxItem(ComboBox comboBox, string content) =>
            comboBox.Items.OfType<ComboBoxItem>().FirstOrDefault(item => item.Content.ToString() == content);
        
        private void UpdateLogFile(string message)
        {
            var profilePath = Path.Combine(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "DDNS_Cloudflare_API"), "Logs.txt");
            var logFile = new FileInfo(profilePath);
            // Ensure the file exists; create it if it does not
            if (!logFile.Exists)
            {
                // Create the file
                using (var stream = logFile.Create())
                {
                }
            }
            // Format the message to include a timestamp and two new lines
            string formattedMessage = $"{DateTime.Now}: {message}\n\n";

            File.AppendAllText(profilePath, formattedMessage);
        }



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
            var messageBox = new MessageBox
            {
                Title = "Error",
                Content = message,
                SecondaryButtonText = "OK"
            };
            await messageBox.ShowDialogAsync();
        }

        private async Task ShowSuccessMessage(string message)
        {
            var dialog = new MessageBox
            {
                Title = "Success",
                Content = message,
            };
            await dialog.ShowDialogAsync();
        }

        private void StartTimer(string profileName, int intervalMinutes)
        {
            if (profileTimers.ContainsKey(profileName))
            {
                profileTimers[profileName].Stop();
                profileTimers[profileName] = null;
            }

            // Load profile data if not already loaded
            if (!profileData.ContainsKey(profileName))
            {
                var profilePath = Path.Combine(profilesFolderPath, $"{profileName}.json");
                if (File.Exists(profilePath))
                {
                    var json = File.ReadAllText(profilePath);
                    var profile = JsonSerializer.Deserialize<Dictionary<string, object>>(json);
                    profileData[profileName] = profile;
                }
            }

            // Use the loaded profile data in the timer
            DispatcherTimer timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMinutes(intervalMinutes)
            };
            timer.Tick += async (sender, e) => await UpdateDnsRecordsForProfile(profileName);
            timer.Start();

            profileTimers[profileName] = timer; // Store the timer in the dictionary
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

                    // Use the stored profile data for the API call
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

        private async Task UpdateDnsRecords()
        {
            if (itemsControlDnsRecords.Items.Count == 0)
            {
                txtStatus.Text = "ERORR";
                await ShowErrorMessage("you have to complete all parameters for API call.");
                return;
            }
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



        private void StopTimer(string profileName)
        {
            if (profileTimers.ContainsKey(profileName))
            {
                profileTimers[profileName].Stop();
                profileTimers.Remove(profileName); // Remove the timer from the dictionary
                txtStatus.Text = $"{profileName} Stopped";
            }
        }


        private bool IsDnsRecordValid(params object[] fields) => fields.All(field => field != null && !string.IsNullOrEmpty(field.ToString()));

    }
}
