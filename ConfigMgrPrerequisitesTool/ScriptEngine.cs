using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Management.Automation;
using System.Collections.ObjectModel;
using System.Management.Automation.Runspaces;
using System.Runtime.Serialization;
using System.Management.Automation.Remoting;
using System.Reflection;
using System.IO;
using System.Windows.Forms;

namespace ConfigMgrPrerequisitesTool
{
    class ScriptEngine
    {
        /// <summary>
        ///  This method invokes Install-WindowsFeature PowerShell cmdlet to install a specific feature.
        /// </summary>
        async public Task<object> AddWindowsFeature(string featureName)
        {
            object installStatus = string.Empty;

            //' Create PowerShell instance
            using (PowerShell psInstance = PowerShell.Create())
            {
                //' Add command and parameter to PowerShell instance
                psInstance.AddCommand("Install-WindowsFeature");
                psInstance.AddParameter("Name", featureName);

                // Construct collection to hold pipeline stream objects
                PSDataCollection<PSObject> streamCollection = new PSDataCollection<PSObject>();

                // Invoke execution on the pipeline
                try
                {
                    PSDataCollection<PSObject> tResult = await Task.Factory.FromAsync(psInstance.BeginInvoke<PSObject, PSObject>(null, streamCollection), pResult => psInstance.EndInvoke(pResult));

                    foreach (PSObject psObject in streamCollection)
                    {
                        installStatus = psObject.Members["ExitCode"].Value;
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(String.Format("{0}", ex.Message));
                }
            }

            return installStatus;
        }

        /// <summary>
        ///  This method invokes Install-WindowsFeature PowerShell cmdlet to install a specific feature by using source files from a specified location.
        /// </summary>
        async public Task<object> AddWindowsFeature(string featureName, string source)
        {
            object installStatus = string.Empty;

            //' Create PowerShell instance
            using (PowerShell psInstance = PowerShell.Create())
            {
                //' Add command and parameter to PowerShell instance
                psInstance.AddCommand("Install-WindowsFeature");
                psInstance.AddParameter("Name", featureName);
                psInstance.AddParameter("Source", String.Format("{0}", source));

                //' Construct collection to hold pipeline stream objects
                PSDataCollection<PSObject> streamCollection = new PSDataCollection<PSObject>();

                //' Invoke execution on the pipeline
                PSDataCollection<PSObject> tResult = await Task.Factory.FromAsync(psInstance.BeginInvoke<PSObject, PSObject>(null, streamCollection), pResult => psInstance.EndInvoke(pResult));

                foreach (PSObject psObject in streamCollection)
                {
                    installStatus = psObject.Members["ExitCode"].Value;
                }
            }

            return installStatus;
        }

        /// <summary>
        ///  This method invokes Install-WindowsFeature PowerShell cmdlet on a remote runspace to install a specific feature.
        /// </summary>
        async public Task<object> AddWindowsFeatureRemote(string featureName, Runspace runspace)
        {
            object installStatus = string.Empty;

            //' Create PowerShell instance
            using (PowerShell psInstance = PowerShell.Create())
            {
                //' Set runspace
                psInstance.Runspace = runspace;

                //' Add command and parameter to PowerShell instance
                psInstance.AddCommand("Install-WindowsFeature");
                psInstance.AddParameter("Name", featureName);

                //' Construct collection to hold pipeline stream objects
                PSDataCollection<PSObject> streamCollection = new PSDataCollection<PSObject>();

                //' Invoke execution on the pipeline
                PSDataCollection<PSObject> tResult = await Task.Factory.FromAsync(psInstance.BeginInvoke<PSObject, PSObject>(null, streamCollection), pResult => psInstance.EndInvoke(pResult));

                foreach (PSObject psObject in streamCollection)
                {
                    installStatus = psObject.Members["ExitCode"].Value;
                }
            }

            return installStatus;
        }

        /// <summary>
        ///  This method invokes Start-Process PowerShell cmdlet on a remote runspace to run a local executable.
        /// </summary>
        async public Task<bool> StartProcessRemote(string filePath, Runspace runspace)
        {
            bool executionSuccess;

            //' Create PowerShell instance
            using (PowerShell psInstance = PowerShell.Create())
            {
                //' Set runspace
                psInstance.Runspace = runspace;

                //' Add command and parameter to PowerShell instance
                psInstance.AddCommand("Start-Process");
                psInstance.AddParameter("FilePath", filePath);
                psInstance.AddParameter("Wait", true);

                // Construct collection to hold pipeline stream objects
                PSDataCollection<PSObject> streamCollection = new PSDataCollection<PSObject>();

                // Invoke execution on the pipeline and collection any errors
                PSDataCollection<PSObject> tResult = await Task.Factory.FromAsync(psInstance.BeginInvoke<PSObject, PSObject>(null, streamCollection), pResult => psInstance.EndInvoke(pResult));
                executionSuccess = psInstance.HadErrors;
            }

            return executionSuccess;
        }

        async public Task<bool> NewADContainer(Runspace runspace)
        {
            bool executionSuccess;

            //' Create PowerShell instance
            using (PowerShell psInstance = PowerShell.Create())
            {
                //' Set runspace
                psInstance.Runspace = runspace;

                //' Add embedded PowerShell script file
                Assembly assembly = Assembly.GetExecutingAssembly();
                using (Stream stream = assembly.GetManifestResourceStream("ConfigMgrPrerequisitesTool.Scripts.CreateSystemManagementContainer.ps1"))
                using (StreamReader reader = new StreamReader(stream))
                {
                    string result = reader.ReadToEnd();
                    psInstance.AddScript(result);
                }

                // Construct collection to hold pipeline stream objects
                PSDataCollection<PSObject> streamCollection = new PSDataCollection<PSObject>();

                // Invoke execution on the pipeline and collection any errors
                PSDataCollection<PSObject> tResult = await Task.Factory.FromAsync(psInstance.BeginInvoke<PSObject, PSObject>(null, streamCollection), pResult => psInstance.EndInvoke(pResult));
                executionSuccess = psInstance.HadErrors;
            }

            return executionSuccess;
        }

        public Runspace NewRunspace(WSManConnectionInfo connectionInfo)
        {
            Runspace runspace = RunspaceFactory.CreateRunspace(connectionInfo);

            return runspace;
        }

        public WSManConnectionInfo NewWSManConnectionInfo(string computer, PSCredential credentials)
        {
            WSManConnectionInfo connectionInfo = new WSManConnectionInfo(false, computer, 5985, "/wsman", "http://schemas.microsoft.com/powershell/Microsoft.PowerShell", credentials);
            connectionInfo.OperationTimeout = 2 * 15 * 1000;
            connectionInfo.OpenTimeout = 1 * 15 * 1000;

            return connectionInfo;
        }
    }
}