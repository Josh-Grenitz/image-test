using System;
using System.Collections.Generic;

namespace SB.AdminDashboard.EF.Models;

public partial class PostApprovalAction
{
    public int Id { get; set; }

    public string Action { get; set; } = null!;

    public string ExecutionType { get; set; } = null!;

    public string Item { get; set; } = null!;

    public virtual ICollection<ApprovalObjectConfiguration> ApprovalObjectConfigurations { get; set; } = new List<ApprovalObjectConfiguration>();
}
