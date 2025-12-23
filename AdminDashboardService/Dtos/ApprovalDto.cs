using System;

namespace AdminDashboardService.Dtos
{
    public class ApprovalDto
    {
        public string UserId { get; set; }
        public string Comments { get; set; }
        public DateTime ApprovedTime { get; set; }
        public string LevelName { get; set; }
    }
}
