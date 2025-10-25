using Microsoft.AspNetCore.Authorization;
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
        public DashboardModel(ApplicationDbContext context) => _context = context;

        public string UserId { get; set; }
        public int OverallPlace { get; set; }
        public List<RaceResult> RecentResults { get; set; } = new();
        public Pick? CurrentWeekPick { get; set; }
        public Race? CurrentRace { get; set; }

        public async Task OnGetAsync()
        {
            // Example: get current user id (replace with your auth logic)
            UserId = "";

            var standings = await _context.Picks
                .GroupBy(p => p.UserId)
                .Select(g => new { UserId = g.Key, TotalPoints = g.Sum(p => p.Points) })
                .OrderBy(s => s.TotalPoints)
                .ToListAsync();

            OverallPlace = standings.FindIndex(s => s.UserId == UserId) + 1;

            var recentRace = await _context.Races
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
                .Where(r => r.Date >= DateTime.Today)
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