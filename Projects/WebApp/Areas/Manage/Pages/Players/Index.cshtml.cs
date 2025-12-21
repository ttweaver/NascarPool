using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using WebApp.Data;
using WebApp.Models;

namespace WebApp.Areas.Manage.Pages.Players
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
        public Pool? CurrentPool { get; set; }

        public class UserViewModel
        {
            public string Id { get; set; } = string.Empty;
            public string FirstName { get; set; } = string.Empty;
            public string LastName { get; set; } = string.Empty;
            public string Email { get; set; } = string.Empty;
            public Driver? PrimaryDriverFirstHalf { get; set; }
            public Driver? PrimaryDriverSecondHalf { get; set; }
        }

        private Pool? GetCurrentSeasonFromCookie()
        {
            // Try to get poolId from cookie
            var poolIdCookie = Request.Cookies["poolId"];
            Pool? currentSeason = null;

            if (!string.IsNullOrEmpty(poolIdCookie) && int.TryParse(poolIdCookie, out var cookiePoolId))
            {
                currentSeason = _context.Pools.Include(p => p.Members).FirstOrDefault(p => p.Id == cookiePoolId);
            }

            // Fallback to latest season if cookie not found or invalid
            if (currentSeason == null)
            {
                currentSeason = _context.Pools.Include(p => p.Members).AsEnumerable()
                    .OrderByDescending(s => s.CurrentYear)
                    .FirstOrDefault();
            }

            return currentSeason;
        }

        public async Task OnGetAsync()
        {
            try
            {
                _logger.LogInformation("User {UserId} accessed Users Index page", User.Identity?.Name ?? "Anonymous");
                
                CurrentPool = GetCurrentSeasonFromCookie();
                
                if (CurrentPool == null)
                {
                    _logger.LogWarning("No current pool found");
                    Users = new List<UserViewModel>();
                    return;
                }

                _logger.LogInformation("Loading players for pool {PoolId} ({PoolName})", CurrentPool.Id, CurrentPool.Name);

                // Get user IDs that are part of the current pool
                var poolUserIds = CurrentPool.Members
                    .Select(pu => pu.Id)
                    .ToList();

                // Get only players that are part of the current pool
                var players = await _context.Users
                    .Where(u => u.IsPlayer && poolUserIds.Contains(u.Id))
                    .OrderBy(p => p.FirstName)
                    .ToListAsync();

                // Get all primary driver assignments for the current pool
                var primaryDrivers = await _context.UserPoolPrimaryDrivers
                    .Include(uppd => uppd.PrimaryDriverFirstHalf)
                    .Include(uppd => uppd.PrimaryDriverSecondHalf)
                    .Where(uppd => uppd.PoolId == CurrentPool.Id)
                    .ToListAsync();

                // Build view models
                Users = players.Select(player =>
                {
                    var poolDriver = primaryDrivers.FirstOrDefault(pd => pd.UserId == player.Id);
                    return new UserViewModel
                    {
                        Id = player.Id,
                        FirstName = player.FirstName,
                        LastName = player.LastName,
                        Email = player.Email ?? string.Empty,
                        PrimaryDriverFirstHalf = poolDriver?.PrimaryDriverFirstHalf,
                        PrimaryDriverSecondHalf = poolDriver?.PrimaryDriverSecondHalf
                    };
                }).ToList();
                
                _logger.LogInformation("Successfully loaded {Count} users for pool {PoolName}", Users.Count, CurrentPool.Name);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading users list");
                throw;
            }
        }
    }
}