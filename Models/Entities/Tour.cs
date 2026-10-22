using System;
using System.Collections.Generic;

namespace TourManagementWeb.Models.Entities;

public partial class Tour
{
    public int TourId { get; set; }

    public string TourCode { get; set; } = null!;

    public int CategoryId { get; set; }

    public string TourName { get; set; } = null!;

    public string? Summary { get; set; }

    public string? Description { get; set; }

    public byte DurationDays { get; set; }

    public byte DurationNights { get; set; }

    public int DepartureProvinceId { get; set; }

    public string Transport { get; set; } = null!;

    public string Status { get; set; } = null!;

    public int CreatedBy { get; set; }

    public DateTime CreatedAt { get; set; }

    public decimal DefaultRating { get; set; }

    public virtual TourCategory Category { get; set; } = null!;

    public virtual ICollection<Contact> Contacts { get; set; } = new List<Contact>();

    public virtual User CreatedByNavigation { get; set; } = null!;

    public virtual Province DepartureProvince { get; set; } = null!;

    public virtual ICollection<FavoriteTour> FavoriteTours { get; set; } = new List<FavoriteTour>();

    public virtual ICollection<Review> Reviews { get; set; } = new List<Review>();

    public virtual ICollection<TourDeparture> TourDepartures { get; set; } = new List<TourDeparture>();

    public virtual ICollection<TourDestination> TourDestinations { get; set; } = new List<TourDestination>();

    public virtual ICollection<TourImage> TourImages { get; set; } = new List<TourImage>();

    public virtual ICollection<TourItinerary> TourItineraries { get; set; } = new List<TourItinerary>();
}
