using System;
using System.Collections.Generic;

namespace TourManagementWeb.Models.Entities;

public partial class TourItinerary
{
    public int ItineraryId { get; set; }

    public int TourId { get; set; }

    public byte DayNo { get; set; }

    public string Title { get; set; } = null!;

    public string? Content { get; set; }

    public string? Meals { get; set; }

    public string? Accommodation { get; set; }

    public virtual Tour Tour { get; set; } = null!;
}
