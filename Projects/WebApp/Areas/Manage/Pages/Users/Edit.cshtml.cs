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

namespace WebApp.Areas.Manage.Pages.Users
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
                    .FirstOrDefaultAsync(u => u.Id == id);

                if (User == null)
                {
                    _logger.LogWarning("User not found with ID {UserId}", id);
                    return NotFound();
                }

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
                ModelState.Remove("User.Password");
                ModelState.Remove("User.ConfirmPassword");

                if (!ModelState.IsValid)
                {
                    _logger.LogWarning("Edit user validation failed for user ID {UserId}", User.Id);
                    return Page();
                }

                var existing = await _context.Users
                    .Include(u => u.Pools)
                    .FirstOrDefaultAsync(u => u.Id == User.Id);
                    
                if (existing == null)
                {
                    _logger.LogWarning("User not found during update. User ID {UserId}", User.Id);
                    return NotFound();
                }

                // Capture original values for logging
                var originalEmail = existing.Email;
                var originalFirstName = existing.FirstName;
                var originalLastName = existing.LastName;
                var originalIsPlayer = existing.IsPlayer;

                // update only editable fields
                existing.UserName = User.Email; // Set username to email for consistency
                existing.NormalizedUserName = User.Email.ToUpperInvariant();
                existing.FirstName = User.FirstName;
                existing.LastName = User.LastName;
                existing.IsPlayer = User.IsPlayer;
                existing.Email = User.Email;
                existing.NormalizedEmail = User.Email.ToUpperInvariant();

                _context.Users.Update(existing);
                await _context.SaveChangesAsync();

                _logger.LogInformation("User updated successfully. UserId: {UserId}, " +
                    "Email: {OldEmail} -> {NewEmail}, " +
                    "Name: {OldFirstName} {OldLastName} -> {NewFirstName} {NewLastName}, " +
                    "IsPlayer: {OldIsPlayer} -> {NewIsPlayer}, " +
                    "UpdatedBy: {UpdatedBy}",
                    User.Id, originalEmail, existing.Email, 
                    originalFirstName, originalLastName, existing.FirstName, existing.LastName,
                    originalIsPlayer, existing.IsPlayer,
                    User?.UserName ?? "Anonymous");

                TempData["Success"] = $"User {existing.FirstName} {existing.LastName} updated successfully.";
                return RedirectToPage("Index");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user ID {UserId}", User.Id);
                TempData["Error"] = "An error occurred while updating the user.";
                throw;
            }
        }
    }
}