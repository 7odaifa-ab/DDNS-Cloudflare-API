using CommunityToolkit.Mvvm.ComponentModel;
using DDNS_Cloudflare_API.Services;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Windows.Threading;

namespace DDNS_Cloudflare_API.ViewModels.Pages
{
    public partial class DashboardViewModel : ObservableObject
    {
        private readonly ProfileTimerService _profileTimerService;
        private bool _isInitialized;

        [ObservableProperty]
        private ObservableCollection<ProfileStatus> profileStatuses;

        public event EventHandler<string> ProfileTimerUpdated;
        public IRelayCommand RefreshCommand { get; }

        public DashboardViewModel(ProfileTimerService profileTimerService)
        {
            _profileTimerService = profileTimerService;

            // Subscribe to events
            _profileTimerService.RemainingTimeUpdated += OnRemainingTimeUpdated;
            _profileTimerService.ProfileTimerUpdated += OnProfileTimerUpdated;

            ProfileStatuses = new ObservableCollection<ProfileStatus>();

            // Command for refreshing statuses
            RefreshCommand = new RelayCommand(RefreshStatuses);

            // Load profiles and mark initialization
            LoadProfiles();
            _isInitialized = true;
        }

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

        private string GetRemainingTime(DispatcherTimer timer)
        {
            if (timer.Tag is DateTime lastRunTime)
            {
                TimeSpan timeLeft = timer.Interval - (DateTime.Now - lastRunTime);
                return timeLeft.ToString(@"hh\:mm\:ss");
            }
            return "N/A";
        }

        private string GetNextApiCallTime(DispatcherTimer timer)
        {
            if (timer.Tag is DateTime lastRunTime)
            {
                DateTime nextCallTime = lastRunTime + timer.Interval;
                return nextCallTime.ToString("HH:mm:ss");
            }
            return "N/A";
        }

        private void OnRemainingTimeUpdated(object sender, (string profileName, TimeSpan remainingTime) e)
        {
            var profile = ProfileStatuses.FirstOrDefault(p => p.ProfileName == e.profileName);
            if (profile != null)
            {
                profile.RemainingTime = e.remainingTime.ToString(@"hh\:mm\:ss");
                OnPropertyChanged(nameof(profile.RemainingTime));
            }
        }

        private void OnProfileTimerUpdated(string profileName, string status)
        {
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                UpdateProfile(profileName, status);
            });
        }

        public void RefreshStatuses()
        {
            ProfileStatuses.Clear();
            LoadProfiles();
            Debug.WriteLine("Refreshing");
        }
    }

    public class ProfileStatus : INotifyPropertyChanged
    {
        private string _remainingTime;
        private string _status;
        private string _nextApiCallTime;

        public string ProfileName { get; set; }

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

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
