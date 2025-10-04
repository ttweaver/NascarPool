using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using WebApp.Data;
using WebApp.Models;

namespace WebApp.Pages.Users
{
    public class DetailsModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        public DetailsModel(ApplicationDbContext context) => _context = context;

        public User User { get; set; } = default!;

        public async Task<IActionResult> OnGetAsync(int id)
        {
            User = await _context.Users.FindAsync(id);
            if (User == null) return NotFound();
            return Page();
        }
    }
}