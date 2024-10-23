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
    /// Interaction logic for RegisterWindow.xaml
    /// </summary>
    public partial class RegisterWindow : Window
    {
        private string _licenseKey;
        public bool activated;
        public RegisterWindow(string licenseKey)
        {
            InitializeComponent();

            _licenseKey = licenseKey;
            this.tb_licensekey.Text = licenseKey;
            activated = false;
        }

        private void OnOK(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;

            // decrypt product key
            string decryptedProductKey = LicenseKey.DecryptProductKey(tb_productkey.Text, true);
            string [] productKeyItems = decryptedProductKey.Split(' ');
            // check product key items
            if(productKeyItems.Length != 3)
            {
                MessageBox.Show("Product Key is not valid! Please Enter a valid Product Key!", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            // check if license key is equal
            if(productKeyItems[0].Equals(_licenseKey) != true)
            {
                MessageBox.Show("Product Key is not valid! Please Enter a valid Product Key!", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            // parse valid date
            DateTime validDate;
            if(DateTime.TryParse(productKeyItems[2], out validDate) != true)
            {
                MessageBox.Show("Product Key is not valid! Please Enter a valid Product Key!", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            // check valid date
            if(validDate < DateTime.Now)
            {
                MessageBox.Show("Your product key is expired. Please purchase new product key.", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            new TrialTimeManager().Activate(productKeyItems[2]);
            activated = true;
            MessageBox.Show("Thank you for activation!",
                    "Activated", MessageBoxButton.OK, MessageBoxImage.Information);
            Close();
        }

        private void OnCancel(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
