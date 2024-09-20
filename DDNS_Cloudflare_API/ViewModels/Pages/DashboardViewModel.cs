using CommunityToolkit.Mvvm.ComponentModel;
using DDNS_Cloudflare_API.Services;
using System.Collections.ObjectModel;
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

        public DashboardViewModel(ProfileTimerService profileTimerService)
        {
            _profileTimerService = profileTimerService;

            // Subscribe to the ProfileTimerUpdated event
            _profileTimerService.ProfileTimerUpdated += OnProfileTimerUpdated;

            // Initialize the ProfileStatuses list
            ProfileStatuses = new ObservableCollection<ProfileStatus>();

            RefreshCommand = new RelayCommand(RefreshStatuses);


            // Load existing profiles
            LoadProfiles();

            // Mark that the ViewModel is initialized
            _isInitialized = true;
        }

        public IRelayCommand RefreshCommand { get; }


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
                Debug.WriteLine(nextCallTime.ToString("HH:mm:ss"));

                return nextCallTime.ToString("HH:mm:ss");
            }
            return "N/A";
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

        private void OnProfileTimerUpdated(string profileName, string status)
        {
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                UpdateProfile(profileName, status);
            });
        }



        // You can call this method to refresh the status at runtime when necessary
        public void RefreshStatuses()
        {
            ProfileStatuses.Clear();
            LoadProfiles();
            Debug.WriteLine($"Refreshing");
        }
    }

    public class ProfileStatus
    {
        public string ProfileName { get; set; }
        public string Status { get; set; }
        public string RemainingTime { get; set; }  // Add this for remaining time
        public string NextApiCallTime { get; set; } // Add this for next API call time
    }

}
