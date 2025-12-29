using System;

namespace LandPortal.Api.Entities
{
    public class PendingUnlock
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        // Razorpay order id (or other provider order id)
        public string? OrderId { get; set; }

        public Guid PropertyId { get; set; }
        public Guid UserId { get; set; }

        // amount in major currency units (eg. 499.00)
        public decimal Amount { get; set; }
        public string? Currency { get; set; } = "INR";

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? CompletedAt { get; set; } = null;

        // Status: Pending, Completed, Failed
        public string Status { get; set; } = "Pending";

        // Optional: link to created ContactUnlockLog.Id if completed
        public Guid? UnlockLogId { get; set; }

        public string? Notes { get; set; }
        public string UserEmail { get; set; } = string.Empty;
        public string PropertyTitle { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
    }
}
