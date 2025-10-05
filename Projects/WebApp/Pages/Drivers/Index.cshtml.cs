using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using WebApp.Data;
using WebApp.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace WebApp.Pages.Drivers
{
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        public IndexModel(ApplicationDbContext context) => _context = context;

        public IList<Driver> Drivers { get; set; } = default!;

        [BindProperty]
        public string DriverName { get; set; } = string.Empty;

        [BindProperty]
        public string CarNumber { get; set; } = string.Empty;

        public async Task OnGetAsync()
        {
            Drivers = await _context.Drivers.ToListAsync();
        }

        public async Task<IActionResult> OnPostCreateAsync()
        {
            if (string.IsNullOrWhiteSpace(DriverName) || string.IsNullOrWhiteSpace(CarNumber))
            {
                ModelState.AddModelError(string.Empty, "Both Name and Car Number are required.");
                await OnGetAsync();
                return Page();
            }

            var driver = new Driver
            {
                Name = DriverName.Trim(),
                CarNumber = CarNumber.Trim()
            };

            _context.Drivers.Add(driver);
            await _context.SaveChangesAsync();

            return RedirectToPage();
        }

        [BindProperty]
        public int DriverId { get; set; }

        public async Task<IActionResult> OnPostDeleteAsync()
        {
            var driver = await _context.Drivers.FindAsync(DriverId);
            if (driver != null)
            {
                _context.Drivers.Remove(driver);
                await _context.SaveChangesAsync();
            }
            return RedirectToPage();
        }
    }
}