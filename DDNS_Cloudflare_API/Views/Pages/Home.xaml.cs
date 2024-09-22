using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using DDNS_Cloudflare_API.ViewModels.Pages;
using System.Windows.Threading;
using Wpf.Ui.Controls;
using System.Collections.ObjectModel;
using DDNS_Cloudflare_API.Services;
using System.Diagnostics;
using System.Text.Json;
using System.Globalization;
using System.Text.RegularExpressions;

namespace DDNS_Cloudflare_API.Views.Pages
{
    public partial class Home : INavigableView<HomeViewModel>
    {
        // Observable collection to bind to the DataGrid
        public ObservableCollection<ProfileInfo> Profiles { get; set; }

        public HomeViewModel ViewModel { get; }

        private readonly ProfileTimerService timerService;

        public Home(HomeViewModel viewModel, ProfileTimerService timerService)
        {
            InitializeComponent();

            Profiles = new ObservableCollection<ProfileInfo>();
            ViewModel = viewModel;  // Set ViewModel
            DataContext = ViewModel;  // Bind DataContext to the ViewModel
            this.timerService = timerService;  // Inject the timer service

            // Assign the UpdateLastApiCallLog to the delegate in the timer service
            timerService.UpdateLastApiCallLogAction = UpdateLastApiCallLog;
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            RefreshDashboard();
            UpdateLastApiCallLog();  // Load the last API call on page load
        }

        // Method to refresh the dashboard
        public void RefreshDashboard()
        {
            ViewModel.RefreshStatuses();
        }


        private void UpdateLastApiCallLog()
        {
            string logFolderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "DDNS_Cloudflare_API");
            string logFilePath = Path.Combine(logFolderPath, "Logs.txt");

            var logEntry = GetLastApiCall(logFilePath);  // Parse log file
            if (logEntry != null)
            {
                // Check if a timer is running for the profile
                bool isRunning = timerService.IsProfileTimerRunning(logEntry.ProfileName);

                // Bind log entry data to the ViewModel for the UI
                ViewModel.LastLogEntry = new LogEntry
                {
                    ProfileName = logEntry.ProfileName,
                    CallStatus = logEntry.CallStatus,
                    Domain = logEntry.Domain,
                    IpAddress = logEntry.IpAddress,
                    Date = logEntry.Date,
                    RunningStatus = isRunning ? "Running" : "Stopped"  // Set RunningStatus based on the timer
                };
            }
            else
            {
                Debug.WriteLine("No log entry found or unable to parse.");
            }
        }

        public LogEntry GetLastApiCall(string logFilePath)
        {
            if (!File.Exists(logFilePath))
            {
                Debug.WriteLine("Log file not found.");
                return null;
            }

            var lines = File.ReadAllLines(logFilePath);
            LogEntry lastEntry = null;

            foreach (var line in lines.Reverse())  // Read in reverse for the last entry
            {
                if (line.Contains("Response: {"))  // Match specific log entries for API responses
                {
                    lastEntry = ParseLogEntry(line);
                    if (lastEntry != null)
                    {
                        Debug.WriteLine("Log entry successfully parsed.");
                    }
                    break;
                }
            }

            return lastEntry;
        }

        private LogEntry ParseLogEntry(string logLine)
        {
            try
            {
                // Find where the "Response:" starts to extract the preceding info
                int jsonStartIndex = logLine.IndexOf("Response:");
                if (jsonStartIndex != -1)
                {
                    // Extract the header part before "Response:"
                    string headerPart = logLine.Substring(0, jsonStartIndex).Trim();

                    // Extract profile name, domain, and IP from the header
                    var profileNameMatch = Regex.Match(headerPart, @"Profile:\s([^,]+)");
                    var domainMatch = Regex.Match(headerPart, @"Domain:\s([^,]+)");
                    var ipMatch = Regex.Match(headerPart, @"IP:\s([^,]+)");

                    // Extract the Date, capturing AM/PM or Arabic markers
                    string datePart = ExtractDatePart(headerPart);

                    // Extract the JSON part after "Response:"
                    string jsonString = logLine.Substring(jsonStartIndex + 9).Trim();
                    var jsonElement = JsonSerializer.Deserialize<JsonElement>(jsonString);

                    var logEntry = new LogEntry
                    {
                        // Use the extracted values from the header for Profile, Domain, and IP
                        ProfileName = profileNameMatch.Success ? profileNameMatch.Groups[1].Value : "Unknown",
                        Domain = domainMatch.Success ? domainMatch.Groups[1].Value : "Unknown",
                        IpAddress = ipMatch.Success ? ipMatch.Groups[1].Value : "Unknown",

                        // Use the raw date part for On Date
                        Date = datePart
                    };

                    // Get the call status from the JSON response
                    logEntry.CallStatus = jsonElement.GetProperty("success").GetBoolean() ? "Success" : "Failure";

                    return logEntry;
                }
            }
            catch (JsonException ex)
            {
                Debug.WriteLine($"Error parsing log entry: {ex.Message}");
            }

            return null;
        }

        private string ExtractDatePart(string logHeader)
        {
            // Capture full date and time with any markers (Arabic or English AM/PM)
            var datePattern = @"(\d{1,2}/\d{1,2}/\d{4}\s\d{1,2}:\d{2}:\d{2}\s?[صمAMPMampm]*)";
            var dateMatch = Regex.Match(logHeader, datePattern, RegexOptions.IgnoreCase);

            return dateMatch.Success ? dateMatch.Groups[1].Value.Trim() : "Unknown Date";
        }



    }

    public class ProfileInfo
    {
        public string ProfileName { get; set; }  // FullName (Name + Domain)
        public string TimerStatus { get; set; }  // Running or Stopped
        public string RemainingTime { get; set; }  // Timer remaining time
        public string NextApiCallTime { get; set; }  // Next API call scheduled time
    }

}
