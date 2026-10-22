using System;
using System.Collections.Generic;

namespace TourManagementWeb.Models.Entities;

public partial class Payment
{
    public int PaymentId { get; set; }

    public int BookingId { get; set; }

    public string PaymentType { get; set; } = null!;

    public string Method { get; set; } = null!;

    public decimal Amount { get; set; }

    public string? TransactionRef { get; set; }

    public string Status { get; set; } = null!;

    public DateTime? PaidAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual Booking Booking { get; set; } = null!;
}
