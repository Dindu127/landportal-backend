namespace LandPortal.Api.Entities
{
    public class EmailOtp
    {
        public Guid Id { get; set; }
        public string Email { get; set; } = default!;
        public string Otp { get; set; } = default!;
        public DateTime ExpiresAt { get; set; }
        public bool IsVerified { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
