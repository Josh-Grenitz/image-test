using System;
using System.Collections.Generic;

namespace SB.AdminDashboard.EF.Models;

public partial class AdminDashboardMapping
{
    public int Id { get; set; }

    public string Application { get; set; } = null!;

    public string Configuration { get; set; } = null!;

    public string ApiEndpoint { get; set; } = null!;

    public string Type { get; set; } = null!;

    public string Map { get; set; } = null!;

    public string GetConditions { get; set; } = null!;

    public string BusinessUnit { get; set; } = null!;
}
