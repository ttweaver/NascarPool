using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using WebApp.Data;
using WebApp.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WebApp.Pages.Races
{
    public class ViewResultsModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        public ViewResultsModel(ApplicationDbContext context) => _context = context;

        [BindProperty(SupportsGet = true)]
        public int RaceId { get; set; }
        public Race? Race { get; set; }

        public List<UserResult> RankedResults { get; set; } = new();
        public int? CurrentUserPlace { get; set; }
        public int TotalPlayers { get; set; }

        public class UserResult
        {
            public string UserName { get; set; } = string.Empty;
            public int Points { get; set; }
        }

        public async Task<IActionResult> OnGetAsync()
        {
            Race = await _context.Races.Include(r => r.Pool).FirstOrDefaultAsync(r => r.Id == RaceId);
            if (Race == null) return NotFound();

            // Get all picks for this race, including user, filter by IsPlayer
            var picks = await _context.Picks
                .Include(p => p.User)
                .Where(p => p.RaceId == RaceId && p.User.IsPlayer)
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

            TotalPlayers = RankedResults.Count;

            // Find current user's place
            if (User.Identity?.IsAuthenticated == true)
            {
                var currentUserName = User.Identity.Name;
                CurrentUserPlace = RankedResults
                    .Select((result, index) => new { result.UserName, Place = index + 1 })
                    .FirstOrDefault(x => x.UserName == currentUserName)?.Place;
            }

            return Page();
        }
    }
}