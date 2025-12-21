using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using WebApp.Data;
using WebApp.Models;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace WebApp.Areas.Manage.Pages.Users
{
    public class DeleteModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<DeleteModel> _logger;

        public DeleteModel(ApplicationDbContext context, ILogger<DeleteModel> logger)
        {
            _context = context;
            _logger = logger;
        }

        [BindProperty]
        public ApplicationUser User { get; set; } = default!;

        public async Task<IActionResult> OnGetAsync(string id)
        {
            try
            {
                if (string.IsNullOrEmpty(id))
                {
                    _logger.LogWarning("Delete user attempted with null/empty ID");
                    return NotFound();
                }

                _logger.LogInformation("User {CurrentUser} accessing delete confirmation for user ID {UserId}", 
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
                _logger.LogError(ex, "Error loading delete confirmation for user ID {UserId}", id);
                throw;
            }
        }

        public async Task<IActionResult> OnPostAsync(string id)
        {
            try
            {
                var user = await _context.Users.FindAsync(id);
                if (user != null)
                {
                    var userEmail = user.Email;
                    var userName = $"{user.FirstName} {user.LastName}";
                    
                    _context.Users.Remove(user);
                    await _context.SaveChangesAsync();

                    _logger.LogWarning("User deleted. UserId: {UserId}, Email: {Email}, Name: {Name}, DeletedBy: {DeletedBy}",
                        id, userEmail, userName, User?.UserName ?? "Anonymous");
                }
                else
                {
                    _logger.LogWarning("Attempted to delete non-existent user ID {UserId}", id);
                }

                return RedirectToPage("Index");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting user ID {UserId}", id);
                throw;
            }
        }
    }
}