namespace AdminDashboardService.Models
{
    public class ApprovalResult
    {
        public bool Success { get; set; }
        public string Message { get; set; }

        public static ApprovalResult SuccessResult(string message = "Request approved successfully")
        {
            return new ApprovalResult { Success = true, Message = message };
        }

        public static ApprovalResult FailureResult(string message)
        {
            return new ApprovalResult { Success = false, Message = message };
        }
    }
}
