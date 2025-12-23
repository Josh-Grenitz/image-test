using CommonApiUtilities.Interfaces;
using Microsoft.Extensions.Logging;
using System;
using System.Data;
using System.Data.SqlClient;

namespace AdminDashboardService
{
    public class DataCache
    {
        private readonly IApplicationConfiguration m_applicationConfiguration;
        private readonly ILogger m_logger;
        public DataTable DashboardMapping { get; private set; }
        public static DataCache Current { get; set; }

        public DataCache(
            IApplicationConfiguration applicationConfiguration,
            ILogger logger)
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
            string sqlStatement = "SELECT * FROM [DODSDevOps].[dbo].[AdminDashboardConfigMapping];";

            try
            {
                using (var conn = new SqlConnection(m_applicationConfiguration.GetApplicationFileConfiguration<string>("SqlDatabaseConnectionStringFO")))
                {
                    conn.Open();
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
