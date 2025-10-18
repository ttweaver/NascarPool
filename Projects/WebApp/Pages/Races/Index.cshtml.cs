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

        public async Task OnGetAsync()
        {
            Races = await _context.Races
                .Include(r => r.Pool)
                .ToListAsync();
            Pools = await _context.Pools.ToListAsync();
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
    }
}