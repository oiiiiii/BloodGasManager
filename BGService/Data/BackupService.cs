using BGShared.Database;
using BGShared.Models;
using Microsoft.Data.Sqlite;

namespace BGService.Data;

public class BackupService
{
    private readonly SpecimenRepository _specimenRepo;

    public BackupService()
    {
        _specimenRepo = new SpecimenRepository();
    }

    public (int SpecimenCount, string FilePath) ExportDatabase(string outputPath)
    {
        string dbPath = DbConnectionFactory.DefaultDbPath;

        if (!File.Exists(dbPath))
            throw new FileNotFoundException("数据库文件不存在", dbPath);

        // WAL 模式下用 VACUUM INTO 生成一致性快照，避免直接拷贝丢失 wal 内容
        using var conn = DbConnectionFactory.CreateConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = $"VACUUM INTO '{outputPath.Replace("'", "''")}';";
        cmd.CommandTimeout = 300;
        cmd.ExecuteNonQuery();

        int count = (int)_specimenRepo.GetCount();
        return (count, outputPath);
    }

    public (int ImportedCount, int SkippedCount) ImportDatabase(string backupDbPath)
    {
        if (!File.Exists(backupDbPath))
            throw new FileNotFoundException("备份数据库文件不存在", backupDbPath);

        int imported = 0;
        int skipped = 0;

        using var backupConn = new SqliteConnection($"Data Source={backupDbPath};");
        backupConn.Open();

        using var cmd = backupConn.CreateCommand();
        cmd.CommandText = "SELECT specimen_id FROM specimens ORDER BY id;";
        using var reader = cmd.ExecuteReader();

        var specimenIds = new List<string>();
        while (reader.Read())
            specimenIds.Add(reader.GetString(0));

        foreach (var specimenId in specimenIds)
        {
            if (_specimenRepo.Exists(specimenId))
            {
                skipped++;
                continue;
            }

            try
            {
                var specimen = ReadSpecimenFromBackup(backupConn, specimenId);
                if (specimen != null)
                {
                    _specimenRepo.Insert(specimen);
                    imported++;
                }
                else
                {
                    skipped++;
                }
            }
            catch
            {
                skipped++;
            }
        }

        return (imported, skipped);
    }

    private static Specimen? ReadSpecimenFromBackup(SqliteConnection conn, string specimenId)
    {
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT * FROM specimens WHERE specimen_id = @id LIMIT 1;";
        cmd.Parameters.AddWithValue("@id", specimenId);

        using var reader = cmd.ExecuteReader();
        if (!reader.Read()) return null;

        var spec = new Specimen
        {
            Id = reader.GetInt64(reader.GetOrdinal("id")),
            SpecimenId = reader.GetString(reader.GetOrdinal("specimen_id")),
            PatientId = reader.GetString(reader.GetOrdinal("patient_id")),
            PatientName = reader.GetString(reader.GetOrdinal("patient_name")),
            SpecimenNo = reader.GetString(reader.GetOrdinal("specimen_no")),
            SampleType = reader.GetString(reader.GetOrdinal("sample_type")),
            BedNo = reader.GetString(reader.GetOrdinal("bed_no")),
            Department = reader.GetString(reader.GetOrdinal("department")),
            TestTime = GetDateTimeNullable(reader, "test_time"),
            ReceiveTime = reader.GetDateTime(reader.GetOrdinal("receive_time")),
            Status = (SpecimenStatus)reader.GetInt32(reader.GetOrdinal("status")),
            AuditTime = GetDateTimeNullable(reader, "audit_time"),
            AuditUser = reader.GetString(reader.GetOrdinal("audit_user")),
            RawMessagePath = reader.GetString(reader.GetOrdinal("raw_message_path")),
            Remark = reader.GetString(reader.GetOrdinal("remark")),
            CreatedAt = reader.GetDateTime(reader.GetOrdinal("created_at")),
            UpdatedAt = GetDateTimeNullable(reader, "updated_at")
        };

        for (int i = 0; i < reader.FieldCount; i++)
        {
            var colName = reader.GetName(i).ToLower();
            switch (colName)
            {
                case "gender":
                    spec.Gender = reader.GetString(i);
                    break;
                case "original_patient_id":
                    spec.OriginalPatientId = reader.GetString(i);
                    break;
                case "snapshot_patient_name":
                    spec.SnapshotPatientName = reader.GetString(i);
                    break;
                case "snapshot_bed_no":
                    spec.SnapshotBedNo = reader.GetString(i);
                    break;
                case "snapshot_patient_id":
                    spec.SnapshotPatientId = reader.GetString(i);
                    break;
                case "snapshot_name_abbreviation":
                    spec.SnapshotNameAbbreviation = reader.GetString(i);
                    break;
                case "snapshot_department":
                    spec.SnapshotDepartment = reader.GetString(i);
                    break;
                case "snapshot_gender":
                    spec.SnapshotGender = reader.GetString(i);
                    break;
                case "snapshot_sample_type":
                    spec.SnapshotSampleType = reader.GetString(i);
                    break;
            }
        }

        spec.Results = ReadResultsFromBackup(conn, specimenId);
        return spec;
    }

    private static List<TestResult> ReadResultsFromBackup(SqliteConnection conn, string specimenId)
    {
        var results = new List<TestResult>();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT * FROM test_results WHERE specimen_id = @id ORDER BY id;";
        cmd.Parameters.AddWithValue("@id", specimenId);

        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            results.Add(new TestResult
            {
                Id = reader.GetInt64(0),
                SpecimenId = reader.GetString(1),
                TestName = reader.GetString(2),
                TestCode = reader.GetString(3),
                Value = GetNullableDouble(reader, 4),
                RawValue = reader.GetString(5),
                Unit = reader.GetString(6),
                Flag = (ResultFlag)reader.GetInt32(7),
                MinRange = GetNullableDouble(reader, 8),
                MaxRange = GetNullableDouble(reader, 9),
                ResultType = reader.GetString(10),
                CreatedAt = reader.GetDateTime(11)
            });
        }
        return results;
    }

    private static DateTime? GetDateTimeNullable(SqliteDataReader reader, string name)
    {
        int ordinal = reader.GetOrdinal(name);
        if (reader.IsDBNull(ordinal)) return null;
        return reader.GetDateTime(ordinal);
    }

    private static double? GetNullableDouble(SqliteDataReader reader, int ordinal)
    {
        if (reader.IsDBNull(ordinal)) return null;
        return reader.GetDouble(ordinal);
    }
}