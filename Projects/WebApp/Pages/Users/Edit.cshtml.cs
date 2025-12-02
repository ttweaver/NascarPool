using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using WebApp.Data;
using WebApp.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace WebApp.Pages.Users
{
    public class EditModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        public EditModel(ApplicationDbContext context) => _context = context;

        [BindProperty]
        public ApplicationUser User { get; set; } = default!;

        // options for the driver select lists
        public List<SelectListItem> DriverOptions { get; set; } = new();

        public async Task<IActionResult> OnGetAsync(string id)
        {
            if (string.IsNullOrEmpty(id)) return NotFound();

            User = await _context.Users
                .Include(u => u.PrimaryDriverFirstHalf)
                .Include(u => u.PrimaryDriverSecondHalf)
                .FirstOrDefaultAsync(u => u.Id == id);

            if (User == null) return NotFound();

            await PopulateDriverOptionsAsync();
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            // repopulate selects if we redisplay the page
            await PopulateDriverOptionsAsync();

            ModelState.Remove("User.PrimaryDriverFirstHalf");
            ModelState.Remove("User.PrimaryDriverSecondHalf");
            ModelState.Remove("User.Password");

            if (!ModelState.IsValid) return Page();

            var existing = await _context.Users.FindAsync(User.Id);
            if (existing == null) return NotFound();

            // update only editable fields
            existing.UserName = User.UserName;
            existing.FirstName = User.FirstName;
            existing.LastName = User.LastName;
            existing.IsPlayer = User.IsPlayer;
            existing.Email = User.Email;

            // update primary driver selections (nullable ints)
            existing.PrimaryDriverFirstHalfId = User.PrimaryDriverFirstHalfId;
            existing.PrimaryDriverSecondHalfId = User.PrimaryDriverSecondHalfId;

            _context.Users.Update(existing);
            await _context.SaveChangesAsync();

            return RedirectToPage("Index");
        }

        private async Task PopulateDriverOptionsAsync()
        {
            var drivers = await _context.Drivers
                .OrderBy(d => d.Name)
                .ToListAsync();

            DriverOptions = drivers
                .Select(d => new SelectListItem
                {
                    Value = d.Id.ToString(),
                    Text = $"{d.Name} ({d.CarNumber})"
                })
                .ToList();

            //// allow "none" option
            //DriverOptions.Insert(0, new SelectListItem { Value = string.Empty, Text = "-- None --" });
        }
    }
}