using Google;
using Google.Apis.Auth.OAuth2;
using Google.Cloud.Storage.V1;
using LandPortal.Api.Data;
using LandPortal.Api.DTOs;
using LandPortal.Api.Entities;
using LandPortal.Api.Helpers;
using LandPortal.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace LandPortal.Api.Controllers
{


    [ApiController]
    // Base route — every action here works with the propertyId route value
    [Route("api/properties/{propertyId:guid}/images")]
    public class PropertyImagesController : ControllerBase
    {
        private readonly GcsSignerService _gcs;
        private readonly LandPortalDbContext _db;
        private readonly ILogger<PropertyImagesController> _logger;
        private readonly IConfiguration _config;

        public PropertyImagesController(
            GcsSignerService gcs,
            LandPortalDbContext db,
            ILogger<PropertyImagesController> logger,
            IConfiguration config)
        {
            _gcs = gcs;
            _db = db;
            _logger = logger;
            _config = config;
        }

        // 1) Request signed URL
        // POST /api/properties/{propertyId}/images/sign
        [HttpPost("sign")]
        [Authorize]
        public ActionResult SignUpload([FromRoute] Guid propertyId, [FromBody] SignImageRequest req)
        {
            if (req == null) return BadRequest(new { detail = "Request body required" });

            var fileName = string.IsNullOrWhiteSpace(req.FileName) ? $"{Guid.NewGuid()}.jpg" : req.FileName;
            var objectName = $"images/properties/{propertyId}/{Guid.NewGuid()}-{fileName}";

            // Create a short-lived signed upload URL
            var signedUrl = _gcs.CreateSignedUploadUrl(objectName, TimeSpan.FromMinutes(15), req.ContentType ?? "image/jpeg");
            var publicUrl = _gcs.PublicUrl(objectName);

            return Ok(new { uploadUrl = signedUrl, publicUrl });
        }

        // 2) Commit metadata after client uploads directly to GCS
        // POST /api/properties/{propertyId}/images/commit
        [HttpPost("commit")]
        [Authorize]
        public async Task<IActionResult> Commit([FromRoute] Guid propertyId, [FromBody] CommitImageRequest req)
        {
            // Basic validation
            if (req == null)
                return BadRequest(new { detail = "Request body required" });

            if (string.IsNullOrWhiteSpace(req.Url))
                return BadRequest(new { detail = "Url is required" });

            _logger.LogInformation(
                "Commit called: propertyId={PropertyId} url={Url} isCover={IsCover}",
                propertyId, req.Url, req.IsCover);

            // Load property with existing media
            var property = await _db.Properties
                .Include(p => p.Media)
                .FirstOrDefaultAsync(p => p.Id == propertyId);

            if (property == null)
            {
                _logger.LogWarning("Property not found for commit: {PropertyId}", propertyId);
                return NotFound(new { detail = "Property not found" });
            }

            _logger.LogInformation(
                "Property before commit: Id={Id} CoverImageUrl={Cover}",
                property.Id, property.CoverImageUrl);

            // Derive a GCS object path from the public URL (if possible)
            string? objectPath = null;
            if (TryParseGcsUrl(req.Url, out var bucket, out var objName))
            {
                objectPath = objName;
            }

            // Determine sort order: if client sends 0, append after existing media
            var sortOrder = req.SortOrder;
            if (sortOrder <= 0)
            {
                var existingCount = property.Media?.Count ?? 0;
                sortOrder = existingCount + 1;
            }

            var media = new PropertyMedia
            {
                Id = Guid.NewGuid(),
                PropertyId = propertyId,
                Url = req.Url,
                PublicUrl = req.Url,                                  // ensure PublicUrl not null
                Path = objectPath,                                    // store object name if parsed
                ContentType = req.ContentType ?? "application/octet-stream",
                SizeBytes = req.SizeBytes,                      // null-safe
                Width = req.Width,
                Height = req.Height,
                CreatedAt = DateTime.UtcNow,
                SortOrder = sortOrder,
                IsCover = req.IsCover
            };

            _db.PropertyMedia.Add(media);

            // If this media is requested as cover, unset other covers first (so only one cover exists)
            if (req.IsCover)
            {
                var otherCovers = _db.PropertyMedia
                    .Where(m => m.PropertyId == propertyId && m.IsCover && m.Id != media.Id);

                await otherCovers.ForEachAsync(m => m.IsCover = false);
                property.CoverImageUrl = req.Url;

                _logger.LogInformation(
                    "Will update property.CoverImageUrl -> {Url} and unset previous covers",
                    req.Url);
            }
            else
            {
                // If property has no cover at all, treat the uploaded media as cover (optional behavior)
                if (string.IsNullOrEmpty(property.CoverImageUrl))
                {
                    property.CoverImageUrl = req.Url;
                    media.IsCover = true;

                    _logger.LogInformation(
                        "Property had no cover; auto-setting this media as cover: {Url}",
                        req.Url);
                }
            }

            try
            {
                await _db.SaveChangesAsync();

                _logger.LogInformation(
                    "Saved media {MediaId}. Property cover is now {Cover}",
                    media.Id, property.CoverImageUrl);

                // Return a DTO (avoid returning EF entities which can cause cycles)
                var mediaDto = new DTOs.PropertyMediaResponse
                {
                    Id = media.Id,
                    PropertyId = media.PropertyId,
                    Url = media.Url,
                    ContentType = media.ContentType,
                    SizeBytes = media.SizeBytes,
                    Width = media.Width,
                    Height = media.Height,
                    SortOrder = media.SortOrder,
                    CreatedAt = media.CreatedAt,
                    IsCover = media.IsCover,
                    Path = media.Path,
                    PublicUrl = media.PublicUrl
                };

                return CreatedAtAction(nameof(Commit),
                    new { propertyId = propertyId, id = media.Id }, mediaDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Commit failed for property {PropertyId}. MediaUrl={Url}",
                    propertyId, req.Url);

                var isDev = string.Equals(
                    Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"),
                    "Development",
                    StringComparison.OrdinalIgnoreCase);

                if (isDev)
                {
                    // In dev, return full exception to see exact SQL error text
                    return Problem(detail: ex.ToString(), statusCode: 500);
                }

                return Problem(detail: "Failed saving property media", statusCode: 500);
            }
        }



        // DELETE /api/properties/{propertyId}/images?url={url}
        [HttpDelete("")]
        public async Task<IActionResult> Delete([FromRoute] Guid propertyId, [FromQuery] string url)
        {
            if (string.IsNullOrWhiteSpace(url))
                return BadRequest(new { detail = "url required" });

            // Find exact match by propertyId + Url (normalize both sides if necessary)
            var media = await _db.PropertyMedia
                .FirstOrDefaultAsync(m => m.PropertyId == propertyId && m.Url == url);

            if (media == null)
            {
                _logger.LogInformation("Delete called but media not found. propertyId={PropertyId} url={Url}", propertyId, url);
                return NotFound(new { detail = "Media not found" });
            }

            // remove DB row
            _db.PropertyMedia.Remove(media);

            try
            {
                var rows = await _db.SaveChangesAsync();
                _logger.LogInformation("Deleted media {MediaId} rowsAffected={Rows}", media.Id, rows);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed saving DB changes when deleting media {MediaId}", media.Id);
                return Problem(detail: "Failed removing media record", statusCode: 500);
            }

            // now attempt to remove blob from GCS; we don't fail the request if this fails.
            try
            {
                // If your service expects a path/object name rather than full URL, translate here.
                // Example: await _gcs.DeleteObjectIfExistsAsync(media.Path ?? media.Url);
                await _gcs.DeleteObjectIfExistsAsync(media.Path ?? media.Url);
                _logger.LogInformation("Deleted blob for media {MediaId}", media.Id);
            }
            catch (Exception ex)
            {
                // Already removed DB row. Log and continue.
                _logger.LogError(ex, "Failed deleting blob for media {MediaId}. DB record removed.", media.Id);
            }

            return NoContent();
        }

        // helper to parse public URL -> bucket & object path
        private bool TryParseGcsUrl(string url, out string bucket, out string objectName)
        {
            bucket = null!;
            objectName = null!;

            if (!Uri.TryCreate(url, UriKind.Absolute, out var u)) return false;

            // pattern 1: https://storage.googleapis.com/{bucket}/{object...}
            if (u.Host.Equals("storage.googleapis.com", StringComparison.OrdinalIgnoreCase))
            {
                var path = u.AbsolutePath.TrimStart('/');
                var idx = path.IndexOf('/');
                if (idx <= 0) return false;
                bucket = path.Substring(0, idx);
                objectName = Uri.UnescapeDataString(path.Substring(idx + 1));
                return true;
            }

            // pattern 2: https://{bucket}.storage.googleapis.com/{object...}
            if (u.Host.EndsWith(".storage.googleapis.com", StringComparison.OrdinalIgnoreCase))
            {
                var hostIndex = u.Host.IndexOf(".storage.googleapis.com", StringComparison.OrdinalIgnoreCase);
                bucket = u.Host.Substring(0, hostIndex);
                objectName = Uri.UnescapeDataString(u.AbsolutePath.TrimStart('/'));
                return true;
            }

            // pattern 3: ACL-style URLs like https://www.googleapis.com/storage/v1/b/{bucket}/o/{object}
            var segs = u.Segments.Select(s => s.Trim('/')).Where(s => s.Length > 0).ToArray();
            for (int i = 0; i < segs.Length - 1; i++)
            {
                if (segs[i] == "b" && i + 2 <= segs.Length - 1 && segs[i + 2] == "o")
                {
                    bucket = segs[i + 1];
                    objectName = Uri.UnescapeDataString(string.Join('/', segs.Skip(i + 3)));
                    return true;
                }
            }

            return false;
        }

        // PATCH /api/properties/{propertyId}/images/reorder
        [HttpPatch("reorder")]
        [Authorize]
        public async Task<IActionResult> Reorder(
            [FromRoute] Guid propertyId,
            [FromBody] Guid[] orderedMediaIds)
        {
            var media = await _db.PropertyMedia
                .Where(m => m.PropertyId == propertyId)
                .ToListAsync();

            if (!media.Any())
                return NotFound();

            for (int i = 0; i < orderedMediaIds.Length; i++)
            {
                var item = media.FirstOrDefault(m => m.Id == orderedMediaIds[i]);
                if (item != null)
                {
                    item.SortOrder = i + 1;
                    item.IsCover = i == 0;
                }
            }

            var cover = media.FirstOrDefault(m => m.IsCover);
            if (cover != null)
            {
                var prop = await _db.Properties.FindAsync(propertyId);
                prop!.CoverImageUrl = cover.Url;
            }

            await _db.SaveChangesAsync();
            return NoContent();
        }

    }
}
