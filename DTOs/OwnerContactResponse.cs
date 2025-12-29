namespace LandPortal.Api.DTOs
{
    public class OwnerContactResponse
    {
        public Guid PropertyId { get; set; }
        public Guid OwnerId { get; set; }
        public string OwnerName { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public string? Email { get; set; }
        public string? WhatsApp { get; set; }   // we'll set this to Phone on the server
        public bool IsUnlocked { get; set; } = false;

    }
}
