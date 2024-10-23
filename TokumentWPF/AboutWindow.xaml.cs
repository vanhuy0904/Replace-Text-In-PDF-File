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
using System.Windows.Shapes;

namespace Tokument
{
    /// <summary>
    /// Interaction logic for AboutWindow.xaml
    /// </summary>
    public partial class AboutWindow : Window
    {
        public AboutWindow()
        {
            InitializeComponent();

            TrialTimeManager trialManager = new TrialTimeManager();
            int daysLeft = trialManager.Expired(out bool isTrial);
            string msg;
            if (isTrial == true || daysLeft < 5)
            {
                if(isTrial == true)
                    msg = string.Format("Your trial version will be expired in {0} days.", daysLeft);
                else
                    msg = string.Format("License Product Key applied.\nYour product version will be expired in {0} days.", daysLeft);
            }
            else
            {
                msg = string.Format("License Product Key applied.\nYour product version will be expired in {0} days.", daysLeft);
                this.button_activate.Visibility = Visibility.Collapsed;
            }
            this.label_status.Content = msg;
        }

        private void OnActivate(object sender, RoutedEventArgs e)
        {
            string licenseKey = LicenseKey.GetLicenseKey();
            RegisterWindow registerWnd = new RegisterWindow(licenseKey);
            if(registerWnd.ShowDialog() == true)
            {
                this.DialogResult = true;
                this.Close();
            }
        }
    }
}
