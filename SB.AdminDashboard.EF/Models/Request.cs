using System;
using System.Collections.Generic;

namespace SB.AdminDashboard.EF.Models;

public partial class Request
{
    public int RequestId { get; set; }

    public int ConfigurationId { get; set; }

    public string MetaDataKey { get; set; } = null!;

    public string ObjectName { get; set; } = null!;

    public string Operation { get; set; } = null!;

    public string RequestingUserId { get; set; } = null!;

    public string RequestingComments { get; set; } = null!;

    public DateTime RequestedTime { get; set; }

    public string Type { get; set; } = null!;

    public string UpdatedData { get; set; } = null!;

    public string? OriginalData { get; set; }

    public DateTime LastUpdatedTime { get; set; }

    public string LastUpdatedBy { get; set; } = null!;

    public virtual ApprovalObjectConfiguration Configuration { get; set; } = null!;
}
