using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using WebApp.Data;
using WebApp.Models;

namespace WebApp.Areas.Manage.Pages.Players
{
    [ValidateAntiForgeryToken]
    public class ManageAllowedUsersModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ManageAllowedUsersModel> _logger;

        public ManageAllowedUsersModel(ApplicationDbContext context, ILogger<ManageAllowedUsersModel> logger)
        {
            _context = context;
            _logger = logger;
        }

        public List<AllowedUsers> AllowedUsers { get; set; } = new();

        public async Task<IActionResult> OnGetAsync()
        {
            try
            {
                _logger.LogInformation("User {UserId} accessed Manage Allowed Users page", User.Identity?.Name ?? "Anonymous");

                AllowedUsers = await _context.AllowedUsers
                    .OrderBy(u => u.LastName)
                    .ThenBy(u => u.FirstName)
                    .ToListAsync();

                _logger.LogInformation("Successfully loaded {Count} allowed users", AllowedUsers.Count);

                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading allowed users list");
                throw;
            }
        }

        public async Task<IActionResult> OnPostCreateAsync(string firstName, string lastName, string email)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(firstName) || string.IsNullOrWhiteSpace(lastName) || string.IsNullOrWhiteSpace(email))
                {
                    _logger.LogWarning("Create allowed user validation failed - missing required fields");
                    TempData["ErrorMessage"] = "All fields are required.";
                    return RedirectToPage();
                }

                // Check if email already exists
                var existingUser = await _context.AllowedUsers
                    .FirstOrDefaultAsync(u => u.Email.ToLower() == email.ToLower());

                if (existingUser != null)
                {
                    _logger.LogWarning("Attempted to create duplicate allowed user with email {Email}", email);
                    TempData["ErrorMessage"] = "A user with this email address already exists.";
                    return RedirectToPage();
                }

                var newUser = new AllowedUsers
                {
                    FirstName = firstName.Trim(),
                    LastName = lastName.Trim(),
                    Email = email.Trim().ToLower()
                };

                _context.AllowedUsers.Add(newUser);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Allowed user created. Name: {FirstName} {LastName}, Email: {Email}, CreatedBy: {CreatedBy}",
                    firstName, lastName, email, User.Identity?.Name ?? "Anonymous");

                TempData["SuccessMessage"] = $"User '{firstName} {lastName}' has been successfully added.";
                return RedirectToPage();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating allowed user with email {Email}", email);
                throw;
            }
        }

        public async Task<IActionResult> OnPostEditAsync(int userId, string firstName, string lastName, string email)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(firstName) || string.IsNullOrWhiteSpace(lastName) || string.IsNullOrWhiteSpace(email))
                {
                    _logger.LogWarning("Edit allowed user validation failed for user ID {UserId}", userId);
                    TempData["ErrorMessage"] = "All fields are required.";
                    return RedirectToPage();
                }

                var user = await _context.AllowedUsers.FindAsync(userId);

                if (user == null)
                {
                    _logger.LogWarning("Allowed user not found with ID {UserId}", userId);
                    TempData["ErrorMessage"] = "User not found.";
                    return RedirectToPage();
                }

                // Check if email already exists for a different user
                var existingUser = await _context.AllowedUsers
                    .FirstOrDefaultAsync(u => u.Email.ToLower() == email.ToLower() && u.Id != userId);

                if (existingUser != null)
                {
                    _logger.LogWarning("Attempted to update allowed user {UserId} with duplicate email {Email}", userId, email);
                    TempData["ErrorMessage"] = "A user with this email address already exists.";
                    return RedirectToPage();
                }

                // Capture original values
                var originalEmail = user.Email;
                var originalName = $"{user.FirstName} {user.LastName}";

                user.FirstName = firstName.Trim();
                user.LastName = lastName.Trim();
                user.Email = email.Trim().ToLower();

                await _context.SaveChangesAsync();

                _logger.LogInformation("Allowed user updated. UserId: {UserId}, Email: {OldEmail} -> {NewEmail}, " +
                    "Name: {OldName} -> {NewName}, UpdatedBy: {UpdatedBy}",
                    userId, originalEmail, user.Email, originalName, $"{firstName} {lastName}", 
                    User.Identity?.Name ?? "Anonymous");

                TempData["SuccessMessage"] = $"User '{firstName} {lastName}' has been successfully updated.";
                return RedirectToPage();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating allowed user ID {UserId}", userId);
                throw;
            }
        }

        public async Task<IActionResult> OnPostDeleteAsync(int userId)
        {
            try
            {
                var user = await _context.AllowedUsers.FindAsync(userId);

                if (user == null)
                {
                    _logger.LogWarning("Attempted to delete non-existent allowed user ID {UserId}", userId);
                    TempData["ErrorMessage"] = "User not found.";
                    return RedirectToPage();
                }

                string userName = $"{user.FirstName} {user.LastName}";
                string userEmail = user.Email;

                _context.AllowedUsers.Remove(user);
                await _context.SaveChangesAsync();

                _logger.LogWarning("Allowed user deleted. UserId: {UserId}, Email: {Email}, Name: {Name}, DeletedBy: {DeletedBy}",
                    userId, userEmail, userName, User.Identity?.Name ?? "Anonymous");

                TempData["SuccessMessage"] = $"User '{userName}' has been successfully deleted.";
                return RedirectToPage();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting allowed user ID {UserId}", userId);
                throw;
            }
        }
    }
}