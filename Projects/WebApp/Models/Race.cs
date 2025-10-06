namespace WebApp.Models;

public class Race
{
    public int Id { get; set; }
    public string Name { get; set; }
    public DateTime Date { get; set; }
    public ICollection<Pick> Picks { get; set; }
    public int PoolId { get; set; }
    public Pool Pool { get; set; }
}