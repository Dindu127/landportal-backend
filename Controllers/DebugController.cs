using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using LandPortal.Api.Services;
using LandPortal.Api.Entities;

namespace LandPortal.Api.Controllers
{
    [ApiController]
    [Route("api/debug")]
    public class DebugController : ControllerBase
    {
        private readonly RazorpayClientFactory _factory;
        private readonly RazorpayOptions _opts;

        public DebugController(RazorpayClientFactory factory, IOptions<RazorpayOptions> opts)
        {
            _factory = factory ?? throw new ArgumentNullException(nameof(factory));
            _opts = opts?.Value ?? new RazorpayOptions();
        }

        // returns masked key and whether key/secret are configured
        [HttpGet("razorpay-keys")]
        public IActionResult RazorpayKeys()
        {
            var keyMasked = string.IsNullOrEmpty(_opts.KeyId) ? null :
                (_opts.KeyId.Length > 6 ? _opts.KeyId.Substring(0, 6) + "..." : _opts.KeyId);

            return Ok(new
            {
                keyIdMasked = keyMasked,
                hasKey = !string.IsNullOrEmpty(_opts.KeyId),
                hasSecret = !string.IsNullOrEmpty(_opts.KeySecret)
            });
        }

        // quick smoke test that creates an order using the Razorpay SDK
        [HttpGet("razorpay-smoke")]
        public IActionResult Smoke()
        {
            try
            {
                // Create Razorpay client from factory
                var client = _factory.Create();

                // amount in paise (100 paise = Rs 1.00)
                var options = new Dictionary<string, object>
                {
                    { "amount", 100 },
                    { "currency", "INR" },
                    { "receipt", "smoketest" }
                };

                // the Razorpay SDK returns a dynamic-like object (Razorpay.Api.Resource)
                var order = client.Order.Create(options);

                // return the id for verification
                return Ok(new { ok = true, id = order["id"].ToString() });
            }
            catch (Exception ex)
            {
                // expose message for debugging (you can reduce verbosity later)
                return StatusCode(500, new { ok = false, err = ex.Message, type = ex.GetType().FullName });
            }
        }
    }
}
