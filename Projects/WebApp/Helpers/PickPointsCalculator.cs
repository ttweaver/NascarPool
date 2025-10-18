using WebApp.Data;
using WebApp.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;

namespace WebApp.Helpers
{
    public static class PickPointsCalculator
    {
        /// <summary>
        /// Gets all picks for a race, loads their race results, and calculates total points for each pick.
        /// </summary>
        public static async Task<List<Pick>> CalculateAllPicksPointsAsync(ApplicationDbContext context, int raceId)
        {
            // Get all picks for the race
            var picks = await context.Picks
                .Include(p => p.Race.Results)
                .Where(p => p.RaceId == raceId)
                .ToListAsync();

            foreach (var pick in picks)
            {
                pick.CalculateTotalPoints(context, pick.Race.Results);
            }

            return picks;
        }
    }
}