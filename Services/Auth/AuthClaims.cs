using System.Security.Claims;
using WhatToEatApp.Entities;

namespace WhatToEatApp.Services.Auth
{
    public static class AuthClaims
    {
        /// <summary>Ligger i cookien och jämförs mot databasen vid varje begäran.</summary>
        public const string SecurityStamp = "security-stamp";

        /// <summary>
        /// Finns bara när kontot står på ett tillfälligt lösenord. Sätts vid inloggning och
        /// faller med cookien: båda vägarna ut ur tillståndet roterar stämpeln.
        /// </summary>
        public const string MustChangePassword = "must-change-password";

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
            if (user.TemporaryPasswordExpiresAt is not null)
            {
                claims.Add(new Claim(MustChangePassword, "true"));
            }
            return new ClaimsPrincipal(new ClaimsIdentity(claims, authenticationScheme));
        }

        public static Guid? GetUserId(ClaimsPrincipal principal)
            => Guid.TryParse(principal.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;

        public static bool RequiresPasswordChange(ClaimsPrincipal principal)
            => principal.HasClaim(MustChangePassword, "true");

        public static Guid? GetSecurityStamp(ClaimsPrincipal principal)
            => Guid.TryParse(principal.FindFirstValue(SecurityStamp), out var stamp) ? stamp : null;
    }
}
