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
using Application = System.Windows.Application;

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
        public static T? GetService<T>()
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
            
            // Handle unhandled non-UI thread exceptions
            AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
        }


        /// <summary>
        /// Occurs when an exception is thrown by an application but not handled.
        /// </summary>


        // Constants for crash handling
        private const string LogFileName = "error.log";
        private const int MaxRestartAttempts = 3;
        private static int _restartAttempts = 0;
        private static readonly object _logLock = new object();

        private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            // Log the exception
            LogException(e.Exception);


            // Show a user-friendly message
            var result = System.Windows.MessageBox.Show(
                "An unexpected error occurred. Would you like to restart the application?\n\n" +
                "Click 'Yes' to restart or 'No' to close the application.",
                "Error",
                MessageBoxButton.YesNo,
                MessageBoxImage.Error);

            // Prevent default unhandled exception processing
            e.Handled = true;

            if (result == MessageBoxResult.Yes)
            {
                // Restart the application
                RestartApplication();
            }
            else
            {
                // Close the application
                if (Application.Current != null)
                    Application.Current.Shutdown(1);
                else
                    Environment.Exit(1);
            }
        }

        private void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            // Log the exception
            LogException(e.ExceptionObject as Exception);

            // For non-UI thread crashes, restart the application automatically
            RestartApplication();
        }

        private void LogException(Exception ex)
        {
            if (ex == null) return;

            try
            {
                string logDirectory = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "DDNS_Cloudflare_API");

                // Ensure the log directory exists
                Directory.CreateDirectory(logDirectory);


                string logPath = Path.Combine(logDirectory, LogFileName);
                string logMessage = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] " +
                                  $"Error: {ex.Message}\n" +
                                  $"Type: {ex.GetType().FullName}\n" +
                                  $"Stack Trace: {ex.StackTrace}\n" +
                                  $"Source: {ex.Source}\n" +
                                  "----------------------------------------\n";

                // Thread-safe file writing
                lock (_logLock)
                {
                    File.AppendAllText(logPath, logMessage);
                }

                Debug.WriteLine(logMessage);
            }
            catch (Exception logEx)
            {
                Debug.WriteLine($"Failed to log error: {logEx.Message}");
            }
        }

        private async void RestartApplication()
        {
            _restartAttempts++;

            // Check if we've exceeded maximum restart attempts
            if (_restartAttempts > MaxRestartAttempts)
            {
                string errorMessage = "The application has crashed multiple times and cannot restart automatically.\n" +
                                    "Please contact support with the error log at:\n" +
                                    $"{Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "DDNS_Cloudflare_API", LogFileName)}";

                System.Windows.MessageBox.Show(errorMessage, "Critical Error", MessageBoxButton.OK, MessageBoxImage.Error);

                if (Application.Current != null)
                    Application.Current.Shutdown(1);
                else
                    Environment.Exit(1);
                return;
            }


            try
            {
                // Get the full path to the currently running executable (.exe)
                string applicationPath = Process.GetCurrentProcess().MainModule.FileName;


                // Create a process start info to start the new instance
                var processStartInfo = new ProcessStartInfo(applicationPath)
                {
                    UseShellExecute = true,
                    Arguments = $"--restart-attempt {_restartAttempts}"
                };


                // Start the new instance
                Process.Start(processStartInfo);


                // Give it a moment to start
                await Task.Delay(1000);


                // Shutdown the current instance
                if (Application.Current != null)
                    Application.Current.Shutdown();
                else
                    Environment.Exit(0);
            }
            catch (Exception ex)
            {
                // Log the restart failure
                LogException(new Exception($"Failed to restart application. Attempt {_restartAttempts}/{MaxRestartAttempts}", ex));

                string errorMessage = "Failed to restart the application. " +
                                    $"Attempt {_restartAttempts}/{MaxRestartAttempts}.\n\n" +
                                    "Error: " + ex.Message;

                System.Windows.MessageBox.Show(errorMessage, "Restart Error", MessageBoxButton.OK, MessageBoxImage.Error);

                if (Application.Current != null)
                    Application.Current.Shutdown(1);
                else
                    Environment.Exit(1);
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
