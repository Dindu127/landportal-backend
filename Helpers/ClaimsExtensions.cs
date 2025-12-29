using System;
using System.Linq;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;

namespace LandPortal.Api.Helpers
{
    public static class ClaimsExtensions
    {
        public static Guid GetUserId(this ClaimsPrincipal user)
        {
            if (user == null) throw new ArgumentNullException(nameof(user));

            // Try common claim types where the subject/user id might be
            var subClaim =
                user.FindFirst(JwtRegisteredClaimNames.Sub) ??
                user.FindFirst(ClaimTypes.NameIdentifier) ??
                user.FindFirst("sub") ??
                user.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier");

            if (subClaim == null || string.IsNullOrWhiteSpace(subClaim.Value))
                throw new InvalidOperationException("subject (sub) claim is missing from token");

            if (!Guid.TryParse(subClaim.Value, out var userId))
                throw new InvalidOperationException("subject (sub) claim is not a valid GUID");

            return userId;
        }

        public static bool IsAdmin(this ClaimsPrincipal user)
            => user?.IsInRole("Admin") ?? false;
    }
}
