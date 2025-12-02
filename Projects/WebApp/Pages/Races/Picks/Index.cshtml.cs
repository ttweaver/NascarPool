using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using WebApp.Data;
using WebApp.Models;

namespace WebApp.Pages.Races.Picks
{
    public class PicksModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        public PicksModel(ApplicationDbContext context) => _context = context;

        [BindProperty(SupportsGet = true)]
        public int RaceId { get; set; }
        public Race? Race { get; set; }
        public List<ApplicationUser> Users { get; set; } = new();
        public List<Driver> Drivers { get; set; } = new();
        public Dictionary<string, WebApp.Models.Pick> UserPicks { get; set; } = new();

        // New: races for the pool and per-user default Pick1 (first or second half)
        public List<Race> Races { get; set; } = new();
        public Dictionary<string, int?> UserDefaultPick1 { get; set; } = new();

        public async Task<IActionResult> OnGetAsync()
        {
            Race = await _context.Races.Include(r => r.Pool.Members).FirstOrDefaultAsync(r => r.Id == RaceId);
            if (Race == null) return NotFound();

            // Get users in the current pool
            var memberIds = Race.Pool.Members.Select(m => m.Id).ToList();
            Users = await _context.Users
                .Where(u => memberIds.Contains(u.Id))
                .ToListAsync();

            // Get drivers in the current pool
            Drivers = await _context.Drivers
                .Where(d => d.Pool.Id == Race.Pool.Id)
                .OrderBy(d => d.Name)
                .ToListAsync();

            // Get existing picks for this race
            var picks = await _context.Picks
                .Where(p => p.RaceId == RaceId)
                .ToListAsync();

            UserPicks = picks.ToDictionary(p => p.UserId, p => p);

            // Build races list for the pool and compute first/second half
            if (Race.PoolId != 0)
            {
                Races = await _context.Races
                    .Where(r => r.PoolId == Race.PoolId)
                    .OrderBy(r => r.Date)
                    .ToListAsync();
            }

            var totalRaces = Races.Count;
            var half = totalRaces / 2;
            var raceIndex = Races.FindIndex(r => r.Id == RaceId);
            var isFirstHalf = raceIndex >= 0 && raceIndex < half;

            // Prepare per-user default Pick1 (first-half or second-half primary driver)
            UserDefaultPick1 = new Dictionary<string, int?>();
            foreach (var user in Users)
            {
                int? primaryDriverId = null;
                // ApplicationUser is expected to have these properties; handle if missing (null)
                var userEntry = await _context.Users.FirstOrDefaultAsync(u => u.Id == user.Id);
                if (userEntry != null)
                {
                    if (isFirstHalf)
                        primaryDriverId = userEntry.PrimaryDriverFirstHalfId;
                    else
                        primaryDriverId = userEntry.PrimaryDriverSecondHalfId;
                }
                UserDefaultPick1[user.Id] = primaryDriverId;
            }

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            Race = await _context.Races.Include(r => r.Pool.Members).FirstOrDefaultAsync(r => r.Id == RaceId);
            if (Race == null) return NotFound();

            var memberIds = Race.Pool.Members.Select(m => m.Id).ToList();
            Users = await _context.Users
                .Where(u => memberIds.Contains(u.Id))
                .ToListAsync();

            Drivers = await _context.Drivers
                .Where(d => d.Pool.Id == Race.Pool.Id)
                .ToListAsync();

            // Build races list for the pool and compute first/second half (so view can re-render defaults on validation errors)
            if (Race.PoolId != 0)
            {
                Races = await _context.Races
                    .Where(r => r.PoolId == Race.PoolId)
                    .OrderBy(r => r.Date)
                    .ToListAsync();
            }

            var totalRaces = Races.Count;
            var half = totalRaces / 2;
            var raceIndex = Races.FindIndex(r => r.Id == RaceId);
            var isFirstHalf = raceIndex >= 0 && raceIndex < half;

            UserDefaultPick1 = new Dictionary<string, int?>();
            foreach (var user in Users)
            {
                int? primaryDriverId = null;
                var userEntry = await _context.Users.FirstOrDefaultAsync(u => u.Id == user.Id);
                if (userEntry != null)
                {
                    if (isFirstHalf)
                        primaryDriverId = userEntry.PrimaryDriverFirstHalfId;
                    else
                        primaryDriverId = userEntry.PrimaryDriverSecondHalfId;
                }
                UserDefaultPick1[user.Id] = primaryDriverId;
            }

            var userIds = Request.Form["UserIds"].ToArray();
            var pick1Ids = Request.Form["Pick1Ids"].ToArray();
            var pick2Ids = Request.Form["Pick2Ids"].ToArray();
            var pick3Ids = Request.Form["Pick3Ids"].ToArray();

            for (int i = 0; i < userIds.Length; i++)
            {
                var userId = userIds[i];
                int.TryParse(pick1Ids[i], out var pick1Id);
                int.TryParse(pick2Ids[i], out var pick2Id);
                int.TryParse(pick3Ids[i], out var pick3Id);

                // Validate picks
                if (pick1Id == 0 || pick2Id == 0 || pick3Id == 0)
                {
                    ModelState.AddModelError(string.Empty, $"All picks required for user {Users.FirstOrDefault(u => u.Id == userId)?.UserName}.");
                    continue;
                }

                // Check for duplicate picks
                var picksSet = new HashSet<int> { pick1Id, pick2Id, pick3Id };
                if (picksSet.Count < 3)
                {
                    ModelState.AddModelError(string.Empty, $"Duplicate picks detected for user {Users.FirstOrDefault(u => u.Id == userId)?.UserName}.");
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
                    var pick = new WebApp.Models.Pick
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
                var picks = await _context.Picks.Where(p => p.RaceId == RaceId).ToListAsync();
                UserPicks = picks.ToDictionary(p => p.UserId, p => p);
                return Page();
            }

            await _context.SaveChangesAsync();
            return RedirectToPage(new { raceId = RaceId });
        }
    }
}