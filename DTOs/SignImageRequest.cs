public class SignImageRequest
{
    public string FileName { get; set; } = default!;
    public string ContentType { get; set; } = "image/jpeg";
}

public class CommitImageRequest
{
    public string Url { get; set; } = default!;
    public string ContentType { get; set; } = default!;
    public long SizeBytes { get; set; }
    public int? Width { get; set; }
    public int? Height { get; set; }
    public bool IsCover { get; set; }
    public int SortOrder { get; internal set; }
}
