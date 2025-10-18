using System.Collections.Generic;
using System.Linq;
using WebApp.Data;

namespace WebApp.Models;

public class Pick
{
    public int Id { get; set; }
    public int RaceId { get; set; }
    public int UserId { get; set; }
    public int Pick1Id { get; set; }
    public int Pick2Id { get; set; }
    public int Pick3Id { get; set; }

    public Race Race { get; set; } = default!;
    public User User { get; set; } = default!;
    public Driver Pick1 { get; set; } = default!;
    public Driver Pick2 { get; set; } = default!;
    public Driver Pick3 { get; set; } = default!;
    public int Points { get; set; }

    /// <summary>
    /// Calculates the total points for this pick for the race.
    /// Uses Pick1Id as the primary driver for scoring.
    /// Saves the updated points to the database.
    /// </summary>
    public void CalculateTotalPoints(ApplicationDbContext context, ICollection<RaceResult> RaceResults)
    {
        var driverIds = new[] { Pick1Id, Pick2Id, Pick3Id };
        // Get all race results for the selected drivers in this race
        var results = RaceResults
            .Where(r => r.RaceId == RaceId && driverIds.Contains(r.DriverId))
            .ToList();

        // Use Pick1Id as the primary driver for scoring
        int total = results.Sum(r => r.CalculateScore(Pick1Id));
        this.Points = total;

        // Save changes to the database
        context.Picks.Update(this);
        context.SaveChanges();
    }
}