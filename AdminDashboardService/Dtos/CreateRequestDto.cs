using System;

namespace AdminDashboardService.Dtos
{
    public class CreateRequestDto
    {
        public string ObjectName { get; set; }
        public string Operation { get; set; }
        public string MetaDataKey { get; set; }
        public string RequestingUserId { get; set; }
        public string RequestingComments { get; set; }
        public string OriginalData { get; set; }   // JSON from table row before
        public string UpdatedData { get; set; }    // JSON from UI after edits
        public string Type { get; set; }           // optional: Insert/Update/Delete etc.
    }
}
