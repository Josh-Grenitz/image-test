namespace AdminDashboardService.Constants
{
    public static class ApprovalStatusConstants
    {
        public const string Pending = "Pending";
        public const string FirstReview = "First Review";
        public const string FinalReview = "Final Review";
        public const string Denied = "Denied";
        public const string Completed = "Completed";
        public const string Unknown = "Unknown";
    }

    public static class ApprovalCommentConstants
    {
        public const string Approved = "Approved";
        public const string Denied = "Denied";
        public const string SentBackForEdit = "Sent back for edit";
        public const string FinalReviewApproved = "Final Review Approved";
        public const string FirstReviewApproved = "First Review Approved";
        public const string RequestCompleted = "Request Completed";
    }

    public static class AuditLogFieldConstants
    {
        public const string Application = "Application";
        public const string Configuration = "Configuration";
        public const string ApiEndpoint = "ApiEndpoint";
        public const string OldValue = "OldValue";
        public const string NewValue = "NewValue";
        public const string User = "User";
    }

    public static class SystemUserConstants
    {
        public const string SystemUser = "SYSTEM";
    }

    public static class ConfigurationKeyConstants
    {
        public const string AuditTableName = "AuditTableName";
        public const string SqlDatabaseConnectionString = "SqlDatabaseConnectionString";
        public const string SqlDatabaseConnectionStringFO = "SqlDatabaseConnectionStringFO";
        public const string SqlDatabaseConnectionStringMO = "SqlDatabaseConnectionStringMO";
    }

    public static class OperationTypeConstants
    {
        public const string Insert = "INSERT";
        public const string Update = "UPDATE";
        public const string Delete = "DELETE";
    }
}
