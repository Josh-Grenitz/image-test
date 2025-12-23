using AdminDashboardService.Dtos;
using AdminDashboardService.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using SB.AdminDashboard.EF.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AdminDashboardService.Controllers
{
    public class RequestController : Controller
    {
        private readonly ILogger<RequestController> _logger;
        private readonly IRequestDataAccessor _requestDataAccessor;
        
        public RequestController(ILogger<RequestController> logger, 
                               IRequestDataAccessor requestDataAccessor)
        {
            _logger = logger;
            _requestDataAccessor = requestDataAccessor;
        }

        // GET api/requests
        [HttpGet]
        [Route("api/requests")]
        [Authorize(Policy ="Dashboard:Read")]
        public async Task<ActionResult<List<RequestSummaryDto>>> GetAllRequests()
        {
            try
            {
                var result = await _requestDataAccessor.GetAllSummaryAsync();
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting all requests");
                return StatusCode(500, $"Error occurred while getting all requests: {ex.Message}");
            }
        }

        // GET api/requests/pending?userId={userId}
        [HttpGet]
        [Route("api/requests/pending")]
        [Authorize(Policy = "Dashboard:Read")]
        public async Task<ActionResult<List<RequestSummaryDto>>> GetPending([FromQuery] string userId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(userId))
                {
                    return BadRequest("UserId parameter is required");
                }

                var result = await _requestDataAccessor.GetPendingSummaryForUserAsync(userId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting pending requests for user {UserId}", userId);
                return StatusCode(500, $"Error occurred while getting pending requests: {ex.Message}");
            }
        }

        // GET api/requests/5
        [HttpGet]
        [Route("api/requests/{id}")]
        [Authorize(Policy = "Dashboard:Read")]
        public async Task<ActionResult<RequestDetailDto>> GetById(int id)
        {
            try
            {
                var result = await _requestDataAccessor.GetDetailByIdAsync(id);
                if (result == null) return NotFound();
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting request by id: {Id}", id);
                return StatusCode(500, $"Error occurred while getting request by id: {ex.Message}");
            }
        }

        // GET api/requests/check-active?objectName={objectName}&metaDataKey={metaDataKey}
        [HttpGet]
        [Route("api/requests/check-active")]
        [Authorize(Policy = "Dashboard:Read")]
        public async Task<ActionResult<bool>> CheckActiveRequest([FromQuery] string objectName, [FromQuery] string metaDataKey)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(objectName))
                    return BadRequest("ObjectName parameter is required");

                if (string.IsNullOrWhiteSpace(metaDataKey))
                    return BadRequest("MetaDataKey parameter is required");

                var hasActiveRequest = await _requestDataAccessor.HasActiveRequestAsync(objectName, metaDataKey);
                return Ok(hasActiveRequest);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while checking for active request for ObjectName: {ObjectName}, MetaDataKey: {MetaDataKey}", objectName, metaDataKey);
                return StatusCode(500, $"Error occurred while checking for active request: {ex.Message}");
            }
        }

        // POST api/requests
        [HttpPost]
        [Route("api/requests")]
        [Authorize(Policy = "Dashboard:Write")]
        public async Task<ActionResult<int>> Create([FromBody] CreateRequestDto dto)
        {
            try
            {
                var id = await _requestDataAccessor.CreateRequestAsync(dto);
                return CreatedAtAction(nameof(GetById), new { id }, id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while creating request");
                return StatusCode(500, $"Error occurred while creating request: {ex.Message}");
            }
        }

        // PUT api/requests/5
        [HttpPut]
        [Route("api/requests/{id}")]
        [Authorize(Policy = "Dashboard:Write")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateRequestDto dto)
        {
            try
            {
                await _requestDataAccessor.UpdateRequestAsync(id, dto);
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while updating request: {Id}", id);
                return StatusCode(500, $"Error occurred while updating request: {ex.Message}");
            }
        }

        [HttpGet]
        [Route("api/requests/check-outstanding")]
        [Authorize(Policy = "Dashboard:Read")]
        public async Task<IActionResult> CheckOutstandingRequests()
        {
            try
            {
                await _requestDataAccessor.CheckOutstandingRequestsAsync();

                return Ok("Successfully checked for outstanding requests.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while checking outstanding requests");
                return StatusCode(500, $"Error occurred while checking outstanding requests: {ex.Message}");
            }
        }
    }
}
