using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using WebApp.Data;
using WebApp.Models;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using System;

namespace WebApp.Pages.Races
{
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        public IndexModel(ApplicationDbContext context) => _context = context;

        public IList<Race> Races { get; set; } = default!;
        public IList<Pool> Pools { get; set; } = default!;
        public IList<User> Users { get; set; } = default!;
        public IList<Driver> Drivers { get; set; } = default!; // Assuming picks are drivers
        public IList<Pick> Picks { get; set; } = new List<Pick>();

        public async Task OnGetAsync()
        {
            Races = await _context.Races.Include(r => r.Pool).ToListAsync();
            Pools = await _context.Pools.ToListAsync();
            Users = await _context.Users.ToListAsync();      // Add this
            Drivers = await _context.Drivers.ToListAsync();  // Add this (or whatever entity is used for picks)
            Picks = await _context.Picks.ToListAsync();
        }

        // CREATE
        public async Task<IActionResult> OnPostCreateAsync()
        {
            var name = Request.Form["CreateRaceName"];
            var dateStr = Request.Form["CreateRaceDate"];

            if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(dateStr))
            {
                ModelState.AddModelError(string.Empty, "All fields are required.");
                await OnGetAsync();
                return Page();
            }

            if (!DateTime.TryParse(dateStr, out var date))
            {
                ModelState.AddModelError(string.Empty, "Invalid date.");
                await OnGetAsync();
                return Page();
            }

            // Find the current year pool
            var currentPool = _context.Pools.AsEnumerable()
                .FirstOrDefault(p => p.CurrentYear);

            if (currentPool == null)
            {
                ModelState.AddModelError(string.Empty, "No current pool found.");
                await OnGetAsync();
                return Page();
            }

            if (date.Year != currentPool.Year)
            {
                ModelState.AddModelError(string.Empty, $"Race date must be in the current season year: {currentPool.Year}.");
                await OnGetAsync();
                return Page();
            }

            var race = new Race
            {
                Name = name,
                Date = date,
                PoolId = currentPool.Id
            };

            _context.Races.Add(race);
            await _context.SaveChangesAsync();
            return RedirectToPage();
        }

        // EDIT
        public async Task<IActionResult> OnPostEditAsync()
        {
            var idStr = Request.Form["EditRaceId"];
            var name = Request.Form["EditRaceName"];
            var dateStr = Request.Form["EditRaceDate"];
            var poolIdStr = Request.Form["EditRacePoolId"];

            if (!int.TryParse(idStr, out var id) ||
                string.IsNullOrWhiteSpace(name) ||
                string.IsNullOrWhiteSpace(dateStr) ||
                string.IsNullOrWhiteSpace(poolIdStr))
            {
                ModelState.AddModelError(string.Empty, "All fields are required.");
                await OnGetAsync();
                return Page();
            }

            if (!DateTime.TryParse(dateStr, out var date) || !int.TryParse(poolIdStr, out var poolId))
            {
                ModelState.AddModelError(string.Empty, "Invalid date or pool.");
                await OnGetAsync();
                return Page();
            }

            var pool = await _context.Pools.FindAsync(poolId);
            if (pool == null)
            {
                ModelState.AddModelError(string.Empty, "Selected pool not found.");
                await OnGetAsync();
                return Page();
            }

            if (date.Year != pool.Year)
            {
                ModelState.AddModelError(string.Empty, $"Race date must be in the selected pool's year: {pool.Year}.");
                await OnGetAsync();
                return Page();
            }

            var race = await _context.Races.FindAsync(id);
            if (race == null)
            {
                ModelState.AddModelError(string.Empty, "Race not found.");
                await OnGetAsync();
                return Page();
            }

            race.Name = name;
            race.Date = date;
            race.PoolId = poolId;

            _context.Races.Update(race);
            await _context.SaveChangesAsync();
            return RedirectToPage();
        }

        // DELETE
        public async Task<IActionResult> OnPostDeleteAsync()
        {
            var idStr = Request.Form["DeleteRaceId"];
            if (!int.TryParse(idStr, out var id))
            {
                ModelState.AddModelError(string.Empty, "Invalid race id.");
                await OnGetAsync();
                return Page();
            }

            var race = await _context.Races.FindAsync(id);
            if (race != null)
            {
                _context.Races.Remove(race);
                await _context.SaveChangesAsync();
            }
            return RedirectToPage();
        }

        // ENTER PICKS
        public async Task<IActionResult> OnPostEnterPicksAsync()
        {
            var raceIdStr = Request.Form["EnterPicksRaceId"];
            var userIdStr = Request.Form["EnterPicksUser"];
            var pick1IdStr = Request.Form["EnterPick1"];
            var pick2IdStr = Request.Form["EnterPick2"];
            var pick3IdStr = Request.Form["EnterPick3"];

            if (!int.TryParse(raceIdStr, out var raceId) ||
                !int.TryParse(userIdStr, out var userId) ||
                !int.TryParse(pick1IdStr, out var pick1Id) ||
                !int.TryParse(pick2IdStr, out var pick2Id) ||
                !int.TryParse(pick3IdStr, out var pick3Id))
            {
                ModelState.AddModelError(string.Empty, "All fields are required.");
                await OnGetAsync();
                return Page();
            }

            // Ensure picks are unique
            if (pick1Id == pick2Id || pick1Id == pick3Id || pick2Id == pick3Id)
            {
                ModelState.AddModelError(string.Empty, "All picks must be unique.");
                await OnGetAsync();
                return Page();
            }

            // Check for existing pick
            var existingPick = await _context.Picks
                .FirstOrDefaultAsync(p => p.RaceId == raceId && p.UserId == userId);

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
                    RaceId = raceId,
                    UserId = userId,
                    Pick1Id = pick1Id,
                    Pick2Id = pick2Id,
                    Pick3Id = pick3Id
                };
                _context.Picks.Add(pick);
            }

            await _context.SaveChangesAsync();

            return RedirectToPage();
        }
    }
}