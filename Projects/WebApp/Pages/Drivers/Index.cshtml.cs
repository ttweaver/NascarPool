using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using WebApp.Data;
using WebApp.Models;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;

namespace WebApp.Pages.Drivers
{
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        public IndexModel(ApplicationDbContext context) => _context = context;

        public IList<Driver> Drivers { get; set; } = default!;
        public IList<Pool> Pools { get; set; } = default!;

        [BindProperty(SupportsGet = true)]
        public int SelectedPoolId { get; set; }

        [BindProperty]
        public string DriverName { get; set; } = string.Empty;

        [BindProperty]
        public string CarNumber { get; set; } = string.Empty;

        [BindProperty]
        public int DriverId { get; set; }

        public async Task OnGetAsync(int? poolId)
        {
            Pools = await _context.Pools.OrderByDescending(p => p.Year).ToListAsync();
            if (poolId.HasValue && Pools.Any(p => p.Id == poolId.Value))
            {
                SelectedPoolId = poolId.Value;
            }
            else if (SelectedPoolId == 0 && Pools.Any())
            {
                SelectedPoolId = Pools.First().Id;
            }

            Drivers = await _context.Drivers
                .Include(d => d.Pool)
                .Where(d => d.PoolId == SelectedPoolId)
                .ToListAsync();
        }

        public async Task<IActionResult> OnPostCreateAsync()
        {
            Pools = await _context.Pools.OrderByDescending(p => p.Year).ToListAsync();
            if (SelectedPoolId == 0 && Pools.Any())
            {
                SelectedPoolId = Pools.First().Id;
            }

            if (string.IsNullOrWhiteSpace(DriverName) || string.IsNullOrWhiteSpace(CarNumber))
            {
                ModelState.AddModelError(string.Empty, "Both Name and Car Number are required.");
                await OnGetAsync(SelectedPoolId);
                return Page();
            }

            var pool = Pools.FirstOrDefault(p => p.Id == SelectedPoolId);
            if (pool == null)
            {
                ModelState.AddModelError(string.Empty, "Selected pool not found.");
                await OnGetAsync(SelectedPoolId);
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

            return RedirectToPage(new { PoolId = SelectedPoolId });
        }

        public async Task<IActionResult> OnPostDeleteAsync()
        {
            var driver = await _context.Drivers.FindAsync(DriverId);
            if (driver != null)
            {
                _context.Drivers.Remove(driver);
                await _context.SaveChangesAsync();
            }
            return RedirectToPage(new { PoolId = SelectedPoolId });
        }
    }
}