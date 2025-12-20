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
            public string UserName { get; set; } = string.Empty;
            public int Points { get; set; }
        }

        private Pool? GetCurrentSeasonFromCookie()
        {
            // Try to get poolId from cookie
            var poolIdCookie = Request.Cookies["poolId"];
            Pool? currentSeason = null;

            if (!string.IsNullOrEmpty(poolIdCookie) && int.TryParse(poolIdCookie, out var cookiePoolId))
            {
                currentSeason = _context.Pools.FirstOrDefault(p => p.Id == cookiePoolId);
            }

            // Fallback to latest season if cookie not found or invalid
            if (currentSeason == null)
            {
                currentSeason = _context.Pools.AsEnumerable()
                    .OrderByDescending(s => s.CurrentYear)
                    .FirstOrDefault();
            }

            return currentSeason;
        }

        public async Task<IActionResult> OnGetAsync()
        {
            Race = await _context.Races.Include(r => r.Pool).FirstOrDefaultAsync(r => r.Id == RaceId);
            if (Race == null) return NotFound();

            // Verify that the race belongs to the current season from cookie (optional validation)
            var currentSeason = GetCurrentSeasonFromCookie();
            if (currentSeason != null && Race.PoolId != currentSeason.Id)
            {
                // Log or handle the case where viewing results for a race from a different season
                // For now, we'll allow it but you could add validation here
            }

            // Get all picks for this race, including user
            var picks = await _context.Picks
                .Include(p => p.User)
                .Where(p => p.RaceId == RaceId)
                .ToListAsync();

            // Order by least points (lowest is best)
            RankedResults = picks
                .OrderBy(p => p.Points)
                .Select(p => new UserResult
                {
                    UserName = p.User.UserName,
                    Points = p.Points
                })
                .ToList();

            return Page();
        }
    }
}