using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using WebApp.Data;
using WebApp.Models;

namespace WebApp.Areas.Manage.Pages.Races.Picks
{
    public class EditModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<EditModel> _logger;

        public EditModel(ApplicationDbContext context, ILogger<EditModel> logger)
        {
            _context = context;
            _logger = logger;
        }

        [BindProperty(SupportsGet = true)]
        public int RaceId { get; set; }
        public Race? Race { get; set; }
        public List<ApplicationUser> Users { get; set; } = new();
        public List<Driver> Drivers { get; set; } = new();
        public Dictionary<string, WebApp.Models.Pick> UserPicks { get; set; } = new();

        public List<Race> Races { get; set; } = new();
        public Dictionary<string, int?> UserDefaultPick1 { get; set; } = new();

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
                _logger.LogInformation("Admin {UserId} ({Email}) accessing bulk pick edit for race {RaceId}", 
                    User.Identity?.Name ?? "Anonymous", User.Identity?.Name ?? "Anonymous", RaceId);

                Race = await _context.Races.Include(r => r.Pool.Members).FirstOrDefaultAsync(r => r.Id == RaceId);
                if (Race == null)
                {
                    _logger.LogWarning("Race not found during bulk pick edit. RaceId: {RaceId}, Admin: {AdminEmail}", 
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

                var memberIds = Race.Pool.Members.Select(m => m.Id).ToList();
                Users = await _context.Users
                    .Where(u => memberIds.Contains(u.Id))
                    .OrderBy(u => u.FirstName)
                    .ThenBy(u => u.LastName)
                    .ToListAsync();

                Drivers = await _context.Drivers
                    .Where(d => d.Pool.Id == Race.Pool.Id)
                    .OrderBy(d => d.Name)
                    .ToListAsync();

                var picks = await _context.Picks
                    .Where(p => p.RaceId == RaceId)
                    .ToListAsync();

                UserPicks = picks.ToDictionary(p => p.UserId, p => p);

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

                // Load all primary driver assignments for this pool
                var primaryDriverAssignments = await _context.UserPoolPrimaryDrivers
                    .Where(uppd => uppd.PoolId == Race.PoolId)
                    .ToListAsync();

                UserDefaultPick1 = new Dictionary<string, int?>();
                foreach (var user in Users)
                {
                    int? primaryDriverId = null;
                    var userPoolDriver = primaryDriverAssignments.FirstOrDefault(uppd => uppd.UserId == user.Id);
                    
                    if (userPoolDriver != null)
                    {
                        if (isFirstHalf)
                            primaryDriverId = userPoolDriver.PrimaryDriverFirstHalfId;
                        else
                            primaryDriverId = userPoolDriver.PrimaryDriverSecondHalfId;
                    }
                    
                    UserDefaultPick1[user.Id] = primaryDriverId;
                }

                _logger.LogInformation("Bulk pick edit page loaded successfully. RaceId: {RaceId}, Race: {RaceName}, PoolId: {PoolId}, UserCount: {UserCount}, ExistingPickCount: {PickCount}", 
                    RaceId, Race.Name, Race.PoolId, Users.Count, picks.Count);

                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading bulk pick edit page for race {RaceId}", RaceId);
                throw;
            }
        }

        public async Task<IActionResult> OnPostAsync()
        {
            try
            {
                var adminEmail = User.Identity?.Name ?? "Anonymous";
                var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

                _logger.LogInformation("Admin {AdminEmail} attempting to save bulk picks for race {RaceId}, IP: {IpAddress}", 
                    adminEmail, RaceId, ipAddress);

                Race = await _context.Races.Include(r => r.Pool.Members).FirstOrDefaultAsync(r => r.Id == RaceId);
                if (Race == null)
                {
                    _logger.LogWarning("Race not found during bulk pick save. RaceId: {RaceId}, Admin: {AdminEmail}", 
                        RaceId, adminEmail);
                    return NotFound();
                }

                var memberIds = Race.Pool.Members.Select(m => m.Id).ToList();
                Users = await _context.Users
                    .Where(u => memberIds.Contains(u.Id))
                    .OrderBy(u => u.FirstName)
                    .ThenBy(u => u.LastName)
                    .ToListAsync();

                Drivers = await _context.Drivers
                    .Where(d => d.Pool.Id == Race.Pool.Id)
                    .ToListAsync();

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

                // Load all primary driver assignments for this pool
                var primaryDriverAssignments = await _context.UserPoolPrimaryDrivers
                    .Where(uppd => uppd.PoolId == Race.PoolId)
                    .ToListAsync();

                UserDefaultPick1 = new Dictionary<string, int?>();
                foreach (var user in Users)
                {
                    int? primaryDriverId = null;
                    var userPoolDriver = primaryDriverAssignments.FirstOrDefault(uppd => uppd.UserId == user.Id);
                    
                    if (userPoolDriver != null)
                    {
                        if (isFirstHalf)
                            primaryDriverId = userPoolDriver.PrimaryDriverFirstHalfId;
                        else
                            primaryDriverId = userPoolDriver.PrimaryDriverSecondHalfId;
                    }
                    
                    UserDefaultPick1[user.Id] = primaryDriverId;
                }

                var userIds = Request.Form["UserIds"].ToArray();
                var pick1Ids = Request.Form["Pick1Ids"].ToArray();
                var pick2Ids = Request.Form["Pick2Ids"].ToArray();
                var pick3Ids = Request.Form["Pick3Ids"].ToArray();

                int createdCount = 0;
                int updatedCount = 0;
                int errorCount = 0;

                for (int i = 0; i < userIds.Length; i++)
                {
                    var userId = userIds[i];
                    var user = Users.FirstOrDefault(u => u.Id == userId);
                    var userName = user != null ? $"{user.FirstName} {user.LastName}" : userId;

                    int.TryParse(pick1Ids[i], out var pick1Id);
                    int.TryParse(pick2Ids[i], out var pick2Id);
                    int.TryParse(pick3Ids[i], out var pick3Id);

                    if (pick1Id == 0 || pick2Id == 0 || pick3Id == 0)
                    {
                        _logger.LogWarning("Bulk pick validation failed - missing drivers. User: {UserName} ({UserId}), Race: {RaceId}, Pick1: {Pick1}, Pick2: {Pick2}, Pick3: {Pick3}, Admin: {AdminEmail}", 
                            userName, userId, RaceId, pick1Id, pick2Id, pick3Id, adminEmail);
                        ModelState.AddModelError(string.Empty, $"All picks required for user {userName}.");
                        errorCount++;
                        continue;
                    }

                    var picksSet = new HashSet<int> { pick1Id, pick2Id, pick3Id };
                    if (picksSet.Count < 3)
                    {
                        _logger.LogWarning("Bulk pick validation failed - duplicate drivers. User: {UserName} ({UserId}), Race: {RaceId}, Picks: {Pick1}, {Pick2}, {Pick3}, Admin: {AdminEmail}", 
                            userName, userId, RaceId, pick1Id, pick2Id, pick3Id, adminEmail);
                        ModelState.AddModelError(string.Empty, $"Duplicate picks detected for user {userName}.");
                        errorCount++;
                        continue;
                    }

                    var existingPick = await _context.Picks.FirstOrDefaultAsync(p => p.RaceId == RaceId && p.UserId == userId);

                    // Get driver names for logging
                    var drivers = await _context.Drivers.Where(d => 
                        d.Id == pick1Id || d.Id == pick2Id || d.Id == pick3Id).ToListAsync();
                    var pick1Name = drivers.FirstOrDefault(d => d.Id == pick1Id)?.Name ?? $"ID:{pick1Id}";
                    var pick2Name = drivers.FirstOrDefault(d => d.Id == pick2Id)?.Name ?? $"ID:{pick2Id}";
                    var pick3Name = drivers.FirstOrDefault(d => d.Id == pick3Id)?.Name ?? $"ID:{pick3Id}";

                    if (existingPick != null)
                    {
                        // Capture original values
                        var originalDrivers = await _context.Drivers.Where(d => 
                            d.Id == existingPick.Pick1Id || d.Id == existingPick.Pick2Id || d.Id == existingPick.Pick3Id).ToListAsync();
                        var oldPick1 = originalDrivers.FirstOrDefault(d => d.Id == existingPick.Pick1Id)?.Name ?? $"ID:{existingPick.Pick1Id}";
                        var oldPick2 = originalDrivers.FirstOrDefault(d => d.Id == existingPick.Pick2Id)?.Name ?? $"ID:{existingPick.Pick2Id}";
                        var oldPick3 = originalDrivers.FirstOrDefault(d => d.Id == existingPick.Pick3Id)?.Name ?? $"ID:{existingPick.Pick3Id}";

                        existingPick.Pick1Id = pick1Id;
                        existingPick.Pick2Id = pick2Id;
                        existingPick.Pick3Id = pick3Id;
                        _context.Picks.Update(existingPick);

                        _logger.LogInformation("Pick updated via bulk edit. User: {UserName} ({UserId}), Race: {RaceId} ({RaceName}), " +
                            "Pick1: {OldPick1} -> {NewPick1}, Pick2: {OldPick2} -> {NewPick2}, Pick3: {OldPick3} -> {NewPick3}, " +
                            "Admin: {AdminEmail}, IP: {IpAddress}", 
                            userName, userId, RaceId, Race.Name, 
                            oldPick1, pick1Name, oldPick2, pick2Name, oldPick3, pick3Name, 
                            adminEmail, ipAddress);
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

                        _logger.LogInformation("Pick created via bulk edit. User: {UserName} ({UserId}), Race: {RaceId} ({RaceName}), " +
                            "Pick1: {Pick1}, Pick2: {Pick2}, Pick3: {Pick3}, Admin: {AdminEmail}, IP: {IpAddress}", 
                            userName, userId, RaceId, Race.Name, 
                            pick1Name, pick2Name, pick3Name, 
                            adminEmail, ipAddress);
                    }

                    // Save changes immediately after validating each user
                    await _context.SaveChangesAsync();

                    if (existingPick != null)
                        updatedCount++;
                    else
                        createdCount++;
                }

                if (errorCount > 0)
                {
                    _logger.LogWarning("Bulk pick save had {ErrorCount} validation errors. RaceId: {RaceId}, Admin: {AdminEmail}", 
                        errorCount, RaceId, adminEmail);
                }

                _logger.LogInformation("Bulk pick save completed. RaceId: {RaceId}, Race: {RaceName}, PoolId: {PoolId}, " +
                    "Created: {CreatedCount}, Updated: {UpdatedCount}, Errors: {ErrorCount}, Admin: {AdminEmail}, IP: {IpAddress}", 
                    RaceId, Race.Name, Race.PoolId, createdCount, updatedCount, errorCount, adminEmail, ipAddress);

                var totalSaved = createdCount + updatedCount;
                if (errorCount > 0)
                {
                    TempData["SuccessMessage"] = $"Saved {totalSaved} pick(s) successfully. {errorCount} row(s) had validation errors and were skipped.";
                }
                else
                {
                    TempData["SuccessMessage"] = $"All picks saved successfully! ({totalSaved} pick(s))";
                }

                return RedirectToPage(new { raceId = RaceId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during bulk pick save for race {RaceId}, Admin: {AdminEmail}", 
                    RaceId, User.Identity?.Name ?? "Anonymous");
                throw;
            }
        }
    }
}