using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using WebApp.Data;
using WebApp.Models;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;

namespace WebApp.Pages
{
    [Authorize]
    public class DashboardModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<DashboardModel> _logger;

        public DashboardModel(ApplicationDbContext context, UserManager<ApplicationUser> userManager, ILogger<DashboardModel> logger)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
        }

        public string UserId { get; set; }
        public int OverallPlace { get; set; }
        public int TotalPoints { get; set; }
        public int TotalRaces { get; set; }
        public int CurrentRaceNumber { get; set; }
        public List<RaceResult> RecentResults { get; set; } = new();
        public List<RaceResult> AllRecentResults { get; set; } = new();
        public List<Race> RacesWithResults { get; set; } = new();
        public Pick? CurrentWeekPick { get; set; }
        public Race? CurrentRace { get; set; }
        public bool HasResults { get; set; }

        public Driver PrimaryDriver { get; set; }
        public Race FirstRace { get; set; }
        public List<Driver> AvailableDrivers { get; set; }
        public Driver SecondHalfPrimaryDriver { get; set; }
        public Race SecondHalfFirstRace { get; set; }

        public async Task OnGetAsync()
        {
            try
            {
                UserId = _userManager.GetUserId(User);
                _logger.LogInformation("User {UserId} accessed Dashboard page", UserId);

                var currentSeason = _context.Pools.AsEnumerable()
                    .OrderByDescending(s => s.CurrentYear)
                    .FirstOrDefault();

                if (currentSeason == null)
                {
                    _logger.LogWarning("No current season found for user {UserId}", UserId);
                    OverallPlace = 0;
                    TotalPoints = 0;
                    return;
                }

                var seasonRaceIds = await _context.Races
                    .Where(r => r.Pool.Id == currentSeason.Id)
                    .Select(r => r.Id)
                    .ToListAsync();
                
                TotalRaces = seasonRaceIds.Count;

                var standings = await _context.Picks
                    .Where(p => seasonRaceIds.Contains(p.RaceId))
                    .GroupBy(p => p.UserId)
                    .Select(g => new { UserId = g.Key, TotalPoints = g.Sum(p => p.Points) })
                    .OrderBy(s => s.TotalPoints)
                    .ToListAsync();

                HasResults = false;
                OverallPlace = standings.FindIndex(s => s.UserId == UserId) + 1;
                TotalPoints = standings.FirstOrDefault(s => s.UserId == UserId)?.TotalPoints ?? 0;

                DateTime latestResultsRaceDate = DateTime.Now;
                if (_context.RaceResults.Any())
                {
                    latestResultsRaceDate = _context.RaceResults.Max(rr => rr.Race.Date);
                }

                var recentRace = await _context.Races
                    .Where(r => r.Pool.Id == currentSeason.Id && r.Date <= latestResultsRaceDate)
                    .OrderByDescending(r => r.Date)
                    .FirstOrDefaultAsync();

                if (recentRace != null)
                {
                    RecentResults = await _context.RaceResults
                        .Where(r => r.RaceId == recentRace.Id)
                        .OrderBy(r => r.Place)
                        .Include(r => r.Driver)
                        .Include(r => r.Race)
                        .ToListAsync();
                }

                RacesWithResults = await _context.Races
                    .Where(r => r.Pool.Id == currentSeason.Id && _context.RaceResults.Any(rr => rr.RaceId == r.Id))
                    .OrderBy(r => r.Date)
                    .ToListAsync();

                if (RacesWithResults.Any())
                {
                    AllRecentResults = await _context.RaceResults
                        .Include(rr => rr.Driver)
                        .Include(rr => rr.Race)
                        .Where(rr => RacesWithResults.Select(r => r.Id).Contains(rr.RaceId))
                        .OrderByDescending(rr => rr.Race.Date)
                        .ThenBy(rr => rr.Place)
                        .ToListAsync();
                }

                CurrentRace = await _context.Races
                    .Where(r => r.Pool.Id == currentSeason.Id && r.Date >= DateTime.Today)
                    .OrderBy(r => r.Date)
                    .FirstOrDefaultAsync();

                if (CurrentRace != null)
                {
                    CurrentWeekPick = await _context.Picks
                        .Include(p => p.Pick1)
                        .Include(p => p.Pick2)
                        .Include(p => p.Pick3)
                        .FirstOrDefaultAsync(p => p.RaceId == CurrentRace.Id && p.UserId == UserId);

                    var orderedSeasonRaceIds = await _context.Races
                        .Where(r => r.Pool.Id == currentSeason.Id)
                        .OrderBy(r => r.Date)
                        .Select(r => r.Id)
                        .ToListAsync();

                    var idx = orderedSeasonRaceIds.IndexOf(CurrentRace.Id);
                    if (idx >= 0)
                    {
                        CurrentRaceNumber = idx + 1;
                    }
                }

                AvailableDrivers = await _context.Drivers
                    .Where(d => d.PoolId == currentSeason.Id)
                    .OrderBy(d => d.Name)
                    .ToListAsync();

                var user = await _userManager.Users
                    .Include(u => u.PrimaryDriverFirstHalf)
                    .Include(u => u.PrimaryDriverSecondHalf)
                    .FirstOrDefaultAsync(u => u.Id == UserId);

                PrimaryDriver = user?.PrimaryDriverFirstHalf;
                SecondHalfPrimaryDriver = user?.PrimaryDriverSecondHalf;

                FirstRace = await _context.Races
                    .Where(r => r.Pool.Id == currentSeason.Id)
                    .OrderBy(r => r.Date)
                    .FirstOrDefaultAsync();

                if (seasonRaceIds.Count > 0)
                {
                    int midpointIndex = seasonRaceIds.Count / 2;
                    var midpointRaceId = seasonRaceIds.OrderBy(id => id).ElementAt(midpointIndex);

                    SecondHalfFirstRace = await _context.Races
                        .Where(r => r.Id == midpointRaceId)
                        .FirstOrDefaultAsync();
                }

                if ((RecentResults == null || !RecentResults.Any()) && RacesWithResults.Any())
                {
                    var mostRecent = RacesWithResults.First();
                    RecentResults = await _context.RaceResults
                        .Where(r => r.RaceId == mostRecent.Id)
                        .OrderBy(r => r.Place)
                        .Include(r => r.Driver)
                        .Include(r => r.Race)
                        .ToListAsync();
                }

                if (FirstRace != null)
                {
                    var firstRaceResultDriverIds = await _context.RaceResults
                        .Where(rr => rr.RaceId == FirstRace.Id)
                        .Select(rr => rr.DriverId)
                        .ToListAsync();

                    var participantUserIds = await _context.Picks
                        .Where(p => seasonRaceIds.Contains(p.RaceId))
                        .Select(p => p.UserId)
                        .Distinct()
                        .ToListAsync();

                    var firstRacePicks = await _context.Picks
                        .Where(p => p.RaceId == FirstRace.Id && participantUserIds.Contains(p.UserId))
                        .ToListAsync();

                    if (!firstRaceResultDriverIds.Any())
                    {
                        HasResults = false;
                    }
                    else
                    {
                        bool allParticipantsHaveCompletePicksAndResults = participantUserIds.All(uid =>
                        {
                            var pick = firstRacePicks.FirstOrDefault(p => p.UserId == uid);
                            if (pick == null) return false;

                            return firstRaceResultDriverIds.Contains(pick.Pick1Id)
                                && firstRaceResultDriverIds.Contains(pick.Pick2Id)
                                && firstRaceResultDriverIds.Contains(pick.Pick3Id);
                        });

                        HasResults = allParticipantsHaveCompletePicksAndResults;
                    }
                }

                _logger.LogInformation("Dashboard loaded successfully for user {UserId}. OverallPlace: {Place}, TotalPoints: {Points}, CurrentRace: {Race}", 
                    UserId, OverallPlace, TotalPoints, CurrentRace?.Name ?? "None");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading dashboard for user {UserId}", UserId);
                throw;
            }
        }

        public Task<IActionResult> OnPostSetPrimaryDriverAsync(int driverId)
        {
            return HandleSetPrimaryDriverAsync(driverId, setSecondHalf: false);
        }

        public Task<IActionResult> OnPostSetPrimarySecondHalfDriverAsync(int secondHalfDriverId)
        {
            return HandleSetPrimaryDriverAsync(secondHalfDriverId, setSecondHalf: true);
        }

        private async Task<IActionResult> HandleSetPrimaryDriverAsync(int driverId, bool setSecondHalf)
        {
            try
            {
                UserId = _userManager.GetUserId(User);
                var halfType = setSecondHalf ? "Second Half" : "First Half";
                var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

                _logger.LogInformation("User {UserId} ({Email}) attempting to set primary driver ({HalfType}). DriverId: {DriverId}, IP: {IpAddress}", 
                    UserId, User.Identity?.Name ?? "Anonymous", halfType, driverId, ipAddress);

                var (user, selectedDriver, errorResult) = await TryGetUserAndDriverAsync(driverId);
                if (errorResult != null)
                {
                    _logger.LogWarning("Primary driver selection failed for user {UserId}. DriverId: {DriverId}, Half: {Half}, IP: {IpAddress}", 
                        UserId, driverId, halfType, ipAddress);
                    return errorResult!;
                }

                // Capture original values for comprehensive logging
                int? previousDriverId = null;
                string previousDriverName = null;
                string previousDriverCarNumber = null;

                if (setSecondHalf)
                {
                    previousDriverId = user.PrimaryDriverSecondHalfId;
                    if (user.PrimaryDriverSecondHalf != null)
                    {
                        previousDriverName = user.PrimaryDriverSecondHalf.Name;
                        previousDriverCarNumber = user.PrimaryDriverSecondHalf.CarNumber;
                    }
                    user.PrimaryDriverSecondHalf = selectedDriver;
                    user.PrimaryDriverSecondHalfId = selectedDriver.Id;
                }
                else
                {
                    previousDriverId = user.PrimaryDriverFirstHalfId;
                    if (user.PrimaryDriverFirstHalf != null)
                    {
                        previousDriverName = user.PrimaryDriverFirstHalf.Name;
                        previousDriverCarNumber = user.PrimaryDriverFirstHalf.CarNumber;
                    }
                    user.PrimaryDriverFirstHalf = selectedDriver;
                    user.PrimaryDriverFirstHalfId = selectedDriver.Id;
                }

                _context.Update(user);

                // Update picks and log the changes
                var updatedPicksCount = await UpdatePicksForUserAsync(user.Id, selectedDriver.Id, targetSecondHalf: setSecondHalf);
                
                await _context.SaveChangesAsync();

                var changeDescription = previousDriverId.HasValue 
                    ? $"{previousDriverName} (#{previousDriverCarNumber}) -> {selectedDriver.Name} (#{selectedDriver.CarNumber})" 
                    : $"None -> {selectedDriver.Name} (#{selectedDriver.CarNumber})";

                _logger.LogInformation("Primary driver ({HalfType}) set successfully. " +
                    "UserId: {UserId}, Email: {Email}, Change: {Change}, " +
                    "PreviousDriverId: {PreviousId}, NewDriverId: {NewId}, " +
                    "PicksUpdated: {PickCount}, IP: {IpAddress}", 
                    halfType, UserId, user.Email, changeDescription,
                    previousDriverId, selectedDriver.Id, updatedPicksCount, ipAddress);

                await OnGetAsync();
                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting primary driver for user {UserId}. DriverId: {DriverId}, SecondHalf: {SecondHalf}, IP: {IpAddress}", 
                    UserId, driverId, setSecondHalf, HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown");
                throw;
            }
        }

        private async Task<(ApplicationUser user, Driver driver, IActionResult? errorResult)> TryGetUserAndDriverAsync(int driverId)
        {
            var user = await _userManager.Users
                .Include(u => u.PrimaryDriverFirstHalf)
                .Include(u => u.PrimaryDriverSecondHalf)
                .FirstOrDefaultAsync(u => u.Id == _userManager.GetUserId(User));

            if (user == null)
            {
                _logger.LogWarning("User not found during primary driver selection. UserId: {UserId}", 
                    _userManager.GetUserId(User));
                return (null!, null!, NotFound());
            }

            var selectedDriver = await _context.Drivers.FirstOrDefaultAsync(d => d.Id == driverId);
            if (selectedDriver == null)
            {
                _logger.LogWarning("Driver not found during primary driver selection. DriverId: {DriverId}, UserId: {UserId}, Email: {Email}", 
                    driverId, user.Id, user.Email);
                ModelState.AddModelError(string.Empty, "Selected driver not found.");
                await OnGetAsync();
                return (user, null!, Page());
            }

            return (user, selectedDriver, null);
        }

        private async Task<int> UpdatePicksForUserAsync(string userId, int newPrimaryDriverId, bool targetSecondHalf = false)
        {
            try
            {
                var currentSeason = await _context.Pools
                    .OrderByDescending(p => p.Year)
                    .FirstOrDefaultAsync();

                if (currentSeason == null)
                {
                    _logger.LogWarning("No current season found while updating picks for user {UserId}", userId);
                    return 0;
                }

                var seasonRaces = await _context.Races
                    .Where(r => r.PoolId == currentSeason.Id)
                    .OrderBy(r => r.Date)
                    .ToListAsync();

                if (seasonRaces.Count == 0)
                {
                    _logger.LogWarning("No races found in current season while updating picks for user {UserId}", userId);
                    return 0;
                }

                int half = seasonRaces.Count / 2;

                // Determine target race ids based on requested half
                List<int> targetRaceIds;
                string halfDescription;
                if (targetSecondHalf)
                {
                    targetRaceIds = seasonRaces.Skip(half).Select(r => r.Id).ToList();
                    halfDescription = "Second Half";
                }
                else
                {
                    targetRaceIds = seasonRaces.Take(half).Select(r => r.Id).ToList();
                    halfDescription = "First Half";
                }

                if (!targetRaceIds.Any())
                {
                    _logger.LogWarning("No target races found for {Half} while updating picks for user {UserId}", 
                        halfDescription, userId);
                    return 0;
                }

                var picksToUpdate = await _context.Picks
                    .Where(p => p.UserId == userId && targetRaceIds.Contains(p.RaceId))
                    .ToListAsync();

                _logger.LogInformation("Updating picks for user {UserId} - {Half}. PicksToUpdate: {Count}, NewPrimaryDriverId: {DriverId}", 
                    userId, halfDescription, picksToUpdate.Count, newPrimaryDriverId);

                int updatedCount = 0;
                foreach (var pick in picksToUpdate)
                {
                    var oldPick1Id = pick.Pick1Id;
                    pick.Pick1Id = newPrimaryDriverId;

                    // Recalculate points for this pick using existing race results
                    var raceResults = await _context.RaceResults
                        .Where(rr => rr.RaceId == pick.RaceId)
                        .ToListAsync();

                    var oldPoints = pick.Points;
                    pick.CalculateTotalPoints(_context, raceResults);

                    _logger.LogDebug("Pick updated for user {UserId}. PickId: {PickId}, RaceId: {RaceId}, " +
                        "Pick1: {OldDriverId} -> {NewDriverId}, Points: {OldPoints} -> {NewPoints}", 
                        userId, pick.Id, pick.RaceId, oldPick1Id, newPrimaryDriverId, oldPoints, pick.Points);

                    updatedCount++;
                }

                _logger.LogInformation("Successfully updated {Count} picks for user {UserId} - {Half}", 
                    updatedCount, userId, halfDescription);

                return updatedCount;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating picks for user {UserId}. NewPrimaryDriverId: {DriverId}, SecondHalf: {SecondHalf}", 
                    userId, newPrimaryDriverId, targetSecondHalf);
                throw;
            }
        }
    }
}