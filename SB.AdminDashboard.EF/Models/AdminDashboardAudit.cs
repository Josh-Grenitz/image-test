using System;
using System.Collections.Generic;

namespace SB.AdminDashboard.EF.Models;

public partial class AdminDashboardAudit
{
    public int Id { get; set; }

    public string Application { get; set; } = null!;

    public string Configuration { get; set; } = null!;

    public string ApiEndpoint { get; set; } = null!;

    public string OldValue { get; set; } = null!;

    public string? NewValue { get; set; }

    public string User { get; set; } = null!;

    public DateTime LastUpdatedTime { get; set; }
}
