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

        [ObservableProperty]
        private ObservableCollection<ProfileStatus> profileStatuses;

        public DashboardViewModel(ProfileTimerService profileTimerService)
        {
            _profileTimerService = profileTimerService;

            // Subscribe to the ProfileTimerUpdated event
            _profileTimerService.ProfileTimerUpdated += OnProfileTimerUpdated;
            Debug.WriteLine("here subscribed to ProfileTimerUpdated event");

            // Initialize the ProfileStatuses list
            ProfileStatuses = new ObservableCollection<ProfileStatus>();

            Debug.WriteLine($"ProfileTimerService instance in ViewModel: {_profileTimerService.GetHashCode()}");


            // Load existing profiles
           // LoadProfiles();

            // Load initial profile statuses
            LoadInitialStatuses();
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

        private void OnProfileTimerUpdated(string profileName, string status)
        {
            var profileStatus = ProfileStatuses.FirstOrDefault(p => p.ProfileName == profileName);
            Debug.WriteLine($"**** i'm  trying here to update the profile staues");
            if (profileStatus != null)
            {
                profileStatus.Status = status;
                //profileStatus.NextRunTime = nextRunTime;
            }
            else
            {
                ProfileStatuses.Add(new ProfileStatus
                {
                    ProfileName = profileName,
                    Status = status,
                    //NextRunTime = nextRunTime
                });
            }

            // Notify UI about changes
            OnPropertyChanged(nameof(ProfileStatuses));
        }
        private void LoadInitialStatuses()
        {
            // Initialize the ProfileStatuses collection with some data
            foreach (var profileTimer in _profileTimerService.GetProfileTimers())
            {
                ProfileStatuses.Add(new ProfileStatus { ProfileName = profileTimer.Key, Status = "Stopped" });
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
