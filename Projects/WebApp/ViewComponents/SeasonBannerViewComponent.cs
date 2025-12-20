using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApp.Data;

namespace WebApp.ViewComponents
{
    public class SeasonBannerViewComponent : ViewComponent
    {
        private readonly ApplicationDbContext _context;

        public SeasonBannerViewComponent(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            // Get poolId from cookie
            var poolIdFromCookie = Request.Cookies["poolId"];
            
            if (string.IsNullOrEmpty(poolIdFromCookie) || !int.TryParse(poolIdFromCookie, out var poolId))
            {
                return Content(string.Empty);
            }

            var pool = await _context.Pools
                .Where(p => p.Id == poolId)
                .Select(p => new { p.Year })
                .FirstOrDefaultAsync();

            if (pool == null)
            {
                return Content(string.Empty);
            }

            var currentYear = DateTime.Now.Year;
            var model = new SeasonBannerViewModel
            {
                Year = pool.Year,
                IsArchived = pool.Year < currentYear,
                IsComingUp = pool.Year > currentYear
            };

            // Only show banner if not current season
            if (!model.IsArchived && !model.IsComingUp)
            {
                return Content(string.Empty);
            }

            return View(model);
        }
    }

    public class SeasonBannerViewModel
    {
        public int Year { get; set; }
        public bool IsArchived { get; set; }
        public bool IsComingUp { get; set; }
    }
}