using System;

namespace AdminDashboardService.Dtos
{
    public class UpdateRequestDto
    {
        public string UpdatedData { get; set; }
        public string RequestingComments { get; set; }
        public string LastUpdatedBy { get; set; }
    }
}
