using System.ComponentModel.DataAnnotations;

public sealed class Team
{
    public int Id { get; set; }

    [MaxLength(100)]
    public string Name { get; set; } = "";

    public List<Player> Players { get; set; } = new();
}
