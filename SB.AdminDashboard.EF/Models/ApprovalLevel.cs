using System;
using System.Collections.Generic;

namespace SB.AdminDashboard.EF.Models;

public partial class ApprovalLevel
{
    public int Id { get; set; }

    public string LevelName { get; set; } = null!;
}
