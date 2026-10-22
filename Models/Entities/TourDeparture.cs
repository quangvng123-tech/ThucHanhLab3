using System;
using System.Collections.Generic;

namespace TourManagementWeb.Models.Entities;

public partial class TourDeparture
{
    public int DepartureId { get; set; }

    public int TourId { get; set; }

    public DateTime DepartureDate { get; set; }

    public DateTime ReturnDate { get; set; }

    public string? MeetingPoint { get; set; }

    public decimal PriceAdult { get; set; }

    public decimal PriceChild { get; set; }

    public byte MaxSeats { get; set; }

    public byte BookedSeats { get; set; }

    public string? GuideName { get; set; }

    public string Status { get; set; } = null!;

    public virtual ICollection<Booking> Bookings { get; set; } = new List<Booking>();

    public virtual Tour Tour { get; set; } = null!;
}
