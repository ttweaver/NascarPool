using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using WebApp.Data;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WebApp.Models;

namespace WebApp.Pages.Standings
{
    [Authorize]
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public IndexModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public List<StandingEntry> Standings { get; set; } = new();

        public class StandingEntry
        {
            public string UserId { get; set; } = string.Empty;
            public string? UserName { get; set; }
            public int TotalPoints { get; set; }
            public int Place { get; set; }
        }

        public async Task OnGetAsync()
        {
            var currentSeason = _context.Pools.AsEnumerable<Pool>()
				.OrderByDescending(s => s.CurrentYear)
				.FirstOrDefault();

            if (currentSeason == null)
                return;

            var seasonRaceIds = await _context.Races
                .Where(r => r.Pool.Id == currentSeason.Id)
                .Select(r => r.Id)
                .ToListAsync();

            var standingsQuery = await _context.Picks
                .Where(p => seasonRaceIds.Contains(p.RaceId))
                .GroupBy(p => p.UserId)
                .Select(g => new
                {
                    UserId = g.Key,
                    TotalPoints = g.Sum(p => p.Points)
                })
                .OrderBy(s => s.TotalPoints)
                .ToListAsync();

            // Get usernames for display
            var userIds = standingsQuery.Select(s => s.UserId).ToList();
            var users = await _context.Users
                .Where(u => userIds.Contains(u.Id))
                .ToDictionaryAsync(u => u.Id, u => u.UserName);

            int place = 1;
            Standings = standingsQuery
                .Select(s => new StandingEntry
                {
                    UserId = s.UserId,
                    UserName = users.ContainsKey(s.UserId) ? users[s.UserId] : s.UserId,
                    TotalPoints = s.TotalPoints,
                    Place = place++
                })
                .ToList();
        }
    }
}