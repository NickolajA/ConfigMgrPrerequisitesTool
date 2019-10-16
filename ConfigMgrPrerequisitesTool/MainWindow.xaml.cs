using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Media;
using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using System.ComponentModel;
using System.DirectoryServices;
using System.Threading.Tasks;
using System.Collections;
using System.Data;
using System.Net;
using System.Data.SqlClient;
using System.Text.RegularExpressions;
using System.Windows.Data;
using System.Text;
using System.Windows.Markup;
using System.Globalization;
using System.Collections.Specialized;
using System.Net.NetworkInformation;
using System.Management;

namespace ConfigMgrPrerequisitesTool
{
    public partial class MainWindow : MetroWindow
    {
        //' Construct new class objects
        ScriptEngine scriptEngine = new ScriptEngine();
        FileSystem fileSystem = new FileSystem();
        DirectoryEngine activeDirectory = new DirectoryEngine();
        WebEngine webParser = new WebEngine();
        SqlEngine sqlEngine = new SqlEngine();

        //' Construct observable collections as datagrids item source
        private ObservableCollection<WindowsFeature> siteTypeCollection = new ObservableCollection<WindowsFeature>();
        private ObservableCollection<FileSystem> sitePreferenceFileCollection = new ObservableCollection<FileSystem>();
        private ObservableCollection<WindowsFeature> rolesCollection = new ObservableCollection<WindowsFeature>();
        private ObservableCollection<DirectoryEngine> directoryContainerCollection = new ObservableCollection<DirectoryEngine>();
        private ObservableCollection<WebEngine> collectionADKOnline = new ObservableCollection<WebEngine>();
        private ObservableCollection<WindowsFeature> collectionWSUS = new ObservableCollection<WindowsFeature>();

        //' Initialize global Settings section objects
        private PSCredential psCredentials = null;
        private SqlConnection sqlConnection = null;

        //' Construct dictionaries
        Dictionary<string, string> loadedADKversions = new Dictionary<string, string>();

        public MainWindow()
        {
            InitializeComponent();

            //' Set item source for data grids
            dataGridSiteType.ItemsSource = siteTypeCollection;
            dataGridSitePrefFile.ItemsSource = sitePreferenceFileCollection;
            dataGridRoles.ItemsSource = rolesCollection;
            dataGridADPermissions.ItemsSource = directoryContainerCollection;
            dataGridWSUSFeatures.ItemsSource = collectionWSUS;

            //' Set item source for combo boxes
            comboBoxADKOnlineVersion.ItemsSource = collectionADKOnline;

            //' Load data into controls
            LoadGridSitePreferenceFile();

            //' Handle events
            directoryContainerCollection.CollectionChanged += OnCollectionChanged;
        }

        public void OnCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Reset)
            {
                buttonADPermissionsConfigure.IsEnabled = false;
            }
        }

        /// <summary>
        ///  Based on parameter input, returns a string array object containing the Windows Features required per Site typ.
        /// </summary>
        private List<string> GetWindowsFeatures(string selection)
        {
            List<string> featureList = new List<string>();

            switch (selection)
            {
                case "Primary Site":
                    string[] primarySite = new string[] { "NET-Framework-45-Features", "NET-Framework-45-Core", "NET-Framework-Core", "BITS", "BITS-Compact-Server", "RDC", "UpdateServices-RSAT", "UpdateServices-API", "UpdateServices-UI" };
                    featureList.AddRange(primarySite);
                    break;
                case "Central Administration Site":
                    string[] centralAdminSite = new string[] { "NET-Framework-45-Features", "NET-Framework-45-Core", "NET-Framework-Core", "BITS", "BITS-Compact-Server", "RDC", "UpdateServices-RSAT", "UpdateServices-API", "UpdateServices-UI" };
                    featureList.AddRange(centralAdminSite);
                    break;
                case "Secondary Site":
                    string[] secondarySite = new string[] { "NET-Framework-Core", "BITS", "BITS-IIS-Ext", "BITS-Compact-Server", "RDC", "WAS-Process-Model", "WAS-Config-APIs", "WAS-Net-Environment", "Web-Server", "Web-ISAPI-Ext", "Web-Windows-Auth", "Web-Basic-Auth", "Web-URL-Auth", "Web-IP-Security", "Web-Scripting-Tools", "Web-Mgmt-Service", "Web-Metabase", "Web-WMI" };
                    featureList.AddRange(secondarySite);
                    break;
                case "Management Point":
                    string[] managementPoint = new string[] { "NET-Framework-Core", "NET-Framework-45-Features", "NET-Framework-45-Core", "NET-WCF-TCP-PortSharing45", "NET-WCF-Services45", "BITS", "BITS-IIS-Ext", "BITS-Compact-Server", "RSAT-Bits-Server", "Web-Server", "Web-WebServer", "Web-ISAPI-Ext", "Web-WMI", "Web-Metabase", "Web-Windows-Auth", "Web-ASP", "Web-Asp-Net", "Web-Asp-Net45" };
                    featureList.AddRange(managementPoint);
                    break;
                case "Distribution Point":
                    string[] distributionPoint = new string[] { "FS-FileServer", "RDC", "Web-WebServer", "Web-Common-Http", "Web-Default-Doc", "Web-Dir-Browsing", "Web-Http-Errors", "Web-Static-Content", "Web-Http-Redirect", "Web-Health", "Web-Http-Logging", "Web-Performance", "Web-Stat-Compression", "Web-Security", "Web-Filtering", "Web-Windows-Auth", "Web-App-Dev", "Web-ISAPI-Ext", "Web-Mgmt-Tools", "Web-Mgmt-Console", "Web-Mgmt-Compat", "Web-Metabase", "Web-WMI", "Web-Scripting-Tools" };
                    featureList.AddRange(distributionPoint);
                    break;
                case "Application Catalog":
                    string[] appCatalog = new string[] { "NET-Framework-Features", "NET-Framework-Core", "NET-HTTP-Activation", "NET-Non-HTTP-Activ", "NET-WCF-Services45", "NET-WCF-HTTP-Activation45", "RDC", "WAS", "WAS-Process-Model", "WAS-NET-Environment", "WAS-Config-APIs", "Web-Server", "Web-WebServer", "Web-Common-Http", "Web-Static-Content", "Web-Default-Doc", "Web-App-Dev", "Web-ASP-Net", "Web-ASP-Net45", "Web-Net-Ext", "Web-Net-Ext45", "Web-ISAPI-Ext", "Web-ISAPI-Filter", "Web-Security", "Web-Windows-Auth", "Web-Filtering", "Web-Mgmt-Tools", "Web-Mgmt-Console", "Web-Scripting-Tools", "Web-Mgmt-Compat", "Web-Metabase", "Web-Lgcy-Mgmt-Console", "Web-Lgcy-Scripting", "Web-WMI" };
                    featureList.AddRange(appCatalog);
                    break;
                case "State Migration Point":
                    string[] migrationPoint = new string[] { "Web-Server", "Web-Common-Http", "Web-Default-Doc", "Web-Dir-Browsing", "Web-Http-Errors", "Web-Static-Content", "Web-Http-Logging", "Web-Dyn-Compression", "Web-Filtering", "Web-Windows-Auth", "Web-Mgmt-Tools", "Web-Mgmt-Console" };
                    featureList.AddRange(migrationPoint);
                    break;
                case "Enrollment Point":
                    string[] enrollmentPoint = new string[] { "Web-Server", "Web-WebServer", "Web-Default-Doc", "Web-Dir-Browsing", "Web-Http-Errors", "Web-Static-Content", "Web-Http-Logging", "Web-Stat-Compression", "Web-Filtering", "Web-Net-Ext", "Web-Asp-Net", "Web-ISAPI-Ext", "Web-ISAPI-Filter", "Web-Mgmt-Console", "Web-Metabase", "NET-Framework-Core", "NET-Framework-Features", "NET-HTTP-Activation", "NET-Framework-45-Features", "NET-Framework-45-Core", "NET-Framework-45-ASPNET", "NET-WCF-Services45", "NET-WCF-TCP-PortSharing45" };
                    featureList.AddRange(enrollmentPoint);
                    break;
                case "Enrollment Proxy Point":
                    string[] enrollmentProxyPoint = new string[] { "Web-Server", "Web-WebServer", "Web-Default-Doc", "Web-Dir-Browsing", "Web-Http-Errors", "Web-Static-Content", "Web-Http-Logging", "Web-Stat-Compression", "Web-Filtering", "Web-Windows-Auth", "Web-Net-Ext", "Web-Net-Ext45", "Web-Asp-Net", "Web-Asp-Net45", "Web-ISAPI-Ext", "Web-ISAPI-Filter", "Web-Mgmt-Console", "Web-Metabase", "NET-Framework-Core", "NET-Framework-Features", "NET-Framework-45-Features", "NET-Framework-45-Core", "NET-Framework-45-ASPNET", "NET-WCF-Services45", "NET-WCF-TCP-PortSharing45" };
                    featureList.AddRange(enrollmentProxyPoint);
                    break;
                case "Certificate Registration Point":
                    string[] certificatePoint = new string[] { "NET-Framework-45-Features", "NET-Framework-45-Core", "NET-Framework-45-ASPNET", "NET-WCF-Services45", "NET-WCF-HTTP-Activation45", "NET-WCF-TCP-PortSharing45", "Web-Server", "Web-WebServer", "Web-Common-Http", "Web-Default-Doc", "Web-Dir-Browsing", "Web-Http-Errors", "Web-Static-Content", "Web-Health", "Web-Http-Logging", "Web-Performance", "Web-Stat-Compression", "Web-Security", "Web-Filtering", "Web-Mgmt-Tools", "Web-Mgmt-Console", "Web-Mgmt-Compat", "Web-Metabase", "Web-WMI", "Web-App-Dev", "Web-Net-Ext45", "Web-Asp-Net45", "Web-ISAPI-Ext", "Web-ISAPI-Filter", "WAS", "WAS-Process-Model", "WAS-Config-APIs" };
                    featureList.AddRange(certificatePoint);
                    break;
                case "WSUSWID":
                    string[] wsusWid = new string[] { "UpdateServices", "UpdateServices-WidDB", "UpdateServices-Services", "UpdateServices-RSAT", "UpdateServices-API", "UpdateServices-UI" };
                    featureList.AddRange(wsusWid);
                    break;
                case "WSUSSQL":
                    string[] wsusSql = new string[] { "UpdateServices-Services", "UpdateServices-RSAT", "UpdateServices-API", "UpdateServices-UI", "UpdateServices-DB" };
                    featureList.AddRange(wsusSql);
                    break;
            }

            return featureList;
        }

        private string GetADKDownloadURL(string selection)
        {
            WebEngine link = collectionADKOnline.FirstOrDefault(version => version.LinkName == selection);

            return link.LinkValue;
        }

        /// <summary>
        ///  Invokes a new process and blocks the code awaiting this method by constructing a new 
        ///  TaskCompletionSource, forcing the status of the underlying Task into the WaitingForActivation state.
        ///  When SetResult is called, state of the Task changes to Completed, resulting in awaiting code to continue.
        /// </summary>
        public Task RunProcessAsync(ProcessStartInfo processInfo)
        {
            //' Construct a new task completion source object to block awaiting code
            TaskCompletionSource<object> source = new TaskCompletionSource<object>();

            //' Construct the process object and handle exited event, when process has completed set result for task to null releasing the awaiting code to continue
            Process process = new Process { EnableRaisingEvents = true, StartInfo = processInfo };
            process.Exited += (sender, args) =>
            {
                if (process.ExitCode != 0)
                {
                    string errorMessage = process.StandardError.ReadToEnd();

                    if (!String.IsNullOrEmpty(errorMessage))
                    {
                        source.SetException(new InvalidOperationException(String.Format("The process did not exit correctly or was unexpectedly terminated. Refer to external log files or resources. Error message: {0}", process.StandardError.ReadToEnd())));
                    }
                    else
                    {
                        source.SetException(new InvalidOperationException("The process did not exit correctly or was unexpectedly terminated. Refer to external log files or resources."));
                    }
                }
                else
                {
                    source.SetResult(null);
                }

                //' Cleanup process object
                process.Dispose();
            };

            //' Invoke the process
            process.Start();

            return source.Task;
        }

        private void LoadGridSitePreferenceFile()
        {
            //' Clear existing items in observable collection
            if (dataGridSitePrefFile.Items.Count >= 1)
            {
                sitePreferenceFileCollection.Clear();
            }

            //' Get volumes and add to observable collection
            List<FileSystem> volumes = fileSystem.GetVolumeInfo();

            foreach (FileSystem volume in volumes)
            {
                sitePreferenceFileCollection.Add(new FileSystem { DriveSelected = false, VolumeLabel = volume.VolumeLabel, DriveName = volume.DriveName, DriveFreeSpace = volume.DriveFreeSpace });
            }
        }

        private string[] GetRemoteServers()
        {
            //' Construct string separator
            string[] separator = new string[] { "," };
            string[] remoteServers = null;

            //' Replace space chars and split
            if (textBoxRolesRemoteSystem.Text.Length >= 1)
            {
                remoteServers = textBoxRolesRemoteSystem.Text.Replace(" ", "").Split(separator, StringSplitOptions.RemoveEmptyEntries);
            }

            return remoteServers;
        }

        private string GetApplicationPath(string applicationName)
        {
            string applicationPath = string.Empty;

            using (OpenFileDialog browseDialog = new OpenFileDialog())
            {
                browseDialog.DefaultExt = ".exe";
                browseDialog.Filter = @"All Files|*.*|Executable|*.exe";
                browseDialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
                browseDialog.FilterIndex = 2;

                DialogResult dialogResult = browseDialog.ShowDialog();

                if (dialogResult == System.Windows.Forms.DialogResult.OK && !String.IsNullOrEmpty(browseDialog.FileName))
                {
                    if (browseDialog.FileName.ToUpper().Contains(applicationName))
                    {
                        applicationPath = browseDialog.FileName;
                    }
                    else
                    {
                        ShowMessageBox("WARNING", String.Format(@"Incorrect file selection. Please select {0} in SMSSETUP\BIN\X64.", applicationName));
                    }
                }
            }

            return applicationPath;
        }

        public uint GetProductType()
        {
            uint productType = 0;

            using (ManagementObjectSearcher searcher = new ManagementObjectSearcher("SELECT * FROM Win32_OperatingSystem"))
            {
                foreach (ManagementObject managementObject in searcher.Get())
                {
                    productType = (uint)managementObject.GetPropertyValue("ProductType");
                }
            }

            return productType;
        }

        async public Task DownloadFileAsync(string url, string location)
        {
            using (WebClient client = new WebClient())
            {
                client.DownloadProgressChanged += WebClient_DownloadProgressChanged;
                client.DownloadFileCompleted += WebClient_DownloadFileCompleted;
                await client.DownloadFileTaskAsync(new Uri(url), location);
            }
        }

        private void WebClient_DownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            progressBarADKOnline.Maximum = (int)e.TotalBytesToReceive / 100;
            progressBarADKOnline.Value = (int)e.BytesReceived / 100;
        }

        private void WebClient_DownloadFileCompleted(object sender, AsyncCompletedEventArgs e)
        {
            progressBarADKOnline.Value = 0;

            if (e.Error == null)
            {
                ShowMessageBox("SUCCESS", "Successfully downloaded the selected setup file. Installation will now continue.");
            }
        }

        private bool GetIsNetworkAvailable()
        {
            bool networkState = false;

            if (NetworkInterface.GetIsNetworkAvailable())
            {
                if (CanPingADKWebSite())
                {
                    networkState = true;
                }
            }

            return networkState;
        }

        private bool CanPingADKWebSite()
        {
            try
            {
                using (WebClient client = new WebClient())
                using (client.OpenRead("http://www.msftconnecttest.com/connecttest.txt"))
                    return true;
            }
            catch
            {
                return false;
            }
        }

        async public void ShowMessageBox(string title, string message)
        {
            //' Construct new metro dialog settings
            MetroDialogSettings settings = new MetroDialogSettings();
            settings.AffirmativeButtonText = "Continue";
            settings.ColorScheme = MetroDialogColorScheme.Theme;
            settings.DefaultButtonFocus = MessageDialogResult.Affirmative;
            settings.AnimateShow = true;

            MessageDialogResult dialogResult = await this.ShowMessageAsync(title, message, MessageDialogStyle.Affirmative, settings);
        }

        async public void ShowPlatformBox(string title, string message, bool shutdown = false)
        {
            //' Construct new metro dialog settings
            MetroDialogSettings settings = new MetroDialogSettings();
            settings.AffirmativeButtonText = "Close";
            settings.ColorScheme = MetroDialogColorScheme.Theme;
            settings.DefaultButtonFocus = MessageDialogResult.Affirmative;
            settings.AnimateShow = true;

            MessageDialogResult dialogResult = await this.ShowMessageAsync(title, message, MessageDialogStyle.Affirmative, settings);

            if (shutdown == true)
            {
                switch (dialogResult)
                {
                    case MessageDialogResult.Affirmative:
                        System.Windows.Application.Current.Shutdown();
                        break;
                }
            }
        }

        private ProcessStartInfo NewProcessStartInfo(string filePath, string arguments)
        {
            ProcessStartInfo processStartInfo = new ProcessStartInfo();
            processStartInfo.FileName = filePath;
            processStartInfo.Arguments = arguments;
            processStartInfo.UseShellExecute = false;
            processStartInfo.CreateNoWindow = true;
            processStartInfo.RedirectStandardError = true;

            return processStartInfo;
        }

        private bool IsTextNumeric(string input)
        {
            Regex regex = new Regex("^[0-9]+$");

            return regex.IsMatch(input);
        }

        private void MainWindow_ContentRendered(object sender, EventArgs e)
        {
            //' Check environment prerequisites
            uint productType = GetProductType();
            switch (productType)
            {
                case 0:
                    ShowPlatformBox("UNHANDLED ERROR", "Unable to detect platform product type from WMI. Application will now terminate.", true);
                    break;
                case 1:
                    ShowPlatformBox("UNSUPPORTED PLATFORM", "Unsupported platform detected. This application is not supported on a workstation. Application will now terminate.", true);
                    break;
                case 2:
                    ShowPlatformBox("WARNING", "Unsupported platform type detect. It's not recommended to run this application on a domain controller.");
                    break;
            }
        }

        private void ToggleSettingsSource_Click(object sender, RoutedEventArgs e)
        {
            if (toggleSettingsSource.IsChecked == true)
            {
                buttonSettingsSourceBrowse.IsEnabled = true;
                textBoxSettingsSource.IsEnabled = true;
            }
            else
            {
                buttonSettingsSourceBrowse.IsEnabled = false;
                textBoxSettingsSource.IsEnabled = false;
            }
        }

        private void SettingsCredsAdd_Click(object sender, RoutedEventArgs e)
        {
            if (!String.IsNullOrEmpty(textBoxSettingsCredsUserName.Text))
            {
                if (!String.IsNullOrEmpty(passwordBoxSettingsCredsPassword.Password))
                {
                    //' Construct new PSCredential
                    psCredentials = new PSCredential(textBoxSettingsCredsUserName.Text, passwordBoxSettingsCredsPassword.SecurePassword);

                    //' Handle UI elements
                    textBoxSettingsCredsUserName.IsEnabled = false;
                    passwordBoxSettingsCredsPassword.IsEnabled = false;
                    buttonSettingsCredsAdd.IsEnabled = false;
                    buttonSettingsCredsClear.IsEnabled = true;

                    ShowMessageBox("CREDENTIALS", "New credentials stored successfully.");
                }
                else
                {
                    ShowMessageBox("WARNING", "Please enter a password.");
                }
            }
            else
            {
                ShowMessageBox("WARNING", "Please enter a username.");
            }
        }

        private void SettingsCredsClear_Click(object sender, RoutedEventArgs e)
        {
            //' Clear existing credentials
            if (psCredentials != null)
            {
                psCredentials = null;
            }

            //' Clear controls
            passwordBoxSettingsCredsPassword.Password = null;
            textBoxSettingsCredsUserName.Text = null;

            //' Handle UI elements
            textBoxSettingsCredsUserName.IsEnabled = true;
            passwordBoxSettingsCredsPassword.IsEnabled = true;
            buttonSettingsCredsAdd.IsEnabled = true;
            buttonSettingsCredsClear.IsEnabled = false;
        }

        private void SettingsSourcesBrowse_Click(object sender, RoutedEventArgs e)
        {
            using (FolderBrowserDialog folderBrowse = new FolderBrowserDialog())
            {
                DialogResult dialogResult = folderBrowse.ShowDialog();

                if (dialogResult == System.Windows.Forms.DialogResult.OK && !String.IsNullOrEmpty(folderBrowse.SelectedPath))
                {
                    if (fileSystem.IsFolderEmpty(folderBrowse.SelectedPath) == false)
                    {
                        textBoxSettingsSource.Text = folderBrowse.SelectedPath;
                    }
                    else
                    {
                        ShowMessageBox("WARNING", @"Selected folder is empty, please select another source location.");
                    }
                }
            }
        }

        async private void SettingsSQLServerConnect_Click(object sender, RoutedEventArgs e)
        {
            //' Handle UI elements
            progressBarSettingsConnectionsSQLServer.IsIndeterminate = true;
            buttonSettingsConnectionsSQLServerConnect.IsEnabled = false;

            //' Construct new SqlConnect object
            sqlConnection = sqlEngine.NewSQLServerConnection(textBoxSettingsConnectionsSQLServerName.Text, textBoxSettingsConnectionsSQLServerInstance.Text);

            //' Attempt to connect to SQL server
            try
            {
                await sqlConnection.OpenAsync();

                if (sqlConnection.State == ConnectionState.Open)
                {
                    ShowMessageBox("SUCCESS", "Successfully established a connection to the specified SQL Server.");

                    //' Handle UI elements
                    textBoxSettingsConnectionsSQLServerName.IsEnabled = false;
                    textBoxSettingsConnectionsSQLServerInstance.IsEnabled = false;
                    buttonSettingsConnectionsSQLServerConnect.IsEnabled = false;
                    buttonSettingsConnectionsSQLServerClose.IsEnabled = true;
                }
                else
                {
                    buttonSettingsConnectionsSQLServerConnect.IsEnabled = true;
                }
            }
            catch (Exception ex)
            {
                ShowMessageBox("ERROR", String.Format("{0}", ex.Message));
                buttonSettingsConnectionsSQLServerConnect.IsEnabled = true;
            }

            //' Handle UI elements
            progressBarSettingsConnectionsSQLServer.IsIndeterminate = false;
        }

        private void SettingsSQLServerClose_Click(object sender, RoutedEventArgs e)
        {
            if (sqlConnection != null && sqlConnection.State == ConnectionState.Open)
            {
                sqlConnection.Close();

                //' Handle UI elements
                textBoxSettingsConnectionsSQLServerName.IsEnabled = true;
                textBoxSettingsConnectionsSQLServerInstance.IsEnabled = true;
                buttonSettingsConnectionsSQLServerConnect.IsEnabled = true;
                buttonSettingsConnectionsSQLServerClose.IsEnabled = false;
            }
        }

        async private void SiteTypeInstall_Click(object sender, RoutedEventArgs e)
        {
            //' Clear existing items from observable collection
            if (dataGridSiteType.Items.Count >= 1)
            {
                siteTypeCollection.Clear();
            }

            //' Get windows features for selected site type
            List<string> featureList = GetWindowsFeatures(comboBoxSiteType.SelectedItem.ToString());

            //' Update progress bar properties
            progressBarSiteType.Maximum = featureList.Count - 1;
            int progressBarValue = 0;
            labelSiteTypeProgress.Content = string.Empty;

            //' Process each windows feature for installation
            foreach (string feature in featureList)
            {
                //' Update progress bar
                progressBarSiteType.Value = progressBarValue++;
                labelSiteTypeProgress.Content = String.Format("{0} / {1}", progressBarValue, featureList.Count);

                //' Add new item for current windows feature installation state
                siteTypeCollection.Add(new WindowsFeature { Name = feature, Progress = true, Result = "Installing..." });
                dataGridSiteType.ScrollIntoView(siteTypeCollection[siteTypeCollection.Count - 1]);

                //' Invoke windows feature installation via PowerShell runspace
                object installResult = await scriptEngine.AddWindowsFeature(feature);
                string featureState = installResult.ToString();

                //' Update current row on data grid
                if (!String.IsNullOrEmpty(featureState))
                {
                    var currentCollectionItem = siteTypeCollection.FirstOrDefault(winFeature => winFeature.Name == feature);

                    if (featureState == "Failed")
                    {
                        if (checkBoxSiteTypeRetryFailed.IsChecked == true)
                        {
                            //' Invoke windows feature installation via PowerShell runspace with alternate source
                            currentCollectionItem.Result = "RetryWithSource";
                            object retryResult = await scriptEngine.AddWindowsFeature(feature, textBoxSettingsSource.Text);
                            featureState = retryResult.ToString();

                            if (featureState == "Failed")
                            {
                                featureState = "FailedAfterRetry";
                            }
                        }
                    }

                    //' Update datagrid elements
                    currentCollectionItem.Progress = false;
                    currentCollectionItem.Result = featureState;
                }

                //' Set color of progressbar
                // new prop needed for binding
            }

            //' Determine failed windows feature installations
            IEnumerable<WindowsFeature> failedFeatures = (dataGridSiteType.ItemsSource as IEnumerable<WindowsFeature>).Where(feature => feature.Result == "Failed");

            if (failedFeatures != null && failedFeatures.Count() >= 1)
            {
                checkBoxSiteTypeRetryFailed.IsEnabled = true;
            }

            switch (comboBoxSiteType.SelectedItem.ToString())
            {
                case "Primary Site":
                    ShowMessageBox("COMPLETED", String.Format("Windows Feature installation has completed for site type {0}. Remember to install Windows Features for Management Point and Distribution Point roles within this site either on this site server or a remote site server.", comboBoxSiteType.SelectedItem.ToString()));
                    break;
                case "Central Administration Site":
                    ShowMessageBox("COMPLETED", String.Format("Windows Feature installation has completed for site type {0}.", comboBoxSiteType.SelectedItem.ToString()));
                    break;
                case "Secondary Site":
                    ShowMessageBox("COMPLETED", String.Format("Windows Feature installation has completed for site type {0}.", comboBoxSiteType.SelectedItem.ToString()));
                    break;
            }
        }

        private void SiteTypeRetry_Checked(object sender, RoutedEventArgs e)
        {
            if (textBoxSettingsSource.Text.Length <= 5)
            {
                ShowMessageBox("WARNING", "Invalid alternate source location detected, please go to Settings and set the correct source location.");

                //' Clear checkbox selection
                System.Windows.Controls.CheckBox checkBox = sender as System.Windows.Controls.CheckBox;
                checkBox.IsChecked = false;
            }
        }

        async private void RolesInstall_Click(object sender, RoutedEventArgs e)
        {
            //' Clear existing items from observable collection
            if (dataGridRoles.Items.Count >= 1)
            {
                rolesCollection.Clear();
            }

            //' Determine whether remote connection or not
            if (radioButtonRolesRemote.IsChecked == true)
            {
                WSManConnectionInfo connectionInfo = null;
                Runspace runspace = null;

                //' Get windows features for selected site system role
                List<string> featureList = GetWindowsFeatures(comboBoxRolesSelection.SelectedItem.ToString());

                if (featureList != null && featureList.Count >= 1)
                {

                    //' Get list of servers to process
                    string[] remoteServers = GetRemoteServers();

                    if (remoteServers != null)
                    {
                        foreach (string remoteServer in remoteServers)
                        {
                            //' Determine whether to use alternate credentials or not
                            if (checkBoxRolesCreds.IsChecked == true)
                            {
                                if (psCredentials != null)
                                {
                                    connectionInfo = scriptEngine.NewWSManConnectionInfo(remoteServer, psCredentials);
                                }
                            }
                            else
                            {
                                connectionInfo = scriptEngine.NewWSManConnectionInfo(remoteServer, PSCredential.Empty);
                            }

                            //' Open a remote runspace using connection info
                            if (connectionInfo != null)
                            {
                                runspace = scriptEngine.NewRunspace(connectionInfo);
                                try
                                {
                                    runspace.Open();
                                }
                                catch (Exception ex)
                                {
                                    ShowMessageBox("ERROR", String.Format("{0}", ex.Message));
                                }
                            }

                            if (runspace.RunspaceStateInfo.State == RunspaceState.Opened)
                            {
                                //' Update progress bar properties
                                progressBarRoles.Maximum = featureList.Count - 1;
                                int progressBarValue = 0;
                                labelRolesProgress.Content = string.Empty;

                                foreach (string feature in featureList)
                                {
                                    //' Update progress bar
                                    progressBarRoles.Value = progressBarValue++;
                                    labelRolesProgress.Content = String.Format("{0} / {1}", progressBarValue, featureList.Count);

                                    //' Add new item for current windows feature installation state
                                    rolesCollection.Add(new WindowsFeature { Server = remoteServer, Name = feature, Progress = true, Result = "Installing..." });
                                    dataGridRoles.ScrollIntoView(rolesCollection[rolesCollection.Count - 1]);

                                    //' Invoke windows feature installation via PowerShell runspace
                                    object installResult = await scriptEngine.AddWindowsFeatureRemote(feature, runspace);

                                    //' Update current row on data grid
                                    if (!String.IsNullOrEmpty(installResult.ToString()))
                                    {
                                        var currentCollectionItem = rolesCollection.FirstOrDefault(winFeature => winFeature.Name == feature && winFeature.Server == remoteServer);
                                        currentCollectionItem.Progress = false;
                                        currentCollectionItem.Result = installResult.ToString();
                                    }
                                }

                                //' Cleanup runspace
                                runspace.Close();
                            }
                        }
                    }
                    else
                    {
                        ShowMessageBox("WARNING", "No remote servers was specified. Please specify at least one remote server.");
                    }
                }
            }

            if (radioButtonRolesLocal.IsChecked == true)
            {
                //' Get windows features for selected site system role
                List<string> featureList = GetWindowsFeatures(comboBoxRolesSelection.SelectedItem.ToString());

                if (featureList != null && featureList.Count >= 1)
                {
                    //' Update progress bar properties
                    progressBarRoles.Maximum = featureList.Count - 1;
                    int progressBarValue = 0;
                    labelRolesProgress.Content = string.Empty;

                    //' Get local computer name
                    string localComputer = System.Net.Dns.GetHostName();

                    foreach (string feature in featureList)
                    {
                        //' Update progress bar
                        progressBarRoles.Value = progressBarValue++;
                        labelRolesProgress.Content = String.Format("{0} / {1}", progressBarValue, featureList.Count);

                        //' Add new item for current windows feature installation state
                        rolesCollection.Add(new WindowsFeature { Server = localComputer, Name = feature, Progress = true, Result = "Installing..." });
                        dataGridRoles.ScrollIntoView(rolesCollection[rolesCollection.Count - 1]);

                        object installResult = null;

                        //' Invoke windows feature installation via PowerShell runspace
                        installResult = await scriptEngine.AddWindowsFeature(feature);

                        //' Update current row on data grid
                        if (!String.IsNullOrEmpty(installResult.ToString()))
                        {
                            var currentCollectionItem = rolesCollection.FirstOrDefault(winFeature => winFeature.Name == feature);
                            currentCollectionItem.Progress = false;
                            currentCollectionItem.Result = installResult.ToString();
                        }
                    }
                }
            }

            ShowMessageBox("COMPLETED", String.Format("Windows Feature installation has completed for site system role {0}.", comboBoxRolesSelection.SelectedItem.ToString()));
        }

        private void DataGrid_LoadingRow(object sender, DataGridRowEventArgs e)
        {
            DataGridRow row = e.Row;
            row.Background = (SolidColorBrush)new BrushConverter().ConvertFrom("#333337");
        }

        private void SitePrereqApplicationBrowse_Click(object sender, RoutedEventArgs e)
        {
            using (OpenFileDialog browseDialog = new OpenFileDialog())
            {
                browseDialog.DefaultExt = ".exe";
                browseDialog.Filter = @"All Files|*.*|Executable|*.exe";
                browseDialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
                browseDialog.FilterIndex = 2;

                DialogResult dialogResult = browseDialog.ShowDialog();

                if (dialogResult == System.Windows.Forms.DialogResult.OK && !String.IsNullOrEmpty(browseDialog.FileName))
                {
                    if (browseDialog.FileName.ToUpper().Contains("SETUPDL.EXE"))
                    {
                        textBoxSitePrereqBrowse.Text = browseDialog.FileName;
                    }
                    else
                    {
                        ShowMessageBox("WARNING", @"Incorrect file selection. Please select SETUPDL.EXE in SMSSETUP\BIN\X64.");
                    }
                }
            }
        }

        private void SitePrereqLocationBrowse_Click(object sender, RoutedEventArgs e)
        {
            using (FolderBrowserDialog folderBrowse = new FolderBrowserDialog())
            {
                DialogResult dialogResult = folderBrowse.ShowDialog();

                if (dialogResult == System.Windows.Forms.DialogResult.OK && !String.IsNullOrEmpty(folderBrowse.SelectedPath))
                {
                    if (fileSystem.IsFolderEmpty(folderBrowse.SelectedPath) == true)
                    {
                        textBoxSitePrereqDownload.Text = folderBrowse.SelectedPath;
                    }
                    else
                    {
                        ShowMessageBox("WARNING", @"Selected folder is not empty, please select another or create a new.");
                    }
                }
            }
        }

        async private void SitePrereqStart_Click(object sender, RoutedEventArgs e)
        {
            progressBarSitePrereq.IsIndeterminate = true;

            string arguments = "\"" + textBoxSitePrereqDownload.Text + "\"";

            ProcessStartInfo processStartInfo = new ProcessStartInfo();
            processStartInfo.FileName = textBoxSitePrereqBrowse.Text;
            processStartInfo.Arguments = arguments;
            processStartInfo.UseShellExecute = false;
            processStartInfo.CreateNoWindow = true;
            processStartInfo.RedirectStandardError = true;

            try
            {
                await RunProcessAsync(processStartInfo);
                ShowMessageBox("DOWNLOAD COMPLETE", "Successfully downloaded the Configuration Manager setup prerequisite files.");
            }
            catch (Exception ex)
            {
                ShowMessageBox("ERROR", String.Format("{0}", ex.Message));
            }

            progressBarSitePrereq.IsIndeterminate = false;
        }

        private void DataGridFilesSelectAll_Checked(object sender, RoutedEventArgs e)
        {
            foreach (System.Windows.Controls.CheckBox checkBox in VisualTreeHelpers.FindChildren<System.Windows.Controls.CheckBox>(dataGridSitePrefFile))
            {
                if (checkBox.IsChecked == false)
                {
                    checkBox.IsChecked = true;
                }
            }
        }

        private void DataGridFilesSelectAll_Unchecked(object sender, RoutedEventArgs e)
        {
            foreach (System.Windows.Controls.CheckBox checkBox in VisualTreeHelpers.FindChildren<System.Windows.Controls.CheckBox>(dataGridSitePrefFile))
            {
                if (checkBox.IsChecked == true)
                {
                    checkBox.IsChecked = false;
                }
            }
        }

        private void FilesDataGridCheckBox_OnChecked(object sender, RoutedEventArgs e)
        {
            System.Windows.Controls.CheckBox checkBox = (System.Windows.Controls.CheckBox)e.OriginalSource;
            DataGridRow dataGridRow = VisualTreeHelpers.FindAncestor<DataGridRow>(checkBox);

            FileSystem row = (FileSystem)dataGridRow.DataContext;

            switch (checkBox.IsChecked)
            {
                case true:
                    row.DriveSelected = true;
                    break;
                case false:
                    row.DriveSelected = false;
                    break;
            }

            e.Handled = true;
        }

        async private void SitePrefFilesCreate_Click(object sender, RoutedEventArgs e)
        {
            progressBarPref.IsIndeterminate = true;

            int volumeCount = 0;
            foreach (FileSystem volume in dataGridSitePrefFile.ItemsSource)
            {
                if (volume.DriveSelected == true)
                {
                    volumeCount++;
                    await fileSystem.WriteFileAsync(volume.DriveName, string.Empty);
                }
            }

            if (volumeCount >= 1)
            {
                ShowMessageBox("FILE CREATION", @"Successfully created a NO_SMS_ON_DRIVE.SMS file on the selected drives.");
            }

            progressBarPref.IsIndeterminate = false;
        }

        private void RolesRemoteSystem_Checked(object sender, RoutedEventArgs e)
        {
            if (textBoxRolesRemoteSystem != null)
            {
                textBoxRolesRemoteSystem.IsEnabled = true;
            }

            if (checkBoxRolesCreds != null)
            {
                checkBoxRolesCreds.IsEnabled = true;
            }
        }

        private void RolesLocalSystem_Checked(object sender, RoutedEventArgs e)
        {
            if (textBoxRolesRemoteSystem != null)
            {
                textBoxRolesRemoteSystem.IsEnabled = false;
            }

            if (checkBoxRolesCreds != null)
            {
                checkBoxRolesCreds.IsEnabled = false;
            }
        }

        private void RolesCreds_Checked(object sender, RoutedEventArgs e)
        {
            if (psCredentials == null)
            {
                ShowMessageBox("WARNING", "No alternate credentials was found, please go to Settings and define your credentials.");

                //' Clear checkbox selection
                System.Windows.Controls.CheckBox checkBox = sender as System.Windows.Controls.CheckBox;
                checkBox.IsChecked = false;
            }
        }

        private void DirectorySchemaCreds_Checked(object sender, RoutedEventArgs e)
        {
            if (psCredentials == null)
            {
                ShowMessageBox("WARNING", "No alternate credentials was found, please go to Settings and define your credentials.");

                //' Clear checkbox selection
                System.Windows.Controls.CheckBox checkBox = sender as System.Windows.Controls.CheckBox;
                checkBox.IsChecked = false;
            }
        }

        private void DirectorySchemaDetect_Click(object sender, RoutedEventArgs e)
        {
            string schemaMaster = string.Empty;

            //' Determine schema master
            if (activeDirectory.IsDomainUser())
            {
                try
                {
                    schemaMaster = activeDirectory.GetSchemaMasterRoleOwner();
                }
                catch (Exception ex)
                {
                    ShowMessageBox("ERROR", String.Format("{0}", ex.Message));
                }
            }
            else
            {
                ShowMessageBox("WARNING", "Current logged on user is not a domain account. Please login with a domain account to detect the Schema Master role owner.");
            }

            if (!String.IsNullOrEmpty(schemaMaster))
            {
                //' Update textBox UI control with detected schema master
                textBoxADSchemaServer.Text = schemaMaster;
                
                //' Handle UI button controls
                buttonADSchemaValidate.IsEnabled = false;
                buttonADSchemaExtend.IsEnabled = true;
            }
        }

        private void DirectorySchemaBrowse_Click(object sender, RoutedEventArgs e)
        {
            string applicationPath = GetApplicationPath("EXTADSCH.EXE");

            if (!String.IsNullOrEmpty(applicationPath))
            {
                textBoxADSchemaFile.Text = applicationPath;
                buttonADSchemaStage.IsEnabled = true;
            }
        }

        private void DirectorySchemaValidate_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                bool validationStatus = activeDirectory.ValidateSchemaMasterRoleOwner(textBoxADSchemaServer.Text);

                if (validationStatus == true)
                {
                    ShowMessageBox("SCHEMA MASTER", "Successfully validated specified domain controller as Schema Master role owner in the current forest.");
                    buttonADSchemaValidate.IsEnabled = false;
                    buttonADSchemaExtend.IsEnabled = true;
                }
                else
                {
                    ShowMessageBox("ERROR", "Specified server is not the Schema Master role owner in the current forest. Please specify the correct domain controller or use the automatically detection operation.");
                    buttonADSchemaExtend.IsEnabled = false;
                }
            }
            catch (Exception ex)
            {
                ShowMessageBox("ERROR", String.Format("{0}", ex.Message));
            }
        }

        private void DirectorySchemaDetect_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (textBoxADSchemaServer != null)
            {
                if (textBoxADSchemaServer.Text.Length >= 2)
                {
                    buttonADSchemaValidate.IsEnabled = true;
                    buttonADSchemaExtend.IsEnabled = false;
                }
                else
                {
                    buttonADSchemaValidate.IsEnabled = false;
                }
            }
        }

        async private void DirectorySchemaExtend_Click(object sender, RoutedEventArgs e)
        {
            //' Start progress bar
            progressBarADSchema.IsIndeterminate = true;

            try
            {
                WSManConnectionInfo connectionInfo = null;
                Runspace runspace = null;
                string remoteServer = textBoxADSchemaServer.Text;

                //' Determine whether to use alternate credentials or not
                if (checkBoxADSchemaCreds.IsChecked == true)
                {
                    if (psCredentials != null)
                    {
                        connectionInfo = scriptEngine.NewWSManConnectionInfo(remoteServer, psCredentials);
                    }
                }
                else
                {
                    connectionInfo = scriptEngine.NewWSManConnectionInfo(remoteServer, PSCredential.Empty);
                }

                //' Open a remote runspace using connection info
                if (connectionInfo != null)
                {
                    runspace = scriptEngine.NewRunspace(connectionInfo);
                    try
                    {
                        runspace.Open();
                    }
                    catch (Exception ex)
                    {
                        ShowMessageBox("PowerShell Remoting error", String.Format("{0}", ex.Message));
                    }
                }

                if (runspace.RunspaceStateInfo.State == RunspaceState.Opened)
                {
                    //' Run EXTADSCH.exe on schema master role owner domain controller
                    bool executionResult = await scriptEngine.StartProcessRemote(@"C:\extadsch.exe", runspace);

                    if (executionResult == false)
                    {
                        ShowMessageBox("SCHEMA EXTENSION", "Successfully extended Active Directory schema for Configuration Manager.");
                    }

                    //' Cleanup runspace
                    runspace.Close();
                }
            }
            catch (Exception ex)
            {
                ShowMessageBox("ERROR", String.Format("{0}", ex.Message));
            }

            //' Stop progress bar
            progressBarADSchema.IsIndeterminate = false;
        }

        async private void DirectorySchemaStage_Click(object sender, RoutedEventArgs e)
        {
            progressBarADSchemaStage.IsIndeterminate = true;

            //' Define variables for copy operation
            string fileName = "extadsch.exe";
            string localFile = textBoxADSchemaFile.Text;
            string remoteFile = String.Format(@"\\{0}\C$\{1}", textBoxADSchemaServer.Text, fileName);

            //' Copy the file to schema master admin share
            try
            {
                await fileSystem.CopyFileAsync(localFile, remoteFile, System.Threading.CancellationToken.None);
            }
            catch (Exception ex)
            {
                ShowMessageBox("STAGE ERROR", String.Format("{0}", ex.Message));
            }

            progressBarADSchemaStage.IsIndeterminate = false;
        }

        private void DirectoryContainerDetect_Click(object sender, RoutedEventArgs e)
        {
            string pdcEmulator = string.Empty;

            try
            {
                pdcEmulator = activeDirectory.GetPDCRoleOwner();
            }
            catch (Exception ex)
            {
                ShowMessageBox("ERROR", String.Format("{0}", ex.Message));
            }

            if (!String.IsNullOrEmpty(pdcEmulator))
            {
                //' Update textBox UI control
                textBoxADContainerServer.Text = pdcEmulator;

                //' Handle UI button controls
                buttonADContainerValidate.IsEnabled = false;
                buttonADContainerCreate.IsEnabled = true;
            }
        }

        private void DirectoryContainerValidate_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                bool validationStatus = activeDirectory.ValidatePDCRoleOwner(textBoxADContainerServer.Text);

                if (validationStatus == true)
                {
                    ShowMessageBox("PDC Emulator", "Successfully validated specified domain controller as PDC Emulator role owner in the current forest.");
                    buttonADContainerValidate.IsEnabled = false;
                    buttonADContainerCreate.IsEnabled = true;
                }
                else
                {
                    ShowMessageBox("ERROR", "Specified server is not the PDC Emulator role owner in the current forest. Please specify the correct domain controller or use the automatically detection operation.");
                    buttonADContainerCreate.IsEnabled = false;
                }
            }
            catch (Exception ex)
            {
                ShowMessageBox("ERROR", String.Format("{0}", ex.Message));
            }
        }

        private void DirectoryContainer_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (textBoxADContainerServer != null)
            {
                if (textBoxADContainerServer.Text.Length >= 2)
                {
                    buttonADContainerValidate.IsEnabled = true;
                    buttonADContainerCreate.IsEnabled = false;
                }
                else
                {
                    buttonADContainerValidate.IsEnabled = false;
                }
            }
        }

        private void DirectoryContainerCreds_Checked(object sender, RoutedEventArgs e)
        {
            if (psCredentials == null)
            {
                ShowMessageBox("WARNING", "No alternate credentials was found, please go to Settings and define your credentials.");

                //' Clear checkbox selection
                System.Windows.Controls.CheckBox checkBox = sender as System.Windows.Controls.CheckBox;
                checkBox.IsChecked = false;
            }
        }

        async private void DirectoryContainerCreate_Click(object sender, RoutedEventArgs e)
        {
            //' Check if System Management container exist
            if (activeDirectory.CheckSystemManagementContainer() == false)
            {
                //' Attempt to create System Management container
                WSManConnectionInfo connectionInfo = null;
                Runspace runspace = null;
                string remoteServer = activeDirectory.GetPDCRoleOwner();

                //' Determine whether to use alternate credentials or not
                if (checkBoxADContainerCreds.IsChecked == true)
                {
                    if (psCredentials != null)
                    {
                        connectionInfo = scriptEngine.NewWSManConnectionInfo(remoteServer, psCredentials);
                    }
                }
                else
                {
                    connectionInfo = scriptEngine.NewWSManConnectionInfo(remoteServer, PSCredential.Empty);
                }

                //' Open a remote runspace using connection info
                if (connectionInfo != null)
                {
                    runspace = scriptEngine.NewRunspace(connectionInfo);
                    try
                    {
                        runspace.Open();
                    }
                    catch (Exception ex)
                    {
                        ShowMessageBox("PowerShell Remoting error", String.Format("{0}", ex.Message));
                    }
                }

                if (runspace.RunspaceStateInfo.State == RunspaceState.Opened)
                {
                    try
                    {
                        bool result = await scriptEngine.NewADContainer(runspace);

                        if (result == false)
                        {
                            ShowMessageBox("SYSTEM MANAGEMENT CONTAINER", "Successfully created the System Management container.");
                        }
                    }
                    catch (Exception ex)
                    {
                        ShowMessageBox("ERROR", String.Format("{0}", ex.Message));
                    }

                    //' Cleanup runspace
                    runspace.Close();
                }
            }
            else
            {
                ShowMessageBox("SYSTEM MANAGEMENT CONTAINER", "System management container already exist.");
            }
        }

        private void DirectoryPermissionsSearch_Click(object sender, RoutedEventArgs e)
        {
            progressBarADPermissions.IsIndeterminate = true;

            //' Clear datagrid
            if (dataGridADPermissions.Items.Count >= 1)
            {
                directoryContainerCollection.Clear();
            }

            if (!String.IsNullOrEmpty(textBoxADPermissionsGroupSearch.Text))
            {
                //' Construct new background worker
                BackgroundWorker worker = new BackgroundWorker();
                worker.DoWork += WorkerDoWork_PermissionSearch;
                worker.RunWorkerCompleted += WorkerCompleted_PermissionSearch;

                //' Invoke background worker
                worker.RunWorkerAsync(textBoxADPermissionsGroupSearch.Text);
            }
            else
            {
                progressBarADPermissions.IsIndeterminate = false;
            }
        }

        private void WorkerDoWork_PermissionSearch(object sender, DoWorkEventArgs e)
        {
            //' Catch arguments from run worker
            string workerArgs = (string)e.Argument;

            //' Invoke active directory searcher
            List<DirectoryEngine> searchResults = activeDirectory.InvokeADSearcher(workerArgs);

            //' Return search results
            e.Result = searchResults;
        }

        private void WorkerCompleted_PermissionSearch(object sender, RunWorkerCompletedEventArgs e)
        {
            //' Collect search results
            List<DirectoryEngine> result = (List<DirectoryEngine>)e.Result;

            foreach (DirectoryEngine item in result)
            {
                directoryContainerCollection.Add(new DirectoryEngine { DisplayName = item.DisplayName, ObjectSelected = item.ObjectSelected, SamAccountName = item.SamAccountName, DistinguishedName = item.DistinguishedName });
            }

            if (directoryContainerCollection.Count >= 1)
            {
                buttonADPermissionsConfigure.IsEnabled = true;
            }

            progressBarADPermissions.IsIndeterminate = false;
        }

        private void DirectoryPermissionsDataGridCheckBox_OnChecked(object sender, RoutedEventArgs e)
        {
            System.Windows.Controls.CheckBox checkBox = (System.Windows.Controls.CheckBox)e.OriginalSource;
            DataGridRow dataGridRow = VisualTreeHelpers.FindAncestor<DataGridRow>(checkBox);
            DirectoryEngine row = (DirectoryEngine)dataGridRow.DataContext;

            switch (checkBox.IsChecked)
            {
                case true:
                    row.ObjectSelected = true;
                    break;
                case false:
                    row.ObjectSelected = false;
                    break;
            }

            e.Handled = true;
        }

        private void DirectoryPermissionsConfigure_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (directoryContainerCollection.Count >= 1)
                {
                    //' Get all selected rows
                    IEnumerable<DirectoryEngine> selectedRows = (dataGridADPermissions.ItemsSource as IEnumerable<DirectoryEngine>).Where(row => row.ObjectSelected == true);

                    if (selectedRows.ToList().Count >= 1)
                    {
                        //' Process all selected groups
                        foreach (DirectoryEngine row in dataGridADPermissions.ItemsSource)
                        {
                            if (row.ObjectSelected == true)
                            {
                                string groupSid = activeDirectory.GetADObjectSID(row.DistinguishedName);
                                bool result = activeDirectory.AddOrganizationalUnitACL(groupSid);

                                if (result == true)
                                {
                                    ShowMessageBox("SUCCESS", String.Format("Successfully added permissions for System Management container with Active Directory group {0}", row.DisplayName));
                                }
                                else
                                {
                                    ShowMessageBox("INFORMATION", String.Format("Active Directory group {0} is already added to the System Management container. Please verify the existing permissions.", row.DisplayName));
                                }
                            }
                        }
                    }
                    else
                    {
                        ShowMessageBox("EMPTY ITEM SELECTION", "Please select at least one item that should be added to the System Management container.");
                    }
                }
            }
            catch (Exception ex)
            {
                ShowMessageBox("ERROR", String.Format("{0}", ex.Message));
            }
        }

        private void ADKOnlineLoadVersions_Click(object sender, RoutedEventArgs e)
        {
            bool networkAvailable = GetIsNetworkAvailable();

            if (networkAvailable == true)
            {
                progressBarADKOnlineLoad.IsIndeterminate = true;

                if (comboBoxADKOnlineVersion.Items.Count >= 1)
                {
                    collectionADKOnline.Clear();
                }

                //' Construct new background worker
                BackgroundWorker worker = new BackgroundWorker();
                worker.DoWork += WorkerDoWork_ADKVersionOnlineSearch;
                worker.RunWorkerCompleted += WorkerCompleted_ADKVersionOnlineSearch;

                //' Invoke background worker
                worker.RunWorkerAsync();
            }
            else
            {
                ShowMessageBox("INTERNET CONNECTIVITY", "A connection to internet could not be established, use offline installation method instead.");
            }
        }

        private void WorkerDoWork_ADKVersionOnlineSearch(object sender, DoWorkEventArgs e)
        {
            List<WebEngine> links = new List<WebEngine>();

            //' Invoke web parser
            try
            {
                links = webParser.LoadWindowsADKFromXMLFeed();
            }
            catch (Exception ex)
            {
                throw new Exception(String.Format("Error occurred in background thread. Error message {0}", ex.Message));
            }

            //' Return search results
            e.Result = links;
        }

        private void WorkerCompleted_ADKVersionOnlineSearch(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Error == null)
            {
                //' Collect search results
                List<WebEngine> links = (List<WebEngine>)e.Result;

                if (links != null && links.Count >= 1)
                {
                    foreach (WebEngine link in links)
                    {
                        collectionADKOnline.Add(new WebEngine {
                            LinkName = link.LinkName,
                            LinkValue = link.LinkValue,
                            LinkType = link.LinkType
                        });
                    }

                    comboBoxADKOnlineVersion.SelectedIndex = 0;
                }
            }
            else
            {
                ShowMessageBox("ERROR", "Unable to parse Windows ADK feed for information. Make sure that you can access the following URL: http://www.scconfigmgr.com/windows-adk-feed.xml");
            }

            progressBarADKOnlineLoad.IsIndeterminate = false;
        }

        private void ADKOnlineLocationBrowse_Click(object sender, RoutedEventArgs e)
        {
            using (FolderBrowserDialog folderBrowse = new FolderBrowserDialog())
            {
                DialogResult dialogResult = folderBrowse.ShowDialog();

                if (dialogResult == System.Windows.Forms.DialogResult.OK && !String.IsNullOrEmpty(folderBrowse.SelectedPath))
                {
                    textBoxADKOnlineLocation.Text = folderBrowse.SelectedPath;
                }
            }
        }
        private void ADKOnlineLoad_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (comboBoxADKOnlineVersion.Items.Count >= 1)
            {
                buttonADKOnlineInstall.IsEnabled = true;
            }
        }

        private void ADKOnlineLocation_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (collectionADKOnline.Count >= 1)
            {
                if (Directory.Exists(textBoxADKOnlineLocation.Text))
                {
                    buttonADKOnlineInstall.IsEnabled = true;
                }
                else
                {
                    buttonADKOnlineInstall.IsEnabled = false;
                }
            }
        }

        async private void ADKOnlineInstall_Click(object sender, RoutedEventArgs e)
        {
            //' Get link object from observable collection
            WebEngine link = (WebEngine)comboBoxADKOnlineVersion.SelectedItem;

            //' Combine download location with file name and download
            string filePath = string.Empty;
            switch (link.LinkType)
            {
                case "Main":
                    filePath = Path.Combine(textBoxADKOnlineLocation.Text, "adksetup.exe");
                    break;
                case "Addon":
                    filePath = Path.Combine(textBoxADKOnlineLocation.Text, "adksetup_winpe.exe");
                    break;
            }

            try {
                await DownloadFileAsync(link.LinkValue, filePath);
            }
            catch (Exception ex)
            {
                ShowMessageBox("ERROR", String.Format("An error occured while downloading ADK setup bootstrap file. Error message: {0}", ex.Message));
            }

            //' Handle progress bar UI element
            progressBarADKOnline.IsIndeterminate = true;

            //' Invoke ADK setup bootstrap file if download successful
            if (File.Exists(filePath))
            {
                ProcessStartInfo processStartInfo = new ProcessStartInfo
                {
                    FileName = filePath,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardError = true
                };

                switch (link.LinkType)
                {
                    case "Main":
                        processStartInfo.Arguments = @"/norestart /quiet /ceip off /features OptionId.DeploymentTools OptionId.UserStateMigrationTool";
                        break;
                    case "Addon":
                        processStartInfo.Arguments = @"/norestart /quiet /ceip off /features OptionId.WindowsPreinstallationEnvironment";
                        break;
                }

                try
                {
                    await RunProcessAsync(processStartInfo);
                    ShowMessageBox("SUCCESS", "Successfully installed Windows ADK. Please restart the system to complete the installation.");
                }
                catch (Exception ex)
                {
                    ShowMessageBox("ERROR", String.Format("An error occured while installing Windows ADK. Error message: {0}", ex.Message));
                }
            }
            else
            {
                ShowMessageBox("FILE NOT FOUND", "Unable to locate ADK setup bootstrap file.");
            }

            //' Handle progress bar UI element
            progressBarADKOnline.IsIndeterminate = false;
        }

        private void ADKOfflineLocationBrowse_Click(object sender, RoutedEventArgs e)
        {
            using (FolderBrowserDialog folderBrowse = new FolderBrowserDialog())
            {
                DialogResult dialogResult = folderBrowse.ShowDialog();

                if (dialogResult == System.Windows.Forms.DialogResult.OK && !String.IsNullOrEmpty(folderBrowse.SelectedPath))
                {
                    textBoxADKOfflineLocation.Text = folderBrowse.SelectedPath;
                }
            }
        }

        private void ADKOfflineLocation_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (Directory.Exists(textBoxADKOfflineLocation.Text))
            {
                buttonADKOfflineInstall.IsEnabled = true;
            }
            else
            {
                buttonADKOfflineInstall.IsEnabled = false;
            }
        }

        async private void ADKOfflineInstall_Click(object sender, RoutedEventArgs e)
        {
            //' Handle progress bar UI element
            progressBarADKOffline.IsIndeterminate = true;

            string filePath = Path.Combine(textBoxADKOfflineLocation.Text, "adksetup.exe");

            //' Invoke ADK setup bootstrap file if download successful
            if (File.Exists(filePath))
            {
                ProcessStartInfo processStartInfo = new ProcessStartInfo();
                processStartInfo.FileName = filePath;
                processStartInfo.UseShellExecute = false;
                processStartInfo.CreateNoWindow = true;
                processStartInfo.RedirectStandardError = true;

                switch (comboBoxADKOfflineType.SelectedItem)
                {
                    case "Setup Installer":
                        processStartInfo.Arguments = @"/norestart /q /ceip off /features OptionId.DeploymentTools OptionId.UserStateMigrationTool";
                        break;
                    case "Setup Add-on":
                        processStartInfo.Arguments = @"/norestart /q /ceip off /features OptionId.WindowsPreinstallationEnvironment";
                        break;
                    case "Setup Legacy":
                        processStartInfo.Arguments = @"/norestart /q /ceip off /features OptionId.WindowsPreinstallationEnvironment OptionId.DeploymentTools OptionId.UserStateMigrationTool";
                        break;
                }

                try
                {
                    await RunProcessAsync(processStartInfo);
                    ShowMessageBox("SUCCESS", "Successfully installed Windows ADK. Please restart the system to complete the installation.");
                }
                catch (Exception ex)
                {
                    ShowMessageBox("ERROR", String.Format("An error occured while installing Windows ADK. Error message: {0}", ex.Message));
                }
            }
            else
            {
                ShowMessageBox("FILE NOT FOUND", "Unable to locate ADK setup bootstrap file.");
            }

            //' Handle progress bar UI element
            progressBarADKOffline.IsIndeterminate = false;
        }

        private void SQLServerGeneralMaxMemory_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (textBoxSQLGeneralMaxMemory != null && buttonSQLGeneralMemoryConfigure != null)
            {
                if (textBoxSQLGeneralMaxMemory.Text.Length >= 1)
                {
                    buttonSQLGeneralMemoryConfigure.IsEnabled = true;
                }
                else
                {
                    buttonSQLGeneralMemoryConfigure.IsEnabled = false;
                }
            }
        }

        private void SQLServerGeneralMinMemory_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (textBoxSQLGeneralMinMemory != null && buttonSQLGeneralMemoryConfigure != null)
            {
                if (textBoxSQLGeneralMinMemory.Text.Length >= 1)
                {
                    buttonSQLGeneralMemoryConfigure.IsEnabled = true;
                }
                else
                {
                    buttonSQLGeneralMemoryConfigure.IsEnabled = false;
                }
            }
        }

        async private void SQLServerGeneralMemoryConfigure_Click(object sender, RoutedEventArgs e)
        {
            //' Check if database initial size is of a numeric value
            if (IsTextNumeric(textBoxSQLGeneralMaxMemory.Text) == true && textBoxSQLGeneralMaxMemory.Text.Length >= 1)
            {
                if (IsTextNumeric(textBoxSQLGeneralMinMemory.Text) == true && textBoxSQLGeneralMinMemory.Text.Length >= 1)
                {
                    if (sqlConnection != null)
                    {
                        if (sqlConnection.State == ConnectionState.Open)
                        {
                            progressBarSQLMemory.IsIndeterminate = true;
                            int result = await sqlEngine.SetSQLServerMemory(sqlConnection, textBoxSQLGeneralMaxMemory.Text, textBoxSQLGeneralMinMemory.Text);

                            switch (result)
                            {
                                case 0:
                                    ShowMessageBox("SUCCESS", "Successfully configured the SQL Server memory settings.");
                                    break;
                                case 1:
                                    ShowMessageBox("ERROR", "An error occurred while configuring the SQL Server memory settings.");
                                    break;
                            }

                            progressBarSQLMemory.IsIndeterminate = false;
                        }
                        else
                        {
                            ShowMessageBox("CONNECTION ERROR", "Unable to detect an open SQL connection. Please go to Settings and connect to a SQL Server.");
                        }
                    }
                    else
                    {
                        ShowMessageBox("CONNECTION ERROR", "Unable to detect an open SQL connection. Please go to Settings and connect to a SQL Server.");
                    }
                }
                else
                {
                    ShowMessageBox("INVALID INPUT", "Please enter a numeric value for the minimum memory text field.");
                }
            }
            else
            {
                ShowMessageBox("INVALID INPUT", "Please enter a numeric value for the maximum memory text field.");
            }
        }

        async private void SQLServerCollationValidate_Click(object sender, RoutedEventArgs e)
        {
            if (sqlConnection != null)
            {
                if (sqlConnection.State == ConnectionState.Open)
                {
                    string collation = await sqlEngine.GetSQLInstanceCollation(sqlConnection);

                    if (collation == "SQL_Latin1_General_CP1_CI_AS")
                    {
                        iconSQLCollationSuccess.Visibility = Visibility.Visible;
                    }
                    else
                    {
                        iconSQLCollationFailure.Visibility = Visibility.Visible;
                    }
                    textBoxSQLCollation.Text = collation;
                }
            }
            else
            {
                ShowMessageBox("CONNECTION ERROR", "Unable to detect an open SQL connection. Please go to Settings and connect to a SQL Server.");
            }
        }

        private void SQLServerDatabaseCMSiteCode_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (textBoxSQLDatabaseCMSiteCode != null && buttonSQLDatabaseCMCreate != null)
            {
                if (textBoxSQLDatabaseCMSiteCode.Text.Length == 3)
                {
                    buttonSQLDatabaseCMCreate.IsEnabled = true;
                }
                else
                {
                    buttonSQLDatabaseCMCreate.IsEnabled = false;
                }
            }
        }

        private void SQLServerDatabaseCMInitDBSize_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (textBoxSQLDatabaseCMInitSize != null && buttonSQLDatabaseCMCreate != null)
            {
                if (textBoxSQLDatabaseCMInitSize.Text.Length >= 1)
                {
                    buttonSQLDatabaseCMCreate.IsEnabled = true;
                }
                else
                {
                    buttonSQLDatabaseCMCreate.IsEnabled = false;
                }
            }
        }

        private void SQLServerDatabaseCMInitLogSize_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (textBoxSQLDatabaseCMInitLogSize != null && buttonSQLDatabaseCMCreate != null)
            {
                if (textBoxSQLDatabaseCMInitLogSize.Text.Length >= 1)
                {
                    buttonSQLDatabaseCMCreate.IsEnabled = true;
                }
                else
                {
                    buttonSQLDatabaseCMCreate.IsEnabled = false;
                }
            }
        }

        async private void SQLServerDatabaseCMCreate_Click(object sender, RoutedEventArgs e)
        {
            //' Check if database initial size is of a numeric value
            if (IsTextNumeric(textBoxSQLDatabaseCMInitSize.Text) == true)
            {
                if (IsTextNumeric(textBoxSQLDatabaseCMInitLogSize.Text) == true)
                {
                    if (textBoxSQLDatabaseCMSiteCode.Text.Length == 3)
                    {
                        if (sqlConnection != null)
                        {
                            if (sqlConnection.State == ConnectionState.Open)
                            {
                                progressBarSQLDatabaseCM.IsIndeterminate = true;

                                try
                                {
                                    int returnValue = await sqlEngine.NewCMDatabase(sqlConnection, textBoxSQLDatabaseCMSiteCode.Text, comboBoxSQLDatabaseCMSplit.SelectedItem.ToString(), textBoxSQLDatabaseCMInitSize.Text, textBoxSQLDatabaseCMInitLogSize.Text);

                                    switch (returnValue)
                                    {
                                        case 0:
                                            ShowMessageBox("SUCCESS", "Successfully created the Configuration Manager database.");
                                            break;
                                        case 1:
                                            ShowMessageBox("ERROR", "An unhandled error occured while creating the Configuration Manager database.");
                                            break;
                                        case 2:
                                            ShowMessageBox("WARNING", "A database already exists with the given name, specify another Site Code.");
                                            break;
                                    }
                                }
                                catch (Exception ex)
                                {
                                    ShowMessageBox("ERROR", String.Format("An unhandled error occured while creating the Configuration Manager database. Error message: {0}", ex.Message));
                                }

                                progressBarSQLDatabaseCM.IsIndeterminate = false;
                            }
                        }
                        else
                        {
                            ShowMessageBox("CONNECTION ERROR", "Unable to detect an open SQL connection. Please go to Settings and connect to a SQL Server.");
                        }
                    }
                    else
                    {
                        ShowMessageBox("INVALID INPUT", "Please enter a valid Site Code by using 3 alphanumerics characters.");
                    }
                }
                else
                {
                    ShowMessageBox("INVALID INPUT", "Please enter a numeric value for the initial size of the Configuration Manager database log files.");
                }
            }
            else
            {
                ShowMessageBox("INVALID INPUT", "Please enter a numeric value for the initial size of the Configuration Manager database.");
            }
        }

        private void SQLServerSSRSReportServerDB_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (textBoxSQLSSRSReportServerMaxSize != null && buttonSQLSSRSConfigure != null)
            {
                if (textBoxSQLSSRSReportServerMaxSize.Text.Length >= 1)
                {
                    buttonSQLSSRSConfigure.IsEnabled = true;
                }
                else
                {
                    buttonSQLSSRSConfigure.IsEnabled = false;
                }
            }
        }

        private void SQLServerSSRSReportServerTempDB_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (textBoxSQLSSRSReportServerTempDBMaxSize != null && buttonSQLSSRSConfigure != null)
            {
                if (textBoxSQLSSRSReportServerTempDBMaxSize.Text.Length >= 1)
                {
                    buttonSQLSSRSConfigure.IsEnabled = true;
                }
                else
                {
                    buttonSQLSSRSConfigure.IsEnabled = false;
                }
            }
        }

        async private void SQLServerSSRSConfigure_Click(object sender, RoutedEventArgs e)
        {
            //' Check if database initial size is of a numeric value
            if (IsTextNumeric(textBoxSQLSSRSReportServerMaxSize.Text) == true)
            {
                if (IsTextNumeric(textBoxSQLSSRSReportServerTempDBMaxSize.Text) == true)
                {
                    if (sqlConnection != null)
                    {
                        if (sqlConnection.State == ConnectionState.Open)
                        {
                            progressBarSQLSSRS.IsIndeterminate = true;

                            try
                            {
                                int returnValue = await sqlEngine.SetReportServerDBConfig(sqlConnection, textBoxSQLSSRSReportServerMaxSize.Text, textBoxSQLSSRSReportServerTempDBMaxSize.Text);

                                switch (returnValue)
                                {
                                    case 0:
                                        ShowMessageBox("SUCCESS", "Successfully configured the SSRS databases.");
                                        break;
                                    case 1:
                                        ShowMessageBox("ERROR", "An unhandled error occured while configuring the SSRS databases.");
                                        break;
                                }
                            }
                            catch (Exception ex)
                            {
                                ShowMessageBox("ERROR", String.Format("An unhandled error occured while configuring the SSRS databases. Error message: {0}", ex.Message));
                            }

                            progressBarSQLSSRS.IsIndeterminate = false;
                        }
                    }
                    else
                    {
                        ShowMessageBox("CONNECTION ERROR", "Unable to detect an open SQL connection. Please go to Settings and connect to a SQL Server.");
                    }
                }
                else
                {
                    ShowMessageBox("INVALID INPUT", "Please enter a numeric value for the maximum file size of the ReportServerTempDB database.");
                }
            }
            else
            {
                ShowMessageBox("INVALID INPUT", "Please enter a numeric value for the maximum file size of the ReportServer database.");
            }
        }

        private void WSUSPostInstallBrowse_Click(object sender, RoutedEventArgs e)
        {
            using (FolderBrowserDialog folderBrowse = new FolderBrowserDialog())
            {
                DialogResult dialogResult = folderBrowse.ShowDialog();

                if (dialogResult == System.Windows.Forms.DialogResult.OK && !String.IsNullOrEmpty(folderBrowse.SelectedPath))
                {
                    textBoxWSUSPostInstallLocation.Text = folderBrowse.SelectedPath;
                }
            }
        }

        async private void WSUSFeaturesInstall_Click(object sender, RoutedEventArgs e)
        {
            //' Clear existing items from observable collection
            if (dataGridWSUSFeatures.Items.Count >= 1)
            {
                collectionWSUS.Clear();
            }

            //' Get list features for selected database option
            List<string> featureList = new List<string>();
            switch (comboBoxWSUSFeatures.SelectedItem.ToString())
            {
                case "SQL Server":
                    featureList = GetWindowsFeatures("WSUSSQL");
                    break;
                case "Windows Internal Database":
                    featureList = GetWindowsFeatures("WSUSWID");
                    break;
            }

            //' Update progress bar properties
            progressBarWSUSFeatures.Maximum = featureList.Count - 1;
            int progressBarValue = 0;
            labelWSUSFeaturesProgress.Content = string.Empty;

            //' Process each windows feature for installation
            foreach (string feature in featureList)
            {
                //' Update progress bar
                progressBarSiteType.Value = progressBarValue++;
                labelSiteTypeProgress.Content = String.Format("{0} / {1}", progressBarValue, featureList.Count);

                //' Add new item for current windows feature installation state
                collectionWSUS.Add(new WindowsFeature { Name = feature, Progress = true, Result = "Installing..." });
                dataGridWSUSFeatures.ScrollIntoView(collectionWSUS[collectionWSUS.Count - 1]);

                //' Invoke windows feature installation via PowerShell runspace
                object installResult = await scriptEngine.AddWindowsFeature(feature);

                //' Update current row on data grid
                if (!String.IsNullOrEmpty(installResult.ToString()))
                {
                    var currentCollectionItem = collectionWSUS.FirstOrDefault(winFeature => winFeature.Name == feature);
                    currentCollectionItem.Progress = false;
                    currentCollectionItem.Result = installResult.ToString();
                }

                //' Set color of progressbar
                // new prop needed for binding
            }
        }

        private void WSUSPostInstallComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (textBoxWSUSPostInstallSQLServer != null && textBoxWSUSPostInstallSQLServerInstance != null)
            {
                switch (comboBoxWSUSPostInstall.SelectedItem.ToString())
                {
                    case "SQL Server":
                        textBoxWSUSPostInstallSQLServer.IsEnabled = true;
                        textBoxWSUSPostInstallSQLServerInstance.IsEnabled = true;
                        break;
                    case "Windows Internal Database":
                        textBoxWSUSPostInstallSQLServer.IsEnabled = false;
                        textBoxWSUSPostInstallSQLServerInstance.IsEnabled = false;
                        break;
                }
            }
        }

        async private void WSUSPostInstallConfigure_Click(object sender, RoutedEventArgs e)
        {
            //' Handle progress bar UI element
            progressBarWSUSPost.IsIndeterminate = true;

            //' Construct the path to wsusutil.exe
            string wsusUtil = Path.Combine(Environment.GetEnvironmentVariable("ProgramFiles") + @"\Update Services\Tools", "wsusutil.exe");

            if (File.Exists(wsusUtil))
            {
                if (Directory.Exists(textBoxWSUSPostInstallLocation.Text))
                {
                    if (fileSystem.IsFolderEmpty(textBoxWSUSPostInstallLocation.Text))
                    {
                        //' Perform a post install for SQL Server selection
                        if (comboBoxWSUSPostInstall.SelectedItem.ToString() == "SQL Server")
                        {
                            if (textBoxWSUSPostInstallSQLServer.Text.Length >= 2)
                            {
                                //' Set arguments based upon SQL Server instance presence or not
                                string arguments = string.Empty;
                                if (!String.IsNullOrEmpty(textBoxWSUSPostInstallSQLServerInstance.Text))
                                {
                                    arguments = String.Format(@"POSTINSTALL SQL_INSTANCE_NAME={0}\{1} CONTENT_DIR={2}", textBoxWSUSPostInstallSQLServer.Text, textBoxWSUSPostInstallSQLServerInstance.Text, textBoxWSUSPostInstallLocation.Text);
                                }
                                else
                                {
                                    arguments = String.Format(@"POSTINSTALL SQL_INSTANCE_NAME={0} CONTENT_DIR={1}", textBoxWSUSPostInstallSQLServer.Text, textBoxWSUSPostInstallLocation.Text);
                                }

                                //' Construct new processstartinfo object
                                ProcessStartInfo processStartInfo = NewProcessStartInfo(wsusUtil, arguments);

                                try
                                {
                                    await RunProcessAsync(processStartInfo);
                                    ShowMessageBox("SUCCESS", "Successfully completed the WSUS post install configuration.");
                                }
                                catch (Exception ex)
                                {
                                    ShowMessageBox("ERROR", String.Format("An error occured while configuring WSUS post install. Error message: {0}", ex.Message));
                                }
                            }
                            else
                            {
                                ShowMessageBox("SQL SERVER", "Please enter a SQL Server");
                            }
                        }

                        //' Perform a post install for WID
                        if (comboBoxWSUSPostInstall.SelectedItem.ToString() == "Windows Internal Database")
                        {
                            //' Construct new processstartinfo object
                            string arguments = String.Format(@"POSTINSTALL CONTENT_DIR={0}", textBoxWSUSPostInstallLocation.Text);
                            ProcessStartInfo processStartInfo = NewProcessStartInfo(wsusUtil, arguments);

                            try
                            {
                                await RunProcessAsync(processStartInfo);
                                ShowMessageBox("SUCCESS", "Successfully completed the WSUS post install configuration.");
                            }
                            catch (Exception ex)
                            {
                                ShowMessageBox("ERROR", String.Format("An error occured while configuring WSUS post install. Error message: {0}", ex.Message));
                            }
                        }
                    }
                    else
                    {
                        ShowMessageBox("FOLDER NOT EMPTY", "Specified directory for WSUS content location is not empty. Please enter an empty directory");
                    }
                }
                else
                {
                    ShowMessageBox("DIRECTORY NOT FOUND", "Specified directory for WSUS content location does not exist. Please enter a valid directory.");
                }
            }
            else
            {
                ShowMessageBox("FILE NOT FOUND", "Unable to detect required exectuable to perform WSUS post install. Make sure that WSUS have been installed prior to running the configuration.");
            }

            //' Handle progress bar UI element
            progressBarWSUSPost.IsIndeterminate = false;
        }
    }
}