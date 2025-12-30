using System;
using System.IO;
using System.Linq;
using System.Dynamic;
using System.Collections.Generic;
using System.Diagnostics;
using System.Data;
using Microsoft.Data.SqlClient;
using AdminDashboardService;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Authorization;

using CommonApiUtilities.Interfaces;
using CommonApiUtilities.Security.Interfaces;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Http;

namespace AdminDashboard.Controllers
{
#if DEBUG
#else
    [Authorize]
#endif
    [Produces("application/json")]
    // [EnableCors("AllowHeaders")]
    public class DashboardController : Controller
    {
        private readonly ILogger<DashboardController> m_logger;
        private readonly IApplicationConfiguration m_applicationConfiguration;
        private readonly ISqlCrud m_sqlCrud;
        private readonly IExpandoObjectHandler m_expandoObjectHandler;
        private readonly IJsonUtilities m_jsonUtilities;
        private readonly IFileUtilities m_fileUtilities;
        private readonly IActiveDirectoryCache m_activeDirectoryAuthenticationCache;

        public DashboardController(ILogger<DashboardController> logger,
                                   IApplicationConfiguration applicationConfiguration,
                                   IJsonUtilities jsonUtilities,
                                   IFileUtilities fileUtilities,
                                   ISqlCrud sqlCrud,
                                   IExpandoObjectHandler expandoObjectHandler,
                                   IActiveDirectoryCache activeDirectoryAuthenticationCache)
        {
            m_logger = logger;
            m_applicationConfiguration = applicationConfiguration;
            m_jsonUtilities = jsonUtilities;
            m_fileUtilities = fileUtilities;
            m_expandoObjectHandler = expandoObjectHandler;
            m_sqlCrud = sqlCrud;
            m_activeDirectoryAuthenticationCache = activeDirectoryAuthenticationCache;
        }

#if DEBUG
#else
        [Authorize(Policy = "CREATE")]
#endif
        [HttpPost]
        [Route("api/Admin/Dashboard/Create/{ApiEndpoint}")]
        public IActionResult Post(string ApiEndpoint, [FromBody] JObject newData)
        {
            try
            {
                ExpandoObject[] appMap = (ExpandoObject[])m_expandoObjectHandler.GetExpandoProperty("ApplicationMapping", m_applicationConfiguration.GetApplicationSqlConfiguration<object>("ApplicationMapping"));
                ExpandoObject endPoint = appMap.FirstOrDefault(ep => (ep as IDictionary<string, object>)["ApiEndpoint"].ToString().ToLower() == ApiEndpoint.ToLower());

                if ((string)m_expandoObjectHandler.GetExpandoProperty("Type", endPoint) == "SQL")
                {
                    if (!newData.HasValues)
                        throw new ArgumentNullException("newData", "Input empty");

                    m_sqlCrud.CreateDataInSqlTable(
                        m_jsonUtilities.ConvertJsonToDataTable(newData[ApiEndpoint]),
                        (string)m_expandoObjectHandler.GetExpandoProperty("Map", endPoint),
                        m_applicationConfiguration.GetApplicationFileConfiguration<string>("SqlDatabaseConnectionString"),
                        this.User.Identity.Name.ToString());
                    
                    var logData = new Dictionary<string, string>()
                    {
                        { "Application", (string)m_expandoObjectHandler.GetExpandoProperty("Application", endPoint) },
                        { "Configuration", (string)m_expandoObjectHandler.GetExpandoProperty("Configuration", endPoint) },
                        { "ApiEndpoint", (string)m_expandoObjectHandler.GetExpandoProperty("ApiEndpoint", endPoint) },
                        { "OldValue", "" },
                        { "NewValue", newData.ToString(Formatting.None) },
                        { "User", this.User.Identity.Name.ToString() }
                    };
                    
                    m_sqlCrud.LogForAudtit(m_applicationConfiguration.GetApplicationFileConfiguration<string>("AuditTableName"),
                                           logData,
                                           m_applicationConfiguration.GetApplicationFileConfiguration<string>("SqlDatabaseConnectionString"));

                    if (ApiEndpoint == "DODSDevOpsAdminDashboardMapping")
                        m_applicationConfiguration.RefreshMapping();
                    return Ok();
                }
                else
                {
                    string oldLocation = m_fileUtilities.UpdateFile(new DirectoryInfo(m_applicationConfiguration.GetApplicationFileConfiguration<string>("ArchiveFileLocation")), 
                                                                    new System.IO.FileInfo((string)m_expandoObjectHandler.GetExpandoProperty("Map", endPoint)),
                                                                    m_jsonUtilities.ConvertJsonToString(newData[ApiEndpoint]));

                    var logData = new Dictionary<string, string>()
                    {
                        { "Application", (string)m_expandoObjectHandler.GetExpandoProperty("Application", endPoint) },
                        { "Configuration", (string)m_expandoObjectHandler.GetExpandoProperty("Configuration", endPoint) },
                        { "ApiEndpoint", (string)m_expandoObjectHandler.GetExpandoProperty("ApiEndpoint", endPoint) },
                        { "OldValue", oldLocation },
                        { "NewValue", (string)m_expandoObjectHandler.GetExpandoProperty("Map", endPoint) },
                        { "User", this.User.Identity.Name.ToString() }
                    };

                    m_sqlCrud.LogForAudtit(m_applicationConfiguration.GetApplicationFileConfiguration<string>("AuditTableName"),
                                           logData,
                                           m_applicationConfiguration.GetApplicationFileConfiguration<string>("SqlDatabaseConnectionString"));
                    return Ok();
                }
            }
            catch (Exception e)
            {
                m_logger.LogError(e, $"Error Calling Post @ api/Admin/Dashboard/Create/{ApiEndpoint}");
                return StatusCode(StatusCodes.Status500InternalServerError, e.Message);
            }
        }

#if DEBUG
#else
        [Authorize(Policy = "READ")]
#endif
        [HttpGet]
        [Route("api/Admin/Dashboard/ReadSchema")]
        public IActionResult Get(string Application, string Config)
        {
            string queryString = $"Application = '{Application}' AND Configuration = '{Config}'";
            string columnName = "Map";
            try
            {
                string config = m_applicationConfiguration.GetApplicationFileConfiguration<string>("SqlDatabaseConnectionString");
                if (string.IsNullOrWhiteSpace(config)) {
                    throw new ArgumentException("Parameter cannot be null or empty string.", nameof(config));
                }
                using (SqlConnection conn = new SqlConnection(config))
                {
                    Console.Out.WriteLine("Config: " + config + '\n');

                    if (conn == null)
                    {
                        throw new ArgumentNullException(nameof(conn));
                    }
                    conn.Open();
                    if (conn.State != ConnectionState.Open)
                    {
                        throw new InvalidOperationException("Failed to connect to SQL server.");
                    }

                    string targetTableName = DataCache.Current.DashboardMapping.Select(queryString)[0][columnName].ToString();
                    string sqlStatement = $"SELECT * FROM {targetTableName};";
                    using (var cmd = new SqlCommand(sqlStatement, conn))
                    {
                        using (var reader = cmd.ExecuteReader())
                        {
                            string schemaJSON = JsonConvert.SerializeObject(reader.GetSchemaTable());
                            return Ok(schemaJSON);
                        }
                    }
                }
            }

            catch (Exception ex)
            {
                m_logger.LogError(ex, "Error Calling Get @ api/Admin/Audit/ReadSchema");
                var st = new StackTrace(ex, true);
                var frame = st.GetFrame(0);
                var line = frame.GetFileLineNumber();
                Console.Out.WriteLine(ex.Message + $"on line {line}\n" + ex.ToString() + ex.InnerException?.ToString());
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }

        }

#if DEBUG
#else
        [Authorize(Policy = "READ")]
#endif
        [HttpGet]
        [Route("api/Admin/Dashboard/Read/{ApiEndpoint}")]
        public IActionResult Get(string ApiEndpoint)
        {
            try
            {
                switch (ApiEndpoint)
                {
                    case "ApplicationMapping":
                        return Ok(m_applicationConfiguration.GetApplicationSqlConfiguration<object>("ApplicationMapping"));
                    case "ApplicationSqlMapping":
                        return Ok(m_applicationConfiguration.GetApplicationSqlConfiguration<object>("ApplicationSqlMapping"));
                    case "ApplicationFileMapping":
                        return Ok(m_applicationConfiguration.GetApplicationSqlConfiguration<object>("ApplicationFileMapping"));
                    case "ApplicationCacheMapping":
                        return Ok(m_applicationConfiguration.GetApplicationSqlConfiguration<object>("ApplicationCacheMapping"));
                    default:
                        ExpandoObject[] appMap = (ExpandoObject[])m_expandoObjectHandler.GetExpandoProperty("ApplicationMapping", m_applicationConfiguration.GetApplicationSqlConfiguration<object>("ApplicationMapping"));
                            ExpandoObject endPoint = appMap.FirstOrDefault(ep => (ep as IDictionary<string, object>)["ApiEndpoint"].ToString().ToLower() == ApiEndpoint.ToLower());
                        if (endPoint != null && (string)m_expandoObjectHandler.GetExpandoProperty("Type", endPoint) == "SQL")
                        {
                            m_sqlCrud.ReadDataFromSqlTable(
                                ApiEndpoint,
                                m_applicationConfiguration.GetMasterExpando(),
                                (string)m_expandoObjectHandler.GetExpandoProperty("Map", endPoint),
                                (string)m_expandoObjectHandler.GetExpandoProperty("GetConditions", endPoint),
                                m_applicationConfiguration.GetApplicationFileConfiguration<string>("SqlDatabaseConnectionString"));
                            
                            return Ok(m_expandoObjectHandler.GetExpandoProperty(ApiEndpoint, m_applicationConfiguration.GetMasterExpando()));
                        }
                        else if(endPoint == null)
                            {
                            return Ok($"Invalid endpoint: {ApiEndpoint}");
                        }
                        else
                        {
                            return Ok(m_fileUtilities.ReadFile(new System.IO.FileInfo(m_expandoObjectHandler.GetExpandoProperty("Map", endPoint).ToString())));
                        }
                }
            }
            catch (Exception e)
            {
                m_logger.LogError(e, $"Error Calling Get @ api/Admin/Dashboard/Read/{ApiEndpoint}");
                return StatusCode(StatusCodes.Status500InternalServerError, e.Message);
            }
        }
#if DEBUG
#else
        [Authorize(Policy = "UPDATE")]
#endif
        [HttpPut]
        [Route("api/Admin/Dashboard/Update/{ApiEndpoint}")]
        public IActionResult Put(string ApiEndpoint, [FromBody] JObject data)
        {
            try
            {
                ExpandoObject[] appMap = (ExpandoObject[])m_expandoObjectHandler.GetExpandoProperty("ApplicationMapping", m_applicationConfiguration.GetApplicationSqlConfiguration<object>("ApplicationMapping"));
                ExpandoObject endPoint = appMap.FirstOrDefault(ep => (ep as IDictionary<string, object>)["ApiEndpoint"].ToString().ToLower() == ApiEndpoint.ToLower());

                if ((string)m_expandoObjectHandler.GetExpandoProperty("Type", endPoint) == "SQL")
                {
                    if (!data.HasValues)
                        throw new ArgumentNullException("data", "Input is empty!");

                    m_sqlCrud.UpdateRowInSqlTable(
                        (string)m_expandoObjectHandler.GetExpandoProperty("Map", endPoint),
                        m_jsonUtilities.ConvertJsonToDictionaryCleaned(data[ApiEndpoint + ".old"]),
                        m_jsonUtilities.ConvertJsonToDictionaryCleaned(data[ApiEndpoint + ".new"]),
                        m_applicationConfiguration.GetApplicationFileConfiguration<string>("SqlDatabaseConnectionString"),
                        this.User.Identity.Name.ToString());

                    var logData = new Dictionary<string, string>()
                    {
                        { "Application", (string)m_expandoObjectHandler.GetExpandoProperty("Application", endPoint) },
                        { "Configuration", (string)m_expandoObjectHandler.GetExpandoProperty("Configuration", endPoint) },
                        { "ApiEndpoint", (string)m_expandoObjectHandler.GetExpandoProperty("ApiEndpoint", endPoint) },
                        { "OldValue", data[ApiEndpoint + ".old"].ToString(Formatting.None) },
                        { "NewValue", data[ApiEndpoint + ".new"].ToString(Formatting.None) },
                        { "User", this.User.Identity.Name.ToString() }
                    };

                    m_sqlCrud.LogForAudtit(m_applicationConfiguration.GetApplicationFileConfiguration<string>("AuditTableName"),
                                           logData,
                                           m_applicationConfiguration.GetApplicationFileConfiguration<string>("SqlDatabaseConnectionString"));
                   
                    if (ApiEndpoint == "DODSDevOpsAdminDashboardMapping")
                        m_applicationConfiguration.RefreshMapping();
                    return Ok();
                }
                else
                {
                    string oldLocation = m_fileUtilities.UpdateFile(new DirectoryInfo(m_applicationConfiguration.GetApplicationFileConfiguration<string>("ArchiveFileLocation")), 
                                                                    new System.IO.FileInfo((string)m_expandoObjectHandler.GetExpandoProperty("Map", endPoint)),
                                                                    m_jsonUtilities.ConvertJsonToString(data[ApiEndpoint]));
                    
                    var logData = new Dictionary<string, string>()
                    {
                        { "Application", (string)m_expandoObjectHandler.GetExpandoProperty("Application", endPoint) },
                        { "Configuration", (string)m_expandoObjectHandler.GetExpandoProperty("Configuration", endPoint) },
                        { "ApiEndpoint", (string)m_expandoObjectHandler.GetExpandoProperty("ApiEndpoint", endPoint) },
                        { "OldValue", oldLocation },
                        { "NewValue", (string)m_expandoObjectHandler.GetExpandoProperty("Map", endPoint) },
                        { "User", this.User.Identity.Name.ToString() }
                    };

                    m_sqlCrud.LogForAudtit(m_applicationConfiguration.GetApplicationFileConfiguration<string>("AuditTableName"),
                                           logData,
                                           m_applicationConfiguration.GetApplicationFileConfiguration<string>("SqlDatabaseConnectionString"));
                   
                    return Ok();
                }
            }
            catch (Exception e)
            {
                m_logger.LogError(e, $"Error Calling Put @ api/Admin/Dashboard/Update/{ApiEndpoint}");
                return StatusCode(StatusCodes.Status500InternalServerError, e.Message);
            }
        }

#if DEBUG
#else
        [Authorize(Policy = "DELETE")]
#endif
        [HttpDelete]
        [Route("api/Admin/Dashboard/Delete/{ApiEndpoint}")]
        public IActionResult Delete(string ApiEndpoint, [FromBody] JObject deadData)
        {
            try
            {
                ExpandoObject[] appMap = (ExpandoObject[])m_expandoObjectHandler.GetExpandoProperty("ApplicationMapping", m_applicationConfiguration.GetApplicationSqlConfiguration<object>("ApplicationMapping"));
                ExpandoObject endPoint = appMap.FirstOrDefault(ep => (ep as IDictionary<string, object>)["ApiEndpoint"].ToString().ToLower() == ApiEndpoint.ToLower());

                if ((string)m_expandoObjectHandler.GetExpandoProperty("Type", endPoint) == "SQL")
                {
                    if (!deadData.HasValues)
                        throw new ArgumentNullException("deadData", "Json input empty");

                    m_sqlCrud.DeleteRowFromSqlTable(
                        (string)m_expandoObjectHandler.GetExpandoProperty("Map", endPoint),
                        m_jsonUtilities.ConvertJsonToDictionaryCleaned(deadData[ApiEndpoint]),
                        m_applicationConfiguration.GetApplicationFileConfiguration<string>("SqlDatabaseConnectionString"));

                    var logData = new Dictionary<string, string>()
                    {
                        { "Application", (string)m_expandoObjectHandler.GetExpandoProperty("Application", endPoint) },
                        { "Configuration", (string)m_expandoObjectHandler.GetExpandoProperty("Configuration", endPoint) },
                        { "ApiEndpoint", (string)m_expandoObjectHandler.GetExpandoProperty("ApiEndpoint", endPoint) },
                        { "OldValue", deadData.ToString(Formatting.None) },
                        { "NewValue", "" },
                        { "User", this.User.Identity.Name.ToString() }
                    };

                    m_sqlCrud.LogForAudtit(m_applicationConfiguration.GetApplicationFileConfiguration<string>("AuditTableName"),
                                           logData,
                                           m_applicationConfiguration.GetApplicationFileConfiguration<string>("SqlDatabaseConnectionString"));
                                       
                    if (ApiEndpoint == "DODSDevOpsAdminDashboardMapping")
                        m_applicationConfiguration.RefreshMapping();
                    return Ok();
                }
                else
                {
                    string oldLocation = m_fileUtilities.DeleteFile(new DirectoryInfo(m_applicationConfiguration.GetApplicationFileConfiguration<string>("ArchiveFileLocation")),
                                                                    new System.IO.FileInfo((string)m_expandoObjectHandler.GetExpandoProperty("Map",
                                                                    endPoint)));

                    var logData = new Dictionary<string, string>()
                    {
                        { "Application", (string)m_expandoObjectHandler.GetExpandoProperty("Application", endPoint) },
                        { "Configuration", (string)m_expandoObjectHandler.GetExpandoProperty("Configuration", endPoint) },
                        { "ApiEndpoint", (string)m_expandoObjectHandler.GetExpandoProperty("ApiEndpoint", endPoint) },
                        { "OldValue", oldLocation },
                        { "NewValue", "" },
                        { "User", this.User.Identity.Name.ToString() }
                    };

                    m_sqlCrud.LogForAudtit(m_applicationConfiguration.GetApplicationFileConfiguration<string>("AuditTableName"),
                                           logData,
                                           m_applicationConfiguration.GetApplicationFileConfiguration<string>("SqlDatabaseConnectionString"));
                    
                    return Ok();
                }
            }
            catch (Exception e)
            {
                m_logger.LogError(e, $"Error Calling Delete @ api/Admin/Dashboard/Delete/{ApiEndpoint}");
                return StatusCode(StatusCodes.Status500InternalServerError, e.Message);
            }
        }

#if DEBUG
#else
        [Authorize(Policy = "REFRESH")]
#endif
        [HttpGet]
        [Route("api/Admin/Dashboard/RefreshMapping")]
        public IActionResult RefreshMapping()
        {
            try
            {
                m_applicationConfiguration.RefreshMapping();
                return Ok();
            }
            catch (Exception e)
            {
                m_logger.LogError(e, $"Error Calling RefreshMapping @ api/Admin/Dashboard/RefreshMapping");
                return StatusCode(StatusCodes.Status500InternalServerError, e.Message);
            }
        }

#if DEBUG
#else
        [Authorize(Policy = "REFRESH")]
#endif
        [HttpGet]
        [Route("api/Admin/Dashboard/RefreshRoles")]
        public IActionResult RefreshRoles()
        {
            try
            {
                m_logger.LogInformation($"Loading AdAuthenticationHelperCache information from SQL {m_applicationConfiguration.GetApplicationFileConfiguration<string>("SqlDatabaseConnectionString")}");
                m_activeDirectoryAuthenticationCache.RefreshMiddlewareCache();
                return Ok();
            }
            catch (Exception e)
            {
                m_logger.LogError(e, $"Error Calling RefreshUsers @ api/Admin/Dashboard/RefreshUsers");
                return StatusCode(StatusCodes.Status500InternalServerError, e.Message);
            }
        }
    }
}
