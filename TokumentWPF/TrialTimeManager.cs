using System;
using System.Configuration;
using System.Linq;
using System.Windows;

namespace Tokument
{
    class TrialTimeManager
    {
        private const int trial_days = 5;
        public TrialTimeManager()
        {
            var appSettings = ConfigurationManager.AppSettings;
            if(appSettings["Date"] == null)
                SetNewDate();
        }

        /// Sets the new date +31 days add for trial.
        public void SetNewDate()
        {
            DateTime newDate = DateTime.Now.AddDays(trial_days+1);
            string validDate = newDate.ToShortDateString();

            var configFile = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            var settings = configFile.AppSettings.Settings;

            settings.Add("Date", Utils.Base64Encode(validDate));
            settings.Add("Activation", Utils.Base64Encode("ABC000000DEF"));
            //settings["Date"].Value = Utils.Base64Encode(validDate);
            //settings["Activation"].Value = Utils.Base64Encode("ABC000000DEF");

            configFile.Save(ConfigurationSaveMode.Modified);
            ConfigurationManager.RefreshSection(configFile.AppSettings.SectionInformation.Name);
        }

        /// Checks if expire or NOT.
        public int Expired(out bool isTrial)
        {
            var appSettings = ConfigurationManager.AppSettings;
            string d = Utils.Base64Decode(appSettings["Date"]);
            string d1 = Utils.Base64Decode(appSettings["Activation"]);

            if (d1.Equals("ABC111111DEF"))
                isTrial = false;
            else
                isTrial = true;

            DateTime till = DateTime.Parse(d);
            int leftDays = (till.Subtract(DateTime.Now)).Days;

            return leftDays;
        }

        public void Activate(string validDate)
        {
            var configFile = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            var settings = configFile.AppSettings.Settings;

            settings["Date"].Value = Utils.Base64Encode(validDate);
            settings["Activation"].Value = Utils.Base64Encode("ABC111111DEF");

            configFile.Save(ConfigurationSaveMode.Modified);
            ConfigurationManager.RefreshSection(configFile.AppSettings.SectionInformation.Name);
        }
    }
}
