using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using WebApp.Data;
using WebApp.Models;
using Microsoft.Extensions.Logging;

namespace WebApp.Areas.Manage.Pages.Players
{
    public class DetailsModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<DetailsModel> _logger;

        public DetailsModel(ApplicationDbContext context, ILogger<DetailsModel> logger)
        {
            _context = context;
            _logger = logger;
        }

        public ApplicationUser User { get; set; } = default!;

        public async Task<IActionResult> OnGetAsync(string id)
        {
            try
            {
                if (string.IsNullOrEmpty(id))
                {
                    _logger.LogWarning("View user details attempted with null/empty ID");
                    return NotFound();
                }

                _logger.LogInformation("User {CurrentUser} viewing details for user ID {UserId}", 
                    User?.UserName ?? "Anonymous", id);

                User = await _context.Users.FindAsync(id);
                if (User == null)
                {
                    _logger.LogWarning("User not found with ID {UserId}", id);
                    return NotFound();
                }

                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading details for user ID {UserId}", id);
                throw;
            }
        }
    }
}