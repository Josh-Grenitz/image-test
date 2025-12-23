using System;
using System.Collections.Generic;

namespace SB.AdminDashboard.EF.Models;

public partial class AuditApprovalObjectConfiguration
{
    public int? Id { get; set; }

    public string? ObjectName { get; set; }

    public int? RequiredApprovals { get; set; }

    public int? PostApprovalActionId { get; set; }

    public DateTime? LastUpdatedTime { get; set; }

    public string? LastUpdatedBy { get; set; }

    public string? AuditOperation { get; set; }

    public DateTime? AuditTimestamp { get; set; }

    public string? AuditUser { get; set; }
}
