using CommonApiUtilities.Interfaces;
using Microsoft.Extensions.Logging;
using System;
using System.Data;
using Microsoft.Data.SqlClient;
using Azure.Identity;

namespace AdminDashboardService
{
    public class DataCache
    {
        private readonly IApplicationConfiguration m_applicationConfiguration;
        private readonly ILogger<Startup> m_logger;
        public DataTable DashboardMapping { get; private set; }
        public static DataCache Current { get; set; }

        public DataCache(
            IApplicationConfiguration applicationConfiguration,
            ILogger<Startup> logger)
        {
            m_applicationConfiguration = applicationConfiguration ?? throw new ArgumentNullException(nameof(applicationConfiguration));
            m_logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }
        
        public bool Initialize()
        {
            try
            {
                Current.loadMappingTable();
                return true;
            }

            catch (Exception ex)
            {
                m_logger.LogError(ex, ex.Message);
                return false;
            }
        }

        private void loadMappingTable()
        {
            string sqlStatement = "SELECT * FROM [DODSDevOps].[dbo].[AdminDashboardMapping];";

            try
            {
                // Replace with your User-Assigned Managed Identity client ID
                string clientId = "9ef795c4-8050-42fc-9335-a1ac8e9e10e5";

                // Create a DefaultAzureCredential with the client ID
                var credential = new DefaultAzureCredential(new DefaultAzureCredentialOptions
                {
                    ManagedIdentityClientId = clientId
                });

                // Connection string for Azure SQL Managed Instance
                var connectionString = new SqlConnectionStringBuilder
                {
                    DataSource = "free-sql-mi-9924470.07c22b5a3295.database.windows.net",
                    InitialCatalog = "DODSDevOps",
                    Authentication = SqlAuthenticationMethod.ActiveDirectoryManagedIdentity
                }.ConnectionString;

                using (var conn = new SqlConnection(connectionString))
                {
                    conn.AccessToken = credential.GetToken(
                        new Azure.Core.TokenRequestContext(new[] { "https://database.windows.net/" })
                    ).Token;
                    conn.Open();
                    Console.WriteLine("Connected to Azure SQL Managed Instance using User-Assigned Managed Identity.");
                    if (conn.State != ConnectionState.Open)
                    {
                        throw new InvalidOperationException("Failed to connect to SQL server.");
                    }

                    using (var cmd = new SqlCommand(sqlStatement, conn))
                    {
                        using (var reader = cmd.ExecuteReader())
                        {
                            DashboardMapping = new DataTable();
                            DashboardMapping.Load(reader);
                        }
                    }
                }
            }

            catch (Exception)
            {
                throw;
            }
        }
    }
}
