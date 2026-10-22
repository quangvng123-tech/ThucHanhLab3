using System;
using System.Collections.Generic;

namespace TourManagementWeb.Models.Entities;

public partial class VTourStat
{
    public int TourId { get; set; }

    public string TourName { get; set; } = null!;

    public decimal? TotalBookings { get; set; }

    public decimal? ConfirmedBookings { get; set; }

    public decimal? PendingBookings { get; set; }

    public decimal? CancelledBookings { get; set; }

    public decimal? TotalPassengers { get; set; }

    public decimal? Revenue { get; set; }
}
