using Microsoft.Data.Sqlite;
using DAL.Models;

namespace DAL;

public class HistoryRepository : IHistoryRepository
{
    private readonly string _connectionString;

    public HistoryRepository(string dbPath)
    {
        _connectionString = $"Data Source={dbPath}";
    }

    public void Initialize()
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        var cmd = connection.CreateCommand();
        cmd.CommandText = """
            CREATE TABLE IF NOT EXISTS History (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                ImageBase64 TEXT NOT NULL,
                ImageExtension TEXT NOT NULL DEFAULT 'jpeg',
                ResultText TEXT NOT NULL,
                CreatedAt TEXT NOT NULL
            )
            """;
        cmd.ExecuteNonQuery();
    }

    public void Insert(HistoryEntry entry)
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        var cmd = connection.CreateCommand();
        cmd.CommandText = """
            INSERT INTO History (ImageBase64, ImageExtension, ResultText, CreatedAt)
            VALUES ($imageBase64, $imageExtension, $resultText, $createdAt)
            """;
        cmd.Parameters.AddWithValue("$imageBase64", entry.ImageBase64);
        cmd.Parameters.AddWithValue("$imageExtension", entry.ImageExtension);
        cmd.Parameters.AddWithValue("$resultText", entry.ResultText);
        cmd.Parameters.AddWithValue("$createdAt", entry.CreatedAt.ToString("o"));
        cmd.ExecuteNonQuery();

        cmd.CommandText = "SELECT last_insert_rowid()";
        entry.Id = (int)(long)cmd.ExecuteScalar()!;
    }

    public IEnumerable<HistoryEntry> GetAll()
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT Id, ImageBase64, ImageExtension, ResultText, CreatedAt FROM History ORDER BY CreatedAt DESC";

        var entries = new List<HistoryEntry>();
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            entries.Add(new HistoryEntry
            {
                Id = reader.GetInt32(0),
                ImageBase64 = reader.GetString(1),
                ImageExtension = reader.GetString(2),
                ResultText = reader.GetString(3),
                CreatedAt = DateTime.Parse(reader.GetString(4))
            });
        }
        return entries;
    }

    public void Delete(int id)
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        var cmd = connection.CreateCommand();
        cmd.CommandText = "DELETE FROM History WHERE Id = $id";
        cmd.Parameters.AddWithValue("$id", id);
        cmd.ExecuteNonQuery();
    }
}
