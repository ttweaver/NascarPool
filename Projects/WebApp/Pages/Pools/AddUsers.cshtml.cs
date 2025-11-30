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
            var latestPool = await _context.Pools.OrderByDescending(static p => p.Year).FirstOrDefaultAsync();
            if (latestPool != null)
            {
                PoolId = latestPool.Id;
                await LoadPoolsAndAvailableUsersAsync(PoolId);
            }
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (SelectedUserIds.Count == 0)
            {
                ModelState.AddModelError(string.Empty, "Please select at least one user.");
                await LoadPoolsAndAvailableUsersAsync(PoolId);
                return Page();
            }

            var pool = await _context.Pools
                .Include(p => p.Members)
                .FirstOrDefaultAsync(p => p.Id == PoolId);

            if (pool == null)
            {
                ModelState.AddModelError(string.Empty, "Pool not found.");
                await LoadPoolsAndAvailableUsersAsync(PoolId);
                return Page();
            }

            var usersToAdd = await _context.Users.Players()
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

        private async Task LoadPoolsAndAvailableUsersAsync(int poolId)
        {
            Pools = await _context.Pools.ToListAsync();

            if (poolId > 0)
            {
                var pool = await _context.Pools
                    .Include(p => p.Members)
                    .FirstOrDefaultAsync(p => p.Id == poolId);

                if (pool != null && pool.Members != null && pool.Members.Any())
                {
                    var memberIds = pool.Members.Select(m => m.Id).ToList();
                    Users = await _context.Users.Players()
                        .Where(u => !memberIds.Contains(u.Id))
                        .ToListAsync();
                    return;
                }
            }

            Users = await _context.Users.Players().ToListAsync();
        }
    }
}