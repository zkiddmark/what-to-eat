using System.Security.Claims;
using WhatToEatApp.Entities;

namespace WhatToEatApp.Services.Auth
{
    public static class AuthClaims
    {
        /// <summary>Ligger i cookien och jämförs mot databasen vid varje begäran.</summary>
        public const string SecurityStamp = "security-stamp";

        public static ClaimsPrincipal CreatePrincipal(AppUser user, string authenticationScheme)
        {
            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new(ClaimTypes.Name, user.Alias),
                new(ClaimTypes.Email, user.Email),
                new(ClaimTypes.Role, user.Role),
                new(SecurityStamp, user.SecurityStamp.ToString()),
            };
            return new ClaimsPrincipal(new ClaimsIdentity(claims, authenticationScheme));
        }

        public static Guid? GetUserId(ClaimsPrincipal principal)
            => Guid.TryParse(principal.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;

        public static Guid? GetSecurityStamp(ClaimsPrincipal principal)
            => Guid.TryParse(principal.FindFirstValue(SecurityStamp), out var stamp) ? stamp : null;
    }
}
