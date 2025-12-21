using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using WebApp.Data;
using WebApp.Models;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;

namespace WebApp.Areas.Manage.Pages.Pools
{
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        public IndexModel(ApplicationDbContext context) => _context = context;

        public IList<Pool> Pools { get; set; } = default!;

        public async Task OnGetAsync()
        {
            // Try to get poolId from cookie
            var poolIdCookie = Request.Cookies["poolId"];
            
            if (!string.IsNullOrEmpty(poolIdCookie) && int.TryParse(poolIdCookie, out var cookiePoolId))
            {
                // Filter to show only the pool matching the cookie
                var pool = await _context.Pools
                    .Where(p => p.Id == cookiePoolId)
                    .ToListAsync();
                Pools = pool;
            }
            else
            {
                // If no cookie, show all pools (or you could show the most recent)
                Pools = await _context.Pools.ToListAsync();
            }
        }
    }
}