using System;
using System.Collections.Generic;

namespace SB.AdminDashboard.EF.Models;

public partial class AuditPostApprovalAction
{
    public int? Id { get; set; }

    public string? Action { get; set; }

    public string? ExecutionType { get; set; }

    public string? Item { get; set; }

    public string? AuditOperation { get; set; }

    public DateTime? AuditTimestamp { get; set; }

    public string? AuditUser { get; set; }
}
