// DTOs/PropertyResponse.cs
using System;
using LandPortal.Api.Enums;

namespace LandPortal.Api.DTOs
{
    public class PropertyResponse
    {
        public Guid Id { get; set; }
        public Guid OwnerId { get; set; }
        public string Title { get; set; } = default!;
        public string Description { get; set; } = default!;
        public decimal Price { get; set; }
        public string City { get; set; } = default!;
        public string Locality { get; set; } = default!;
        public decimal LandSize { get; set; }
        public SizeUnit SizeUnit { get; set; }
        public string? CoverImageUrl { get; set; }
        public bool IsFeatured { get; set; }
        public DateTime? ListedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public string Status { get; set; } = default!;
        //public List<MediaDto> Media { get; set; } = new List<MediaDto>();
        public List<PropertyMediaResponse> Media { get; set; } = new List<PropertyMediaResponse>();
        public string? RoadAccess { get; set; }   // e.g. "20 ft tar road"
        public string? Facing { get; set; }       // e.g. "East", "North-East"
        public string? PlotType { get; set; }     // e.g. "DTCP Approved"
        public string? Brokerage { get; set; }

        public bool IsSold { get; set; }
        public DateTime? SoldAt { get; set; }
        // optionally: public Guid? SoldById { get; set; }

    }
}
