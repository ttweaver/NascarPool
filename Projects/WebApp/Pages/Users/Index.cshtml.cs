using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using WebApp.Data;
using WebApp.Models;

namespace WebApp.Pages.Users
{
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        public IndexModel(ApplicationDbContext context) => _context = context;

        public IList<ApplicationUser> Users { get; set; } = default!;

        public async Task OnGetAsync()
        {
            Users = await _context.Users.Players()
                                        .Include(d => d.PrimaryDriverFirstHalf)
                                        .Include(d => d.PrimaryDriverSecondHalf)
                                        .OrderBy(p => p.FirstName).ToListAsync();
        }
    }
}