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

namespace ProductKeyGenerator
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

        private void OnClickGenerate(object sender, RoutedEventArgs e)
        {
            string licenseKey = tb_license.Text;
            int validDays;
            try
            {
                validDays = Convert.ToInt16(tb_days.Text);
            }
            catch (Exception)
            {
                validDays = 0;
            }
            if(validDays == 0)
            {
                MessageBox.Show("Please input vaild days.");
                return;
            }
            tb_productkey.Text = LicenseKey.GetProductKey(licenseKey, validDays);
        }

        private void OnClickCancel(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
