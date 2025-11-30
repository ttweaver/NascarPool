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

        public CreateModel(ApplicationDbContext context, IPasswordHasher<ApplicationUser> passwordHasher) =>
            (_context, _passwordHasher) = (context, passwordHasher);

        [BindProperty]
        public ApplicationUser User { get; set; } = default!;

        public void OnGet() { }

        public async Task<IActionResult> OnPostAsync()
        {
            ModelState.Remove("User.PrimaryDriverFirstHalf");
            ModelState.Remove("User.PrimaryDriverSecondHalf");

            User.UserName = User.Email;
            User.NormalizedUserName = User.Email.ToUpper();
            User.NormalizedEmail = User.Email.ToUpper();

            if (!ModelState.IsValid) return Page();

            // Hash and set the password (Password is NotMapped so we must set PasswordHash)
            if (!string.IsNullOrWhiteSpace(User.Password))
            {
                User.PasswordHash = _passwordHasher.HashPassword(User, User.Password);
            }

            _context.Users.Add(User);
            await _context.SaveChangesAsync();
            return RedirectToPage("Index");
        }
    }
}