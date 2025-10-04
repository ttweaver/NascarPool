namespace WebApp.Models;

public class User
{
    public int Id { get; set; }
    public string UserName { get; set; }
    // Add authentication fields as needed (e.g., Email, PasswordHash)
    public ICollection<Pick> Picks { get; set; } = new List<Pick>();
	public ICollection<Pool> Pools { get; set; } = new List<Pool>();
}