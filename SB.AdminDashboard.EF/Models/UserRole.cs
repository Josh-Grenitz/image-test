using System;
using System.Collections.Generic;

namespace SB.AdminDashboard.EF.Models;

public partial class UserRole
{
    public int Id { get; set; }

    public string? Role { get; set; }

    public string? Group { get; set; }

    public string? Application { get; set; }
}
