using System;
using System.Collections.Generic;

namespace TourManagementWeb.Models.Entities;

public partial class FavoriteTour
{
    public int CustomerId { get; set; }

    public int TourId { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual Customer Customer { get; set; } = null!;

    public virtual Tour Tour { get; set; } = null!;
}
