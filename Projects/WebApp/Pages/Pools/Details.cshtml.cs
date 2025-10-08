using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using WebApp.Data;
using WebApp.Models;
using System.Threading.Tasks;

namespace WebApp.Pages.Pools
{
    public class DetailsModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        public DetailsModel(ApplicationDbContext context) => _context = context;

        public Pool Pool { get; set; } = default!;

        public async Task<IActionResult> OnGetAsync(int id)
        {
            Pool = await _context.Pools
                .Include(p => p.Members)
                .Include(p => p.Drivers)
                .Include(p => p.Races)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (Pool == null) return NotFound();
            return Page();
        }

        public async Task<IActionResult> OnPostRemoveUserAsync(int id, int userId)
        {
            var pool = await _context.Pools
                .Include(p => p.Members)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (pool == null) return NotFound();

            var user = pool.Members.FirstOrDefault(u => u.Id == userId);
            if (user != null)
            {
                pool.Members.Remove(user);
                await _context.SaveChangesAsync();
            }

            return RedirectToPage(new { id });
        }
    }
}