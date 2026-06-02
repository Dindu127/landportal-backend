using LandPortal.Api.Data;
using LandPortal.Api.DTOs;
using LandPortal.Api.Entities;
using LandPortal.Api.Enums;
using LandPortal.Api.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace LandPortal.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PropertiesController : ControllerBase
    {
        private readonly LandPortalDbContext _db;
        private readonly GcpStorageService _gcs;
        public PropertiesController(LandPortalDbContext db, GcpStorageService gcs) {_db = db; _gcs = gcs; }

        // GET /api/properties
        [HttpGet]
        [AllowAnonymous]
        public async Task<ActionResult<PagedResult<PropertyResponse>>> Search(
            [FromQuery] string? search = null,
            [FromQuery] string? city = null,
            [FromQuery] string? locality = null,
            [FromQuery] decimal? minPrice = null,
            [FromQuery] decimal? maxPrice = null,
            [FromQuery] decimal? minSize = null,
            [FromQuery] decimal? maxSize = null,
            [FromQuery] string? plotType = null,
            [FromQuery] string? facing = null,
            [FromQuery] string? roadAccess = null,
            [FromQuery] string? brokerage = null,
            [FromQuery] bool onlyFeatured = false,
            [FromQuery] string? sort = "listedDesc",
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 12,
            [FromQuery] string? location = null)
        {
            if (string.IsNullOrWhiteSpace(city) && !string.IsNullOrWhiteSpace(location))
            {
                city = location?.Trim();
            }

            page = Math.Max(1, page);
            pageSize = Math.Clamp(pageSize, 1, 100);

            var q = _db.Properties.AsNoTracking()
                .Where(p => p.Status != null && p.Status == "Approved")
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim().ToLower();
                q = q.Where(p =>
                    (p.Title != null && p.Title.ToLower().Contains(s)) ||
                    (p.Description != null && p.Description.ToLower().Contains(s))
                );
            }

            if (!string.IsNullOrWhiteSpace(city))
            {
                var c = city.Trim().ToLower();
                q = q.Where(p => p.City != null && p.City.ToLower() == c);
            }

            if (!string.IsNullOrWhiteSpace(locality))
            {
                var l = locality.Trim().ToLower();
                q = q.Where(p => p.Locality != null && p.Locality.ToLower() == l);
            }

            if (!string.IsNullOrWhiteSpace(plotType))
            {
                var v = plotType.Trim().ToLower();
                q = q.Where(p => p.PlotType != null && p.PlotType.ToLower() == v);
            }

            if (!string.IsNullOrWhiteSpace(facing))
            {
                var v = facing.Trim().ToLower();
                q = q.Where(p => p.Facing != null && p.Facing.ToLower() == v);
            }

            if (!string.IsNullOrWhiteSpace(roadAccess))
            {
                var v = roadAccess.Trim().ToLower();
                q = q.Where(p => p.RoadAccess != null && p.RoadAccess.ToLower() == v);
            }

            if (!string.IsNullOrWhiteSpace(brokerage))
            {
                var v = brokerage.Trim().ToLower();
                q = q.Where(p => p.Brokerage != null && p.Brokerage.ToLower().Contains(v));
            }

            if (minPrice.HasValue) q = q.Where(p => p.Price >= minPrice.Value);
            if (maxPrice.HasValue) q = q.Where(p => p.Price <= maxPrice.Value);
            if (minSize.HasValue) q = q.Where(p => p.LandSize >= minSize.Value);
            if (maxSize.HasValue) q = q.Where(p => p.LandSize <= maxSize.Value);

            if (onlyFeatured) q = q.Where(p => p.IsFeatured == true);

            q = (sort ?? "").ToLower() switch
            {
                "pricedesc" => q.OrderByDescending(p => p.Price).ThenByDescending(p => p.ListedAt),
                "priceasc" => q.OrderBy(p => p.Price).ThenByDescending(p => p.ListedAt),
                "sizedesc" => q.OrderByDescending(p => p.LandSize).ThenByDescending(p => p.ListedAt),
                "sizeasc" => q.OrderBy(p => p.LandSize).ThenByDescending(p => p.ListedAt),
                "updateddesc" => q.OrderByDescending(p => p.UpdatedAt).ThenByDescending(p => p.ListedAt),
                "featuredfirst" => q.OrderByDescending(p => p.IsFeatured).ThenByDescending(p => p.Price).ThenByDescending(p => p.ListedAt),
                _ => q.OrderByDescending(p => p.IsFeatured).ThenByDescending(p => p.ListedAt),
            };

            var total = await q.CountAsync();

            var items = await q.Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(p => new PropertyResponse
                {
                    Id = p.Id,
                    OwnerId = p.OwnerId,
                    Title = p.Title,
                    Description = p.Description,
                    Price = p.Price,
                    City = p.City,
                    Locality = p.Locality,
                    LandSize = p.LandSize,
                    SizeUnit = p.SizeUnit,
                    CoverImageUrl = p.CoverImageUrl,
                    IsFeatured = p.IsFeatured,
                    IsSold = p.IsSold,            // ← included
                    ListedAt = p.ListedAt,
                    UpdatedAt = p.UpdatedAt,
                    Brokerage = p.Brokerage,
                    Status = p.Status.ToString(),
                    RoadAccess = p.RoadAccess,
                    Facing = p.Facing,
                    PlotType = p.PlotType
                })
                .ToListAsync();

            return Ok(new PagedResult<PropertyResponse>
            {
                Total = total,
                Page = page,
                PageSize = pageSize,
                Items = items
            });
        }

        // GET /api/properties/metadata/cities
        [HttpGet("metadata/cities")]
        [AllowAnonymous]
        public async Task<ActionResult<IEnumerable<string>>> GetCities()
        {
            var cities = await _db.Properties
                .AsNoTracking()
                .Where(p => p.Status != null && p.Status == "Approved" && !string.IsNullOrEmpty(p.City))
                .Select(p => p.City!.Trim())
                .Distinct()
                .OrderBy(c => c)
                .ToListAsync();

            return Ok(cities);
        }

        // GET /api/properties/metadata/localities?city=Chennai
        [HttpGet("metadata/localities")]
        [AllowAnonymous]
        public async Task<ActionResult<IEnumerable<string>>> GetLocalities([FromQuery] string? city = null)
        {
            var q = _db.Properties.AsNoTracking().Where(p => p.Status != null && p.Status == "Approved" && !string.IsNullOrEmpty(p.Locality));

            if (!string.IsNullOrWhiteSpace(city))
            {
                var c = city.Trim();
                q = q.Where(p => p.City == c);
            }

            var localities = await q.Select(p => p.Locality!.Trim())
                                   .Distinct()
                                   .OrderBy(l => l)
                                   .ToListAsync();

            return Ok(localities);
        }

        // GET /api/properties/{id}
        [HttpGet("{id:guid}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetById(Guid id)
        {
            var dto = await _db.Properties
                .Include(p => p.Media)
                .Where(p => p.Id == id)
                .Select(p => new PropertyResponse
                {
                    Id = p.Id,
                    OwnerId = p.OwnerId,
                    Title = p.Title,
                    Description = p.Description,
                    Price = p.Price,
                    City = p.City,
                    Locality = p.Locality,
                    LandSize = p.LandSize,
                    SizeUnit = p.SizeUnit,
                    CoverImageUrl = p.CoverImageUrl,
                    IsFeatured = p.IsFeatured,
                    IsSold = p.IsSold,
                    ListedAt = p.ListedAt,
                    UpdatedAt = p.UpdatedAt,
                    Status = p.Status.ToString(),
                    RoadAccess = p.RoadAccess,
                    Facing = p.Facing,
                    PlotType = p.PlotType,
                    Brokerage = p.Brokerage,

                    Media = p.Media
                        .OrderBy(m => m.SortOrder)
                        .Select(m => new PropertyMediaResponse
                        {
                            Id = m.Id,
                            PropertyId = m.PropertyId,
                            Url = m.Url,
                            ContentType = m.ContentType,
                            SizeBytes = m.SizeBytes,
                            Width = m.Width,
                            Height = m.Height,
                            SortOrder = m.SortOrder,
                            CreatedAt = m.CreatedAt,
                            IsCover = m.IsCover,
                            Path = m.Path,
                            PublicUrl = m.PublicUrl
                        })
                        .ToList()
                })
                .FirstOrDefaultAsync();

            if (dto == null) return NotFound();
            return Ok(dto);
        }

        // POST /api/properties  (owner creates → Pending)
        [HttpPost]
        [Authorize]
        public async Task<ActionResult<PropertyResponse>> Create([FromBody] CreatePropertyRequest req)
        {
            if (req == null) return BadRequest(new { message = "Request body is required" });

            if (string.IsNullOrWhiteSpace(req.Title))
                ModelState.AddModelError(nameof(req.Title), "Title is required.");
            if (string.IsNullOrWhiteSpace(req.Description))
                ModelState.AddModelError(nameof(req.Description), "Description is required.");
            if (string.IsNullOrWhiteSpace(req.City))
                ModelState.AddModelError(nameof(req.City), "City is required.");
            if (string.IsNullOrWhiteSpace(req.Locality))
                ModelState.AddModelError(nameof(req.Locality), "Locality is required.");
            if (req.Price <= 0)
                ModelState.AddModelError(nameof(req.Price), "Price must be greater than zero.");
            if (req.LandSize <= 0)
                ModelState.AddModelError(nameof(req.LandSize), "Land size must be greater than zero.");

            if (!ModelState.IsValid)
                return ValidationProblem(ModelState);

            Guid ownerId;
            try
            {
                ownerId = User.GetUserId();
            }
            catch
            {
                return Unauthorized(new { message = "Invalid token: missing subject (sub) claim." });
            }

            var p = new Property
            {
                OwnerId = ownerId,
                Title = req.Title!.Trim(),
                Description = req.Description!.Trim(),
                Price = req.Price,
                City = req.City!.Trim(),
                Locality = req.Locality!.Trim(),
                LandSize = req.LandSize,
                SizeUnit = req.SizeUnit,

                // ✅ REQUIRED FIXES
                Status = PropertyStatus.Pending.ToString(),
                IsFeatured = false,
                IsSold = false,
                ListedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,

                RoadAccess = req.RoadAccess,
                Facing = req.Facing,
                PlotType = req.PlotType,
                Brokerage = req.Brokerage
            };

            try
            {
                _db.Properties.Add(p);
                await _db.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                return Problem(
                    title: "Property save failed",
                    detail: ex.InnerException?.Message ?? ex.Message,
                    statusCode: 500
                );
            }


            return CreatedAtAction(nameof(GetById), new { id = p.Id }, new PropertyResponse
            {
                Id = p.Id,
                OwnerId = p.OwnerId,
                Title = p.Title,
                Description = p.Description,
                Price = p.Price,
                City = p.City,
                Locality = p.Locality,
                LandSize = p.LandSize,
                //SizeUnit = p.SizeUnit,
                CoverImageUrl = p.CoverImageUrl,
                IsFeatured = p.IsFeatured,
                IsSold = p.IsSold,            // ← included
                ListedAt = p.ListedAt,
                UpdatedAt = p.UpdatedAt,
                //Status = p.Status.ToString()
            });
        }

        [ApiExplorerSettings(IgnoreApi = true)]
        [Authorize]
        [HttpPost("{id:guid}/images/upload")]
        public async Task<IActionResult> UploadPropertyImage(
        Guid id,
        IFormFile file,
        [FromServices] GcpStorageService gcs)
        {
            if (file == null || file.Length == 0)
                return BadRequest("No file uploaded");

            var userId = User.GetUserId();

            var property = await _db.Properties
                .Include(p => p.Media)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (property == null)
                return NotFound();

            // 🔐 Owner or Admin only
            if (property.OwnerId != userId && !User.IsAdmin())
                return Forbid();

            // ☁ Upload to GCS
            var url = await gcs.UploadAsync(
                file.OpenReadStream(),
                $"images/properties/{id}/{Guid.NewGuid()}{Path.GetExtension(file.FileName)}",
                file.ContentType ?? "application/octet-stream"
            );

            var media = new PropertyMedia
            {
                Id = Guid.NewGuid(),
                PropertyId = id,
                Url = url,
                PublicUrl = url,
                Path = url,
                ContentType = file.ContentType ?? "application/octet-stream", // ✅ FIX
                SizeBytes = file.Length,                                       // ✅ FIX
                IsCover = !property.Media.Any(),
                SortOrder = property.Media.Count + 1,
                CreatedAt = DateTime.UtcNow
            };

            _db.PropertyMedia.Add(media);

            if (media.IsCover)
                property.CoverImageUrl = url;

            await _db.SaveChangesAsync();

            return Ok(new
            {
                media.Id,
                media.Url,
                media.IsCover
            });
        }


        // DTO for marking sold (optional)
        public class MarkSoldRequest
        {
            public DateTime? SoldAt { get; set; }
        }

        // PUT mark-sold
        [HttpPut("{id:guid}/mark-sold")]
        [Authorize]
        public async Task<IActionResult> MarkSold(Guid id, [FromBody] MarkSoldRequest? req = null)
        {
            var p = await _db.Properties.FindAsync(id);
            if (p == null) return NotFound();

            var me = User.GetUserId();

            // owner OR admin can mark sold (unchanged)
            if (p.OwnerId != me && !User.IsAdmin())
                return Forbid();

            if (p.IsSold)
            {
                return BadRequest(new { message = "Property already marked as sold." });
            }

            // ✅ MARK SOLD
            p.IsSold = true;
            p.SoldById = me;

            // 🔴 AUTO-UNFEATURE WHEN SOLD (KEY LINE)
            if (p.IsFeatured)
            {
                p.IsFeatured = false;
            }

            p.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();
            return NoContent();
        }


        // PUT mark-available
        [HttpPut("{id:guid}/mark-available")]
        [Authorize]
        public async Task<IActionResult> MarkAvailable(Guid id)
        {
            var p = await _db.Properties.FindAsync(id);
            if (p == null) return NotFound();

            var me = User.GetUserId();

            if (p.OwnerId != me && !User.IsAdmin()) return Forbid();

            if (!p.IsSold)
            {
                return BadRequest(new { message = "Property is not marked as sold." });
            }

            p.IsSold = false;
            p.SoldById = null;
            p.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();
            return NoContent();
        }

        [Authorize]
        [HttpGet("my")]
        public async Task<IActionResult> MyProperties()
        {
            var userId = Guid.Parse(
                User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value
            );

            var data = await _db.Properties
                .Where(p => p.OwnerId == userId)
                .OrderByDescending(p => p.UpdatedAt)
                .Select(p => new
                {
                    p.Id,
                    p.Title,
                    p.Price,
                    p.IsFeatured,
                    p.City,
                    p.Locality,
                    p.Status,
                    p.UpdatedAt,
                    IsSold = p.Status != null && p.Status == "Approved" && p.IsSold, // Fixed comparison and syntax
                    imageUrl = p.Media
                        .OrderByDescending(m => m.IsCover)
                        .Select(m => m.Url)
                        .FirstOrDefault()
                })
                .ToListAsync();

            return Ok(data);
        }


        [Authorize]
        [HttpGet("{id}/edit")]
        public async Task<IActionResult> GetForEdit(Guid id)
        {
            //  var userId = Guid.Parse(User.FindFirst("uid")!.Value);
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
                return Unauthorized();

            var userId = Guid.Parse(userIdClaim.Value);


            var prop = await _db.Properties
                .Where(p =>p.Id == id &&(p.OwnerId == userId || User.IsAdmin()))
                .Select(p => new
                {
                    p.Title,
                    p.Description,
                    p.Price,
                    p.City,
                    p.Locality,
                    p.LandSize,
                    p.SizeUnit,
                    Status = p.Status.ToString(),
                    p.CoverImageUrl,
                    p.Brokerage,
                    p.Facing,
                    p.PlotType,
                    p.RoadAccess
                })
                .FirstOrDefaultAsync();

            if (prop == null)
                return NotFound();
            ;

            return Ok(prop);
        }


        [Authorize]
        [HttpPut("{id:guid}")]
        public async Task<IActionResult> Update(Guid id, UpdatePropertyDto dto)
        {
            var userId = User.GetUserId();

            if (!ModelState.IsValid)
                return ValidationProblem(ModelState);

            var prop = await _db.Properties
                .Include(p => p.Media)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (prop == null)
                return NotFound();

            if (prop.OwnerId != userId && !User.IsAdmin())
                return Forbid();

            prop.Title = dto.Title?.Trim();
            prop.Description = dto.Description?.Trim();
            prop.Price = dto.Price;
            prop.City = dto.City?.Trim();
            prop.Locality = dto.Locality?.Trim();
            prop.LandSize = dto.LandSize;
            prop.SizeUnit = dto.SizeUnit;
            prop.RoadAccess = dto.RoadAccess;
            prop.Facing = dto.Facing;
            prop.PlotType = dto.PlotType;
            prop.Brokerage = dto.Brokerage;
            prop.UpdatedAt = DateTime.UtcNow;

            if (!string.IsNullOrWhiteSpace(dto.CoverImageUrl))
                prop.CoverImageUrl = dto.CoverImageUrl.Trim();

            await _db.SaveChangesAsync();
            return NoContent();
        }


        [Authorize]
        [HttpPut("{id}/images")]
        public async Task<IActionResult> UpdateImages(Guid id, UpdatePropertyImagesDto dto)
        {
            var userId = User.GetUserId();

            var prop = await _db.Properties
                .Include(p => p.Media)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (prop == null)
                return NotFound();

            if (prop.OwnerId != userId && !User.IsAdmin())
                return Forbid();

            // REMOVE only existing DB records
            _db.PropertyMedia.RemoveRange(prop.Media);

            var now = DateTime.UtcNow;

            prop.Media = dto.Images.Select(i => new PropertyMedia
            {
                Id = Guid.NewGuid(),
                PropertyId = prop.Id,
                Url = i.Url,
                PublicUrl = i.Url,
                Path = i.Url,
                IsCover = i.IsCover,
                SortOrder = i.SortOrder,
                ContentType = "image/jpeg",   // or infer from URL
                SizeBytes = 0,                // optional if unknown
                CreatedAt = now
            }).ToList();

            prop.CoverImageUrl = prop.Media.FirstOrDefault(x => x.IsCover)?.Url;
            prop.UpdatedAt = now;

            await _db.SaveChangesAsync();
            return Ok();
        }


        [Authorize(Roles = "Admin")]
        [HttpPut("{id:guid}/feature")]
        public async Task<IActionResult> SetFeatured(
            Guid id,
            [FromBody] FeaturePropertyDto dto)
        {
            var property = await _db.Properties.FindAsync(id);
            if (property == null)
                return NotFound();

            if (property.IsSold)
            {
                return BadRequest("Sold property cannot be featured.");
            }

            property.IsFeatured = dto.IsFeatured;
            property.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();
            return NoContent(); // 204
        }


        [Authorize]
        [HttpPut("{id:guid}/status")]
        public async Task<IActionResult> ChangeStatus(Guid id, [FromQuery] PropertyStatus status)
        {
            if (!User.IsAdmin())
                return Forbid();

            var prop = await _db.Properties.FindAsync(id);
            if (prop == null)
                return NotFound();

            prop.Status = status.ToString();
            prop.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();
            return NoContent();
        }

        //[Authorize]
        [HttpGet("admin")]
        public async Task<IActionResult> AdminList(
            [FromQuery] string? search = null,
            [FromQuery] PropertyStatus? status = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20
        )
        {
            //if (!User.IsAdmin())
            //    return Forbid();

            page = Math.Max(1, page);
            pageSize = Math.Clamp(pageSize, 1, 100);

            var q = _db.Properties
                    .AsNoTracking()
                    .Include(p => p.Owner)   // ✅ navigation only
                    .AsQueryable();

            // Search: title / city / owner email
            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim().ToLower();
                q = q.Where(p =>
                    p.Title.ToLower().Contains(s) ||
                    p.City.ToLower().Contains(s) ||
                    p.Owner.Email.ToLower().Contains(s)
                );
            }

            // Filter by status
            if (status.HasValue)
            {
                q = q.Where(p => p.Status == status.Value.ToString());
            }

            var total = await q.CountAsync();

            var items = await q
                .OrderByDescending(p => p.UpdatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(p => new
                {
                    p.Id,
                    p.Title,
                    p.Price,
                    p.City,
                    p.Locality,
                    Status = p.Status.ToString(),
                    p.IsFeatured,
                    p.IsSold,
                    p.UpdatedAt,
                    OwnerEmail = p.Owner.Email
                })
                .ToListAsync();

            return Ok(new
            {
                total,
                page,
                pageSize,
                items
            });
        }


    }

    public class FeaturePropertyDto
    {
        public bool IsFeatured { get; set; }
    }

}
