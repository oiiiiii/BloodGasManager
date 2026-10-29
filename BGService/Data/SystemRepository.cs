using BGShared.Database;
using BGShared.Models;
using BGShared.Utils;
using Microsoft.Data.Sqlite;
using System.Linq;

namespace BGService.Data;

public class SystemLogRepository : ISystemLogRepository
{
    public long Add(string level, string category, string message, string? detail = null)
    {
        using var conn = DbConnectionFactory.CreateConnection();
        return SqlHelpers.ExecuteNonQuery(conn,
            @"INSERT INTO system_logs (level, category, message, detail)
              VALUES (@level, @cat, @msg, @detail);",
            ("@level", level),
            ("@cat", category),
            ("@msg", message),
            ("@detail", detail ?? (object)DBNull.Value));
    }

    public List<SystemLog> GetRecent(int count = 100, string? level = null, string? category = null)
    {
        using var conn = DbConnectionFactory.CreateConnection();
        var where = new List<string>();
        var param = new List<(string, object)>();

        if (level.HasValue())
        {
            where.Add("level = @level");
            param.Add(("@level", level!));
        }
        if (category.HasValue())
        {
            where.Add("category = @cat");
            param.Add(("@cat", category!));
        }

        string whereSql = where.Count > 0 ? "WHERE " + string.Join(" AND ", where) : "";

        return SqlHelpers.Query(conn,
            $"SELECT * FROM system_logs {whereSql} ORDER BY id DESC LIMIT @count;",
            MapLog,
            param.Concat(new (string, object)[] { ("@count", count) }).ToArray());
    }

    public void CleanOld(int keepDays = 90)
    {
        using var conn = DbConnectionFactory.CreateConnection();
        SqlHelpers.ExecuteNonQuery(conn,
            "DELETE FROM system_logs WHERE created_at < @cutoff;",
            ("@cutoff", DateTime.Now.AddDays(-keepDays)));
    }

    private static SystemLog MapLog(SqliteDataReader reader)
    {
        return new SystemLog
        {
            Id = reader.GetInt64(0),
            Level = reader.GetString(1),
            Category = reader.GetString(2),
            Message = reader.GetString(3),
            Detail = SqlHelpers.GetStringSafe(reader, 4),
            CreatedAt = reader.GetDateTime(5)
        };
    }
}

public class ServiceStatusRepository : IServiceStatusRepository
{
    public ServiceStatus Get()
    {
        using var conn = DbConnectionFactory.CreateConnection();
        var results = SqlHelpers.Query(conn,
            "SELECT is_running, serial_port, baud_rate, last_heartbeat, total_specimens, today_specimens, last_message_time, last_error, version FROM service_status WHERE id = 1;",
            r => new ServiceStatus
            {
                IsRunning = r.GetInt32(0) == 1,
                SerialPort = r.GetString(1),
                BaudRate = r.GetInt32(2),
                LastHeartbeat = SqlHelpers.GetDateTimeNullable(r, 3),
                TotalSpecimens = r.GetInt64(4),
                TodaySpecimens = r.GetInt32(5),
                LastMessageTime = SqlHelpers.GetDateTimeNullable(r, 6),
                LastError = SqlHelpers.GetStringSafe(r, 7),
                Version = SqlHelpers.GetStringSafe(r, 8)
            });
        return results.Count > 0 ? results[0] : new ServiceStatus();
    }

    public void Update(Action<ServiceStatus> updateAction)
    {
        var status = Get();
        updateAction(status);
        Save(status);
    }

    public void Save(ServiceStatus status)
    {
        using var conn = DbConnectionFactory.CreateConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            UPDATE service_status SET
                is_running = @running,
                serial_port = @port,
                baud_rate = @baud,
                last_heartbeat = @heartbeat,
                total_specimens = @total,
                today_specimens = @today,
                last_message_time = @lastMsg,
                last_error = @lastError,
                version = @version
            WHERE id = 1;";

        cmd.Parameters.AddWithValue("@running", status.IsRunning ? 1 : 0);
        cmd.Parameters.AddWithValue("@port", status.SerialPort);
        cmd.Parameters.AddWithValue("@baud", status.BaudRate);
        cmd.Parameters.AddWithValue("@heartbeat", (object?)status.LastHeartbeat ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@total", status.TotalSpecimens);
        cmd.Parameters.AddWithValue("@today", status.TodaySpecimens);
        cmd.Parameters.AddWithValue("@lastMsg", (object?)status.LastMessageTime ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@lastError", status.LastError);
        cmd.Parameters.AddWithValue("@version", status.Version);
        cmd.ExecuteNonQuery();
    }

    public void UpdateHeartbeat()
    {
        using var conn = DbConnectionFactory.CreateConnection();
        SqlHelpers.ExecuteNonQuery(conn,
            "UPDATE service_status SET last_heartbeat = @hb WHERE id = 1;",
            ("@hb", DateTime.Now));
    }
}
