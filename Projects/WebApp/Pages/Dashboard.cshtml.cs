using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using WebApp.Data;
using WebApp.Models;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace WebApp.Pages
{
    [Authorize] // Require user to be logged in
    public class DashboardModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public DashboardModel(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public string UserId { get; set; }
        public int OverallPlace { get; set; }
        public int TotalPoints { get; set; }
        public List<RaceResult> RecentResults { get; set; } = new();
        public Pick? CurrentWeekPick { get; set; }
        public Race? CurrentRace { get; set; }

        public async Task OnGetAsync()
        {
            UserId = _userManager.GetUserId(User);

            // Get current season (assumes a Season entity and a way to determine current season)
            var currentSeason = _context.Pools.AsEnumerable<Pool>()
                .OrderByDescending(s => s.CurrentYear)
                .FirstOrDefault();

            if (currentSeason == null)
            {
                OverallPlace = 0;
                TotalPoints = 0;
                return;
            }

            // Only get picks for the current season
            var seasonRaceIds = await _context.Races
                .Where(r => r.Pool.Id == currentSeason.Id)
                .Select(r => r.Id)
                .ToListAsync();

            var standings = await _context.Picks
                .Where(p => seasonRaceIds.Contains(p.RaceId))
                .GroupBy(p => p.UserId)
                .Select(g => new { UserId = g.Key, TotalPoints = g.Sum(p => p.Points) })
                .OrderBy(s => s.TotalPoints)
                .ToListAsync();

            OverallPlace = standings.FindIndex(s => s.UserId == UserId) + 1;
            TotalPoints = standings.FirstOrDefault(s => s.UserId == UserId)?.TotalPoints ?? 0;

            var recentRace = await _context.Races
                .Where(r => r.Pool.Id == currentSeason.Id)
                .OrderByDescending(r => r.Date)
                .FirstOrDefaultAsync();

            if (recentRace != null)
            {
                RecentResults = await _context.RaceResults
                    .Where(r => r.RaceId == recentRace.Id)
                    .OrderBy(r => r.Place)
                    .Include(r => r.Driver)
                    .ToListAsync();
            }

            CurrentRace = await _context.Races
                .Where(r => r.Pool.Id == currentSeason.Id && r.Date >= DateTime.Today)
                .OrderBy(r => r.Date)
                .FirstOrDefaultAsync();

            if (CurrentRace != null)
            {
                CurrentWeekPick = await _context.Picks
                    .Include(p => p.Pick1)
                    .Include(p => p.Pick2)
                    .Include(p => p.Pick3)
                    .FirstOrDefaultAsync(p => p.RaceId == CurrentRace.Id && p.UserId == UserId);
            }
        }
    }
}