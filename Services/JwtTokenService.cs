using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using LandPortal.Api.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace LandPortal.Api.Services
{
    public interface IJwtTokenService
    {
        string CreateToken(User user);
    }

    public class JwtTokenService : IJwtTokenService
    {
        private readonly IConfiguration _config;
        public JwtTokenService(IConfiguration config) => _config = config;

        public string CreateToken(User user)
        {
            if (user == null) throw new ArgumentNullException(nameof(user));

            var key = _config["Jwt:Key"] ?? throw new InvalidOperationException("Jwt:Key not set");
            var issuer = _config["Jwt:Issuer"];
            var audience = _config["Jwt:Audience"];
            var expiryHours = int.TryParse(_config["Jwt:ExpiryHours"], out var h) ? h : 2;

            // ✅ IMPORTANT: Add all required custom claims
            var claims = new List<Claim>
            {
                new Claim("userId", user.Id.ToString()),
                new Claim("email", user.Email ?? string.Empty),
                new Claim("fullName", user.FullName ?? string.Empty),
                new Claim("phone", user.Phone ?? string.Empty),
                new Claim("role", user.Role ?? "User"),

                // Standard claims
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            var creds = new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
                SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                expires: DateTime.UtcNow.AddHours(expiryHours),
                signingCredentials: creds);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
