using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using WebApp.Data;
using WebApp.Models;

namespace WebApp.Areas.Manage.Pages.Users
{
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<IndexModel> _logger;

        public IndexModel(ApplicationDbContext context, ILogger<IndexModel> logger)
        {
            _context = context;
            _logger = logger;
        }

        public IList<UserViewModel> Users { get; set; } = default!;

        public class UserViewModel
        {
            public string Id { get; set; } = string.Empty;
            public string FirstName { get; set; } = string.Empty;
            public string LastName { get; set; } = string.Empty;
            public string Email { get; set; } = string.Empty;
            public bool IsPlayer { get; set; }
            public List<PoolInfo> PoolMemberships { get; set; } = new();
        }

        public class PoolInfo
        {
            public int PoolId { get; set; }
            public string PoolName { get; set; } = string.Empty;
            public int Year { get; set; }
            public Driver? PrimaryDriverFirstHalf { get; set; }
            public Driver? PrimaryDriverSecondHalf { get; set; }
        }

        public async Task OnGetAsync()
        {
            try
            {
                _logger.LogInformation("User {UserId} accessed Users Index page (all users, no pool filter)", User.Identity?.Name ?? "Anonymous");
                
                // Get all users regardless of pool membership
                var allUsers = await _context.Users
                    .OrderBy(u => u.FirstName)
                    .ThenBy(u => u.LastName)
                    .ToListAsync();

                // Get all pools with members
                var pools = await _context.Pools
                    .Include(p => p.Members)
                    .OrderByDescending(p => p.Year)
                    .ToListAsync();

                // Get all primary driver assignments
                var allPrimaryDrivers = await _context.UserPoolPrimaryDrivers
                    .Include(uppd => uppd.PrimaryDriverFirstHalf)
                    .Include(uppd => uppd.PrimaryDriverSecondHalf)
                    .Include(uppd => uppd.Pool)
                    .ToListAsync();

                // Build view models with pool membership information
                Users = allUsers.Select(user =>
                {
                    // Get all pools this user is a member of
                    var userPools = pools
                        .Where(p => p.Members.Any(m => m.Id == user.Id))
                        .Select(p =>
                        {
                            var poolDriver = allPrimaryDrivers
                                .FirstOrDefault(pd => pd.UserId == user.Id && pd.PoolId == p.Id);
                            
                            return new PoolInfo
                            {
                                PoolId = p.Id,
                                PoolName = p.Name,
                                Year = p.Year,
                                PrimaryDriverFirstHalf = poolDriver?.PrimaryDriverFirstHalf,
                                PrimaryDriverSecondHalf = poolDriver?.PrimaryDriverSecondHalf
                            };
                        })
                        .ToList();

                    return new UserViewModel
                    {
                        Id = user.Id,
                        FirstName = user.FirstName,
                        LastName = user.LastName,
                        Email = user.Email ?? string.Empty,
                        IsPlayer = user.IsPlayer,
                        PoolMemberships = userPools
                    };
                }).ToList();
                
                _logger.LogInformation("Successfully loaded {Count} users (all pools)", Users.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading users list");
                throw;
            }
        }
    }
}