using System;
using System.Collections.Generic;

namespace TourManagementWeb.Models.Entities;

public partial class DestinationImage
{
    public int ImageId { get; set; }

    public int DestinationId { get; set; }

    public string ImageUrl { get; set; } = null!;

    public string? Caption { get; set; }

    public bool? IsPrimary { get; set; }

    public virtual Destination Destination { get; set; } = null!;
}
