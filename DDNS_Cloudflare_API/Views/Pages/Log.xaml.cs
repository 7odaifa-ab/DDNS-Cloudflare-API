/*
 * Author: Hudaifa Abdullah
 * @7odaifa_ab
 * info@huimangtech.com
 *
 * This class manages the Log page of the DDNS Cloudflare API application.
 * It displays log entries in reverse order and formats JSON responses for easy readability.
 */

using DDNS_Cloudflare_API.ViewModels.Pages;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Navigation;
using Wpf.Ui.Controls;

namespace DDNS_Cloudflare_API.Views.Pages
{
    public partial class Log : INavigableView<DataViewModel>
    {
        public DataViewModel ViewModel { get; }

        #region Constructor

        // Constructor initializes the Log page and loads the log entries
        public Log(DataViewModel viewModel)
        {
            ViewModel = viewModel;
            DataContext = this;
            InitializeComponent();
            LoadLog();
        }

        #endregion

        #region Methods

        // Loads and displays the log file content in reverse order
        private void LoadLog()
        {
            string logFolderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "DDNS_Cloudflare_API");
            string logFilePath = Path.Combine(logFolderPath, "Logs.txt");

            if (File.Exists(logFilePath))
            {
                var lines = File.ReadAllLines(logFilePath);
                var reversedLines = lines.Reverse().ToArray();
                string formattedLogContent = FormatLogContent(reversedLines);
                txtLog.Text = formattedLogContent;
            }
            else
            {
                txtLog.Text = "Log file not found.";
            }
        }

        // Formats log entries and beautifies JSON responses for readability
        private string FormatLogContent(string[] logLines)
        {
            StringBuilder formattedLog = new StringBuilder();

            foreach (var line in logLines)
            {
                if (line.Contains("Response: {") || line.Contains("Fetched IP:"))
                {
                    int jsonStartIndex = line.IndexOf("{");
                    if (jsonStartIndex != -1)
                    {
                        string jsonString = line.Substring(jsonStartIndex);
                        try
                        {
                            var jsonElement = JsonSerializer.Deserialize<JsonElement>(jsonString);
                            string formattedJson = JsonSerializer.Serialize(jsonElement, new JsonSerializerOptions { WriteIndented = true });
                            string logPrefix = line.Substring(0, jsonStartIndex);
                            formattedLog.AppendLine($"{logPrefix}{formattedJson}");
                        }
                        catch (JsonException)
                        {
                            formattedLog.AppendLine(line);
                        }
                    }
                    else
                    {
                        formattedLog.AppendLine(line);
                    }
                }
                else
                {
                    formattedLog.AppendLine(line);
                }
            }

            return formattedLog.ToString();
        }

        // Event handler for the button click to update the log display
        private void UpdateLogButton_Click(object sender, RoutedEventArgs e)
        {
            LoadLog(); // Refresh the log content
        }

        #endregion
    }
}
