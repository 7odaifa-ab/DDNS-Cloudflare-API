using System.Net.Http;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace DDNS_Cloudflare_API
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }


        private async void UpdateDnsRecordButton_Click(object sender, RoutedEventArgs e)
        {
            string zoneId = ZoneIdTextBox.Text;
            string dnsRecordId = DnsRecordIdTextBox.Text;
            string authEmail = AuthEmailTextBox.Text;
            string ipAddress = IpAddressTextBox.Text;
            string domainName = DomainNameTextBox.Text;

            string apiUrl = $"https://api.cloudflare.com/client/v4/zones/{zoneId}/dns_records/{dnsRecordId}";
            string jsonData = $@"
            {{
                ""content"": ""{ipAddress}"",
                ""name"": ""{domainName}"",
                ""proxied"": false,
                ""type"": ""A"",
                ""comment"": ""Domain verification record"",
                ""id"": ""{dnsRecordId}"",
                ""tags"": [""owner:dns-team""],
                ""ttl"": 3600
            }}";

            using (HttpClient client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("Content-Type", "application/json");
                client.DefaultRequestHeaders.Add("X-Auth-Email", authEmail);

                var content = new StringContent(jsonData, Encoding.UTF8, "application/json");

                try
                {
                    HttpResponseMessage response = await client.PutAsync(apiUrl, content);
                    response.EnsureSuccessStatusCode();
                    MessageBox.Show("DNS record updated successfully!");
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error updating DNS record: {ex.Message}");
                }
            }
        }
    }
}