using System.ComponentModel.DataAnnotations.Schema;

namespace LandPortal.Api.Entities
{
    public class ContactView
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid PropertyId { get; set; }
        public Guid UserId { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? AmountPaid { get; set; }

        public string? TransactionId { get; set; }
        public bool IsPremiumAccess { get; set; } = false;
        public DateTime ViewedAt { get; set; } = DateTime.UtcNow;

    }
}
