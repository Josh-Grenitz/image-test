using System;
using System.Collections.Generic;

namespace SB.AdminDashboard.EF.Models;

public partial class RequestStatus
{
    public int RequestId { get; set; }

    public int StatusId { get; set; }

    public DateTime LastUpdatedTime { get; set; }

    public string LastUpdatedBy { get; set; } = null!;

    public virtual Request Request { get; set; } = null!;

    public virtual ApprovalStatus Status { get; set; } = null!;
}
