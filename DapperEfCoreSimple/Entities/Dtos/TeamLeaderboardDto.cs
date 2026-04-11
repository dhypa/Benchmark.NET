public sealed class TeamLeaderboardDto
{
    public int TeamId { get; set; }
    public string TeamName { get; set; } = "";
    public int PlayerCount { get; set; }
    public double AvgAge { get; set; }
    public double AvgRating { get; set; }
    public decimal MaxSalary { get; set; }
}
