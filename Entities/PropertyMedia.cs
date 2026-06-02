using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace LandPortal.Api.Entities
{
    public class PropertyMedia
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid PropertyId { get; set; }

        public string Url { get; set; } = default!;
        public string ContentType { get; set; } = default!;
        [Column("size_bytes")]
        public long SizeBytes { get; set; }
        public int? Width { get; set; }
        public int? Height { get; set; }
        public int SortOrder { get; set; } = 0;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public string? Path { get; set; }
        public string? PublicUrl { get; set; }
        public bool IsCover { get; set; }

        [JsonIgnore]
         public Property Property { get; set; } = default!;

    }
}
