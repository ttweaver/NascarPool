using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using WebApp.Data;
using WebApp.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace WebApp.Areas.Manage.Pages.Players
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

        [BindProperty]
        public ApplicationUser User { get; set; } = default!;

        // options for the driver select lists
        public List<SelectListItem> DriverOptions { get; set; } = new();

        // options for the pool select list
        public List<SelectListItem> PoolOptions { get; set; } = new();

        // JSON data for all drivers with pool IDs (for client-side filtering)
        public string AllDriversJson { get; set; } = "[]";

        public async Task<IActionResult> OnGetAsync(string id)
        {
            try
            {
                if (string.IsNullOrEmpty(id))
                {
                    _logger.LogWarning("Edit user attempted with null/empty ID");
                    return NotFound();
                }

                _logger.LogInformation("User {CurrentUser} accessing edit page for user ID {UserId}", 
                    User?.UserName ?? "Anonymous", id);

                User = await _context.Users
                    .Include(u => u.PrimaryDriverFirstHalf)
                    .Include(u => u.PrimaryDriverSecondHalf)
                    .FirstOrDefaultAsync(u => u.Id == id);

                if (User == null)
                {
                    _logger.LogWarning("User not found with ID {UserId}", id);
                    return NotFound();
                }

                await PopulatePoolOptionsAsync();
                await PopulateDriverOptionsAsync();
                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading edit page for user ID {UserId}", id);
                throw;
            }
        }

        public async Task<IActionResult> OnPostAsync()
        {
            try
            {
                // repopulate selects if we redisplay the page
                await PopulatePoolOptionsAsync();
                await PopulateDriverOptionsAsync();

                ModelState.Remove("User.PrimaryDriverFirstHalf");
                ModelState.Remove("User.PrimaryDriverSecondHalf");
                ModelState.Remove("User.Password");

                if (!ModelState.IsValid)
                {
                    _logger.LogWarning("Edit user validation failed for user ID {UserId}", User.Id);
                    return Page();
                }

                var existing = await _context.Users.FindAsync(User.Id);
                if (existing == null)
                {
                    _logger.LogWarning("User not found during update. User ID {UserId}", User.Id);
                    return NotFound();
                }

                // Capture original values for logging
                var originalEmail = existing.Email;
                var originalFirstName = existing.FirstName;
                var originalLastName = existing.LastName;

                // update only editable fields
                existing.UserName = User.UserName;
                existing.FirstName = User.FirstName;
                existing.LastName = User.LastName;
                existing.IsPlayer = User.IsPlayer;
                existing.Email = User.Email;

                // update primary driver selections (nullable ints)
                existing.PrimaryDriverFirstHalfId = User.PrimaryDriverFirstHalfId;
                existing.PrimaryDriverSecondHalfId = User.PrimaryDriverSecondHalfId;

                _context.Users.Update(existing);
                await _context.SaveChangesAsync();

                _logger.LogInformation("User updated successfully. UserId: {UserId}, Email: {OldEmail} -> {NewEmail}, " +
                    "Name: {OldFirstName} {OldLastName} -> {NewFirstName} {NewLastName}, UpdatedBy: {UpdatedBy}",
                    User.Id, originalEmail, existing.Email, originalFirstName, originalLastName, 
                    existing.FirstName, existing.LastName, User?.UserName ?? "Anonymous");

                return RedirectToPage("Index");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user ID {UserId}", User.Id);
                throw;
            }
        }

        private async Task PopulatePoolOptionsAsync()
        {
            var pools = await _context.Pools
                .OrderByDescending(p => p.Year)
                .ToListAsync();

            // Get the current pool (highest year)
            var currentPool = pools.FirstOrDefault();

            PoolOptions = pools
                .Select(p => new SelectListItem
                {
                    Value = p.Id.ToString(),
                    Text = $"{p.Name} ({p.Year})",
                    Selected = currentPool != null && p.Id == currentPool.Id
                })
                .ToList();
        }

        private async Task PopulateDriverOptionsAsync()
        {
            var drivers = await _context.Drivers
                .Include(d => d.Pool)
                .OrderBy(d => d.Pool.Year)
                .ThenBy(d => d.Name)
                .ToListAsync();

            // Get the current pool to filter drivers initially
            var currentPool = await _context.Pools
                .OrderByDescending(p => p.Year)
                .FirstOrDefaultAsync();

            // Set initial driver options for current pool
            DriverOptions = drivers
                .Where(d => currentPool != null && d.PoolId == currentPool.Id)
                .Select(d => new SelectListItem
                {
                    Value = d.Id.ToString(),
                    Text = $"{d.Name} ({d.CarNumber})"
                })
                .ToList();

            // Create JSON for all drivers (for client-side filtering)
            var allDriversData = drivers.Select(d => new
            {
                value = d.Id.ToString(),
                text = $"{d.Name} ({d.CarNumber})",
                poolId = d.PoolId
            }).ToList();

            AllDriversJson = System.Text.Json.JsonSerializer.Serialize(allDriversData);
        }
    }
}