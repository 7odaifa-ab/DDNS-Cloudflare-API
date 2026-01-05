/*
 * Author: Hudaifa Abdullah
 * @7odaifa_ab
 * info@huimangtech.com
 *
 * This class handles the logic for the Home Page of the DDNS Cloudflare API application.
 * It manages profile information, displays status, and processes logs to show recent API activity.
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using DDNS_Cloudflare_API.ViewModels.Pages;
using System.Windows.Threading;
using Wpf.Ui.Controls;
using System.Collections.ObjectModel;
using DDNS_Cloudflare_API.Services;
using System.Diagnostics;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Globalization;

namespace DDNS_Cloudflare_API.Views.Pages
{
    public partial class Home : INavigableView<HomeViewModel>
    {
        #region Fields

        // Observable collection for DataGrid binding
        public ObservableCollection<ProfileInfo> Profiles { get; set; }

        // ViewModel for the Home page
        public HomeViewModel ViewModel { get; }

        // Timer service to manage profile timers
        private readonly ProfileTimerService timerService;

        #endregion

        #region Constructor

        // Constructor initializes the ViewModel and binds data to the page
        public Home(HomeViewModel viewModel, ProfileTimerService timerService)
        {
            InitializeComponent();
            Profiles = new ObservableCollection<ProfileInfo>();
            ViewModel = viewModel;
            DataContext = ViewModel; // Bind ViewModel
            this.timerService = timerService; // Inject the timer service
            timerService.UpdateLastApiCallLogAction = UpdateLastApiCallLog; // Assign delegate
        }

        #endregion

        #region Event Handlers

        // Triggered when the page is loaded to refresh the dashboard and load the last API call log
        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            RefreshDashboard();
            UpdateLastApiCallLog();
        }

        #endregion

        #region Methods

        // Refresh the dashboard by calling the ViewModel method
        public void RefreshDashboard()
        {
            ViewModel.RefreshStatuses();
        }

        // Update the dashboard with the most recent API call log
        private void UpdateLastApiCallLog()
        {
            string logFolderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "DDNS_Cloudflare_API");
            string logFilePath = Path.Combine(logFolderPath, "Logs.txt");

            var logEntry = GetLastApiCall(logFilePath); // Parse log file
            if (logEntry != null)
            {
                // Check if the profile timer is running
                bool isRunning = timerService.IsProfileTimerRunning(logEntry.ProfileName);

                // Bind log entry data to the ViewModel for the UI
                ViewModel.LastLogEntry = new LogEntry
                {
                    ProfileName = logEntry.ProfileName,
                    CallStatus = logEntry.CallStatus,
                    Domain = logEntry.Domain,
                    IpAddress = logEntry.IpAddress,
                    Date = logEntry.Date,
                    RunningStatus = isRunning ? "Running" : "Stopped" // Set timer status
                };
            }
            else
            {
                Debug.WriteLine("No log entry found or unable to parse.");
            }
        }

        // Get the last API call entry from the log file
        public LogEntry? GetLastApiCall(string logFilePath)
        {
            if (!File.Exists(logFilePath))
            {
                Debug.WriteLine("Log file not found.");
                return null;
            }

            var lines = File.ReadAllLines(logFilePath);
            LogEntry lastEntry = null;

            foreach (var line in lines.Reverse()) // Read in reverse to get the last entry
            {
                if (line.Contains("Response: {"))
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

        // Parse a single log line to extract profile and API call details
        private LogEntry? ParseLogEntry(string logLine)
        {
            try
            {
                int jsonStartIndex = logLine.IndexOf("Response:");
                if (jsonStartIndex != -1)
                {
                    string headerPart = logLine.Substring(0, jsonStartIndex).Trim();

                    // Extract profile name, domain, and IP using regex
                    var profileNameMatch = Regex.Match(headerPart, @"Profile:\s([^,]+)");
                    var domainMatch = Regex.Match(headerPart, @"Domain:\s([^,]+)");
                    var ipMatch = Regex.Match(headerPart, @"IP:\s([^,]+)");

                    // Extract date from the header
                    string datePart = ExtractDatePart(headerPart);

                    // Extract JSON part after "Response:"
                    string jsonString = logLine.Substring(jsonStartIndex + 9).Trim();
                    var jsonElement = JsonSerializer.Deserialize<JsonElement>(jsonString);

                    var logEntry = new LogEntry
                    {
                        ProfileName = profileNameMatch.Success ? profileNameMatch.Groups[1].Value : "Unknown",
                        Domain = domainMatch.Success ? domainMatch.Groups[1].Value : "Unknown",
                        IpAddress = ipMatch.Success ? ipMatch.Groups[1].Value : "Unknown",
                        Date = datePart,
                        CallStatus = jsonElement.GetProperty("success").GetBoolean() ? "Success" : "Failure"
                    };

                    return logEntry;
                }
            }
            catch (JsonException ex)
            {
                Debug.WriteLine($"Error parsing log entry: {ex.Message}");
            }

            return null;
        }

        // Extract date part from the log header
        private string ExtractDatePart(string logHeader)
        {
            // Try multiple date patterns to handle different formats
            // Pattern 1: ISO format with AM/PM (e.g., "2026-01-05 1:38:09 PM")
            var datePattern1 = @"(\d{4}-\d{2}-\d{2}\s+\d{1,2}:\d{2}:\d{2}\s+[APap][Mm])";
            var dateMatch = Regex.Match(logHeader, datePattern1, RegexOptions.IgnoreCase);
            
            if (dateMatch.Success)
            {
                return dateMatch.Groups[1].Value.Trim();
            }
            
            // Pattern 2: ISO format 24-hour (e.g., "2026-01-05 13:38:09")
            var datePattern2 = @"(\d{4}-\d{2}-\d{2}\s+\d{1,2}:\d{2}:\d{2})";
            dateMatch = Regex.Match(logHeader, datePattern2);
            
            if (dateMatch.Success)
            {
                return dateMatch.Groups[1].Value.Trim();
            }
            
            // Pattern 3: US format with slashes and AM/PM (e.g., "1/5/2026 1:35:42 PM")
            var datePattern3 = @"(\d{1,2}/\d{1,2}/\d{4}\s+\d{1,2}:\d{2}:\d{2}\s+[APap][Mm])";
            dateMatch = Regex.Match(logHeader, datePattern3, RegexOptions.IgnoreCase);
            
            if (dateMatch.Success)
            {
                return dateMatch.Groups[1].Value.Trim();
            }
            
            // Pattern 4: US format with slashes 24-hour (e.g., "1/5/2026 13:35:42")
            var datePattern4 = @"(\d{1,2}/\d{1,2}/\d{4}\s+\d{1,2}:\d{2}:\d{2})";
            dateMatch = Regex.Match(logHeader, datePattern4);
            
            return dateMatch.Success ? dateMatch.Groups[1].Value.Trim() : "Unknown Date";
        }

        #endregion
    }

    // Data structure for holding profile info in the UI
    public class ProfileInfo
    {
        public string ProfileName { get; set; }
        public string TimerStatus { get; set; } // Running or Stopped
        public string RemainingTime { get; set; } // Time left for next API call
        public string NextApiCallTime { get; set; } // When the next API call is scheduled
    }
}
