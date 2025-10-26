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
        public bool HasPicks { get; set; }

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

            HasPicks = standings.Any(s => s.UserId == UserId);
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
        }

        public async Task<IActionResult> OnPostSetPrimaryDriverAsync(int driverId)
        {
            UserId = _userManager.GetUserId(User);

            // Find the user
            var user = await _userManager.Users.FirstOrDefaultAsync(u => u.Id == UserId);
            if (user == null)
            {
                return NotFound();
            }

            // Find the selected driver
            var selectedDriver = await _context.Drivers.FirstOrDefaultAsync(d => d.Id == driverId);
            if (selectedDriver == null)
            {
                ModelState.AddModelError(string.Empty, "Selected driver not found.");
                await OnGetAsync();
                return Page();
            }

            // Set the primary driver for the first half (adjust property name if needed)
            user.PrimaryDriverFirstHalf = selectedDriver;

            // Save changes to the user
            _context.Update(user);
            await _context.SaveChangesAsync();

            // After saving, reload the page data
            await OnGetAsync();
            return Page();
        }



			public async Task<IActionResult> OnPostSetPrimarySecondHalfDriverAsync(int secondHalfDriverId)
		{
			UserId = _userManager.GetUserId(User);

			// Find the user
			var user = await _userManager.Users.FirstOrDefaultAsync(u => u.Id == UserId);
			if (user == null)
			{
				return NotFound();
			}

			// Find the selected driver
			var selectedDriver = await _context.Drivers.FirstOrDefaultAsync(d => d.Id == secondHalfDriverId);
			if (selectedDriver == null)
			{
				ModelState.AddModelError(string.Empty, "Selected driver not found.");
				await OnGetAsync();
				return Page();
			}

			// Set the primary driver for the first half (adjust property name if needed)
			user.PrimaryDriverSecondHalf = selectedDriver;
			
			// Save changes to the user
			_context.Update(user);
			await _context.SaveChangesAsync();

			// After saving, reload the page data
			await OnGetAsync();
			return Page();
		}
	}   
}