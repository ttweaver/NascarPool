using System.ComponentModel.DataAnnotations.Schema;

namespace WebApp.Models;

public class Pool
{
    public int Id { get; set; }
    public string Name { get; set; }
    public int Year { get; set; }
    [NotMapped]
    public bool CurrentYear => Year == DateTime.Now.Year;
	public ICollection<User> Members { get; set; } = new List<User>();
    public ICollection<Race> Races { get; set; } = new List<Race>();
    public ICollection<Driver> Drivers { get; set; } = new List<Driver>();
}