using System;
using System.Collections.Generic;

namespace TourManagementWeb.Models.Entities;

public partial class Review
{
    public int ReviewId { get; set; }

    public int? BookingId { get; set; }

    public int CustomerId { get; set; }

    public int TourId { get; set; }

    public byte Rating { get; set; }

    public string? CommentText { get; set; }

    public string Status { get; set; } = null!;

    public int? ModeratedBy { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual Booking? Booking { get; set; }

    public virtual Customer Customer { get; set; } = null!;

    public virtual User? ModeratedByNavigation { get; set; }

    public virtual Tour Tour { get; set; } = null!;
}
