namespace LandPortal.Api.DTOs
{
    public class UserSummaryDto
    {
        public Guid UserId { get; set; }
        public string FullName { get; set; } = "";
        public string Email { get; set; } = "";
        public string? Phone { get; set; }
        public string? Role { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
