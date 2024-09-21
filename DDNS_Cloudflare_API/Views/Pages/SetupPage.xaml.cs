using System.IO;
using System.Windows.Controls;
using System.Text.Json;
using System.Windows;
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
using System.Threading.Tasks;

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
            var mainDomain = txtMainDomain.Text;  // Get the main domain from input
            var dnsRecords = SerializeDnsRecords();  // Serialize DNS records to a list

            // Generate profile name using mainDomain and DNS record types
            string profileName = GenerateProfileName(mainDomain, dnsRecords);

            // Check if profile with the same name exists and create numbered profile name if needed
            profileName = GetUniqueProfileName(profileName);

            // Save the profile with the dynamic profile name
            var profilePath = Path.Combine(profilesFolderPath, $"{profileName}.json");

            var profile = new Dictionary<string, object>
    {
        { "ApiKey", EncryptionHelper.EncryptString(txtApiKey.Text) },
        { "ZoneId", EncryptionHelper.EncryptString(txtZoneId.Text) },
        { "mainDomain", mainDomain },  // Save the main domain
        { "DnsRecords", dnsRecords }   // Save the DNS records
    };

            var json = JsonSerializer.Serialize(profile);
            File.WriteAllText(profilePath, json);  // Save the profile to a file with the dynamic profile name

            LoadProfiles();  // Reload profiles after saving
            _ = ShowSuccessMessage("Profile saved successfully.");
        }

        // Method to generate profile name based on mainDomain and DNS record types
        private string GenerateProfileName(string mainDomain, string dnsRecords)
        {
            var dnsRecordsList = JsonSerializer.Deserialize<List<Dictionary<string, object>>>(dnsRecords);
            string profileName = mainDomain;

            foreach (var record in dnsRecordsList)
            {
                string recordType = record["Type"]?.ToString();
                if (!string.IsNullOrEmpty(recordType))
                {
                    profileName += "-" + recordType;  // Concatenate the record type
                }
            }

            return profileName;
        }

        // Method to generate a unique profile name if one already exists
        private string GetUniqueProfileName(string baseName)
        {
            int counter = 1;
            string profilePath = Path.Combine(profilesFolderPath, $"{baseName}.json");

            while (File.Exists(profilePath))
            {
                string newName = $"{baseName}-{counter}";
                profilePath = Path.Combine(profilesFolderPath, $"{newName}.json");
                counter++;
            }

            return Path.GetFileNameWithoutExtension(profilePath);
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

        private void BtnUpdateProfile_Click(object sender, RoutedEventArgs e)
        {
            if (cmbProfiles.SelectedItem is string profileName)
            {
                var profilePath = Path.Combine(profilesFolderPath, $"{profileName}.json");

                if (File.Exists(profilePath))
                {
                    var mainDomain = txtMainDomain.Text;  // Get the main domain from input
                    var dnsRecords = SerializeDnsRecords();  // Serialize DNS records to a list

                    var profile = new Dictionary<string, object>
            {
                { "ApiKey", EncryptionHelper.EncryptString(txtApiKey.Text) },
                { "ZoneId", EncryptionHelper.EncryptString(txtZoneId.Text) },
                { "mainDomain", mainDomain },  // Save the main domain
                { "DnsRecords", dnsRecords }   // Save the DNS records
            };

                    var json = JsonSerializer.Serialize(profile);
                    File.WriteAllText(profilePath, json);  // Overwrite the profile with updated details

                    LoadProfiles();  // Reload profiles after updating
                    _ = ShowSuccessMessage("Profile updated successfully.");
                }
                else
                {
                    _ = ShowErrorMessage("Profile not found for updating.");
                }
            }
            else
            {
                _ = ShowErrorMessage("No profile selected to update.");
            }
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
            txtMainDomain.Text = string.Empty;
            itemsControlDnsRecords.Items.Clear();
        }


        private void BtnStart_Click(object sender, RoutedEventArgs e)
        {
            if (cmbProfiles.SelectedItem != null)
            {
                UpdateDnsRecords();
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

                // First, add all existing profiles
                foreach (var file in profileFiles)
                {
                    cmbProfiles.Items.Add(Path.GetFileNameWithoutExtension(file));
                }

                // Finally, add the "+ Add a New Profile" option at the end
                ComboBoxItem newProfileItem = new ComboBoxItem { Content = "+ Add a New Profile" };
                cmbProfiles.Items.Add(newProfileItem);

                cmbProfiles.IsEnabled = cmbProfiles.Items.Count > 0;
            }
        }



        private void CmbProfiles_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cmbProfiles.SelectedItem is ComboBoxItem selectedItem && selectedItem.Content.ToString() == "+ Add a New Profile")
            {
                // Clear all input fields to allow creating a new profile
                ClearInputFields();
                btnUpdateProfile.IsEnabled = false;  // Disable the Update button when adding a new profile
            }
            else if (cmbProfiles.SelectedItem is string profileName)
            {
                var profilePath = Path.Combine(profilesFolderPath, $"{profileName}.json");

                if (File.Exists(profilePath))
                {
                    LoadProfile(profilePath);  // Load the selected profile's data
                    btnUpdateProfile.IsEnabled = true;  // Enable the Update button when a profile is selected
                }
                else
                {
                    ClearInputFields();
                    btnUpdateProfile.IsEnabled = false;  // Disable the Update button
                }
            }
            else
            {
                ClearInputFields();  // If no valid selection is made, clear input fields
                btnUpdateProfile.IsEnabled = false;
            }
        }



        private void LoadProfile(string profilePath)
        {
            var json = File.ReadAllText(profilePath);
            var profile = JsonSerializer.Deserialize<Dictionary<string, object>>(json);

            txtApiKey.Text = EncryptionHelper.DecryptString(profile["ApiKey"].ToString());
            txtZoneId.Text = EncryptionHelper.DecryptString(profile["ZoneId"].ToString());
            txtMainDomain.Text = profile["mainDomain"].ToString();  // Load main domain


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

                    // Validate DNS record fields
                    if (IsDnsRecordValid(dnsRecordId, name, content, type, proxied, ttl))
                    {
                        string ipContent = await timerService.GetIpContent((ComboBoxItem)content.SelectedItem);

                        // Get mainDomain from the input field
                        string mainDomain = txtMainDomain.Text;

                        // Create the full record name (Name + mainDomain)
                        string fullName = name.Text + "." + mainDomain;

                        var record = new
                        {
                            content = ipContent,
                            name = fullName,  // Use the full name for the DNS record
                            proxied = ((ComboBoxItem)proxied.SelectedItem).Content.ToString() == "True",
                            type = ((ComboBoxItem)type.SelectedItem).Content.ToString(),
                            ttl = GetTtlInSeconds(ttl.SelectedIndex),
                            comment = "DDNS updated from WPF - one time update"
                        };

                        try
                        {
                            // Call the method and get the response content
                            string response = await timerService.UpdateDnsRecordForProfile(
                                txtApiKey.Text,
                                txtZoneId.Text,
                                record,
                                dnsRecordId.Text);

                            // Update status with the API response using OnStatusUpdated
                            OnStatusUpdated($"Update Successful: {response}");
                            Debug.WriteLine("UpdateDnsRecordForProfile call succeeded");
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine($"Error in UpdateDnsRecordForProfile: {ex.Message}");
                        }
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
                Orientation = Orientation.Vertical,
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
                PlaceholderText = holderText,
                HorizontalAlignment = System.Windows.HorizontalAlignment.Stretch,  // Use the fully qualified type name
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
