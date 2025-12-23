using System;
using System.Collections.Generic;

namespace AdminDashboardService.Dtos
{
    public class RequestDetailDto
    {
        public int RequestId { get; set; }
        public string ObjectName { get; set; }
        public string Operation { get; set; }
        public string RequestingUserId { get; set; }
        public string RequestingComments { get; set; }
        public DateTime RequestedTime { get; set; }
        public string OriginalData { get; set; }   // JSON
        public string UpdatedData { get; set; }    // JSON
        public string Status { get; set; }
        public List<ApprovalDto> Approvals { get; set; } = new();
    }
}
