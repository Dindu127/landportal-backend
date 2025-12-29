namespace LandPortal.Api.DTOs
{
    public class UnlockedContactDto
    {
        public Guid PropertyId { get; set; }
        public string PropertyTitle { get; set; } = "";
        public DateTime UnlockedOn { get; set; }
        public string? TransactionId { get; set; }
        public string OwnerName { get; set; } = "";
        public string OwnerPhone { get; set; } = "";
        public Guid UnlockedByUserId { get; set; }
        public decimal? PaymentAmount { get; set; }
        public string? PaymentStatus { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
