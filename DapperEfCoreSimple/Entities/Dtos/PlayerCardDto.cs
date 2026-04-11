
// DTOs
public sealed class PlayerCardDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public int Age { get; set; }
    public decimal Salary { get; set; }
    public double Rating { get; set; }
    public string TeamName { get; set; } = "";
    public int LatestSeason { get; set; }
    public int LatestPoints { get; set; }
}
