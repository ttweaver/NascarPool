using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using WebApp.Data;
using WebApp.Models;
using System.Threading.Tasks;
using System.Linq;

namespace WebApp.Areas.Manage.Pages.Pools
{
    public class EditModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        public EditModel(ApplicationDbContext context) => _context = context;

        [BindProperty]
        public Pool Pool { get; set; } = default!;

        public async Task<IActionResult> OnGetAsync(int id)
        {
            Pool = await _context.Pools.FindAsync(id);
            if (Pool == null) return NotFound();
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid) return Page();

            _context.Attach(Pool).State = EntityState.Modified;
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.Pools.Any(e => e.Id == Pool.Id))
                    return NotFound();
                else
                    throw;
            }
            return RedirectToPage("Index");
        }
    }
}