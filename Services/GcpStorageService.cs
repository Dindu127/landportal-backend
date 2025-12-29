using Google.Cloud.Storage.V1;

public class GcpStorageService
{
    private readonly StorageClient _client;
    private readonly string _bucket = null!;

    public GcpStorageService(IConfiguration config)
    {
        _client = StorageClient.Create();
        _bucket = config["Gcs:Bucket"]
            ?? throw new Exception("Gcs:Bucket not configured");

    }

    public async Task<string> UploadAsync(Stream file, string fileName, string contentType)
    {
        var obj = await _client.UploadObjectAsync(
            _bucket,
            $"properties/{Guid.NewGuid()}_{fileName}",
            contentType,
            file
        );

        return $"https://storage.googleapis.com/{_bucket}/{obj.Name}";
    }

    public async Task DeleteAsync(string url)
    {
        var name = new Uri(url).AbsolutePath.TrimStart('/');
        await _client.DeleteObjectAsync(_bucket, name);
    }

    public async Task<string> UploadProfilePhotoAsync(
    Stream file,
    string fileName,
    string contentType,
    Guid userId
)
    {
        var objectName = $"users/{userId}/profile_{Guid.NewGuid()}_{fileName}";

        var obj = await _client.UploadObjectAsync(
            _bucket,
            objectName,
            contentType,
            file
        );

        return $"https://storage.googleapis.com/{_bucket}/{objectName}";
    }


}
