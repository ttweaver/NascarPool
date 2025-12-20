using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApp.Data;
using WebApp.Models;

namespace WebApp.ViewComponents
{
    public class PoolSelectorViewComponent : ViewComponent
    {
        private readonly ApplicationDbContext _context;

        public PoolSelectorViewComponent(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var pools = await _context.Pools
                .OrderByDescending(p => p.Year)
                .Select(p => new PoolSelectItem
                {
                    Id = p.Id,
                    Year = p.Year
                })
                .ToListAsync();

            if (!pools.Any())
            {
                return Content(string.Empty);
            }

            // Get poolId from cookie
            var poolIdFromCookie = Request.Cookies["poolId"];
            int selectedPoolId;

            if (!string.IsNullOrEmpty(poolIdFromCookie) && int.TryParse(poolIdFromCookie, out var cookiePoolId))
            {
                // Use cookie value if valid
                selectedPoolId = pools.Any(p => p.Id == cookiePoolId) ? cookiePoolId : pools.First().Id;
            }
            else
            {
                // Default to latest pool
                selectedPoolId = pools.First().Id;
            }

            var model = new PoolSelectorViewModel
            {
                Pools = pools,
                SelectedPoolId = selectedPoolId
            };

            return View(model);
        }
    }

    public class PoolSelectorViewModel
    {
        public List<PoolSelectItem> Pools { get; set; }
        public int SelectedPoolId { get; set; }
    }

    public class PoolSelectItem
    {
        public int Id { get; set; }
        public int Year { get; set; }
    }
}