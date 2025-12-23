using System;

namespace AdminDashboardService.Dtos
{
    public class RequestSummaryDto
    {
        public int RequestId { get; set; }
        public string ObjectName { get; set; }
        public string Operation { get; set; }
        public string RequestingUser { get; set; }
        public string RequestingComments { get; set; }
        public DateTime RequestedTime { get; set; }
        public string UpdatedData { get; set; }
        public string OriginalData { get; set; }
        public string ApprovalStatus { get; set; }
        public string PostApprovalAction { get; set; }
        public DateTime LastUpdatedTime { get; set; }
        public string LastUpdatedBy { get; set; }
        public int? CurrentApprovalLevel { get; set; }
        public int? TotalApprovalLevels { get; set; }
    }
}
