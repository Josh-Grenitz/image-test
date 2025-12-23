using System;
using System.Collections.Generic;

namespace SB.AdminDashboard.EF.Models;

public partial class ApprovalObjectConfiguration
{
    public int Id { get; set; }

    public string ObjectName { get; set; } = null!;

    public int RequiredApprovals { get; set; }

    public int? PostApprovalActionId { get; set; }

    public string BusinessUnit { get; set; } 

    public DateTime LastUpdatedTime { get; set; }

    public string LastUpdatedBy { get; set; } = null!;

    public virtual PostApprovalAction PostApprovalAction { get; set; } = null!;

    public virtual ICollection<Request> Requests { get; set; } = new List<Request>();
}
