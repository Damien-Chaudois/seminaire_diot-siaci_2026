using DAL.Models;
using Microsoft.Data.Sqlite;

namespace DAL;

public class PersonalityRepository : IPersonalityRepository
{
    private readonly string _connectionString;

    public PersonalityRepository(string dbPath)
    {
        _connectionString = $"Data Source={dbPath}";
    }

    public void Initialize()
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        var cmd = connection.CreateCommand();
        cmd.CommandText = """
            CREATE TABLE IF NOT EXISTS Personalities (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Name TEXT NOT NULL UNIQUE,
                Description TEXT NOT NULL DEFAULT '',
                FinalPersonality TEXT NOT NULL DEFAULT '',
                Curiosity INTEGER NOT NULL DEFAULT 60,
                Competence INTEGER NOT NULL DEFAULT 60,
                Practicality INTEGER NOT NULL DEFAULT 60,
                AestheticSensitivity INTEGER NOT NULL DEFAULT 60,
                Rigor INTEGER NOT NULL DEFAULT 60,
                LimitedVisionFlag INTEGER NOT NULL DEFAULT 0,
                ElderlyFlag INTEGER NOT NULL DEFAULT 0,
                LowMobilityFlag INTEGER NOT NULL DEFAULT 0,
                LowDigitalLiteracyFlag INTEGER NOT NULL DEFAULT 0,
                AvatarPngBase64 TEXT NOT NULL DEFAULT '',
                CreatedAt TEXT NOT NULL,
                UpdatedAt TEXT NOT NULL
            )
            """;
        cmd.ExecuteNonQuery();

        EnsureColumn(connection, "Description", "TEXT NOT NULL DEFAULT ''");
        EnsureColumn(connection, "FinalPersonality", "TEXT NOT NULL DEFAULT ''");
        EnsureColumn(connection, "Curiosity", "INTEGER NOT NULL DEFAULT 60");
        EnsureColumn(connection, "Competence", "INTEGER NOT NULL DEFAULT 60");
        EnsureColumn(connection, "Practicality", "INTEGER NOT NULL DEFAULT 60");
        EnsureColumn(connection, "AestheticSensitivity", "INTEGER NOT NULL DEFAULT 60");
        EnsureColumn(connection, "Rigor", "INTEGER NOT NULL DEFAULT 60");
        EnsureColumn(connection, "LimitedVisionFlag", "INTEGER NOT NULL DEFAULT 0");
        EnsureColumn(connection, "ElderlyFlag", "INTEGER NOT NULL DEFAULT 0");
        EnsureColumn(connection, "LowMobilityFlag", "INTEGER NOT NULL DEFAULT 0");
        EnsureColumn(connection, "LowDigitalLiteracyFlag", "INTEGER NOT NULL DEFAULT 0");
        EnsureColumn(connection, "AvatarPngBase64", "TEXT NOT NULL DEFAULT ''");
        EnsureColumn(connection, "CreatedAt", "TEXT NOT NULL DEFAULT ''");
        EnsureColumn(connection, "UpdatedAt", "TEXT NOT NULL DEFAULT ''");
    }

    public IEnumerable<PersonalityEntry> GetAll()
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        var cmd = connection.CreateCommand();
        cmd.CommandText = """
            SELECT
                Id,
                Name,
                Description,
                FinalPersonality,
                Curiosity,
                Competence,
                Practicality,
                AestheticSensitivity,
                Rigor,
                LimitedVisionFlag,
                ElderlyFlag,
                LowMobilityFlag,
                LowDigitalLiteracyFlag,
                AvatarPngBase64,
                CreatedAt,
                UpdatedAt
            FROM Personalities
            ORDER BY Name COLLATE NOCASE ASC
            """;

        var list = new List<PersonalityEntry>();
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            list.Add(new PersonalityEntry
            {
                Id = reader.GetInt32(0),
                Name = reader.GetString(1),
                Description = reader.GetString(2),
                FinalPersonality = reader.GetString(3),
                Curiosity = reader.GetInt32(4),
                Competence = reader.GetInt32(5),
                Practicality = reader.GetInt32(6),
                AestheticSensitivity = reader.GetInt32(7),
                Rigor = reader.GetInt32(8),
                LimitedVisionFlag = reader.GetInt32(9) == 1,
                ElderlyFlag = reader.GetInt32(10) == 1,
                LowMobilityFlag = reader.GetInt32(11) == 1,
                LowDigitalLiteracyFlag = reader.GetInt32(12) == 1,
                AvatarPngBase64 = reader.GetString(13),
                CreatedAt = ParseIsoDate(reader.GetString(14)),
                UpdatedAt = ParseIsoDate(reader.GetString(15))
            });
        }

        return list;
    }

    public PersonalityEntry Insert(PersonalityEntry entry)
    {
        var now = DateTime.Now;
        entry.CreatedAt = now;
        entry.UpdatedAt = now;

        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        var cmd = connection.CreateCommand();
        cmd.CommandText = """
            INSERT INTO Personalities (
                Name,
                Description,
                FinalPersonality,
                Curiosity,
                Competence,
                Practicality,
                AestheticSensitivity,
                Rigor,
                LimitedVisionFlag,
                ElderlyFlag,
                LowMobilityFlag,
                LowDigitalLiteracyFlag,
                AvatarPngBase64,
                CreatedAt,
                UpdatedAt)
            VALUES (
                $name,
                $description,
                $finalPersonality,
                $curiosity,
                $competence,
                $practicality,
                $aestheticSensitivity,
                $rigor,
                $limitedVisionFlag,
                $elderlyFlag,
                $lowMobilityFlag,
                $lowDigitalLiteracyFlag,
                $avatarPngBase64,
                $createdAt,
                $updatedAt)
            """;

        Bind(entry, cmd);
        cmd.Parameters.AddWithValue("$createdAt", entry.CreatedAt.ToString("o"));
        cmd.Parameters.AddWithValue("$updatedAt", entry.UpdatedAt.ToString("o"));
        cmd.ExecuteNonQuery();

        cmd.CommandText = "SELECT last_insert_rowid()";
        entry.Id = (int)(long)cmd.ExecuteScalar()!;
        return entry;
    }

    public void Update(PersonalityEntry entry)
    {
        entry.UpdatedAt = DateTime.Now;

        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        var cmd = connection.CreateCommand();
        cmd.CommandText = """
            UPDATE Personalities
            SET
                Name = $name,
                Description = $description,
                FinalPersonality = $finalPersonality,
                Curiosity = $curiosity,
                Competence = $competence,
                Practicality = $practicality,
                AestheticSensitivity = $aestheticSensitivity,
                Rigor = $rigor,
                LimitedVisionFlag = $limitedVisionFlag,
                ElderlyFlag = $elderlyFlag,
                LowMobilityFlag = $lowMobilityFlag,
                LowDigitalLiteracyFlag = $lowDigitalLiteracyFlag,
                AvatarPngBase64 = $avatarPngBase64,
                UpdatedAt = $updatedAt
            WHERE Id = $id
            """;

        Bind(entry, cmd);
        cmd.Parameters.AddWithValue("$updatedAt", entry.UpdatedAt.ToString("o"));
        cmd.Parameters.AddWithValue("$id", entry.Id);
        cmd.ExecuteNonQuery();
    }

    private static void Bind(PersonalityEntry entry, SqliteCommand cmd)
    {
        cmd.Parameters.AddWithValue("$name", entry.Name);
        cmd.Parameters.AddWithValue("$description", entry.Description);
        cmd.Parameters.AddWithValue("$finalPersonality", entry.FinalPersonality);
        cmd.Parameters.AddWithValue("$curiosity", entry.Curiosity);
        cmd.Parameters.AddWithValue("$competence", entry.Competence);
        cmd.Parameters.AddWithValue("$practicality", entry.Practicality);
        cmd.Parameters.AddWithValue("$aestheticSensitivity", entry.AestheticSensitivity);
        cmd.Parameters.AddWithValue("$rigor", entry.Rigor);
        cmd.Parameters.AddWithValue("$limitedVisionFlag", entry.LimitedVisionFlag ? 1 : 0);
        cmd.Parameters.AddWithValue("$elderlyFlag", entry.ElderlyFlag ? 1 : 0);
        cmd.Parameters.AddWithValue("$lowMobilityFlag", entry.LowMobilityFlag ? 1 : 0);
        cmd.Parameters.AddWithValue("$lowDigitalLiteracyFlag", entry.LowDigitalLiteracyFlag ? 1 : 0);
        cmd.Parameters.AddWithValue("$avatarPngBase64", entry.AvatarPngBase64);
    }

    private static DateTime ParseIsoDate(string raw)
    {
        return DateTime.TryParse(raw, out var date) ? date : DateTime.Now;
    }

    private static void EnsureColumn(SqliteConnection connection, string name, string definition)
    {
        var alterCmd = connection.CreateCommand();
        alterCmd.CommandText = $"ALTER TABLE Personalities ADD COLUMN {name} {definition}";
        try
        {
            alterCmd.ExecuteNonQuery();
        }
        catch (SqliteException)
        {
            // Column already exists.
        }
    }
}