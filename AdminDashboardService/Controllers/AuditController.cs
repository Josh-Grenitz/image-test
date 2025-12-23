using CommonApiUtilities.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using System.Collections.Generic;
using System.Linq;
using System.Dynamic;
using System.Text.Json;

namespace AdminDashboard.Controllers
{

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

        [HttpGet]
        [Route("api/Admin/Audit/Read/")]
        [Authorize(Policy = "Dashboard:Read")]
        public IActionResult Get(string Application, string Config, string User, string StartDate, string EndDate)
        {
            try
            {
                string database1 = "SqlDatabaseConnectionString" + "FO";
                string database2 = "SqlDatabaseConnectionString" + "MO";
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
                dynamic expando1 = new ExpandoObject();
                dynamic expando2 = new ExpandoObject();
                try
                {
                    m_sqlCrud.ReadDataFromSqlTable(
                        ApiEndpoint,
                        expando1,
                        m_applicationConfiguration.GetApplicationFileConfiguration<string>("AuditTableName"),
                        whereStatement,
                        m_applicationConfiguration.GetApplicationFileConfiguration<string>(database1));
                    m_sqlCrud.ReadDataFromSqlTable(
                        ApiEndpoint,
                        expando2,
                        m_applicationConfiguration.GetApplicationFileConfiguration<string>("AuditTableName"),
                        whereStatement,
                        m_applicationConfiguration.GetApplicationFileConfiguration<string>(database2));
                }

                catch (System.Data.DataException ex)
                {
                    if (ex.Message.Contains("Reader has no rows!"))
                    {
                        return NotFound();
                    }
                }
                var dict2 = (IDictionary<string, object>)expando2;
                var dict1 = (IDictionary<string, object>)expando1;
                dict1["AdminDashboardAudit"] = ((IDictionary<string, object>)dict1["AdminDashboardAudit"])["AdminDashboardAudit"];
                dict2["AdminDashboardAudit"] = ((IDictionary<string, object>)dict2["AdminDashboardAudit"])["AdminDashboardAudit"];
                var list1 = (IEnumerable<dynamic>)dict1["AdminDashboardAudit"];
                var list2 = (IEnumerable<dynamic>)dict2["AdminDashboardAudit"];
                foreach (var item in list1)
                {
                    ((IDictionary<string, object>)item)["BusinessUnit"] = "FO";
                }
                foreach (var item in list2)
                {
                    ((IDictionary<string, object>)item)["BusinessUnit"] = "MO";
                }
                var combined = new List<dynamic>();
                combined.AddRange(list1);
                combined.AddRange(list2);
                var serializerSettings = new Newtonsoft.Json.JsonSerializerSettings
                {
                    ContractResolver = new Newtonsoft.Json.Serialization.DefaultContractResolver()
                };
                return new JsonResult(new { AdminDashboardAudit = combined }, serializerSettings);
            }
            catch (Exception e)
            {
                m_logger.LogError(e, "Error Calling Get @ api/Admin/Audit/Get/");
                return StatusCode(StatusCodes.Status500InternalServerError, e.Message);
            }
        }
    }
}