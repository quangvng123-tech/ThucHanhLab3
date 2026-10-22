using System;
using System.Collections.Generic;

namespace TourManagementWeb.Models.Entities;

public partial class VDeparturePassenger
{
    public int DepartureId { get; set; }

    public string TourCode { get; set; } = null!;

    public string TourName { get; set; } = null!;

    public DateTime DepartureDate { get; set; }

    public DateTime ReturnDate { get; set; }

    public string? GuideName { get; set; }

    public int BookingId { get; set; }

    public string BookingCode { get; set; } = null!;

    public int PassengerId { get; set; }

    public string PassengerName { get; set; } = null!;

    public string? Gender { get; set; }

    public DateTime? DateOfBirth { get; set; }

    public string PassengerType { get; set; } = null!;

    public string ContactName { get; set; } = null!;

    public string ContactPhone { get; set; } = null!;
}
