using System;
using System.Collections.Generic;

namespace SB.AdminDashboard.EF.Models;

public partial class ConfigurationParameter
{
    public string Group { get; set; } = null!;

    public string Name { get; set; } = null!;

    public string? Value { get; set; }

    public string? Application { get; set; }
}
