public class UpdatePropertyImagesDto
{
    public List<ImageItemDto> Images { get; set; } = [];
}

public class ImageItemDto
{
    public string Url { get; set; } = "";
    public bool IsCover { get; set; }
    public int SortOrder { get; set; }
}
