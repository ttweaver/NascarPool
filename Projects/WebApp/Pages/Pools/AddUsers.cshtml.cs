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

        // New: list of member ids for the selected pool (used by the view to mark selected options)
        public List<string> MemberIds { get; set; } = new();

        [TempData]
        public string StatusMessage { get; set; }

        public List<Pool> Pools { get; set; } = new();
        public List<ApplicationUser> Users { get; set; } = new();

        public async Task OnGetAsync(int? id)
        {
            if (id.HasValue)
            {
                PoolId = id.Value;
            }
            else
            {
				var latestPool = await _context.Pools.OrderByDescending(static p => p.Year).FirstOrDefaultAsync();
				if (latestPool != null)
				{
					PoolId = latestPool.Id;
				}
            }
			await LoadPoolsAndAvailableUsersAsync(PoolId);
		}

        public async Task<IActionResult> OnPostAsync()
        {
            // Allow empty selection to remove all members. Ensure SelectedUserIds is not null.
            SelectedUserIds ??= new List<string>();

            var pool = await _context.Pools
                .Include(p => p.Members)
                .FirstOrDefaultAsync(p => p.Id == PoolId);

            if (pool == null)
            {
                ModelState.AddModelError(string.Empty, "Pool not found.");
                await LoadPoolsAndAvailableUsersAsync(PoolId);
                return Page();
            }

            // Ensure members collection exists
            if (pool.Members == null)
                pool.Members = new List<ApplicationUser>();

            // Add selected users that are not already members
            var usersToAdd = await _context.Users.Players()
                .Where(u => SelectedUserIds.Contains(u.Id))
                .ToListAsync();

            foreach (var user in usersToAdd)
            {
                if (!pool.Members.Any(u => u.Id == user.Id))
                    pool.Members.Add(user);
            }

            // Remove existing members that were not selected
            var membersToRemove = pool.Members
                .Where(m => !SelectedUserIds.Contains(m.Id))
                .ToList();

            foreach (var member in membersToRemove)
            {
                pool.Members.Remove(member);
            }

            await _context.SaveChangesAsync();

            // Set a status message to display after redirect
            StatusMessage = "Players were updated.";

            // Redirect back to this page (PRG) so the message is shown and refresh won't repost.
            return RedirectToPage(new { id = PoolId });
        }

        private async Task LoadPoolsAndAvailableUsersAsync(int poolId)
        {
            Pools = await _context.Pools.ToListAsync();

            // Always load all players for the select list,
            // and populate MemberIds from the selected pool so the view can mark options as selected.
            if (poolId > 0)
            {
                var pool = await _context.Pools
                    .Include(p => p.Members)
                    .FirstOrDefaultAsync(p => p.Id == poolId);

                if (pool != null)
                {
                    MemberIds = pool.Members?.Select(m => m.Id).ToList() ?? new List<string>();
                }
            }

            Users = await _context.Users.Players().ToListAsync();
        }
    }
}