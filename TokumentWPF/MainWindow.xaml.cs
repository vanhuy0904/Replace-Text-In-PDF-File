using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
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

namespace Tokument
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    /// 
    public partial class MainWindow : Window
    {
        private string _template_file_path;
        private string _datasource_file_path;
        private string _result_file_path;
        private List<MRUItem> mRUItems;
        private string _currentProjectPath;
        private DataSource _dataSource = new DataSource();
        private bool _activated = false;
        private int _availableMaxStep;

        const int RECENT_FILE_COUNT = 5;

        public MainWindow()
        {
            InitializeComponent();
            CreateNewDocument();

            mRUItems = new List<MRUItem>();

            LoadRecentList();

            foreach (MRUItem item in mRUItems)
            {
                MenuItem mnRecent = new MenuItem();
                mnRecent.Header = item.CompactPath;
                mnRecent.Click += OnMenuClickRecentProject;
                this.menu_open_recent.Items.Add(mnRecent);
            }

            // check license
            _activated = true;// false;
            //string licenseKey = LicenseKey.GetLicenseKey();
            //TrialTimeManager trilaManager = new TrialTimeManager();
            //int daysLeft = trilaManager.Expired(out bool isTrial);
            //RegisterWindow registerWnd = new RegisterWindow(licenseKey);
            //string msg;
            //if (isTrial == true)
            //{
            //    if (daysLeft < 0)
            //        msg = "The trial version is now expired! Would you like to activate it now?";
            //    else
            //        msg = string.Format("You are using trial version, you have {0} days left " +
            //            "to Activate! Would you like to activate it now?", daysLeft);
            //    if (MessageBox.Show(msg, "Product key", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
            //    {
            //        registerWnd.ShowDialog();
            //    }
            //}
            //else
            //{
            //    if (daysLeft < 5)
            //    {
            //        if(daysLeft < 0)
            //            msg = string.Format("Your product version is now expired.\nWould you like to activate it now?");
            //        else
            //            msg = string.Format("Your product version will be expired in {0} days.\nWould you like to activate it now?", daysLeft);
            //        if (MessageBox.Show(msg, "Product key",
            //            MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
            //        {
            //            registerWnd.ShowDialog();
            //        }
            //    }
            //}

            //daysLeft = new TrialTimeManager().Expired(out bool trial);
            //if (daysLeft < 0)
            //    Environment.Exit(0);
            //else
            //    _activated = !trial;

            SplashScreen screen = new SplashScreen();
            screen.Show();
            Thread.Sleep(1500);
            screen.Close();
        }

        [DllImport("shlwapi.dll", CharSet = CharSet.Auto)]
        static extern bool PathCompactPathEx([Out] StringBuilder pszOut, string szPath, int cchMax, int dwFlags);
        private string PathShortener(string path, int length)
        {
            StringBuilder sb = new StringBuilder(length);
            PathCompactPathEx(sb, path, length, 0);
            return sb.ToString();
        }
        private void OpenProject(string projectPath)
        {
            Project project = new Project();
            if (!project.LoadProject(projectPath))
                return;

            CreateNewDocument();

            this.label_merge_status.Foreground = Brushes.Black;
            this.label_merge_status.Content = "Ready";
            this.label_result_file_name.Content = "";

            // update template file path
            this._template_file_path = project.TemplatePath;
            this.tb_template_path.Text = System.IO.Path.GetFileName(_template_file_path);
            // update data source file path
            this._datasource_file_path = project.DataSourcePath;
            this.tb_datasource_path.Text = System.IO.Path.GetFileName(_datasource_file_path);
            // update result format
            if (project.ResultFormat.ToLower() == "pdf")
                this.cbo_output_format.SelectedIndex = 0;
            else
                this.cbo_output_format.SelectedIndex = 1;
            // update result file name
            this.tb_output_file_name.Text = project.ResultName;
            // update output folder
            this.tb_output_path.Text = project.OutputFolder;

            _currentProjectPath = projectPath;

            // update recent list
            this.menu_open_recent.Items.Clear();

            string compactPath = PathShortener(projectPath, 32);
            MRUItem newItem = new MRUItem(compactPath, projectPath);
            int index = 0;
            foreach (MRUItem item in mRUItems)
            {
                if (item.CompactPath == newItem.CompactPath)
                    break;
                index++;
            }

            if (index != mRUItems.Count)
                mRUItems.RemoveAt(index);

            mRUItems.Insert(0, newItem);

            while (mRUItems.Count > RECENT_FILE_COUNT)
                mRUItems.RemoveAt(mRUItems.Count - 1);

            foreach (MRUItem item in mRUItems)
            {
                MenuItem mnRecent = new MenuItem();
                mnRecent.Header = item.CompactPath;
                mnRecent.Click += OnMenuClickRecentProject;
                this.menu_open_recent.Items.Add(mnRecent);
            }

            // update step and buttons
            _availableMaxStep = 2;
            this.button_step1.IsEnabled = true;
            this.rect_step1.Fill = Brushes.Black;
            this.rect_step1.Stroke = Brushes.Black;
            this.button_step2.IsEnabled = true;
            this.rect_step2.Fill = Brushes.Black;
            this.rect_step2.Stroke = Brushes.Black;
            this.button_step3.IsEnabled = true;

            // save recent list in file
            StreamWriter stringToWrite = new
                   StreamWriter(System.Environment.CurrentDirectory +
                   @"\Recent.txt");

            foreach (MRUItem item in mRUItems)
                stringToWrite.WriteLine($"{item.CompactPath};{item.FullPath}");

            stringToWrite.Flush();
            stringToWrite.Close();
        }

        private void SaveProject(string projectPath)
        {
            // check 
            Project project = new Project
            {
                TemplatePath = _template_file_path,
                DataSourcePath = _datasource_file_path,
                ResultFormat = this.cbo_output_format.Text.ToLower(),
                ResultName = this.tb_output_file_name.Text,
                OutputFolder = this.tb_output_path.Text
            };

            project.SaveProject(projectPath);
        }

        // get full project path from compact path
        public string ProjectFullPath(string compactPath)
        {
            foreach (MRUItem item in mRUItems)
            {
                if (item.CompactPath == compactPath)
                    return item.FullPath;
            }
            return "";
        }

        public bool LoadRecentList()
        {
            string recentFile = Environment.CurrentDirectory + @"\Recent.txt";
            if (!System.IO.File.Exists(recentFile))
                return false;

            mRUItems.Clear();
            string[] lines = System.IO.File.ReadAllLines(Environment.CurrentDirectory + @"\Recent.txt");
            foreach (string line in lines)
            {
                string[] paths = line.Split(';');
                mRUItems.Insert(0, new MRUItem(paths[0], paths[1]));
            }
            return true;
        }

        public void SaveRecentList(MenuItem recentMenu, string strPath)
        {
            recentMenu.Items.Clear();

            LoadRecentList();

            string compactPath = PathShortener(strPath, 32);
            MRUItem newItem = new MRUItem(compactPath, strPath);
            int index = 0;
            foreach (MRUItem item in mRUItems)
            {
                if (item.CompactPath == newItem.CompactPath)
                    break;
                index++;
            }

            if (index == mRUItems.Count)
                mRUItems.Insert(0, newItem);

            while (mRUItems.Count > RECENT_FILE_COUNT)
                mRUItems.RemoveAt(mRUItems.Count - 1);

            foreach (MRUItem item in mRUItems)
            {
                MenuItem mnRecent = new MenuItem();
                mnRecent.Header = item.CompactPath;
                mnRecent.Click += OnMenuClickRecentProject;
                recentMenu.Items.Add(mnRecent);
            }

            StreamWriter stringToWrite = new
               StreamWriter(System.Environment.CurrentDirectory +
               @"\Recent.txt");

            foreach (MRUItem item in mRUItems)
                stringToWrite.WriteLine($"{item.CompactPath};{item.FullPath}");

            stringToWrite.Flush();
            stringToWrite.Close();
        }

        private void CreateNewDocument()
        {
            _template_file_path = "";
            _datasource_file_path = "";
            _result_file_path = "";
            _currentProjectPath = "";

            this.label_header.Content = "Upload Your Template File";

            _availableMaxStep = 0;
            this.button_step2.IsEnabled = false;
            this.rect_step1.Fill = Brushes.White;
            this.rect_step1.Stroke = Brushes.White;
            this.button_step3.IsEnabled = false;
            this.rect_step2.Fill = Brushes.White;
            this.rect_step2.Stroke = Brushes.White;

            this.tab_control.SelectedIndex = 0;
            
            this.tb_template_path.Text = "";

            this.tb_datasource_path.Text = "";
            
            this.tb_output_file_name.Text = "";
            this.tb_output_path.Text = "";
            this.tb_output_file_name.Text = "";
            this.cbo_output_format.SelectedIndex = 0;

            this.label_merge_status.Content = "";
            this.label_result_file_name.Content = "";
            this.label_tag_replaced.Content = "";
            this.tb_missing_data.Text = "";

            this.button_open_result.Visibility = Visibility.Collapsed;
        }

        private void EnableNextStep(int nextStep)
        {
            int curStep = _availableMaxStep;
            if(_availableMaxStep < nextStep)
                _availableMaxStep = nextStep;
            switch(_availableMaxStep)
            {
                case 1:
                    this.rect_step1.Fill = Brushes.Black;
                    this.rect_step1.Stroke = Brushes.Black;
                    this.button_step2.IsEnabled = true;
                    // move next step window
                    if (_availableMaxStep != curStep)
                        OnClickStep2(null, null);
                    break;
                case 2:
                    this.rect_step2.Fill = Brushes.Black;
                    this.rect_step2.Stroke = Brushes.Black;
                    this.button_step3.IsEnabled = true;
                    // move next step window
                    if (_availableMaxStep != curStep)
                        OnClickStep3(null, null);
                    break;
                default:
                    break;
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);

            Application.Current.Shutdown();
        }

        private delegate void UpdateProgressBarDelegate(TemplateProcessor m, int setp, int maxStep);
        private void UpdateProgressBar(TemplateProcessor m, int step, int maxStep)   //subscriber class method
        {
            if (!Dispatcher.CheckAccess()) // CheckAccess returns true if you're on the dispatcher thread
            {
                Dispatcher.Invoke(new UpdateProgressBarDelegate(UpdateProgressBar), m, step, maxStep);
                return;
            }
            //if (step == 0)
            //{
            //    this.progressBar.Maximum = maxStep;
            //}
            //this.progressBar.Value = step;
        }

        private delegate void UpdateLogDelegate(TemplateProcessor m, string msg);
        private void UpdateLog(TemplateProcessor m, string msg)
        {
            if (!Dispatcher.CheckAccess()) // CheckAccess returns true if you're on the dispatcher thread
            {
                Dispatcher.Invoke(new UpdateLogDelegate(UpdateLog), m, msg);
                return;
            }
            //this.tb_missing_data.Text += (msg + Environment.NewLine);
            //this.tb_missing_data.Focus();
            //this.tb_missing_data.CaretIndex = this.tb_missing_data.Text.Length;
            //this.tb_missing_data.ScrollToEnd();
        }

        private delegate void UpdateStatusDelegate(TemplateProcessor m, bool success, int replaced, int total, List<string> missingTags);
        private void UpdateStatus(TemplateProcessor m, bool success, int replaced, int total, List<string> missingTags)
        {
            if (!Dispatcher.CheckAccess()) // CheckAccess returns true if you're on the dispatcher thread
            {
                Dispatcher.Invoke(new UpdateStatusDelegate(UpdateStatus), m, success, replaced, total, missingTags);
                return;
            }
            if (success == true)
            {
                this.label_merge_status.Foreground = Brushes.Green;
                this.label_merge_status.Content = "Data Merge Successful";
                this.label_result_file_name.Content = $"({this.tb_output_file_name.Text}.{cbo_output_format.Text.ToLower()})";
                this.label_tag_replaced.Content = $"{replaced}/{total}";
                _result_file_path = $"{this.tb_output_path.Text}\\{this.tb_output_file_name.Text}.{cbo_output_format.Text.ToLower()}";
                foreach (string tag in missingTags)
                    this.tb_missing_data.Text += (tag + Environment.NewLine);
                this.button_open_result.Visibility = Visibility.Visible;
            }
            else
            {
                this.label_merge_status.Foreground = Brushes.Red;
                this.label_merge_status.Content = "Data Merge Failed";
                this.label_result_file_name.Content = "";
                this.label_tag_replaced.Content = "";
                this.tb_missing_data.Text = "";
                this.button_open_result.Visibility = Visibility.Collapsed;
            }
            //EnableAllControls(true);
        }

        private delegate void UpdateDetectedCountDelegate(TemplateProcessor m, string templateType, int detectedCount);
        private void UpdateDetectedCount(TemplateProcessor m, string templateType, int detectedCount)
        {
            if (!Dispatcher.CheckAccess()) // CheckAccess returns true if you're on the dispatcher thread
            {
                Dispatcher.Invoke(new UpdateDetectedCountDelegate(UpdateDetectedCount), m, detectedCount);
                return;
            }
            //this.label_template_detected.Text = $"Total Tags Detected : {detectedCount}";
        }

        private void OnClickStep1(object sender, RoutedEventArgs e)
        {
            this.tab_control.SelectedIndex = 0;
            this.label_header.Content = "Upload Your Template File";
        }

        private void OnClickStep2(object sender, RoutedEventArgs e)
        {
            this.tab_control.SelectedIndex = 1;
            this.label_header.Content = "Upload Your Datasource File";
        }

        private void OnClickStep3(object sender, RoutedEventArgs e)
        {
            this.tab_control.SelectedIndex = 2;
            this.label_header.Content = "Generate Your Document";
        }

        private void OnPreviewDragOverTemplate(object sender, DragEventArgs e)
        {
            e.Handled = true;
        }

        private void OnDropTemplate(object sender, DragEventArgs e)
        {
            if (e.Data.GetData(DataFormats.FileDrop) is string[] files && files.Any())
            {
                string file_path = files.First();

                var ext = System.IO.Path.GetExtension(file_path);
                if (ext.Equals(".docx", StringComparison.CurrentCultureIgnoreCase) ||
                    ext.Equals(".pdf", StringComparison.CurrentCultureIgnoreCase))
                {
                    _template_file_path = file_path;
                    this.tb_template_path.Text = System.IO.Path.GetFileName(_template_file_path);
                    EnableNextStep(1);
                }
            }
        }

        private void OnClickBrowseTemplate(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dlg = new OpenFileDialog
            {
                Filter = "Template Files|*.pdf|Template Files|*.docx"
            };
            if (dlg.ShowDialog() == true)
            {
                _template_file_path = dlg.FileName;
                this.tb_template_path.Text = System.IO.Path.GetFileName(_template_file_path);
                EnableNextStep(1);
            }
        }

        private void OnPreviewDragOverDatasource(object sender, DragEventArgs e)
        {
            e.Handled = true;
        }

        private void OnDropDatasource(object sender, DragEventArgs e)
        {
            if (e.Data.GetData(DataFormats.FileDrop) is string[] files && files.Any())
            {
                string file_path = files.First();

                var ext = System.IO.Path.GetExtension(file_path);
                if (ext.Equals(".xlsx", StringComparison.CurrentCultureIgnoreCase) ||
                    ext.Equals(".xls", StringComparison.CurrentCultureIgnoreCase) ||
                    ext.Equals(".csv", StringComparison.CurrentCultureIgnoreCase))
                {
                    _datasource_file_path = file_path;
                    this.tb_datasource_path.Text = System.IO.Path.GetFileName(_datasource_file_path);
                    EnableNextStep(2);
                }
            }
        }

        private void OnClickBrowseDatasource(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dlg = new OpenFileDialog
            {
                Filter = "Excel Files|*.xlsx|Excel Files|*.xls|Excel Files|*.csv"
            };
            if (dlg.ShowDialog() == true)
            {
                _datasource_file_path = dlg.FileName;
                this.tb_datasource_path.Text = System.IO.Path.GetFileName(_datasource_file_path);
                EnableNextStep(2);
            }

        }

        private void OnBrowseOutputPath(object sender, MouseButtonEventArgs e)
        {
            using (var dialog = new System.Windows.Forms.FolderBrowserDialog())
            {
                System.Windows.Forms.DialogResult result = dialog.ShowDialog();
                if(dialog.SelectedPath != "")
                    this.tb_output_path.Text = dialog.SelectedPath;
            }
        }

        private void OnClickStartMerge(object sender, RoutedEventArgs e)
        {
            // check parameters
            if (tb_output_file_name.Text == ""
                || tb_output_path.Text == ""
                || _template_file_path == ""
                || _datasource_file_path == "")
            {
                MessageBox.Show("Please set all paths.", "Tokument",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            // check if datasource file exists
            if (!System.IO.File.Exists(_datasource_file_path))
            {
                MessageBox.Show("Please check data source file exists.", "Tokument",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            // check if template file exists
            if (!System.IO.File.Exists(_template_file_path))
            {
                MessageBox.Show("Please check template file exists.", "Tokument",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            // restrict trial version 
            if (_activated != true) // trial version
            {
                if (System.IO.Path.GetExtension(_template_file_path).ToLower() != ".docx"
                    || cbo_output_format.Text.ToLower() != "pdf")
                {
                    MessageBox.Show("Only Merging DOCX to PDF is available in trial version.", "Tokument",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
            }

            if(_dataSource.LoadDataSource(_datasource_file_path) != true)
            {
                MessageBox.Show("Loading datasource failed.", "Tokument",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // start merging
            TemplateProcessor processor = null;
            // check template format and start auto fill
            if (_template_file_path.EndsWith(".docx"))
            {
                processor = new DocProcessor();
            }
            else if (_template_file_path.EndsWith(".pdf"))
            {
                // check output format
                if (cbo_output_format.Text != "PDF")
                {
                    MessageBox.Show("Pdf template can be saved as only pdf.", "Tokument",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                processor = new PdfProcessor();
            }
            else
            {
                MessageBox.Show("Invalid template format.", "Tokument",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            this.label_merge_status.Foreground = Brushes.Black;
            this.label_merge_status.Content = "Processing...";
            this.label_result_file_name.Content = "";

            //EnableAllControls(false);

            // add delegates
            processor.progresser += UpdateProgressBar;
            processor.logger += UpdateLog;
            processor.successHandler += UpdateStatus;

            string output_floder = this.tb_output_path.Text;
            string resultName = this.tb_output_file_name.Text;
            string resultExt = cbo_output_format.Text.ToLower();
            // start auto fill thread
            Thread thread = new Thread(() =>
                processor.StartMerge(_dataSource, _template_file_path, output_floder, resultName, resultExt));
            thread.Start();
        }

        private void OnClickOpenResult(object sender, RoutedEventArgs e)
        {
            if (System.IO.File.Exists(_result_file_path) != true)
            {
                MessageBox.Show("Cannot find result file", "Tokument",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            System.Diagnostics.Process.Start(_result_file_path);
        }
        
        private void OnMenuClickNewDocument(object sender, RoutedEventArgs e)
        {
            CreateNewDocument();
        }
        private void OnMenuClickRecentProject(object sender, RoutedEventArgs e)
        {
            // get project full path
            MenuItem menuItem = (MenuItem)sender;
            string projectPath = ProjectFullPath(menuItem.Header.ToString());
            // open project
            OpenProject(projectPath);
        }
        private void OnMenuClickSave(object sender, RoutedEventArgs e)
        {
            if (_currentProjectPath == "")
            {
                SaveFileDialog dlg = new SaveFileDialog
                {
                    Filter = "Document Files|*.tok"
                };
                if (dlg.ShowDialog() == true)
                {
                    SaveProject(dlg.FileName);
                    SaveRecentList(this.menu_open_recent, dlg.FileName);
                }
            }
            else
            {
                SaveProject(_currentProjectPath);
                SaveRecentList(this.menu_open_recent, _currentProjectPath);
            }
        }

        private void OnMenuClickSaveAs(object sender, RoutedEventArgs e)
        {
            SaveFileDialog dlg = new SaveFileDialog
            {
                Filter = "Document Files|*.tok"
            };
            if (dlg.ShowDialog() == true)
            {
                SaveProject(dlg.FileName);
                SaveRecentList(this.menu_open_recent, dlg.FileName);
            }

        }

        private void OnMenuClickClose(object sender, RoutedEventArgs e)
        {
            Environment.Exit(0);
        }

        private void OnMenuClickOpenDocument(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dlg = new OpenFileDialog
            {
                Filter = "Document Files|*.tok"
            };
            if (dlg.ShowDialog() == true)
            {
                OpenProject(dlg.FileName);
            }

        }

        private void OnMenuClickHelp(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start("http://www.tokument.com");
        }

        private void OnMenuClickAbout(object sender, RoutedEventArgs e)
        {
            AboutWindow wnd = new AboutWindow();
            if (wnd.ShowDialog() == true)
                _activated = true;
        }
    }
}
