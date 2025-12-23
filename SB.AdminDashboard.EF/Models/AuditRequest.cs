using System;
using System.Collections.Generic;

namespace SB.AdminDashboard.EF.Models;

public partial class AuditRequest
{
    public int? RequestId { get; set; }

    public int? ConfigurationId { get; set; }

    public string? MetaDataKey { get; set; }

    public string? ObjectName { get; set; }

    public string? Operation { get; set; }

    public string? RequestingUserId { get; set; }

    public string? RequestingComments { get; set; }

    public DateTime? RequestedTime { get; set; }

    public string? Type { get; set; }

    public string? UpdatedData { get; set; }

    public string? OriginalData { get; set; }

    public DateTime? LastUpdatedTime { get; set; }

    public string? LastUpdatedBy { get; set; }

    public string? AuditOperation { get; set; }

    public DateTime? AuditTimestamp { get; set; }

    public string? AuditUser { get; set; }
}
