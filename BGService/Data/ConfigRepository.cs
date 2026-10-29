using BGShared.Database;
using BGShared.Models;
using BGShared.Utils;
using Microsoft.Data.Sqlite;

namespace BGService.Data;

public class ConfigRepository
{
    public List<SystemConfig> GetAll(string? category = null)
    {
        using var conn = DbConnectionFactory.CreateConnection();
        string sql = "SELECT key, value, category, description, updated_at FROM system_config";
        if (category.HasValue())
        {
            sql += " WHERE category = @cat";
            sql += " ORDER BY category, key;";
            return SqlHelpers.Query(conn, sql, MapConfig, ("@cat", category!));
        }
        sql += " ORDER BY category, key;";
        return SqlHelpers.Query(conn, sql, MapConfig);
    }

    public Dictionary<string, string> GetAllDict()
    {
        using var conn = DbConnectionFactory.CreateConnection();
        var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var list = SqlHelpers.Query(conn,
            "SELECT key, value, category, description, updated_at FROM system_config;",
            MapConfig);
        foreach (var c in list)
            dict[c.Key] = c.Value;
        return dict;
    }

    public string? GetValue(string key)
    {
        using var conn = DbConnectionFactory.CreateConnection();
        var results = SqlHelpers.Query(conn,
            "SELECT key, value, category, description, updated_at FROM system_config WHERE key = @k LIMIT 1;",
            MapConfig, ("@k", key));
        return results.Count > 0 ? results[0].Value : null;
    }

    public T GetValue<T>(string key, T defaultValue)
    {
        var val = GetValue(key);
        if (val == null) return defaultValue;
        try
        {
            return (T)Convert.ChangeType(val, typeof(T));
        }
        catch
        {
            return defaultValue;
        }
    }

    public void SetValue(string key, string value, string category = "", string description = "")
    {
        using var conn = DbConnectionFactory.CreateConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            INSERT INTO system_config (key, value, category, description, updated_at)
            VALUES (@k, @v, @cat, @desc, @upd)
            ON CONFLICT(key) DO UPDATE SET
                value = excluded.value,
                category = excluded.category,
                description = excluded.description,
                updated_at = excluded.updated_at;";
        cmd.Parameters.AddWithValue("@k", key);
        cmd.Parameters.AddWithValue("@v", value);
        cmd.Parameters.AddWithValue("@cat", category);
        cmd.Parameters.AddWithValue("@desc", description);
        cmd.Parameters.AddWithValue("@upd", DateTime.Now);
        cmd.ExecuteNonQuery();
    }

    private static SystemConfig MapConfig(SqliteDataReader reader)
    {
        return new SystemConfig
        {
            Key = reader.GetString(0),
            Value = reader.GetString(1),
            Category = reader.GetString(2),
            Description = reader.GetString(3),
            UpdatedAt = SqlHelpers.GetDateTimeNullable(reader, 4)
        };
    }
}
