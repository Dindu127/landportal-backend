using System.ComponentModel.DataAnnotations;

namespace LandPortal.Api.DTOs
{
    public class RegisterRequest
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = default!;
        [Required]
        public string Password { get; set; } = default!;
        [Required]
        public string FullName { get; set; } = default!;
        [Required]
        public string? Phone { get; set; }
    }

    public class LoginRequest
    {
        public string? Email { get; set; }
        public string? Phone { get; set; } 
        [Required]
        public string Password { get; set; } = default!;
    }

    public class AuthResponse
    {
        public string AccessToken { get; set; } = default!;
        public string Email { get; set; } = default!;
        public string FullName { get; set; } = default!;
        public string Role { get; set; } = default!;
        public string UserId { get; set; } = default!;
    }
}
