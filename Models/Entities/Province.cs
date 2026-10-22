using System;
using System.Collections.Generic;

namespace TourManagementWeb.Models.Entities;

public partial class Province
{
    public int ProvinceId { get; set; }

    public string ProvinceName { get; set; } = null!;

    public string Region { get; set; } = null!;

    public string? Description { get; set; }

    public string? ImageUrl { get; set; }

    public virtual ICollection<Destination> Destinations { get; set; } = new List<Destination>();

    public virtual ICollection<Tour> Tours { get; set; } = new List<Tour>();
}
