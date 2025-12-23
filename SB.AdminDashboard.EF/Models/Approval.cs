using System;
using System.Collections.Generic;

namespace SB.AdminDashboard.EF.Models;

public partial class Approval
{
    public int RequestId { get; set; }

    public string UserId { get; set; } = null!;

    public string Comments { get; set; } = null!;

    public DateTime ApprovedTime { get; set; }

    public int LevelId { get; set; }

    public virtual ApprovalLevel Level { get; set; } = null!;

    public virtual Request Request { get; set; } = null!;
}
