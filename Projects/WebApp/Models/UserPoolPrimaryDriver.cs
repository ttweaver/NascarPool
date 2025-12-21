namespace WebApp.Models
{
    public class UserPoolPrimaryDriver
    {
        public int Id { get; set; }
        
        public string UserId { get; set; } = string.Empty;
        public ApplicationUser User { get; set; } = default!;
        
        public int PoolId { get; set; }
        public Pool Pool { get; set; } = default!;
        
        public int? PrimaryDriverFirstHalfId { get; set; }
        public Driver? PrimaryDriverFirstHalf { get; set; }
        
        public int? PrimaryDriverSecondHalfId { get; set; }
        public Driver? PrimaryDriverSecondHalf { get; set; }
    }
}