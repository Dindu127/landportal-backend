using System.ComponentModel.DataAnnotations.Schema;

namespace LandPortal.Api.DTOs
{
    public class PropertyMediaResponse
    {
        public Guid Id { get; set; }
        public Guid PropertyId { get; set; }
        public string Url { get; set; } = default!;
        public string? ContentType { get; set; }
        [Column("size_bytes")]
        public long SizeBytes { get; set; }
        public int? Width { get; set; }
        public int? Height { get; set; }
        public int SortOrder { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsCover { get; set; }
        public string? Path { get; set; }
        public string? PublicUrl { get; set; }
        public DateTime? ListedAt { get; set; }
    }

}
