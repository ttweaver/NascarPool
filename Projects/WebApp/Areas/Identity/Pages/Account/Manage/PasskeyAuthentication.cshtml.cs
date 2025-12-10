using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Buffers.Text;
using WebApp.Data.Migrations;
using WebApp.Models;
using static WebApp.Areas.Identity.Pages.Account.LoginModel;

namespace WebApp.Areas.Identity.Pages.Account.Manage
{
	public class PasskeyAuthenticationModel : PageModel
	{
		public int MaxPasskeyCount { get; } = 100;

		public IList<UserPasskeyInfo>? CurrentPasskeys { get; set; }

		private readonly UserManager<ApplicationUser> _userManager;
		private readonly SignInManager<ApplicationUser> _signInManager;
		private readonly IUserStore<ApplicationUser> _userStore;
		private readonly IAntiforgery _antiforgery;

		[BindProperty]
		public string? Action { get; set; }

		[BindProperty]
		public string? CredentialId { get; set; }

		[TempData]
		public string StatusMessage { get; set; }

		public PasskeyAuthenticationModel(
			UserManager<ApplicationUser> userManager,
			SignInManager<ApplicationUser> signInManager,
			IUserStore<ApplicationUser> userStore,
			IAntiforgery antiforgery)
		{
			_userManager = userManager;
			_signInManager = signInManager;
			_userStore = userStore;
			_antiforgery = antiforgery;
		}

		public async Task<IActionResult> OnGet()
		{
			var user = await _userManager.GetUserAsync(User);
			if (user == null)
			{
				return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
			}

			if (_userStore is IUserPasskeyStore<ApplicationUser> userPasswordStore)
			{
				CurrentPasskeys = await userPasswordStore.GetPasskeysAsync(user, CancellationToken.None);
			}
			return Page();
		}

		public async Task<IActionResult> OnPostPasskeyCreationOptionsAsync()
		{
			await _antiforgery.ValidateRequestAsync(HttpContext);

			var user = await _userManager.GetUserAsync(User);
			if (user is null)
			{
				return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
			}

			var userId = await _userManager.GetUserIdAsync(user);
			var userName = await _userManager.GetUserNameAsync(user) ?? "User";
			var optionsString = await _signInManager.MakePasskeyCreationOptionsAsync(new()
			{
				Id = userId,
				Name = userName,
				DisplayName = userName
			});

			return Content(optionsString, "application/json");
		}

		public async Task<IActionResult> OnPostRegisterPasskeyAsync([FromBody] PasskeyInputModel input)
		{
			// Validate the antiforgery token for AJAX JSON POSTs that include the header
			await _antiforgery.ValidateRequestAsync(HttpContext);

			if (input == null)
			{
				return BadRequest("Invalid passkey payload.");
			}

			var user = await _userManager.GetUserAsync(User);
			if (user == null)
			{
				return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
			}

			if (!string.IsNullOrEmpty(input.Error))
			{
				StatusMessage = input.Error;
				return RedirectToPage();
			}

			if (string.IsNullOrEmpty(input.CredentialJson))
			{
				input.Error = "Error: The browser did not provide a passkey.";
				StatusMessage = input.Error;
				return RedirectToPage();
			}

			if (_userStore is IUserPasskeyStore<ApplicationUser> userPasswordStore)
			{
				CurrentPasskeys = await userPasswordStore.GetPasskeysAsync(user, CancellationToken.None);
			}

			if (CurrentPasskeys!.Count >= MaxPasskeyCount)
			{
				input.Error = $"Error: You have reached the maximum number of allowed passkeys ({MaxPasskeyCount}).";
				return RedirectToPage();
			}

			var attestationResult = await _signInManager.PerformPasskeyAttestationAsync(input.CredentialJson);
			if (!attestationResult.Succeeded)
			{
				input.Error = $"Error: Could not add the passkey: {attestationResult.Failure.Message}";
				return RedirectToPage();
			}

			var addPasskeyResult = await _userManager.AddOrUpdatePasskeyAsync(user, attestationResult.Passkey);
			if (!addPasskeyResult.Succeeded)
			{
				input.Error = "Error: The passkey could not be added to your account.";
				return RedirectToPage();
			}

			// Immediately prompt the user to enter a name for the credential
			string credentialIdBase64Url = Base64Url.EncodeToString(attestationResult.Passkey.CredentialId);
			return RedirectToPage("./RenamePasskey", new { id = credentialIdBase64Url });
		}

		public async Task<IActionResult> OnPostUpdatePasskeyAsync()
		{
			var user = (await _userManager.GetUserAsync(User));
			byte[] credentialId;
			try
			{
				credentialId = Base64Url.DecodeFromChars(CredentialId);
			}
			catch (FormatException)
			{
				return RedirectToPage("./PasskeyAuthentication");
			}
			var passkey = await _userManager.GetPasskeyAsync(user, credentialId);

			switch (Action)
			{
				case "rename":
					return RedirectToPage("./RenamePasskey", new { id = CredentialId, name = passkey?.Name });
				case "delete":
					// Return the delete handler's result so the redirect is honored and no query string is left behind.
					return await OnPostDeletePasskeyAsync();
				default:
					return RedirectToPage("./PasskeyAuthentication");
			}
		}

		public async Task<IActionResult> OnPostDeletePasskeyAsync()
		{
			var user = await _userManager.GetUserAsync(User);
			if (user == null)
			{
				return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
			}
			if (_userStore is IUserPasskeyStore<ApplicationUser> userPasswordStore)
			{
				CurrentPasskeys = await userPasswordStore.GetPasskeysAsync(user, CancellationToken.None);
			}

			byte[] credentialId;
			try
			{
				credentialId = Base64Url.DecodeFromChars(CredentialId);
			}
			catch (FormatException)
			{
				StatusMessage = "Error: The specified passkey ID had an invalid format.";
				return Page();
			}

			var result = await _userManager.RemovePasskeyAsync(user, credentialId);
			if (!result.Succeeded)
			{
				StatusMessage = "Error: The passkey could not be deleted.";
				return Page();
			}

			if (_userStore is IUserPasskeyStore<ApplicationUser> userPasswordStore2)
			{
				CurrentPasskeys = await userPasswordStore2.GetPasskeysAsync(user, CancellationToken.None);
			}

			StatusMessage = "Passkey deleted successfully.";
			// Use the reserved "area" route value key (lowercase) so it is used for routing
			// instead of being added as a query string parameter.
			return RedirectToPage("/Account/Manage/PasskeyAuthentication", new { area = "Identity" });
		}
	}
}
