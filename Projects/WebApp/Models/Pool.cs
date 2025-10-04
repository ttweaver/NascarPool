namespace WebApp.Models;

public class Pool
{
    public int Id { get; set; }
    public string Name { get; set; }
    public ICollection<User> Members { get; set; }
    public ICollection<Race> Races { get; set; }
}