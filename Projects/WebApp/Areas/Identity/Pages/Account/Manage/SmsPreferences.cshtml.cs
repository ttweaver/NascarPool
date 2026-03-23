using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using WebApp.Models;

namespace WebApp.Areas.Identity.Pages.Account.Manage
{
    public class SmsPreferencesModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;

        public SmsPreferencesModel(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
        }

        [TempData]
        public string? StatusMessage { get; set; }

        [BindProperty]
        public InputModel Input { get; set; } = new InputModel();
        
        public DateTime? SmsOptInDate { get; set; }

        public class InputModel
        {
            [Phone]
            [Display(Name = "Phone Number")]
            public string? PhoneNumber { get; set; }

            [Display(Name = "SMS Opt-In")]
            public bool SmsOptIn { get; set; }
        }

        private async Task LoadAsync(ApplicationUser user)
        {
            var phoneNumber = await _userManager.GetPhoneNumberAsync(user);
            
            Input = new InputModel
            {
                PhoneNumber = phoneNumber,
                SmsOptIn = user.SmsOptIn
            };
            
            SmsOptInDate = user.SmsOptInDate;
        }

        public async Task<IActionResult> OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }

            await LoadAsync(user);
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }

            if (!ModelState.IsValid)
            {
                await LoadAsync(user);
                return Page();
            }

            var phoneNumber = await _userManager.GetPhoneNumberAsync(user);
            if (Input.PhoneNumber != phoneNumber)
            {
                var setPhoneResult = await _userManager.SetPhoneNumberAsync(user, Input.PhoneNumber);
                if (!setPhoneResult.Succeeded)
                {
                    StatusMessage = "Error: Failed to set phone number.";
                    return RedirectToPage();
                }
            }

            // Update SMS opt-in preference
            var previousOptIn = user.SmsOptIn;
            user.SmsOptIn = Input.SmsOptIn;
            
            // Update opt-in date if they're opting in for the first time or re-opting in
            if (Input.SmsOptIn && (!previousOptIn || !user.SmsOptInDate.HasValue))
            {
                user.SmsOptInDate = DateTime.UtcNow;
            }
            // Clear opt-in date if they're opting out
            else if (!Input.SmsOptIn && previousOptIn)
            {
                user.SmsOptInDate = null;
            }

            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                StatusMessage = "Error: Failed to update SMS preferences.";
                return RedirectToPage();
            }

            await _signInManager.RefreshSignInAsync(user);
            StatusMessage = "Your SMS preferences have been updated.";
            return RedirectToPage();
        }
    }
}
