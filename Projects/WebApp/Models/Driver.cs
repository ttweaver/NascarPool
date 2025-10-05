namespace WebApp.Models;

public class Driver
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string CarNumber { get; set; }

    // Make driver pool-specific
    public int PoolId { get; set; }
    public Pool Pool { get; set; }
}