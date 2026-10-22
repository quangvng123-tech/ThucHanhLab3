using System;
using System.Collections.Generic;

namespace TourManagementWeb.Models.Entities;

public partial class Booking
{
    public int BookingId { get; set; }

    public string BookingCode { get; set; } = null!;

    public int CustomerId { get; set; }

    public int DepartureId { get; set; }

    public int? PromotionId { get; set; }

    public byte NumAdults { get; set; }

    public byte NumChildren { get; set; }

    public decimal TotalAmount { get; set; }

    public decimal DiscountAmount { get; set; }

    public decimal FinalAmount { get; set; }

    public string Status { get; set; } = null!;

    public string PaymentStatus { get; set; } = null!;

    public string? Note { get; set; }

    public DateTime? PaymentDeadline { get; set; }

    public int? ConfirmedBy { get; set; }

    public DateTime? ConfirmedAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual ICollection<BookingPassenger> BookingPassengers { get; set; } = new List<BookingPassenger>();

    public virtual User? ConfirmedByNavigation { get; set; }

    public virtual Customer Customer { get; set; } = null!;

    public virtual TourDeparture Departure { get; set; } = null!;

    public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();

    public virtual Promotion? Promotion { get; set; }

    public virtual ICollection<Review> Reviews { get; set; } = new List<Review>();
}
