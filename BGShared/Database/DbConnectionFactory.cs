using Microsoft.Data.Sqlite;

namespace BGShared.Database;

public static class DbConnectionFactory
{
    public static string DefaultDbPath { get; set; } = "bloodgas.db";

    public static SqliteConnection CreateConnection()
    {
        return CreateConnection(DefaultDbPath);
    }

    public static SqliteConnection CreateConnection(string dbPath)
    {
        var connectionString = $"Data Source={dbPath};Pooling=True;";
        var conn = new SqliteConnection(connectionString);
        conn.Open();

        using var cmd = conn.CreateCommand();
        cmd.CommandText = "PRAGMA journal_mode=WAL; PRAGMA synchronous=NORMAL; PRAGMA foreign_keys=ON;";
        cmd.ExecuteNonQuery();

        return conn;
    }

    public static bool DatabaseExists(string dbPath)
    {
        return File.Exists(dbPath);
    }

    public static void Initialize(string dbPath)
    {
        DefaultDbPath = dbPath;
        var dir = Path.GetDirectoryName(dbPath);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            Directory.CreateDirectory(dir);

        using var conn = CreateConnection(dbPath);
        SchemaMigration.EnsureSchema(conn);
    }
}
