using SB.AdminDashboard.EF.Models;
using AdminDashboardService.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AdminDashboardService.Interfaces
{
    public interface IApprovalDataAccessor
    {
        Task AddApprovalAsync(Approval approval);
        Task<int> GetApprovalCountAsync(int requestId);
        Task<ApprovalObjectConfiguration> GetConfigurationForRequestAsync(int requestId);
        Task<ApprovalObjectConfiguration> GetConfigurationByObjectNameAsync(string objectName);
        Task<List<ApprovingUser>> GetApprovingUsersAsync(int configurationId);
        Task<List<ApprovingUser>> GetApprovingUsersAsync(string objectName);
        Task UpdateRequestStatusAsync(int requestId, int statusId, string updatedBy);
        Task<bool> IsUserApproverForConfigurationAsync(string userId, int requestID);
        Task<bool> DenyRequest(int requestId, string deniedBy, string comments = "");
        Task<ApprovalResult> ApproveRequest(int requestId, string approvedBy, string comments = "");
        Task<bool> SendBackRequest(int requestId, string sentBackBy, string comments = "");
    }
}
