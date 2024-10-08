﻿using System.Collections.ObjectModel;
using Wpf.Ui.Controls;

namespace DDNS_Cloudflare_API.ViewModels.Windows
{
    public partial class MainWindowViewModel : ObservableObject
    {
        [ObservableProperty]
        private string _applicationTitle = "DDNS - Cloudflare API Script";

        [ObservableProperty]
        private ObservableCollection<object> _menuItems = new()
        {
            new NavigationViewItem()
            {
                Content = "Home",
                Icon = new SymbolIcon { Symbol = SymbolRegular.ContentSettings24},
                TargetPageType = typeof(Views.Pages.Home)
            },
            new NavigationViewItem()
            {
                Content = "Setup",
                Icon = new SymbolIcon { Symbol = SymbolRegular.ContentSettings24},
                TargetPageType = typeof(Views.Pages.SetupPage)
            },
            new NavigationViewItem()
            {
                Content = "Records",
                Icon = new SymbolIcon { Symbol = SymbolRegular.GroupList24},
                TargetPageType = typeof(Views.Pages.Records)
            },
            new NavigationViewItem()
            {
                Content = "Tutorial",
                Icon = new SymbolIcon { Symbol = SymbolRegular.NotebookQuestionMark24},
                TargetPageType = typeof(Views.Pages.Tutorial)
            }
        };

        [ObservableProperty]
        private ObservableCollection<object> _footerMenuItems = new()
        {
            new NavigationViewItem()
            {
                Content = "Log",
                Icon = new SymbolIcon { Symbol = SymbolRegular.DocumentTextClock24},
                TargetPageType = typeof(Views.Pages.Log)
            },
            new NavigationViewItem()
            {
                Content = "Settings",
                Icon = new SymbolIcon { Symbol = SymbolRegular.Settings24 },
                TargetPageType = typeof(Views.Pages.SettingsPage)
            }
        };

        [ObservableProperty]
        private ObservableCollection<MenuItem> _trayMenuItems = new()
        {
            new MenuItem { Header = "Show", Tag = "tray_home" }
        };

    }
}
