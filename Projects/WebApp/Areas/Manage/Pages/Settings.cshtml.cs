using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using WebApp.Services;

namespace WebApp.Areas.Manage.Pages
{
    [Authorize(Roles = "Admin")]
    public class SettingsModel : PageModel
    {
        private readonly ISystemSettingsService _settingsService;

        public SettingsModel(ISystemSettingsService settingsService)
        {
            _settingsService = settingsService;
        }

        [BindProperty]
        public bool EnableSms { get; set; }

        [TempData]
        public string? StatusMessage { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            EnableSms = await _settingsService.GetBoolSettingAsync("EnableSms", false);
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            await _settingsService.SetSettingAsync(
                "EnableSms", 
                EnableSms.ToString(), 
                "Enable or disable SMS text messaging system-wide");

            StatusMessage = "Settings saved successfully.";
            return RedirectToPage();
        }
    }
}
