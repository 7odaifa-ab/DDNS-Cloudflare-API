namespace DDNS_Cloudflare_API.ViewModels.Pages
{
    public partial class SetupViewModel : ObservableObject
    {
        [ObservableProperty]
        private int _counter = 0;

        [RelayCommand]
        private void OnCounterIncrement()
        {
            Counter++;
        }
    }
}
