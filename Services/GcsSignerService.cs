using Google.Apis.Auth.OAuth2;
using Google.Cloud.Storage.V1;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Net.Http;
using System.Collections.Generic;
using System.Threading.Tasks;
using Google;

namespace LandPortal.Api.Services
{
    public class GcsSignerService
    {
        private readonly UrlSigner _signer;
        private readonly string _bucket;
        private readonly ILogger<GcsSignerService> _logger;
        private readonly StorageClient _storageClient;

        public GcsSignerService(StorageClient storageClient, IConfiguration config, ILogger<GcsSignerService> logger)
        {
            _bucket = config["Gcs:Bucket"] ?? throw new InvalidOperationException("Gcs:Bucket not set");
            var saPath = config["Gcs:ServiceAccountJsonPath"] ?? throw new InvalidOperationException("ServiceAccountJsonPath not set");
            _signer = UrlSigner.FromCredentialFile(saPath);
            _logger = logger;
            _storageClient = storageClient;
        }

        // Signed PUT URL valid for `duration`. contentType e.g. "image/jpeg"
        public string CreateSignedUploadUrl(string objectName, TimeSpan duration, string contentType)
        {
            // NOTE: UrlSigner.Sign(bucket, objectName, duration, HttpMethod.Put) returns a signed PUT URL.
            // The contentType parameter is kept to communicate intent to the caller (and to later use in commit metadata).
            return _signer.Sign(_bucket, objectName, duration, HttpMethod.Put);
        }

        // Public URL for saved object (used in commit)
        public string PublicUrl(string objectName)
            => $"https://storage.googleapis.com/{_bucket}/{objectName}";

        public async Task DeleteObjectIfExistsAsync(string urlOrPath)
        {
            if (string.IsNullOrWhiteSpace(urlOrPath))
            {
                _logger.LogDebug("DeleteObjectIfExistsAsync called with empty urlOrPath.");
                return;
            }

            var objectName = GetObjectNameFromUrlOrPath(urlOrPath);

            try
            {
                await _storageClient.DeleteObjectAsync(_bucket, objectName);
                _logger.LogInformation("Deleted object from GCS bucket '{Bucket}' object='{ObjectName}'", _bucket, objectName);
            }
            catch (Google.GoogleApiException gex) when (gex.HttpStatusCode == System.Net.HttpStatusCode.NotFound)
            {
                _logger.LogInformation("GCS object not found: bucket='{Bucket}' object='{ObjectName}'", _bucket, objectName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed deleting object from GCS bucket '{Bucket}' object='{ObjectName}'", _bucket, objectName);
                throw;
            }
        }

        private string GetObjectNameFromUrlOrPath(string urlOrPath)
        {
            if (!urlOrPath.StartsWith("http", StringComparison.OrdinalIgnoreCase) &&
                !urlOrPath.StartsWith("gs://", StringComparison.OrdinalIgnoreCase))
            {
                return urlOrPath.TrimStart('/');
            }

            try
            {
                var uri = new Uri(urlOrPath);
                if (uri.Scheme == "gs")
                {
                    return uri.AbsolutePath.TrimStart('/');
                }

                var segments = uri.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
                if (segments.Length >= 2)
                {
                    var objectName = string.Join('/', segments, 1, segments.Length - 1);
                    return objectName;
                }

                return uri.AbsolutePath.TrimStart('/');
            }
            catch
            {
                return urlOrPath.TrimStart('/');
            }
        }
    }
}