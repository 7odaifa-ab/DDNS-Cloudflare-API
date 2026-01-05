/*
 * Author: Hudaifa Abdullah
 * @7odaifa_ab
 * info@huimangtech.com
 *
 * This ViewModel manages the logic for the Home page in the DDNS Cloudflare API application.
 * It handles updating the profile statuses, managing timers, and interacting with the UI to show profile-related information.
 */

using CommunityToolkit.Mvvm.ComponentModel;
using DDNS_Cloudflare_API.Services;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Windows.Threading;

namespace DDNS_Cloudflare_API.ViewModels.Pages
{
    public partial class HomeViewModel : ObservableObject
    {
        private readonly ProfileTimerService _profileTimerService;
        private bool _isInitialized;
        private LogEntry? _lastLogEntry;

        [ObservableProperty]
        private ObservableCollection<ProfileStatus> profileStatuses;

        public event EventHandler<string>? ProfileTimerUpdated;
        public IRelayCommand RefreshCommand { get; }

        #region Properties

        // Tracks the last log entry for the UI
        public LogEntry? LastLogEntry
        {
            get => _lastLogEntry;
            set
            {
                _lastLogEntry = value;
                OnPropertyChanged(nameof(LastLogEntry));  // Notify UI about the change
            }
        }

        #endregion

        #region Constructor

        // Initializes the ViewModel, subscribes to events, and loads profile data
        public HomeViewModel(ProfileTimerService profileTimerService)
        {
            _profileTimerService = profileTimerService;

            // Subscribe to timer and status update events
            _profileTimerService.RemainingTimeUpdated += OnRemainingTimeUpdated;
            _profileTimerService.ProfileTimerUpdated += OnProfileTimerUpdated;

            ProfileStatuses = new ObservableCollection<ProfileStatus>();
            RefreshCommand = new RelayCommand(RefreshStatuses);

            LoadProfiles(); // Load profiles on startup
            _isInitialized = true;
        }

        #endregion

        #region Methods

        // Loads profiles from the ProfileTimerService and updates the UI
        private void LoadProfiles()
        {
            var profiles = _profileTimerService.GetProfileData();

            foreach (var profile in profiles)
            {
                bool isTimerRunning = _profileTimerService.GetProfileTimers().ContainsKey(profile.Key);
                string status = isTimerRunning ? "Running" : "Stopped";
                UpdateProfile(profile.Key, status);
            }
        }

        // Updates profile statuses based on timer states
        private void UpdateProfile(string profileName, string status)
        {
            var profileStatus = ProfileStatuses.FirstOrDefault(p => p.ProfileName == profileName);

            if (profileStatus == null)
            {
                profileStatus = new ProfileStatus { ProfileName = profileName };
                ProfileStatuses.Add(profileStatus);
            }

            profileStatus.Status = status;

            if (_profileTimerService.GetProfileTimers().ContainsKey(profileName))
            {
                var timer = _profileTimerService.GetProfileTimers()[profileName];
                profileStatus.RemainingTime = GetRemainingTime(timer);
                profileStatus.NextApiCallTime = GetNextApiCallTime(timer);
            }
            else
            {
                profileStatus.RemainingTime = "N/A";
                profileStatus.NextApiCallTime = "N/A";
            }

            OnPropertyChanged(nameof(ProfileStatuses));
        }

        // Gets the remaining time before the next API call
        private string GetRemainingTime(DispatcherTimer timer)
        {
            if (timer.Tag is DateTime lastRunTime)
            {
                TimeSpan timeLeft = timer.Interval - (DateTime.Now - lastRunTime);
                return timeLeft.ToString(@"hh\:mm\:ss");
            }
            return "N/A";
        }

        // Gets the next scheduled API call time
        private string GetNextApiCallTime(DispatcherTimer timer)
        {
            if (timer.Tag is DateTime lastRunTime)
            {
                DateTime nextCallTime = lastRunTime + timer.Interval;
                return nextCallTime.ToString("HH:mm:ss");
            }
            return "N/A";
        }

        // Handles updates when remaining time is updated for a profile
        private void OnRemainingTimeUpdated(object sender, (string profileName, TimeSpan remainingTime) e)
        {
            var profile = ProfileStatuses.FirstOrDefault(p => p.ProfileName == e.profileName);
            if (profile != null)
            {
                profile.RemainingTime = e.remainingTime.ToString(@"hh\:mm\:ss");
                OnPropertyChanged(nameof(profile.RemainingTime));
            }
        }

        // Handles profile timer status updates
        private void OnProfileTimerUpdated(string profileName, string status)
        {
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                UpdateProfile(profileName, status);
            });
        }

        // Refreshes the statuses in the ViewModel
        public void RefreshStatuses()
        {
            ProfileStatuses.Clear();
            LoadProfiles();
            Debug.WriteLine("Refreshing");
        }

        #endregion
    }

    // Model to hold individual profile statuses for display
    public class ProfileStatus : INotifyPropertyChanged
    {
        private string _remainingTime = string.Empty;
        private string _status = string.Empty;
        private string _nextApiCallTime = string.Empty;

        public string ProfileName { get; set; } = string.Empty;

        public string Status
        {
            get => _status;
            set
            {
                _status = value;
                OnPropertyChanged(nameof(Status));
            }
        }

        public string RemainingTime
        {
            get => _remainingTime;
            set
            {
                _remainingTime = value;
                OnPropertyChanged(nameof(RemainingTime));
            }
        }

        public string NextApiCallTime
        {
            get => _nextApiCallTime;
            set
            {
                _nextApiCallTime = value;
                OnPropertyChanged(nameof(NextApiCallTime));
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    // LogEntry model to track log details
    public class LogEntry
    {
        public string ProfileName { get; set; } = string.Empty;
        public string CallStatus { get; set; } = string.Empty;
        public string Domain { get; set; } = string.Empty;
        public string IpAddress { get; set; } = string.Empty;
        public string Date { get; set; } = string.Empty;
        public string RunningStatus { get; set; } = string.Empty;
    }
}
