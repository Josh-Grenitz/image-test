using System;
using System.Collections.Generic;

namespace SB.AdminDashboard.EF.Models;

public partial class SecurityPolicy
{
    public int PolicyId { get; set; }

    public string App { get; set; } = null!;

    public string Policy { get; set; } = null!;

    public string Role { get; set; } = null!;

    public bool Status { get; set; }

    public DateTime Created { get; set; }

    public string CreatedBy { get; set; } = null!;

    public string? LastUpdatedBy { get; set; }

    public DateTime? LastUpdatedTime { get; set; }
}
