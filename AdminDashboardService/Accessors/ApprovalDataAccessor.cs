using AdminDashboardService.Constants;
using AdminDashboardService.Interfaces;
using AdminDashboardService.Enums;
using AdminDashboardService.Models;
using Microsoft.EntityFrameworkCore;
using SB.AdminDashboard.EF.Data;
using SB.AdminDashboard.EF.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CommonApiUtilities.Interfaces;
using Newtonsoft.Json.Linq;
using System.Data;
using Microsoft.Extensions.Logging;

namespace AdminDashboardService.Accessors
{
    public class ApprovalDataAccessor : IApprovalDataAccessor
    {
        private readonly DODSDevOps _context;
        private readonly IEmailService _emailService;
        private readonly ISqlCrud _sqlCrud;
        private readonly IApplicationConfiguration _applicationConfiguration;
        private readonly ILogger<ApprovalDataAccessor> _logger;

        public ApprovalDataAccessor(DODSDevOps context,
                                   IEmailService emailService,
                                   ISqlCrud sqlCrud,
                                   IApplicationConfiguration applicationConfiguration,
                                   ILogger<ApprovalDataAccessor> logger)
        {
            _context = context;
            _emailService = emailService;
            _sqlCrud = sqlCrud;
            _applicationConfiguration = applicationConfiguration;
            _logger = logger;
        }
        public async Task AddApprovalAsync(Approval approval)
        {
            _context.Approvals.Add(approval);
            await _context.SaveChangesAsync();
        }

        public Task<int> GetApprovalCountAsync(int requestId)
        {
            return _context.Approvals.CountAsync(a => a.RequestId == requestId);
        }

        public async Task<ApprovalObjectConfiguration> GetConfigurationForRequestAsync(int requestId)
        {
            return await _context.Requests
                .Where(r => r.RequestId == requestId)
                .Select(r => r.Configuration)
                .FirstOrDefaultAsync();
        }

        public async Task<ApprovalObjectConfiguration> GetConfigurationByObjectNameAsync(string objectName)
        {
            return await _context.ApprovalObjectConfigurations
                .FirstOrDefaultAsync(c => c.ObjectName == objectName);
        }

        public async Task<List<ApprovingUser>> GetApprovingUsersAsync(int configurationId)
        {
            return await _context.ApprovingUsers
                .Include(au => au.Level)
                .Where(au => au.ConfigurationId == configurationId)
                .OrderBy(au => au.Level.Id) // Order by approval level
                .ToListAsync();
        }

        public async Task<List<ApprovingUser>> GetApprovingUsersAsync(string objectName)
        {
            var approvalObject = await GetConfigurationByObjectNameAsync(objectName);

            return await GetApprovingUsersAsync(approvalObject.Id);
        }

        public async Task UpdateRequestStatusAsync(int requestId, int statusId, string updatedBy)
        {
            // Since (RequestId, StatusId) is a composite key, we need to delete and insert
            // to change the status. History is maintained by AuditRequestStatus table.
            var existingStatus = await _context.RequestStatuses
                .FirstOrDefaultAsync(rs => rs.RequestId == requestId);

            if (existingStatus != null)
            {
                // Delete the existing status record
                _context.RequestStatuses.Remove(existingStatus);
                await _context.SaveChangesAsync();
            }

            // Insert new status record
            var newStatus = new RequestStatus
            {
                RequestId = requestId,
                StatusId = statusId,
                LastUpdatedTime = DateTime.Now,
                LastUpdatedBy = updatedBy
            };

            _context.RequestStatuses.Add(newStatus);
            await _context.SaveChangesAsync();
        }

        public async Task<bool> IsUserApproverForConfigurationAsync(string userId, int requestId)
        {
            var request = await _context.Requests
                .Where(r => r.RequestId == requestId)
                .Select(r => new { r.RequestingUserId, r.ConfigurationId })
                .FirstOrDefaultAsync();

            if (request == null)
                return false;

            // Prevent self-approval
            if (string.Equals(request.RequestingUserId, userId, StringComparison.OrdinalIgnoreCase))
                return false;

            return await _context.ApprovingUsers
                .AnyAsync(au => au.ConfigurationId == request.ConfigurationId &&
                               au.UserId.ToLower() == userId.ToLower());
        }

        public async Task<bool> DenyRequest(int requestId, string deniedBy, string comments = "")
        {
            _logger.LogInformation("User {UserId} attempting to deny request {RequestId}", deniedBy, requestId);
            
            var requestExists = await _context.Requests.AnyAsync(r => r.RequestId == requestId);
            if (!requestExists)
            {
                _logger.LogWarning("Deny failed: Request {RequestId} not found", requestId);
                return false;
            }

            await UpdateRequestStatusAsync(requestId, (int)ApprovalStatusEnum.Denied, deniedBy);
            // Add entry to Approval table for Denied with current LevelId
            var request = await _context.Requests.FirstOrDefaultAsync(r => r.RequestId == requestId);
            int levelId = 0;
            if (request != null)
            {
                var approver = await _context.ApprovingUsers.FirstOrDefaultAsync(au => au.ConfigurationId == request.ConfigurationId && au.UserId == deniedBy);
                if (approver != null)
                    levelId = approver.LevelId;
            }
            var deniedApproval = new Approval
            {
                RequestId = requestId,
                UserId = deniedBy,
                Comments = string.IsNullOrWhiteSpace(comments) ? ApprovalCommentConstants.Denied : comments,
                ApprovedTime = DateTime.Now,
                LevelId = levelId
            };
            await AddApprovalAsync(deniedApproval);
            _logger.LogInformation("Request {RequestId} denied by user {UserId}", requestId, deniedBy);
            return true;
        }

        public async Task<bool> SendBackRequest(int requestId, string sentBackBy, string comments = "")
        {
            _logger.LogInformation("User {UserId} attempting to send back request {RequestId}", sentBackBy, requestId);
            
            var request = await _context.Requests.FirstOrDefaultAsync(r => r.RequestId == requestId);
            if (request == null)
            {
                _logger.LogWarning("Send back failed: Request {RequestId} not found", requestId);
                return false;
            }


            // Delete all existing approvals for this request (reset approval process)
            var existingApprovals = await _context.Approvals
                .Where(a => a.RequestId == requestId)
                .ToListAsync();

            if (existingApprovals.Any())
            {
                _logger.LogInformation("Removing {Count} existing approvals for request {RequestId}", existingApprovals.Count, requestId);
                _context.Approvals.RemoveRange(existingApprovals);
                await _context.SaveChangesAsync();
            }

            // Add entry to Approval table for Send Back with current LevelId
            int levelId = 0;
            if (request != null)
            {
                var approver = await _context.ApprovingUsers.FirstOrDefaultAsync(au => au.ConfigurationId == request.ConfigurationId && au.UserId == sentBackBy);
                if (approver != null)
                    levelId = approver.LevelId;
            }
            var sendBackApproval = new Approval
            {
                RequestId = requestId,
                UserId = sentBackBy,
                Comments = string.IsNullOrWhiteSpace(comments) ? ApprovalCommentConstants.SentBackForEdit : comments,
                ApprovedTime = DateTime.Now,
                LevelId = levelId
            };
            await AddApprovalAsync(sendBackApproval);

            // Reset status to Pending
            await UpdateRequestStatusAsync(requestId, (int)ApprovalStatusEnum.Pending, sentBackBy);
            _logger.LogInformation("Request {RequestId} sent back by user {UserId}, status reset to Pending", requestId, sentBackBy);

            // Send notification email to the requester
            try
            {
                await SendSendBackNotificationEmailAsync(requestId, request.ObjectName, request.RequestingUserId, sentBackBy, comments);
            }
            catch (Exception emailEx)
            {
                _logger.LogError(emailEx, "Email notification failed for sent back request {RequestId}", requestId);
            }

            return true;
        }

        public async Task<ApprovalResult> ApproveRequest(int requestId, string approvedBy, string comments = "")
        {
            _logger.LogInformation("Starting approval process for request {RequestId} by user {UserId}", requestId, approvedBy);
            
            // Basic arg guard (optional)
            if (string.IsNullOrWhiteSpace(approvedBy))
            {
                _logger.LogWarning("Approval failed: Invalid approver for request {RequestId}", requestId);
                return ApprovalResult.FailureResult("Invalid approver.");
            }

            var request = await _context.Requests
                .FirstOrDefaultAsync(i => i.RequestId == requestId);

            if (request == null)
            {
                _logger.LogWarning("Approval failed: Request {RequestId} not found", requestId);
                return ApprovalResult.FailureResult("Request not found");
            }

            // Get current approver
            var approvingUser = await _context.ApprovingUsers
                .FirstOrDefaultAsync(au =>
                    au.ConfigurationId == request.ConfigurationId &&
                    au.UserId == approvedBy);

            if (approvingUser == null)
            {
                _logger.LogWarning("Approval failed: User {UserId} is not authorized to approve request {RequestId}", approvedBy, requestId);
                return ApprovalResult.FailureResult("You are not authorized to approve this request");
            }

            // Get all approvers for this configuration
            var approvingUsers = await _context.ApprovingUsers
                .Where(au => au.ConfigurationId == request.ConfigurationId)
                .OrderBy(au => au.LevelId)
                .ToListAsync();

            // Get existing approvals for this request
            var existingApprovals = await _context.Approvals
                .Where(a => a.RequestId == requestId)
                .ToListAsync();

            // Enforce sequential level approvals (e.g. 2 cannot approve before 1)
            if (approvingUsers.Count > 1 && approvingUser.LevelId > 1)
            {
                var previousLevel = approvingUser.LevelId - 1;
                var previousLevelApproved = existingApprovals.Any(a => a.LevelId == previousLevel);

                if (!previousLevelApproved)
                {
                    return ApprovalResult.FailureResult(
                        $"This request requires Level {previousLevel} approval before it can be approved at Level {approvingUser.LevelId}. Please wait for the previous level approval.");
                }
            }

            // Prevent duplicate approval by same user at same level
            var alreadyApproved = existingApprovals
                .Any(a => a.UserId == approvedBy && a.LevelId == approvingUser.LevelId);

            if (alreadyApproved)
            {
                _logger.LogWarning("Approval failed: User {UserId} has already approved request {RequestId} at level {LevelId}", approvedBy, requestId, approvingUser.LevelId);
                return ApprovalResult.FailureResult("You have already approved this request");
            }

            // Prepare approval entry; delay persistence so we only insert once per approver/level
            var approval = new Approval
            {
                RequestId = requestId,
                UserId = approvedBy,
                Comments = string.IsNullOrWhiteSpace(comments) ? ApprovalCommentConstants.Approved : comments,
                ApprovedTime = DateTime.Now,
                LevelId = approvingUser.LevelId
            };

            // Determine if this is the final approval
            bool isFinalApproval;
            if (approvingUsers.Count == 1)
            {
                isFinalApproval = true;
            }
            else
            {
                var highestLevel = approvingUsers.Max(au => au.LevelId);
                isFinalApproval = approvingUser.LevelId == highestLevel;
            }

            if (isFinalApproval)
            {
                // Final approval path
                _logger.LogInformation("Final approval granted for request {RequestId} by user {UserId}", requestId, approvedBy);
                await UpdateRequestStatusAsync(requestId, (int)ApprovalStatusEnum.FinalReview, approvedBy);

                try
                {
                    await SendApprovalNotificationEmailAsync(
                        requestId, request.ObjectName, request.RequestingUserId, approvedBy, comments);
                }
                catch (Exception emailEx)
                {
                    _logger.LogError(emailEx, "Email notification failed for approved request {RequestId}", requestId);
                }

                var operationSuccess = await ExecuteRequestOperationAsync(request);

                if (!operationSuccess)
                {
                    _logger.LogError("Request {RequestId} approved but operation execution failed", requestId);
                    if (string.IsNullOrWhiteSpace(comments))
                    {
                        approval.Comments = ApprovalCommentConstants.FinalReviewApproved;
                    }

                    await AddApprovalAsync(approval);
                    return ApprovalResult.FailureResult("Request approved but operation execution failed");
                }

                _logger.LogInformation("Request {RequestId} completed successfully", requestId);
                await UpdateRequestStatusAsync(requestId, (int)ApprovalStatusEnum.Completed, approvedBy);

                if (string.IsNullOrWhiteSpace(comments))
                {
                    approval.Comments = ApprovalCommentConstants.RequestCompleted;
                }

                await AddApprovalAsync(approval);

                try
                {
                    await SendCompletionNotificationEmailAsync(
                        requestId, request.ObjectName, request.RequestingUserId, request.Operation);
                }
                catch (Exception emailEx)
                {
                    _logger.LogError(emailEx, "Email notification failed for completed request {RequestId}", requestId);
                }

                return ApprovalResult.SuccessResult("Request approved and successfully completed");
            }

            // Intermediate approval path
            _logger.LogInformation("Intermediate approval (Level {LevelId}) granted for request {RequestId} by user {UserId}", approvingUser.LevelId, requestId, approvedBy);
            await UpdateRequestStatusAsync(requestId, (int)ApprovalStatusEnum.FirstReview, approvedBy);

            if (string.IsNullOrWhiteSpace(comments))
            {
                approval.Comments = ApprovalCommentConstants.FirstReviewApproved;
            }

            await AddApprovalAsync(approval);

            try
            {
                await SendApprovalNotificationEmailAsync(
                    requestId, request.ObjectName, request.RequestingUserId, approvedBy, comments);
            }
            catch (Exception emailEx)
            {
                _logger.LogError(emailEx, "Email notification failed for approved request {RequestId}", requestId);
            }

            return ApprovalResult.SuccessResult(
                $"Level {approvingUser.LevelId} approval completed. Waiting for next level approval.");
        }


        private async Task SendApprovalNotificationEmailAsync(int requestId, string objectName, string requestingUserId, string approvedBy, string comments)
        {
            try
            {
                // Create email content for approval notification
                var subject = $"Request Approved - {objectName} (Request #{requestId}) - {DateTime.Now:MM/dd/yyyy}";
                var body = CreateApprovalEmailBody(requestId, objectName, requestingUserId, approvedBy, comments);

                // Get valid email addresses
                var requestingUserEmail = requestingUserId;
                var approverEmail = approvedBy;

                var toEmails = new List<string>();
                var ccEmails = new List<string>();

                // Send to requesting user
                if (!string.IsNullOrEmpty(requestingUserEmail))
                {
                    toEmails.Add(requestingUserEmail);
                }

                // CC the approver
                if (!string.IsNullOrEmpty(approverEmail) && approverEmail != requestingUserEmail)
                {
                    ccEmails.Add(approverEmail);
                }

                if (toEmails.Count > 0)
                {
                    if (ccEmails.Count > 0)
                    {
                        _emailService.SendEmail(toEmails, ccEmails, subject, body);
                    }
                    else
                    {
                        _emailService.SendEmail(toEmails, subject, body);
                    }
                }
            }
            catch (Exception ex)
            {
                // Log error but don't fail the approval process
                _logger.LogError(ex, "Failed to send approval email for request {RequestId}", requestId);
            }
        }

        private async Task SendSendBackNotificationEmailAsync(int requestId, string objectName, string requestingUserId, string sentBackBy, string comments)
        {
            try
            {
                var subject = $"Request Sent Back - {objectName} (Request #{requestId}) - {DateTime.Now:MM/dd/yyyy}";
                var body = CreateSendBackEmailBody(requestId, objectName, requestingUserId, sentBackBy, comments);

                var toEmails = new List<string>();

                if (!string.IsNullOrEmpty(requestingUserId))
                {
                    toEmails.Add(requestingUserId);
                }

                if (toEmails.Count > 0)
                {
                    _emailService.SendEmail(toEmails, subject, body);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send send back email for request {RequestId}", requestId);
            }
        }



        private async Task SendCompletionNotificationEmailAsync(int requestId, string objectName, string requestingUserId, string operation)
        {
            try
            {
                // Get request with configuration including post-approval action
                var request = await _context.Requests
                    .Include(r => r.Configuration)
                    .ThenInclude(c => c.PostApprovalAction)
                    .FirstOrDefaultAsync(r => r.RequestId == requestId);

                if (request == null)
                {
                    _logger.LogWarning("Request {RequestId} not found for completion email", requestId);
                    return;
                }

                // Get post-approval action if configured
                string postApprovalActionText = null;
                if (request.Configuration?.PostApprovalActionId != null && request.Configuration.PostApprovalAction != null)
                {
                    postApprovalActionText = request.Configuration.PostApprovalAction.Action;
                }

                var subject = $"Request Completed - {objectName} (Request #{requestId}) - {DateTime.Now:MM/dd/yyyy}";
                var body = CreateCompletionEmailBody(requestId, objectName, requestingUserId, operation, postApprovalActionText);

                var toEmails = new List<string>();

                if (!string.IsNullOrEmpty(requestingUserId))
                {
                    toEmails.Add(requestingUserId);
                }

                if (toEmails.Count > 0)
                {
                    _emailService.SendEmail(toEmails, subject, body);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send completion email for request {RequestId}", requestId);
                throw;
            }
        }

        private string CreateCompletionEmailBody(int requestId, string objectName, string requestingUser, string operation, string postApprovalAction)
        {
            var postApprovalSection = string.IsNullOrEmpty(postApprovalAction)
                ? ""
                : $@"
        <div class='action-required'>
            <h3>⚠️ Post-Approval Action Required</h3>
            <p>{postApprovalAction}</p>
        </div>";

            var body = $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; margin: 20px; }}
        .header {{ background-color: #d1ecf1; padding: 15px; border-left: 4px solid #0c5460; }}
        .content {{ padding: 20px; }}
        .details {{ background-color: #f8f9fa; padding: 15px; margin: 15px 0; border-radius: 5px; }}
        .action-required {{ background-color: #fff3cd; padding: 15px; margin: 15px 0; border-radius: 5px; border-left: 4px solid #856404; }}
        .footer {{ margin-top: 30px; padding-top: 15px; border-top: 1px solid #dee2e6; color: #6c757d; }}
        .highlight {{ color: #0c5460; font-weight: bold; }}
        .info {{ color: #0c5460; }}
    </style>
</head>
<body>
    <div class='header'>
        <h2 class='info'>✓ Request Completed - Implementation Done</h2>
    </div>
    
    <div class='content'>
        <p>Your request has been <strong class='info'>completed and implemented</strong>:</p>
        
        <div class='details'>
            <p><strong>Request ID:</strong> <span class='highlight'>#{requestId}</span></p>
            <p><strong>Object:</strong> {objectName}</p>
            <p><strong>Operation:</strong> {operation}</p>
            <p><strong>Requested by:</strong> {requestingUser}</p>
            <p><strong>Completed on:</strong> {DateTime.Now:yyyy-MM-dd HH:mm:ss}</p>
        </div>
        
        {postApprovalSection}
        
        <p>The requested changes have been successfully applied to the system.</p>
    </div>
    
    <div class='footer'>
        <p>Thank you,<br/>
        <strong>Admin Dashboard System</strong></p>
    </div>
</body>
</html>";

            return body;
        }

        private string CreateSendBackEmailBody(int requestId, string objectName, string requestingUser, string sentBackBy, string comments)
        {
            var body = $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; margin: 20px; }}
        .header {{ background-color: #fff3cd; padding: 15px; border-left: 4px solid #856404; }}
        .content {{ padding: 20px; }}
        .details {{ background-color: #f8f9fa; padding: 15px; margin: 15px 0; border-radius: 5px; }}
        .comments {{ background-color: #e9ecef; padding: 10px; margin: 10px 0; border-radius: 3px; border-left: 3px solid #856404; }}
        .footer {{ margin-top: 30px; padding-top: 15px; border-top: 1px solid #dee2e6; color: #6c757d; }}
        .highlight {{ color: #856404; font-weight: bold; }}
        .warning {{ color: #856404; }}
    </style>
</head>
<body>
    <div class='header'>
        <h2 class='warning'>↩️ Request Sent Back for Modifications</h2>
    </div>
    
    <div class='content'>
        <p>Your request has been <strong class='warning'>sent back</strong> for modifications:</p>
        
        <div class='details'>
            <p><strong>Request ID:</strong> <span class='highlight'>#{requestId}</span></p>
            <p><strong>Object:</strong> {objectName}</p>
            <p><strong>Requested by:</strong> {requestingUser}</p>
            <p><strong>Sent back by:</strong> {sentBackBy}</p>
            <p><strong>Sent back on:</strong> {DateTime.Now:yyyy-MM-dd HH:mm:ss}</p>
        </div>
        
        <div class='comments'>
            <p><strong>Comments:</strong></p>
            <p>{(string.IsNullOrWhiteSpace(comments) ? "No comments provided" : comments)}</p>
        </div>
        
        <p>Please review the comments above, make the necessary modifications, and resubmit your request.</p>
    </div>
    
    <div class='footer'>
        <p>Thank you,<br/>
        <strong>Admin Dashboard System</strong></p>
    </div>
</body>
</html>";

            return body;
        }

        private string CreateApprovalEmailBody(int requestId, string objectName, string requestingUser, string approvedBy, string comments)
        {
            var body = $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; margin: 20px; }}
        .header {{ background-color: #d4edda; padding: 15px; border-left: 4px solid #28a745; }}
        .content {{ padding: 20px; }}
        .details {{ background-color: #f8f9fa; padding: 15px; margin: 15px 0; border-radius: 5px; }}
        .comments {{ background-color: #e9ecef; padding: 10px; margin: 10px 0; border-radius: 3px; border-left: 3px solid #28a745; }}
        .footer {{ margin-top: 30px; padding-top: 15px; border-top: 1px solid #dee2e6; color: #6c757d; }}
        .highlight {{ color: #28a745; font-weight: bold; }}
        .success {{ color: #28a745; }}
    </style>
</head>
<body>
    <div class='header'>
        <h2 class='success'>✅ Request Approved</h2>
    </div>
    
    <div class='content'>
        <p>Your request has been <strong class='success'>approved</strong>:</p>
        
        <div class='details'>
            <p><strong>Request ID:</strong> <span class='highlight'>#{requestId}</span></p>
            <p><strong>Object:</strong> {objectName}</p>
            <p><strong>Requested by:</strong> {requestingUser}</p>
            <p><strong>Approved by:</strong> {approvedBy}</p>
            <p><strong>Approved on:</strong> {DateTime.Now:yyyy-MM-dd HH:mm:ss}</p>
        </div>
        
        <div class='comments'>
            <p><strong>Approval Comments:</strong></p>
            <p>{comments}</p>
        </div>
        
        <p>Your request has been processed and approved. You can view the details in the Admin Dashboard.</p>
    </div>
    
    <div class='footer'>
        <p>Thank you,<br/>
        <strong>Admin Dashboard System</strong></p>
    </div>
</body>
</html>";

            return body;
        }

        private async Task<bool> ExecuteRequestOperationAsync(Request request)
        {
            _logger.LogInformation("Executing operation {Operation} for request {RequestId}, object {ObjectName}", request.Operation, request.RequestId, request.ObjectName);
            
            try
            {
                // Get the endpoint configuration for this object
                var appMap = _context.AdminDashboardConfigMappings.Where(acm => acm.Map == request.ObjectName).FirstOrDefault();

                var endPoint = appMap.ApiEndpoint;

                if (endPoint == null)
                {
                    _logger.LogError("No endpoint configuration found for object: {ObjectName}", request.ObjectName);
                    return false;
                }

                var businessunit = _context.ApprovalObjectConfigurations.Where(i => i.Id == request.ConfigurationId).Select(i => i.BusinessUnit).FirstOrDefault();
                string database = businessunit == "MO" ? ConfigurationKeyConstants.SqlDatabaseConnectionStringMO : ConfigurationKeyConstants.SqlDatabaseConnectionStringFO;
                string connectionString = _applicationConfiguration.GetApplicationFileConfiguration<string>(database);
                string fodatabase = ConfigurationKeyConstants.SqlDatabaseConnectionStringFO; // Default business unit
                string foconnectionString = _applicationConfiguration.GetApplicationFileConfiguration<string>(fodatabase);
                string tableName = appMap.Map;

                // Parse the operation type and execute accordingly
                var operation = request.Operation.ToUpper();

                switch (operation)
                {
                    case OperationTypeConstants.Insert:
                        return await ExecuteInsertOperationAsync(request, appMap, tableName, connectionString, foconnectionString);

                    case OperationTypeConstants.Update:
                        return await ExecuteUpdateOperationAsync(request, appMap, tableName, connectionString, foconnectionString);

                    case OperationTypeConstants.Delete:
                        return await ExecuteDeleteOperationAsync(request, appMap, tableName, connectionString, foconnectionString);

                    default:
                        _logger.LogError("Unknown operation type: {Operation}", operation);
                        return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing request operation for request {RequestId}", request.RequestId);
                return false;
            }
        }

        private async Task<bool> ExecuteInsertOperationAsync(Request request, AdminDashboardConfigMapping configMapping,
            string tableName, string connectionString, string foconnectionString)
        {
            _logger.LogInformation("Executing INSERT operation for request {RequestId} on table {TableName}", request.RequestId, tableName);
            
            try
            {
                var jsonData = JObject.Parse(request.UpdatedData);
                var dataTable = ConvertJsonToDataTable(jsonData);

                _sqlCrud.CreateDataInSqlTable(dataTable, tableName, connectionString, SystemUserConstants.SystemUser);
                LogAuditData(configMapping, "", request.UpdatedData, foconnectionString);

                _logger.LogInformation("INSERT operation completed successfully for request {RequestId}", request.RequestId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing INSERT operation for request {RequestId}", request.RequestId);
                return false;
            }
        }


        private async Task<bool> ExecuteUpdateOperationAsync(Request request, AdminDashboardConfigMapping configMapping,
            string tableName, string connectionString, string foconnectionString)
        {
            _logger.LogInformation("Executing UPDATE operation for request {RequestId} on table {TableName}", request.RequestId, tableName);
            
            try
            {
                var oldJsonObject = JObject.Parse(request.OriginalData);
                var newJsonObject = JObject.Parse(request.UpdatedData);

                var oldDataDictionary = ConvertJsonToDictionary(oldJsonObject);
                var newDataDictionary = ConvertJsonToDictionary(newJsonObject);

                if (oldDataDictionary.Count == 0 || newDataDictionary.Count == 0)
                {
                    _logger.LogError("Parsed dictionary is empty for UPDATE operation. Request {RequestId}", request.RequestId);
                    return false;
                }

                _sqlCrud.UpdateRowInSqlTable(tableName, oldDataDictionary, newDataDictionary, connectionString, SystemUserConstants.SystemUser);
                LogAuditData(configMapping, request.OriginalData, request.UpdatedData, foconnectionString);

                _logger.LogInformation("UPDATE operation completed successfully for request {RequestId}", request.RequestId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing UPDATE operation for request {RequestId}", request.RequestId);
                return false;
            }
        }



        private async Task<bool> ExecuteDeleteOperationAsync(Request request, AdminDashboardConfigMapping configMapping,
            string tableName, string connectionString, string foconnectionString)
        {
            _logger.LogInformation("Executing DELETE operation for request {RequestId} on table {TableName}", request.RequestId, tableName);
            
            try
            {
                Dictionary<string, string> deleteData;

                if (!string.IsNullOrWhiteSpace(request.OriginalData))
                {
                    var jsonObject = JObject.Parse(request.OriginalData);
                    deleteData = ConvertJsonToDictionary(jsonObject);

                    if (deleteData.Count == 0)
                    {
                        _logger.LogError("Delete JSON converted to empty dictionary for request {RequestId}", request.RequestId);
                        return false;
                    }
                }
                else if (!string.IsNullOrWhiteSpace(request.MetaDataKey))
                {
                    deleteData = new Dictionary<string, string> { { "Id", request.MetaDataKey } };
                }
                else
                {
                    _logger.LogError("No delete criteria found for request {RequestId}. Must supply OriginalData or MetaDataKey", request.RequestId);
                    return false;
                }

                _sqlCrud.DeleteRowFromSqlTable(tableName, deleteData, connectionString);
                LogAuditData(configMapping, request.OriginalData ?? request.MetaDataKey, "", foconnectionString);

                _logger.LogInformation("DELETE operation completed successfully for request {RequestId}", request.RequestId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing DELETE operation for request {RequestId}", request.RequestId);
                return false;
            }
        }


        // Helper method to convert JObject to Dictionary - handles null values
        private Dictionary<string, string> ConvertJsonToDictionary(JObject json)
        {
            var dict = new Dictionary<string, string>();

            foreach (var prop in json.Properties())
            {
                dict[prop.Name] = prop.Value.Type == JTokenType.Null
                    ? null
                    : prop.Value.ToString();
            }

            return dict;
        }

        // Helper method to convert JObject to DataTable
        private static DataTable ConvertJsonToDataTable(JObject json)
        {
            var table = new DataTable();

            foreach (var prop in json.Properties())
            {
                table.Columns.Add(prop.Name, typeof(string));
            }

            var row = table.NewRow();

            foreach (var prop in json.Properties())
            {
                var value = prop.Value.Type == JTokenType.Null
                    ? null
                    : prop.Value.ToString();

                row[prop.Name] = value;
            }

            table.Rows.Add(row);

            return table;
        }

        // Helper method to log audit data
        private void LogAuditData(AdminDashboardConfigMapping configMapping, string oldValue, string newValue, string connectionString)
        {
            var logData = new Dictionary<string, string>
            {
                { AuditLogFieldConstants.Application, configMapping.Application },
                { AuditLogFieldConstants.Configuration, configMapping.Configuration },
                { AuditLogFieldConstants.ApiEndpoint, configMapping.ApiEndpoint },
                { AuditLogFieldConstants.OldValue, oldValue },
                { AuditLogFieldConstants.NewValue, newValue },
                { AuditLogFieldConstants.User, SystemUserConstants.SystemUser }
            };

            _sqlCrud.LogForAudtit(
                _applicationConfiguration.GetApplicationFileConfiguration<string>(ConfigurationKeyConstants.AuditTableName),
                logData,
                connectionString
            );
        }
    }
}
