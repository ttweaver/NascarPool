using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using WebApp.Data;

namespace WebApp.Areas.Manage.Pages
{
    [Authorize(Roles = "Admin")]
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public IndexModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public int PoolCount { get; set; }
        public int RaceCount { get; set; }
        public int DriverCount { get; set; }
        public int PlayerCount { get; set; }

        public async Task OnGetAsync()
        {
            PoolCount = await _context.Pools.CountAsync();
            RaceCount = await _context.Races.CountAsync();
            DriverCount = await _context.Drivers.CountAsync();
            PlayerCount = await _context.Users.CountAsync();
        }
    }
}