using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Configuration;

using ConnectionKiller;

namespace HBR
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        readonly Dictionary<string, string> AppSettings = new ()
        {
            { "ProcessName", "Hearthstone" },
            { "TcpPorts", "3724, 1119" },
            { "MonitorId", "1" },
            { "OffsetRight", "20" },
            { "OffsetBottom", "60" }
        };

        public MainWindow()
        {
            InitializeComponent();

            btnClose.Click += BtnClose_Click;
            btnReco.Click += BtnReco_Click;

            if (!Administrator.isAdmin)
            {
                btnReco.IsEnabled = false;
                btnReco.Content = "Run as Administrator !";
            }

            this.Show();

            ReadAllSettings();

            SetWindowPosition();

        }

        void UpdateSettings(KeyValueConfigurationCollection settings)
        {
            foreach (string key in AppSettings.Keys)
            {
                if (settings.AllKeys.Contains(key))
                {
                    AppSettings[key] = settings[key].Value;
                }
            }
        }

        void ReadAllSettings()
        {
            var configFile = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            var settings = configFile.AppSettings.Settings;
            try
            {
                if (settings.Count > 0)
                {
                    UpdateSettings(settings);
                }
            }
            catch (ConfigurationErrorsException)
            {
                Console.WriteLine("Error while loading settings");
            }
        }

        private void SetWindowPosition()
        {
            var width = SystemParameters.PrimaryScreenWidth;
            var height = SystemParameters.PrimaryScreenHeight;
            if ("2".Equals(AppSettings["MonitorId"]))
            {
                width = SystemParameters.VirtualScreenWidth;
                height = SystemParameters.VirtualScreenHeight;
            }
            this.Top = height - this.Height - int.Parse(AppSettings["OffsetBottom"]);
            this.Left = width - this.Width - int.Parse(AppSettings["OffsetRight"]);
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void BtnReco_Click(object sender, RoutedEventArgs e)
        {
            var c = ConnectionManagement.SearchConnection(
                AppSettings["ProcessName"],
                AppSettings["TcpPorts"]);
            
            if (c is not null)
            {
                Console.WriteLine("Reconnect");
                ConnectionManagement.CloseConnection(c);
            }
        }
    }
}
