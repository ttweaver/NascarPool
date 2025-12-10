using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Buffers.Text;
using System.ComponentModel.DataAnnotations;
using WebApp.Models;

namespace WebApp.Areas.Identity.Pages.Account.Manage
{
    public class RenamePasskeyModel : PageModel
    {
		private ApplicationUser? user;
		public UserPasskeyInfo? Passkey { get; set; }
		private readonly UserManager<ApplicationUser> _userManager;

		[BindProperty]
		public string? Id { get; set; }

		[BindProperty(SupportsGet = true)]
		public InputModel Input { get; set; } = default!;

		public RenamePasskeyModel(UserManager<ApplicationUser> userManager)
		{
			_userManager = userManager;
		}
		
		public async Task<IActionResult> OnGetAsync(string? id)
		{
			Id = id;
			Input ??= new ();
			return Page();
		}
		public async Task<IActionResult> OnPostAsync(string? id, string name)
		{
			Input ??= new();

			Id = id;

			user = (await _userManager.GetUserAsync(User));
			if (user == null)
			{
				return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
			}

			byte[] credentialId;
			try
			{
				credentialId = Base64Url.DecodeFromChars(Id);
			}
			catch (FormatException)
			{
				return RedirectToPage("./PasskeyAuthentication");
			}

			Passkey = await _userManager.GetPasskeyAsync(user, credentialId);
			if (Passkey is null)
			{
				return RedirectToPage("./PasskeyAuthentication");
			}

			Passkey!.Name = Input.Name;
			var result = await _userManager.AddOrUpdatePasskeyAsync(user!, Passkey);
			if (!result.Succeeded)
			{
				return RedirectToPage("./RenamePasskey");
			}

			return RedirectToPage("./PasskeyAuthentication");
		}


		public sealed class InputModel
		{
			[Required]
			[StringLength(200, ErrorMessage = "Passkey names must be no longer than {1} characters.")]
			public string Name { get; set; } = "";
		}
	}
}
