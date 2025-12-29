namespace LandPortal.Api
{
    // code/GoogleStorageService.cs
    using Google.Cloud.Storage.V1;
    using System;
    using System.IO;
    using System.Threading.Tasks;
    using System.Net.Http; // for HttpMethod
    
    public class GoogleStorageService
    {
        private readonly StorageClient _client;
        private readonly string _bucketName;

        public GoogleStorageService(string bucketName)
        {
            _client = StorageClient.Create(); // uses ADC or GOOGLE_APPLICATION_CREDENTIALS
            _bucketName = bucketName;
        }

        public async Task<string> UploadFileAsync(Stream fileStream, string objectName, string contentType)
        {
            var obj = await _client.UploadObjectAsync(_bucketName, objectName, contentType, fileStream);
            // object URL (public) - if you use signed URLs or make objects public accordingly
            return $"gs://{_bucketName}/{objectName}";
        }

        public async Task DeleteObjectAsync(string objectName)
        {
            await _client.DeleteObjectAsync(_bucketName, objectName);
        }

        // Get a signed URL (optional) - for frontend private access
        public string GetSignedUrl(string objectName, TimeSpan validFor)
        {
            // read service account path from env var (make sure launchSettings.json uses escaped backslashes or forward slashes)
            var credentialsPath = Environment.GetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS");
            if (string.IsNullOrEmpty(credentialsPath))
                throw new InvalidOperationException("GOOGLE_APPLICATION_CREDENTIALS not set.");

            // NEW recommended factory method
            var signer = UrlSigner.FromCredentialFile(credentialsPath);

            // Use the overload that accepts a TimeSpan (validFor)
            return signer.Sign(_bucketName, objectName, validFor, HttpMethod.Get);
        }
    }

}
