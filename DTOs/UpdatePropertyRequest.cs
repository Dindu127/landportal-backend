using System.ComponentModel.DataAnnotations;
using LandPortal.Api.Enums;

namespace LandPortal.Api.DTOs
{
    public class UpdatePropertyRequest
    {
        [Required, StringLength(200)]
        public string Title { get; set; } = default!;

        [Required, StringLength(4000)]
        public string Description { get; set; } = default!;

        [Range(1, double.MaxValue)]
        public decimal Price { get; set; }

        [Required, StringLength(120)]
        public string City { get; set; } = default!;

        [Required, StringLength(200)]
        public string Locality { get; set; } = default!;

        [Range(0.01, double.MaxValue)]
        public decimal LandSize { get; set; }

        public SizeUnit SizeUnit { get; set; } = SizeUnit.Sqft;

        public bool? IsFeatured { get; set; }
        public string? CoverImageUrl { get; set; }

        public string? RoadAccess { get; set; }
        public string? Facing { get; set; }
        public string? PlotType { get; set; }
        public string? Brokerage { get; set; }
    }
}
