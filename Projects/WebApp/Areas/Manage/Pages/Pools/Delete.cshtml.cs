using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using WebApp.Data;
using WebApp.Models;
using System.Threading.Tasks;

namespace WebApp.Areas.Manage.Pages.Pools
{
    public class DeleteModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        public DeleteModel(ApplicationDbContext context) => _context = context;

        [BindProperty]
        public Pool Pool { get; set; } = default!;

        public async Task<IActionResult> OnGetAsync(int id)
        {
            Pool = await _context.Pools.FindAsync(id);
            if (Pool == null) return NotFound();
            return Page();
        }

        public async Task<IActionResult> OnPostAsync(int id)
        {
            var pool = await _context.Pools.FindAsync(id);
            if (pool != null)
            {
                _context.Pools.Remove(pool);
                await _context.SaveChangesAsync();
            }
            return RedirectToPage("Index");
        }
    }
}