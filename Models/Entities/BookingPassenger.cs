using System;
using System.Collections.Generic;

namespace TourManagementWeb.Models.Entities;

public partial class BookingPassenger
{
    public int PassengerId { get; set; }

    public int BookingId { get; set; }

    public string FullName { get; set; } = null!;

    public string? Gender { get; set; }

    public DateTime? DateOfBirth { get; set; }

    public string PassengerType { get; set; } = null!;

    public string? IdNumber { get; set; }

    public virtual Booking Booking { get; set; } = null!;
}
