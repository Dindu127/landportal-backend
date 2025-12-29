// DTOs/SignImageResponse.cs
using System;

namespace LandPortal.Api.DTOs
{
    public class SignImageResponse
    {
        public Guid Id { get; set; }           // media id
        public string Url { get; set; } = default!;
        public string PublicUrl { get; set; } = default!;
        public string ContentType { get; set; } = default!;
        public long SizeBytes { get; set; }
        public int? Width { get; set; }
        public int? Height { get; set; }
        public bool IsCover { get; set; }
        public DateTime UploadedAtUtc { get; set; }
        public string? Path { get; set; }
    }
}
