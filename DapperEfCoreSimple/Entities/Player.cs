using BenchmarkDotNet.Running;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

public sealed class Player
{
    public Guid Id { get; set; }

    [MaxLength(200)]
    public string Name { get; set; } = "";

    public int Age { get; set; }
    public decimal Salary { get; set; }
    public double Rating { get; set; }

    public int TeamId { get; set; }
    public Team Team { get; set; } = null!;

    public List<PlayerSeason> Seasons { get; set; } = new();
    public List<PlayerAchievement> Achievements { get; set; } = new();
}
