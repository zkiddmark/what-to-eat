using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using WhatToEatApp.Services.Auth;

namespace WhatToEatApp.Pages
{
    /// <summary>
    /// Razor Page, inte Blazor-komponent: ett lyckat byte roterar SecurityStamp och den egna
    /// cookien måste skrivas om i samma svar för att sessionen ska överleva. En Blazor-krets
    /// kan inte sätta cookies.
    /// </summary>
    [Authorize]
    public class AccountModel : PageModel
    {
        private readonly IUserService _userService;

        public AccountModel(IUserService userService)
        {
            _userService = userService;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public string? ErrorMessage { get; set; }

        public bool PasswordChanged { get; set; }

        public string Alias => User.FindFirstValue(ClaimTypes.Name) ?? string.Empty;

        public string Email => User.FindFirstValue(ClaimTypes.Email) ?? string.Empty;

        public class InputModel
        {
            [Required(ErrorMessage = "Fyll i ditt nuvarande lösenord.")]
            [DataType(DataType.Password)]
            [Display(Name = "Nuvarande lösenord")]
            public string CurrentPassword { get; set; } = string.Empty;

            [Required(ErrorMessage = "Fyll i ett nytt lösenord.")]
            [StringLength(UserService.MaximumPasswordLength, MinimumLength = UserService.MinimumPasswordLength,
                ErrorMessage = "Det nya lösenordet måste vara minst 12 tecken.")]
            [DataType(DataType.Password)]
            [Display(Name = "Nytt lösenord")]
            public string NewPassword { get; set; } = string.Empty;

            [Required(ErrorMessage = "Upprepa det nya lösenordet.")]
            [Compare(nameof(NewPassword), ErrorMessage = "Lösenorden stämmer inte överens.")]
            [DataType(DataType.Password)]
            [Display(Name = "Upprepa nytt lösenord")]
            public string ConfirmPassword { get; set; } = string.Empty;
        }

        public void OnGet()
        {
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return ClearedPage();
            }

            var userId = AuthClaims.GetUserId(User);
            if (userId is null)
            {
                ErrorMessage = "Något gick fel. Försök igen.";
                return ClearedPage();
            }

            var result = await _userService.ChangePasswordAsync(
                userId.Value, Input.CurrentPassword, Input.NewPassword);

            switch (result)
            {
                case ChangePasswordResult.Success:
                    break;
                case ChangePasswordResult.InvalidCurrentPassword:
                    ErrorMessage = "Fel nuvarande lösenord.";
                    return ClearedPage();
                case ChangePasswordResult.PasswordLengthInvalid:
                    ErrorMessage = $"Det nya lösenordet måste vara mellan {UserService.MinimumPasswordLength} " +
                        $"och {UserService.MaximumPasswordLength} tecken.";
                    return ClearedPage();
                default:
                    ErrorMessage = "Något gick fel och lösenordet byttes inte. Försök igen.";
                    return ClearedPage();
            }

            // Stämpeln är roterad — den gamla cookien är därmed död vid nästa begäran. Skriv
            // en ny med den nya stämpeln så att den här webbläsaren är kvar inloggad medan
            // övriga faller.
            var refreshed = await _userService.GetByIdAsync(userId.Value);
            if (refreshed is not null)
            {
                var principal = AuthClaims.CreatePrincipal(
                    refreshed, CookieAuthenticationDefaults.AuthenticationScheme);
                await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);
            }

            PasswordChanged = true;
            return ClearedPage();
        }

        /// <summary>
        /// Lösenordsfälten följer aldrig med tillbaka till formuläret. Valideringsfelen gör
        /// det däremot — de ska stå kvar vid sitt fält.
        /// </summary>
        private PageResult ClearedPage()
        {
            Input.CurrentPassword = string.Empty;
            Input.NewPassword = string.Empty;
            Input.ConfirmPassword = string.Empty;
            return Page();
        }
    }
}
