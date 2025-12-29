// LandPortal.Api.Controllers/PropertyContactController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Security.Claims;
using System.Threading.Tasks;
using LandPortal.Api.Data;
using LandPortal.Api.DTOs;
using LandPortal.Api.Services;

namespace LandPortal.Api.Controllers
{
    [ApiController]
    [Route("api/properties/{propertyId:guid}/contact-owner")]
    public class PropertyContactController : ControllerBase
    {
        private readonly LandPortalDbContext _db;
        private readonly IContactService _contactService;

        public PropertyContactController(LandPortalDbContext db, IContactService contactService)
        {
            _db = db;
            _contactService = contactService;
        }

        // GET: /api/properties/{propertyId}/contact-owner
        [HttpGet]
        [Authorize]
        public async Task<IActionResult> GetOwnerContact([FromRoute] Guid propertyId)
        {
            var userId = GetCurrentUserId();
            if (userId == Guid.Empty) return Unauthorized();

            var property = await _db.Properties.Include(p => p.Owner).FirstOrDefaultAsync(p => p.Id == propertyId);
            if (property == null) return NotFound(new { detail = "Property not found" });

            var allowed = await _contactService.HasAccessToOwnerContactAsync(propertyId, userId);
            if (!allowed)
            {
                // return 402 Payment Required with unlock info
                return StatusCode(402, new
                {
                    message = "Contact owner details are locked. Unlock to view.",
                    unlockEndpoint = Url.Action(nameof(UnlockContact), "PropertyContact", new { propertyId }, Request.Scheme),
                    price = 199.00m,
                    currency = "INR"
                });
            }

            var owner = property.Owner;
            var dto = new OwnerContactResponse
            {
                PropertyId = property.Id,
                OwnerId = property.OwnerId,
                OwnerName = owner?.FullName ?? string.Empty,
                Phone = owner?.Phone,
                Email = owner?.Email,
                WhatsApp = owner?.Phone,     // whatsapp == phone
                IsUnlocked = true
            };

            return Ok(dto);
        }

        // POST: /api/properties/{propertyId}/contact-owner/unlock
        [HttpPost("unlock")]
        [Authorize]
        public async Task<IActionResult> UnlockContact([FromRoute] Guid propertyId, [FromBody] UnlockContactRequest req)
        {
            var userId = GetCurrentUserId();
            if (userId == Guid.Empty) return Unauthorized();

            var property = await _db.Properties.AsNoTracking().FirstOrDefaultAsync(p => p.Id == propertyId);
            if (property == null) return NotFound(new { detail = "Property not found" });

            // Quick local test: accept token or zero amount
            var paymentSuccess = !string.IsNullOrEmpty(req?.PaymentToken) || (req?.Amount.GetValueOrDefault() == 0);

            if (!paymentSuccess)
            {
                return BadRequest(new { detail = "Payment required. Provide payment token." });
            }

            var transactionId = req?.PaymentToken ?? $"sim-{Guid.NewGuid()}";

            var cv = await _contactService.CreateUnlockAsync(propertyId, userId, req?.Amount, transactionId, isPremiumAccess: true);

            var owner = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == property.OwnerId);
            var dto = new OwnerContactResponse
            {
                PropertyId = propertyId,
                OwnerId = property.OwnerId,
                OwnerName = owner?.FullName ?? string.Empty,
                Phone = owner?.Phone,
                Email = owner?.Email,
                WhatsApp = owner?.Phone,   // whatsapp == phone
                IsUnlocked = true
            };

            return Ok(new { message = "Unlocked", contact = dto, recordId = cv.Id });
        }

        private Guid GetCurrentUserId()
        {
            var sub = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
            return Guid.TryParse(sub, out var id) ? id : Guid.Empty;
        }
    }
}

public class UnlockRequest
{
    public Guid UserId { get; set; }
    public string? UserEmail { get; set; }
    public string? UserName { get; set; }
    public string? PropertyTitle { get; set; }
    public string? PaymentId { get; set; }
    public decimal? PaymentAmount { get; set; }
    public string? Currency { get; set; }
    public string? PaymentStatus { get; set; }
    public string? Notes { get; set; }
}
