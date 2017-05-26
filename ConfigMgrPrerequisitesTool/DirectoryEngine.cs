using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.DirectoryServices.ActiveDirectory;

namespace ConfigMgrPrerequisitesTool
{
    class DirectoryEngine
    {
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
    }
}
