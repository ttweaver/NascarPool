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

		// Added: selected pool id for the page (matches Drivers page behavior)
		public int SelectedPoolId { get; set; }

        public async Task OnGetAsync(int? poolId = null)
        {
            Pools = await _context.Pools
                .OrderByDescending(p => p.Year)
                .ToListAsync();

			var lastestPool = await _context.Pools.GetLatestPoolYearAsync();
			if (poolId.HasValue)
			{
				SelectedPoolId = poolId.Value;
			}
			else
			{
				SelectedPoolId = lastestPool.Id;
			}

			Races = await _context.Races
                .Include(r => r.Pool)
                .Where(r => r.PoolId == SelectedPoolId)
                .ToListAsync();
           
        }

        // CREATE
        public async Task<IActionResult> OnPostCreateAsync()
        {
            if (!User.IsInRole("Admin"))
            {
                return Forbid();
            }

            var name = Request.Form["CreateRaceName"];
            var dateStr = Request.Form["CreateRaceDate"];
            var city = Request.Form["CreateRaceCity"];
            var state = Request.Form["CreateRaceState"];
            var poolIdStr = Request.Form["PoolId"];

            if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(dateStr) || 
                string.IsNullOrWhiteSpace(city) || string.IsNullOrWhiteSpace(state))
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

            Pool currentPool = null;

            if (!string.IsNullOrWhiteSpace(poolIdStr) && int.TryParse(poolIdStr, out var postedPoolId))
            {
                currentPool = await _context.Pools.FindAsync(postedPoolId);
            }

            // fallback to pool marked as current year
            if (currentPool == null)
            {
                currentPool = await _context.Pools.FirstOrDefaultAsync(p => p.CurrentYear);
            }

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
                City = city,
                State = state,
                PoolId = currentPool.Id
            };

            _context.Races.Add(race);
            await _context.SaveChangesAsync();
            return RedirectToPage();
        }

        // EDIT
        public async Task<IActionResult> OnPostEditAsync()
        {
			if (!User.IsInRole("Admin"))
			{
				return Forbid();
			}

			var idStr = Request.Form["EditRaceId"];
            var name = Request.Form["EditRaceName"];
            var dateStr = Request.Form["EditRaceDate"];
            var city = Request.Form["EditRaceCity"];
            var state = Request.Form["EditRaceState"];
            var poolIdStr = Request.Form["EditRacePoolId"];

            if (!int.TryParse(idStr, out var id) ||
                string.IsNullOrWhiteSpace(name) ||
                string.IsNullOrWhiteSpace(dateStr) ||
                string.IsNullOrWhiteSpace(city) ||
                string.IsNullOrWhiteSpace(state) ||
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
            race.City = city;
            race.State = state;
            race.PoolId = poolId;

            _context.Races.Update(race);
            await _context.SaveChangesAsync();
            return RedirectToPage();
        }

        // DELETE
        public async Task<IActionResult> OnPostDeleteAsync()
        {
			if (!User.IsInRole("Admin"))
			{
				return Forbid();
			}

			var idStr = Request.Form["DeleteRaceId"];
            var city = Request.Form["DeleteRaceCity"];
            var state = Request.Form["DeleteRaceState"];

            if (!int.TryParse(idStr, out var id))
            {
                ModelState.AddModelError(string.Empty, "Invalid race id.");
                await OnGetAsync();
                return Page();
            }

            var race = await _context.Races.FindAsync(id);
            if (race != null)
            {
                // Optional: Verify city and state match for additional safety
                if (!string.IsNullOrWhiteSpace(city) && !string.IsNullOrWhiteSpace(state))
                {
                    if (race.City != city || race.State != state)
                    {
                        ModelState.AddModelError(string.Empty, "Race details do not match.");
                        await OnGetAsync();
                        return Page();
                    }
                }

                _context.Races.Remove(race);
                await _context.SaveChangesAsync();
            }
            return RedirectToPage();
        }
    }
}