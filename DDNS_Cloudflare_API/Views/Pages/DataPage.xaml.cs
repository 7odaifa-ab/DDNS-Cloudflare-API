using DDNS_Cloudflare_API.ViewModels.Pages;
using Wpf.Ui.Controls;

namespace DDNS_Cloudflare_API.Views.Pages
{
    public partial class DataPage : INavigableView<DataViewModel>
    {
        public DataViewModel ViewModel { get; }

        public DataPage(DataViewModel viewModel)
        {
            ViewModel = viewModel;
            DataContext = this;

            InitializeComponent();
        }
    }
}
