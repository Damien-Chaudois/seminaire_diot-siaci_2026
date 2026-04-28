using Microsoft.Data.Sqlite;
using DAL.Models;

namespace DAL;

public class ConfigRepository : IConfigRepository
{
    private readonly string _connectionString;

    public ConfigRepository(string dbPath)
    {
        _connectionString = $"Data Source={dbPath}";
    }

    public void Initialize()
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        var cmd = connection.CreateCommand();
        cmd.CommandText = """
            CREATE TABLE IF NOT EXISTS Config (
                [Key] TEXT PRIMARY KEY,
                Value TEXT NOT NULL
            )
            """;
        cmd.ExecuteNonQuery();
    }

    public string Get(string key)
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT Value FROM Config WHERE [Key] = $key";
        cmd.Parameters.AddWithValue("$key", key);

        var result = cmd.ExecuteScalar();
        return result != null ? result.ToString()! : string.Empty;
    }

    public void Set(string key, string value)
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        var cmd = connection.CreateCommand();
        cmd.CommandText = """
            INSERT OR REPLACE INTO Config ([Key], Value)
            VALUES ($key, $value)
            """;
        cmd.Parameters.AddWithValue("$key", key);
        cmd.Parameters.AddWithValue("$value", value);
        cmd.ExecuteNonQuery();
    }
}
