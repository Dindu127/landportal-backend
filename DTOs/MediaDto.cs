namespace LandPortal.Api.DTOs
{
    public class MediaDto
    {
        public Guid Id { get; set; }
        public Guid PropertyId { get; set; }
        public string Url { get; set; } = default!;
        public string ContentType { get; set; } = default!;
        public long SizeBytes { get; set; }
        public int? Width { get; set; }
        public int? Height { get; set; }
        public int SortOrder { get; set; }
        public DateTime CreatedAt { get; set; }
        public string? Path { get; set; }
        public string? PublicUrl { get; set; }
        public bool IsCover { get; set; }

        public string? RoadAccess { get; set; }   // e.g. "20 ft tar road"
        public string? Facing { get; set; }       // e.g. "East", "North-East"
        public string? PlotType { get; set; }     // e.g. "DTCP Approved"
        public string? Brokerage { get; set; }
    }
}
