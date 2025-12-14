using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using WebApp.Data;
using WebApp.Models;

namespace WebApp.Pages.Users
{
    public class CreateModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly IPasswordHasher<ApplicationUser> _passwordHasher;
        private readonly ILogger<CreateModel> _logger;

        public CreateModel(ApplicationDbContext context, IPasswordHasher<ApplicationUser> passwordHasher, ILogger<CreateModel> logger)
        {
            _context = context;
            _passwordHasher = passwordHasher;
            _logger = logger;
        }

        [BindProperty]
        public ApplicationUser User { get; set; } = default!;

        public void OnGet()
        {
            _logger.LogInformation("User {UserId} accessed Create User page", User?.UserName ?? "Anonymous");
        }

        public async Task<IActionResult> OnPostAsync()
        {
            try
            {
                ModelState.Remove("User.PrimaryDriverFirstHalf");
                ModelState.Remove("User.PrimaryDriverSecondHalf");

                User.UserName = User.Email;
                User.NormalizedUserName = User.Email.ToUpper();
                User.NormalizedEmail = User.Email.ToUpper();

                if (!ModelState.IsValid)
                {
                    _logger.LogWarning("Create user failed validation for email {Email}", User.Email);
                    return Page();
                }

                // Hash and set the password (Password is NotMapped so we must set PasswordHash)
                if (!string.IsNullOrWhiteSpace(User.Password))
                {
                    User.PasswordHash = _passwordHasher.HashPassword(User, User.Password);
                }

                _context.Users.Add(User);
                await _context.SaveChangesAsync();

                _logger.LogInformation("User created successfully. Email: {Email}, UserId: {UserId}, CreatedBy: {CreatedBy}",
                    User.Email, User.Id, User?.UserName ?? "Anonymous");

                return RedirectToPage("Index");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating user with email {Email}", User.Email);
                throw;
            }
        }
    }
}