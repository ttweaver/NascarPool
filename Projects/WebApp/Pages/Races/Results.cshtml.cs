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
    public class ResultsModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        public ResultsModel(ApplicationDbContext context) => _context = context;

        [BindProperty(SupportsGet = true)]
        public int RaceId { get; set; }
        public Race? Race { get; set; }
        public List<Driver> Drivers { get; set; } = new();
        public Dictionary<int, RaceResult> DriverResults { get; set; } = new();

        public async Task<IActionResult> OnGetAsync()
        {
            Race = await _context.Races.Include(r => r.Pool).FirstOrDefaultAsync(r => r.Id == RaceId);
            if (Race == null) return NotFound();

            Drivers = await _context.Drivers
                .Where(d => d.Pool.Id == Race.Pool.Id)
                .OrderBy(d => d.CarNumber)
                .ToListAsync();

            var results = await _context.RaceResults
                .Where(r => r.RaceId == RaceId)
                .ToListAsync();

            DriverResults = results.ToDictionary(r => r.DriverId, r => r);

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            Race = await _context.Races.Include(r => r.Pool).FirstOrDefaultAsync(r => r.Id == RaceId);
            if (Race == null) return NotFound();

            Drivers = await _context.Drivers
                .Where(d => d.Pool.Id == Race.Pool.Id)
                .OrderBy(d => d.CarNumber)
                .ToListAsync();

            var driverIds = Request.Form["DriverIds"].ToArray();
            var places = Request.Form["Places"].ToArray();

            for (int i = 0; i < driverIds.Length; i++)
            {
                if (!int.TryParse(driverIds[i], out var driverId)) continue;
                if (!int.TryParse(places[i], out var place) || place < 1)
                {
                    ModelState.AddModelError(string.Empty, $"Invalid place for driver {Drivers.FirstOrDefault(d => d.Id == driverId)?.Name}.");
                    continue;
                }

                var existingResult = await _context.RaceResults.FirstOrDefaultAsync(r => r.RaceId == RaceId && r.DriverId == driverId);
                if (existingResult != null)
                {
                    existingResult.Place = place;
                    _context.RaceResults.Update(existingResult);
                }
                else
                {
                    var result = new RaceResult
                    {
                        RaceId = RaceId,
                        DriverId = driverId,
                        Place = place
                    };
                    _context.RaceResults.Add(result);
                }
            }

            if (!ModelState.IsValid)
            {
                var results = await _context.RaceResults.Where(r => r.RaceId == RaceId).ToListAsync();
                DriverResults = results.ToDictionary(r => r.DriverId, r => r);
                return Page();
            }

            await _context.SaveChangesAsync();
            return RedirectToPage(new { raceId = RaceId });
        }
    }
}