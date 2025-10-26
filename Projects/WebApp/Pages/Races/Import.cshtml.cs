using System.Net.Http;
using System.Threading.Tasks;
using HtmlAgilityPack;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using WebApp.Data;
using WebApp.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace WebApp.Pages.Races
{
    public class ImportModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        public ImportModel(ApplicationDbContext context) => _context = context;

        [BindProperty]
        public int PoolId { get; set; }
        public List<Race> ImportedRaces { get; set; } = new();
        public List<Pool> Pools { get; set; }

        public void OnGet()
        {
            Pools = _context.Pools.ToList();
		}

        public async Task<IActionResult> OnPostAsync()
        {
            var pool = _context.Pools.FirstOrDefault(p => p.Id == PoolId);
            if (pool == null)
            {
                ModelState.AddModelError("", "Pool not found.");
                return Page();
            }

            var url = "https://www.nascar.com/nascar-cup-series/2026/schedule/";
            var races = new List<Race>();

            using var httpClient = new HttpClient();
            var html = await httpClient.GetStringAsync(url);

            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            // This XPath/CSS selector may need adjustment based on actual page structure
            var raceNodes = doc.DocumentNode.SelectNodes("//li[contains(@class,'race-schedule-evet')]");
            if (raceNodes != null)
            {
                foreach (var node in raceNodes)
                {
                    var nameNode = node.SelectSingleNode(".//div[contains(@class,'race-schedule-race-name')]");
                    var dateNode = node.SelectSingleNode(".//div[contains(@class,'race-schedule-event-date')]");

                    var name = nameNode?.InnerText.Trim() ?? "Unknown";
                    var dateStr = dateNode?.InnerText.Trim() ?? "";
                    DateTime date;
                    if (!DateTime.TryParse(dateStr, out date))
                        continue;

                    races.Add(new Race { Name = name, Date = date, Pool = pool });
                }
            }

            if (races.Any())
            {
                _context.Races.AddRange(races);
                await _context.SaveChangesAsync();
                ImportedRaces = races;
            }
            else
            {
                ModelState.AddModelError("", "No races found or unable to parse schedule.");
            }

            return Page();
        }
    }
}
