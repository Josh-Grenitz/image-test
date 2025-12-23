using System;
using System.Collections.Generic;

namespace SB.AdminDashboard.EF.Models;

public partial class ApprovalStatus
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public string Description { get; set; } = null!;
}
