using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using WebApp.Data;
using WebApp.Models;
using WebApp.Helpers;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace WebApp.Areas.Manage.Pages.Races
{
    public class ResultsModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ResultsModel> _logger;

        public ResultsModel(ApplicationDbContext context, ILogger<ResultsModel> logger)
        {
            _context = context;
            _logger = logger;
        }

        [BindProperty(SupportsGet = true)]
        public int RaceId { get; set; }
        public Race? Race { get; set; }
        public List<Driver> Drivers { get; set; } = new();
        public Dictionary<int, RaceResult> DriverResults { get; set; } = new();

        private Pool? GetCurrentSeasonFromCookie()
        {
            // Try to get poolId from cookie
            var poolIdCookie = Request.Cookies["poolId"];
            Pool? currentSeason = null;

            if (!string.IsNullOrEmpty(poolIdCookie) && int.TryParse(poolIdCookie, out var cookiePoolId))
            {
                currentSeason = _context.Pools.FirstOrDefault(p => p.Id == cookiePoolId);
            }

            // Fallback to latest season if cookie not found or invalid
            if (currentSeason == null)
            {
                currentSeason = _context.Pools.AsEnumerable()
                    .OrderByDescending(s => s.CurrentYear)
                    .FirstOrDefault();
            }

            return currentSeason;
        }

        public async Task<IActionResult> OnGetAsync()
        {
            try
            {
                _logger.LogInformation("User {UserId} ({Email}) accessing race results page for race {RaceId}", 
                    User.Identity?.Name ?? "Anonymous", User.Identity?.Name ?? "Anonymous", RaceId);

                Race = await _context.Races.Include(r => r.Pool).FirstOrDefaultAsync(r => r.Id == RaceId);
                if (Race == null)
                {
                    _logger.LogWarning("Race not found during results page access. RaceId: {RaceId}, User: {UserId}", 
                        RaceId, User.Identity?.Name ?? "Anonymous");
                    return NotFound();
                }

                // Verify that the race belongs to the current season from cookie (optional validation)
                var currentSeason = GetCurrentSeasonFromCookie();
                if (currentSeason != null && Race.PoolId != currentSeason.Id)
                {
                    _logger.LogWarning("Race {RaceId} does not belong to current season {PoolId}. Race PoolId: {RacePoolId}. Redirecting to race index.", 
                        RaceId, currentSeason.Id, Race.PoolId);
                    return RedirectToPage("/Races/Index", new { area = "Manage" });
                }

                Drivers = await _context.Drivers
                    .Where(d => d.Pool.Id == Race.Pool.Id)
                    .OrderBy(d => d.CarNumber)
                    .ToListAsync();

                var results = await _context.RaceResults
                    .Where(r => r.RaceId == RaceId)
                    .ToListAsync();

                DriverResults = results.ToDictionary(r => r.DriverId, r => r);

                _logger.LogInformation("Race results page loaded successfully. RaceId: {RaceId}, Race: {RaceName}, " +
                    "PoolId: {PoolId}, DriverCount: {DriverCount}, ExistingResultCount: {ResultCount}", 
                    RaceId, Race.Name, Race.PoolId, Drivers.Count, results.Count);

                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading race results page for race {RaceId}", RaceId);
                throw;
            }
        }

        public async Task<IActionResult> OnPostAsync()
        {
            try
            {
                var adminEmail = User.Identity?.Name ?? "Anonymous";
                var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

                _logger.LogInformation("User {AdminEmail} attempting to save race results for race {RaceId}, IP: {IpAddress}", 
                    adminEmail, RaceId, ipAddress);

                Race = await _context.Races.Include(r => r.Pool).FirstOrDefaultAsync(r => r.Id == RaceId);
                if (Race == null)
                {
                    _logger.LogWarning("Race not found during results save. RaceId: {RaceId}, User: {UserId}", 
                        RaceId, adminEmail);
                    return NotFound();
                }

                // Verify that the race belongs to the current season from cookie (optional validation)
                var currentSeason = GetCurrentSeasonFromCookie();
                if (currentSeason != null && Race.PoolId != currentSeason.Id)
                {
                    _logger.LogWarning("Saving results for race {RaceId} which does not belong to current season {PoolId}. Race PoolId: {RacePoolId}", 
                        RaceId, currentSeason.Id, Race.PoolId);
                }

                Drivers = await _context.Drivers
                    .Where(d => d.Pool.Id == Race.Pool.Id)
                    .OrderBy(d => d.CarNumber)
                    .ToListAsync();

                var driverIds = Request.Form["DriverIds"].ToArray();
                var places = Request.Form["Places"].ToArray();

                _logger.LogDebug("Processing {Count} race result entries for race {RaceId}", driverIds.Length, RaceId);

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

                    _logger.LogWarning("Race result validation failed - duplicate place. RaceId: {RaceId}, Place: {Place}, " +
                        "Drivers: {Drivers}, User: {UserId}", 
                        RaceId, kvp.Key, string.Join(", ", driverNames), adminEmail);

                    ModelState.AddModelError(string.Empty, $"Duplicate place '{kvp.Key}' assigned to: {string.Join(", ", driverNames)}.");
                }

                int createdCount = 0;
                int updatedCount = 0;
                int skippedCount = 0;

                // Save or update race results only if no duplicate places
                if (ModelState.IsValid)
                {
                    for (int i = 0; i < driverIds.Length; i++)
                    {
                        if (!int.TryParse(driverIds[i], out var driverId))
                        {
                            skippedCount++;
                            continue;
                        }

                        if (!int.TryParse(places[i], out var place) || place < 1)
                        {
                            var driverName = Drivers.FirstOrDefault(d => d.Id == driverId)?.Name ?? $"DriverId {driverId}";
                            _logger.LogWarning("Race result validation failed - invalid place. RaceId: {RaceId}, " +
                                "Driver: {DriverName}, Place: {Place}, User: {UserId}", 
                                RaceId, driverName, places[i], adminEmail);

                            ModelState.AddModelError(string.Empty, $"Invalid place for driver {driverName}.");
                            skippedCount++;
                            continue;
                        }

                        var driver = Drivers.FirstOrDefault(d => d.Id == driverId);
                        var driverInfo = driver != null ? $"{driver.Name} (#{driver.CarNumber})" : $"DriverId {driverId}";

                        var existingResult = await _context.RaceResults.FirstOrDefaultAsync(r => r.RaceId == RaceId && r.DriverId == driverId);
                        
                        if (existingResult != null)
                        {
                            var oldPlace = existingResult.Place;
                            existingResult.Place = place;
                            _context.RaceResults.Update(existingResult);
                            updatedCount++;

                            _logger.LogInformation("Race result updated. ResultId: {ResultId}, RaceId: {RaceId}, Race: {RaceName}, " +
                                "Driver: {DriverInfo}, Place: {OldPlace} -> {NewPlace}, User: {UserId}, IP: {IpAddress}", 
                                existingResult.Id, RaceId, Race.Name, driverInfo, oldPlace, place, adminEmail, ipAddress);
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
                            createdCount++;

                            _logger.LogInformation("Race result created. RaceId: {RaceId}, Race: {RaceName}, " +
                                "Driver: {DriverInfo}, Place: {Place}, User: {UserId}, IP: {IpAddress}", 
                                RaceId, Race.Name, driverInfo, place, adminEmail, ipAddress);
                        }
                    }
                }

                if (!ModelState.IsValid)
                {
                    _logger.LogWarning("Race results save failed validation. RaceId: {RaceId}, " +
                        "Created: {Created}, Updated: {Updated}, Skipped: {Skipped}, User: {UserId}", 
                        RaceId, createdCount, updatedCount, skippedCount, adminEmail);

                    var results = await _context.RaceResults.Where(r => r.RaceId == RaceId).ToListAsync();
                    DriverResults = results.ToDictionary(r => r.DriverId, r => r);
                    return Page();
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation("Race results saved successfully. RaceId: {RaceId}, Race: {RaceName}, PoolId: {PoolId}, " +
                    "Created: {Created}, Updated: {Updated}, Skipped: {Skipped}, User: {UserId}, IP: {IpAddress}", 
                    RaceId, Race.Name, Race.PoolId, createdCount, updatedCount, skippedCount, adminEmail, ipAddress);

                // After saving results, update points for all picks related to this race
                _logger.LogInformation("Starting pick points recalculation for race {RaceId}", RaceId);

                var raceResults = await _context.RaceResults.Where(r => r.RaceId == RaceId).ToListAsync();
                var picks = await _context.Picks.Where(p => p.RaceId == RaceId).ToListAsync();

                int pickPointsUpdated = 0;
                foreach (var pick in picks)
                {
                    var oldPoints = pick.Points;
                    pick.CalculateTotalPoints(_context, raceResults);
                    _context.Picks.Update(pick);

                    if (oldPoints != pick.Points)
                    {
                        pickPointsUpdated++;
                        _logger.LogDebug("Pick points recalculated. PickId: {PickId}, UserId: {UserId}, " +
                            "RaceId: {RaceId}, Points: {OldPoints} -> {NewPoints}", 
                            pick.Id, pick.UserId, RaceId, oldPoints, pick.Points);
                    }
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation("Pick points recalculation completed. RaceId: {RaceId}, " +
                    "TotalPicks: {TotalPicks}, PicksWithPointChanges: {ChangedPicks}, User: {UserId}", 
                    RaceId, picks.Count, pickPointsUpdated, adminEmail);

                return RedirectToPage(new { raceId = RaceId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving race results for race {RaceId}, User: {UserId}", 
                    RaceId, User.Identity?.Name ?? "Anonymous");
                throw;
            }
        }

        public async Task<IActionResult> OnPostCalculatePointsAsync()
        {
            try
            {
                var adminEmail = User.Identity?.Name ?? "Anonymous";
                var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

                _logger.LogInformation("User {AdminEmail} triggering manual points recalculation for race {RaceId}, IP: {IpAddress}", 
                    adminEmail, RaceId, ipAddress);

                var race = await _context.Races.Include(r => r.Pool).FirstOrDefaultAsync(r => r.Id == RaceId);
                if (race == null)
                {
                    _logger.LogWarning("Race not found during manual points calculation. RaceId: {RaceId}, User: {UserId}", 
                        RaceId, adminEmail);
                    return NotFound();
                }

                // Verify that the race belongs to the current season from cookie (optional validation)
                var currentSeason = GetCurrentSeasonFromCookie();
                if (currentSeason != null && race.PoolId != currentSeason.Id)
                {
                    _logger.LogWarning("Recalculating points for race {RaceId} which does not belong to current season {PoolId}. Race PoolId: {RacePoolId}", 
                        RaceId, currentSeason.Id, race.PoolId);
                }

                var picksBefore = await _context.Picks
                    .Where(p => p.RaceId == RaceId)
                    .Select(p => new { p.Id, p.UserId, p.Points })
                    .ToListAsync();

                await PickPointsCalculator.CalculateAllPicksPointsAsync(_context, RaceId);
                await _context.SaveChangesAsync();

                var picksAfter = await _context.Picks
                    .Where(p => p.RaceId == RaceId)
                    .Select(p => new { p.Id, p.UserId, p.Points })
                    .ToListAsync();

                int changedCount = 0;
                foreach (var before in picksBefore)
                {
                    var after = picksAfter.FirstOrDefault(p => p.Id == before.Id);
                    if (after != null && before.Points != after.Points)
                    {
                        changedCount++;
                        _logger.LogDebug("Pick points changed during manual recalculation. PickId: {PickId}, " +
                            "UserId: {UserId}, Points: {OldPoints} -> {NewPoints}", 
                            before.Id, before.UserId, before.Points, after.Points);
                    }
                }

                _logger.LogInformation("Manual points recalculation completed successfully. RaceId: {RaceId}, " +
                    "Race: {RaceName}, PoolId: {PoolId}, TotalPicks: {TotalPicks}, PicksChanged: {ChangedPicks}, " +
                    "User: {UserId}, IP: {IpAddress}", 
                    RaceId, race.Name, race.PoolId, picksBefore.Count, changedCount, adminEmail, ipAddress);

                return RedirectToPage(new { raceId = RaceId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during manual points recalculation for race {RaceId}, User: {UserId}", 
                    RaceId, User.Identity?.Name ?? "Anonymous");
                throw;
            }
        }
    }
}