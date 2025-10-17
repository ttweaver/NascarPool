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
    public class PicksModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        public PicksModel(ApplicationDbContext context) => _context = context;

        [BindProperty(SupportsGet = true)]
        public int RaceId { get; set; }
        public Race? Race { get; set; }
        public List<User> Users { get; set; } = new();
        public List<Driver> Drivers { get; set; } = new();
        public Dictionary<int, Pick> UserPicks { get; set; } = new();

        public async Task<IActionResult> OnGetAsync()
        {
            Race = await _context.Races.Include(r => r.Pool).FirstOrDefaultAsync(r => r.Id == RaceId);
            if (Race == null) return NotFound();

            // Get users in the current pool
            var memberIds = Race.Pool.Members.Select(m => m.Id).ToList();
            Users = await _context.Users
                .Where(u => memberIds.Contains(u.Id))
                .ToListAsync();

            // Get drivers in the current pool
            Drivers = await _context.Drivers
                .Where(d => d.Pool.Id == Race.Pool.Id)
                .ToListAsync();

            // Get existing picks for this race
            var picks = await _context.Picks
                .Where(p => p.RaceId == RaceId)
                .ToListAsync();

            UserPicks = picks.ToDictionary(p => p.UserId, p => p);

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            Race = await _context.Races.Include(r => r.Pool).FirstOrDefaultAsync(r => r.Id == RaceId);
            if (Race == null) return NotFound();

            // Get users in the current pool
            var memberIds = Race.Pool.Members.Select(m => m.Id).ToList();
            Users = await _context.Users
                .Where(u => memberIds.Contains(u.Id))
                .ToListAsync();

            Drivers = await _context.Drivers
                .Where(d => d.Pool.Id == Race.Pool.Id)
                .ToListAsync();

            var userIds = Request.Form["UserIds"].ToArray();
            var pick1Ids = Request.Form["Pick1Ids"].ToArray();
            var pick2Ids = Request.Form["Pick2Ids"].ToArray();
            var pick3Ids = Request.Form["Pick3Ids"].ToArray();

            for (int i = 0; i < userIds.Length; i++)
            {
                if (!int.TryParse(userIds[i], out var userId)) continue;
                int.TryParse(pick1Ids[i], out var pick1Id);
                int.TryParse(pick2Ids[i], out var pick2Id);
                int.TryParse(pick3Ids[i], out var pick3Id);

                // Validate picks
                if (pick1Id == 0 || pick2Id == 0 || pick3Id == 0)
                {
                    ModelState.AddModelError(string.Empty, $"All picks required for user {Users.FirstOrDefault(u => u.Id == userId)?.UserName}.");
                    continue;
                }

                var existingPick = await _context.Picks.FirstOrDefaultAsync(p => p.RaceId == RaceId && p.UserId == userId);
                if (existingPick != null)
                {
                    existingPick.Pick1Id = pick1Id;
                    existingPick.Pick2Id = pick2Id;
                    existingPick.Pick3Id = pick3Id;
                    _context.Picks.Update(existingPick);
                }
                else
                {
                    var pick = new Pick
                    {
                        RaceId = RaceId,
                        UserId = userId,
                        Pick1Id = pick1Id,
                        Pick2Id = pick2Id,
                        Pick3Id = pick3Id
                    };
                    _context.Picks.Add(pick);
                }
            }

            if (!ModelState.IsValid)
            {
                // Reload for redisplay
                var picks = await _context.Picks.Where(p => p.RaceId == RaceId).ToListAsync();
                UserPicks = picks.ToDictionary(p => p.UserId, p => p);
                return Page();
            }

            await _context.SaveChangesAsync();
            return RedirectToPage(new { raceId = RaceId });
        }
    }
}