using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace ConfigMgrPrerequisitesTool
{
    class SqlEngine
    {
        public SqlConnection NewSQLServerConnection(string server, string instance)
        {
            SqlConnectionStringBuilder connectionString = GetSqlConnectionString(server, instance);

            //' Connect to SQL server instance
            SqlConnection connection = new SqlConnection();
            connection.ConnectionString = connectionString.ConnectionString;

            return connection;
        }

        private SqlConnectionStringBuilder GetSqlConnectionString(string server, string instance)
        {
            //' Set database connection string
            SqlConnectionStringBuilder connectionString = new SqlConnectionStringBuilder();

            if (!String.IsNullOrEmpty(instance))
            {
                connectionString.DataSource = server;
                //connectionString.InitialCatalog = mdtDatabase;
                connectionString.IntegratedSecurity = true;
            }
            else
            {
                connectionString.DataSource = String.Format("{0}\\{1}", server, instance);
                //connectionString.InitialCatalog = mdtDatabase;
                connectionString.IntegratedSecurity = true;
            }

            //' Set general properties for connection string
            connectionString.ConnectTimeout = 15;

            return connectionString;
        }

        async public Task<bool> SetSQLServerMemory(SqlConnection connection, string maxMemory, string minMemory)
        {
            bool returnValue = false;

            Assembly assembly = Assembly.GetExecutingAssembly();
            using (Stream stream = assembly.GetManifestResourceStream("ConfigMgrPrerequisitesTool.Scripts.SetSQLServerMemory.sql"))
            using (StreamReader reader = new StreamReader(stream))
            {
                string command = reader.ReadToEnd();

                //' Create new SQL command
                SqlCommand sqlCommand = connection.CreateCommand();
                sqlCommand.CommandText = command;

                //' Add parameters
                sqlCommand.Parameters.Add("@MaxMem", SqlDbType.VarChar).Value = maxMemory;
                sqlCommand.Parameters.Add("@MinMem", SqlDbType.VarChar).Value = minMemory;

                //' Execute command asynchronous
                object executionResult = await sqlCommand.ExecuteScalarAsync();

                if (executionResult != null)
                {
                    returnValue = true;
                }

                //' Cleanup SQL command
                sqlCommand.Dispose();
            }

            return returnValue;
        }
    }
}
