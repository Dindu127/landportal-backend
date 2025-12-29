using LandPortal.Api.Data;
using LandPortal.Api.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text;
using System.Globalization;

[ApiController]
[Route("api/admin/unlock-logs")]
[Authorize(Roles = "Admin")] // requires admin role
public class AdminUnlockLogsController : ControllerBase
{
    private readonly LandPortalDbContext _db;

    public AdminUnlockLogsController(LandPortalDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] int page = 1, [FromQuery] int pageSize = 20,
                                         [FromQuery] string? search = null,
                                         [FromQuery] Guid? propertyId = null,
                                         [FromQuery] Guid? userId = null,
                                         [FromQuery] DateTime? from = null,
                                         [FromQuery] DateTime? to = null)
    {
        var query = _db.ContactUnlockLogs.AsQueryable();

        if (propertyId.HasValue) query = query.Where(x => x.PropertyId == propertyId.Value);
        if (userId.HasValue) query = query.Where(x => x.UnlockedByUserId == userId.Value);
        if (!string.IsNullOrEmpty(search))
        {
            var s = search.Trim();
            query = query.Where(x =>
             (x.PropertyTitle != null && x.PropertyTitle.Contains(s)) ||
             (x.UnlockedByUserEmail != null && x.UnlockedByUserEmail.Contains(s)) ||
             (x.UnlockedByUserName != null && x.UnlockedByUserName.Contains(s)) );
        }
        if (from.HasValue) query = query.Where(x => x.CreatedAt >= from.Value);
        if (to.HasValue) query = query.Where(x => x.CreatedAt <= to.Value);

        var total = await query.CountAsync();
        var items = await query
            .OrderByDescending(x => x.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new UnlockLogDto
            {
                Id = x.Id,
                PropertyId = x.PropertyId,
                PropertyTitle = x.PropertyTitle,
                UnlockedByUserId = x.UnlockedByUserId,
                UnlockedByUserName = x.UnlockedByUserName,
                UnlockedByUserEmail = x.UnlockedByUserEmail,
                PaymentId = x.PaymentId,
                PaymentAmount = x.PaymentAmount,
                Currency = x.Currency,
                PaymentStatus = x.PaymentStatus,
                CreatedAt = x.CreatedAt
            })
            .ToListAsync();

        return Ok(new { total, page, pageSize, items });
    }

    // Optional: export endpoint
    [HttpGet("export")]
    public async Task<IActionResult> Export([FromQuery] string? search = null, [FromQuery] DateTime? from = null, [FromQuery] DateTime? to = null)
    {
        var query = _db.ContactUnlockLogs.AsQueryable();

        // same filters as in Get()
        if (!string.IsNullOrEmpty(search))
        {
            var s = search.Trim();
            query = query.Where(x =>
                (x.PropertyTitle != null && x.PropertyTitle.Contains(s)) ||
                (x.UnlockedByUserEmail != null && x.UnlockedByUserEmail.Contains(s)) ||
                (x.UnlockedByUserName != null && x.UnlockedByUserName.Contains(s))
            );
        }

        if (from.HasValue) query = query.Where(x => x.CreatedAt >= from.Value);
        if (to.HasValue) query = query.Where(x => x.CreatedAt <= to.Value);

        var items = await query
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => new UnlockLogDto
            {
                Id = x.Id,
                PropertyId = x.PropertyId,
                PropertyTitle = x.PropertyTitle,
                UnlockedByUserId = x.UnlockedByUserId,
                UnlockedByUserName = x.UnlockedByUserName,
                UnlockedByUserEmail = x.UnlockedByUserEmail,
                PaymentId = x.PaymentId,
                PaymentAmount = x.PaymentAmount,
                Currency = x.Currency,
                PaymentStatus = x.PaymentStatus,
                CreatedAt = x.CreatedAt,
                // Notes included if you added it to the DTO
                // Notes = x.Notes
            })
            .ToListAsync();

        // Build CSV
        string EscapeCsv(string? field)
        {
            if (string.IsNullOrEmpty(field)) return "";
            // escape quotes by doubling them
            var escaped = field.Replace("\"", "\"\"");
            // if it contains comma, quote or newline, wrap in quotes
            if (escaped.Contains(',') || escaped.Contains('"') || escaped.Contains('\n') || escaped.Contains('\r'))
                return $"\"{escaped}\"";
            return escaped;
        }

        var sb = new StringBuilder();

        // Header
        sb.AppendLine("Id,PropertyId,PropertyTitle,UnlockedByUserId,UnlockedByUserName,UnlockedByUserEmail,PaymentId,PaymentAmount,Currency,PaymentStatus,CreatedAt");

        // Rows
        foreach (var it in items)
        {
            sb.Append(it.Id.ToString());
            sb.Append(',');
            sb.Append(it.PropertyId.ToString());
            sb.Append(',');
            sb.Append(EscapeCsv(it.PropertyTitle));
            sb.Append(',');
            sb.Append(it.UnlockedByUserId.ToString());
            sb.Append(',');
            sb.Append(EscapeCsv(it.UnlockedByUserName));
            sb.Append(',');
            sb.Append(EscapeCsv(it.UnlockedByUserEmail));
            sb.Append(',');
            sb.Append(EscapeCsv(it.PaymentId));
            sb.Append(',');
            sb.Append(it.PaymentAmount.HasValue ? it.PaymentAmount.Value.ToString("F2", CultureInfo.InvariantCulture) : "");
            sb.Append(',');
            sb.Append(EscapeCsv(it.Currency));
            sb.Append(',');
            sb.Append(EscapeCsv(it.PaymentStatus));
            sb.Append(',');
            sb.Append(it.CreatedAt.ToString("o", CultureInfo.InvariantCulture)); // ISO format
            sb.AppendLine();
        }

        // Optionally prepend BOM so Excel detects UTF-8 correctly
        var csvString = sb.ToString();
        var bom = Encoding.UTF8.GetPreamble(); // returns BOM bytes if any
        var csvBytes = Encoding.UTF8.GetBytes(csvString);

        byte[] resultBytes;
        if (bom != null && bom.Length > 0)
        {
            resultBytes = new byte[bom.Length + csvBytes.Length];
            Buffer.BlockCopy(bom, 0, resultBytes, 0, bom.Length);
            Buffer.BlockCopy(csvBytes, 0, resultBytes, bom.Length, csvBytes.Length);
        }
        else
        {
            resultBytes = csvBytes;
        }

        return File(resultBytes, "text/csv", $"unlock-logs-{DateTime.UtcNow:yyyyMMddHHmmss}.csv");
    }

}
