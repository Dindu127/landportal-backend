namespace LandPortal.Api.DTOs
{
    public class AdminPropertyDto
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = default!;
        public string City { get; set; } = default!;
        public string Locality { get; set; } = default!;
        public decimal Price { get; set; }
        public Guid OwnerId { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime UpdatedAt { get; set; }
        public DateTime? ListedAt { get; set; }
        public List<MediaDto> Media { get; set; } = new();
        public string? RoadAccess { get; set; }   // e.g. "20 ft tar road"
        public string? Facing { get; set; }       // e.g. "East", "North-East"
        public string? PlotType { get; set; }     // e.g. "DTCP Approved"
        public string? Brokerage { get; set; }
        public string? OwnerName { get; set; }
        public bool IsFeatured { get; set; }
        public bool IsSold { get; set; }
    }
}
