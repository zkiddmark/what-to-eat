using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using WhatToEatApp.Services.Auth;

namespace WhatToEatApp.Pages
{
    [AllowAnonymous]
    public class LoginModel : PageModel
    {
        private readonly IUserService _userService;

        public LoginModel(IUserService userService)
        {
            _userService = userService;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public string? ErrorMessage { get; set; }

        public class InputModel
        {
            [Required(ErrorMessage = "Fyll i din e-postadress.")]
            [EmailAddress(ErrorMessage = "Det ser inte ut som en e-postadress.")]
            [Display(Name = "E-post")]
            public string Email { get; set; } = string.Empty;

            [Required(ErrorMessage = "Fyll i ditt lösenord.")]
            [DataType(DataType.Password)]
            [Display(Name = "Lösenord")]
            public string Password { get; set; } = string.Empty;
        }

        public IActionResult OnGet(string? returnUrl = null)
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                return Redirect("~/");
            }
            return Page();
        }

        public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            var (result, user) = await _userService.ValidateCredentialsAsync(Input.Email, Input.Password);

            if (result != LoginResult.Success || user is null)
            {
                // Lösenordet följer aldrig med tillbaka till formuläret; e-posten gör det.
                Input.Password = string.Empty;
                ErrorMessage = result switch
                {
                    LoginResult.PendingApproval =>
                        "Ditt konto väntar på godkännande. Du kan logga in när det har godkänts.",
                    LoginResult.Rejected =>
                        "Ditt konto har nekats åtkomst. Kontakta den som administrerar appen.",
                    _ => "Fel e-post eller lösenord.",
                };
                return Page();
            }

            var principal = AuthClaims.CreatePrincipal(user, CookieAuthenticationDefaults.AuthenticationScheme);
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
            return Redirect("~/");
        }
    }
}
