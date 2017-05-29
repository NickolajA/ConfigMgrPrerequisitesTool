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
            //' Get current forest and determine schema master role owner
            Forest forest = Forest.GetCurrentForest();
            DomainController schemaMaster = forest.SchemaRoleOwner;

            return schemaMaster.Name;
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
            List<DirectoryEngine> directoryEntries = new List<DirectoryEngine>();

            string searchFilter = String.Format(@"(&(ObjectCategory=group)(samAccountName=*{0}*))", groupFilter);
            DirectorySearcher searcher = new DirectorySearcher(searchFilter);
            searcher.Asynchronous = true;
            searcher.PropertiesToLoad.Add("samaccountname");
            searcher.PropertiesToLoad.Add("cn");
            searcher.PropertiesToLoad.Add("distinguishedName");

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
    }
}
