using System;
using System.Collections.Generic;

namespace SB.AdminDashboard.EF.Models;

public partial class AuditApprovingUser
{
    public int? ConfigurationId { get; set; }

    public string? UserId { get; set; }

    public int? LevelId { get; set; }

    public string? AuditOperation { get; set; }

    public DateTime? AuditTimestamp { get; set; }

    public string? AuditUser { get; set; }
}
