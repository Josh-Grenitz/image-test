using System;
using System.Collections.Generic;

namespace SB.AdminDashboard.EF.Models;

public partial class AuditApprovalLevel
{
    public int? Id { get; set; }

    public string? LevelName { get; set; }

    public string? AuditOperation { get; set; }

    public DateTime? AuditTimestamp { get; set; }

    public string? AuditUser { get; set; }
}
