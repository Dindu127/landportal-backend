namespace LandPortal.Api.DTOs
{
    public class CommitImageRequest
    {
        public string Url { get; set; } = default!;
        public string? ContentType { get; set; }
        public long? SizeBytes { get; set; }
        public int? Width { get; set; }
        public int? Height { get; set; }
        public bool IsCover { get; set; } = false;
        public int SortOrder { get; set; } = 0;
    }
}
