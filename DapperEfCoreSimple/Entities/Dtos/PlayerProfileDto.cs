public sealed class PlayerProfileDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public int Age { get; set; }
    public decimal Salary { get; set; }
    public double Rating { get; set; }
    public string TeamName { get; set; } = "";
    public List<SeasonLineDto> Seasons { get; set; } = new();
    public List<AchievementLineDto> Achievements { get; set; } = new();
}
