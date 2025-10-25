using WebApp.Data;
using WebApp.Models;

public static class DbInitializer
{
    public static void Seed(ApplicationDbContext context)
    {
        if (context.Pools.Any()) return; // Prevent duplicate seed

        var pool = new Pool
        {
            Name = "2025 Season",
            Year = DateTime.Now.Year
        };

        var drivers = new[]
        {
            new Driver { Name = "Kyle Larson", CarNumber = "5", Pool = pool },
            new Driver { Name = "Chase Elliott", CarNumber = "9", Pool = pool },
            new Driver { Name = "William Byron", CarNumber = "24", Pool = pool },
            new Driver { Name = "Alex Bowman", CarNumber = "48", Pool = pool },
            new Driver { Name = "Denny Hamlin", CarNumber = "11", Pool = pool },
            new Driver { Name = "Martin Truex Jr.", CarNumber = "19", Pool = pool },
            new Driver { Name = "Christopher Bell", CarNumber = "20", Pool = pool },
            new Driver { Name = "Ty Gibbs", CarNumber = "54", Pool = pool },
            new Driver { Name = "Joey Logano", CarNumber = "22", Pool = pool },
            new Driver { Name = "Ryan Blaney", CarNumber = "12", Pool = pool },
            new Driver { Name = "Austin Cindric", CarNumber = "2", Pool = pool },
            new Driver { Name = "Chris Buescher", CarNumber = "17", Pool = pool },
            new Driver { Name = "Brad Keselowski", CarNumber = "6", Pool = pool },
            new Driver { Name = "Ross Chastain", CarNumber = "1", Pool = pool },
            new Driver { Name = "Daniel Suarez", CarNumber = "99", Pool = pool },
            new Driver { Name = "Tyler Reddick", CarNumber = "45", Pool = pool },
            new Driver { Name = "Bubba Wallace", CarNumber = "23", Pool = pool },
            new Driver { Name = "Erik Jones", CarNumber = "43", Pool = pool },
            new Driver { Name = "Carson Hocevar", CarNumber = "77", Pool = pool },
            new Driver { Name = "Corey LaJoie", CarNumber = "7", Pool = pool },
            new Driver { Name = "Michael McDowell", CarNumber = "34", Pool = pool },
            new Driver { Name = "Todd Gilliland", CarNumber = "38", Pool = pool },
            new Driver { Name = "Justin Haley", CarNumber = "51", Pool = pool },
            new Driver { Name = "Harrison Burton", CarNumber = "21", Pool = pool },
            new Driver { Name = "Austin Dillon", CarNumber = "3", Pool = pool },
            new Driver { Name = "Kyle Busch", CarNumber = "8", Pool = pool },
            new Driver { Name = "Zane Smith", CarNumber = "71", Pool = pool },
            new Driver { Name = "Josh Berry", CarNumber = "4", Pool = pool },
        };

        pool.Drivers = drivers.ToList();

        // 2025 NASCAR Cup Series schedule (sample, update dates as needed)
        var races = new[]
        {
            new Race { Name = "Daytona 500", Date = new DateTime(2025, 2, 16), Pool = pool },
            new Race { Name = "Atlanta Motor Speedway", Date = new DateTime(2025, 2, 23), Pool = pool },
            new Race { Name = "Las Vegas Motor Speedway", Date = new DateTime(2025, 3, 2), Pool = pool },
            new Race { Name = "Phoenix Raceway", Date = new DateTime(2025, 3, 9), Pool = pool },
            new Race { Name = "Bristol Motor Speedway", Date = new DateTime(2025, 3, 16), Pool = pool },
            new Race { Name = "Circuit of the Americas", Date = new DateTime(2025, 3, 23), Pool = pool },
            new Race { Name = "Richmond Raceway", Date = new DateTime(2025, 3, 30), Pool = pool },
            new Race { Name = "Martinsville Speedway", Date = new DateTime(2025, 4, 6), Pool = pool },
            new Race { Name = "Texas Motor Speedway", Date = new DateTime(2025, 4, 13), Pool = pool },
            new Race { Name = "Talladega Superspeedway", Date = new DateTime(2025, 4, 20), Pool = pool },
            new Race { Name = "Dover Motor Speedway", Date = new DateTime(2025, 4, 27), Pool = pool },
            new Race { Name = "Kansas Speedway", Date = new DateTime(2025, 5, 4), Pool = pool },
            new Race { Name = "Darlington Raceway", Date = new DateTime(2025, 5, 11), Pool = pool },
            new Race { Name = "Charlotte Motor Speedway", Date = new DateTime(2025, 5, 18), Pool = pool },
            new Race { Name = "World Wide Technology Raceway", Date = new DateTime(2025, 6, 1), Pool = pool },
            new Race { Name = "Sonoma Raceway", Date = new DateTime(2025, 6, 8), Pool = pool },
            new Race { Name = "Nashville Superspeedway", Date = new DateTime(2025, 6, 15), Pool = pool },
            new Race { Name = "New Hampshire Motor Speedway", Date = new DateTime(2025, 6, 22), Pool = pool },
            new Race { Name = "Pocono Raceway", Date = new DateTime(2025, 6, 29), Pool = pool },
            new Race { Name = "Indianapolis Motor Speedway", Date = new DateTime(2025, 7, 6), Pool = pool },
            new Race { Name = "Chicago Street Race", Date = new DateTime(2025, 7, 13), Pool = pool },
            new Race { Name = "Iowa Speedway", Date = new DateTime(2025, 7, 20), Pool = pool },
            new Race { Name = "Watkins Glen International", Date = new DateTime(2025, 7, 27), Pool = pool },
            new Race { Name = "Michigan International Speedway", Date = new DateTime(2025, 8, 3), Pool = pool },
            new Race { Name = "Richmond Raceway", Date = new DateTime(2025, 8, 10), Pool = pool },
            new Race { Name = "Daytona International Speedway", Date = new DateTime(2025, 8, 17), Pool = pool },
            new Race { Name = "Darlington Raceway", Date = new DateTime(2025, 8, 24), Pool = pool },
            new Race { Name = "Kansas Speedway", Date = new DateTime(2025, 8, 31), Pool = pool },
            new Race { Name = "Bristol Motor Speedway", Date = new DateTime(2025, 9, 7), Pool = pool },
            new Race { Name = "Texas Motor Speedway", Date = new DateTime(2025, 9, 14), Pool = pool },
            new Race { Name = "Talladega Superspeedway", Date = new DateTime(2025, 9, 21), Pool = pool },
            new Race { Name = "Charlotte Roval", Date = new DateTime(2025, 9, 28), Pool = pool },
            new Race { Name = "Las Vegas Motor Speedway", Date = new DateTime(2025, 10, 5), Pool = pool },
            new Race { Name = "Homestead-Miami Speedway", Date = new DateTime(2025, 10, 12), Pool = pool },
            new Race { Name = "Martinsville Speedway", Date = new DateTime(2025, 10, 19), Pool = pool },
            new Race { Name = "Phoenix Raceway (Championship)", Date = new DateTime(2025, 10, 26), Pool = pool },
        };

        pool.Races = races.ToList();

        context.Pools.Add(pool);
        context.SaveChanges();
    }
}