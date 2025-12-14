using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using WebApp.Data;
using WebApp.Models;

namespace WebApp.Pages.Users
{
    [ValidateAntiForgeryToken]
    public class ManageAllowedUsersModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public ManageAllowedUsersModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public List<AllowedUsers> AllowedUsers { get; set; } = new();

        public async Task<IActionResult> OnGetAsync()
        {
            AllowedUsers = await _context.AllowedUsers
                .OrderBy(u => u.LastName)
                .ThenBy(u => u.FirstName)
                .ToListAsync();

            return Page();
        }

        public async Task<IActionResult> OnPostCreateAsync(string firstName, string lastName, string email)
        {
            if (string.IsNullOrWhiteSpace(firstName) || string.IsNullOrWhiteSpace(lastName) || string.IsNullOrWhiteSpace(email))
            {
                TempData["ErrorMessage"] = "All fields are required.";
                return RedirectToPage();
            }

            // Check if email already exists
            var existingUser = await _context.AllowedUsers
                .FirstOrDefaultAsync(u => u.Email.ToLower() == email.ToLower());

            if (existingUser != null)
            {
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

            TempData["SuccessMessage"] = $"User '{firstName} {lastName}' has been successfully added.";
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostEditAsync(int userId, string firstName, string lastName, string email)
        {
            if (string.IsNullOrWhiteSpace(firstName) || string.IsNullOrWhiteSpace(lastName) || string.IsNullOrWhiteSpace(email))
            {
                TempData["ErrorMessage"] = "All fields are required.";
                return RedirectToPage();
            }

            var user = await _context.AllowedUsers.FindAsync(userId);

            if (user == null)
            {
                TempData["ErrorMessage"] = "User not found.";
                return RedirectToPage();
            }

            // Check if email already exists for a different user
            var existingUser = await _context.AllowedUsers
                .FirstOrDefaultAsync(u => u.Email.ToLower() == email.ToLower() && u.Id != userId);

            if (existingUser != null)
            {
                TempData["ErrorMessage"] = "A user with this email address already exists.";
                return RedirectToPage();
            }

            user.FirstName = firstName.Trim();
            user.LastName = lastName.Trim();
            user.Email = email.Trim().ToLower();

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"User '{firstName} {lastName}' has been successfully updated.";
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostDeleteAsync(int userId)
        {
            var user = await _context.AllowedUsers.FindAsync(userId);

            if (user == null)
            {
                TempData["ErrorMessage"] = "User not found.";
                return RedirectToPage();
            }

            string userName = $"{user.FirstName} {user.LastName}";

            _context.AllowedUsers.Remove(user);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"User '{userName}' has been successfully deleted.";
            return RedirectToPage();
        }
    }
}