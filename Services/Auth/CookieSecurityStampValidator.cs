using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace WhatToEatApp.Services.Auth
{
    /// <summary>
    /// Cookie-auth är självbärande: att radera kakan hos klienten räcker inte, en sparad kopia
    /// skulle fortsätta gälla. Därför jämförs stämpeln i cookien mot den i databasen vid varje
    /// begäran, och utloggning roterar stämpeln.
    /// </summary>
    public static class CookieSecurityStampValidator
    {
        public static async Task ValidateAsync(CookieValidatePrincipalContext context)
        {
            var principal = context.Principal;
            var userId = principal is null ? null : AuthClaims.GetUserId(principal);
            var stamp = principal is null ? null : AuthClaims.GetSecurityStamp(principal);

            if (userId is null || stamp is null)
            {
                await RejectAsync(context);
                return;
            }

            var userService = context.HttpContext.RequestServices.GetRequiredService<IUserService>();
            var user = await userService.FindBySecurityStampAsync(userId.Value, stamp.Value);
            if (user is null)
            {
                await RejectAsync(context);
            }
        }

        private static async Task RejectAsync(CookieValidatePrincipalContext context)
        {
            context.RejectPrincipal();
            await context.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        }
    }
}
