using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using WebApp.Data;
using WebApp.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WebApp.Pages.DriverStats
{
    [Authorize]
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public IndexModel(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public string CurrentUserId { get; set; } = string.Empty;
        public List<UserInfo> Users { get; set; } = new();

        public class UserInfo
        {
            public string UserId { get; set; } = string.Empty;
            public string FirstName { get; set; } = string.Empty;
            public string LastName { get; set; } = string.Empty;
        }

        public class DriverFinishInfo
        {
            public int DriverId { get; set; }
            public string DriverName { get; set; } = string.Empty;
            public string CarNumber { get; set; } = string.Empty;
            public int FinishPosition { get; set; }
        }

        public class WeeklyDriverStats
        {
            public int WeekNumber { get; set; }
            public int RaceId { get; set; }
            public string RaceName { get; set; } = string.Empty;
            public List<DriverFinishInfo> DriverFinishes { get; set; } = new();
        }

        public class DriverSummary
        {
            public int DriverId { get; set; }
            public string DriverName { get; set; } = string.Empty;
            public string CarNumber { get; set; } = string.Empty;
            public int BestFinish { get; set; }
            public int WorstFinish { get; set; }
            public string AvgFinish { get; set; } = string.Empty;
            public int RacesCount { get; set; }
        }

        public class DriverStatsResponse
        {
            public List<WeeklyDriverStats> WeeklyStats { get; set; } = new();
            public List<DriverSummary> DriverSummaries { get; set; } = new();
        }

        public async Task OnGetAsync()
        {
            CurrentUserId = _userManager.GetUserId(User);

            var currentSeason = _context.Pools.AsEnumerable()
                .OrderByDescending(s => s.CurrentYear)
                .FirstOrDefault();

            if (currentSeason == null)
                return;

            // Get all users who are players in the current pool
            Users = await _context.Users
                .Where(u => u.IsPlayer && u.Pools.Any(p => p.Id == currentSeason.Id))
                .OrderBy(u => u.FirstName)
                .ThenBy(u => u.LastName)
                .Select(u => new UserInfo
                {
                    UserId = u.Id,
                    FirstName = u.FirstName,
                    LastName = u.LastName
                })
                .ToListAsync();

            // Ensure current user is in the list if they're a player
            if (!Users.Any(u => u.UserId == CurrentUserId))
            {
                var currentUser = await _userManager.GetUserAsync(User);
                if (currentUser != null && currentUser.IsPlayer)
                {
                    Users.Add(new UserInfo
                    {
                        UserId = currentUser.Id,
                        FirstName = currentUser.FirstName,
                        LastName = currentUser.LastName
                    });
                    Users = Users.OrderBy(u => u.FirstName).ThenBy(u => u.LastName).ToList();
                }
            }
        }

        public async Task<IActionResult> OnGetDriverStatsAsync(string userId)
        {
            if (string.IsNullOrEmpty(userId))
                return new JsonResult(new DriverStatsResponse());

            var currentSeason = _context.Pools.AsEnumerable()
                .OrderByDescending(s => s.CurrentYear)
                .FirstOrDefault();

            if (currentSeason == null)
                return new JsonResult(new DriverStatsResponse());

            var now = DateTime.UtcNow;

            // Get only races that have already occurred for the current season
            var seasonRaces = await _context.Races
                .Where(r => r.PoolId == currentSeason.Id && r.Date < now)
                .OrderBy(r => r.Date)
                .ToListAsync();

            if (!seasonRaces.Any())
                return new JsonResult(new DriverStatsResponse());

            var seasonRaceIds = seasonRaces.Select(r => r.Id).ToList();

            // Get user's picks for all races
            var userPicks = await _context.Picks
                .Include(p => p.Pick1)
                .Include(p => p.Pick2)
                .Include(p => p.Pick3)
                .Where(p => p.UserId == userId && seasonRaceIds.Contains(p.RaceId))
                .ToListAsync();

            if (!userPicks.Any())
                return new JsonResult(new DriverStatsResponse());

            // Get all driver IDs from user's picks
            var driverIds = new HashSet<int>();
            foreach (var pick in userPicks)
            {
                driverIds.Add(pick.Pick1Id);
                driverIds.Add(pick.Pick2Id);
                driverIds.Add(pick.Pick3Id);
            }

            // Get race results for these drivers
            var raceResults = await _context.RaceResults
                .Include(rr => rr.Driver)
                .Include(rr => rr.Race)
                .Where(rr => seasonRaceIds.Contains(rr.RaceId) && driverIds.Contains(rr.DriverId))
                .ToListAsync();

            // Build weekly stats
            var weeklyStats = new List<WeeklyDriverStats>();
            int weekNumber = 1;

            foreach (var race in seasonRaces)
            {
                var pick = userPicks.FirstOrDefault(p => p.RaceId == race.Id);
                if (pick == null)
                    continue;

                var raceDriverResults = raceResults.Where(rr => rr.RaceId == race.Id).ToList();
                if (!raceDriverResults.Any())
                    continue;

                var weekStats = new WeeklyDriverStats
                {
                    WeekNumber = weekNumber,
                    RaceId = race.Id,
                    RaceName = race.Name,
                    DriverFinishes = new List<DriverFinishInfo>()
                };

                // Add results for user's three picks
                var pickDriverIds = new[] { pick.Pick1Id, pick.Pick2Id, pick.Pick3Id };
                foreach (var driverId in pickDriverIds)
                {
                    var result = raceDriverResults.FirstOrDefault(rr => rr.DriverId == driverId);
                    if (result != null)
                    {
                        weekStats.DriverFinishes.Add(new DriverFinishInfo
                        {
                            DriverId = result.DriverId,
                            DriverName = result.Driver.Name,
                            CarNumber = result.Driver.CarNumber,
                            FinishPosition = result.Place
                        });
                    }
                }

                if (weekStats.DriverFinishes.Any())
                {
                    weeklyStats.Add(weekStats);
                    weekNumber++;
                }
            }

            // Build driver summaries
            var driverSummaries = new List<DriverSummary>();
            foreach (var driverId in driverIds)
            {
                var driverResults = raceResults.Where(rr => rr.DriverId == driverId).ToList();
                if (!driverResults.Any())
                    continue;

                var driver = driverResults.First().Driver;
                var finishes = driverResults.Select(rr => rr.Place).ToList();

                driverSummaries.Add(new DriverSummary
                {
                    DriverId = driver.Id,
                    DriverName = driver.Name,
                    CarNumber = driver.CarNumber,
                    BestFinish = finishes.Min(),
                    WorstFinish = finishes.Max(),
                    AvgFinish = finishes.Average().ToString("F1"),
                    RacesCount = finishes.Count
                });
            }

            driverSummaries = driverSummaries.OrderBy(ds => ds.DriverName).ToList();

            var response = new DriverStatsResponse
            {
                WeeklyStats = weeklyStats,
                DriverSummaries = driverSummaries
            };

            return new JsonResult(response);
        }
    }
}