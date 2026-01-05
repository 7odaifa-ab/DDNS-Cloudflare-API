/*
 * Author: Hudaifa Abdullah
 * @7odaifa_ab
 * info@huimangtech.com
 *
 * This class defines the logic for the Tutorial page of the DDNS Cloudflare API application.
 * It displays instructional content on how to use the application and handles hyperlink navigation to external resources.
 */

using DDNS_Cloudflare_API.ViewModels.Pages;
using System.Windows.Navigation;
using Wpf.Ui.Controls;
using Wpf.Ui.Abstractions.Controls;

namespace DDNS_Cloudflare_API.Views.Pages
{
    public partial class Tutorial : INavigableView<TutorialViewModel>
    {
        public TutorialViewModel ViewModel { get; }

        #region Constructor

        // Initializes the Tutorial page with its ViewModel
        public Tutorial(TutorialViewModel viewModel)
        {
            InitializeComponent();
            ViewModel = viewModel;
        }

        #endregion

        #region Hyperlink Navigation

        // Handles hyperlink navigation to external URLs
        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = e.Uri.AbsoluteUri,
                UseShellExecute = true  // Ensures the link opens in the default browser
            });
            e.Handled = true;
        }

        #endregion
    }
}
