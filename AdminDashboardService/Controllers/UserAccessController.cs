using AdminDashboard.Controllers;
using AdminDashboardService.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Net;

namespace AdminDashboardService.Controllers
{
    [ApiController]
    [Route("api")]
    public class UserAccessController : ControllerBase
    {
        private readonly ILogger<UserAccessController> _logger;
        private readonly IUserAccessor _userAccessor;

        public UserAccessController(ILogger<UserAccessController> logger, IUserAccessor userAccessor)
        {
            _logger = logger;
            _userAccessor = userAccessor;
        }

        [HttpGet("UserAccess/GetRole")]
        [Authorize(Policy = "Dashboard:Read")]
        public IActionResult GetUserRole()
        {
            try
            {
                var userRoles = _userAccessor.GetCurrentUserRole();
                string userRole = string.Join(",", userRoles);
                return Ok(userRole);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return StatusCode((int)HttpStatusCode.InternalServerError, $"Error occurred while getting user roles: {ex.Message}");
            }
        }
    }
}
