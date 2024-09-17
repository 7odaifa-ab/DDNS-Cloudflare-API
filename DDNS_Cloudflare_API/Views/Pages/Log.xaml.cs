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

        public Log(DataViewModel viewModel)
        {
            ViewModel = viewModel;
            DataContext = this;

            InitializeComponent();

            LoadLog();
        }

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

        private string FormatLogContent(string[] logLines)
        {
            StringBuilder formattedLog = new StringBuilder();

            foreach (var line in logLines)
            {
                if (line.Contains("Response: {") || line.Contains("Fetched IP:"))
                {
                    // Extract the JSON part of the log
                    int jsonStartIndex = line.IndexOf("{");
                    if (jsonStartIndex != -1)
                    {
                        string jsonString = line.Substring(jsonStartIndex);

                        try
                        {
                            // Try to parse and format the JSON
                            var jsonElement = JsonSerializer.Deserialize<JsonElement>(jsonString);
                            string formattedJson = JsonSerializer.Serialize(jsonElement, new JsonSerializerOptions { WriteIndented = true });

                            // Combine the formatted JSON with the log prefix
                            string logPrefix = line.Substring(0, jsonStartIndex);
                            formattedLog.AppendLine($"{logPrefix}{formattedJson}");
                        }
                        catch (JsonException)
                        {
                            // If parsing fails, keep the original line
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

        private void UpdateLogButton_Click(object sender, RoutedEventArgs e)
        {
            LoadLog(); // Refresh the log content
        }
    }
}
