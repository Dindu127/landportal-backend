using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System;

namespace LandPortal.Api.Entities
{
    public class User
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Email { get; set; } = default!;
        public string? PasswordHash { get; set; } = default!;
        public string? FullName { get; set; } = default!;

        public string? Phone { get; set; }
        public string Role { get; set; } = "User"; // "User" | "Admin"
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public string? PasswordResetOtp { get; set; }
        public DateTime? PasswordResetExpiry { get; set; }
        public int PasswordResetAttempts { get; set; } = 0;
        public string? ProfilePhotoUrl { get; set; }

        // nav
        public ICollection<Property> Properties { get; set; } = new List<Property>();
    }
}
