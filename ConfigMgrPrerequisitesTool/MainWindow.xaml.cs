using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Media;
using MahApps.Metro.Controls;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using MahApps.Metro.Controls.Dialogs;
using System.ComponentModel;

namespace ConfigMgrPrerequisitesTool
{
    public partial class MainWindow : MetroWindow
    {
        ScriptEngine scriptEngine = new ScriptEngine();
        FileSystem fileSystem = new FileSystem();
        DirectoryEngine activeDirectory = new DirectoryEngine();

        private ObservableCollection<WindowsFeature> siteTypeCollection = new ObservableCollection<WindowsFeature>();
        private ObservableCollection<FileSystem> sitePreferenceFileCollection = new ObservableCollection<FileSystem>();
        private ObservableCollection<WindowsFeature> rolesCollection = new ObservableCollection<WindowsFeature>();
        private PSCredential psCredentials = null;

        public MainWindow()
        {
            InitializeComponent();

            //' Set item source for data grids
            dataGridSiteType.ItemsSource = siteTypeCollection;
            dataGridSitePrefFile.ItemsSource = sitePreferenceFileCollection;
            dataGridRoles.ItemsSource = rolesCollection;

            //' Load data into controls
            LoadGridSitePreferenceFile();
        }

        /// <summary>
        /// Based on parameter input, returns a string array object containing the Windows Features required per Site typ.
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

        private void DownloadPrereqFiles(string exePath, string location)
        {
            // using?
            ProcessStartInfo processInfo = new ProcessStartInfo("setupdl.exe", location);
            processInfo.CreateNoWindow = true;
            processInfo.WorkingDirectory = fileSystem.GetParentFolder(exePath);
            processInfo.UseShellExecute = true;
            processInfo.Verb = "Runas";
            processInfo.WindowStyle = ProcessWindowStyle.Hidden;
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

        private void DataGridFilesSelectAll_Checked(object sender, RoutedEventArgs e)
        {
            foreach (FileSystem volume in dataGridSitePrefFile.ItemsSource)
            {
                volume.DriveSelected = true;
            }
        }

        private void DataGridFilesSelectAll_Unchecked(object sender, RoutedEventArgs e)
        {
            foreach (FileSystem volume in dataGridSitePrefFile.ItemsSource)
            {
                volume.DriveSelected = false;
            }
        }

        private void SitePrefFilesCreate_Click(object sender, RoutedEventArgs e)
        {
            int volumeCount = 0;
            foreach (FileSystem volume in dataGridSitePrefFile.ItemsSource)
            {
                if (volume.DriveSelected == true)
                {
                    volumeCount++;
                    fileSystem.NewNoSmsOnDriveFile(volume.DriveName);
                }
            }

            if (volumeCount >= 1)
            {
                ShowMessageBox("FILE CREATION", @"Successfully created a NO_SMS_ON_DRIVE.SMS file on the selected drives.");
            }
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
            //' Set schema master role owner in textbox
            string schemaMaster = activeDirectory.GetSchemaMasterRoleOwner();
            textBoxADSchemaServer.Text = schemaMaster;

            //' Disable validate button
            buttonADSchemaValidate.IsEnabled = false;
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
            //' Define variables for copy operation
            string fileName = "extadsch.exe";
            string localFile = textBoxADSchemaFile.Text;
            string remoteFile = String.Format(@"\\{0}\C$\{1}", textBoxADSchemaServer.Text, fileName);

            //' Copy the file to schema master admin share
            try
            {
                progressBarADSchemaStage.IsIndeterminate = true;
                await fileSystem.CopyFileAsync(localFile, remoteFile, System.Threading.CancellationToken.None);
            }
            catch (Exception ex)
            {
                ShowMessageBox("STAGE ERROR", String.Format("{0}", ex.Message));
            }

            progressBarADSchemaStage.IsIndeterminate = false;
        }
    }
}
