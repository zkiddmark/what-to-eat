using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using WhatToEatApp.Services.Auth;

namespace WhatToEatApp.Pages
{
    [AllowAnonymous]
    public class RegisterModel : PageModel
    {
        private readonly IUserService _userService;

        public RegisterModel(IUserService userService)
        {
            _userService = userService;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public string? ErrorMessage { get; set; }

        public bool Registered { get; set; }

        public class InputModel
        {
            [Required(ErrorMessage = "Fyll i ett namn.")]
            [StringLength(100, ErrorMessage = "Namnet får vara högst 100 tecken.")]
            [Display(Name = "Namn")]
            public string Alias { get; set; } = string.Empty;

            [Required(ErrorMessage = "Fyll i din e-postadress.")]
            [EmailAddress(ErrorMessage = "Det ser inte ut som en e-postadress.")]
            [Display(Name = "E-post")]
            public string Email { get; set; } = string.Empty;

            [Required(ErrorMessage = "Fyll i ett lösenord.")]
            [StringLength(UserService.MaximumPasswordLength, MinimumLength = UserService.MinimumPasswordLength,
                ErrorMessage = "Lösenordet måste vara minst 12 tecken.")]
            [DataType(DataType.Password)]
            [Display(Name = "Lösenord")]
            public string Password { get; set; } = string.Empty;
        }

        public IActionResult OnGet()
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                return Redirect("~/");
            }
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                Input.Password = string.Empty;
                return Page();
            }

            var result = await _userService.RegisterAsync(Input.Alias, Input.Email, Input.Password);
            Input.Password = string.Empty;

            switch (result)
            {
                case RegisterResult.Success:
                    Registered = true;
                    return Page();
                case RegisterResult.EmailAlreadyRegistered:
                    ErrorMessage = "Det finns redan ett konto med den e-postadressen.";
                    return Page();
                default:
                    ErrorMessage = $"Lösenordet måste vara mellan {UserService.MinimumPasswordLength} " +
                        $"och {UserService.MaximumPasswordLength} tecken.";
                    return Page();
            }
        }
    }
}
