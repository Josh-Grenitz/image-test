using AdminDashboardService.Dtos;
using AdminDashboardService.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace AdminDashboardService.Controllers
{
    public class ApproverController : Controller
    {
        private readonly IApprovalDataAccessor _approvalDataAccessor;
        private readonly ILogger<ApproverController> _logger;
        public ApproverController(ILogger<ApproverController> logger, IApprovalDataAccessor approvalDataAccessor)
        {
            _logger = logger;
            _approvalDataAccessor = approvalDataAccessor;
        }


        /// <summary>
        /// Unified endpoint to check if a user can approve for a configuration
        /// Optionally validates against a specific request to prevent self-approval
        /// </summary>
        /// <param name="configurationId">The configuration ID to check</param>
        /// <param name="userId">The user ID to check (passed from UI)</param>
        /// <param name="requestId">Optional: specific request ID to validate against (prevents self-approval)</param>
        /// <returns>True if the user can approve, false otherwise</returns>
        [HttpGet]
        [Route("api/approve/CanApprove")]
        [Authorize(Policy = "Dashboard:Read")]
        public async Task<ActionResult<bool>> CanUserApprove([FromQuery] string userId, [FromQuery] int requestId)
        {
            try
            {
                _logger.LogInformation("Checking if user {UserId} can approve request {RequestId}", userId, requestId);
                // Use the unified method that handles both scenarios
                var canApprove = await _approvalDataAccessor.IsUserApproverForConfigurationAsync(userId, requestId);
                _logger.LogInformation("User {UserId} can approve request {RequestId}: {CanApprove}", userId, requestId, canApprove);
                return Ok(canApprove);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if user {UserId} can approve request {RequestId}", userId, requestId);
                return StatusCode(500, $"An error occurred: {ex.Message}");
            }
        }

        /// <summary>
        /// Check if an object is configured in the ApprovalObjectConfiguration table
        /// </summary>
        /// <param name="objectName">The object name to check</param>
        /// <returns>True if the object is configured, false otherwise</returns>
        [HttpGet]
        [Route("api/approve/CheckConfiguration")]
        [Authorize(Policy = "Dashboard:Read")]
        public async Task<ActionResult<bool>> CheckObjectConfiguration([FromQuery] string objectName)
        {
            try
            {
                _logger.LogInformation("Checking if object {ObjectName} is configured in ApprovalObjectConfiguration", objectName);
                
                if (string.IsNullOrWhiteSpace(objectName))
                {
                    _logger.LogWarning("CheckObjectConfiguration failed: ObjectName parameter is required");
                    return BadRequest("ObjectName parameter is required");
                }

                var configuration = await _approvalDataAccessor.GetConfigurationByObjectNameAsync(objectName);
                var isConfigured = configuration != null;
                
                _logger.LogInformation("Object {ObjectName} configuration status: {IsConfigured}", objectName, isConfigured);
                return Ok(isConfigured);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while checking configuration for ObjectName: {ObjectName}", objectName);
                return StatusCode(500, $"An error occurred: {ex.Message}");
            }
        }

        /// <summary>
        /// Deny a request
        /// </summary>
        /// <param name="requestId">The request ID to deny</param>
        /// <param name="userId">The user ID who is denying the request</param>
        /// <returns>True if successfully denied, false otherwise</returns>
        [HttpPost]
        [Route("api/approve/DenyRequest")]
        [Authorize(Policy = "Dashboard:Read")]
        public async Task<ActionResult<bool>> DenyRequest([FromQuery] int requestId, [FromQuery] string userId,[FromQuery] string comments = "")
        {
            try
            {
                _logger.LogInformation("User {UserId} attempting to deny request {RequestId}", userId, requestId);
                if (requestId <= 0)
                {
                    _logger.LogWarning("DenyRequest failed: Invalid requestId {RequestId}", requestId);
                    return BadRequest("Request ID must be greater than 0");
                }

                if (string.IsNullOrWhiteSpace(userId))
                {
                    _logger.LogWarning("DenyRequest failed: User ID is required");
                    return BadRequest("User ID is required");
                }

                // First check if the user can approve this request (has permission and didn't create it)
                var canApprove = await _approvalDataAccessor.IsUserApproverForConfigurationAsync(userId, requestId);
                if (!canApprove)
                {
                    _logger.LogWarning("User {UserId} does not have permission to deny request {RequestId}", userId, requestId);
                    return Forbid("User does not have permission to deny this request");
                }

                // Deny the request
                var denied = await _approvalDataAccessor.DenyRequest(requestId, userId, comments);
                _logger.LogInformation("Request {RequestId} denied by user {UserId}: {Denied}", requestId, userId, denied);
                return Ok(denied);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error denying request {RequestId} by user {UserId}", requestId, userId);
                return StatusCode(500, $"An error occurred: {ex.Message}");
            }
        }

        /// <summary>
        /// Approve a request
        /// </summary>
        /// <param name="request">The approval request containing request ID, user ID, and required comments</param>
        /// <returns>Approval result with success status and message</returns>
        [HttpPost]
        [Route("api/approve/ApproveRequest")]
        [Authorize(Policy = "Dashboard:Read")]
        public async Task<IActionResult> ApproveRequest([FromBody] ApproveRequestDto request)
        {
            try
            {
                _logger.LogInformation("User {UserId} attempting to approve request {RequestId}", request.UserId, request.RequestId);
                if (!ModelState.IsValid)
                {
                    _logger.LogWarning("ApproveRequest failed: Invalid model state for user {UserId} and request {RequestId}", request.UserId, request.RequestId);
                    return BadRequest(ModelState);
                }

                // First check if the user can approve this request (has permission and didn't create it)
                var canApprove = await _approvalDataAccessor.IsUserApproverForConfigurationAsync(request.UserId, request.RequestId);
                if (!canApprove)
                {
                    _logger.LogWarning("User {UserId} does not have permission to approve request {RequestId}", request.UserId, request.RequestId);
                    return StatusCode(403, ApiResponseDto.FailureResponse("User does not have permission to approve this request"));
                }

                // Approve the request with required comments
                var result = await _approvalDataAccessor.ApproveRequest(request.RequestId, request.UserId, request.Comments);
                if (result.Success)
                {
                    _logger.LogInformation("Request {RequestId} approved by user {UserId}", request.RequestId, request.UserId);
                    return Ok(ApiResponseDto.SuccessResponse(result.Message));
                }
                else
                {
                    _logger.LogWarning("Approval failed for request {RequestId} by user {UserId}: {Message}", request.RequestId, request.UserId, result.Message);
                    return BadRequest(ApiResponseDto.FailureResponse(result.Message));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error approving request {RequestId} by user {UserId}", request.RequestId, request.UserId);
                return StatusCode(500, ApiResponseDto.FailureResponse($"An error occurred: {ex.Message}"));
            }
        }

        /// <summary>
        /// Send back a request to the requester for modifications
        /// Resets the request status to Pending
        /// </summary>
        /// <param name="requestId">The request ID to send back</param>
        /// <param name="userId">The user ID who is sending back the request</param>
        /// <param name="comments">Optional comments explaining why the request is being sent back</param>
        /// <returns>True if successfully sent back, false otherwise</returns>
        [HttpPost]
        [Route("api/approve/SendBackRequest")]
        [Authorize(Policy = "Dashboard:Read")]
        public async Task<IActionResult> SendBackRequest([FromQuery] int requestId, [FromQuery] string userId, [FromQuery] string comments = "")
        {
            try
            {
                _logger.LogInformation("User {UserId} attempting to send back request {RequestId}", userId, requestId);
                if (requestId <= 0)
                {
                    _logger.LogWarning("SendBackRequest failed: Invalid requestId {RequestId}", requestId);
                    return BadRequest(ApiResponseDto.FailureResponse("Request ID must be greater than 0"));
                }

                if (string.IsNullOrWhiteSpace(userId))
                {
                    _logger.LogWarning("SendBackRequest failed: User ID is required");
                    return BadRequest(ApiResponseDto.FailureResponse("User ID is required"));
                }

                // First check if the user can approve this request (has permission and didn't create it)
                var canApprove = await _approvalDataAccessor.IsUserApproverForConfigurationAsync(userId, requestId);
                if (!canApprove)
                {
                    _logger.LogWarning("User {UserId} does not have permission to send back request {RequestId}", userId, requestId);
                    return StatusCode(403, ApiResponseDto.FailureResponse("User does not have permission to send back this request"));
                }

                // Send back the request
                var sentBack = await _approvalDataAccessor.SendBackRequest(requestId, userId, comments);
                if (sentBack)
                {
                    _logger.LogInformation("Request {RequestId} sent back by user {UserId}", requestId, userId);
                    return Ok(ApiResponseDto.SuccessResponse("Request has been sent back to the requester for modifications"));
                }
                else
                {
                    _logger.LogWarning("Failed to send back request {RequestId} by user {UserId}", requestId, userId);
                    return BadRequest(ApiResponseDto.FailureResponse("Failed to send back request"));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending back request {RequestId} by user {UserId}", requestId, userId);
                return StatusCode(500, ApiResponseDto.FailureResponse($"An error occurred: {ex.Message}"));
            }
        }
    }
}
