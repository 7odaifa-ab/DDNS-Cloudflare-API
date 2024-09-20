using DDNS_Cloudflare_API.ViewModels.Windows;
using Wpf.Ui;
using Wpf.Ui.Appearance;
using Wpf.Ui.Controls;
using System;
using System.Windows;
using System.Windows.Forms; // For NotifyIcon
using System.Drawing; // For Icon
using Application = System.Windows.Application; // To avoid conflict with System.Windows.Forms.Application
using ContextMenu = System.Windows.Controls.ContextMenu; // To avoid conflict with System.Windows.Forms.ContextMenu
using MenuItem = System.Windows.Controls.MenuItem;
using DDNS_Cloudflare_API.Services;
using System.Diagnostics;
using System.IO; // To avoid conflict with System.Windows.Forms.MenuItem


namespace DDNS_Cloudflare_API.Views.Windows
{
    public partial class MainWindow : INavigationWindow
    {
        public MainWindowViewModel ViewModel { get; }
        private NotifyIcon trayIcon;
        private bool isRunning = false; // To track if the timer is running

        private readonly ProfileTimerService _profileTimerService;


        public MainWindow(
            MainWindowViewModel viewModel,
            IPageService pageService,
            INavigationService navigationService, ProfileTimerService timerService
        )
        {
            ViewModel = viewModel;
            DataContext = this;

            SystemThemeWatcher.Watch(this);

            InitializeComponent();
            SetPageService(pageService);

            navigationService.SetNavigationControl(RootNavigation);

            // Get the full path to the icon file
            string iconPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "DDNS.ico");

            // Initialize system tray icon
            trayIcon = new NotifyIcon
            {
                Icon = new Icon(iconPath), // Use the full path to your icon file
                Visible = true, // Initially visible
                Text = "DDNS Cloudflare API"
            };

            trayIcon.DoubleClick += TrayIcon_DoubleClick;


            CreateContextMenu();

            _profileTimerService = timerService;
            Debug.WriteLine($"ProfileTimerService instance in ViewModel: {timerService.GetHashCode()}");

            // Load startup settings when the MainWindow is loaded
            Loaded += MainWindow_Loaded;

        }
        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            var settingsFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "DDNS_Cloudflare_API", "startupSettings.json");
            await _profileTimerService.LoadStartupSettings(settingsFilePath);
        }

        #region INavigationWindow methods

        public INavigationView GetNavigation() => RootNavigation;

        public bool Navigate(Type pageType) => RootNavigation.Navigate(pageType);

        public void SetPageService(IPageService pageService) => RootNavigation.SetPageService(pageService);

        public void ShowWindow() => Show();

        public void CloseWindow() => Close();

        #endregion INavigationWindow methods

        /// <summary>
        /// Raises the closed event.
        /// </summary>
        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            trayIcon.Dispose(); // Clean up tray icon resources
            // Make sure that closing this window will begin the process of closing the application.
            Application.Current.Shutdown();
        }

        INavigationView INavigationWindow.GetNavigation()
        {
            throw new NotImplementedException();
        }

        public void SetServiceProvider(IServiceProvider serviceProvider)
        {
            throw new NotImplementedException();
        }

        private void CreateContextMenu()
        {
            ContextMenu contextMenu = new ContextMenu();

            MenuItem showMenuItem = new MenuItem
            {
                Header = "Show"
            };
            showMenuItem.Click += (s, e) => ShowWindow();

            MenuItem exitMenuItem = new MenuItem
            {
                Header = "Exit"
            };
            exitMenuItem.Click += (s, e) => Application.Current.Shutdown();

            contextMenu.Items.Add(showMenuItem);
            contextMenu.Items.Add(exitMenuItem);

            trayIcon.ContextMenuStrip = new System.Windows.Forms.ContextMenuStrip();
            trayIcon.ContextMenuStrip.Items.Add("Show", null, (s, e) => ShowWindow());
            trayIcon.ContextMenuStrip.Items.Add("Exit", null, (s, e) => Application.Current.Shutdown());
        }

        private void TrayIcon_DoubleClick(object sender, EventArgs e)
        {
            // Restore the window
            this.Show();
            this.WindowState = WindowState.Normal;
        }

        protected override void OnStateChanged(EventArgs e)
        {
            base.OnStateChanged(e);

            if (WindowState == WindowState.Minimized)
            {
                this.Hide(); // Hide the window
                trayIcon.Visible = true; // Show system tray icon
            }
            else if (WindowState == WindowState.Normal)
            {
                trayIcon.Visible = false; // Hide system tray icon
            }
        }

        public void SetRunningStatus(bool running)
        {
            isRunning = running;
            trayIcon.Visible = running;
        }

    }
}
