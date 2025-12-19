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
using Microsoft.Extensions.Logging;

namespace WebApp.Pages.Races.Picks
{
	[Authorize]
	public class EditModel : PageModel
	{
		private readonly ApplicationDbContext _context;
		private readonly UserManager<ApplicationUser> _userManager;
		private readonly ILogger<EditModel> _logger;

		public EditModel(ApplicationDbContext context, UserManager<ApplicationUser> userManager, ILogger<EditModel> logger)
		{
			_context = context;
			_userManager = userManager;
			_logger = logger;
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

		public List<Race> Races { get; set; } = new();

		public async Task<IActionResult> OnGetAsync(int raceId)
		{
			try
			{
				var userId = _userManager.GetUserId(User);
				_logger.LogInformation("User {UserId} ({Email}) accessing picks page for race {RaceId}", 
					userId, User.Identity?.Name ?? "Anonymous", raceId);

				RaceId = raceId;
				Race = await _context.Races.Include(r => r.Pool).FirstOrDefaultAsync(r => r.Id == raceId);
				if (Race == null)
				{
					_logger.LogWarning("Race not found. RaceId: {RaceId}, UserId: {UserId}", raceId, userId);
					return NotFound();
				}

				CanEnterPicks = DateTime.Today < Race.Date;
				_logger.LogDebug("Pick entry allowed for user {UserId} on race {RaceId}: {Allowed}", 
					userId, raceId, CanEnterPicks);

				Drivers = await _context.Drivers.OrderBy(d => d.Name).ToListAsync();

				if (Race.PoolId != 0)
				{
					Races = await _context.Races
						.Where(r => r.PoolId == Race.PoolId)
						.OrderBy(r => r.Date)
						.ToListAsync();
				}

				if (string.IsNullOrEmpty(userId))
				{
					_logger.LogWarning("User ID is null during pick page access. RaceId: {RaceId}", raceId);
					return Challenge();
				}

				int totalRaces = Races.Count;
				int half = totalRaces / 2;
				int raceIndex = Races.FindIndex(r => r.Id == raceId);
				bool isFirstHalf = raceIndex >= 0 && raceIndex < half;

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

					_logger.LogInformation("Existing picks loaded for user {UserId} on race {RaceId} ({RaceName}). " +
						"Pick1: {Pick1Name}, Pick2: {Pick2Name}, Pick3: {Pick3Name}", 
						userId, raceId, Race.Name, 
						ExistingPick.Pick1?.Name ?? "None", 
						ExistingPick.Pick2?.Name ?? "None", 
						ExistingPick.Pick3?.Name ?? "None");
				}
				else
				{
					_logger.LogInformation("No existing picks found for user {UserId} on race {RaceId} ({RaceName}). Primary driver: {PrimaryDriverId}", 
						userId, raceId, Race.Name, primaryDriverId);
				}

				return Page();
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error loading picks page for race {RaceId}", raceId);
				throw;
			}
		}

		public async Task<IActionResult> OnPostAsync()
		{
			try
			{
				var userId = _userManager.GetUserId(User);
				var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

				_logger.LogInformation("User {UserId} ({Email}) attempting to save picks for race {RaceId}. " +
					"Pick1Id: {Pick1}, Pick2Id: {Pick2}, Pick3Id: {Pick3}, IP: {IpAddress}", 
					userId, User.Identity?.Name ?? "Anonymous", RaceId, Pick1Id, Pick2Id, Pick3Id, ipAddress);

				Race = await _context.Races.Include(r => r.Pool).FirstOrDefaultAsync(r => r.Id == RaceId);
				if (Race == null)
				{
					_logger.LogWarning("Race not found during pick save. RaceId: {RaceId}, UserId: {UserId}", 
						RaceId, userId);
					return NotFound();
				}

				if (DateTime.Today >= Race.Date)
				{
					_logger.LogWarning("User {UserId} attempted to save picks on/after race day. RaceId: {RaceId}, RaceDate: {RaceDate}, IP: {IpAddress}", 
						userId, RaceId, Race.Date, ipAddress);
					ModelState.AddModelError(string.Empty, "Picks cannot be entered on or after race day.");
					return Page();
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
					ModelState.AddModelError(string.Empty, "All three picks are required.");
					return Page();
				}

				// Check for duplicate picks
				var picksSet = new HashSet<int> { Pick1Id, Pick2Id, Pick3Id };
				if (picksSet.Count < 3)
				{
					_logger.LogWarning("Pick validation failed - duplicate drivers selected. UserId: {UserId}, RaceId: {RaceId}, " +
						"Pick1: {Pick1}, Pick2: {Pick2}, Pick3: {Pick3}, IP: {IpAddress}", 
						userId, RaceId, Pick1Id, Pick2Id, Pick3Id, ipAddress);
					ModelState.AddModelError(string.Empty, "You cannot select the same driver multiple times.");
					return Page();
				}

				var pick = await _context.Picks.FirstOrDefaultAsync(p => p.RaceId == RaceId && p.UserId == userId);

				// Get driver names for logging
				var drivers = await _context.Drivers.Where(d => 
					d.Id == Pick1Id || d.Id == Pick2Id || d.Id == Pick3Id).ToListAsync();
				var pick1Name = drivers.FirstOrDefault(d => d.Id == Pick1Id)?.Name ?? $"ID:{Pick1Id}";
				var pick2Name = drivers.FirstOrDefault(d => d.Id == Pick2Id)?.Name ?? $"ID:{Pick2Id}";
				var pick3Name = drivers.FirstOrDefault(d => d.Id == Pick3Id)?.Name ?? $"ID:{Pick3Id}";

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

					_logger.LogInformation("New picks created for user {UserId} on race {RaceId} ({RaceName}). " +
						"Pick1: {Pick1Name} (#{Pick1Car}), Pick2: {Pick2Name} (#{Pick2Car}), Pick3: {Pick3Name} (#{Pick3Car}), IP: {IpAddress}", 
						userId, RaceId, Race.Name, 
						pick1Name, drivers.FirstOrDefault(d => d.Id == Pick1Id)?.CarNumber,
						pick2Name, drivers.FirstOrDefault(d => d.Id == Pick2Id)?.CarNumber,
						pick3Name, drivers.FirstOrDefault(d => d.Id == Pick3Id)?.CarNumber,
						ipAddress);
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
						userId, RaceId, Race.Name, 
						oldPick1Name, pick1Name, 
						oldPick2Name, pick2Name, 
						oldPick3Name, pick3Name, 
						ipAddress);
				}

				await _context.SaveChangesAsync();

				_logger.LogInformation("Picks saved successfully for user {UserId} on race {RaceId}. PickId: {PickId}", 
					userId, RaceId, pick.Id);

				TempData["Success"] = "Your picks have been saved.";
				return RedirectToPage("/Races/Pick/Index", new { raceId = RaceId });
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error saving picks for race {RaceId}, User: {UserId}", 
					RaceId, _userManager.GetUserId(User));
				throw;
			}
		}
	}
}