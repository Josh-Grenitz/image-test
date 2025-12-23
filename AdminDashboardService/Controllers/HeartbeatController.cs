using System;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Authorization;
using Newtonsoft.Json.Linq;

namespace AdminDashboardService.Controllers
{

    public class HeartbeatController : Controller
    {
        private readonly ILogger<HeartbeatController> m_logger;

        public HeartbeatController(ILogger<HeartbeatController> logger) => m_logger = logger;


        [HttpGet]
        [Route("api/Heartbeat")]
        [Authorize(Policy = "Dashboard:Read")]
        public IActionResult Get()
        {
            string heartbeatResponse = $"AdminDashboard Alive and well at {DateTime.Now.ToString("M/d/yyy hh:mm")}";
            m_logger.LogInformation($"Returning heartbeat: {heartbeatResponse}");
            return Ok(heartbeatResponse);
        }
    }
}
