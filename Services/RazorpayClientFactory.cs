using Microsoft.Extensions.Options;
using Razorpay.Api;
using LandPortal.Api.Entities;
using System;

namespace LandPortal.Api.Services
{
    public class RazorpayClientFactory
    {
        private readonly RazorpayOptions _opts;

        public RazorpayClientFactory(IOptions<RazorpayOptions> opts)
        {
            _opts = opts?.Value ?? throw new ArgumentNullException(nameof(opts));
        }

        public RazorpayClient Create()
        {
            if (string.IsNullOrWhiteSpace(_opts.KeyId) || string.IsNullOrWhiteSpace(_opts.KeySecret))
                throw new InvalidOperationException("Razorpay KeyId/KeySecret not configured.");

            return new RazorpayClient(_opts.KeyId, _opts.KeySecret);
        }
    }
}
