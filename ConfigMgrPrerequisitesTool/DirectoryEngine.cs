using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.DirectoryServices.ActiveDirectory;
using System.DirectoryServices;
using System.Security.Principal;
using System.ComponentModel;
using System.Security.AccessControl;

namespace ConfigMgrPrerequisitesTool
{
    class DirectoryEngine : INotifyPropertyChanged
    {
        public string DisplayName { get; set; }
        public string SamAccountName { get; set; }
        public string DistinguishedName { get; set; }
        private bool _ObjectSelected;

        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        ///  This method triggers the PropertyChanged event and is used when properties
        ///  in a data grid has been programmatically changed.
        /// </summary>
        public void OnPropertyChanged(String propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        public bool ObjectSelected
        {
            get { return _ObjectSelected; }
            set
            {
                if (_ObjectSelected != value)
                {
                    _ObjectSelected = value;
                    OnPropertyChanged("ObjectSelected");
                }
            }
        }

        public bool IsDomainUser()
        {
            bool returnValue = false;

            //' Check if domain SID for current user exist (Returns TRUE for a machine that is on a workgroup)
            if (WindowsIdentity.GetCurrent().User.AccountDomainSid != null)
            {
                SecurityIdentifier domainUsers = new SecurityIdentifier(WellKnownSidType.AccountDomainUsersSid, WindowsIdentity.GetCurrent().User.AccountDomainSid);
                WindowsPrincipal currentUser = new WindowsPrincipal(WindowsIdentity.GetCurrent());

                if (currentUser.IsInRole(domainUsers))
                {
                    returnValue = true;
                }
            }

            return returnValue;
        }

        public string GetSchemaMasterRoleOwner()
        {
            string schemaMaster = string.Empty;

            //' Get current forest and determine schema master role owner
            Forest forest = Forest.GetCurrentForest();
            DomainController domainController = forest.SchemaRoleOwner;
            schemaMaster = domainController.Name;

            return schemaMaster;
        }

        public bool ValidateSchemaMasterRoleOwner(string serverName)
        {
            bool validationStatus = false;

            //' Get current forest and determine schema master role owner
            Forest forest = Forest.GetCurrentForest();
            DomainController schemaMaster = forest.SchemaRoleOwner;

            if (serverName == schemaMaster.Name)
            {
                validationStatus = true;
            }

            return validationStatus;
        }

        public List<DirectoryEngine> InvokeADSearcher(string groupFilter)
        {
            //' Construct list for all DirectoryEngine objects to be returned
            List<DirectoryEngine> directoryEntries = new List<DirectoryEngine>();

            //' Construct active directory searcher and define loaded properties
            string searchFilter = String.Format(@"(&(ObjectCategory=group)(samAccountName=*{0}*))", groupFilter);
            DirectorySearcher searcher = new DirectorySearcher(searchFilter);
            searcher.Asynchronous = true;
            searcher.PropertiesToLoad.Add("samaccountname");
            searcher.PropertiesToLoad.Add("cn");
            searcher.PropertiesToLoad.Add("distinguishedName");

            //' Invoke active directory searcher
            SearchResultCollection results = searcher.FindAll();

            if (results != null && results.Count >= 1)
            {
                foreach (SearchResult result in results)
                {
                    directoryEntries.Add(new DirectoryEngine {
                        DisplayName = result.Properties["cn"][0].ToString(),
                        SamAccountName = result.Properties["samaccountname"][0].ToString(),
                        DistinguishedName = result.Properties["distinguishedName"][0].ToString(),
                        ObjectSelected = false
                    });
                }
            }

            return directoryEntries;
        }

        private bool IsACLRuleAdded(AuthorizationRuleCollection rules, string sid)
        {
            bool returnValue = false;

            foreach (AuthorizationRule rule in rules)
            {
                if (rule.IdentityReference.Value == sid)
                {
                    returnValue = true;
                }
            }

            return returnValue;
        }

        public bool AddOrganizationalUnitACL(string groupSID)
        {
            bool returnValue = false;

            //' Construct active directory searcher for system management container and define loaded properties
            string searchFilter = @"(&(ObjectCategory=container)(name=System Management))";
            DirectorySearcher searcher = new DirectorySearcher(searchFilter);
            searcher.PropertiesToLoad.Add("cn");
            searcher.PropertiesToLoad.Add("distinguishedName");
            searcher.PropertiesToLoad.Add("objectSid");

            //' Invoke active directory searcher
            SearchResult results = searcher.FindOne();

            if (results != null)
            {
                //' Retrieve directory entry for system management container
                DirectoryEntry container = results.GetDirectoryEntry();

                // Check if groupSID exists
                AuthorizationRuleCollection existingRules = container.ObjectSecurity.GetAccessRules(true, true, typeof(SecurityIdentifier));
                bool groupExists = IsACLRuleAdded(existingRules, groupSID);

                if (groupExists == false)
                {
                    //' Construct new access rule and add it to the system management container
                    ActiveDirectoryAccessRule accessRule = new ActiveDirectoryAccessRule(new SecurityIdentifier(groupSID), ActiveDirectoryRights.GenericAll, System.Security.AccessControl.AccessControlType.Allow, ActiveDirectorySecurityInheritance.All, Guid.Empty);
                    container.ObjectSecurity.AddAccessRule(accessRule);

                    //' Write only the DACL information back and don't change the ownership
                    container.Options.SecurityMasks = SecurityMasks.Dacl;

                    //' Commit changes with new access rule
                    container.CommitChanges();

                    returnValue = true;
                }
            }

            return returnValue;
        }

        public string GetADObjectSID(string distinguishedName)
        {
            string returnValue = string.Empty;

            DirectoryEntry group = new DirectoryEntry(String.Format("LDAP://{0}", distinguishedName));
            SecurityIdentifier groupSid = new SecurityIdentifier(group.Properties["objectSid"][0] as byte[], 0);
            returnValue = groupSid.Value;

            return returnValue;
        }

        public string GetPDCRoleOwner()
        {
            string pdcEmulator = string.Empty;

            //' Get current domain and determine PDC Emulator rolw owner
            Domain domain = Domain.GetCurrentDomain();
            DomainController domainController = domain.PdcRoleOwner;
            pdcEmulator = domainController.Name;

            return pdcEmulator;
        }

        public bool ValidatePDCRoleOwner(string serverName)
        {
            bool validationStatus = false;

            //' Get current forest and determine PDC Emulator role owner
            Domain domain = Domain.GetCurrentDomain();
            DomainController domainController = domain.PdcRoleOwner;

            if (serverName == domainController.Name)
            {
                validationStatus = true;
            }

            return validationStatus;
        }

        public bool CheckSystemManagementContainer()
        {
            bool checkStatus = false;

            DirectoryEntry rootDSE = new DirectoryEntry("LDAP://RootDSE");
            string defaultNamingContext = rootDSE.Properties["defaultNamingContext"].Value.ToString();

            DirectoryEntry defaultEntry = new DirectoryEntry("LDAP://" + defaultNamingContext);
            DirectorySearcher containerSearcher = new DirectorySearcher(defaultEntry, @"(&(ObjectCategory=container)(name=System Management))", null, SearchScope.Subtree);

            SearchResult systemManagementContainer = containerSearcher.FindOne();

            if (systemManagementContainer != null)
            {
                //' test to see if correct object or something

                checkStatus = true;
            }

            return checkStatus;
        }
    }
}
