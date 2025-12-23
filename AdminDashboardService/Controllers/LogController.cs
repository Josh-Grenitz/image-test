using System;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Authorization;

using CommonApiUtilities.Interfaces;
using System.IO;
using Microsoft.AspNetCore.Http;

namespace AdminDashboard.Controllers
{
#if DEBUG
#else
    [Authorize]
#endif
    [Produces("application/json")]
    public class LogController : Controller
    {
        private readonly ILogger<LogController> m_logger;
        private readonly IApplicationConfiguration m_applicationConfiguration;
        private readonly IFileUtilities m_fileUtilities;
        private readonly IExpandoObjectHandler m_expandoObjectHandler;

        public LogController(ILogger<LogController> logger, IApplicationConfiguration applicationConfiguration, IFileUtilities fileUtilities, IExpandoObjectHandler expandoObjectHandler)
        {
            m_logger = logger;
            m_applicationConfiguration = applicationConfiguration;
            m_fileUtilities = fileUtilities;
            m_expandoObjectHandler = expandoObjectHandler;
        }

        [HttpGet]
        [Route("api/Admin/Log/Read")]
        [Authorize(Policy = "Dashboard:Read")]
        public IActionResult Get()
        {
            try
            {
                m_fileUtilities.MapLogDirectory(new System.IO.DirectoryInfo(m_applicationConfiguration.GetApplicationFileConfiguration<string>("LogLocation")), m_applicationConfiguration.GetMasterExpando());

                return Ok(m_expandoObjectHandler.GetExpandoProperty("LogMapping", m_applicationConfiguration.GetMasterExpando()));
            }
            catch (Exception e)
            {
                m_logger.LogError(e, "Error Calling Get @ api/Admin/Log/Read/");
                return StatusCode(StatusCodes.Status500InternalServerError, e.Message);
            }
        }


        [HttpGet]
        [Route("api/Admin/Log/Read/{directoryName}/{logFileName}")]
        [Authorize(Policy = "Dashboard:Read")]
        public IActionResult Get(string directoryName, string logFileName)
        {
            string ApiEndpoint = $"{directoryName}\\{logFileName}";
            try
            {
                string output = m_applicationConfiguration.GetApplicationFileConfiguration<string>("LogLocation");                                
                var file_info = new System.IO.FileInfo($"{output}\\{ApiEndpoint}");
                if(!file_info.Exists)
                {
                    throw new FileNotFoundException("This file was not found.");
                }
                return Ok(m_fileUtilities.ReadFileReverse(file_info));
            }
            catch (FileNotFoundException e)
            {
                m_logger.LogError(e, $"Error Calling Get @ api/Admin/Log/Read/{directoryName}/{logFileName}");
                return StatusCode(StatusCodes.Status500InternalServerError, e.Message);
            }
            catch (Exception e)
            {
                m_logger.LogError(e, $"Error Calling Get @ api/Admin/Log/Read/{directoryName}/{logFileName}");
                return StatusCode(StatusCodes.Status500InternalServerError, e.Message);
            }
        }
    }
}
