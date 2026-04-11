using System.ComponentModel.DataAnnotations;

public sealed class Achievement
{
    public int Id { get; set; }

    [MaxLength(100)]
    public string Name { get; set; } = "";

    public List<PlayerAchievement> PlayerAchievements { get; set; } = new();
}
