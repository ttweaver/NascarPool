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

        public bool IsPrimaryDriverValid { get; set; }
        public bool IsSecondHalfPrimaryDriverValid { get; set; }

        // Tie information
        public bool IsTied { get; set; }
        public int TiedWithCount { get; set; }
        public List<string> TiedPlayerNames { get; set; } = new();

        public class PlayerRaceResult
        {
            public int Place { get; set; }
            public string PlayerName { get; set; } = string.Empty;
            public int Points { get; set; }
        }

        public IEnumerable<PlayerRaceResult>? CurrentWeekPlayerResults { get; set; }

        public Dictionary<int, HashSet<int>> UserPicksByRaceId { get; set; } = new();

        public int GetPrimaryDriverIdForCurrentRace()
        {
            if (CurrentRace == null) return 0;

            var currentSeason = GetCurrentSeasonFromCookie();
            if (currentSeason == null) return 0;

            var seasonRaces = _context.Races
                .Where(r => r.PoolId == currentSeason.Id)
                .OrderBy(r => r.Date)
                .ToList();

            if (!seasonRaces.Any()) return 0;

            int half = seasonRaces.Count / 2;
            int raceIndex = seasonRaces.FindIndex(r => r.Id == CurrentRace.Id);
            bool isFirstHalf = raceIndex >= 0 && raceIndex < half;

            return isFirstHalf ? (PrimaryDriver?.Id ?? 0) : (SecondHalfPrimaryDriver?.Id ?? 0);
        }

        private Pool GetCurrentSeasonFromCookie()
        {
            // Try to get poolId from cookie
            var poolIdCookie = Request.Cookies["poolId"];
            Pool currentSeason = null;

            if (!string.IsNullOrEmpty(poolIdCookie) && int.TryParse(poolIdCookie, out var cookiePoolId))
            {
                currentSeason = _context.Pools.Include(p => p.Members).FirstOrDefault(p => p.Id == cookiePoolId);
            }

            // Fallback to latest season if cookie not found or invalid
            if (currentSeason == null)
            {
                currentSeason = _context.Pools.Include(p => p.Members).AsEnumerable()
                    .OrderByDescending(s => s.CurrentYear)
                    .FirstOrDefault();
                
                if (currentSeason != null)
                {
                    _logger.LogInformation("Fallback to latest season. PoolId: {PoolId}, Year: {Year}", 
                        currentSeason.Id, currentSeason.Year);
                }
            }

            return currentSeason;
        }

        public async Task OnGetAsync()
        {
            try
            {
                UserId = _userManager.GetUserId(User);
                _logger.LogInformation("User {UserId} accessed Dashboard page", UserId);

                var currentSeason = GetCurrentSeasonFromCookie();

                if (currentSeason == null)
                {
                    _logger.LogWarning("No current season found for user {UserId}", UserId);
                    OverallPlace = 0;
                    TotalPoints = 0;
                    return;
                }

                // Get pool member IDs
                var poolMemberIds = currentSeason.Members
                    .Select(m => m.Id)
                    .ToList();

                // Only include races that have already occurred for standings calculations
                var completedSeasonRaceIds = await _context.Races
                    .Where(r => r.Pool.Id == currentSeason.Id && r.Date <= DateTime.Today)
                    .Select(r => r.Id)
                    .ToListAsync();
                
                // Total races includes all races (for display purposes)
                TotalRaces = await _context.Races
                    .Where(r => r.Pool.Id == currentSeason.Id)
                    .CountAsync();

                // Calculate standings only from completed races and only for pool members
                var standingsData = await _context.Picks
                    .Where(p => completedSeasonRaceIds.Contains(p.RaceId) && p.User.IsPlayer && poolMemberIds.Contains(p.UserId))
                    .GroupBy(p => p.UserId)
                    .Select(g => new { UserId = g.Key, TotalPoints = g.Sum(p => p.Points), User = g.First().User })
                    .OrderBy(s => s.TotalPoints) // Lower points = better rank
                    .ToListAsync();

                var standings = new List<(string UserId, string UserName, int TotalPoints, int Rank)>();
                int currentRank = 1;
                int? previousPoints = null;
                int playersAtCurrentRank = 0;

                foreach (var standing in standingsData)
                {
                    if (previousPoints.HasValue && standing.TotalPoints != previousPoints.Value)
                    {
                        // Points changed, so advance rank by number of players at previous rank
                        currentRank += playersAtCurrentRank;
                        playersAtCurrentRank = 0;
                    }

                    playersAtCurrentRank++;
                    var userName = $"{standing.User.FirstName} {standing.User.LastName}";
                    standings.Add((standing.UserId, userName, standing.TotalPoints, currentRank));
                    previousPoints = standing.TotalPoints;
                }

                HasResults = standings.Any();
                var userStanding = standings.FirstOrDefault(s => s.UserId == UserId);
                OverallPlace = userStanding.Rank;
                TotalPoints = userStanding.TotalPoints;

                // Calculate tie information
                var playersAtSameRank = standings.Where(s => s.Rank == OverallPlace && s.TotalPoints == TotalPoints).ToList();
                if (playersAtSameRank.Count > 1)
                {
                    IsTied = true;
                    TiedWithCount = playersAtSameRank.Count - 1; // Exclude current user
                    TiedPlayerNames = playersAtSameRank
                        .Where(s => s.UserId != UserId)
                        .Select(s => s.UserName)
                        .ToList();
                    
                    _logger.LogInformation("User {UserId} is tied with {TiedCount} other player(s) at rank {Rank} with {Points} points", 
                        UserId, TiedWithCount, OverallPlace, TotalPoints);
                }

                // Calculate current week player results (only for pool members)
                if (CurrentRace != null && CurrentRace.Date <= DateTime.Today)
                {
                    var currentWeekPicks = await _context.Picks
                        .Where(p => p.RaceId == CurrentRace.Id && p.User.IsPlayer && poolMemberIds.Contains(p.UserId))
                        .Include(p => p.User)
                        .OrderBy(p => p.Points) // Lower points = better rank
                        .ToListAsync();

                    if (currentWeekPicks.Any())
                    {
                        var weekResults = new List<PlayerRaceResult>();
                        int place = 1;
                        int? prevPoints = null;
                        int playersAtPlace = 0;

                        foreach (var pick in currentWeekPicks)
                        {
                            if (prevPoints.HasValue && pick.Points != prevPoints.Value)
                            {
                                place += playersAtPlace;
                                playersAtPlace = 0;
                            }

                            playersAtPlace++;
                            weekResults.Add(new PlayerRaceResult
                            {
                                Place = place,
                                PlayerName = $"{pick.User.FirstName} {pick.User.LastName}",
                                Points = pick.Points
                            });

                            prevPoints = pick.Points;
                        }

                        CurrentWeekPlayerResults = weekResults;
                    }
                }

                DateTime latestResultsRaceDate = DateTime.Now;
                if (_context.RaceResults.Any())
                {
                    latestResultsRaceDate = _context.RaceResults.Max(rr => rr.Race.Date);
                }

                var recentRace = await _context.Races
                    .Where(r => r.Pool.Id == currentSeason.Id && r.Date <= latestResultsRaceDate && r.Date <= DateTime.Today)
                    .OrderByDescending(r => r.Date)
                    .FirstOrDefaultAsync();

                if (recentRace != null)
                {
                    RecentResults = await _context.RaceResults
                        .Where(r => r.RaceId == recentRace.Id)
                        .Where(r => r.Race.Date <= DateTime.Today)
                        .OrderBy(r => r.Place)
                        .Include(r => r.Driver)
                        .Include(r => r.Race)
                        .ToListAsync();
                }

                RacesWithResults = await _context.Races
                    .Where(r => r.Pool.Id == currentSeason.Id && _context.RaceResults.Any(rr => rr.RaceId == r.Id))
                    .Where(r => r.Date.Date <= DateTime.Today)
                    .OrderBy(r => r.Date)
                    .ToListAsync();

                if (RacesWithResults.Any())
                {
                    AllRecentResults = await _context.RaceResults
                        .Include(rr => rr.Driver)
                        .Include(rr => rr.Race)
                        .Where(rr => RacesWithResults.Select(r => r.Id).Contains(rr.RaceId))
                        .Where(rr => rr.Race.Date <= DateTime.Today)
                        .OrderByDescending(rr => rr.Race.Date)
                        .ThenBy(rr => rr.Place)
                        .ToListAsync();
                }

                // Load user's picks for all races with results (only completed races)
                if (RacesWithResults.Any())
                {
                    var raceIdsWithResults = RacesWithResults.Select(r => r.Id).ToList();
                    var userPicks = await _context.Picks
                        .Where(p => p.UserId == UserId && raceIdsWithResults.Contains(p.RaceId))
                        .ToListAsync();

                    UserPicksByRaceId = userPicks.ToDictionary(
                        p => p.RaceId,
                        p => new HashSet<int> { p.Pick1Id, p.Pick2Id, p.Pick3Id }
                    );
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
                    .FirstOrDefaultAsync(u => u.Id == UserId);

                var currentSeasonFromCookie = GetCurrentSeasonFromCookie();
                if (currentSeasonFromCookie != null)
                {
                    var userPoolDriver = await _context.UserPoolPrimaryDrivers
                        .Include(uppd => uppd.PrimaryDriverFirstHalf)
                        .Include(uppd => uppd.PrimaryDriverSecondHalf)
                        .FirstOrDefaultAsync(uppd => uppd.UserId == UserId && uppd.PoolId == currentSeasonFromCookie.Id);
                    
                    PrimaryDriver = userPoolDriver?.PrimaryDriverFirstHalf;
                    SecondHalfPrimaryDriver = userPoolDriver?.PrimaryDriverSecondHalf;
                    
                    // Validate primary drivers belong to current season
                    IsPrimaryDriverValid = PrimaryDriver == null || PrimaryDriver.PoolId == currentSeason.Id;
                    IsSecondHalfPrimaryDriverValid = SecondHalfPrimaryDriver == null || SecondHalfPrimaryDriver.PoolId == currentSeason.Id;
                }

                FirstRace = await _context.Races
                    .Where(r => r.Pool.Id == currentSeason.Id)
                    .OrderBy(r => r.Date)
                    .FirstOrDefaultAsync();

                var allSeasonRaceIds = await _context.Races
                    .Where(r => r.Pool.Id == currentSeason.Id)
                    .OrderBy(r => r.Date)
                    .Select(r => r.Id)
                    .ToListAsync();

                if (allSeasonRaceIds.Count > 0)
                {
                    int midpointIndex = allSeasonRaceIds.Count / 2;
                    var midpointRaceId = allSeasonRaceIds.ElementAt(midpointIndex);

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

                    // Only include picks from completed races for participant determination (pool members only)
                    var participantUserIds = await _context.Picks
                        .Where(p => completedSeasonRaceIds.Contains(p.RaceId) && poolMemberIds.Contains(p.UserId))
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

                _logger.LogInformation("Dashboard loaded successfully for user {UserId}. OverallPlace: {Place}, TotalPoints: {Points}, IsTied: {IsTied}, CurrentRace: {Race}, Season: {SeasonYear}, PoolMembers: {MemberCount}", 
                    UserId, OverallPlace, TotalPoints, IsTied, CurrentRace?.Name ?? "None", currentSeason.Year, poolMemberIds.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading dashboard for user {UserId}", UserId);
                throw;
            }
        }

        public async Task<IActionResult> OnPostSavePicksAsync(int RaceId, int Pick1Id, int Pick2Id, int Pick3Id)
        {
            try
            {
                var userId = _userManager.GetUserId(User);
                var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

                _logger.LogInformation("User {UserId} ({Email}) attempting to save picks for race {RaceId}. " +
                    "Pick1Id: {Pick1}, Pick2Id: {Pick2}, Pick3Id: {Pick3}, IP: {IpAddress}", 
                    userId, User.Identity?.Name ?? "Anonymous", RaceId, Pick1Id, Pick2Id, Pick3Id, ipAddress);

                var race = await _context.Races.Include(r => r.Pool).FirstOrDefaultAsync(r => r.Id == RaceId);
                if (race == null)
                {
                    _logger.LogWarning("Race not found during pick save. RaceId: {RaceId}, UserId: {UserId}", 
                        RaceId, userId);
                    TempData["Error"] = "Race not found.";
                    return RedirectToPage();
                }

                // Validate race belongs to current season
                var currentSeason = GetCurrentSeasonFromCookie();
                if (currentSeason == null || race.PoolId != currentSeason.Id)
                {
                    _logger.LogWarning("User {UserId} attempted to save picks for race {RaceId} which does not belong to current season. " +
                        "Race PoolId: {RacePoolId}, Current Season: {CurrentSeasonId}, IP: {IpAddress}", 
                        userId, RaceId, race.PoolId, currentSeason?.Id, ipAddress);
                    TempData["Error"] = "You can only save picks for races in the current season.";
                    return RedirectToPage();
                }

                if (DateTime.Today >= race.Date)
                {
                    _logger.LogWarning("User {UserId} attempted to save picks on/after race day. RaceId: {RaceId}, RaceDate: {RaceDate}, IP: {IpAddress}", 
                        userId, RaceId, race.Date, ipAddress);
                    TempData["Error"] = "Picks cannot be entered on or after race day.";
                    return RedirectToPage();
                }

                if (string.IsNullOrEmpty(userId))
                {
                    _logger.LogWarning("User ID is null during pick save attempt. RaceId: {RaceId}", RaceId);
                    return Challenge();
                }

                // Validate picks
                if (Pick1Id == 0 || Pick2Id == 0 || Pick3Id == 0)
                {
                    _logger.LogWarning("Pick validation failed - missing driver selection. UserId: {UserId}, RaceId: {RaceId}, " +
                        "Pick1: {Pick1}, Pick2: {Pick2}, Pick3: {Pick3}, IP: {IpAddress}", 
                        userId, RaceId, Pick1Id, Pick2Id, Pick3Id, ipAddress);
                    TempData["Error"] = "All three picks are required.";
                    return RedirectToPage();
                }

                // Check for duplicate picks
                var picksSet = new HashSet<int> { Pick1Id, Pick2Id, Pick3Id };
                if (picksSet.Count < 3)
                {
                    _logger.LogWarning("Pick validation failed - duplicate drivers selected. UserId: {UserId}, RaceId: {RaceId}, " +
                        "Pick1: {Pick1}, Pick2: {Pick2}, Pick3: {Pick3}, IP: {IpAddress}", 
                        userId, RaceId, Pick1Id, Pick2Id, Pick3Id, ipAddress);
                    TempData["Error"] = "You cannot select the same driver multiple times.";
                    return RedirectToPage();
                }

                // Validate all drivers belong to current season
                var drivers = await _context.Drivers.Where(d => 
                    d.Id == Pick1Id || d.Id == Pick2Id || d.Id == Pick3Id).ToListAsync();
                
                if (drivers.Any(d => d.PoolId != currentSeason.Id))
                {
                    _logger.LogWarning("Pick validation failed - drivers not from current season. UserId: {UserId}, RaceId: {RaceId}, " +
                        "Pick1: {Pick1}, Pick2: {Pick2}, Pick3: {Pick3}, CurrentSeasonId: {SeasonId}, IP: {IpAddress}", 
                        userId, RaceId, Pick1Id, Pick2Id, Pick3Id, currentSeason.Id, ipAddress);
                    TempData["Error"] = "All selected drivers must be from the current season.";
                    return RedirectToPage();
                }

                var pick = await _context.Picks.FirstOrDefaultAsync(p => p.RaceId == RaceId && p.UserId == userId);

                // Get driver names for logging
                var pick1Name = drivers.FirstOrDefault(d => d.Id == Pick1Id)?.Name ?? $"ID:{Pick1Id}";
                var pick2Name = drivers.FirstOrDefault(d => d.Id == Pick2Id)?.Name ?? $"ID:{Pick2Id}";
                var pick3Name = drivers.FirstOrDefault(d => d.Id == Pick3Id)?.Name ?? $"ID:{Pick3Id}";

                if (pick == null)
                {
                    pick = new Pick
                    {
                        RaceId = RaceId,
                        UserId = userId,
                        Pick1Id = Pick1Id,
                        Pick2Id = Pick2Id,
                        Pick3Id = Pick3Id
                    };
                    _context.Picks.Add(pick);

                    _logger.LogInformation("New picks created for user {UserId} on race {RaceId} ({RaceName}). " +
                        "Pick1: {Pick1Name} (#{Pick1Car}), Pick2: {Pick2Name} (#{Pick2Car}), Pick3: {Pick3Name} (#{Pick3Car}), IP: {IpAddress}", 
                        userId, RaceId, race.Name, 
                        pick1Name, drivers.FirstOrDefault(d => d.Id == Pick1Id)?.CarNumber,
                        pick2Name, drivers.FirstOrDefault(d => d.Id == Pick2Id)?.CarNumber,
                        pick3Name, drivers.FirstOrDefault(d => d.Id == Pick3Id)?.CarNumber,
                        ipAddress);
                    
                    TempData["Success"] = "Your picks have been saved successfully!";
                }
                else
                {
                    // Capture original values for logging
                    var originalDrivers = await _context.Drivers.Where(d => 
                        d.Id == pick.Pick1Id || d.Id == pick.Pick2Id || d.Id == pick.Pick3Id).ToListAsync();
                    var oldPick1Name = originalDrivers.FirstOrDefault(d => d.Id == pick.Pick1Id)?.Name ?? $"ID:{pick.Pick1Id}";
                    var oldPick2Name = originalDrivers.FirstOrDefault(d => d.Id == pick.Pick2Id)?.Name ?? $"ID:{pick.Pick2Id}";
                    var oldPick3Name = originalDrivers.FirstOrDefault(d => d.Id == pick.Pick3Id)?.Name ?? $"ID:{pick.Pick3Id}";

                    pick.Pick1Id = Pick1Id;
                    pick.Pick2Id = Pick2Id;
                    pick.Pick3Id = Pick3Id;
                    _context.Picks.Update(pick);

                    _logger.LogInformation("Picks updated for user {UserId} on race {RaceId} ({RaceName}). " +
                        "Pick1: {OldPick1} -> {NewPick1}, Pick2: {OldPick2} -> {NewPick2}, Pick3: {OldPick3} -> {NewPick3}, IP: {IpAddress}", 
                        userId, RaceId, race.Name, 
                        oldPick1Name, pick1Name, 
                        oldPick2Name, pick2Name, 
                        oldPick3Name, pick3Name, 
                        ipAddress);

                    TempData["Success"] = "Your picks have been updated successfully!";
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation("Picks saved successfully for user {UserId} on race {RaceId}. PickId: {PickId}", 
                    userId, RaceId, pick.Id);

                return RedirectToPage();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving picks for race {RaceId}, User: {UserId}", 
                    RaceId, _userManager.GetUserId(User));
                TempData["Error"] = "An error occurred while saving your picks. Please try again.";
                return RedirectToPage();
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
            UserId = _userManager.GetUserId(User);
            var halfType = setSecondHalf ? "Second Half" : "First Half";
            
            var currentSeason = GetCurrentSeasonFromCookie();
            if (currentSeason == null)
            {
                _logger.LogWarning("No current season found for user {UserId} attempting to set primary driver", UserId);
                ModelState.AddModelError(string.Empty, "No active season found.");
                await OnGetAsync();
                return Page();
            }
            
            var selectedDriver = await _context.Drivers.FirstOrDefaultAsync(d => d.Id == driverId);
            if (selectedDriver == null)
            {
                _logger.LogWarning("Driver not found. DriverId: {DriverId}, UserId: {UserId}", driverId, UserId);
                ModelState.AddModelError(string.Empty, "Selected driver not found.");
                await OnGetAsync();
                return Page();
            }

            // Validate driver belongs to current season
            if (selectedDriver.PoolId != currentSeason.Id)
            {
                _logger.LogWarning("User {UserId} attempted to set primary driver {DriverId} which does not belong to current season. " +
                    "Driver PoolId: {DriverPoolId}, Current Season: {CurrentSeasonId}", 
                    UserId, driverId, selectedDriver.PoolId, currentSeason.Id);
                ModelState.AddModelError(string.Empty, "You can only set primary drivers from the current season.");
                await OnGetAsync();
                return Page();
            }
            
            // Get or create UserPoolPrimaryDriver record
            var userPoolDriver = await _context.UserPoolPrimaryDrivers
                .FirstOrDefaultAsync(uppd => uppd.UserId == UserId && uppd.PoolId == currentSeason.Id);
            
            if (userPoolDriver == null)
            {
                userPoolDriver = new UserPoolPrimaryDriver
                {
                    UserId = UserId,
                    PoolId = currentSeason.Id
                };
                _context.UserPoolPrimaryDrivers.Add(userPoolDriver);
                _logger.LogInformation("Creating new UserPoolPrimaryDriver record for user {UserId}, pool {PoolId}", 
                    UserId, currentSeason.Id);
            }
            
            if (setSecondHalf)
            {
                userPoolDriver.PrimaryDriverSecondHalfId = selectedDriver.Id;
                _logger.LogInformation("User {UserId} set second half primary driver to {DriverName} ({DriverId}) for season {SeasonId}", 
                    UserId, selectedDriver.Name, selectedDriver.Id, currentSeason.Id);
            }
            else
            {
                userPoolDriver.PrimaryDriverFirstHalfId = selectedDriver.Id;
                _logger.LogInformation("User {UserId} set first half primary driver to {DriverName} ({DriverId}) for season {SeasonId}", 
                    UserId, selectedDriver.Name, selectedDriver.Id, currentSeason.Id);
            }
            
            await _context.SaveChangesAsync();
            
            // Update picks logic - only updates picks for current season
            var updatedPicksCount = await UpdatePicksForUserAsync(UserId, selectedDriver.Id, targetSecondHalf: setSecondHalf, currentSeason.Id);
            
            TempData["Success"] = $"Your {halfType} primary driver has been set to {selectedDriver.Name}. {updatedPicksCount} pick(s) updated.";
            
            await OnGetAsync();
            return Page();
        }
        
        private async Task<int> UpdatePicksForUserAsync(string userId, int newPrimaryDriverId, bool targetSecondHalf = false, int? currentSeasonId = null)
        {
            try
            {
                var currentSeason = currentSeasonId.HasValue ? 
                    await _context.Pools.FindAsync(currentSeasonId.Value) : 
                    GetCurrentSeasonFromCookie();

                if (currentSeason == null)
                {
                    _logger.LogWarning("No current season found while updating picks for user {UserId}", userId);
                    return 0;
                }

                // Only update picks for the current season
                var seasonRaces = await _context.Races
                    .Where(r => r.PoolId == currentSeason.Id)
                    .OrderBy(r => r.Date)
                    .ToListAsync();

                if (seasonRaces.Count == 0)
                {
                    _logger.LogWarning("No races found in current season {SeasonId} while updating picks for user {UserId}", 
                        currentSeason.Id, userId);
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
                    _logger.LogWarning("No target races found for {Half} in season {SeasonId} while updating picks for user {UserId}", 
                        halfDescription, currentSeason.Id, userId);
                    return 0;
                }

                var picksToUpdate = await _context.Picks
                    .Where(p => p.UserId == userId && targetRaceIds.Contains(p.RaceId))
                    .ToListAsync();

                _logger.LogInformation("Updating picks for user {UserId} - {Half} of season {SeasonId}. PicksToUpdate: {Count}, NewPrimaryDriverId: {DriverId}", 
                    userId, halfDescription, currentSeason.Id, picksToUpdate.Count, newPrimaryDriverId);

                int updatedCount = 0;
                foreach (var pick in picksToUpdate)
                {
                    var oldPick1Id = pick.Pick1Id;
                    pick.Pick1Id = newPrimaryDriverId;

                    // Recalculate points for this pick using existing race results (only if race has occurred)
                    var race = await _context.Races.FirstOrDefaultAsync(r => r.Id == pick.RaceId);
                    if (race != null && race.Date <= DateTime.Today)
                    {
                        var raceResults = await _context.RaceResults
                            .Where(rr => rr.RaceId == pick.RaceId)
                            .ToListAsync();

                        var oldPoints = pick.Points;
                        pick.CalculateTotalPoints(_context, raceResults);

                        _logger.LogDebug("Pick updated for user {UserId}. PickId: {PickId}, RaceId: {RaceId}, " +
                            "Pick1: {OldDriverId} -> {NewDriverId}, Points: {OldPoints} -> {NewPoints}", 
                            userId, pick.Id, pick.RaceId, oldPick1Id, newPrimaryDriverId, oldPoints, pick.Points);
                    }
                    else
                    {
                        _logger.LogDebug("Pick updated for user {UserId}. PickId: {PickId}, RaceId: {RaceId}, " +
                            "Pick1: {OldDriverId} -> {NewDriverId}, Points: Not recalculated (race not completed)", 
                            userId, pick.Id, pick.RaceId, oldPick1Id, newPrimaryDriverId);
                    }

                    updatedCount++;
                }

                if (updatedCount > 0)
                {
                    await _context.SaveChangesAsync();
                }

                _logger.LogInformation("Successfully updated {Count} picks for user {UserId} - {Half} of season {SeasonId}", 
                    updatedCount, userId, halfDescription, currentSeason.Id);

                return updatedCount;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating picks for user {UserId}. NewPrimaryDriverId: {DriverId}, SecondHalf: {SecondHalf}, SeasonId: {SeasonId}", 
                    userId, newPrimaryDriverId, targetSecondHalf, currentSeasonId);
                throw;
            }
        }
    }
}