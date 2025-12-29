using LandPortal.Api.Enums;

namespace LandPortal.Api.DTOs
{
    public class UpdatePropertyDto
    {
        public string Title { get; set; } = default!;
        public string Description { get; set; } = default!;
        public decimal Price { get; set; }

        public string City { get; set; } = default!;
        public string Locality { get; set; } = default!;

        public decimal LandSize { get; set; }
        public SizeUnit SizeUnit { get; set; }

        public PropertyStatus Status { get; set; }
        public string? CoverImageUrl { get; set; }

        public string? Brokerage { get; set; }
        public string? Facing { get; set; }
        public string? PlotType { get; set; }
        public string? RoadAccess { get; set; }
    }
}
