using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Order;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using System.Data;
using System.Runtime.CompilerServices;

[MemoryDiagnoser]
[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
[CategoriesColumn]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
public class ReadBenchmarks
{
    private const string ConnectionString =
        "Server=localhost;Database=OrmBenchmarks;Trusted_Connection=True;TrustServerCertificate=True;";

    private PooledDbContextFactory<AppDbContext> _dbFactory = null!;
    private AppDbContext _dbContext = null!;

    private SqlConnection _connection = null!;

    private Guid _targetPlayerId;
    private int _targetTeamId;

    private SqlCommand _adoPointRead = null!;
    private SqlCommand _adoRosterPage = null!;
    private SqlCommand _adoLeaderboard = null!;
    private SqlCommand _adoProfile = null!;

    private const int PageSize = 50;
    private const int MinAge = 24;

    private const string PointReadSql = """
        SELECT
            p.Id,
            p.Name,
            p.Age,
            p.Salary,
            p.Rating,
            t.Name AS TeamName,
            latest.Season AS LatestSeason,
            latest.Points AS LatestPoints
        FROM Players p
        INNER JOIN Teams t ON t.Id = p.TeamId
        OUTER APPLY
        (
            SELECT TOP (1) ps.Season, ps.Points
            FROM PlayerSeasons ps
            WHERE ps.PlayerId = p.Id
            ORDER BY ps.Season DESC
        ) AS latest
        WHERE p.Id = @PlayerId;
        """;
    private const string PointReadSqlTemplate = """
    SELECT
        p.Id,
        p.Name,
        p.Age,
        p.Salary,
        p.Rating,
        t.Name AS TeamName,
        latest.Season AS LatestSeason,
        latest.Points AS LatestPoints
    FROM Players p
    INNER JOIN Teams t ON t.Id = p.TeamId
    OUTER APPLY
    (
        SELECT TOP (1) ps.Season, ps.Points
        FROM PlayerSeasons ps
        WHERE ps.PlayerId = p.Id
        ORDER BY ps.Season DESC
    ) AS latest
    WHERE p.Id = {0}
    """;
    private const string RosterPageSql = """
        SELECT TOP (@Take)
            p.Id,
            p.Name,
            p.Age,
            p.Salary,
            p.Rating
        FROM Players p
        WHERE p.TeamId = @TeamId
          AND p.Age >= @MinAge
        ORDER BY p.Rating DESC, p.Id ASC;
        """;
    private readonly FormattableString RosterPageSqlFormattable = null!;

    private const string LeaderboardSql = """
        SELECT
            p.TeamId,
            t.Name AS TeamName,
            COUNT(*) AS PlayerCount,
            AVG(CAST(p.Age AS float)) AS AvgAge,
            AVG(p.Rating) AS AvgRating,
            MAX(p.Salary) AS MaxSalary
        FROM Players p
        INNER JOIN Teams t ON t.Id = p.TeamId
        GROUP BY p.TeamId, t.Name
        ORDER BY AvgRating DESC, p.TeamId ASC;
        """;
    private readonly FormattableString LeaderboardSqlFormattable = null!;

    private const string ProfileSql = """
        SELECT
            p.Id,
            p.Name,
            p.Age,
            p.Salary,
            p.Rating,
            t.Name AS TeamName
        FROM Players p
        INNER JOIN Teams t ON t.Id = p.TeamId
        WHERE p.Id = @PlayerId;

        SELECT
            ps.Season,
            ps.Games,
            ps.Points,
            ps.Assists,
            ps.Rebounds
        FROM PlayerSeasons ps
        WHERE ps.PlayerId = @PlayerId
        ORDER BY ps.Season DESC;

        SELECT
            a.Name,
            pa.AwardedOnUtc
        FROM PlayerAchievements pa
        INNER JOIN Achievements a ON a.Id = pa.AchievementId
        WHERE pa.PlayerId = @PlayerId
        ORDER BY pa.AwardedOnUtc DESC;
        """;
    private readonly FormattableString ProfileSqlFormattable = null!;
    

    private const string RosterPageSqlTemplate = """
    SELECT TOP ({2})
        p.Id,
        p.Name,
        p.Age,
        p.Salary,
        p.Rating
    FROM Players p
    WHERE p.TeamId = {0}
      AND p.Age >= {1}
    ORDER BY p.Rating DESC, p.Id ASC;
    """;

    private const string LeaderboardSqlTemplate = """
    SELECT
        p.TeamId,
        t.Name AS TeamName,
        COUNT(*) AS PlayerCount,
        AVG(CAST(p.Age AS float)) AS AvgAge,
        AVG(p.Rating) AS AvgRating,
        MAX(p.Salary) AS MaxSalary
    FROM Players p
    INNER JOIN Teams t ON t.Id = p.TeamId
    GROUP BY p.TeamId, t.Name
    ORDER BY AvgRating DESC, p.TeamId ASC;
    """;

    // EF Core compiled queries: best-effort hot-path usage for read-only work.
    private static readonly Func<AppDbContext, Guid, PlayerCardDto?> EfPointReadQuery =
        EF.CompileQuery((AppDbContext db, Guid playerId) =>
            db.Players
                .AsNoTracking()
                .Where(p => p.Id == playerId)
                .Select(p => new PlayerCardDto
                {
                    Id = p.Id,
                    Name = p.Name,
                    Age = p.Age,
                    Salary = p.Salary,
                    Rating = p.Rating,
                    TeamName = p.Team.Name,
                    LatestSeason = p.Seasons
                        .OrderByDescending(s => s.Season)
                        .Select(s => (int?)s.Season)
                        .FirstOrDefault() ?? 0,
                    LatestPoints = p.Seasons
                        .OrderByDescending(s => s.Season)
                        .Select(s => (int?)s.Points)
                        .FirstOrDefault() ?? 0
                })
                .FirstOrDefault());

    private static readonly Func<AppDbContext, int, int, int, IEnumerable<RosterItemDto>> EfRosterPageQuery =
        EF.CompileQuery((AppDbContext db, int teamId, int minAge, int take) =>
            db.Players
                .AsNoTracking()
                .Where(p => p.TeamId == teamId && p.Age >= minAge)
                .OrderByDescending(p => p.Rating)
                .ThenBy(p => p.Id)
                .Select(p => new RosterItemDto
                {
                    Id = p.Id,
                    Name = p.Name,
                    Age = p.Age,
                    Salary = p.Salary,
                    Rating = p.Rating
                })
                .Take(take));

    //private static readonly Func<AppDbContext, IEnumerable<TeamLeaderboardDto>> EfLeaderboardQuery =
    //    EF.CompileQuery((AppDbContext db) =>
    //        db.Players
    //            .AsNoTracking()
    //            .GroupBy(p => new { p.TeamId, TeamName = p.Team.Name })
    //            .Select(g => new TeamLeaderboardDto
    //            {
    //                TeamId = g.Key.TeamId,
    //                TeamName = g.Key.TeamName,
    //                PlayerCount = g.Count(),
    //                AvgAge = g.Average(x => x.Age),
    //                AvgRating = g.Average(x => x.Rating),
    //                MaxSalary = g.Max(x => x.Salary)
    //            })
    //            .OrderByDescending(x => x.AvgRating)
    //            .ThenBy(x => x.TeamId)
    //            .AsEnumerable());

    private static readonly Func<AppDbContext, Guid, PlayerProfileDto?> EfProfileQuery =
        EF.CompileQuery((AppDbContext db, Guid playerId) =>
            db.Players
                .AsNoTracking()
                .Where(p => p.Id == playerId)
                .Select(p => new PlayerProfileDto
                {
                    Id = p.Id,
                    Name = p.Name,
                    Age = p.Age,
                    Salary = p.Salary,
                    Rating = p.Rating,
                    TeamName = p.Team.Name,
                    Seasons = p.Seasons
                        .OrderByDescending(s => s.Season)
                        .Select(s => new SeasonLineDto
                        {
                            Season = s.Season,
                            Games = s.Games,
                            Points = s.Points,
                            Assists = s.Assists,
                            Rebounds = s.Rebounds
                        })
                        .ToList(),
                    Achievements = p.Achievements
                        .OrderByDescending(a => a.AwardedOnUtc)
                        .Select(a => new AchievementLineDto
                        {
                            Name = a.Achievement.Name,
                            AwardedOnUtc = a.AwardedOnUtc
                        })
                        .ToList()
                })
                .FirstOrDefault());

    [GlobalSetup]
    public void GlobalSetup()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer(ConnectionString)
            .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking)
            .Options;

        _dbFactory = new PooledDbContextFactory<AppDbContext>(options);
        _dbContext = _dbFactory.CreateDbContext();

        using (var db = new AppDbContext(options))
        {
            db.Database.EnsureDeleted();
            db.Database.EnsureCreated();
            Seed(db);
        }

        _connection = new SqlConnection(ConnectionString);
        _connection.Open();

        _adoPointRead = CreatePreparedCommand(
            _connection,
            PointReadSql,
            ("@PlayerId", SqlDbType.UniqueIdentifier, _targetPlayerId));

        _adoRosterPage = CreatePreparedCommand(
            _connection,
            RosterPageSql,
            ("@TeamId", SqlDbType.Int, _targetTeamId),
            ("@MinAge", SqlDbType.Int, MinAge),
            ("@Take", SqlDbType.Int, PageSize));

        _adoLeaderboard = CreatePreparedCommand(_connection, LeaderboardSql);

        _adoProfile = CreatePreparedCommand(
            _connection,
            ProfileSql,
            ("@PlayerId", SqlDbType.UniqueIdentifier, _targetPlayerId));

        _pointReadSqlFormattable =
    FormattableStringFactory.Create(PointReadSqlTemplate, _targetPlayerId);

        _rosterPageSqlFormattable =
            FormattableStringFactory.Create(RosterPageSqlTemplate, _targetTeamId, MinAge, PageSize);

        _leaderboardSqlFormattable =
            FormattableStringFactory.Create(LeaderboardSqlTemplate);
    }
    private FormattableString _pointReadSqlFormattable = null!;
    private FormattableString _rosterPageSqlFormattable = null!;
    private FormattableString _leaderboardSqlFormattable = null!;
    [GlobalCleanup]
    public void GlobalCleanup()
    {
        _adoPointRead.Dispose();
        _adoRosterPage.Dispose();
        _adoLeaderboard.Dispose();
        _adoProfile.Dispose();
        _connection.Dispose();
    }

    // ---------------------------
    // 1) Point lookup with join + subquery projection
    // ---------------------------

    [BenchmarkCategory("PointRead")]
    [Benchmark(Baseline = true)]
    public PlayerCardDto? AdoNet_PointRead()
    {
        using var reader = _adoPointRead.ExecuteReader(CommandBehavior.SingleRow);
        if (!reader.Read())
            return null;

        return new PlayerCardDto
        {
            Id = reader.GetGuid(0),
            Name = reader.GetString(1),
            Age = reader.GetInt32(2),
            Salary = reader.GetDecimal(3),
            Rating = reader.GetDouble(4),
            TeamName = reader.GetString(5),
            LatestSeason = reader.IsDBNull(6) ? 0 : reader.GetInt32(6),
            LatestPoints = reader.IsDBNull(7) ? 0 : reader.GetInt32(7)
        };
    }

    [BenchmarkCategory("PointRead")]
    [Benchmark]
    public PlayerCardDto? Dapper_PointRead()
    {
        return _connection.QuerySingleOrDefault<PlayerCardDto>(
            PointReadSql,
            new { PlayerId = _targetPlayerId });
    }

    [BenchmarkCategory("PointRead")]
    [Benchmark]
    public PlayerCardDto? EfCore_PointRead()
    {
        
        return EfPointReadQuery(_dbContext, _targetPlayerId);
    }
    [BenchmarkCategory("PointRead")]
    [Benchmark]
    public PlayerCardDto? EfCore_Sql_PointRead()
    {

        return _dbContext.Database
        .SqlQuery<PlayerCardDto>(_pointReadSqlFormattable)
        .FirstOrDefault();
    }

    // ---------------------------
    // 2) Paged roster query
    // ---------------------------

    [BenchmarkCategory("RosterPage")]
    [Benchmark(Baseline = true)]
    public List<RosterItemDto> AdoNet_RosterPage()
    {
        var result = new List<RosterItemDto>(PageSize);

        using var reader = _adoRosterPage.ExecuteReader();
        while (reader.Read())
        {
            result.Add(new RosterItemDto
            {
                Id = reader.GetGuid(0),
                Name = reader.GetString(1),
                Age = reader.GetInt32(2),
                Salary = reader.GetDecimal(3),
                Rating = reader.GetDouble(4)
            });
        }

        return result;
    }

    [BenchmarkCategory("RosterPage")]
    [Benchmark]
    public List<RosterItemDto> Dapper_RosterPage()
    {
        return _connection.Query<RosterItemDto>(
            RosterPageSql,
            new
            {
                TeamId = _targetTeamId,
                MinAge,
                Take = PageSize
            }).AsList();
    }

    [BenchmarkCategory("RosterPage")]
    [Benchmark]
    public List<RosterItemDto> EfCore_RosterPage()
    {
        
        return EfRosterPageQuery(_dbContext, _targetTeamId, MinAge, PageSize).ToList();
    }
    [BenchmarkCategory("RosterPage")]
    [Benchmark]
    public List<RosterItemDto> EfCore_Sql_RosterPage()
    {
        
        return _dbContext.Database.SqlQuery<RosterItemDto>(_rosterPageSqlFormattable).AsNoTracking().ToList();
    }

    // ---------------------------
    // 3) Aggregate leaderboard
    // ---------------------------

    [BenchmarkCategory("Leaderboard")]
    [Benchmark(Baseline = true)]
    public List<TeamLeaderboardDto> AdoNet_Leaderboard()
    {
        var result = new List<TeamLeaderboardDto>(32);

        using var reader = _adoLeaderboard.ExecuteReader();
        while (reader.Read())
        {
            result.Add(new TeamLeaderboardDto
            {
                TeamId = reader.GetInt32(0),
                TeamName = reader.GetString(1),
                PlayerCount = reader.GetInt32(2),
                AvgAge = reader.GetDouble(3),
                AvgRating = reader.GetDouble(4),
                MaxSalary = reader.GetDecimal(5)
            });
        }

        return result;
    }

    [BenchmarkCategory("Leaderboard")]
    [Benchmark]
    public List<TeamLeaderboardDto> Dapper_Leaderboard()
    {
        return _connection.Query<TeamLeaderboardDto>(LeaderboardSql).AsList();
    }

    [BenchmarkCategory("Leaderboard")]
    [Benchmark]
    public List<TeamLeaderboardDto> EfCore_Leaderboard()
    {
        
        return _dbContext.Players
            .AsNoTracking()
            .GroupBy(p => new { p.TeamId, TeamName = p.Team.Name })
            .Select(g => new TeamLeaderboardDto
            {
                TeamId = g.Key.TeamId,
                TeamName = g.Key.TeamName,
                PlayerCount = g.Count(),
                AvgAge = g.Average(x => x.Age),
                AvgRating = g.Average(x => x.Rating),
                MaxSalary = g.Max(x => x.Salary)
            })
            .OrderByDescending(x => x.AvgRating)
            .ThenBy(x => x.TeamId)
            .ToList();
    }
    [BenchmarkCategory("Leaderboard")]
    [Benchmark]
    public List<TeamLeaderboardDto> EfCore_Sql_Leaderboard()
    {
        
        return _dbContext.Database.SqlQuery<TeamLeaderboardDto>(_leaderboardSqlFormattable).AsNoTracking().ToList();
    }

    // ---------------------------
    // 4) Graph/profile load
    // ---------------------------

    [BenchmarkCategory("ProfileGraph")]
    [Benchmark(Baseline = true)]
    public PlayerProfileDto? AdoNet_ProfileGraph()
    {
        using var reader = _adoProfile.ExecuteReader();

        if (!reader.Read())
            return null;

        var profile = new PlayerProfileDto
        {
            Id = reader.GetGuid(0),
            Name = reader.GetString(1),
            Age = reader.GetInt32(2),
            Salary = reader.GetDecimal(3),
            Rating = reader.GetDouble(4),
            TeamName = reader.GetString(5)
        };

        reader.NextResult();
        while (reader.Read())
        {
            profile.Seasons.Add(new SeasonLineDto
            {
                Season = reader.GetInt32(0),
                Games = reader.GetInt32(1),
                Points = reader.GetInt32(2),
                Assists = reader.GetInt32(3),
                Rebounds = reader.GetInt32(4)
            });
        }

        reader.NextResult();
        while (reader.Read())
        {
            profile.Achievements.Add(new AchievementLineDto
            {
                Name = reader.GetString(0),
                AwardedOnUtc = reader.GetDateTime(1)
            });
        }

        return profile;
    }

    [BenchmarkCategory("ProfileGraph")]
    [Benchmark]
    public PlayerProfileDto? Dapper_ProfileGraph()
    {
        using var grid = _connection.QueryMultiple(
            ProfileSql,
            new { PlayerId = _targetPlayerId });

        var head = grid.ReadSingleOrDefault<PlayerProfileDto>();
        if (head is null)
            return null;

        head.Seasons = grid.Read<SeasonLineDto>().AsList();
        head.Achievements = grid.Read<AchievementLineDto>().AsList();

        return head;
    }

    [BenchmarkCategory("ProfileGraph")]
    [Benchmark]
    public PlayerProfileDto? EfCore_ProfileGraph()
    {
        
        return EfProfileQuery(_dbContext, _targetPlayerId);
    }
    [BenchmarkCategory("ProfileGraph")]
    [Benchmark]
    public PlayerProfileDto? EfCore_Sql_ProfileGraph_Split()
    {
        var playerId = _targetPlayerId;

        var head = _dbContext.Database
            .SqlQuery<PlayerProfileHeadDto>(
                $"""
            SELECT
                p.Id,
                p.Name,
                p.Age,
                p.Salary,
                p.Rating,
                t.Name AS TeamName
            FROM Players p
            INNER JOIN Teams t ON t.Id = p.TeamId
            WHERE p.Id = {playerId}
            """)
            .FirstOrDefault();

        if (head is null)
            return null;

        var seasons = _dbContext.Database
            .SqlQuery<SeasonLineDto>(
                $"""
            SELECT
                ps.Season,
                ps.Games,
                ps.Points,
                ps.Assists,
                ps.Rebounds
            FROM PlayerSeasons ps
            WHERE ps.PlayerId = {playerId}
            ORDER BY ps.Season DESC
            """)
            .ToList();

        var achievements = _dbContext.Database
            .SqlQuery<AchievementLineDto>(
                $"""
            SELECT
                a.Name,
                pa.AwardedOnUtc
            FROM PlayerAchievements pa
            INNER JOIN Achievements a ON a.Id = pa.AchievementId
            WHERE pa.PlayerId = {playerId}
            ORDER BY pa.AwardedOnUtc DESC
            """)
            .ToList();

        return new PlayerProfileDto
        {
            Id = head.Id,
            Name = head.Name,
            Age = head.Age,
            Salary = head.Salary,
            Rating = head.Rating,
            TeamName = head.TeamName,
            Seasons = seasons,
            Achievements = achievements
        };
    }

    public sealed class PlayerProfileHeadDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = "";
        public int Age { get; set; }
        public decimal Salary { get; set; }
        public double Rating { get; set; }
        public string TeamName { get; set; } = "";
    }

    private static SqlCommand CreatePreparedCommand(
        SqlConnection connection,
        string sql,
        params (string Name, SqlDbType Type, object Value)[] parameters)
    {
        var cmd = connection.CreateCommand();
        cmd.CommandText = sql;

        foreach (var p in parameters)
        {
            var sqlParameter = cmd.Parameters.Add(p.Name, p.Type);
            sqlParameter.Value = p.Value;
        }

        cmd.Prepare();
        return cmd;
    }

    private void Seed(AppDbContext db)
    {
        const int teamCount = 20;
        const int playersPerTeam = 500;
        const int seasonsPerPlayer = 4;
        const int achievementsToAssignPerPlayer = 2;

        var rng = new Random(42);

        var teams = Enumerable.Range(1, teamCount)
            .Select(i => new Team
            {
                Name = $"Team {i:00}"
            })
            .ToList();

        db.Teams.AddRange(teams);

        var achievements = new List<Achievement>
        {
            new() { Name = "All Star" },
            new() { Name = "MVP" },
            new() { Name = "Defensive Player" },
            new() { Name = "Top Scorer" },
            new() { Name = "Playmaker" }
        };

        db.Achievements.AddRange(achievements);
        db.SaveChanges();

        var players = new List<Player>(teamCount * playersPerTeam);
        var seasons = new List<PlayerSeason>(teamCount * playersPerTeam * seasonsPerPlayer);
        var playerAchievements = new List<PlayerAchievement>(teamCount * playersPerTeam * achievementsToAssignPerPlayer);

        for (var teamIndex = 0; teamIndex < teams.Count; teamIndex++)
        {
            var team = teams[teamIndex];

            for (var i = 0; i < playersPerTeam; i++)
            {
                var player = new Player
                {
                    Id = Guid.NewGuid(),
                    TeamId = team.Id,
                    Name = $"Player {teamIndex + 1:00}-{i + 1:0000}",
                    Age = rng.Next(18, 39),
                    Salary = 50_000m + (decimal)(rng.NextDouble() * 450_000d),
                    Rating = Math.Round(60 + rng.NextDouble() * 40, 3)
                };

                players.Add(player);

                for (var season = 2021; season < 2021 + seasonsPerPlayer; season++)
                {
                    seasons.Add(new PlayerSeason
                    {
                        PlayerId = player.Id,
                        Season = season,
                        Games = rng.Next(20, 83),
                        Points = rng.Next(150, 2400),
                        Assists = rng.Next(20, 700),
                        Rebounds = rng.Next(20, 1100)
                    });
                }

                var awarded = achievements
                    .OrderBy(_ => rng.Next())
                    .Take(achievementsToAssignPerPlayer)
                    .ToList();

                foreach (var achievement in awarded)
                {
                    playerAchievements.Add(new PlayerAchievement
                    {
                        PlayerId = player.Id,
                        AchievementId = achievement.Id,
                        AwardedOnUtc = new DateTime(
                            rng.Next(2021, 2025),
                            rng.Next(1, 13),
                            rng.Next(1, 28),
                            0, 0, 0,
                            DateTimeKind.Utc)
                    });
                }
            }
        }

        db.Players.AddRange(players);
        db.PlayerSeasons.AddRange(seasons);
        db.PlayerAchievements.AddRange(playerAchievements);
        db.SaveChanges();

        // Deterministic targets for the benchmark runs.
        var chosenTeam = db.Teams.OrderBy(t => t.Id).Skip(5).First();
        _targetTeamId = chosenTeam.Id;

        _targetPlayerId = db.Players
            .Where(p => p.TeamId == _targetTeamId)
            .OrderByDescending(p => p.Rating)
            .Select(p => p.Id)
            .First();
    }
}
