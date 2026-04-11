public sealed class PlayerAchievement
{
    public Guid PlayerId { get; set; }
    public Player Player { get; set; } = null!;

    public int AchievementId { get; set; }
    public Achievement Achievement { get; set; } = null!;

    public DateTime AwardedOnUtc { get; set; }
}
