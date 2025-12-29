using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Razorpay.Api;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LandPortal.Api.Entities; // adjust namespace if needed
using LandPortal.Api.Services; // IUnlockLogService

namespace LandPortal.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PaymentsController : ControllerBase
    {
        private readonly RazorpayClientFactory _razorFactory;
        private readonly IUnlockLogService _unlockLogService;
        private readonly RazorpayOptions _opts;

        public PaymentsController(
            RazorpayClientFactory razorFactory,
            IUnlockLogService unlockLogService,
            IOptions<RazorpayOptions> opts)
        {
            _razorFactory = razorFactory;
            _unlockLogService = unlockLogService;
            _opts = opts.Value;
        }

        [HttpPost("create-order")]
        public async Task<IActionResult> CreateOrder([FromBody] CreateOrderRequest req)
        {
            if (req == null) return BadRequest("Missing request body.");
            if (req.Amount <= 0) return BadRequest("Amount must be greater than zero.");

            try
            {
                // amount in paise
                var amountPaise = (int)Math.Round(req.Amount * 100);

                var client = _razorFactory.Create();

                var options = new Dictionary<string, object>
                {
                    { "amount", amountPaise },
                    { "currency", req.Currency ?? "INR" },
                    { "receipt", Guid.NewGuid().ToString() }, // unique receipt
                    { "payment_capture", 1 }
                };

                var order = client.Order.Create(options); // returns a dynamic/JObject-like object

                // Persist a pending unlock record (so webhook can match)
                var pending = new PendingUnlock
                {
                    Id = Guid.NewGuid(),
                    OrderId = order["id"].ToString(),
                    PropertyId = req.PropertyId,
                    UserId = req.UserId,
                    Amount = req.Amount,
                    Currency = req.Currency ?? "INR",
                    PropertyTitle = req.PropertyTitle ?? string.Empty,
                    CreatedAt = DateTime.UtcNow,
                    Status = "Pending"
                };

                await _unlockLogService.CreatePendingAsync(pending);

                var resp = new CreateOrderResponse
                {
                    OrderId = order["id"].ToString(),
                    AmountInPaise = amountPaise,
                    KeyId = _opts.KeyId, // safe to return KeyId (not secret). Mask on UI if needed.
                };

                return Ok(resp);
            }
            catch (Exception rex)
            {
                // return the Razorpay error message for debugging
                return StatusCode(500, new { ok = false, err = rex.Message, type = rex.GetType().FullName });
            }
        }
    }

    // DTOs
    public class CreateOrderRequest
    {
        public Guid PropertyId { get; set; }
        public Guid UserId { get; set; }
        public decimal Amount { get; set; }
        public string? PropertyTitle { get; set; }
        public string? Currency { get; set; } = "INR";
    }

    public class CreateOrderResponse
    {
        public string OrderId { get; set; } = "";
        public int AmountInPaise { get; set; }
        public string? KeyId { get; set; }
    }
}
