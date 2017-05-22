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

namespace ConfigMgrPrerequisitesTool
{
    public partial class MainWindow : MetroWindow
    {
        ScriptEngine scriptEngine = new ScriptEngine();
        FileSystem fileSystem = new FileSystem();

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

        async public void ShowMessageBox(string title, string message)
        {
            //DialogResult messageBox = System.Windows.Forms.MessageBox.Show(message, "ConfigMgr Prerequisites Tool", MessageBoxButtons.OK, icon);

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
                var currentCollectionItem = siteTypeCollection.FirstOrDefault(winFeature => winFeature.Name == feature);
                currentCollectionItem.Progress = false;
                currentCollectionItem.Result = installResult.ToString();

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

                /////// foreach server in textbox needs to be added

                //' Determine whether to use alternate credentials or not
                if (checkBoxRolesCreds.IsChecked == true)
                {
                    if (psCredentials != null)
                    {
                        connectionInfo = scriptEngine.NewWSManConnectionInfo("MP01.corp.scconfigmgr.com", psCredentials);
                    }
                }
                else
                {
                    //////// needs verification for impersonate

                    connectionInfo = scriptEngine.NewWSManConnectionInfo("MP01.corp.scconfigmgr.com", PSCredential.Empty);
                }

                //' Open a remote runspace using connection info
                if (connectionInfo != null)
                {
                    runspace = scriptEngine.NewRunspace(connectionInfo);
                    runspace.Open();
                }

                if (runspace.RunspaceStateInfo.State == RunspaceState.Opened)
                {
                    // foreach here-ish

                    //' Add new item for current windows feature installation state
                    rolesCollection.Add(new WindowsFeature { Server = "MP01.corp.scconfigmgr.com", Name = "NET-Framework-Core", Progress = true, Result = "Installing..." });
                    dataGridRoles.ScrollIntoView(rolesCollection[rolesCollection.Count - 1]);

                    //' Invoke windows feature installation via PowerShell runspace
                    object installResult = await scriptEngine.AddWindowsFeatureRemote("NET-Framework-Core", runspace);

                    //' Update current row on data grid
                    var currentCollectionItem = rolesCollection.FirstOrDefault(winFeature => winFeature.Name == "NET-Framework-Core");
                    currentCollectionItem.Progress = false;
                    currentCollectionItem.Result = installResult.ToString();

                    runspace.Close();
                }
                else
                {
                    ShowMessageBox("ERROR", "Unable to open connection to");
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
            foreach (FileSystem volume in dataGridSitePrefFile.ItemsSource)
            {
                if (volume.DriveSelected == true)
                {
                    fileSystem.NewNoSmsOnDriveFile(volume.DriveName);
                }
            }
        }

        private void RolesRemoteSystem_Checked(object sender, RoutedEventArgs e)
        {
            if (textBoxRolesRemoteSystem != null)
            {
                textBoxRolesRemoteSystem.IsEnabled = true;
            }
        }

        private void RolesLocalSystem_Checked(object sender, RoutedEventArgs e)
        {
            if (textBoxRolesRemoteSystem != null)
            {
                textBoxRolesRemoteSystem.IsEnabled = false;
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

                    ShowMessageBox("INFORMATION", "New credentials stored successfully");
                }
                else
                {
                    ShowMessageBox("WARNING", "Please enter a password");
                }
            }
            else
            {
                ShowMessageBox("WARNING", "Please enter a username");
            }
        }

        private void RolesCreds_Checked(object sender, RoutedEventArgs e)
        {
            if (psCredentials == null)
            {
                ShowMessageBox("WARNING", "No alternate credentials was found, please go to Settings and define your credentials");

                //' Clear checkbox selection
                System.Windows.Controls.CheckBox checkBox = sender as System.Windows.Controls.CheckBox;
                checkBox.IsChecked = false;
            }
        }
    }
}
