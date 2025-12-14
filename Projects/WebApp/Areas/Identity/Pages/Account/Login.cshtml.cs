// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#nullable disable

using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using WebApp.Models;

namespace WebApp.Areas.Identity.Pages.Account
{
    public class LoginModel : PageModel
    {
		private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ILogger<LoginModel> _logger;
        private readonly IAntiforgery _antiforgery;
        private readonly UserManager<ApplicationUser> _userManager;

        public LoginModel(
            SignInManager<ApplicationUser> signInManager,
            ILogger<LoginModel> logger,
            IAntiforgery antiforgery,
            UserManager<ApplicationUser> userManager)
        {
            _signInManager = signInManager;
            _logger = logger;
            _antiforgery = antiforgery;
            _userManager = userManager;
        }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        [BindProperty]
        public InputModel Input { get; set; }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public IList<AuthenticationScheme> ExternalLogins { get; set; }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public string ReturnUrl { get; set; }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        [TempData]
        public string ErrorMessage { get; set; }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public class InputModel
        {
            /// <summary>
            ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
            ///     directly from your code. This API may change or be removed in future releases.
            /// </summary>
            [EmailAddress]
            public string Email { get; set; }

            /// <summary>
            ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
            ///     directly from your code. This API may change or be removed in future releases.
            /// </summary>
            [DataType(DataType.Password)]
            public string Password { get; set; }

            /// <summary>
            ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
            ///     directly from your code. This API may change or be removed in future releases.
            /// </summary>
            [Display(Name = "Remember me?")]
            public bool RememberMe { get; set; }

			public PasskeyInputModel? Passkey { get; set; }
		}

		public class PasskeyInputModel
		{
			public string? CredentialJson { get; set; }
			public string? Error { get; set; }
		}

		public async Task OnGetAsync(string returnUrl = null)
        {
            if (!string.IsNullOrEmpty(ErrorMessage))
            {
                ModelState.AddModelError(string.Empty, ErrorMessage);
            }

            returnUrl ??= Url.Content("~/");

            await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);
            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();
            ReturnUrl = returnUrl;
        }
		public async Task<IActionResult> OnPostMakeAssertionOptionsAsync(string? username)
		{
			await _antiforgery.ValidateRequestAsync(HttpContext);

			var user = string.IsNullOrEmpty(username) ? null : await _userManager.FindByNameAsync(username);
			var optionsString = await _signInManager.MakePasskeyRequestOptionsAsync(user);

			return Content(optionsString, "application/json");
		}

		public async Task<IActionResult> OnPostAsync(string returnUrl = null)
        {
            returnUrl ??= Url.Content("~/Dashboard");

            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();

			// Determine if this is a passkey or password login
			bool isPasskeyLogin = !string.IsNullOrEmpty(Input.Passkey?.CredentialJson);

			// Add conditional validation for email/password when NOT using passkey
			if (!isPasskeyLogin)
			{
				if (string.IsNullOrEmpty(Input.Email))
				{
					ModelState.AddModelError(nameof(Input.Email), "The Email field is required.");
				}
				if (string.IsNullOrEmpty(Input.Password))
				{
					ModelState.AddModelError(nameof(Input.Password), "The Password field is required.");
				}
			}

            if (ModelState.IsValid)
            {
				if (!string.IsNullOrEmpty(Input.Passkey?.Error))
				{
                    ModelState.AddModelError(Input.Passkey?.Error, Input.Passkey?.Error);
                    return Page();
				}

				Microsoft.AspNetCore.Identity.SignInResult result;
				if (isPasskeyLogin)
				{
					result = await _signInManager.PasskeySignInAsync(Input.Passkey.CredentialJson);
				}
				else
				{
					result = await _signInManager.PasswordSignInAsync(Input.Email, Input.Password, Input.RememberMe, lockoutOnFailure: false);
                }

				if (result.Succeeded)
				{
					var userManager = _signInManager.UserManager;
					var user = isPasskeyLogin ? await userManager.GetUserAsync(User) : await userManager.FindByNameAsync(Input.Email);
					if (user != null)
					{
						var isAdmin = await userManager.IsInRoleAsync(user, "Admin");
						var is2faEnabled = await userManager.GetTwoFactorEnabledAsync(user);

						if (isAdmin && !is2faEnabled)
						{
							return RedirectToPage("/Account/Manage/EnableAuthenticator");
						}
					}
					_logger.LogInformation("User logged in.");
					return LocalRedirect(returnUrl);
				}

				if (result.RequiresTwoFactor)
                {
                    return RedirectToPage("./LoginWith2fa", new { ReturnUrl = returnUrl, RememberMe = Input.RememberMe });
                }
                if (result.IsLockedOut)
                {
                    _logger.LogWarning("User account locked out.");
                    return RedirectToPage("./Lockout");
                }
                else
                {
                    ModelState.AddModelError(string.Empty, "Invalid login attempt.");
					Input.Passkey.CredentialJson = null;
					ModelState.Remove("Input.Passkey.CredentialJson");
					return Page();
                }
            }

            // If we got this far, something failed, redisplay form
            return Page();
        }
	}
}
