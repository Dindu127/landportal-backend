namespace LandPortal.Api.Entities
{
    public class ContactUnlockLog
    {
        public Guid Id { get; set; }
        public Guid PropertyId { get; set; }
        public string? PropertyTitle { get; set; }
        public Guid UnlockedByUserId { get; set; }
        public string? UnlockedByUserEmail { get; set; }
        public string? UnlockedByUserName { get; set; }
        public string? PaymentId { get; set; }
        public decimal? PaymentAmount { get; set; }
        public string? Currency { get; set; }
        public string? PaymentStatus { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public string? Notes { get; set; }
        public User? User { get; set; }
        public Property? Property { get; set; }
        public Guid UserId { get;  set; }
    }

}
