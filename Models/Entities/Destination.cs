using System;
using System.Collections.Generic;

namespace TourManagementWeb.Models.Entities;

public partial class Destination
{
    public int DestinationId { get; set; }

    public int ProvinceId { get; set; }

    public string DestinationName { get; set; } = null!;

    public string? Description { get; set; }

    public string? Address { get; set; }

    public string Status { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public virtual ICollection<DestinationImage> DestinationImages { get; set; } = new List<DestinationImage>();

    public virtual Province Province { get; set; } = null!;

    public virtual ICollection<TourDestination> TourDestinations { get; set; } = new List<TourDestination>();
}
