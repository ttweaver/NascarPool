namespace WebApp.Models;

public class Pick
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public User User { get; set; }
    public int RaceId { get; set; }
    public Race Race { get; set; }
    public int DriverId { get; set; }
    public Driver Driver { get; set; }
    public int PoolId { get; set; }
    public Pool Pool { get; set; }
    public int Points { get; set; }
}