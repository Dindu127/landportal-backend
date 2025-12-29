using Google.Apis.Auth.OAuth2;
using Google.Cloud.Storage.V1;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Net.Http;

namespace LandPortal.Api.Controllers
{
    public record SignedUrlRequest(string Filename, string ContentType = "application/octet-stream");
    public record MakePublicRequest(string ObjectName);
    public record SignedGetRequest(string ObjectName, int ExpiresMinutes = 15);

    [ApiController]
    [Route("api/uploads")]
    public class UploadsController : ControllerBase
    {
        private readonly ILogger<UploadsController> _logger;
        private readonly StorageClient _storage;
        private readonly UrlSigner _signer;
        private readonly string _bucket;

        public UploadsController(IConfiguration cfg, ILogger<UploadsController> logger, StorageClient storage)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _storage = storage ?? throw new ArgumentNullException(nameof(storage));

            // Resolve bucket name
            _bucket = cfg["Gcs:Bucket"] ?? cfg["GoogleCloud:BucketName"] ?? "landportal-images";

            // Resolve service account path (config key OR env var)
            var saPath = cfg["Gcs:ServiceAccountJsonPath"]
                         ?? cfg["Gcs:CredentialPath"]
                         ?? cfg["GoogleCloud:CredentialsPath"]
                         ?? Environment.GetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS");

            if (string.IsNullOrWhiteSpace(saPath) || !System.IO.File.Exists(saPath))
            {
                // signer may still be created from ADC in some environments, but UrlSigner needs a service account key
                // We'll throw here so callers know signed URLs won't be available unless signer can be created.
                throw new InvalidOperationException("Service account JSON path not configured or file not found. Set Gcs:ServiceAccountJsonPath or GOOGLE_APPLICATION_CREDENTIALS.");
            }

            // Create UrlSigner from service account json (compatible with many library versions)
            // Replace the obsolete method call with the recommended method
            _signer = UrlSigner.FromCredentialFile(saPath);
           // _signer = UrlSigner.FromServiceAccountPath(saPath);
        }

        /// <summary>
        /// Returns a signed URL for PUT (upload) and a publicUrl (not guaranteed public until you make the object public).
        /// Body: { "filename":"abc.jpg", "contentType":"image/jpeg" }
        /// </summary>
        [HttpPost("signed-url")]
        public IActionResult GetSignedUrl([FromBody] SignedUrlRequest req)
        {
            if (req is null || string.IsNullOrWhiteSpace(req.Filename))
                return BadRequest("Filename required");

            var safeName = Path.GetFileName(req.Filename);
            var objectName = $"{DateTime.UtcNow:yyyyMMddHHmmss}_{Guid.NewGuid()}_{safeName}";

            try
            {
                var validFor = TimeSpan.FromMinutes(15);

                // Sign a PUT URL
                var signedUrl = _signer.Sign(
                    _bucket,
                    objectName,
                    validFor,
                    HttpMethod.Put
                );

                var publicUrl = $"https://storage.googleapis.com/{_bucket}/{objectName}";

                return Ok(new { uploadUrl = signedUrl, publicUrl, objectName });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create signed URL");
                return StatusCode(500, new { message = "Failed to create signed URL", detail = ex.Message });
            }
        }

        /// <summary>
        /// Make an existing object public. Call this AFTER the client has uploaded to the signed PUT URL.
        /// Body: { "objectName": "20251115_..._test.jpg" }
        /// </summary>
        [HttpPost("make-public")]
        public IActionResult MakePublic([FromBody] MakePublicRequest req)
        {
            if (req is null || string.IsNullOrWhiteSpace(req.ObjectName))
                return BadRequest("ObjectName required");

            try
            {
                // Option 1: Use UpdateObject with PredefinedAcl to set public-read
                var updated = _storage.UpdateObject(
                    new Google.Apis.Storage.v1.Data.Object
                    {
                        Bucket = _bucket,
                        Name = req.ObjectName
                    },
                    new UpdateObjectOptions { PredefinedAcl = PredefinedObjectAcl.PublicRead }
                );

                var publicUrl = $"https://storage.googleapis.com/{_bucket}/{req.ObjectName}";
                return Ok(new { publicUrl });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to make object public: {objectName}", req.ObjectName);
                return StatusCode(500, new { message = "Failed to make object public", detail = ex.Message });
            }
        }

        /// <summary>
        /// Returns a signed GET URL for a private object (safer than making objects public). Query string / body can be used.
        /// Example: GET /api/uploads/signed-get?objectName=2025...&expiresMinutes=15
        /// </summary>
        [HttpGet("signed-get")]
        public IActionResult GetSignedGet([FromQuery] SignedGetRequest req)
        {
            if (req is null || string.IsNullOrWhiteSpace(req.ObjectName))
                return BadRequest("objectName required");

            try
            {
                var validFor = TimeSpan.FromMinutes(Math.Max(1, req.ExpiresMinutes));
                var signedGet = _signer.Sign(
                    _bucket,
                    req.ObjectName,
                    validFor,
                    HttpMethod.Get
                );

                return Ok(new { url = signedGet, expiresInMinutes = validFor.TotalMinutes });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create signed GET URL for {object}", req.ObjectName);
                return StatusCode(500, new { message = "Failed to create signed GET URL", detail = ex.Message });
            }
        }
    }
}
