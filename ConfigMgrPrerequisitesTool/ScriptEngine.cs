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

        /// <summary>
        ///  This method invokes Install-WindowsFeature PowerShell cmdlet on a remote runspace to install a specific feature.
        /// </summary>
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

        async public Task<bool> CopyFileToRemoteServer(string filePath, string remoteServer, PSCredential credential)
        {
            bool transferStatus = false;

            string hostName = System.Net.Dns.GetHostName();
            WSManConnectionInfo connectionInfo = new WSManConnectionInfo(false, hostName, 5985, "/wsman", "http://schemas.microsoft.com/powershell/Microsoft.PowerShell", credential);
            connectionInfo.AuthenticationMechanism = AuthenticationMechanism.Kerberos;

            using (Runspace runspace = RunspaceFactory.CreateRunspace(connectionInfo))
            {

                runspace.Open();

                using (PowerShell psInstance = PowerShell.Create())
                {
                    psInstance.Runspace = runspace;

                    // https://stackoverflow.com/questions/17067260/invoke-powershell-command-from-c-sharp-with-different-credential

                    //' Add command and parameter to PowerShell instance
                    psInstance.AddCommand("New-PSDrive");
                    psInstance.AddParameter("Name", "DC");
                    psInstance.AddParameter("PSProvider", "FileSystem");
                    psInstance.AddParameter("Root", String.Format(@"\\{0}\C$", remoteServer));

                    psInstance.AddCommand("Copy-Item");
                    psInstance.AddParameter("Path", filePath);
                    psInstance.AddParameter("Destination", @"DC:\");
                    //psInstance.AddParameter("Destination", String.Format(@"\\{0}\C$", remoteServer));
                    psInstance.AddParameter("Force", true);

                    //' Await for command to finish execution
                    PSDataCollection<PSObject> cResult = await Task.Factory.FromAsync(psInstance.BeginInvoke(), psInstance.EndInvoke);
                    //Collection<PSObject> processes = execRes.ReadAll();

                    transferStatus = psInstance.HadErrors;
                }

                runspace.Close();
            }


            // Create PowerShell instance


            return transferStatus;
        }

        public Runspace NewRunspace(WSManConnectionInfo connectionInfo)
        {
            Runspace runspace = RunspaceFactory.CreateRunspace(connectionInfo);

            return runspace;
        }

        public WSManConnectionInfo NewLocalWSManConnectionInfo(PSCredential credential)
        {
            WSManConnectionInfo connectionInfo = new WSManConnectionInfo() { Credential = credential };

            return connectionInfo;
        }

        public WSManConnectionInfo NewWSManConnectionInfo(string computer, PSCredential credentials)
        {
            WSManConnectionInfo connectionInfo = new WSManConnectionInfo(false, computer, 5985, "/wsman", "http://schemas.microsoft.com/powershell/Microsoft.PowerShell", credentials);
            connectionInfo.OperationTimeout = 2 * 15 * 1000;
            connectionInfo.OpenTimeout = 1 * 15 * 1000;

            return connectionInfo;
        }
    }

    [Serializable]
    internal class ScriptEngineException : Exception
    {
        public ScriptEngineException()
        {
        }

        public ScriptEngineException(string message) : base(message)
        {
        }

        public ScriptEngineException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected ScriptEngineException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}