using System;
using System.Collections.Generic;

namespace TourManagementWeb.Models.Entities;

public partial class Contact
{
    public int ContactId { get; set; }

    public string FullName { get; set; } = null!;

    public string Email { get; set; } = null!;

    public string? Phone { get; set; }

    public int? TourId { get; set; }

    public string Subject { get; set; } = null!;

    public string Message { get; set; } = null!;

    public string Status { get; set; } = null!;

    public int? HandledBy { get; set; }

    public string? Reply { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? RepliedAt { get; set; }

    public virtual User? HandledByNavigation { get; set; }

    public virtual Tour? Tour { get; set; }
}
