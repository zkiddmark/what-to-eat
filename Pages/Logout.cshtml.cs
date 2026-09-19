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

        public async Task<IActionResult> OnGetAsync() => await SignOutAndRedirectAsync();

        public async Task<IActionResult> OnPostAsync() => await SignOutAndRedirectAsync();

        private async Task<IActionResult> SignOutAndRedirectAsync()
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
