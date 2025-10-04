namespace WebApp.Models;

public class Race
{
    public int Id { get; set; }
    public string Name { get; set; }
    public DateTime Date { get; set; }
    public ICollection<Pick> Picks { get; set; }
}