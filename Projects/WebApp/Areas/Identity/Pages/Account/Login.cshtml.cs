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
            try
            {
                _logger.LogInformation("Login page accessed. ReturnUrl: {ReturnUrl}, IP: {IpAddress}", 
                    returnUrl ?? "(none)", HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown");

                if (!string.IsNullOrEmpty(ErrorMessage))
                {
                    _logger.LogWarning("Login page accessed with error message: {ErrorMessage}", ErrorMessage);
                    ModelState.AddModelError(string.Empty, ErrorMessage);
                }

                returnUrl ??= Url.Content("~/");

                await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);
                ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();
                ReturnUrl = returnUrl;

                _logger.LogInformation("Login page loaded successfully. External logins available: {ExternalLoginCount}", 
                    ExternalLogins?.Count ?? 0);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading login page");
                throw;
            }
        }

		public async Task<IActionResult> OnPostMakeAssertionOptionsAsync(string? username)
		{
            try
            {
                _logger.LogInformation("Passkey assertion options requested. Username: {Username}, IP: {IpAddress}", 
                    username ?? "(discoverable)", HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown");

                await _antiforgery.ValidateRequestAsync(HttpContext);

                var user = string.IsNullOrEmpty(username) ? null : await _userManager.FindByNameAsync(username);
                
                if (user != null)
                {
                    _logger.LogInformation("User found for passkey assertion. UserId: {UserId}, Email: {Email}", 
                        user.Id, user.Email);
                }
                else
                {
                    _logger.LogInformation("No user specified or found for passkey assertion - using discoverable credentials");
                }

                var optionsString = await _signInManager.MakePasskeyRequestOptionsAsync(user);

                _logger.LogInformation("Passkey assertion options generated successfully");
                return Content(optionsString, "application/json");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating passkey assertion options for username: {Username}", username);
                throw;
            }
		}

		public async Task<IActionResult> OnPostAsync(string returnUrl = null)
        {
            try
            {
                var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
                _logger.LogInformation("Login attempt started. ReturnUrl: {ReturnUrl}, IP: {IpAddress}", 
                    returnUrl ?? "~/Dashboard", ipAddress);

                returnUrl ??= Url.Content("~/Dashboard");

                ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();

                // Determine if this is a passkey or password login
                bool isPasskeyLogin = !string.IsNullOrEmpty(Input.Passkey?.CredentialJson);

                _logger.LogInformation("Login type: {LoginType}, IP: {IpAddress}", 
                    isPasskeyLogin ? "Passkey" : "Password", ipAddress);

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
                        _logger.LogWarning("Passkey login failed with error: {Error}, IP: {IpAddress}", 
                            Input.Passkey?.Error, ipAddress);
                        ModelState.AddModelError(Input.Passkey?.Error, Input.Passkey?.Error);
                        return Page();
                    }

                    Microsoft.AspNetCore.Identity.SignInResult result;
                    string attemptedEmail = null;

                    if (isPasskeyLogin)
                    {
                        _logger.LogInformation("Attempting passkey sign-in. IP: {IpAddress}", ipAddress);
                        result = await _signInManager.PasskeySignInAsync(Input.Passkey.CredentialJson);
                    }
                    else
                    {
                        attemptedEmail = Input.Email;
                        _logger.LogInformation("Attempting password sign-in for email: {Email}, RememberMe: {RememberMe}, IP: {IpAddress}", 
                            Input.Email, Input.RememberMe, ipAddress);
                        result = await _signInManager.PasswordSignInAsync(Input.Email, Input.Password, Input.RememberMe, lockoutOnFailure: false);
                    }

                    if (result.Succeeded)
                    {
                        var userManager = _signInManager.UserManager;
                        var user = isPasskeyLogin ? await userManager.GetUserAsync(User) : await userManager.FindByNameAsync(Input.Email);
                        
                        if (user != null)
                        {
                            _logger.LogInformation("User logged in successfully. UserId: {UserId}, Email: {Email}, LoginType: {LoginType}, IP: {IpAddress}", 
                                user.Id, user.Email, isPasskeyLogin ? "Passkey" : "Password", ipAddress);

                            var isAdmin = await userManager.IsInRoleAsync(user, "Admin");
                            var is2faEnabled = await userManager.GetTwoFactorEnabledAsync(user);

                            if (isAdmin && !is2faEnabled)
                            {
                                _logger.LogWarning("Admin user {UserId} ({Email}) logged in without 2FA enabled - redirecting to enable authenticator", 
                                    user.Id, user.Email);
                                return RedirectToPage("/Account/Manage/EnableAuthenticator");
                            }
                        }
                        else
                        {
                            _logger.LogWarning("Login succeeded but user object is null. LoginType: {LoginType}, IP: {IpAddress}", 
                                isPasskeyLogin ? "Passkey" : "Password", ipAddress);
                        }

                        return LocalRedirect(returnUrl);
                    }

                    if (result.RequiresTwoFactor)
                    {
                        _logger.LogInformation("Login requires two-factor authentication. Email: {Email}, IP: {IpAddress}", 
                            attemptedEmail ?? "(passkey)", ipAddress);
                        return RedirectToPage("./LoginWith2fa", new { ReturnUrl = returnUrl, RememberMe = Input.RememberMe });
                    }
                    
                    if (result.IsLockedOut)
                    {
                        _logger.LogWarning("User account locked out. Email: {Email}, LoginType: {LoginType}, IP: {IpAddress}", 
                            attemptedEmail ?? "(passkey)", isPasskeyLogin ? "Passkey" : "Password", ipAddress);
                        return RedirectToPage("./Lockout");
                    }
                    else
                    {
                        _logger.LogWarning("Invalid login attempt. Email: {Email}, LoginType: {LoginType}, IP: {IpAddress}", 
                            attemptedEmail ?? "(passkey)", isPasskeyLogin ? "Passkey" : "Password", ipAddress);
                        ModelState.AddModelError(string.Empty, "Invalid login attempt.");
                        
                        if (isPasskeyLogin)
                        {
                            Input.Passkey.CredentialJson = null;
                            ModelState.Remove("Input.Passkey.CredentialJson");
                        }
                        
                        return Page();
                    }
                }
                else
                {
                    _logger.LogWarning("Login attempt with invalid model state. Email: {Email}, LoginType: {LoginType}, IP: {IpAddress}, Errors: {Errors}", 
                        Input?.Email ?? "(passkey)", isPasskeyLogin ? "Passkey" : "Password", ipAddress,
                        string.Join(", ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)));
                }

                // If we got this far, something failed, redisplay form
                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during login attempt. Email: {Email}, IP: {IpAddress}", 
                    Input?.Email ?? "(unknown)", HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown");
                throw;
            }
        }
	}
}
