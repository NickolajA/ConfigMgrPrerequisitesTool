using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.DirectoryServices.ActiveDirectory;
using System.DirectoryServices;

namespace ConfigMgrPrerequisitesTool
{
    class DirectoryEngine
    {
        public string DisplayName { get; set; }
        public string SamAccountName { get; set; }
        public string DistinguishedName { get; set; }
        public bool ObjectSelected { get; set; }

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

            if (results != null)
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
