namespace LandPortal.Api.DTOs
{
    public class UpdateProfileDto
    {
        public required string FullName { get; set; }
        public required string Phone { get; set; }
        public required string Email { get; set; }
    }
}
