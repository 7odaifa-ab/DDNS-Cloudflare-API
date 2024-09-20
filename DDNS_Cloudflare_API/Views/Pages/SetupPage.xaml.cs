using System.IO;
using System.Windows.Controls;
using System.Text.Json;
using Wpf.Ui.Controls;
using DDNS_Cloudflare_API.ViewModels.Pages;
using TextBox = Wpf.Ui.Controls.TextBox;
using Orientation = System.Windows.Controls.Orientation;
using ComboBox = System.Windows.Controls.ComboBox;
using MessageBoxButton = Wpf.Ui.Controls.MessageBoxButton;
using MessageBox = Wpf.Ui.Controls.MessageBox;
using Label = System.Windows.Controls.Label;
using DDNS_Cloudflare_API.Services;
using System.Diagnostics;
using Button = Wpf.Ui.Controls.Button;

namespace DDNS_Cloudflare_API.Views.Pages
{
    public partial class SetupPage : INavigableView<SetupViewModel>
    {
        public SetupViewModel ViewModel { get; }
        private readonly ProfileTimerService timerService;
        private readonly string profilesFolderPath;
        private readonly string settingsFilePath;

        public SetupPage(SetupViewModel viewModel, ProfileTimerService timerService)  // Inject the service here
        {
            ViewModel = viewModel;
            DataContext = this;
            this.timerService = timerService;  // Assign the injected singleton instance to the local variable
            Debug.WriteLine($"ProfileTimerService instance in ViewModel: {timerService.GetHashCode()}");

            InitializeComponent();

            profilesFolderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "DDNS_Cloudflare_API", "Profiles");
            settingsFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "DDNS_Cloudflare_API", "startupSettings.json");
            Directory.CreateDirectory(profilesFolderPath);

            LoadProfiles();
        }

        private void OnStatusUpdated(string message)
        {
            Dispatcher.Invoke(() =>
            {
                txtStatus.Text = message;
            });
        }

        // Event handler for the Save Profile button
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


        // Event handler for the Delete Profile button
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
                else
                {
                    _ = ShowErrorMessage("No profile found to delete.");
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

        private void BtnStart_Click(object sender, RoutedEventArgs e)
        {
            if (cmbProfiles.SelectedItem != null)
            {
                string profileName = cmbProfiles.SelectedItem.ToString();
                timerService.StartTimer(profileName, GetInterval());
                txtStatus.Text = $"{profileName} Started";
            }
        }

        private void BtnStop_Click(object sender, RoutedEventArgs e)
        {
            if (cmbProfiles.SelectedItem != null)
            {
                string profileName = cmbProfiles.SelectedItem.ToString();
                timerService.StopTimer(profileName);
                txtStatus.Text = $"{profileName} Stopped";
            }
        }

        private void BtnOneTime_Click(object sender, RoutedEventArgs e)
        {
            _ = UpdateDnsRecords(); // This calls the one-time DNS update logic
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

        private async Task UpdateDnsRecords()
        {
            if (itemsControlDnsRecords.Items.Count == 0)
            {
                txtStatus.Text = "ERROR";
                await ShowErrorMessage("You have to complete all parameters for the API call.");
                return;
            }

            foreach (var item in itemsControlDnsRecords.Items)
            {
                if (item is StackPanel dnsRecordPanel)
                {
                    var (dnsRecordId, name, content, type, proxied, ttl) = GetDnsRecordFields(dnsRecordPanel);

                    if (IsDnsRecordValid(dnsRecordId, name, content, type, proxied, ttl))
                    {
                        string ipContent = await timerService.GetIpContent((ComboBoxItem)content.SelectedItem);

                        var record = new
                        {
                            content = ipContent,
                            name = name.Text,
                            proxied = ((ComboBoxItem)proxied.SelectedItem).Content.ToString() == "True",
                            type = ((ComboBoxItem)type.SelectedItem).Content.ToString(),
                            ttl = GetTtlInSeconds(ttl.SelectedIndex),
                            comment = "DDNS updated from WPF"
                        };

                        await timerService.UpdateDnsRecordForProfile(EncryptionHelper.DecryptString(txtApiKey.Text), EncryptionHelper.DecryptString(txtZoneId.Text), record, dnsRecordId.Text);
                    }
                    else
                    {
                        await ShowErrorMessage("All parameters for the API call are not complete.");
                    }
                }
            }
        }

        private int GetTtlInSeconds(int index) =>
            index switch
            {
                0 => 1,      // Auto
                1 => 60,     // 1 min
                2 => 120,    // 2 min
                3 => 300,    // 5 min
                4 => 600,    // 10 min
                5 => 900,    // 15 min
                6 => 1800,   // 30 min
                7 => 3600,   // 1 hr
                8 => 7200,   // 2 hr
                9 => 18000,  // 5 hr
                10 => 43200, // 12 hr
                11 => 86400, // 1 day
                _ => 3600    // Default to 1 hr
            };

        private int GetTtlIndex(int ttlInSeconds)
        {
            return ttlInSeconds switch
            {
                1 => 0,       // Auto
                60 => 1,      // 1 min
                120 => 2,     // 2 min
                300 => 3,     // 5 min
                600 => 4,     // 10 min
                900 => 5,     // 15 min
                1800 => 6,    // 30 min
                3600 => 7,    // 1 hr
                7200 => 8,    // 2 hr
                18000 => 9,   // 5 hr
                43200 => 10,  // 12 hr
                86400 => 11,  // 1 day
                _ => 0        // Default to Auto
            };
        }

        private bool IsDnsRecordValid(params object[] fields) => fields.All(field => field != null && !string.IsNullOrEmpty(field.ToString()));

        private (TextBox dnsRecordId, TextBox name, ComboBox content, ComboBox type, ComboBox proxied, ComboBox ttl) GetDnsRecordFields(StackPanel dnsRecordPanel)
        {
            var dnsInputPanel = (StackPanel)dnsRecordPanel.Children[1];
            var dnsComboPanel = (StackPanel)dnsRecordPanel.Children[2];

            return (
                (TextBox)dnsInputPanel.Children[0],
                (TextBox)dnsInputPanel.Children[1],
                (ComboBox)dnsComboPanel.Children[0],
                (ComboBox)dnsComboPanel.Children[1],
                (ComboBox)dnsComboPanel.Children[2],
                (ComboBox)dnsComboPanel.Children[3]
            );
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

        private ComboBoxItem FindComboBoxItem(ComboBox comboBox, string content) =>
            comboBox.Items.OfType<ComboBoxItem>().FirstOrDefault(item => item.Content.ToString() == content);

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

            dnsRecordPanel.Children.Add(CreateLabel("lblRecord", "DNS Record"));

            var dnsInputPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(0, 0, 0, 10)
            };
            dnsInputPanel.Children.Add(CreateTextBox("txtDnsRecordId", "Record ID"));
            dnsInputPanel.Children.Add(CreateTextBox("txtName", "Name"));

            var dnsComboPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(0, 0, 0, 10)
            };
            dnsComboPanel.Children.Add(CreateComboBox("content", new[] { "IPv4", "IPv6" }));
            dnsComboPanel.Children.Add(CreateComboBox("cmbType", new[] { "A", "AAAA" }));
            dnsComboPanel.Children.Add(CreateComboBox("cmbProxied", new[] { "True", "False" }));
            dnsComboPanel.Children.Add(CreateComboBox("cmbTtl", new[]
            {
        "Auto", "1 min", "2 min", "5 min", "10 min", "15 min", "30 min", "1 hr",
        "2 hr", "5 hr", "12 hr", "1 day"
    }));

            // Add a remove button for each DNS record
            var removeButton = new Button
            {
                Content = "Remove",
                Margin = new Thickness(5)
            };
            removeButton.Click += (sender, e) => RemoveDnsRecord(dnsRecordPanel);

            dnsRecordPanel.Children.Add(dnsInputPanel);
            dnsRecordPanel.Children.Add(dnsComboPanel);
            dnsRecordPanel.Children.Add(removeButton);  // Add the remove button to the panel

            return dnsRecordPanel;
        }

        private void RemoveDnsRecord(StackPanel dnsRecordPanel)
        {
            // Ensure at least one DNS record remains
            if (itemsControlDnsRecords.Items.Count > 1)
            {
                itemsControlDnsRecords.Items.Remove(dnsRecordPanel);
            }
            else
            {
                // Show an error message if the user tries to remove the last DNS record
                _ = ShowErrorMessage("At least one DNS record must remain.");
            }
        }



        private Label CreateLabel(string name, string text) => new Label
        {
            Name = name,
            Margin = new Thickness(5),
            Content = text,
        };

        private TextBox CreateTextBox(string name, string holderText, string text = "") =>
            new TextBox
            {
                Name = name,
                Margin = new Thickness(5),
                Text = text,
                Width = 205,
                PlaceholderText = holderText,
                ToolTip = "hint"
            };

        private ComboBox CreateComboBox(string name, string[] items, string selectedItem = null)
        {
            var comboBox = new ComboBox { Name = name, Margin = new Thickness(5), MinWidth = 80 };

            foreach (var item in items)
            {
                comboBox.Items.Add(new ComboBoxItem { Content = item });
            }

            if (selectedItem != null)
            {
                comboBox.SelectedItem = comboBox.Items.OfType<ComboBoxItem>().FirstOrDefault(item => item.Content.ToString() == selectedItem);
            }

            if (comboBox.Items.Count > 0 && selectedItem == null)
            {
                comboBox.SelectedIndex = 0;
            }

            return comboBox;
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
    }
}
