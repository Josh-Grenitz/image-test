using System;
using System.Collections.Generic;

namespace SB.AdminDashboard.EF.Models;

public partial class ApprovingUser
{
    public int ConfigurationId { get; set; }

    public string UserId { get; set; } = null!;

    public int LevelId { get; set; }

    public virtual ApprovalObjectConfiguration Configuration { get; set; } = null!;

    public virtual ApprovalLevel Level { get; set; } = null!;
}
