using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using WebApp.Data;
using WebApp.Models;
using System.Threading.Tasks;

namespace WebApp.Pages.Users
{
    public class DeleteModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        public DeleteModel(ApplicationDbContext context) => _context = context;

        [BindProperty]
        public ApplicationUser User { get; set; } = default!;

        public async Task<IActionResult> OnGetAsync(string id)
        {
            User = await _context.Users.FindAsync(id);
            if (User == null) return NotFound();
            return Page();
        }

        // changed parameter type to string to match Identity user key
        public async Task<IActionResult> OnPostAsync(string id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user != null)
            {
                _context.Users.Remove(user);
                await _context.SaveChangesAsync();
            }
            return RedirectToPage("Index");
        }
    }
}