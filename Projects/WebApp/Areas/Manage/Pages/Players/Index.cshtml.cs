using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using WebApp.Data;
using WebApp.Models;

namespace WebApp.Areas.Manage.Pages.Players
{
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<IndexModel> _logger;

        public IndexModel(ApplicationDbContext context, ILogger<IndexModel> logger)
        {
            _context = context;
            _logger = logger;
        }

        public IList<ApplicationUser> Users { get; set; } = default!;

        public async Task OnGetAsync()
        {
            try
            {
                _logger.LogInformation("User {UserId} accessed Users Index page", User.Identity?.Name ?? "Anonymous");
                
                Users = await _context.Users.Players()
                                            .Include(d => d.PrimaryDriverFirstHalf)
                                            .Include(d => d.PrimaryDriverSecondHalf)
                                            .OrderBy(p => p.FirstName).ToListAsync();
                
                _logger.LogInformation("Successfully loaded {Count} users", Users.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading users list");
                throw;
            }
        }
    }
}