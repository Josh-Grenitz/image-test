using System;
using System.Collections.Generic;

namespace SB.AdminDashboard.EF.Models;

public partial class UserRoleGroupMapping
{
    public int Id { get; set; }

    public string Role { get; set; } = null!;

    public string Group { get; set; } = null!;

    public string Application { get; set; } = null!;

    public string Status { get; set; } = null!;

    public string LastUpdatedBy { get; set; } = null!;

    public DateTime LastUpdatedTime { get; set; }
}
