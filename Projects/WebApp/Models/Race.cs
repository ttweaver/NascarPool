using System.ComponentModel.DataAnnotations.Schema;
using WebApp.Validation;

namespace WebApp.Models;

public class Race
{
    public int Id { get; set; }
    public string Name { get; set; }
    public DateTime Date { get; set; }
    public string City { get; set; }
    [UsState]
    public string State { get; set; }
    [NotMapped]
    public string Location => $"{City}, {State}";
    public ICollection<Pick> Picks { get; set; }
    public ICollection<RaceResult> Results { get; set; }
    public int PoolId { get; set; }
    public Pool Pool { get; set; }
}