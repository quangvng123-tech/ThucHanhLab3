using System;
using System.Collections.Generic;

namespace TourManagementWeb.Models.Entities;

public partial class TourDestination
{
    public int TourId { get; set; }

    public int DestinationId { get; set; }

    public byte? VisitOrder { get; set; }

    public virtual Destination Destination { get; set; } = null!;

    public virtual Tour Tour { get; set; } = null!;
}
