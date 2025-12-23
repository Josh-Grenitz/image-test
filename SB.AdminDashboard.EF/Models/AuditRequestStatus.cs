using System;
using System.Collections.Generic;

namespace SB.AdminDashboard.EF.Models;

public partial class AuditRequestStatus
{
    public int? RequestId { get; set; }

    public int? StatusId { get; set; }

    public DateTime? LastUpdatedTime { get; set; }

    public string? LastUpdatedBy { get; set; }

    public string? AuditOperation { get; set; }

    public DateTime? AuditTimestamp { get; set; }

    public string? AuditUser { get; set; }
}
