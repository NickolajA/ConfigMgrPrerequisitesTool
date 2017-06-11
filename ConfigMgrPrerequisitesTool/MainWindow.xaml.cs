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

            //' Set item source for combo boxes
            comboBoxADKOnlineVersion.ItemsSource = collectionADKOnline;

            //' Load data into controls
            LoadGridSitePreferenceFile();
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
                    string[] primarySite = new string[] { "NET-Framework-Core", "BITS", "BITS-IIS-Ext", "BITS-Compact-Server", "RDC", "WAS-Process-Model", "WAS-Config-APIs", "WAS-Net-Environment", "Web-Server", "Web-ISAPI-Ext", "Web-ISAPI-Filter", "Web-Net-Ext", "Web-Net-Ext45", "Web-ASP-Net", "Web-ASP-Net45", "Web-ASP", "Web-Windows-Auth", "Web-Basic-Auth", "Web-URL-Auth", "Web-IP-Security", "Web-Scripting-Tools", "Web-Mgmt-Service", "Web-Stat-Compression", "Web-Dyn-Compression", "Web-Metabase", "Web-WMI", "Web-HTTP-Redirect", "Web-Log-Libraries", "Web-HTTP-Tracing", "UpdateServices-RSAT", "UpdateServices-API", "UpdateServices-UI" };
                    featureList.AddRange(primarySite);
                    break;
                case "Central Administration Site":
                    string[] centralAdminSite = new string[] { "NET-Framework-Core", "BITS", "BITS-IIS-Ext", "BITS-Compact-Server", "RDC", "WAS-Process-Model", "WAS-Config-APIs", "WAS-Net-Environment", "Web-Server", "Web-ISAPI-Ext", "Web-ISAPI-Filter", "Web-Net-Ext", "Web-Net-Ext45", "Web-ASP-Net", "Web-ASP-Net45", "Web-ASP", "Web-Windows-Auth", "Web-Basic-Auth", "Web-URL-Auth", "Web-IP-Security", "Web-Scripting-Tools", "Web-Mgmt-Service", "Web-Stat-Compression", "Web-Dyn-Compression", "Web-Metabase", "Web-WMI", "Web-HTTP-Redirect", "Web-Log-Libraries", "Web-HTTP-Tracing", "UpdateServices-RSAT", "UpdateServices-API", "UpdateServices-UI" };
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
                        source.SetException(new InvalidOperationException(String.Format("The prerequisite files download process did not exit correctly or was unexpectedly terminated. Please verify the downloaded files and start the download process again if necessary. Error message: {0}", process.StandardError.ReadToEnd())));
                    }
                    else
                    {
                        source.SetException(new InvalidOperationException("The prerequisite files download process did not exit correctly or was unexpectedly terminated. Please verify the downloaded files and start the download process again if necessary."));
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

        async public Task DownloadFileAsync(string url, string location)
        {
            using (WebClient client = new WebClient())
            {
                client.DownloadProgressChanged += WebClient_DownloadProgressChanged;
                client.DownloadFileCompleted += WebClient_DownloadFileCompleted;
                await client.DownloadFileTaskAsync(new Uri(url), location);
            }
        }

        private bool GetIsNetworkAvailable()
        {
            bool networkState = false;

            if (System.Net.NetworkInformation.NetworkInterface.GetIsNetworkAvailable())
            {
                networkState = true;
            }

            return networkState;
        }

        async public void ShowMessageBox(string title, string message)
        {
            //' Construct new metro dialog settings
            MetroDialogSettings settings = new MetroDialogSettings();
            settings.AffirmativeButtonText = "Continue";
            settings.ColorScheme = MetroDialogColorScheme.Theme;
            settings.DefaultButtonFocus = MessageDialogResult.Affirmative;
            settings.AnimateShow = true;

            MessageDialogResult welcomeDialog = await this.ShowMessageAsync(title, message, MessageDialogStyle.Affirmative, settings);
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

        private void SettingsCredsAdd_Click(object sender, RoutedEventArgs e)
        {
            //' Clear existing credentials
            if (psCredentials != null)
            {
                psCredentials = null;
            }

            if (!String.IsNullOrEmpty(textBoxSettingsCredsUserName.Text))
            {
                if (!String.IsNullOrEmpty(passwordBoxSettingsCredsPassword.Password))
                {
                    //' Construct new PSCredential
                    psCredentials = new PSCredential(textBoxSettingsCredsUserName.Text, passwordBoxSettingsCredsPassword.SecurePassword);

                    //' Clear controls
                    passwordBoxSettingsCredsPassword.Password = null;
                    textBoxSettingsCredsUserName.Text = null;

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

        async private void SettingsSQLServerConnect_Click(object sender, RoutedEventArgs e)
        {
            progressBarSettingsSQLServer.IsIndeterminate = true;

            //' Construct new SqlConnect object
            sqlConnection = sqlEngine.NewSQLServerConnection(textBoxSettingsSQLServerName.Text, textBoxSettingsSQLServerInstance.Text);

            //' Attempt to connect to SQL server
            try
            {
                await sqlConnection.OpenAsync();

                if (sqlConnection.State == ConnectionState.Open)
                {
                    ShowMessageBox("SUCCESS", "Successfully established a connection to the specified SQL Server.");

                    //' Handle UI elements
                    textBoxSettingsSQLServerName.IsEnabled = false;
                    textBoxSettingsSQLServerInstance.IsEnabled = false;
                    buttonSettingsSQLServerConnect.IsEnabled = false;
                    buttonSettingsSQLServerClose.IsEnabled = true;
                }
            }
            catch (Exception ex)
            {
                ShowMessageBox("ERROR", String.Format("{0}", ex.Message));
            }

            progressBarSettingsSQLServer.IsIndeterminate = false;
        }

        private void SettingsSQLServerClose_Click(object sender, RoutedEventArgs e)
        {
            if (sqlConnection != null && sqlConnection.State == ConnectionState.Open)
            {
                sqlConnection.Close();

                //' Handle UI elements
                textBoxSettingsSQLServerName.IsEnabled = true;
                textBoxSettingsSQLServerInstance.IsEnabled = true;
                buttonSettingsSQLServerConnect.IsEnabled = true;
                buttonSettingsSQLServerClose.IsEnabled = false;
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

                //' Update current row on data grid
                if (!String.IsNullOrEmpty(installResult.ToString()))
                {
                    var currentCollectionItem = siteTypeCollection.FirstOrDefault(winFeature => winFeature.Name == feature);
                    currentCollectionItem.Progress = false;
                    currentCollectionItem.Result = installResult.ToString();
                }

                //' Set color of progressbar
                // new prop needed for binding
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

                        //' Invoke windows feature installation via PowerShell runspace
                        object installResult = await scriptEngine.AddWindowsFeature(feature);

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
        }

        private void SiteType_LoadingRow(object sender, DataGridRowEventArgs e)
        {
            DataGridRow row = e.Row;
            row.Background = (SolidColorBrush)new BrushConverter().ConvertFrom("#333337");
        }

        private void SitePreferenceFile_LoadingRow(object sender, DataGridRowEventArgs e)
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

            ProcessStartInfo processStartInfo = new ProcessStartInfo();
            processStartInfo.FileName = textBoxSitePrereqBrowse.Text;
            processStartInfo.Arguments = textBoxSitePrereqDownload.Text;
            processStartInfo.UseShellExecute = false;
            processStartInfo.CreateNoWindow = true;
            processStartInfo.RedirectStandardError = true;

            try
            {
                await RunProcessAsync(processStartInfo);
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
            //' Determine schema master
            string schemaMaster = activeDirectory.GetSchemaMasterRoleOwner();

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
            string pdcEmulator = activeDirectory.GetPDCRoleOwner();

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
                    }
                    catch (Exception ex)
                    {
                        ShowMessageBox("ERROR", String.Format("{0}", ex.Message));
                    }

                    //' Cleanup runspace
                    runspace.Close();
                }
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

            //' Construct new background worker
            BackgroundWorker worker = new BackgroundWorker();
            worker.DoWork += WorkerDoWork_PermissionSearch;
            worker.RunWorkerCompleted += WorkerCompleted_PermissionSearch;

            //' Invoke background worker
            worker.RunWorkerAsync(textBoxADPermissionsGroupSearch.Text);
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
                //' Get all selected groups
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
            //' Invoke web parser
            List<WebEngine> links = webParser.LoadWindowsADKVersions();

            //' Return search results
            e.Result = links;
        }

        private void WorkerCompleted_ADKVersionOnlineSearch(object sender, RunWorkerCompletedEventArgs e)
        {
            //' Collect search results
            List<WebEngine> links = (List<WebEngine>)e.Result;

            if (links != null && links.Count >= 1)
            {
                foreach (WebEngine link in links)
                {
                    collectionADKOnline.Add(new WebEngine { LinkName = link.LinkName, LinkValue = link.LinkValue});
                }

                comboBoxADKOnlineVersion.SelectedIndex = 0;
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
            string filePath = Path.Combine(textBoxADKOnlineLocation.Text, "adksetup.exe");

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
                ProcessStartInfo processStartInfo = new ProcessStartInfo();
                processStartInfo.FileName = filePath;
                processStartInfo.Arguments = @"/norestart /q /ceip off /features OptionId.WindowsPreinstallationEnvironment OptionId.DeploymentTools OptionId.UserStateMigrationTool";
                processStartInfo.UseShellExecute = false;
                processStartInfo.CreateNoWindow = true;
                processStartInfo.RedirectStandardError = true;

                try
                {
                    await RunProcessAsync(processStartInfo);
                    ShowMessageBox("SUCCESS", "Successfully installed Windows ADK.");
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
            progressBarADKOnline.IsIndeterminate = true;

            string filePath = Path.Combine(textBoxADKOfflineLocation.Text, "adksetup.exe");

            //' Invoke ADK setup bootstrap file if download successful
            if (File.Exists(filePath))
            {
                ProcessStartInfo processStartInfo = new ProcessStartInfo();
                processStartInfo.FileName = filePath;
                processStartInfo.Arguments = @"/norestart /q /ceip off /features OptionId.WindowsPreinstallationEnvironment OptionId.DeploymentTools OptionId.UserStateMigrationTool";
                processStartInfo.UseShellExecute = false;
                processStartInfo.CreateNoWindow = true;
                processStartInfo.RedirectStandardError = true;

                try
                {
                    await RunProcessAsync(processStartInfo);
                    ShowMessageBox("SUCCESS", "Successfully installed Windows ADK.");
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

        async private void SQLServerGeneralMemoryConfigure_Click(object sender, RoutedEventArgs e)
        {
            bool result = await sqlEngine.SetSQLServerMemory(sqlConnection, textBoxSQLGeneralMaxMemory.Text, textBoxSQLGeneralMinMemory.Text);
        }
    }
}
