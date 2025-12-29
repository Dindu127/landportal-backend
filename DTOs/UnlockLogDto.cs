using System;

namespace LandPortal.Api.DTOs
{
    public class UnlockLogDto
    {
        public Guid Id { get; set; }
        public Guid PropertyId { get; set; }
        public string? PropertyTitle { get; set; }
        public Guid UnlockedByUserId { get; set; }
        public string? UnlockedByUserName { get; set; }
        public string? UnlockedByUserEmail { get; set; }
        public string? PaymentId { get; set; }
        public decimal? PaymentAmount { get; set; }
        public string? Currency { get; set; }
        public string? PaymentStatus { get; set; }
        public DateTime CreatedAt { get; set; }
        public string? Notes { get; set; }  // optional (add if needed)
    }
}
