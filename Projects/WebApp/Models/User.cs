namespace WebApp.Models;

public class User
{
    public int Id { get; set; }
    public string UserName { get; set; }
    public string Email { get; set; } = default!; 
    public ICollection<Pick> Picks { get; set; } = new List<Pick>();
	public ICollection<Pool> Pools { get; set; } = new List<Pool>();
}