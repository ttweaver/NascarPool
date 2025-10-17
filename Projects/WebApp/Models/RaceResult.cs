namespace WebApp.Models
{
    public class RaceResult
    {
        public int Id { get; set; }
        public int RaceId { get; set; }
        public int DriverId { get; set; }
        public int Place { get; set; }

        public Race Race { get; set; } = default!;
        public Driver Driver { get; set; } = default!;

        /// <summary>
        /// Calculates the score for a user based on their primary driver.
        /// If the driver is the user's primary driver and finished 1st, score = Place - 3.
        /// Otherwise, score = Place.
        /// </summary>
        public int CalculateScore(int primaryDriverId)
        {
            if (DriverId == primaryDriverId && Place == 1)
            {
                return Place - 3;
            }
            return Place;
        }
    }
}