Add-Type -AssemblyName "System.DirectoryServices"
$ADDirectoryEntry = New-Object -TypeName System.DirectoryServices.DirectoryEntry
$ADSystemManagementContainer = $ADDirectoryEntry.Create("container", "CN=System Management,CN=System")
$ADSystemManagementContainer.SetInfo()