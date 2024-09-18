using CommunityToolkit.Mvvm.ComponentModel;
using DDNS_Cloudflare_API.Services;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;

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

            // Load existing profiles
            LoadProfiles();

            // Mark that the ViewModel is initialized
            _isInitialized = true;
        }
        private void OnProfileTimerUpdated(string profileName, string status)
        {
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                UpdateProfileStatus(profileName, status);
            });
        }

        private void UpdateProfileStatus(string profileName, string status)
        {
            var profileStatus = ProfileStatuses.FirstOrDefault(p => p.ProfileName == profileName);

            if (profileStatus != null)
            {
                profileStatus.Status = status;  // Update status
            }
            else
            {
                ProfileStatuses.Add(new ProfileStatus
                {
                    ProfileName = profileName,
                    Status = status
                });
            }

            // Notify UI about changes
            OnPropertyChanged(nameof(ProfileStatuses));
        }


        private void LoadProfiles()
        {
            var profiles = _profileTimerService.GetProfileData();

            foreach (var profile in profiles)
            {
                bool isTimerRunning = _profileTimerService.GetProfileTimers().ContainsKey(profile.Key);
                string status = isTimerRunning ? "Running" : "Stopped";

                ProfileStatuses.Add(new ProfileStatus
                {
                    ProfileName = profile.Key,
                    Status = status
                });
            }
        }
    
        // You can call this method to refresh the status at runtime when necessary
        public void RefreshStatuses()
        {
            ProfileStatuses.Clear();
            LoadProfiles();
        }
    }

    public class ProfileStatus
    {
        public string ProfileName { get; set; }
        public string Status { get; set; }
    }
}
