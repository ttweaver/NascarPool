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

namespace WebApp.Pages
{
    [Authorize] // Require user to be logged in
    public class DashboardModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public DashboardModel(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public string UserId { get; set; }
        public int OverallPlace { get; set; }
        public int TotalPoints { get; set; }
        public List<RaceResult> RecentResults { get; set; } = new();
        public Pick? CurrentWeekPick { get; set; }
        public Race? CurrentRace { get; set; }
        public bool HasResults { get; set; }

        public Driver PrimaryDriver { get; set; }
        public Race FirstRace { get; set; }
        public List<Driver> AvailableDrivers { get; set; }
        public Driver SecondHalfPrimaryDriver { get; set; } // Add this property to your DashboardModel class
        public Race SecondHalfFirstRace { get; set; } // Add this property to your DashboardModel class if it is missing

        public async Task OnGetAsync()
        {
            UserId = _userManager.GetUserId(User);

            // Get current season (assumes a Season entity and a way to determine current season)
            var currentSeason = _context.Pools.AsEnumerable()
                .OrderByDescending(s => s.CurrentYear)
                .FirstOrDefault();

            if (currentSeason == null)
            {
                OverallPlace = 0;
                TotalPoints = 0;
                return;
            }

            // Only get picks for the current season
            var seasonRaceIds = await _context.Races
                .Where(r => r.Pool.Id == currentSeason.Id)
                .Select(r => r.Id)
                .ToListAsync();

            var standings = await _context.Picks
                .Where(p => seasonRaceIds.Contains(p.RaceId))
                .GroupBy(p => p.UserId)
                .Select(g => new { UserId = g.Key, TotalPoints = g.Sum(p => p.Points) })
                .OrderBy(s => s.TotalPoints)
                .ToListAsync();

            // initialize; will be computed properly once first race & results are loaded below
            HasResults = false;
            OverallPlace = standings.FindIndex(s => s.UserId == UserId) + 1;
            TotalPoints = standings.FirstOrDefault(s => s.UserId == UserId)?.TotalPoints ?? 0;

            var recentRace = await _context.Races
                .Where(r => r.Pool.Id == currentSeason.Id)
                .OrderByDescending(r => r.Date)
                .FirstOrDefaultAsync();

            if (recentRace != null)
            {
                RecentResults = await _context.RaceResults
                    .Where(r => r.RaceId == recentRace.Id)
                    .OrderBy(r => r.Place)
                    .Include(r => r.Driver)
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
            }

            // Load PrimaryDriver, FirstRace, and AvailableDrivers from your data source

            // Get all drivers for the current season's pool
            AvailableDrivers = await _context.Drivers
                .Where(d => d.PoolId == currentSeason.Id)
                .OrderBy(d => d.Name)
                .ToListAsync();

            // Get the user's primary driver (assumes a PrimaryDriverId property on ApplicationUser)
            var user = await _userManager.Users
                .FirstOrDefaultAsync(u => u.Id == UserId);

            PrimaryDriver = user.PrimaryDriverFirstHalf;
            SecondHalfPrimaryDriver = user.PrimaryDriverSecondHalf;

            // Get the first race of the current season
            FirstRace = await _context.Races
                .Where(r => r.Pool.Id == currentSeason.Id)
                .OrderBy(r => r.Date)
                .FirstOrDefaultAsync();

            // Get the first race of the second half of the season (midpoint logic)
            if (seasonRaceIds.Count > 0)
            {
                int midpointIndex = seasonRaceIds.Count / 2;
                var midpointRaceId = seasonRaceIds.OrderBy(id => id).ElementAt(midpointIndex);

                SecondHalfFirstRace = await _context.Races
                    .Where(r => r.Id == midpointRaceId)
                    .FirstOrDefaultAsync();
            }

            // --- New: Compute HasResults per requirement ---
            // HasResults should be true if:
            //  - the first race exists,
            //  - every participant (anyone who has picks in this season) has submitted a pick for the first race with all pick slots filled,
            //  - and race results for the first race contain entries for each driver chosen by those picks.
            if (FirstRace != null)
            {
                // driver ids that have results for the first race
                var firstRaceResultDriverIds = await _context.RaceResults
                    .Where(rr => rr.RaceId == FirstRace.Id)
                    .Select(rr => rr.DriverId)
                    .ToListAsync();

                // participants in the season (anyone who has submitted picks in this season)
                var participantUserIds = await _context.Picks
                    .Where(p => seasonRaceIds.Contains(p.RaceId))
                    .Select(p => p.UserId)
                    .Distinct()
                    .ToListAsync();

                // picks for the first race for those participants
                var firstRacePicks = await _context.Picks
                    .Where(p => p.RaceId == FirstRace.Id && participantUserIds.Contains(p.UserId))
                    .ToListAsync();

                // if no results exist for the first race, HasResults must be false
                if (!firstRaceResultDriverIds.Any())
                {
                    HasResults = false;
                }
                else
                {
                    // verify every participant has a pick for the first race and that all pick driver ids exist in race results
                    bool allParticipantsHaveCompletePicksAndResults = participantUserIds.All(uid =>
                    {
                        var pick = firstRacePicks.FirstOrDefault(p => p.UserId == uid);
                        if (pick == null) return false;

                        // pick.Pick1Id/Pick2Id/Pick3Id are ints on the model; ensure they are present in results
                        // guard against default zero or unexpected values by checking presence in result set
                        return firstRaceResultDriverIds.Contains(pick.Pick1Id)
                            && firstRaceResultDriverIds.Contains(pick.Pick2Id)
                            && firstRaceResultDriverIds.Contains(pick.Pick3Id);
                    });

                    HasResults = allParticipantsHaveCompletePicksAndResults;
                }
            }
        }

        public Task<IActionResult> OnPostSetPrimaryDriverAsync(int driverId)
        {
            // set first-half primary driver and update first-half picks
            return HandleSetPrimaryDriverAsync(driverId, setSecondHalf: false);
        }

        public Task<IActionResult> OnPostSetPrimarySecondHalfDriverAsync(int secondHalfDriverId)
        {
            // set second-half primary driver and update picks in the selected half
            return HandleSetPrimaryDriverAsync(secondHalfDriverId, setSecondHalf: true);
        }

        // ----------------- extracted helpers -----------------

        private async Task<IActionResult> HandleSetPrimaryDriverAsync(int driverId, bool setSecondHalf)
        {
            UserId = _userManager.GetUserId(User);

            var (user, selectedDriver, errorResult) = await TryGetUserAndDriverAsync(driverId);
            if (errorResult != null) return errorResult!; // already set ModelState or NotFound

            // Apply the driver to the correct primary field and persist user
            if (setSecondHalf)
            {
                user.PrimaryDriverSecondHalf = selectedDriver;
                user.PrimaryDriverSecondHalfId = selectedDriver.Id;
            }
            else
            {
                user.PrimaryDriverFirstHalf = selectedDriver;
                user.PrimaryDriverFirstHalfId = selectedDriver.Id;
            }

            _context.Update(user);


            // Update picks in the target half (first or second) to use the provided driver as Pick1
            await UpdatePicksForUserAsync(user.Id, selectedDriver.Id, targetSecondHalf: setSecondHalf);

			await _context.SaveChangesAsync();

			// After saving and updating picks, reload the page data
			await OnGetAsync();
            return Page();
        }

        private async Task<(ApplicationUser user, Driver driver, IActionResult? errorResult)> TryGetUserAndDriverAsync(int driverId)
        {
            var user = await _userManager.Users.FirstOrDefaultAsync(u => u.Id == _userManager.GetUserId(User));
            if (user == null)
            {
                return (null!, null!, NotFound());
            }

            var selectedDriver = await _context.Drivers.FirstOrDefaultAsync(d => d.Id == driverId);
            if (selectedDriver == null)
            {
                ModelState.AddModelError(string.Empty, "Selected driver not found.");
                await OnGetAsync();
                return (user, null!, Page());
            }

            return (user, selectedDriver, null);
        }

        /// <summary>
        /// Updates picks for the specified user in either the first half or second half of the current season.
        /// When <paramref name="targetSecondHalf"/> is false updates first-half races, otherwise updates second-half races.
        /// </summary>
        private async Task UpdatePicksForUserAsync(string userId, int newPrimaryDriverId, bool targetSecondHalf = false)
        {
            var currentSeason = await _context.Pools
                .OrderByDescending(p => p.Year)
                .FirstOrDefaultAsync();

            if (currentSeason == null) return;

            var seasonRaces = await _context.Races
                .Where(r => r.PoolId == currentSeason.Id)
                .OrderBy(r => r.Date)
                .ToListAsync();

            if (seasonRaces.Count == 0) return;

            int half = seasonRaces.Count / 2;

            // Determine target race ids based on requested half.
            List<int> targetRaceIds;
            if (targetSecondHalf)
            {
                // second half: skip the first 'half' races, include the remainder
                targetRaceIds = seasonRaces.Skip(half).Select(r => r.Id).ToList();
            }
            else
            {
                // first half: take the first 'half' races
                targetRaceIds = seasonRaces.Take(half).Select(r => r.Id).ToList();
            }

            if (!targetRaceIds.Any()) return;

            var picksToUpdate = await _context.Picks
                .Where(p => p.UserId == userId && targetRaceIds.Contains(p.RaceId))
                .ToListAsync();

            foreach (var pick in picksToUpdate)
            {
                pick.Pick1Id = newPrimaryDriverId;

                // Recalculate points for this pick using existing race results
                var raceResults = await _context.RaceResults
                    .Where(rr => rr.RaceId == pick.RaceId)
                    .ToListAsync();

                // CalculateTotalPoints will persist the updated Points for this pick
                pick.CalculateTotalPoints(_context, raceResults);
            }
        }
    }
}