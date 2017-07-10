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
                connectionString.IntegratedSecurity = true;
            }
            else
            {
                connectionString.DataSource = String.Format("{0}\\{1}", server, instance);
                //connectionString.InitialCatalog = mdtDatabase;
                connectionString.IntegratedSecurity = true;
            }

            //' Set general properties for connection string
            connectionString.ConnectTimeout = 60;

            return connectionString;
        }

        async public Task<int> SetReportServerDBConfig(SqlConnection connection, string rsSize, string rstdSize)
        {
            int returnValue;

            //' Parse parameters
            float rsSizeFloat = float.Parse(rsSize);
            float rstdSizeFloat = float.Parse(rstdSize);

            //' Get executing assembly and read SQL script
            Assembly assembly = Assembly.GetExecutingAssembly();
            using (Stream stream = assembly.GetManifestResourceStream("ConfigMgrPrerequisitesTool.Scripts.SetSSRSConfiguration.sql"))
            using (StreamReader reader = new StreamReader(stream))
            {
                string command = reader.ReadToEnd();

                //' Create new SQL command
                SqlCommand sqlCommand = connection.CreateCommand();
                sqlCommand.CommandText = command;
                sqlCommand.CommandTimeout = 360;

                //' Construct output param
                sqlCommand.Parameters.Add("@ReturnValue", SqlDbType.Int, 4).Direction = ParameterDirection.Output;

                //' Add parameters
                sqlCommand.Parameters.Add("@ReportServerMaxSizeGB", SqlDbType.Float).Value = rsSizeFloat;
                sqlCommand.Parameters.Add("@ReportServerTempDBMaxSizeGB", SqlDbType.Float).Value = rstdSizeFloat;

                //' Execute command asynchronous
                int dataReader = await sqlCommand.ExecuteNonQueryAsync(); //' CommandBehavior.CloseConnection

                returnValue = (int)sqlCommand.Parameters["@ReturnValue"].Value;

                //' Cleanup SQL command
                sqlCommand.Dispose();
            }

            return returnValue;
        }

        async public Task<string> GetSQLInstanceCollation(SqlConnection connection)
        {
            string returnValue = string.Empty;

            //' Get executing assembly and read SQL script
            Assembly assembly = Assembly.GetExecutingAssembly();
            using (Stream stream = assembly.GetManifestResourceStream("ConfigMgrPrerequisitesTool.Scripts.GetSQLInstanceCollation.sql"))
            using (StreamReader reader = new StreamReader(stream))
            {
                string command = reader.ReadToEnd();

                //' Create new SQL command
                SqlCommand sqlCommand = connection.CreateCommand();
                sqlCommand.CommandText = command;
                sqlCommand.CommandTimeout = 360;

                //' Execute command asynchronous
                object execResult = await sqlCommand.ExecuteScalarAsync(); //' CommandBehavior.CloseConnection

                returnValue = execResult.ToString();

                //' Cleanup SQL command
                sqlCommand.Dispose();
            }

            return returnValue;
        }

        async public Task<int> NewCMDatabase(SqlConnection connection, string siteCode, string cores, string dbSize, string logSize)
        {
            int returnValue;

            //' Get executing assembly and read SQL script
            Assembly assembly = Assembly.GetExecutingAssembly();
            using (Stream stream = assembly.GetManifestResourceStream("ConfigMgrPrerequisitesTool.Scripts.CreateCMDatabase.sql"))
            using (StreamReader reader = new StreamReader(stream))
            {
                string command = reader.ReadToEnd();

                //' Create new SQL command
                SqlCommand sqlCommand = connection.CreateCommand();
                sqlCommand.CommandText = command;
                sqlCommand.CommandTimeout = 360;

                //' Construct output param
                sqlCommand.Parameters.Add("@ReturnValue", SqlDbType.Int, 4).Direction = ParameterDirection.Output;

                //' Add parameters
                sqlCommand.Parameters.Add("@CMSiteCode", SqlDbType.NChar).Value = siteCode;
                sqlCommand.Parameters.Add("@NumTotalDataFiles", SqlDbType.TinyInt).Value = cores;
                sqlCommand.Parameters.Add("@InitialDataFileSize", SqlDbType.NVarChar, 50).Value = String.Format("{0}MB", dbSize);
                sqlCommand.Parameters.Add("@InitialLogFileSize", SqlDbType.NVarChar, 50).Value = String.Format("{0}MB", logSize);

                //' Execute command asynchronous
                int dataReader = await sqlCommand.ExecuteNonQueryAsync(); //' CommandBehavior.CloseConnection

                returnValue = (int)sqlCommand.Parameters["@ReturnValue"].Value;
               
                //' Cleanup SQL command
                sqlCommand.Dispose();
            }

            return returnValue;
        }

        async public Task<int> SetSQLServerMemory(SqlConnection connection, string maxMemory, string minMemory)
        {
            int returnValue;

            //' Get executing assembly and read SQL script
            Assembly assembly = Assembly.GetExecutingAssembly();
            using (Stream stream = assembly.GetManifestResourceStream("ConfigMgrPrerequisitesTool.Scripts.SetSQLServerMemory.sql"))
            using (StreamReader reader = new StreamReader(stream))
            {
                string command = reader.ReadToEnd();

                //' Create new SQL command
                SqlCommand sqlCommand = connection.CreateCommand();
                sqlCommand.CommandText = command;
                sqlCommand.CommandTimeout = 120;

                //' Add parameters
                sqlCommand.Parameters.Add("@MaxMem", SqlDbType.VarChar).Value = maxMemory;
                sqlCommand.Parameters.Add("@MinMem", SqlDbType.VarChar).Value = minMemory;

                //' Construct output param
                sqlCommand.Parameters.Add("@ReturnValue", SqlDbType.Int, 4).Direction = ParameterDirection.Output;

                try
                {
                    //' Execute command asynchronous
                    int dataReader = await sqlCommand.ExecuteNonQueryAsync();
                    returnValue = (int)sqlCommand.Parameters["@ReturnValue"].Value;
                }
                catch (Exception ex)
                {
                    returnValue = 1;
                }
                finally
                {
                    //' Cleanup SQL command
                    sqlCommand.Dispose();
                }
            }

            return returnValue;
        }
    }
}
