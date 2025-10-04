using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using WebApp.Data;
using WebApp.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace WebApp.Pages.Pools
{
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        public IndexModel(ApplicationDbContext context) => _context = context;

        public IList<Pool> Pools { get; set; } = default!;

        public async Task OnGetAsync()
        {
            Pools = await _context.Pools.ToListAsync();
        }
    }
}