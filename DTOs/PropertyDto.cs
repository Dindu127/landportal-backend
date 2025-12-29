namespace LandPortal.Api.DTOs
{
    public class PropertyDto
    {
        public Guid Id { get; set; }
        public Guid OwnerId { get; set; }
        public string Title { get; set; } = default!;
        public string Description { get; set; } = default!;
        public decimal Price { get; set; }
        public string City { get; set; } = default!;
        public string Locality { get; set; } = default!;
        public decimal LandSize { get; set; }
        public int SizeUnit { get; set; }
        public string? CoverImageUrl { get; set; }
        public bool IsFeatured { get; set; }
        public DateTime? ListedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public string Status { get; set; } = default!;
        public List<MediaDto> Media { get; set; } = new();

        public string? RoadAccess { get; set; }
        public string? Facing { get; set; }
        public string? PlotType { get; set; }
        public string? Brokerage { get; set; }

    }
}
