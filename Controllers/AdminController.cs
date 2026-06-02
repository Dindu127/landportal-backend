using LandPortal.Api.Data;
using LandPortal.Api.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LandPortal.Api.Entities;
using System.Linq;
using LandPortal.Api.DTOs;

namespace LandPortal.Api.Controllers
{
    [ApiController]
    [Authorize(Roles = "Admin")]
    [Route("api/[controller]")]
    public class AdminController : ControllerBase
    {
        private readonly LandPortalDbContext _db;
        public AdminController(LandPortalDbContext db) => _db = db;

        [HttpGet("properties")]
        public async Task<ActionResult<PagedResult<AdminPropertyDto>>> GetByStatus(
            [FromQuery] PropertyStatus status = PropertyStatus.Pending,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            page = Math.Max(1, page);
            pageSize = Math.Clamp(pageSize, 1, 100);

            var q = _db.Properties.AsNoTracking()
                    .Where(p => p.Status == status.ToString())
                    .OrderByDescending(p => p.UpdatedAt);

            var total = await q.CountAsync();

            var items = await q.Skip((page - 1) * pageSize).Take(pageSize)
                .Select(p => new AdminPropertyDto
                {
                    Id = p.Id,
                    Title = p.Title,
                    City = p.City,
                    Locality = p.Locality,
                    Price = p.Price,
                    OwnerId = p.OwnerId,
                    OwnerName = _db.Users.Where(u => u.Id == p.OwnerId).Select(u => u.FullName ?? u.Email).FirstOrDefault(),
                    Status = p.Status.ToString(),
                    UpdatedAt = p.UpdatedAt,
                    ListedAt = p.ListedAt,
                    IsFeatured =p.IsFeatured,
                    RoadAccess = p.RoadAccess,
                    Media = p.Media.Select(m => new MediaDto
                    {
                        Url = m.Url,
                        IsCover = m.IsCover
                    }).ToList(),  
                    Facing = p.Facing,
                    PlotType = p.PlotType,
                    IsSold = p.IsSold,
                    Brokerage = p.Brokerage                    
                }).ToListAsync();

            return Ok(new PagedResult<AdminPropertyDto>
            {
                Total = total,
                Page = page,
                PageSize = pageSize,
                Items = items
            });
        }

        [HttpPost("properties/{id:guid}/approve")]
        public async Task<IActionResult> Approve(Guid id)
        {
            var p = await _db.Properties.FindAsync(id);
            if (p == null) return NotFound();

            p.Status = PropertyStatus.Approved.ToString();
            p.ListedAt ??= DateTime.UtcNow;
            p.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
            return NoContent();
        }

        [HttpPost("properties/{id:guid}/reject")]
        public async Task<IActionResult> Reject(Guid id)
        {
            var p = await _db.Properties.FindAsync(id);
            if (p == null) return NotFound();

            p.Status = PropertyStatus.Rejected.ToString();
            p.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
            return NoContent();
        }

        [HttpGet("users")]
        public async Task<ActionResult<IEnumerable<UserSummaryDto>>> GetUsers()
        {
            var users = await _db.Users
                .AsNoTracking()
                .Select(u => new UserSummaryDto
                {
                    UserId = u.Id,
                    FullName = u.FullName!,
                    Email = u.Email,
                    Phone = u.Phone,
                    Role = u.Role,
                    CreatedAt = u.CreatedAt
                })
                .OrderByDescending(u => u.CreatedAt)
                .ToListAsync();

            return Ok(users);
        }

        [HttpGet("unlocked-contacts/{userId:guid}")]
        public async Task<ActionResult<IEnumerable<UnlockedContactDto>>> GetUnlockedContacts(Guid userId)
        {
            var query = from log in _db.ContactUnlockLogs.AsNoTracking()
                        join prop in _db.Properties.AsNoTracking() on log.PropertyId equals prop.Id into pjoin
                        from prop in pjoin.DefaultIfEmpty()
                        join owner in _db.Users.AsNoTracking() on prop.OwnerId equals owner.Id into ojoin
                        from owner in ojoin.DefaultIfEmpty()
                        where log.UnlockedByUserId == userId
                        orderby log.CreatedAt descending
                        select new UnlockedContactDto
                        {
                            PropertyId = log.PropertyId,
                            PropertyTitle = (log.PropertyTitle ?? (prop != null ? prop.Title : null)) ?? string.Empty,
                            UnlockedOn = log.CreatedAt,
                            TransactionId = log.PaymentId ?? string.Empty,
                            PaymentAmount = log.PaymentAmount ?? 0m,
                            OwnerName = owner != null ? (owner.FullName ?? string.Empty) : string.Empty,
                            OwnerPhone = owner != null ? (owner.Phone ?? string.Empty) : string.Empty,
                            UnlockedByUserId = log.UnlockedByUserId
                        };

            var results = await query.ToListAsync();
            return Ok(results);
        }

        [HttpGet("unlocked-contacts")]
        public async Task<ActionResult<PagedResult<UnlockedContactDto>>> GetAllUnlockedContacts(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 50)
        {
            page = Math.Max(1, page);
            pageSize = Math.Clamp(pageSize, 1, 200);

            var baseQuery = from log in _db.ContactUnlockLogs.AsNoTracking()
                            join prop in _db.Properties.AsNoTracking() on log.PropertyId equals prop.Id into pjoin
                            from prop in pjoin.DefaultIfEmpty()
                            join owner in _db.Users.AsNoTracking() on prop.OwnerId equals owner.Id into ojoin
                            from owner in ojoin.DefaultIfEmpty()
                            orderby log.CreatedAt descending
                            select new UnlockedContactDto
                            {
                                PropertyId = log.PropertyId,
                                PropertyTitle = (log.PropertyTitle ?? (prop != null ? prop.Title : null)) ?? string.Empty,
                                UnlockedOn = log.CreatedAt,
                                TransactionId = log.PaymentId ?? string.Empty,
                                PaymentAmount = log.PaymentAmount ?? 0m,
                                OwnerName = owner != null ? (owner.FullName ?? string.Empty) : string.Empty,
                                OwnerPhone = owner != null ? (owner.Phone ?? string.Empty) : string.Empty,
                                UnlockedByUserId = log.UnlockedByUserId
                            };

            var total = await baseQuery.CountAsync();

            var items = await baseQuery
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return Ok(new PagedResult<UnlockedContactDto>
            {
                Total = total,
                Page = page,
                PageSize = pageSize,
                Items = items
            });
        }
    }
}