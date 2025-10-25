using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using WebApp.Data;
using WebApp.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WebApp.Pages.Pools
{
    public class AddUsersModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        public AddUsersModel(ApplicationDbContext context) => _context = context;

        [BindProperty]
        public int PoolId { get; set; }

        [BindProperty]
        public List<string> SelectedUserIds { get; set; } = new();

        public List<Pool> Pools { get; set; } = new();
        public List<ApplicationUser> Users { get; set; } = new();

        public async Task OnGetAsync()
        {
            Pools = await _context.Pools.ToListAsync();
            Users = await _context.Users.ToListAsync();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (SelectedUserIds.Count == 0)
            {
                ModelState.AddModelError(string.Empty, "Please select at least one user.");
                Pools = await _context.Pools.ToListAsync();
                Users = await _context.Users.ToListAsync();
                return Page();
            }

            var pool = await _context.Pools
                .Include(p => p.Members)
                .FirstOrDefaultAsync(p => p.Id == PoolId);

            if (pool == null)
            {
                ModelState.AddModelError(string.Empty, "Pool not found.");
                Pools = await _context.Pools.ToListAsync();
                Users = await _context.Users.ToListAsync();
                return Page();
            }

            var usersToAdd = await _context.Users
                .Where(u => SelectedUserIds.Contains(u.Id))
                .ToListAsync();

            foreach (var user in usersToAdd)
            {
                if (pool.Members == null)
                    pool.Members = new List<ApplicationUser>();
                if (!pool.Members.Any(u => u.Id == user.Id))
                    pool.Members.Add(user);
            }

            await _context.SaveChangesAsync();
            return RedirectToPage("Details", new { id = PoolId });
        }
    }
}