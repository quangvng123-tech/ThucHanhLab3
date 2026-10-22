using System;
using System.Collections.Generic;

namespace TourManagementWeb.Models.Entities;

public partial class VTourList
{
    public int TourId { get; set; }

    public string TourCode { get; set; } = null!;

    public string TourName { get; set; } = null!;

    public string CategoryName { get; set; } = null!;

    public string DepartureFrom { get; set; } = null!;

    public byte DurationDays { get; set; }

    public byte DurationNights { get; set; }

    public string Transport { get; set; } = null!;

    public decimal? MinPrice { get; set; }

    public DateTime? NextDeparture { get; set; }

    public decimal? AvgRating { get; set; }

    public decimal? ReviewCount { get; set; }
}
