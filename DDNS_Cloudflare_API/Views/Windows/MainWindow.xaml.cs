using DDNS_Cloudflare_API.ViewModels.Windows;
using Wpf.Ui;
using Wpf.Ui.Abstractions;
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
        private NotifyIcon? trayIcon;
        private bool isRunning = false; // To track if the timer is running

        private readonly ProfileTimerService _profileTimerService;


        public MainWindow(
            MainWindowViewModel viewModel,
            INavigationService navigationService,
            IServiceProvider serviceProvider,
            ProfileTimerService timerService
        )
        {
            ViewModel = viewModel;
            DataContext = this;

            SystemThemeWatcher.Watch(this);

            InitializeComponent();
            RootNavigation.SetServiceProvider(serviceProvider);

            navigationService.SetNavigationControl(RootNavigation);

            // Get the full path to the icon file
            string iconPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "DDNS.ico");

            // Initialize system tray icon
            try
            {
                if (File.Exists(iconPath))
                {
                    trayIcon = new NotifyIcon
                    {
                        Icon = new Icon(iconPath),
                        Visible = false, // Initially hidden, shown only when minimized
                        Text = "DDNS Cloudflare API"
                    };
                }
                else
                {
                    Debug.WriteLine($"Icon file not found at: {iconPath}");
                    trayIcon = new NotifyIcon
                    {
                        Icon = SystemIcons.Application,
                        Visible = false,
                        Text = "DDNS Cloudflare API"
                    };
                }

                trayIcon.DoubleClick += TrayIcon_DoubleClick!;
                CreateContextMenu();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error initializing tray icon: {ex.Message}");
            }

            _profileTimerService = timerService;

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

        public void SetPageService(INavigationViewPageProvider pageProvider) => RootNavigation.SetServiceProvider(pageProvider as IServiceProvider ?? throw new ArgumentException("Page provider must be IServiceProvider"));

        public void ShowWindow() => Show();

        public void CloseWindow() => Close();

        #endregion INavigationWindow methods

        /// <summary>
        /// Raises the closed event.
        /// </summary>
        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            
            // Clean up tray icon resources
            if (trayIcon != null)
            {
                trayIcon.Visible = false;
                trayIcon.Dispose();
            }
            
            // Make sure that closing this window will begin the process of closing the application.
            Application.Current.Shutdown();
        }

        INavigationView INavigationWindow.GetNavigation()
        {
            throw new NotImplementedException();
        }

        public void SetServiceProvider(IServiceProvider serviceProvider) => RootNavigation.SetServiceProvider(serviceProvider);

        private void CreateContextMenu()
        {
            if (trayIcon == null) return;

            // Create WinForms context menu for tray icon
            trayIcon.ContextMenuStrip = new System.Windows.Forms.ContextMenuStrip();
            trayIcon.ContextMenuStrip.Items.Add("Show", null, (s, e) => ShowWindow());
            trayIcon.ContextMenuStrip.Items.Add("Exit", null, (s, e) => Application.Current.Shutdown());
        }

        private void TrayIcon_DoubleClick(object? sender, EventArgs e)
        {
            // Restore the window
            this.Show();
            this.WindowState = WindowState.Normal;
            this.Activate(); // Bring window to front
        }

        protected override void OnStateChanged(EventArgs e)
        {
            base.OnStateChanged(e);

            if (WindowState == WindowState.Minimized)
            {
                this.Hide(); // Hide the window
                if (trayIcon != null)
                {
                    trayIcon.Visible = true; // Show system tray icon
                }
            }
            else if (WindowState == WindowState.Normal || WindowState == WindowState.Maximized)
            {
                if (trayIcon != null)
                {
                    trayIcon.Visible = false; // Hide system tray icon when window is visible
                }
            }
        }

        public void SetRunningStatus(bool running)
        {
            isRunning = running;
            if (trayIcon != null)
            {
                trayIcon.Visible = running;
            }
        }

    }
}
