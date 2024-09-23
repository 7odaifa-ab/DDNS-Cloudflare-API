using DDNS_Cloudflare_API.Services;
using DDNS_Cloudflare_API.ViewModels.Pages;
using DDNS_Cloudflare_API.ViewModels.Windows;
using DDNS_Cloudflare_API.Views.Pages;
using DDNS_Cloudflare_API.Views.Windows;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Windows.Threading;
using Wpf.Ui;

namespace DDNS_Cloudflare_API
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App
    {
        // The.NET Generic Host provides dependency injection, configuration, logging, and other services.
        // https://docs.microsoft.com/dotnet/core/extensions/generic-host
        // https://docs.microsoft.com/dotnet/core/extensions/dependency-injection
        // https://docs.microsoft.com/dotnet/core/extensions/configuration
        // https://docs.microsoft.com/dotnet/core/extensions/logging
        private static readonly IHost _host = Host
            .CreateDefaultBuilder()
            .ConfigureAppConfiguration(c => { c.SetBasePath(Path.GetDirectoryName(Assembly.GetEntryAssembly()!.Location)); })
            .ConfigureServices((context, services) =>
            {
                services.AddHostedService<ApplicationHostService>();

                // Page resolver service
                services.AddSingleton<IPageService, PageService>();

                // Theme manipulation
                services.AddSingleton<IThemeService, ThemeService>();

                // TaskBar manipulation
                services.AddSingleton<ITaskBarService, TaskBarService>();

                // Service containing navigation, same as INavigationWindow... but without window
                services.AddSingleton<INavigationService, NavigationService>();

                // Main window with navigation
                services.AddSingleton<INavigationWindow, MainWindow>();
                services.AddSingleton<MainWindowViewModel>();


                services.AddSingleton<ProfileTimerService>();
                services.AddSingleton<HomeViewModel>();
                services.AddSingleton<Home>();
                services.AddSingleton<SetupPage>();
                services.AddSingleton<SetupViewModel>();
                services.AddSingleton<Log>();
                services.AddSingleton<Records>();
                services.AddSingleton<Tutorial>();
                services.AddSingleton<DataViewModel>();
                services.AddSingleton<SettingsPage>();
                services.AddSingleton<SettingsViewModel>();
                services.AddSingleton<TutorialViewModel>();
                services.AddSingleton<Tutorial>();
            }).Build();

        /// <summary>
        /// Gets registered service.
        /// </summary>
        /// <typeparam name="T">Type of the service to get.</typeparam>
        /// <returns>Instance of the service or <see langword="null"/>.</returns>
        public static T GetService<T>()
            where T : class
        {
            return _host.Services.GetService(typeof(T)) as T;
        }

        /// <summary>
        /// Occurs when the application is loading.
        /// </summary>
        private void OnStartup(object sender, StartupEventArgs e)
        {
            _host.Start();
            // Handle unhandled UI thread exceptions
            this.DispatcherUnhandledException += OnDispatcherUnhandledException;

            // Handle unhandled non-UI thread exceptions
            AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
        }


        /// <summary>
        /// Occurs when an exception is thrown by an application but not handled.
        /// </summary>


        private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            // Log the exception (to a file, cloud, etc.)
            LogException(e.Exception);

            // Show a user-friendly message
            System.Windows.MessageBox.Show("An unexpected error occurred. The application will restart.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);

            // Prevent default unhandled exception processing
            e.Handled = true;

            // Optionally restart the application
            RestartApplication();
        }

        private void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            // Log the exception (to a file, cloud, etc.)
            LogException(e.ExceptionObject as Exception);

            // If not UI thread, it's a non-UI crash, so restart the application
            RestartApplication();
        }

        private void LogException(Exception ex)
        {
            if (ex != null)
            {
                // Implement logging here (e.g., to a file)
                Debug.WriteLine($"Unhandled exception: {ex.Message}");
            }
        }

        private async void RestartApplication()
        {
            try
            {
                // Get the full path to the currently running executable (.exe)
                string applicationPath = Process.GetCurrentProcess().MainModule.FileName;

                // Create a process start info to start the new instance
                var processStartInfo = new ProcessStartInfo(applicationPath)
                {
                    UseShellExecute = true, // Use the shell to start the process
                    Arguments = ""          // Add arguments if needed
                };

                // Shutdown the current application immediately
                if (System.Windows.Application.Current != null)
                {
                    // Start a new instance after a slight delay to ensure shutdown
                    await Task.Delay(500); // Adjust the delay as needed
                    Process.Start(processStartInfo);

                    // Shutdown the current instance
                    System.Windows.Application.Current.Shutdown();
                }
                else
                {
                    // Fallback for when Application.Current is null
                    Environment.Exit(0);
                }
            }
            catch (Exception ex)
            {
                // Log any exceptions related to restarting
                Debug.WriteLine($"Failed to restart the application: {ex.Message}");
                System.Windows.MessageBox.Show("Failed to restart the application. Please restart manually.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }




        /// <summary>
        /// Occurs when the application is closing.
        /// </summary>
        private async void OnExit(object sender, ExitEventArgs e)
        {
            await _host.StopAsync();

            _host.Dispose();
        }

    }
}
