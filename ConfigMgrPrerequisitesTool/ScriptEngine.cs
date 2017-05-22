using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Management.Automation;
using System.Collections.ObjectModel;
using System.Management.Automation.Runspaces;

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

            // Create PowerShell instance
            using (PowerShell psInstance = PowerShell.Create())
            {
                //' Add command and parameter to PowerShell instance
                psInstance.AddCommand("Install-WindowsFeature");
                psInstance.AddParameter("Name", featureName);

                // Construct collection to hold pipeline stream objects
                PSDataCollection<PSObject> streamCollection = new PSDataCollection<PSObject>();

                // Invoke execution on the pipeline
                PSDataCollection<PSObject> tResult = await Task.Factory.FromAsync(psInstance.BeginInvoke<PSObject, PSObject>(null, streamCollection), pResult => psInstance.EndInvoke(pResult));

                foreach (PSObject psObject in streamCollection)
                {
                    installStatus = psObject.Members["ExitCode"].Value;
                }
            }

            return installStatus;
        }

        async public Task<object> AddWindowsFeatureRemote(string featureName, Runspace runspace)
        {
            object installStatus = string.Empty;

            // Create PowerShell instance
            using (PowerShell psInstance = PowerShell.Create())
            {
                //' Set runspace
                psInstance.Runspace = runspace;

                //' Add command and parameter to PowerShell instance
                psInstance.AddCommand("Install-WindowsFeature");
                psInstance.AddParameter("Name", featureName);

                // Construct collection to hold pipeline stream objects
                PSDataCollection<PSObject> streamCollection = new PSDataCollection<PSObject>();

                // Invoke execution on the pipeline
                PSDataCollection<PSObject> tResult = await Task.Factory.FromAsync(psInstance.BeginInvoke<PSObject, PSObject>(null, streamCollection), pResult => psInstance.EndInvoke(pResult));

                foreach (PSObject psObject in streamCollection)
                {
                    installStatus = psObject.Members["ExitCode"].Value;
                }
            }

            return installStatus;
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