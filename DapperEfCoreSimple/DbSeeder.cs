using Microsoft.Data.SqlClient;

namespace DapperEfCoreSimple;


internal static class PlayerSeeder
{
    public static void Seed(string connectionString)
    {
        using var connection = new SqlConnection(connectionString);
        connection.Open();

        // Create table if missing
        var createTableSql = @"
            IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='Players' AND xtype='U')
            CREATE TABLE Players (
                Id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
                Name NVARCHAR(100) NOT NULL,
                Age INT NOT NULL
            );
        ";

        using (var cmd = new SqlCommand(createTableSql, connection))
        {
            cmd.ExecuteNonQuery();
        }

        // Check if already seeded
        var countSql = "SELECT COUNT(*) FROM Players";
        using (var cmd = new SqlCommand(countSql, connection))
        {
            var count = (int)cmd.ExecuteScalar();
            if (count > 0)
                return; // Already seeded
        }

        // Insert seed data
        var insertSql = @"
            INSERT INTO Players (Id, Name, Age) VALUES (@Id, @Name, @Age);
        ";

        var players = new[]
        {
            new Player { Id = Guid.NewGuid(), Name = "Alice", Age = 25 },
            new Player { Id = Guid.NewGuid(), Name = "Bob", Age = 30 },
            new Player { Id = Guid.NewGuid(), Name = "Charlie", Age = 22 }
        };
        
        foreach (var p in players)
        {
            using var cmd = new SqlCommand(insertSql, connection);
            cmd.Parameters.AddWithValue("@Id", p.Id);
            cmd.Parameters.AddWithValue("@Name", p.Name);
            cmd.Parameters.AddWithValue("@Age", p.Age);
            cmd.ExecuteNonQuery();
        }
    }
}
