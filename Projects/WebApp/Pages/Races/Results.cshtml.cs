using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using WebApp.Data;
using WebApp.Models;
using WebApp.Helpers;
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

            // Check for duplicate places and collect driver names for duplicates
            var placeToDriverIds = new Dictionary<int, List<int>>();
            for (int i = 0; i < places.Length; i++)
            {
                if (int.TryParse(places[i], out var place) && place > 0)
                {
                    if (!placeToDriverIds.ContainsKey(place))
                        placeToDriverIds[place] = new List<int>();
                    if (int.TryParse(driverIds[i], out var driverId))
                        placeToDriverIds[place].Add(driverId);
                }
            }
            foreach (var kvp in placeToDriverIds.Where(p => p.Value.Count > 1))
            {
                var driverNames = kvp.Value
                    .Select(id => Drivers.FirstOrDefault(d => d.Id == id)?.Name ?? $"DriverId {id}")
                    .ToList();
                ModelState.AddModelError(string.Empty, $"Duplicate place '{kvp.Key}' assigned to: {string.Join(", ", driverNames)}.");
            }

            // Save or update race results only if no duplicate places
            if (ModelState.IsValid)
            {
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
            }

            if (!ModelState.IsValid)
            {
                var results = await _context.RaceResults.Where(r => r.RaceId == RaceId).ToListAsync();
                DriverResults = results.ToDictionary(r => r.DriverId, r => r);
                return Page();
            }

            await _context.SaveChangesAsync();

            // After saving results, update points for all picks related to this race
            var raceResults = await _context.RaceResults.Where(r => r.RaceId == RaceId).ToListAsync();
            var picks = await _context.Picks.Where(p => p.RaceId == RaceId).ToListAsync();
            foreach (var pick in picks)
            {
                pick.CalculateTotalPoints(_context, raceResults);
                _context.Picks.Update(pick);
            }
            await _context.SaveChangesAsync();

            return RedirectToPage(new { raceId = RaceId });
        }

        public async Task<IActionResult> OnPostCalculatePointsAsync()
        {
            await PickPointsCalculator.CalculateAllPicksPointsAsync(_context, RaceId);
            return RedirectToPage(new { raceId = RaceId });
        }
    }
}