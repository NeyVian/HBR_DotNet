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
        int TcpPort = 3724;
        int OffsetRight = 20;
        int OffsetBottom = 60;
        string ProcessName = "";

        public MainWindow()
        {
            InitializeComponent();

            btnClose.Click += BtnClose_Click;
            btnReco.Click += BtnReco_Click;

            if (!Administrator.isAdmin)
            { 
                btnReco.Background = Brushes.Red;
                btnReco.IsEnabled = false;
                btnReco.Content = "Run as Administrator !";
            }

            this.Show();

            ReadAllSettings();

            SetWindowPosition();

        }

        void UpdateTcpPort(KeyValueConfigurationCollection settings)
        {
            if (settings.AllKeys.Contains("TcpPort"))
            {
                int.TryParse(settings["TcpPort"].Value, out TcpPort);
            }
        }

        void UpdateOffsetRight(KeyValueConfigurationCollection settings)
        {
            if (settings.AllKeys.Contains("OffsetRight"))
            {
                int.TryParse(settings["OffsetRight"].Value, out OffsetRight);
            }
        }

        void UpdateOffsetBottom(KeyValueConfigurationCollection settings)
        {
            if (settings.AllKeys.Contains("OffsetBottom"))
            {
                int.TryParse(settings["OffsetBottom"].Value, out OffsetBottom);
            }
        }

        void UpdateProcessName(KeyValueConfigurationCollection settings)
        {
            if (settings.AllKeys.Contains("ProcessName"))
            {
                ProcessName = settings["ProcessName"].Value;
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
                    UpdateTcpPort(settings);
                    UpdateOffsetRight(settings);
                    UpdateOffsetBottom(settings);
                    UpdateProcessName(settings);
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
            this.Top = height - this.Height - OffsetBottom;
            this.Left = width - this.Width - OffsetRight;
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void BtnReco_Click(object sender, RoutedEventArgs e)
        {
            var c = ConnectionManagement.SearchConnection(ProcessName, TcpPort);
            if (!string.IsNullOrEmpty(c.remoteAddress))
            { 
                ConnectionManagement.CloseConnection(c);
            }
        }
    }
}
