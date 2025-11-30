using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using WebApp.Data;
using WebApp.Models;
using System.Threading.Tasks;

namespace WebApp.Pages.Users
{
    public class EditModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        public EditModel(ApplicationDbContext context) => _context = context;

        [BindProperty]
        public ApplicationUser User { get; set; } = default!;

        public async Task<IActionResult> OnGetAsync(string id)
        {
            if (string.IsNullOrEmpty(id)) return NotFound();

            User = await _context.Users.FindAsync(id);
            if (User == null) return NotFound();

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid) return Page();

            var existing = await _context.Users.FindAsync(User.Id);
            if (existing == null) return NotFound();

            // update only the editable fields to avoid accidental overwrites
            existing.UserName = User.UserName;
            existing.FirstName = User.FirstName;
            existing.LastName = User.LastName;
            existing.IsPlayer = User.IsPlayer;
            existing.Email = User.Email;

            _context.Users.Update(existing);
            await _context.SaveChangesAsync();

            return RedirectToPage("Index");
        }
    }
}