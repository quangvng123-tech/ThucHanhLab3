using System;
using System.Collections.Generic;

namespace TourManagementWeb.Models.Entities;

public partial class TourCategory
{
    public int CategoryId { get; set; }

    public string CategoryName { get; set; } = null!;

    public string? Description { get; set; }

    public virtual ICollection<Tour> Tours { get; set; } = new List<Tour>();
}
