using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Tokument
{
    public class Project
    {
        public Project()
        {
        }

        public bool LoadProject(string projectPath)
        {
            // check project file exists
            if (!File.Exists(projectPath))
            {
                MessageBox.Show("Cannot find project file", "Tokument",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }
            // read lines
            string[] lines = File.ReadAllLines(projectPath);
            if (lines.Length != 5)
            {
                MessageBox.Show("Invalid project file", "Tokument",
                     MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            // parse lines
            DataSourcePath = lines[0];
            TemplatePath = lines[1];
            ResultFormat = lines[2];
            ResultName = lines[3];
            OutputFolder = lines[4];

            return true;
        }

        public void SaveProject(string projectPath)
        {
            string lines = $"{DataSourcePath}\n{TemplatePath}\n{ResultFormat}\n{ResultName}\n{OutputFolder}";
            File.WriteAllText(projectPath, lines);
        }

        public string DataSourcePath { get; set; }
        public string TemplatePath { get; set; }
        public string ResultFormat { get; set; }
        public string ResultName { get; set; }
        public string OutputFolder { get; set; }
    }
}
