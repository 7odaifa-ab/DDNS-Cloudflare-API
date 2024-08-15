using DDNS_Cloudflare_API.ViewModels.Pages;
using System.IO;
using System.Windows.Documents;
using System.Windows.Navigation;
using Wpf.Ui.Controls;

namespace DDNS_Cloudflare_API.Views.Pages
{
    public partial class Log : INavigableView<DataViewModel>
    {
        public DataViewModel ViewModel { get; }

        public Log(DataViewModel viewModel)
        {
            ViewModel = viewModel;
            DataContext = this;

            InitializeComponent();

            LoadLog();
        }

        private void LoadLog()
        {
            // Correct path to your log file
            string logFolderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "DDNS_Cloudflare_API");
            string logFilePath = Path.Combine(logFolderPath, "Logs.txt"); // Ensure the file name is included

            // Check if the log file exists
            if (File.Exists(logFilePath))
            {
                // Read all lines from the log file
                var lines = File.ReadAllLines(logFilePath);

                // Reverse the lines and join them with new lines
                var reversedLines = lines.Reverse().ToArray();
                string logContent = string.Join(Environment.NewLine, reversedLines);

                txtLog.Text = logContent;
            }
            else
            {
                txtLog.Text = "Log file not found.";
            }
        }

        private void UpdateLogButton_Click(object sender, RoutedEventArgs e)
        {
            LoadLog(); // Refresh the log content
        }

    }
}
