using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using WhatToEatApp.Services.Auth;

namespace WhatToEatApp.Pages
{
    public class LogoutModel : PageModel
    {
        private readonly IUserService _userService;

        public LogoutModel(IUserService userService)
        {
            _userService = userService;
        }

        /// <summary>
        /// Utloggningen ligger bara på POST. En GET som muterar tillstånd går att utlösa från
        /// en annan sajt, och rotationen av SecurityStamp släcker användarens sessioner
        /// överallt — inte bara i den här webbläsaren.
        /// </summary>
        public IActionResult OnGet() => Redirect("~/");

        public async Task<IActionResult> OnPostAsync()
        {
            // Stämpeln roteras så att en sparad kopia av den gamla cookien slutar gälla.
            var userId = AuthClaims.GetUserId(User);
            if (userId is not null)
            {
                await _userService.RotateSecurityStampAsync(userId.Value);
            }

            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return Redirect("~/login");
        }
    }
}
