using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using DDNS_Cloudflare_API.ViewModels.Pages;
using System.Windows.Threading;
using Wpf.Ui.Controls;
using System.Collections.ObjectModel;

using System;
using System.Collections.ObjectModel;
using System.Windows.Threading;
using DDNS_Cloudflare_API.Services;

namespace DDNS_Cloudflare_API.Views.Pages
{
    public partial class Dashboard : INavigableView<DashboardViewModel>
    {
        // Observable collection to bind to the DataGrid
        public ObservableCollection<ProfileInfo> Profiles { get; set; }
        public DashboardViewModel ViewModel { get; }

        private readonly ProfileTimerService timerService;  // Use the injected service here

        public Dashboard(DashboardViewModel viewModel, ProfileTimerService timerService)  // Inject ProfileTimerService
        {
            InitializeComponent();
            Profiles = new ObservableCollection<ProfileInfo>();
            ViewModel = viewModel;  // Set ViewModel
            DataContext = ViewModel;  // Bind DataContext to the ViewModel

            this.timerService = timerService;  // Use the injected singleton instance
            InitializeProfiles();
        }

        private void InitializeProfiles()
        {
            var profileTimers = timerService.GetProfileTimers();

            foreach (var kvp in profileTimers)
            {
                string profileName = kvp.Key;
                DispatcherTimer timer = kvp.Value;

                Profiles.Add(new ProfileInfo
                {
                    ProfileName = profileName,
                    TimerStatus = "Running",
                    RemainingTime = GetRemainingTime(timer),
                    NextApiCallTime = GetNextApiCallTime(timer)
                });
            }
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
    }

    public class ProfileInfo
    {
        public string ProfileName { get; set; }
        public string TimerStatus { get; set; }
        public string RemainingTime { get; set; }
        public string NextApiCallTime { get; set; }
    }
}

