using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using WebApp.Data;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WebApp.Models;

namespace WebApp.Pages.Standings
{
    [Authorize]
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public IndexModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public List<StandingEntry> Standings { get; set; } = new();
        public string? CurrentUserEncouragementMessage { get; set; }
        public bool HasWeeklyPerformance { get; set; }
        public int CurrentWeekNumber { get; set; }
        public int TotalWeeks { get; set; }
        public List<WeekOption> AvailableWeeks { get; set; } = new();
        public bool HasRaceResults { get; set; }

        public class StandingEntry
        {
            public string UserId { get; set; } = string.Empty;
            public string FirstName { get; set; } = string.Empty;
            public string LastName { get; set; } = string.Empty;
            public int TotalPoints { get; set; }
            public int Place { get; set; }
            public bool IsTied { get; set; }
        }

        public class WeekOption
        {
            public int WeekNumber { get; set; }
            public string RaceName { get; set; } = string.Empty;
        }

        public class WeeklyPerformanceData
        {
            public int WeekNumber { get; set; }
            public int RaceId { get; set; }
            public string RaceName { get; set; } = string.Empty;
            public Dictionary<string, int> PlayerPlaces { get; set; } = new();
        }

        public class ChartDataResponse
        {
            public List<WeeklyPerformanceData> WeeklyData { get; set; } = new();
            public List<PlayerInfo> Players { get; set; } = new();
            public string? CurrentUserId { get; set; }
        }

        public class PlayerInfo
        {
            public string UserId { get; set; } = string.Empty;
            public string FirstName { get; set; } = string.Empty;
            public string LastName { get; set; } = string.Empty;
        }

        public class WeeklyStandingsResponse
        {
            public int WeekNumber { get; set; }
            public string RaceName { get; set; } = string.Empty;
            public List<StandingEntry> Standings { get; set; } = new();
            public int TotalWeeks { get; set; }
        }

        public class CumulativeProgressionData
        {
            public int WeekNumber { get; set; }
            public string RaceName { get; set; } = string.Empty;
            public Dictionary<string, int> PlayerPlaces { get; set; } = new();
            public Dictionary<string, int> PlayerCumulativePoints { get; set; } = new();
        }

        public class CumulativeProgressionResponse
        {
            public List<CumulativeProgressionData> WeeklyProgression { get; set; } = new();
            public List<PlayerInfo> Players { get; set; } = new();
            public string? CurrentUserId { get; set; }
        }

        private Pool? GetCurrentSeasonFromCookie()
        {
            // Try to get poolId from cookie
            var poolIdCookie = Request.Cookies["poolId"];
            Pool? currentSeason = null;

            if (!string.IsNullOrEmpty(poolIdCookie) && int.TryParse(poolIdCookie, out var cookiePoolId))
            {
                currentSeason = _context.Pools.Include(p => p.Members).FirstOrDefault(p => p.Id == cookiePoolId);
            }

            // Fallback to latest season if cookie not found or invalid
            if (currentSeason == null)
            {
                currentSeason = _context.Pools.Include(p => p.Members).AsEnumerable<Pool>()
                    .OrderByDescending(s => s.CurrentYear)
                    .FirstOrDefault();
            }

            return currentSeason;
        }

        public async Task OnGetAsync()
        {
            var currentSeason = GetCurrentSeasonFromCookie();

            if (currentSeason == null)
                return;

            // Get user IDs that are members of the current pool
            var poolMemberIds = currentSeason.Members
                .Select(pu => pu.Id)
                .ToList();

            // Check if there are any race results for the current season
            var hasAnyRaceResults = await _context.RaceResults
                .AnyAsync(rr => rr.Race.PoolId == currentSeason.Id);

            HasRaceResults = hasAnyRaceResults;

            if (!hasAnyRaceResults)
                return;

            var now = DateTime.UtcNow;

            // Only get races that have already occurred
            var seasonRaces = await _context.Races
                .Where(r => r.Pool.Id == currentSeason.Id && r.Date < now)
                .OrderBy(r => r.Date)
                .Select(r => new { r.Id, r.Name })
                .ToListAsync();

            var seasonRaceIds = seasonRaces.Select(r => r.Id).ToList();

            if (!seasonRaceIds.Any())
                return;

            // Calculate current week number based on races with picks that have occurred
            var racesWithPicks = await _context.Picks
                .Where(p => seasonRaceIds.Contains(p.RaceId) && poolMemberIds.Contains(p.UserId))
                .Select(p => p.RaceId)
                .Distinct()
                .ToListAsync();

            CurrentWeekNumber = racesWithPicks.Count;
            TotalWeeks = racesWithPicks.Count;

            // Build available weeks list
            int weekNum = 1;
            foreach (var race in seasonRaces)
            {
                if (racesWithPicks.Contains(race.Id))
                {
                    AvailableWeeks.Add(new WeekOption
                    {
                        WeekNumber = weekNum,
                        RaceName = race.Name
                    });
                    weekNum++;
                }
            }

            var standingsQuery = await _context.Picks
                .Where(p => seasonRaceIds.Contains(p.RaceId) && p.User.IsPlayer && poolMemberIds.Contains(p.UserId))
                .GroupBy(p => p.UserId)
                .Select(g => new
                {
                    UserId = g.Key,
                    TotalPoints = g.Sum(p => p.Points)
                })
                .OrderBy(s => s.TotalPoints) // LOWER points = BETTER rank
                .ToListAsync();

            // Get usernames for display - filter to only players who are pool members
            var userIds = standingsQuery.Select(s => s.UserId).ToList();
            var users = await _context.Users
                .Players()
                .Where(u => userIds.Contains(u.Id) && poolMemberIds.Contains(u.Id))
                .Select(u => new { u.Id, u.FirstName, u.LastName })
                .ToListAsync();

            var userDictionary = users.ToDictionary(u => u.Id);

            // Calculate places with tie handling
            Standings = CalculateStandingsWithTies(standingsQuery, userDictionary);

            // Check if we have weekly performance data
            HasWeeklyPerformance = seasonRaces.Any();

            // Calculate encouragement message for current user
            var currentUserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (!string.IsNullOrEmpty(currentUserId))
            {
                CurrentUserEncouragementMessage = await CalculateEncouragementMessage(currentUserId, seasonRaceIds, poolMemberIds);
            }
        }

        // Change the signature of CalculateStandingsWithTies to use generic type parameters instead of dynamic
        private List<StandingEntry> CalculateStandingsWithTies<TStanding, TUser>(
            List<TStanding> standingsData,
            Dictionary<string, TUser> userDictionary)
            where TStanding : class
            where TUser : class
        {
            var standings = new List<StandingEntry>();
            int currentPlace = 1;
            int? previousPoints = null;
            int playersAtCurrentPlace = 0;

            foreach (var standing in standingsData)
            {
                // Use reflection to access properties since TStanding is anonymous
                var userIdProp = standing.GetType().GetProperty("UserId");
                var totalPointsProp = standing.GetType().GetProperty("TotalPoints") ?? standing.GetType().GetProperty("Points");
                if (userIdProp == null || totalPointsProp == null)
                    continue;

                string userId = (string)userIdProp.GetValue(standing)!;
                int totalPoints = (int)totalPointsProp.GetValue(standing)!;

                if (!userDictionary.ContainsKey(userId))
                    continue;

                // If points changed from previous entry, advance place
                if (previousPoints.HasValue && totalPoints != previousPoints.Value)
                {
                    currentPlace += playersAtCurrentPlace;
                    playersAtCurrentPlace = 0;
                }

                playersAtCurrentPlace++;

                // Check if this player is tied with others at this place
                bool isTied = standingsData.Count(s =>
                {
                    var tp = s.GetType().GetProperty("TotalPoints") ?? s.GetType().GetProperty("Points");
                    return tp != null && (int)tp.GetValue(s)! == totalPoints;
                }) > 1;

                // Use reflection to access user properties
                var user = userDictionary[userId];
                var firstNameProp = user.GetType().GetProperty("FirstName");
                var lastNameProp = user.GetType().GetProperty("LastName");
                string firstName = firstNameProp != null ? (string)firstNameProp.GetValue(user)! : string.Empty;
                string lastName = lastNameProp != null ? (string)lastNameProp.GetValue(user)! : string.Empty;

                standings.Add(new StandingEntry
                {
                    UserId = userId,
                    FirstName = firstName,
                    LastName = lastName,
                    TotalPoints = totalPoints,
                    Place = currentPlace,
                    IsTied = isTied
                });

                previousPoints = totalPoints;
            }

            return standings;
        }

        public async Task<IActionResult> OnGetWeeklyStandingsAsync(int weekNumber)
        {
            var currentSeason = GetCurrentSeasonFromCookie();

            if (currentSeason == null)
                return new JsonResult(new WeeklyStandingsResponse());

            // Get user IDs that are members of the current pool
            var poolMemberIds = currentSeason.Members
                .Select(pu => pu.Id)
                .ToList();

            var now = DateTime.UtcNow;

            // Only get races that have already occurred
            var seasonRaces = await _context.Races
                .Where(r => r.Pool.Id == currentSeason.Id && r.Date < now)
                .OrderBy(r => r.Date)
                .Select(r => new { r.Id, r.Name })
                .ToListAsync();

            if (!seasonRaces.Any())
                return new JsonResult(new WeeklyStandingsResponse());

            var seasonRaceIds = seasonRaces.Select(r => r.Id).ToList();

            // Get races with picks to determine valid week numbers
            var racesWithPicks = await _context.Picks
                .Where(p => seasonRaceIds.Contains(p.RaceId) && poolMemberIds.Contains(p.UserId))
                .Select(p => p.RaceId)
                .Distinct()
                .ToListAsync();

            var validRaces = seasonRaces.Where(r => racesWithPicks.Contains(r.Id)).ToList();
            var totalWeeks = validRaces.Count;

            // Validate week number
            if (weekNumber < 1 || weekNumber > totalWeeks)
                return new JsonResult(new WeeklyStandingsResponse { TotalWeeks = totalWeeks });

            // Get the specific race for this week
            var targetRace = validRaces.ElementAtOrDefault(weekNumber - 1);

            if (targetRace == null)
                return new JsonResult(new WeeklyStandingsResponse { TotalWeeks = totalWeeks });

            // Get standings for ONLY this specific race (not cumulative)
            var standingsQuery = await _context.Picks
                .Where(p => p.RaceId == targetRace.Id && p.User.IsPlayer && poolMemberIds.Contains(p.UserId))
                .Select(p => new
                {
                    p.UserId,
                    Points = p.Points
                })
                .OrderBy(p => p.Points) // LOWER points = BETTER rank
                .ToListAsync();

            // Get usernames for display
            var userIds = standingsQuery.Select(s => s.UserId).ToList();
            var users = await _context.Users
                .Players()
                .Where(u => userIds.Contains(u.Id) && poolMemberIds.Contains(u.Id))
                .Select(u => new { u.Id, u.FirstName, u.LastName })
                .ToListAsync();

            var userDictionary = users.ToDictionary(u => u.Id);

            // Calculate places with tie handling
            var weeklyStandings = CalculateStandingsWithTies(standingsQuery, userDictionary);

            var response = new WeeklyStandingsResponse
            {
                WeekNumber = weekNumber,
                RaceName = targetRace.Name,
                Standings = weeklyStandings,
                TotalWeeks = totalWeeks
            };

            return new JsonResult(response);
        }

        public async Task<IActionResult> OnGetChartDataAsync()
        {
            var currentSeason = GetCurrentSeasonFromCookie();

            if (currentSeason == null)
                return new JsonResult(new ChartDataResponse());

            // Get user IDs that are members of the current pool
            var poolMemberIds = currentSeason.Members
                .Select(pu => pu.Id)
                .ToList();

            var now = DateTime.UtcNow;

            // Only get races that have already occurred
            var seasonRaces = await _context.Races
                .Where(r => r.Pool.Id == currentSeason.Id && r.Date < now)
                .OrderBy(r => r.Date)
                .Select(r => new { r.Id, r.Name })
                .ToListAsync();

            if (!seasonRaces.Any())
                return new JsonResult(new ChartDataResponse());

            // Calculate weekly performance
            var weeklyPerformance = new List<WeeklyPerformanceData>();
            int weekNumber = 1;
            foreach (var race in seasonRaces)
            {
                var racePicks = await _context.Picks
                    .Include(p => p.User)
                    .Where(p => p.RaceId == race.Id && p.User.IsPlayer && poolMemberIds.Contains(p.UserId))
                    .OrderBy(p => p.Points) // LOWER points = BETTER rank
                    .Select(p => new { p.UserId, p.Points })
                    .ToListAsync();

                if (!racePicks.Any())
                    continue;

                var weekData = new WeeklyPerformanceData
                {
                    WeekNumber = weekNumber,
                    RaceId = race.Id,
                    RaceName = race.Name,
                    PlayerPlaces = new Dictionary<string, int>()
                };

                // Calculate places with tie handling
                int currentPlace = 1;
                int? previousPoints = null;
                int playersAtCurrentPlace = 0;

                foreach (var pick in racePicks)
                {
                    // If points changed from previous entry, advance place
                    if (previousPoints.HasValue && pick.Points != previousPoints.Value)
                    {
                        currentPlace += playersAtCurrentPlace;
                        playersAtCurrentPlace = 0;
                    }

                    playersAtCurrentPlace++;
                    weekData.PlayerPlaces[pick.UserId] = currentPlace;
                    previousPoints = pick.Points;
                }

                weeklyPerformance.Add(weekData);
                weekNumber++;
            }

            // Get all players who participated and are pool members
            var seasonRaceIds = seasonRaces.Select(r => r.Id).ToList();
            var playerIds = await _context.Picks
                .Where(p => seasonRaceIds.Contains(p.RaceId) && p.User.IsPlayer && poolMemberIds.Contains(p.UserId))
                .Select(p => p.UserId)
                .Distinct()
                .ToListAsync();

            var players = await _context.Users
                .Players()
                .Where(u => playerIds.Contains(u.Id) && poolMemberIds.Contains(u.Id))
                .Select(u => new PlayerInfo
                {
                    UserId = u.Id,
                    FirstName = u.FirstName,
                    LastName = u.LastName
                })
                .ToListAsync();

            var currentUserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

            var response = new ChartDataResponse
            {
                WeeklyData = weeklyPerformance,
                Players = players,
                CurrentUserId = currentUserId
            };

            return new JsonResult(response);
        }

        public async Task<IActionResult> OnGetCumulativeProgressionAsync()
        {
            var currentSeason = GetCurrentSeasonFromCookie();

            if (currentSeason == null)
                return new JsonResult(new CumulativeProgressionResponse());

            // Get user IDs that are members of the current pool
            var poolMemberIds = currentSeason.Members
                .Select(pu => pu.Id)
                .ToList();

            var now = DateTime.UtcNow;

            // Only get races that have already occurred
            var seasonRaces = await _context.Races
                .Where(r => r.Pool.Id == currentSeason.Id && r.Date < now)
                .OrderBy(r => r.Date)
                .Select(r => new { r.Id, r.Name })
                .ToListAsync();

            if (!seasonRaces.Any())
                return new JsonResult(new CumulativeProgressionResponse());

            var seasonRaceIds = seasonRaces.Select(r => r.Id).ToList();

            // Get races with picks
            var racesWithPicks = await _context.Picks
                .Where(p => seasonRaceIds.Contains(p.RaceId) && poolMemberIds.Contains(p.UserId))
                .Select(p => p.RaceId)
                .Distinct()
                .ToListAsync();

            var validRaces = seasonRaces.Where(r => racesWithPicks.Contains(r.Id)).ToList();

            // Get all players who participated and are pool members
            var playerIds = await _context.Picks
                .Where(p => seasonRaceIds.Contains(p.RaceId) && p.User.IsPlayer && poolMemberIds.Contains(p.UserId))
                .Select(p => p.UserId)
                .Distinct()
                .ToListAsync();

            var players = await _context.Users
                .Players()
                .Where(u => playerIds.Contains(u.Id) && poolMemberIds.Contains(u.Id))
                .Select(u => new PlayerInfo
                {
                    UserId = u.Id,
                    FirstName = u.FirstName,
                    LastName = u.LastName
                })
                .ToListAsync();

            var weeklyProgression = new List<CumulativeProgressionData>();
            int weekNumber = 1;

            // Calculate cumulative standings after each week for all players
            foreach (var race in validRaces)
            {
                // Get races up to and including current race
                var racesUpToNow = validRaces.Take(weekNumber).Select(r => r.Id).ToList();

                // Calculate cumulative standings
                var cumulativeStandings = await _context.Picks
                    .Where(p => racesUpToNow.Contains(p.RaceId) && p.User.IsPlayer && poolMemberIds.Contains(p.UserId))
                    .GroupBy(p => p.UserId)
                    .Select(g => new
                    {
                        UserId = g.Key,
                        TotalPoints = g.Sum(p => p.Points)
                    })
                    .OrderBy(s => s.TotalPoints) // LOWER points = BETTER rank
                    .ToListAsync();

                var weekData = new CumulativeProgressionData
                {
                    WeekNumber = weekNumber,
                    RaceName = race.Name,
                    PlayerPlaces = new Dictionary<string, int>(),
                    PlayerCumulativePoints = new Dictionary<string, int>()
                };

                // Assign places with tie handling
                int currentPlace = 1;
                int? previousPoints = null;
                int playersAtCurrentPlace = 0;

                foreach (var standing in cumulativeStandings)
                {
                    // If points changed from previous entry, advance place
                    if (previousPoints.HasValue && standing.TotalPoints != previousPoints.Value)
                    {
                        currentPlace += playersAtCurrentPlace;
                        playersAtCurrentPlace = 0;
                    }

                    playersAtCurrentPlace++;
                    weekData.PlayerPlaces[standing.UserId] = currentPlace;
                    weekData.PlayerCumulativePoints[standing.UserId] = standing.TotalPoints;
                    previousPoints = standing.TotalPoints;
                }

                weeklyProgression.Add(weekData);
                weekNumber++;
            }

            var currentUserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

            var response = new CumulativeProgressionResponse
            {
                WeeklyProgression = weeklyProgression,
                Players = players,
                CurrentUserId = currentUserId
            };

            return new JsonResult(response);
        }

        private async Task<string?> CalculateEncouragementMessage(string userId, List<int> seasonRaceIds, List<string> poolMemberIds)
        {
            // Need at least 2 races with results to compare week-over-week
            if (seasonRaceIds.Count < 2)
                return null;

            // Check if there are actually results for multiple races
            var racesWithResults = await _context.Picks
                .Where(p => seasonRaceIds.Contains(p.RaceId) && p.Points > 0 && poolMemberIds.Contains(p.UserId))
                .Select(p => p.RaceId)
                .Distinct()
                .CountAsync();

            // If only one race has results, it's effectively week one - don't compare
            if (racesWithResults < 2)
                return null;

            // Get current standings after last race
            var currentStanding = Standings.FirstOrDefault(s => s.UserId == userId);
            if (currentStanding == null)
                return null;

            // Calculate standings after previous race (excluding last race)
            var previousRaceIds = await _context.Picks
                .Where(p => seasonRaceIds.Contains(p.RaceId) && poolMemberIds.Contains(p.UserId))
                .Select(p => new { p.RaceId, p.Race.Date })
                .Distinct()
                .OrderByDescending(x => x.Date)
                .Skip(1)
                .Take(seasonRaceIds.Count - 1)
                .Select(x => x.RaceId)
                .ToListAsync();

            var previousStandingsQuery = await _context.Picks
                .Where(p => previousRaceIds.Contains(p.RaceId) && p.User.IsPlayer && poolMemberIds.Contains(p.UserId))
                .GroupBy(p => p.UserId)
                .Select(g => new
                {
                    UserId = g.Key,
                    TotalPoints = g.Sum(p => p.Points)
                })
                .OrderBy(s => s.TotalPoints) // LOWER points = BETTER rank
                .ToListAsync();

            // Calculate previous place with tie handling
            int previousPlace = 1;
            int? previousPoints = null;
            int playersAtCurrentPlace = 0;
            int? userPreviousPlace = null;

            foreach (var standing in previousStandingsQuery)
            {
                // If points changed from previous entry, advance place
                if (previousPoints.HasValue && standing.TotalPoints != previousPoints.Value)
                {
                    previousPlace += playersAtCurrentPlace;
                    playersAtCurrentPlace = 0;
                }

                playersAtCurrentPlace++;

                if (standing.UserId == userId)
                {
                    userPreviousPlace = previousPlace;
                    break;
                }

                previousPoints = standing.TotalPoints;
            }

            if (!userPreviousPlace.HasValue)
                return null; // User wasn't in previous standings

            int placeDifference = userPreviousPlace.Value - currentStanding.Place;

            // Generate encouraging messages based on performance
            if (placeDifference > 0)
            {
                // User moved up
                if (placeDifference >= 5)
                    return $"🚀 Amazing! You jumped up {placeDifference} places since last week! Keep up the great work!";
                else if (placeDifference >= 3)
                    return $"🎉 Fantastic! You climbed {placeDifference} spots since last week! You're on fire!";
                else
                    return $"📈 Nice work! You moved up {placeDifference} place{(placeDifference > 1 ? "s" : "")} since last week!";
            }
            else if (placeDifference < 0)
            {
                // User moved down
                var absPlaceDiff = Math.Abs(placeDifference);
                if (currentStanding.Place <= 3)
                    return $"💪 You slipped {absPlaceDiff} spot{(absPlaceDiff > 1 ? "s" : "")}, but you're still in the top {currentStanding.Place}! Great job!";
                else if (absPlaceDiff == 1)
                    return $"😊 You dropped one spot, but don't worry—there's plenty of season left to bounce back!";
                else
                    return $"🏁 You're down {absPlaceDiff} spots this week, but keep your head up! Consistency wins championships!";
            }
            else
            {
                // Same place
                if (currentStanding.Place == 1)
                    return "🏆 Still holding onto first place! Excellent work defending your position!";
                else if (currentStanding.Place <= 3)
                    return $"⭐ You're steady in {GetOrdinal(currentStanding.Place)} place! Consistency is key!";
                else
                    return "💯 You maintained your position! Keep grinding—your picks are working!";
            }
        }

        private string GetOrdinal(int number)
        {
            if (number <= 0) return number.ToString();

            switch (number % 100)
            {
                case 11:
                case 12:
                case 13:
                    return number + "th";
            }

            switch (number % 10)
            {
                case 1:
                    return number + "st";
                case 2:
                    return number + "nd";
                case 3:
                    return number + "rd";
                default:
                    return number + "th";
            }
        }
    }
}