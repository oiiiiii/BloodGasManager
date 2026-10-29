using Microsoft.Data.Sqlite;

namespace BGShared.Database;

public static class SqlHelpers
{
    public static T? GetNullableValue<T>(SqliteDataReader reader, int ordinal) where T : struct
    {
        if (reader.IsDBNull(ordinal)) return null;
        return (T)reader.GetValue(ordinal);
    }

    public static string GetStringSafe(SqliteDataReader reader, int ordinal)
    {
        if (reader.IsDBNull(ordinal)) return string.Empty;
        return reader.GetString(ordinal) ?? string.Empty;
    }

    public static DateTime? GetDateTimeNullable(SqliteDataReader reader, int ordinal)
    {
        if (reader.IsDBNull(ordinal)) return null;
        return reader.GetDateTime(ordinal);
    }

    public static long ExecuteNonQuery(SqliteConnection conn, string sql, params (string Name, object Value)[] parameters)
    {
        using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        foreach (var (name, value) in parameters)
        {
            cmd.Parameters.AddWithValue(name, value ?? DBNull.Value);
        }
        cmd.ExecuteNonQuery();
        using var idCmd = conn.CreateCommand();
        idCmd.CommandText = "SELECT last_insert_rowid();";
        var idResult = idCmd.ExecuteScalar();
        return Convert.ToInt64(idResult ?? 0);
    }

    public static T? ExecuteScalar<T>(SqliteConnection conn, string sql, params (string Name, object Value)[] parameters)
    {
        using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        foreach (var (name, value) in parameters)
        {
            cmd.Parameters.AddWithValue(name, value ?? DBNull.Value);
        }
        var result = cmd.ExecuteScalar();
        if (result == null || result == DBNull.Value) return default;
        return (T)Convert.ChangeType(result, typeof(T));
    }

    public static List<T> Query<T>(SqliteConnection conn, string sql, Func<SqliteDataReader, T> mapper, params (string Name, object Value)[] parameters)
    {
        var results = new List<T>();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        foreach (var (name, value) in parameters)
        {
            cmd.Parameters.AddWithValue(name, value ?? DBNull.Value);
        }
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            results.Add(mapper(reader));
        }
        return results;
    }
}
