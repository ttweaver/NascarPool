using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using WebApp.Data;
using WebApp.Models;

namespace WebApp.Pages.Races
{
    public class IndexModel(ApplicationDbContext context) : PageModel
    {
        private readonly ApplicationDbContext _context = context;

		public IList<Race> Races { get; set; } = default!;

		public SelectList PoolSelectList { get; set; } = default!;
        
        [BindProperty(SupportsGet = true)]
		public int? PoolId { get; set; }
        public Pool LatestPool { get; set; } = default!;

        public CreateRaceInputModel CreateInput { get; set; } = default!;

        public EditRaceInputModel EditInput { get; set; } = default!;

        public DeleteRaceInputModel DeleteInput { get; set; } = default!;

        [TempData]
        public string? ActiveModal { get; set; }

        public class CreateRaceInputModel
        {
            [Required(ErrorMessage = "Race name is required.")]
            [StringLength(100, ErrorMessage = "Race name cannot exceed 100 characters.")]
            public string Name { get; set; } = default!;

            [Required(ErrorMessage = "Race date is required.")]
            [DataType(DataType.Date)]
            public DateTime Date { get; set; }

            [Required(ErrorMessage = "City is required.")]
            [StringLength(100, ErrorMessage = "City cannot exceed 100 characters.")]
            public string City { get; set; } = default!;

            [Required(ErrorMessage = "State is required.")]
            [StringLength(50, ErrorMessage = "State cannot exceed 50 characters.")]
            public string State { get; set; } = default!;

            [Required]
            public int PoolId { get; set; }
        }

        public class EditRaceInputModel
        {
            [Required]
            public int Id { get; set; }

            [Required(ErrorMessage = "Race name is required.")]
            [StringLength(100, ErrorMessage = "Race name cannot exceed 100 characters.")]
            public string Name { get; set; } = default!;

            [Required(ErrorMessage = "Race date is required.")]
            [DataType(DataType.Date)]
            public DateTime Date { get; set; }

            [Required(ErrorMessage = "City is required.")]
            [StringLength(100, ErrorMessage = "City cannot exceed 100 characters.")]
            public string City { get; set; } = default!;

            [Required(ErrorMessage = "State is required.")]
            [StringLength(50, ErrorMessage = "State cannot exceed 50 characters.")]
            public string State { get; set; } = default!;
        }

        public class DeleteRaceInputModel
        {
            [Required]
            public int Id { get; set; }

            public string City { get; set; } = default!;
            public string State { get; set; } = default!;
        }

        public async Task<IActionResult> OnGetAsync()
        {
            await LoadDataAsync();
            return Page();
		}

        // CREATE
        public async Task<IActionResult> OnPostCreateAsync()
        {
			TempData["ActiveModal"] = null;

			if (!User.IsInRole("Admin"))
            {
                return Forbid();
            }

			// Manually bind and validate only CreateInput
			CreateInput = new CreateRaceInputModel();
			await TryUpdateModelAsync(CreateInput, "CreateInput");

			if (!ModelState.IsValid)
            {
                ActiveModal = "create";
                await LoadDataAsync();
                return Page();
            }

            var pool = await _context.Pools.FindAsync(CreateInput.PoolId);
            if (pool == null)
            {
                ModelState.AddModelError(string.Empty, "No pool found.");
				ActiveModal = "create";
				await LoadDataAsync();
                return Page();
            }

            if (CreateInput.Date.Year != pool.Year)
            {
                ModelState.AddModelError("CreateInput.Date", $"Race date must be in the pool's year: {pool.Year}.");
                ActiveModal = "create";
				await LoadDataAsync();
                return Page();
            }

            var race = new Race
            {
                Name = CreateInput.Name,
                Date = CreateInput.Date,
                City = CreateInput.City,
                State = CreateInput.State,
                PoolId = pool.Id
            };

            _context.Races.Add(race);
            await _context.SaveChangesAsync();
            return RedirectToPage(new { poolId = pool.Id });
        }

        // EDIT
        public async Task<IActionResult> OnPostEditAsync()
        {
			TempData["ActiveModal"] = null;

            if (!User.IsInRole("Admin"))
            {
                return Forbid();
            }

            // Manually bind and validate only EditInput
            EditInput = new EditRaceInputModel();
            await TryUpdateModelAsync(EditInput, "EditInput");

            if (!ModelState.IsValid)
            {
                ActiveModal = "edit";
                await LoadDataAsync();
                return Page();
            }

            var race = await _context.Races.Include(r => r.Pool).FirstOrDefaultAsync(r => r.Id == EditInput.Id);
            if (race == null)
            {
                ModelState.AddModelError(string.Empty, "Race not found.");
				ActiveModal = "edit";
				await LoadDataAsync();
                return Page();
            }

            if (EditInput.Date.Year != race.Pool.Year)
            {
                ModelState.AddModelError("EditInput.Date", $"Race date must be in the pool's year: {race.Pool.Year}.");
				ActiveModal = "edit";
				await LoadDataAsync();
                return Page();
            }

            race.Name = EditInput.Name;
            race.Date = EditInput.Date;
            race.City = EditInput.City;
            race.State = EditInput.State;

            _context.Races.Update(race);
            await _context.SaveChangesAsync();
            return RedirectToPage(new { poolId = race.PoolId });
        }

        // DELETE
        public async Task<IActionResult> OnPostDeleteAsync()
        {
			TempData["ActiveModal"] = null;

			if (!User.IsInRole("Admin"))
            {
                return Forbid();
            }

            // Manually bind and validate only DeleteInput
            DeleteInput = new DeleteRaceInputModel();
            await TryUpdateModelAsync(DeleteInput, "DeleteInput");

            if (!ModelState.IsValid)
            {
                ActiveModal = "delete";
                await LoadDataAsync();
                return Page();
            }

            var race = await _context.Races.FindAsync(DeleteInput.Id);
            if (race == null)
            {
                ModelState.AddModelError(string.Empty, "Race not found.");
                ActiveModal = "delete";
				await LoadDataAsync();
                return Page();
            }

            // Optional: Verify city and state match for additional safety
            if (!string.IsNullOrWhiteSpace(DeleteInput.City) && !string.IsNullOrWhiteSpace(DeleteInput.State))
            {
                if (race.City != DeleteInput.City || race.State != DeleteInput.State)
                {
                    ModelState.AddModelError(string.Empty, "Race details do not match.");
                    await OnGetAsync();
                    return Page();
                }
            }

            var poolId = race.PoolId;
            _context.Races.Remove(race);
            await _context.SaveChangesAsync();
            return RedirectToPage(new { poolId });
        }

        private async Task LoadDataAsync()
        {
            var pools = await _context.Pools
                .OrderByDescending(p => p.Year)
                .ToListAsync();

            PoolSelectList = new SelectList(pools, nameof(Pool.Id), nameof(Pool.Name));

			if (!PoolId.HasValue)
			{
				PoolId = pools.GetLatestPoolYearAsync().Result.Id;
			}

			Races = await _context.Races
                .Include(r => r.Pool)
                .Where(r => r.PoolId == PoolId)
                .ToListAsync();
        }
    }
}