using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using LandPortal.Api.Services;
using LandPortal.Api.Entities;

namespace LandPortal.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class WebhookController : ControllerBase
    {
        private readonly IUnlockLogService _unlockLogService;
        private readonly PaymentWebhookOptions _opts;

        public WebhookController(IUnlockLogService unlockLogService, IOptions<PaymentWebhookOptions> opts)
        {
            _unlockLogService = unlockLogService;
            _opts = opts.Value;
        }

        [HttpPost("razorpay")]
        public async Task<IActionResult> RazorpayWebhook()
        {
            // Razorpay sends signature in header X-Razorpay-Signature
            if (!Request.Headers.TryGetValue("X-Razorpay-Signature", out var sigHeader))
                return Unauthorized("Missing signature header");

            // read raw body (important for signature calculation)
            Request.EnableBuffering();
            using var sr = new StreamReader(Request.Body, Encoding.UTF8, leaveOpen: true);
            var body = await sr.ReadToEndAsync();
            Request.Body.Position = 0;

            // compute HMACSHA256(body, secret)
            var secret = _opts.Secret ?? "";
            var computed = ComputeHmacSha256(body, secret);

            if (!string.Equals(computed, sigHeader.ToString(), StringComparison.OrdinalIgnoreCase))
                return Unauthorized("Invalid signature");

            // parse event
            using var doc = JsonDocument.Parse(body);
            var root = doc.RootElement;

            // Example: for order/payments we can check payload structure
            // handle payment captured / authorized event
            try
            {
                var ev = root.GetProperty("event").GetString() ?? "";

                // Example: razorpay payment event -> contains payload.payment.entity.order_id & payment_id & amount
                if (root.TryGetProperty("payload", out var payload)
                    && payload.TryGetProperty("payment", out var payment)
                    && payment.TryGetProperty("entity", out var entity))
                {
                    var orderId = entity.GetProperty("order_id").GetString();
                    var paymentId = entity.GetProperty("id").GetString();
                    var amount = entity.GetProperty("amount").GetInt32(); // paise
                    var status = entity.GetProperty("status").GetString();

                    // locate pending by orderId
                    var pending = await _unlockLogService.FindPendingByOrderIdAsync(orderId ?? "");
                    if (pending == null)
                        return Ok(new { ok = true, message = "No matching pending" });

                    // create unlock log and mark pending completed
                    var unlockLog = new ContactUnlockLog
                    {
                        Id = Guid.NewGuid(),
                        PropertyId = pending.PropertyId,
                        PropertyTitle = pending.PropertyTitle,
                        UnlockedByUserId = pending.UserId,
                        UnlockedByUserEmail = pending.UserEmail,
                        UnlockedByUserName = pending.UserName,
                        PaymentId = paymentId,
                        PaymentAmount = (decimal)amount / 100m,
                        Currency = pending.Currency,
                        PaymentStatus = status,
                        Notes = $"Webhook event: {ev}",
                        CreatedAt = DateTime.UtcNow
                    };

                    // optional: use stored proc insert
                    await _unlockLogService.CreateAsync(unlockLog);

                    // mark pending as completed
                    await _unlockLogService.MarkPendingCompletedAsync(pending, unlockLog.Id);

                    return Ok(new { ok = true });
                }

                return Ok(new { ok = true, message = "Unhandled event" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { ok = false, err = ex.Message });
            }
        }

        private static string ComputeHmacSha256(string payload, string secret)
        {
            var keyBytes = Encoding.UTF8.GetBytes(secret);
            var payloadBytes = Encoding.UTF8.GetBytes(payload);
            using var hmac = new HMACSHA256(keyBytes);
            var hash = hmac.ComputeHash(payloadBytes);
            return Convert.ToBase64String(hash);
        }
    }

    public class PaymentWebhookOptions
    {
        public string? Secret { get; set; }
    }
}
