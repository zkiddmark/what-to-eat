using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using WhatToEatApp.Services.Auth;

namespace WhatToEatApp.Pages
{
    /// <summary>
    /// Enda sidan ett konto på tillfälligt lösenord kommer åt. Det tillfälliga lösenordet
    /// efterfrågas inte igen: användaren kom in på det nyss och sidan nås bara med giltig
    /// cookie.
    /// </summary>
    [Authorize]
    public class ForcedPasswordChangeModel : PageModel
    {
        private readonly IUserService _userService;

        public ForcedPasswordChangeModel(IUserService userService)
        {
            _userService = userService;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public string? ErrorMessage { get; set; }

        public class InputModel
        {
            [Required(ErrorMessage = "Fyll i ett nytt lösenord.")]
            // Se Register.cshtml.cs: längdregeln uttrycks bara av tjänsten.
            [StringLength(UserService.MaximumPasswordLength)]
            [DataType(DataType.Password)]
            [Display(Name = "Nytt lösenord")]
            public string NewPassword { get; set; } = string.Empty;

            [Required(ErrorMessage = "Upprepa det nya lösenordet.")]
            [Compare(nameof(NewPassword), ErrorMessage = "Lösenorden stämmer inte överens.")]
            [DataType(DataType.Password)]
            [Display(Name = "Upprepa nytt lösenord")]
            public string ConfirmPassword { get; set; } = string.Empty;
        }

        public IActionResult OnGet()
        {
            // Den som inte står på ett tillfälligt lösenord har inget här att göra.
            if (!AuthClaims.RequiresPasswordChange(User))
            {
                return Redirect("~/");
            }
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!AuthClaims.RequiresPasswordChange(User))
            {
                return Redirect("~/");
            }

            if (!ModelState.IsValid)
            {
                return ClearedPage();
            }

            var userId = AuthClaims.GetUserId(User);
            if (userId is null)
            {
                ErrorMessage = "Något gick fel. Logga in igen.";
                return ClearedPage();
            }

            var result = await _userService.CompleteForcedChangeAsync(userId.Value, Input.NewPassword);
            if (result != ForcedChangeResult.Success)
            {
                if (result == ForcedChangeResult.PasswordLengthInvalid)
                {
                    ModelState.AddModelError("Input.NewPassword",
                        $"Det nya lösenordet måste vara mellan {UserService.MinimumPasswordLength} " +
                        $"och {UserService.MaximumPasswordLength} tecken.");
                    return ClearedPage();
                }

                ErrorMessage = result switch
                {
                    ForcedChangeResult.Expired =>
                        "Det tillfälliga lösenordet har gått ut. Be en administratör sätta ett nytt.",
                    _ => "Något gick fel och lösenordet byttes inte. Försök igen.",
                };
                return ClearedPage();
            }

            // Ny cookie utan claimet — användaren fortsätter inloggad, utan ny inloggning.
            var user = await _userService.GetByIdAsync(userId.Value);
            if (user is not null)
            {
                var principal = AuthClaims.CreatePrincipal(
                    user, CookieAuthenticationDefaults.AuthenticationScheme);
                await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);
            }

            return Redirect("~/");
        }

        private PageResult ClearedPage()
        {
            Input.NewPassword = string.Empty;
            Input.ConfirmPassword = string.Empty;
            return Page();
        }
    }
}
