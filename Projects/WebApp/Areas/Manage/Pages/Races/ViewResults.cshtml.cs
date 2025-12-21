using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using WebApp.Data;
using WebApp.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WebApp.Areas.Manage.Pages.Races
{
    public class ViewResultsModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        public ViewResultsModel(ApplicationDbContext context) => _context = context;

        [BindProperty(SupportsGet = true)]
        public int RaceId { get; set; }
        public Race? Race { get; set; }

        public List<UserResult> RankedResults { get; set; } = new();

        public class UserResult
        {
            public string UserId { get; set; } = string.Empty;
            public string UserName { get; set; } = string.Empty;
            public string FirstName { get; set; } = string.Empty;
            public string LastName { get; set; } = string.Empty;
            public int Points { get; set; }
            public int Place { get; set; }
            public bool IsTied { get; set; }
        }

        private Pool? GetCurrentSeasonFromCookie()
        {
            // Try to get poolId from cookie
            var poolIdCookie = Request.Cookies["poolId"];
            Pool? currentSeason = null;

            if (!string.IsNullOrEmpty(poolIdCookie) && int.TryParse(poolIdCookie, out var cookiePoolId))
            {
                currentSeason = _context.Pools.Include(p => p.Members).FirstOrDefault(p => p.Id == cookiePoolId);
            }

            // Fallback to latest season if cookie not found or invalid
            if (currentSeason == null)
            {
                currentSeason = _context.Pools.Include(p => p.Members).AsEnumerable()
                    .OrderByDescending(s => s.CurrentYear)
                    .FirstOrDefault();
            }

            return currentSeason;
        }

        public async Task<IActionResult> OnGetAsync()
        {
            Race = await _context.Races.Include(r => r.Pool).ThenInclude(p => p.Members).FirstOrDefaultAsync(r => r.Id == RaceId);
            if (Race == null) return NotFound();

            // Get pool member IDs for the pool this race belongs to
            var poolMemberIds = Race.Pool.Members
                .Select(m => m.Id)
                .ToList();

            // Get all picks for this race from pool members only
            var picks = await _context.Picks
                .Include(p => p.User)
                .Where(p => p.RaceId == RaceId && p.User.IsPlayer && poolMemberIds.Contains(p.UserId))
                .OrderBy(p => p.Points) // Lower points = better rank
                .ToListAsync();

            // Calculate places with tie handling
            var rankedResults = new List<UserResult>();
            int currentPlace = 1;
            int? previousPoints = null;
            int playersAtCurrentPlace = 0;

            foreach (var pick in picks)
            {
                // If points changed from previous entry, advance place
                if (previousPoints.HasValue && pick.Points != previousPoints.Value)
                {
                    currentPlace += playersAtCurrentPlace;
                    playersAtCurrentPlace = 0;
                }

                playersAtCurrentPlace++;

                // Check if this player is tied with others at this place
                bool isTied = picks.Count(p => p.Points == pick.Points) > 1;

                rankedResults.Add(new UserResult
                {
                    UserId = pick.UserId,
                    UserName = pick.User.UserName ?? string.Empty,
                    FirstName = pick.User.FirstName,
                    LastName = pick.User.LastName,
                    Points = pick.Points,
                    Place = currentPlace,
                    IsTied = isTied
                });

                previousPoints = pick.Points;
            }

            RankedResults = rankedResults;

            return Page();
        }
    }
}