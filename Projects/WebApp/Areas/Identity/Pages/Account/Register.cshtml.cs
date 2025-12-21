// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#nullable disable

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading;
using System.Threading.Tasks;
using WebApp.Data;
using WebApp.Models;

namespace WebApp.Areas.Identity.Pages.Account
{
    public class RegisterModel : PageModel
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;
		private readonly ApplicationDbContext _context;
		private readonly IUserStore<ApplicationUser> _userStore;
        private readonly IUserEmailStore<ApplicationUser> _emailStore;
        private readonly ILogger<RegisterModel> _logger;
        private readonly IEmailSender _emailSender;

        public RegisterModel(
            UserManager<ApplicationUser> userManager,
            IUserStore<ApplicationUser> userStore,
            SignInManager<ApplicationUser> signInManager,
            ApplicationDbContext context,
			ILogger<RegisterModel> logger,
            IEmailSender emailSender)
        {
            _userManager = userManager;
            _userStore = userStore;
            _emailStore = GetEmailStore();
            _signInManager = signInManager;
			_context = context;
			_logger = logger;
            _emailSender = emailSender;
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
        public string ReturnUrl { get; set; }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public IList<AuthenticationScheme> ExternalLogins { get; set; }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public class InputModel
        {
			[Required]
			[Display(Name = "First Name")]
			public string FirstName { get; set; } = string.Empty;

			[Required]
			[Display(Name = "Last Name")]
			public string LastName { get; set; } = string.Empty;

			/// <summary>
			///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
			///     directly from your code. This API may change or be removed in future releases.
			/// </summary>
			[Required]
            [EmailAddress]
            [Display(Name = "Email")]
            public string Email { get; set; }

            /// <summary>
            ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
            ///     directly from your code. This API may change or be removed in future releases.
            /// </summary>
            [Required]
            [StringLength(100, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 6)]
            [DataType(DataType.Password)]
            [Display(Name = "Password")]
            public string Password { get; set; }

            /// <summary>
            ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
            ///     directly from your code. This API may change or be removed in future releases.
            /// </summary>
            [DataType(DataType.Password)]
            [Display(Name = "Confirm password")]
            [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
            public string ConfirmPassword { get; set; }
        }


        public async Task OnGetAsync(string returnUrl = null)
        {
            try
            {
                var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
                _logger.LogInformation("Registration page accessed. ReturnUrl: {ReturnUrl}, IP: {IpAddress}", 
                    returnUrl ?? "(none)", ipAddress);

                ReturnUrl = returnUrl;
                ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();

                _logger.LogInformation("Registration page loaded successfully. External logins available: {ExternalLoginCount}, IP: {IpAddress}", 
                    ExternalLogins?.Count ?? 0, ipAddress);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading registration page");
                throw;
            }
        }

        public async Task<IActionResult> OnPostAsync(string returnUrl = null)
        {
            try
            {
                var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
                _logger.LogInformation("Registration attempt started. Email: {Email}, Name: {FirstName} {LastName}, IP: {IpAddress}", 
                    Input?.Email ?? "(none)", Input?.FirstName ?? "(none)", Input?.LastName ?? "(none)", ipAddress);

                returnUrl ??= Url.Content("~/");
                ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();
                
                if (ModelState.IsValid)
                {
                    _logger.LogInformation("Model validation passed. Checking AllowedUsers table for: {Email}, {FirstName} {LastName}, IP: {IpAddress}", 
                        Input.Email, Input.FirstName, Input.LastName, ipAddress);

                    // Check AllowedUsers table for a matching record
                    var allowed = await _context.Set<AllowedUsers>().FirstOrDefaultAsync(u =>
                        u.FirstName.ToLower() == Input.FirstName.ToLower() &&
                        u.LastName.ToLower() == Input.LastName.ToLower() &&
                        u.Email.ToLower() == Input.Email.ToLower()
                    );

                    if (allowed == null)
                    {
                        _logger.LogWarning("Registration denied - user not in AllowedUsers list. Email: {Email}, Name: {FirstName} {LastName}, IP: {IpAddress}", 
                            Input.Email, Input.FirstName, Input.LastName, ipAddress);
                        ModelState.AddModelError(string.Empty, "Registration is not allowed");
                        return Page();
                    }

                    _logger.LogInformation("User found in AllowedUsers list. AllowedUserId: {AllowedUserId}, Email: {Email}, IP: {IpAddress}", 
                        allowed.Id, Input.Email, ipAddress);

                    var user = CreateUser();

                    await _userStore.SetUserNameAsync(user, Input.Email, CancellationToken.None);
                    await _emailStore.SetEmailAsync(user, Input.Email, CancellationToken.None);
                    user.FirstName = Input.FirstName;
                    user.LastName = Input.LastName;

                    _logger.LogInformation("Creating user account for: {Email}, Name: {FirstName} {LastName}, IP: {IpAddress}", 
                        Input.Email, Input.FirstName, Input.LastName, ipAddress);

                    var result = await _userManager.CreateAsync(user, Input.Password);

                    if (result.Succeeded)
                    {
                        _logger.LogInformation("User account created successfully. UserId: {UserId}, Email: {Email}, Name: {FirstName} {LastName}, IP: {IpAddress}", 
                            user.Id, user.Email, user.FirstName, user.LastName, ipAddress);

                        var userId = await _userManager.GetUserIdAsync(user);
                        var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                        code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));
                        
                        var callbackUrl = Url.Page(
                            "/Account/ConfirmEmail",
                            pageHandler: null,
                            values: new { area = "Identity", userId = userId, code = code, returnUrl = returnUrl },
                            protocol: Request.Scheme);

                        _logger.LogInformation("Sending confirmation email to: {Email}, UserId: {UserId}, IP: {IpAddress}", 
                            Input.Email, userId, ipAddress);

                        try
                        {
                            await _emailSender.SendEmailAsync(Input.Email, "Confirm your email",
                                $"Please confirm your account by <a href='{HtmlEncoder.Default.Encode(callbackUrl)}'>clicking here</a>.");

                            _logger.LogInformation("Confirmation email sent successfully to: {Email}, UserId: {UserId}", 
                                Input.Email, userId);
                        }
                        catch (Exception emailEx)
                        {
                            _logger.LogError(emailEx, "Failed to send confirmation email to: {Email}, UserId: {UserId}", 
                                Input.Email, userId);
                            // Continue with registration even if email fails
                        }

                        if (_userManager.Options.SignIn.RequireConfirmedAccount)
                        {
                            _logger.LogInformation("Redirecting to registration confirmation page. Email: {Email}, UserId: {UserId}, IP: {IpAddress}", 
                                Input.Email, userId, ipAddress);
                            return RedirectToPage("RegisterConfirmation", new { email = Input.Email, returnUrl = returnUrl });
                        }
                        else
                        {
                            _logger.LogInformation("Auto-signing in user (confirmed account not required). Email: {Email}, UserId: {UserId}, IP: {IpAddress}", 
                                Input.Email, userId, ipAddress);
                            await _signInManager.SignInAsync(user, isPersistent: false);
                            return LocalRedirect(returnUrl);
                        }
                    }
                    
                    _logger.LogWarning("User creation failed. Email: {Email}, Name: {FirstName} {LastName}, IP: {IpAddress}, Errors: {Errors}", 
                        Input.Email, Input.FirstName, Input.LastName, ipAddress,
                        string.Join(", ", result.Errors.Select(e => e.Description)));

                    foreach (var error in result.Errors)
                    {
                        ModelState.AddModelError(string.Empty, error.Description);
                    }
                }
                else
                {
                    _logger.LogWarning("Registration attempt with invalid model state. Email: {Email}, IP: {IpAddress}, Errors: {Errors}", 
                        Input?.Email ?? "(none)", ipAddress,
                        string.Join(", ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)));
                }

                // If we got this far, something failed, redisplay form
                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during registration attempt. Email: {Email}, Name: {FirstName} {LastName}, IP: {IpAddress}", 
                    Input?.Email ?? "(unknown)", Input?.FirstName ?? "(unknown)", Input?.LastName ?? "(unknown)", 
                    HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown");
                throw;
            }
        }

        private ApplicationUser CreateUser()
        {
            try
            {
                _logger.LogDebug("Creating new ApplicationUser instance");
                return Activator.CreateInstance<ApplicationUser>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create ApplicationUser instance");
                throw new InvalidOperationException($"Can't create an instance of '{nameof(ApplicationUser)}'. " +
                    $"Ensure that '{nameof(ApplicationUser)}' is not an abstract class and has a parameterless constructor, or alternatively " +
                    $"override the register page in /Areas/Identity/Pages/Account/Register.cshtml");
            }
        }

        private IUserEmailStore<ApplicationUser> GetEmailStore()
        {
            if (!_userManager.SupportsUserEmail)
            {
                _logger.LogError("User manager does not support email - email store cannot be created");
                throw new NotSupportedException("The default UI requires a user store with email support.");
            }
            return (IUserEmailStore<ApplicationUser>)_userStore;
        }
    }
}
