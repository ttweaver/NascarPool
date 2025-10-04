using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
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
            Pool = await _context.Pools.FindAsync(id);
            if (Pool == null) return NotFound();
            return Page();
        }
    }
}