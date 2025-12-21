using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using WebApp.Data;
using WebApp.Models;

namespace WebApp.Areas.Manage.Pages
{
    [Authorize(Roles = "Admin")]
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public IndexModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public int PoolCount { get; set; }
        public int RaceCount { get; set; }
        public int DriverCount { get; set; }
        public int PlayerCount { get; set; }
        public Pool? CurrentSeason { get; set; }

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

        public async Task OnGetAsync()
        {
            CurrentSeason = GetCurrentSeasonFromCookie();

            // Get total pool count (not filtered by current season)
            PoolCount = await _context.Pools.CountAsync();

            if (CurrentSeason != null)
            {
                // Filter statistics by current season
                RaceCount = await _context.Races
                    .Where(r => r.PoolId == CurrentSeason.Id)
                    .CountAsync();

                DriverCount = await _context.Drivers
                    .Where(d => d.PoolId == CurrentSeason.Id)
                    .CountAsync();

                PlayerCount = await _context.Users
                    .Where(u => u.IsPlayer && u.Pools.Any(p => p.Id == CurrentSeason.Id))
                    .CountAsync();
            }
            else
            {
                // No current season found, show all counts
                RaceCount = await _context.Races.CountAsync();
                DriverCount = await _context.Drivers.CountAsync();
                PlayerCount = await _context.Users.CountAsync();
            }
        }
    }
}