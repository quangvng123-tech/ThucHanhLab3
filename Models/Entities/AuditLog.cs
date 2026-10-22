using System;
using System.Collections.Generic;

namespace TourManagementWeb.Models.Entities;

public partial class AuditLog
{
    public long LogId { get; set; }

    public int? UserId { get; set; }

    public string Action { get; set; } = null!;

    public string? ObjectType { get; set; }

    public long? ObjectId { get; set; }

    public string? Detail { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual User? User { get; set; }
}
