using CommonApiUtilities.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Text;
using Microsoft.AspNetCore.Authorization;

namespace AdminDashboard.Controllers
{
#if DEBUG
#else
    [Authorize]
#endif
    [Produces("application/json")]
    public class AuditController : Controller
    {
        private readonly ILogger<AuditController> m_logger;
        private readonly IApplicationConfiguration m_applicationConfiguration;
        private readonly ISqlCrud m_sqlCrud;
        private readonly IExpandoObjectHandler m_expandoObjectHandler;

        public AuditController(ILogger<AuditController> logger,
                               IApplicationConfiguration applicationConfiguration,
                               ISqlCrud sqlCrud,
                               IExpandoObjectHandler expandoObjectHandler)
        {
            m_logger = logger;
            m_applicationConfiguration = applicationConfiguration;
            m_sqlCrud = sqlCrud;
            m_expandoObjectHandler = expandoObjectHandler;
        }

#if DEBUG
#else        
        [Authorize(Policy = "READ")]
#endif
        [HttpGet]
        [Route("api/Admin/Audit/Read/")]
        public IActionResult Get(string Application, string Config, string User, string StartDate, string EndDate)
        {
            try
            {
                string ApiEndpoint = "AdminDashboardAudit";

                StringBuilder whereBuilder = new StringBuilder("WHERE");
                if (!string.IsNullOrEmpty(Application))
                    whereBuilder.Append($" [Application] LIKE '{Application}' AND");
                if (!string.IsNullOrEmpty(Config))
                    whereBuilder.Append($" [Configuration] LIKE '{Config}' AND");
                if (!string.IsNullOrEmpty(User))
                    whereBuilder.Append($" [User] LIKE '{User}' AND");
                if (!string.IsNullOrEmpty(StartDate))
                    whereBuilder.Append($" [LastUpdatedTime] >= '{StartDate}' AND");
                if (!string.IsNullOrEmpty(EndDate))
                    whereBuilder.Append($" [LastUpdatedTime] <= '{EndDate}' AND");

                string whereStatement = "ORDER BY [LastUpdatedTime] DESC";
                //This removes the AND!
                if (whereBuilder.Length > 5)
                    whereStatement = $"{whereBuilder.ToString().Remove(whereBuilder.Length - 3)} ORDER BY [LastUpdatedTime] DESC";

                try
                {
                    m_sqlCrud.ReadDataFromSqlTable(
                        ApiEndpoint,
                        m_applicationConfiguration.GetMasterExpando(),
                        m_applicationConfiguration.GetApplicationFileConfiguration<string>("AuditTableName"),
                        whereStatement,
                        m_applicationConfiguration.GetApplicationFileConfiguration<string>("SqlDatabaseConnectionString"));
                }

                catch (System.Data.DataException ex) 
                {
                    if (ex.Message.Contains("Reader has no rows!"))
                    {
                        return NotFound();
                    }
                }

                return Ok(m_expandoObjectHandler.GetExpandoProperty(ApiEndpoint, m_applicationConfiguration.GetMasterExpando()));
            }
            catch (Exception e)
            {
                m_logger.LogError(e, "Error Calling Get @ api/Admin/Audit/Get/");
                return StatusCode(StatusCodes.Status500InternalServerError,e.Message);
            }
        }
    }
}
