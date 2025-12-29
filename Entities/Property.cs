using System;
using LandPortal.Api.Enums;

namespace LandPortal.Api.Entities
{
    public class Property
    {

        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid OwnerId { get; set; }

        public string Title { get; set; } = default!;
        public string Description { get; set; } = default!;
        public decimal Price { get; set; }   // decimal(18,2)
        public string City { get; set; } = default!;
        public string Locality { get; set; } = default!;

        public decimal LandSize { get; set; } // decimal(18,2)
        public SizeUnit SizeUnit { get; set; } = SizeUnit.Sqft;

        public PropertyStatus Status { get; set; } = PropertyStatus.Draft;
        public DateTime? ListedAt { get; set; }
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public bool IsFeatured { get; set; } = false;
        public string? CoverImageUrl { get; set; }


        // nav
        public User Owner { get; set; } = default!;
        public ICollection<PropertyMedia> Media { get; set; } = new List<PropertyMedia>();

        public string? RoadAccess { get; set; }   // e.g. "20 ft tar road"
        public string? Facing { get; set; }       // e.g. "East", "North-East"
        public string? PlotType { get; set; }     // e.g. "DTCP Approved"
        public string? Brokerage { get; set; }    // e.g. "No brokerage" / "2%"
        public bool IsSold { get; set; }
        public DateTime? SoldAt { get; set; }
        public Guid? SoldById { get; set; }


    }
}
