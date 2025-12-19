using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using WebApp.Data;
using WebApp.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;

namespace WebApp.Pages.Races.Picks
{
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        public IndexModel(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [BindProperty(SupportsGet = true)]
        public int RaceId { get; set; }

        public Race? Race { get; set; }
        public List<PlayerPicks> AllPlayerPicks { get; set; } = new();
        public string? CurrentUserId { get; set; }

        public class PlayerPicks
        {
            public string UserId { get; set; } = string.Empty;
            public string PlayerName { get; set; } = string.Empty;
            public string? Pick1Name { get; set; }
            public string? Pick1CarNumber { get; set; }
            public string? Pick2Name { get; set; }
            public string? Pick2CarNumber { get; set; }
            public string? Pick3Name { get; set; }
            public string? Pick3CarNumber { get; set; }
            public bool HasPicks { get; set; }
        }

        public async Task<IActionResult> OnGetAsync()
        {
            CurrentUserId = _userManager.GetUserId(User);

            Race = await _context.Races
                .Include(r => r.Pool)
                .ThenInclude(p => p.Members)
                .FirstOrDefaultAsync(r => r.Id == RaceId);

            if (Race == null)
                return NotFound();

            // Get all picks for this race
            var picks = await _context.Picks
                .Include(p => p.User)
                .Include(p => p.Pick1)
                .Include(p => p.Pick2)
                .Include(p => p.Pick3)
                .Where(p => p.RaceId == RaceId)
                .ToListAsync();

            // Get all pool members ordered by first name (primary) then last name as a tie-breaker
            var poolMembers = Race.Pool.Members
                .OrderBy(m => m.FirstName)
                .ThenBy(m => m.LastName)
                .ToList();

            // Build the list of player picks
            AllPlayerPicks = poolMembers.Select(member =>
            {
                var pick = picks.FirstOrDefault(p => p.UserId == member.Id);
                return new PlayerPicks
                {
                    UserId = member.Id,
                    PlayerName = $"{member.FirstName} {member.LastName}",
                    Pick1Name = pick?.Pick1?.Name,
                    Pick1CarNumber = pick?.Pick1?.CarNumber,
                    Pick2Name = pick?.Pick2?.Name,
                    Pick2CarNumber = pick?.Pick2?.CarNumber,
                    Pick3Name = pick?.Pick3?.Name,
                    Pick3CarNumber = pick?.Pick3?.CarNumber,
                    HasPicks = pick != null
                };
            }).ToList();

            return Page();
        }
    }
}