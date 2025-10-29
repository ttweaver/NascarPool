using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using WebApp.Data;
using WebApp.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WebApp.Pages.Races.Pick
{
	[Authorize]
	public class PicksModel : PageModel
	{
		private readonly ApplicationDbContext _context;
		private readonly UserManager<ApplicationUser> _userManager;

		public PicksModel(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
		{
			_context = context;
			_userManager = userManager;
		}

		[BindProperty]
		public int RaceId { get; set; }

		public Race? Race { get; set; }
		public List<Driver> Drivers { get; set; } = new();
		public bool CanEnterPicks { get; set; }
		public WebApp.Models.Pick? ExistingPick { get; set; }

		[BindProperty]
		public int Pick1Id { get; set; }
		[BindProperty]
		public int Pick2Id { get; set; }
		[BindProperty]
		public int Pick3Id { get; set; }

		public List<Race> Races { get; set; } = new(); // <-- Added property

		public async Task<IActionResult> OnGetAsync(int raceId)
		{
			RaceId = raceId;
			Race = await _context.Races.Include(r => r.Pool).FirstOrDefaultAsync(r => r.Id == raceId);
			if (Race == null)
				return NotFound();

			// Only allow picks before race day
			CanEnterPicks = DateTime.Today < Race.Date;

			Drivers = await _context.Drivers.OrderBy(d => d.Name).ToListAsync();

			// Get all races for the same pool as the current race
			if (Race.PoolId != 0)
			{
				Races = await _context.Races
					.Where(r => r.PoolId == Race.PoolId)
					.OrderBy(r => r.Date)
					.ToListAsync();
			}

			var userId = _userManager.GetUserId(User);
			if (string.IsNullOrEmpty(userId))
				return Challenge(); // Redirect to login if userId is null

			// Determine if current race is in first or second part of the season
			int totalRaces = Races.Count;
			int half = totalRaces / 2;
			int raceIndex = Races.FindIndex(r => r.Id == raceId);

			bool isFirstHalf = raceIndex >= 0 && raceIndex < half;

			// Get current user
			var user = await _userManager.Users.FirstOrDefaultAsync(u => u.Id == userId);
			int? primaryDriverId = null;
			if (user != null)
			{
				if (isFirstHalf)
				{
					primaryDriverId = user.PrimaryDriverFirstHalfId;
				}
				else
				{
					primaryDriverId = user.PrimaryDriverSecondHalfId;
				}
			}

			ExistingPick = await _context.Picks
				.Include(p => p.Pick1)
				.Include(p => p.Pick2)
				.Include(p => p.Pick3)
				.FirstOrDefaultAsync(p => p.RaceId == raceId && p.UserId == userId);

			Pick1Id = primaryDriverId ?? 0;

			if (ExistingPick != null)
			{
				Pick2Id = ExistingPick.Pick2Id;
				Pick3Id = ExistingPick.Pick3Id;
			}

			return Page();
		}

		public async Task<IActionResult> OnPostAsync()
		{
			Race = await _context.Races.Include(r => r.Pool).FirstOrDefaultAsync(r => r.Id == RaceId);
			if (Race == null)
				return NotFound();

			// Only allow picks before race day
			if (DateTime.Today >= Race.Date)
			{
				ModelState.AddModelError(string.Empty, "Picks cannot be entered on or after race day.");
				return Page();
			}

			var userId = _userManager.GetUserId(User);
			if (string.IsNullOrEmpty(userId))
				return Challenge(); // Redirect to login if userId is null

			var pick = await _context.Picks.FirstOrDefaultAsync(p => p.RaceId == RaceId && p.UserId == userId);

			if (pick == null)
			{
				pick = new WebApp.Models.Pick
				{
					RaceId = RaceId,
					UserId = userId,
					Pick1Id = Pick1Id,
					Pick2Id = Pick2Id,
					Pick3Id = Pick3Id
				};
				_context.Picks.Add(pick);
			}
			else
			{
				pick.Pick1Id = Pick1Id;
				pick.Pick2Id = Pick2Id;
				pick.Pick3Id = Pick3Id;
				_context.Picks.Update(pick);
			}

			await _context.SaveChangesAsync();
			TempData["Success"] = "Your picks have been saved.";
			return RedirectToPage("/Races/Pick/Index", new { raceId = RaceId });
		}
	}
}