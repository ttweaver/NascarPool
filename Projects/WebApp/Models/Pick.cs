namespace WebApp.Models;

public class Pick
{
    public int Id { get; set; }
    public int RaceId { get; set; }
    public int UserId { get; set; }
    public int Pick1Id { get; set; }
    public int Pick2Id { get; set; }
    public int Pick3Id { get; set; }

    public Race Race { get; set; } = default!;
    public User User { get; set; } = default!;
    public Driver Pick1 { get; set; } = default!;
    public Driver Pick2 { get; set; } = default!;
    public Driver Pick3 { get; set; } = default!;
    public int Points { get; set; }
}