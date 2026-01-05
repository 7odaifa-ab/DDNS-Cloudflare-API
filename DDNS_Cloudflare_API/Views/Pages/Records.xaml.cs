/*
 * Author: Hudaifa Abdullah
 * @7odaifa_ab
 * info@huimangtech.com
 *
 * This class manages the logic for retrieving and displaying DNS records using Cloudflare's API.
 * It interacts with the API, logs responses, and shows DNS records in the user interface.
 */

using DDNS_Cloudflare_API.ViewModels.Pages;
using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Wpf.Ui.Controls;
using Wpf.Ui.Abstractions.Controls;
using TextBlock = Wpf.Ui.Controls.TextBlock;
using TextBox = System.Windows.Controls.TextBox;
using MessageBox = System.Windows.MessageBox;


namespace DDNS_Cloudflare_API.Views.Pages
{
    public partial class Records : INavigableView<DataViewModel>
    {
        public DataViewModel ViewModel { get; }

        #region Constructor

        // Initializes the Records page with the provided ViewModel
        public Records(DataViewModel viewModel)
        {
            ViewModel = viewModel;
            DataContext = this;
            InitializeComponent();
        }

        #endregion

        #region Event Handlers

        // Triggered when the "Get DNS Records" button is clicked
        private async void GetDnsRecords_Click(object sender, RoutedEventArgs e)
        {
            string apiKey = ApiKeyTextBox.Text;
            string zoneId = ZoneIdTextBox.Text;

            if (string.IsNullOrEmpty(apiKey) || string.IsNullOrEmpty(zoneId))
            {
                MessageBox.Show("API Key and Zone ID are required.");
                return;
            }

            try
            {
                using HttpClient client = new HttpClient
                {
                    DefaultRequestHeaders =
                    {
                        Accept = { new MediaTypeWithQualityHeaderValue("application/json") },
                        Authorization = new AuthenticationHeaderValue("Bearer", apiKey)
                    }
                };

                HttpResponseMessage response = await client.GetAsync($"https://api.cloudflare.com/client/v4/zones/{zoneId}/dns_records");
                string responseContent = await response.Content.ReadAsStringAsync();

                UpdateLogFile(responseContent);
                DisplayDnsRecords(responseContent);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error fetching DNS records: {ex.Message}");
            }
        }

        #endregion

        #region Helper Methods

        // Displays DNS records parsed from the API response
        private void DisplayDnsRecords(string responseContent)
        {
            ResultsPanel.Children.Clear();

            var jsonDocument = JsonDocument.Parse(responseContent);
            var resultArray = jsonDocument.RootElement.GetProperty("result").EnumerateArray();
            int recordNumber = 1;  // Initialize record counter

            foreach (var record in resultArray)
            {
                StackPanel recordPanel = new StackPanel
                {
                    Orientation = System.Windows.Controls.Orientation.Vertical,
                    Margin = new Thickness(5)
                };

                // Add a numbered header for each record
                TextBlock header = new TextBlock
                {
                    Text = $"Record {recordNumber++}",
                    FontWeight = FontWeights.Bold,
                    Margin = new Thickness(0, 0, 0, 5)
                };
                recordPanel.Children.Add(header);

                AddLabel(recordPanel, "ID", record.GetProperty("id").GetString() ?? string.Empty);
                AddLabel(recordPanel, "Content", record.GetProperty("content").GetString() ?? string.Empty);
                AddLabel(recordPanel, "Name", record.GetProperty("name").GetString() ?? string.Empty);
                AddLabel(recordPanel, "Type", record.GetProperty("type").GetString() ?? string.Empty);
                AddLabel(recordPanel, "Proxied", record.GetProperty("proxied").GetBoolean().ToString());
                AddLabel(recordPanel, "TTL", record.GetProperty("ttl").GetInt32().ToString());
                AddLabel(recordPanel, "Comment", record.GetProperty("comment").GetString() ?? string.Empty);

                ResultsPanel.Children.Add(recordPanel);
            }
        }

        // Adds labels to display DNS record fields
        private void AddLabel(StackPanel panel, string label, string value)
        {
            TextBox textBox = new TextBox
            {
                Text = $"{label}: {value}",
                IsReadOnly = true,
                BorderThickness = new Thickness(0),
                Background = new SolidColorBrush(Colors.Transparent),
                Margin = new Thickness(0, 0, 0, 2),
                VerticalAlignment = VerticalAlignment.Top,
                TextWrapping = TextWrapping.Wrap
            };
            panel.Children.Add(textBox);
        }

        // Logs the DNS API response to a file
        private void UpdateLogFile(string message)
        {
            string logFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "DDNS_Cloudflare_API", "Logs.txt");

            if (!File.Exists(logFilePath))
            {
                File.Create(logFilePath).Dispose();  // Create and close the file if it doesn't exist
            }

            string formattedMessage = $"{DateTime.Now}: {message}\n\n";
            File.AppendAllText(logFilePath, formattedMessage);
        }

        #endregion
    }
}
