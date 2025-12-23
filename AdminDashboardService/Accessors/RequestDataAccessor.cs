using AdminDashboardService.Interfaces;
using AdminDashboardService.Dtos;
using AdminDashboardService.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SB.AdminDashboard.EF.Data;
using SB.AdminDashboard.EF.Models;
using AutoMapper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Transactions;
using Microsoft.Extensions.Configuration;

namespace AdminDashboardService.Accessors
{
    public class RequestDataAccessor : IRequestDataAccessor
    {
        private readonly DODSDevOps _dODSDevOpsContext;
        private readonly ILogger<RequestDataAccessor> _logger;
        private readonly IApprovalDataAccessor _approvalDataAccessor;
        private readonly IMapper _mapper;
        private readonly IEmailService _emailService;
        private readonly IConfiguration _config;
        private readonly string _dashboardClientBaseUrl;

        public RequestDataAccessor(DODSDevOps dODSDevOpsContext,
                                  ILogger<RequestDataAccessor> logger,
                                  IApprovalDataAccessor approvalDataAccessor,
                                  IMapper mapper,
                                  IEmailService emailService,
                                  IConfiguration configuration)
        {
            _dODSDevOpsContext = dODSDevOpsContext;
            _logger = logger;
            _approvalDataAccessor = approvalDataAccessor;
            _mapper = mapper;
            _emailService = emailService;
            _config = configuration;
            _dashboardClientBaseUrl = _config.GetValue<string>("AdminDashboardClientUrl") ?? string.Empty;
        }

        public async Task<List<RequestView>> GetPendingForUserAsync(string userId)
        {
            try
            {
                _logger.LogInformation("Getting actionable requests for user {UserId}", userId);

                var excludedStatuses = new[]
                {
            (int)ApprovalStatusEnum.Denied,
            (int)ApprovalStatusEnum.Completed
        };

                var userIdLower = userId.ToLower();

                // STEP 1: Get all pending requests where this user is an approver
                var query =
                    from rv in _dODSDevOpsContext.RequestViews
                    join r in _dODSDevOpsContext.Requests
                         on rv.RequestId equals r.RequestId
                    join au in _dODSDevOpsContext.ApprovingUsers
                         on r.ConfigurationId equals au.ConfigurationId
                    where au.UserId.ToLower() == userIdLower
                       && !excludedStatuses.Contains(rv.ApprovalStatus ?? 0)
                    select new
                    {
                        RequestView = rv,
                        Request = r,
                        UserApprover = au
                    };

                // STEP 2: Filter using pure LINQ (NO LOOPS)
                var result =
                    await query
                        .Where(item =>

                            // (A) User has NOT already approved at their level
                            !_dODSDevOpsContext.Approvals.Any(a =>
                                a.RequestId == item.Request.RequestId &&
                                a.LevelId == item.UserApprover.LevelId &&
                                a.UserId.ToLower() == userIdLower)

                            &&

                            // (B) Level 1 can approve anytime → no prereq
                            (
                                item.UserApprover.LevelId == 1 ||

                                // (C) Higher levels require previous level approval
                                _dODSDevOpsContext.Approvals.Any(a =>
                                    a.RequestId == item.Request.RequestId &&
                                    a.LevelId == item.UserApprover.LevelId - 1)
                            )
                        )
                        .Select(item => item.RequestView)
                        .Distinct()
                        .OrderByDescending(rv => rv.RequestedTime)
                        .ToListAsync();

                _logger.LogInformation("Found {Count} actionable requests for user {UserId}", result.Count, userId);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting actionable requests for user {UserId}.", userId);
                return new List<RequestView>();
            }
        }


        public async Task<List<RequestView>> GetAllRequestViewsAsync()
        {
            try
            {
                return await _dODSDevOpsContext.RequestViews
                    .OrderByDescending(rv => rv.RequestedTime)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all request views.");
                return new List<RequestView>();
            }
        }

        public async Task<Request> GetByIdAsync(int id)
        {
            return await _dODSDevOpsContext.Requests
                .FirstOrDefaultAsync(r => r.RequestId == id);
        }

        public async Task<Request> InsertAsync(Request request)
        {
            _dODSDevOpsContext.Requests.Add(request);
            await _dODSDevOpsContext.SaveChangesAsync();
            return request;
        }

        public async Task UpdateAsync(Request request)
        {
            _dODSDevOpsContext.Requests.Update(request);
            await _dODSDevOpsContext.SaveChangesAsync();
        }

        public async Task<bool> HasActiveRequestAsync(string objectName, string metaDataKey)
        {
            try
            {
                // Define active status IDs: Pending, FirstReview, FinalReview
                // Exclude Denied and Completed as these are final states
                var activeStatusIds = new[]
                {
                    (int)ApprovalStatusEnum.Pending,
                    (int)ApprovalStatusEnum.FirstReview,
                    (int)ApprovalStatusEnum.FinalReview
                };

                // Check if there's any request with active status for the same object and metadata key
                var hasActiveRequest = await _dODSDevOpsContext.Requests
                    .Join(_dODSDevOpsContext.RequestStatuses,
                          r => r.RequestId,
                          rs => rs.RequestId,
                          (r, rs) => new { Request = r, Status = rs })
                    .AnyAsync(joined => joined.Request.ObjectName == objectName
                                     && joined.Request.MetaDataKey == metaDataKey
                                     && activeStatusIds.Contains(joined.Status.StatusId));

                return hasActiveRequest;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking for active request for ObjectName: {ObjectName}, MetaDataKey: {MetaDataKey}", objectName, metaDataKey);
                return false; // Return false on error to allow the request to proceed
            }
        }

        // New methods replacing service layer functionality

        private void ValidateCreateRequest(CreateRequestDto dto)
        {
            if (dto == null)
                throw new ArgumentNullException(nameof(dto));

            if (string.IsNullOrWhiteSpace(dto.ObjectName))
                throw new ArgumentException("ObjectName is required and must be configured in ApprovalObjectConfiguration", nameof(dto.ObjectName));

            if (string.IsNullOrWhiteSpace(dto.MetaDataKey))
                throw new ArgumentException("MetaDataKey is required", nameof(dto.MetaDataKey));

            if (string.IsNullOrWhiteSpace(dto.Operation))
                throw new ArgumentException("Operation is required", nameof(dto.Operation));

            if (string.IsNullOrWhiteSpace(dto.RequestingUserId))
                throw new ArgumentException("RequestingUserId is required", nameof(dto.RequestingUserId));

            if (string.IsNullOrWhiteSpace(dto.RequestingComments))
                throw new ArgumentException("RequestingComments is required", nameof(dto.RequestingComments));

            if (string.IsNullOrWhiteSpace(dto.Type))
                throw new ArgumentException("Type is required", nameof(dto.Type));

            if (string.IsNullOrWhiteSpace(dto.UpdatedData))
                throw new ArgumentException("UpdatedData is required", nameof(dto.UpdatedData));
        }

        private void ValidateUpdateRequest(UpdateRequestDto dto)
        {
            if (dto == null)
                throw new ArgumentNullException(nameof(dto));

            if (string.IsNullOrWhiteSpace(dto.UpdatedData))
                throw new ArgumentException("UpdatedData is required", nameof(dto.UpdatedData));

            if (string.IsNullOrWhiteSpace(dto.LastUpdatedBy))
                throw new ArgumentException("LastUpdatedBy is required", nameof(dto.LastUpdatedBy));
        }

        public async Task<List<RequestSummaryDto>> GetPendingSummaryForUserAsync(string userId)
        {
            var views = await GetPendingForUserAsync(userId);
            var summaryDtos = _mapper.Map<List<RequestSummaryDto>>(views);
            
            // Enrich with approval level data
            foreach (var dto in summaryDtos)
            {
                await EnrichWithApprovalLevelsAsync(dto);
            }
            
            return summaryDtos;
        }

        public async Task<RequestDetailDto> GetDetailByIdAsync(int id)
        {
            var request = await GetByIdAsync(id);
            return _mapper.Map<RequestDetailDto>(request);
        }

        public async Task<List<RequestSummaryDto>> GetAllSummaryAsync()
        {
            var requestViews = await GetAllRequestViewsAsync();
            var summaryDtos = _mapper.Map<List<RequestSummaryDto>>(requestViews);
            
            // Enrich with approval level data
            foreach (var dto in summaryDtos)
            {
                await EnrichWithApprovalLevelsAsync(dto);
            }
            
            return summaryDtos;
        }

        public async Task<int> CreateRequestAsync(CreateRequestDto dto)
        {
            ValidateCreateRequest(dto);

            // Look up the configuration for this object type
            var configuration = await _approvalDataAccessor.GetConfigurationByObjectNameAsync(dto.ObjectName);
            if (configuration == null)
            {
                throw new InvalidOperationException($"No approval configuration found for object: {dto.ObjectName}");
            }

            // Get approving users BEFORE the transaction to avoid transaction scope issues
            var approvingUsers = await _approvalDataAccessor.GetApprovingUsersAsync(configuration.Id);

            var now = DateTime.Now;

            var request = new Request
            {
                ConfigurationId = configuration.Id,
                MetaDataKey = dto.MetaDataKey,
                ObjectName = dto.ObjectName,
                Operation = dto.Operation,
                RequestingUserId = dto.RequestingUserId,
                RequestingComments = dto.RequestingComments,
                RequestedTime = now,
                Type = dto.Type,
                OriginalData = dto.OriginalData,
                UpdatedData = dto.UpdatedData,
                LastUpdatedTime = now,
                LastUpdatedBy = dto.RequestingUserId
            };

            // Use transaction scope to ensure both operations succeed or both fail
            using var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled);

            try
            {
                await InsertAsync(request);

                // initial status = Pending
                await _approvalDataAccessor.UpdateRequestStatusAsync(request.RequestId, (int)ApprovalStatusEnum.Pending, updatedBy: dto.RequestingUserId);

                scope.Complete();
            }
            catch
            {
                // Transaction will be automatically rolled back if scope.Complete() is not called
                throw;
            }

            //Send email notifications after successful transaction(outside transaction scope)
            //Pass the pre - fetched approving users to avoid database access after transaction
            try
            {
                await SendRequestNotificationEmailAsync(request.RequestId, dto.ObjectName, dto.RequestingUserId, approvingUsers);
            }
            catch (Exception emailEx)
            {
                // Log email error but don't fail the request creation
                // The business process should continue even if email notification fails
                _logger.LogWarning(emailEx, "Email notification failed for request {RequestId}: {Message}", request.RequestId, emailEx.Message);
            }

            return request.RequestId;
        }

        public async Task UpdateRequestAsync(int id, UpdateRequestDto dto)
        {
            ValidateUpdateRequest(dto);

            var request = await GetByIdAsync(id);
            if (request == null) throw new KeyNotFoundException();

            request.UpdatedData = dto.UpdatedData;
            request.RequestingComments = dto.RequestingComments;
            request.LastUpdatedBy = dto.LastUpdatedBy;
            request.LastUpdatedTime = DateTime.Now;

            await UpdateAsync(request);
        }

        private async Task SendRequestNotificationEmailAsync(int requestId, string objectName, string requestingUserId, List<ApprovingUser> approvingUsers)
        {
            try
            {
                if (approvingUsers == null || approvingUsers.Count == 0)
                {
                    // Log warning but don't fail the request
                    return;
                }

                // Extract and validate email addresses from UserIds
                var approverEmails = new List<string>();
                foreach (var user in approvingUsers)
                {
                    var email = user?.UserId;
                    if (!string.IsNullOrEmpty(email))
                    {
                        approverEmails.Add(email);
                    }
                }

                if (approverEmails.Count == 0)
                {
                    // No valid approver emails found, exit gracefully
                    return;
                }
                List<string> ccList = new List<string>();
                ccList.Add(requestingUserId);
                // Create email content
                var subject = $"New Request Submitted for Approval - {objectName} (Request #{requestId})";
                var body = CreateEmailBody(requestId, objectName, requestingUserId);
                _emailService.SendEmail(approverEmails, ccList, subject, body);

            }
            catch (Exception ex)
            {
                // Log error but don't fail the request creation
                // Email failures should not impact the business process
                _logger.LogError(ex, "Error sending request notification email for request {RequestId}", requestId);
            }
        }

        private string BuildRequestLink(int requestId)
        {
            if (string.IsNullOrWhiteSpace(_dashboardClientBaseUrl))
            {
                return string.Empty;
            }

            var trimmedBase = _dashboardClientBaseUrl.TrimEnd('/');
            return $"{trimmedBase}/Dashboard/Requests?requestId={requestId}";
        }

        private string GetRequestLinkMarkup(int requestId)
        {
            var requestLink = BuildRequestLink(requestId);
            var href = string.IsNullOrEmpty(requestLink) ? "#" : requestLink;

            return $"<a class='highlight' href='{href}' target='_blank' rel='noopener noreferrer'>#{requestId}</a>";
        }

        private string CreateEmailBody(int requestId, string objectName, string requestingUser)
        {
            var body = $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; margin: 20px; }}
        .header {{ background-color: #f8f9fa; padding: 15px; border-left: 4px solid #007bff; }}
        .content {{ padding: 20px; }}
        .details {{ background-color: #f8f9fa; padding: 15px; margin: 15px 0; border-radius: 5px; }}
        .footer {{ margin-top: 30px; padding-top: 15px; border-top: 1px solid #dee2e6; color: #6c757d; }}
        .highlight {{ color: #007bff; font-weight: bold; }}
    </style>
</head>
<body>
    <div class='header'>
        <h2>🔔 New Request Submitted for Approval</h2>
    </div>
    
    <div class='content'>
        <p>A new request has been submitted and requires your approval:</p>
        
        <div class='details'>
            <p><strong>Request ID:</strong> {GetRequestLinkMarkup(requestId)}</p>
            <p><strong>Object:</strong> {objectName}</p>
            <p><strong>Requested by:</strong> {requestingUser}</p>
            <p><strong>Submitted on:</strong> {DateTime.Now:yyyy-MM-dd HH:mm:ss}</p>
        </div>
        
        <p>Please review and approve/reject this request in the Admin Dashboard.</p>
    </div>
    
    <div class='footer'>
        <p>Thank you,<br/>
        <strong>Admin Dashboard System</strong></p>
    </div>
</body>
</html>";

            return body;
        }

        public async Task CheckOutstandingRequestsAsync()
        {
            // Get all requests that are still pending approval
            var pendingRequests = await GetAllRequestViewsAsync();

            // Get number of days from configuration
            var daysThreshold = _config.GetValue<int>("PendingRequestReminderDays");

            pendingRequests = pendingRequests
                .Where(r => (DateTime.Now - r.RequestedTime).TotalDays >= daysThreshold)
                .ToList();

            // For pending requests older than configured days, send reminder emails to approvers
            foreach (var pendingRequest in pendingRequests)
            {
                // Get configured approvers
                var approvers = await _approvalDataAccessor
                    .GetApprovingUsersAsync(pendingRequest.ObjectName);

                // Build email list with requestor and configured approver
                var emails = approvers.Select(item => item.UserId).ToList();

                var emailBody = CreateOutstandingRequestEmailBodyprivate(pendingRequest, daysThreshold);

                var subject = $"Reminder: Approval for {pendingRequest.ObjectName} has been pending for {daysThreshold} Days";

                _emailService.SendEmail(emails, subject, emailBody);
            }
        }

        private string CreateOutstandingRequestEmailBodyprivate(RequestView request, int daysPending)
        {
            var body = $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; margin: 20px; }}
        .header {{ background-color: #f8f9fa; padding: 15px; border-left: 4px solid #007bff; }}
        .content {{ padding: 20px; }}
        .details {{ background-color: #f8f9fa; padding: 15px; margin: 15px 0; border-radius: 5px; }}
        .footer {{ margin-top: 30px; padding-top: 15px; border-top: 1px solid #dee2e6; color: #6c757d; }}
        .highlight {{ color: #007bff; font-weight: bold; }}
    </style>
</head>
<body>
    <div class='header'>
        <h2>Pending Request Reminder</h2>
    </div>
    
    <div class='content'>
        <p>A request has been pending your approval for {daysPending} days:</p>
        
        <div class='details'>
            <p><strong>Request ID:</strong> {GetRequestLinkMarkup(request.RequestId)}</p>
            <p><strong>Object:</strong> {request.ObjectName}</p>
            <p><strong>Requested by:</strong> {request.RequestingUser}</p>
            <p><strong>Submitted on:</strong> {request.RequestedTime:yyyy-MM-dd HH:mm:ss}</p>
        </div>
        
        <p>Please review and approve/reject this request in the Admin Dashboard.</p>
    </div>
    
    <div class='footer'>
        <p>Thank you,<br/>
        <strong>Admin Dashboard System</strong></p>
    </div>
</body>
</html>";

            return body;
        }

        private async Task EnrichWithApprovalLevelsAsync(RequestSummaryDto dto)
        {
            try
            {
                // Get the request to find its configuration
                var request = await _dODSDevOpsContext.Requests
                    .FirstOrDefaultAsync(r => r.RequestId == dto.RequestId);

                if (request == null)
                {
                    dto.CurrentApprovalLevel = 0;
                    dto.TotalApprovalLevels = 0;
                    return;
                }

                // Get the total number of approval levels for this configuration
                var totalLevels = await _dODSDevOpsContext.ApprovingUsers
                    .Where(au => au.ConfigurationId == request.ConfigurationId)
                    .Select(au => au.LevelId)
                    .Distinct()
                    .CountAsync();

                // Get the current approval level (highest level that has been approved)
                var currentLevel = await _dODSDevOpsContext.Approvals
                    .Where(a => a.RequestId == dto.RequestId)
                    .Select(a => a.LevelId)
                    .Distinct()
                    .CountAsync();

                dto.CurrentApprovalLevel = currentLevel;
                dto.TotalApprovalLevels = totalLevels;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error enriching approval levels for RequestId {RequestId}", dto.RequestId);
                dto.CurrentApprovalLevel = 0;
                dto.TotalApprovalLevels = 0;
            }
        }
    }
}
