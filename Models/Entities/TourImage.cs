using System;
using System.Collections.Generic;

namespace TourManagementWeb.Models.Entities;

public partial class TourImage
{
    public int ImageId { get; set; }

    public int TourId { get; set; }

    public string ImageUrl { get; set; } = null!;

    public string? Caption { get; set; }

    public bool? IsPrimary { get; set; }

    public virtual Tour Tour { get; set; } = null!;
}
