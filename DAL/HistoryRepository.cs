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
                SelectedPersonalitiesCsv TEXT NOT NULL DEFAULT '',
                RatingsCsv TEXT NOT NULL DEFAULT '',
                ResultText TEXT NOT NULL,
                CreatedAt TEXT NOT NULL
            )
            """;
        cmd.ExecuteNonQuery();

        // Migration for databases created before personalities were introduced.
        var alterCmd = connection.CreateCommand();
        alterCmd.CommandText = "ALTER TABLE History ADD COLUMN SelectedPersonalitiesCsv TEXT NOT NULL DEFAULT ''";
        try
        {
            alterCmd.ExecuteNonQuery();
        }
        catch (SqliteException)
        {
            // Column already exists; no action needed.
        }

        var alterRatingsCmd = connection.CreateCommand();
        alterRatingsCmd.CommandText = "ALTER TABLE History ADD COLUMN RatingsCsv TEXT NOT NULL DEFAULT ''";
        try
        {
            alterRatingsCmd.ExecuteNonQuery();
        }
        catch (SqliteException)
        {
            // Column already exists; no action needed.
        }
    }

    public void Insert(HistoryEntry entry)
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        var cmd = connection.CreateCommand();
        cmd.CommandText = """
            INSERT INTO History (ImageBase64, ImageExtension, SelectedPersonalitiesCsv, RatingsCsv, ResultText, CreatedAt)
            VALUES ($imageBase64, $imageExtension, $selectedPersonalitiesCsv, $ratingsCsv, $resultText, $createdAt)
            """;
        cmd.Parameters.AddWithValue("$imageBase64", entry.ImageBase64);
        cmd.Parameters.AddWithValue("$imageExtension", entry.ImageExtension);
        cmd.Parameters.AddWithValue("$selectedPersonalitiesCsv", entry.SelectedPersonalitiesCsv);
        cmd.Parameters.AddWithValue("$ratingsCsv", entry.RatingsCsv);
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
        cmd.CommandText = "SELECT Id, ImageBase64, ImageExtension, SelectedPersonalitiesCsv, RatingsCsv, ResultText, CreatedAt FROM History ORDER BY CreatedAt ASC";

        var entries = new List<HistoryEntry>();
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            entries.Add(new HistoryEntry
            {
                Id = reader.GetInt32(0),
                ImageBase64 = reader.GetString(1),
                ImageExtension = reader.GetString(2),
                SelectedPersonalitiesCsv = reader.GetString(3),
                RatingsCsv = reader.GetString(4),
                ResultText = reader.GetString(5),
                CreatedAt = DateTime.Parse(reader.GetString(6))
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
