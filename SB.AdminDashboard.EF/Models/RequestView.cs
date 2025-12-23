using System;
using System.Collections.Generic;

namespace SB.AdminDashboard.EF.Models;

public partial class RequestView
{
    public int RequestId { get; set; }

    public string ObjectName { get; set; } = null!;

    public string Operation { get; set; } = null!;

    public string RequestingUser { get; set; } = null!;

    public string RequestingComments { get; set; } = null!;

    public DateTime RequestedTime { get; set; }

    public string UpdatedData { get; set; } = null!;

    public string? OriginalData { get; set; }

    public int? ApprovalStatus { get; set; }

    public string? PostApprovalAction { get; set; }

    public DateTime LastUpdatedTime { get; set; }

    public string LastUpdatedBy { get; set; } = null!;
}
