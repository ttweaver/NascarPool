using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using WebApp.Data;
using WebApp.Models;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;

namespace WebApp.Areas.Manage.Pages.Drivers
{
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        public IndexModel(ApplicationDbContext context) => _context = context;

        public IList<Driver> Drivers { get; set; } = default!;
        public IList<Pool> Pools { get; set; } = default!;

        [BindProperty]
        public string DriverName { get; set; } = string.Empty;

        [BindProperty]
        public string CarNumber { get; set; } = string.Empty;

        [BindProperty(SupportsGet = true)]
        public int? PoolId { get; set; }

        [BindProperty]
        public int DriverId { get; set; }

        public async Task OnGetAsync()
        {
			Pools = await _context.Pools
				.OrderByDescending(p => p.Year)
				.ToListAsync();

			var lastestPool = await Pools.GetLatestPoolYearAsync();

            if (PoolId == null)
            {
                PoolId = lastestPool.Id;
            }

			Drivers = await _context.Drivers
                .Include(d => d.Pool)
                .Where(d => d.PoolId == PoolId)
                .ToListAsync();
        }

        public async Task<IActionResult> OnPostCreateAsync()
        {
            Pools = await _context.Pools.OrderByDescending(p => p.Year).ToListAsync();
            if (PoolId == 0 && Pools.Any())
            {
                PoolId = Pools.First().Id;
            }

            if (string.IsNullOrWhiteSpace(DriverName) || string.IsNullOrWhiteSpace(CarNumber))
            {
                ModelState.AddModelError(string.Empty, "Both Name and Car Number are required.");
                await OnGetAsync();
                return Page();
            }

            var pool = Pools.FirstOrDefault(p => p.Id == PoolId);
            if (pool == null)
            {
                ModelState.AddModelError(string.Empty, "Selected pool not found.");
                await OnGetAsync();
                return Page();
            }

            var driver = new Driver
            {
                Name = DriverName.Trim(),
                CarNumber = CarNumber.Trim(),
                PoolId = pool.Id
            };

            _context.Drivers.Add(driver);
            await _context.SaveChangesAsync();

            return RedirectToPage(new { PoolId = PoolId });
        }

        public async Task<IActionResult> OnPostEditAsync()
        {
            // Ensure pools are available for repopulating the page if we need to redisplay
            Pools = await _context.Pools.OrderByDescending(p => p.Year).ToListAsync();
            if (PoolId == 0 && Pools.Any())
            {
				PoolId = Pools.First().Id;
            }

            // Basic validation
            if (DriverId == 0 || string.IsNullOrWhiteSpace(DriverName) || string.IsNullOrWhiteSpace(CarNumber))
            {
                ModelState.AddModelError(string.Empty, "Driver, Name and Car Number are required.");
                await OnGetAsync();
                return Page();
            }

            var driver = await _context.Drivers.FindAsync(DriverId);
            if (driver == null)
            {
                ModelState.AddModelError(string.Empty, "Driver not found.");
                await OnGetAsync();
                return Page();
            }

            // Update fields
            driver.Name = DriverName.Trim();
            driver.CarNumber = CarNumber.Trim();
            driver.PoolId = PoolId.Value;

            _context.Drivers.Update(driver);
            await _context.SaveChangesAsync();

            return RedirectToPage(new { PoolId = PoolId });
        }

        public async Task<IActionResult> OnPostDeleteAsync()
        {
            var driver = await _context.Drivers.FindAsync(DriverId);
            if (driver != null)
            {
                _context.Drivers.Remove(driver);
                await _context.SaveChangesAsync();
            }
            return RedirectToPage(new { PoolId = PoolId });
        }
    }
}