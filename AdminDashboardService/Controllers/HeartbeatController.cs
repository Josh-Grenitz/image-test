using System;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Authorization;
using Newtonsoft.Json.Linq;

namespace AdminDashboardService.Controllers
{
#if DEBUG
#else
    [Authorize]
#endif
    [Produces("application/json")]
    public class HeartbeatController : Controller
    {
        private readonly ILogger<HeartbeatController> m_logger;

        public HeartbeatController(ILogger<HeartbeatController> logger) => m_logger = logger;

#if DEBUG
#else
        [Authorize(Policy = "READ")]
#endif
        [HttpGet]
        [Route("api/Heartbeat")]
        public IActionResult Get()
        {
            string heartbeatResponse = $"AdminDashboard Alive and well at {DateTime.Now.ToString("M/d/yyy hh:mm")}";
            m_logger.LogInformation($"Returning heartbeat: {heartbeatResponse}");
            return Ok(heartbeatResponse);
        }
    }
}
