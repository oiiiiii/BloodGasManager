using BGShared.Database;
using BGShared.Models;
using BGShared.Utils;
using Microsoft.Data.Sqlite;
using System.Linq;

namespace BGService.Data;

public class SpecimenRepository : ISpecimenRepository
{
    public Specimen? GetBySpecimenId(string specimenId)
    {
        using var conn = DbConnectionFactory.CreateConnection();
        var result = SqlHelpers.Query(conn,
            "SELECT * FROM specimens WHERE specimen_id = @id LIMIT 1;",
            MapSpecimen,
            ("@id", specimenId));

        if (result.Count == 0) return null;
        var spec = result[0];
        spec.Results = GetResultsForSpecimen(conn, specimenId);
        return spec;
    }

    public List<Specimen> GetLatest(int count = 50, SpecimenStatus? status = null)
    {
        using var conn = DbConnectionFactory.CreateConnection();
        string sql = "SELECT * FROM specimens";
        if (status.HasValue)
            sql += " WHERE status = @status";
        sql += " ORDER BY test_time DESC, id DESC LIMIT @count;";

        var parameters = new List<(string, object)> { ("@count", count) };
        if (status.HasValue)
            parameters.Add(("@status", (int)status.Value));

        var specimens = SqlHelpers.Query(conn, sql, MapSpecimen, parameters.ToArray());
        foreach (var s in specimens)
        {
            s.Results = GetResultsForSpecimen(conn, s.SpecimenId);
        }
        return specimens;
    }

    public (List<Specimen> Items, long Total) Query(
        DateTime? fromDate = null,
        DateTime? toDate = null,
        string? specimenNo = null,
        string? patientId = null,
        string? patientName = null,
        string? bedNo = null,
        string? department = null,
        SpecimenStatus? status = null,
        int page = 1,
        int pageSize = 20)
    {
        using var conn = DbConnectionFactory.CreateConnection();
        var where = new List<string>();
        var parameters = new List<(string, object)>();

        if (fromDate.HasValue)
        {
            where.Add("test_time >= @fromDate");
            parameters.Add(("@fromDate", fromDate.Value));
        }
        if (toDate.HasValue)
        {
            where.Add("test_time <= @toDate");
            parameters.Add(("@toDate", toDate.Value));
        }
        if (specimenNo.HasValue())
        {
            where.Add("specimen_no LIKE @specimenNo");
            parameters.Add(("@specimenNo", $"%{specimenNo}%"));
        }
        if (patientId.HasValue())
        {
            where.Add("(TRIM(patient_id) = @patientId OR TRIM(snapshot_patient_id) = @patientId)");
            parameters.Add(("@patientId", patientId.Trim()));
        }
        if (patientName.HasValue())
        {
            where.Add("(TRIM(patient_name) = @patientName OR TRIM(snapshot_patient_name) = @patientName)");
            parameters.Add(("@patientName", patientName.Trim()));
        }
        if (bedNo.HasValue())
        {
            where.Add("(TRIM(bed_no) = @bedNo OR TRIM(snapshot_bed_no) = @bedNo)");
            parameters.Add(("@bedNo", bedNo.Trim()));
        }
        if (department.HasValue())
        {
            where.Add("department = @dept");
            parameters.Add(("@dept", department));
        }
        if (status.HasValue)
        {
            where.Add("status = @status");
            parameters.Add(("@status", (int)status.Value));
        }

        string whereSql = where.Count > 0 ? "WHERE " + string.Join(" AND ", where) : "";

        long total = SqlHelpers.ExecuteScalar<long>(conn,
            $"SELECT COUNT(*) FROM specimens {whereSql};",
            parameters.ToArray());

        int offset = (page - 1) * pageSize;
        var specimens = SqlHelpers.Query(conn,
            $"SELECT * FROM specimens {whereSql} ORDER BY test_time DESC, id DESC LIMIT @limit OFFSET @offset;",
            MapSpecimen,
            parameters.Concat(new (string, object)[] { ("@limit", pageSize), ("@offset", offset) }).ToArray());

        foreach (var s in specimens)
        {
            s.Results = GetResultsForSpecimen(conn, s.SpecimenId);
        }

        return (specimens, total);
    }

    public bool Exists(string specimenId)
    {
        using var conn = DbConnectionFactory.CreateConnection();
        var count = SqlHelpers.ExecuteScalar<long>(conn,
            "SELECT COUNT(*) FROM specimens WHERE specimen_id = @id;",
            ("@id", specimenId));
        return count > 0;
    }

    public long Insert(Specimen specimen)
    {
        using var conn = DbConnectionFactory.CreateConnection();
        using var tx = conn.BeginTransaction();

        try
        {
            long id = InsertSpecimen(conn, specimen, tx);
            specimen.Id = id;

            foreach (var result in specimen.Results)
            {
                result.SpecimenId = specimen.SpecimenId;
                InsertResult(conn, result, tx);
            }

            tx.Commit();
            return id;
        }
        catch
        {
            tx.Rollback();
            throw;
        }
    }

    public long Update(Specimen specimen)
    {
        using var conn = DbConnectionFactory.CreateConnection();
        using var tx = conn.BeginTransaction();

        try
        {
            using var cmd = conn.CreateCommand();
            cmd.Transaction = tx;
            cmd.CommandText = @"
                UPDATE specimens SET
                    patient_id = @patientId,
                    patient_name = @patientName,
                    specimen_no = @specimenNo,
                    sample_type = @sampleType,
                    source = @source,
                    bed_no = @bedNo,
                    department = @department,
                    gender = @gender,
                    test_time = @testTime,
                    status = @status,
                    audit_time = @auditTime,
                    audit_user = @auditUser,
                    raw_message_path = @rawPath,
                    remark = @remark,
                    updated_at = @updatedAt
                WHERE id = @id;";

            cmd.Parameters.AddWithValue("@patientId", specimen.PatientId);
            cmd.Parameters.AddWithValue("@patientName", specimen.PatientName);
            cmd.Parameters.AddWithValue("@specimenNo", specimen.SpecimenNo);
            cmd.Parameters.AddWithValue("@sampleType", specimen.SampleType);
            cmd.Parameters.AddWithValue("@source", specimen.Source ?? string.Empty);
            cmd.Parameters.AddWithValue("@bedNo", specimen.BedNo);
            cmd.Parameters.AddWithValue("@department", specimen.Department);
            cmd.Parameters.AddWithValue("@gender", specimen.Gender);
            cmd.Parameters.AddWithValue("@testTime", (object?)specimen.TestTime ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@status", (int)specimen.Status);
            cmd.Parameters.AddWithValue("@auditTime", (object?)specimen.AuditTime ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@auditUser", specimen.AuditUser);
            cmd.Parameters.AddWithValue("@rawPath", specimen.RawMessagePath);
            cmd.Parameters.AddWithValue("@remark", specimen.Remark);
            cmd.Parameters.AddWithValue("@updatedAt", DateTime.Now);
            cmd.Parameters.AddWithValue("@id", specimen.Id);

            cmd.ExecuteNonQuery();
            tx.Commit();
            return specimen.Id;
        }
        catch
        {
            tx.Rollback();
            throw;
        }
    }

    public bool Audit(long id, string auditUser = "system")
    {
        using var conn = DbConnectionFactory.CreateConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            UPDATE specimens SET
                status = @status,
                audit_time = @auditTime,
                audit_user = @auditUser,
                updated_at = @updatedAt
            WHERE id = @id AND status != @status;";

        cmd.Parameters.AddWithValue("@status", (int)SpecimenStatus.Audited);
        cmd.Parameters.AddWithValue("@auditTime", DateTime.Now);
        cmd.Parameters.AddWithValue("@auditUser", auditUser);
        cmd.Parameters.AddWithValue("@updatedAt", DateTime.Now);
        cmd.Parameters.AddWithValue("@id", id);

        return cmd.ExecuteNonQuery() > 0;
    }

    public bool AuditWithSnapshot(long id,
        string snapshotPatientName,
        string snapshotBedNo,
        string snapshotPatientId,
        string snapshotNameAbbreviation,
        string snapshotDepartment,
        string snapshotGender,
        string snapshotSampleType,
        string snapshotRemark,
        string auditUser = "system")
    {
        using var conn = DbConnectionFactory.CreateConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            UPDATE specimens SET
                status = @status,
                audit_time = @auditTime,
                audit_user = @auditUser,
                snapshot_patient_name = @snapshotPatientName,
                snapshot_bed_no = @snapshotBedNo,
                snapshot_patient_id = @snapshotPatientId,
                snapshot_name_abbreviation = @snapshotNameAbbr,
                snapshot_department = @snapshotDept,
                snapshot_gender = @snapshotGender,
                snapshot_sample_type = @snapshotSampleType,
                snapshot_remark = @snapshotRemark,
                updated_at = @updatedAt
            WHERE id = @id AND status != @status;";

        cmd.Parameters.AddWithValue("@status", (int)SpecimenStatus.Audited);
        cmd.Parameters.AddWithValue("@auditTime", DateTime.Now);
        cmd.Parameters.AddWithValue("@auditUser", auditUser);
        cmd.Parameters.AddWithValue("@snapshotPatientName", snapshotPatientName);
        cmd.Parameters.AddWithValue("@snapshotBedNo", snapshotBedNo);
        cmd.Parameters.AddWithValue("@snapshotPatientId", snapshotPatientId);
        cmd.Parameters.AddWithValue("@snapshotNameAbbr", snapshotNameAbbreviation);
        cmd.Parameters.AddWithValue("@snapshotDept", snapshotDepartment);
        cmd.Parameters.AddWithValue("@snapshotGender", snapshotGender);
        cmd.Parameters.AddWithValue("@snapshotSampleType", snapshotSampleType);
        cmd.Parameters.AddWithValue("@snapshotRemark", snapshotRemark ?? string.Empty);
        cmd.Parameters.AddWithValue("@updatedAt", DateTime.Now);
        cmd.Parameters.AddWithValue("@id", id);

        return cmd.ExecuteNonQuery() > 0;
    }

    public bool Unarchive(long id)
    {
        using var conn = DbConnectionFactory.CreateConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            UPDATE specimens SET
                status = @status,
                audit_time = NULL,
                audit_user = '',
                snapshot_patient_name = '',
                snapshot_bed_no = '',
                snapshot_patient_id = '',
                snapshot_name_abbreviation = '',
                snapshot_department = '',
                snapshot_gender = '',
                snapshot_sample_type = '',
                snapshot_remark = '',
                updated_at = @updatedAt
            WHERE id = @id AND status = @auditedStatus;";

        cmd.Parameters.AddWithValue("@status", (int)SpecimenStatus.Pending);
        cmd.Parameters.AddWithValue("@auditedStatus", (int)SpecimenStatus.Audited);
        cmd.Parameters.AddWithValue("@updatedAt", DateTime.Now);
        cmd.Parameters.AddWithValue("@id", id);

        return cmd.ExecuteNonQuery() > 0;
    }

    public bool Delete(long id)
    {
        using var conn = DbConnectionFactory.CreateConnection();
        using var tx = conn.BeginTransaction();
        try
        {
            using var cmd1 = conn.CreateCommand();
            cmd1.Transaction = tx;
            cmd1.CommandText = "DELETE FROM test_results WHERE specimen_id = (SELECT specimen_id FROM specimens WHERE id = @id);";
            cmd1.Parameters.AddWithValue("@id", id);
            cmd1.ExecuteNonQuery();

            using var cmd2 = conn.CreateCommand();
            cmd2.Transaction = tx;
            cmd2.CommandText = "DELETE FROM specimens WHERE id = @id;";
            cmd2.Parameters.AddWithValue("@id", id);
            int rows = cmd2.ExecuteNonQuery();

            tx.Commit();
            return rows > 0;
        }
        catch
        {
            tx.Rollback();
            throw;
        }
    }

    public int DeleteOlderThan(DateTime cutoff)
    {
        using var conn = DbConnectionFactory.CreateConnection();
        using var tx = conn.BeginTransaction();
        try
        {
            using var cmd1 = conn.CreateCommand();
            cmd1.Transaction = tx;
            cmd1.CommandText = "DELETE FROM test_results WHERE specimen_id IN (SELECT specimen_id FROM specimens WHERE COALESCE(test_time, receive_time) < @cutoff);";
            cmd1.Parameters.AddWithValue("@cutoff", cutoff);
            cmd1.ExecuteNonQuery();

            using var cmd2 = conn.CreateCommand();
            cmd2.Transaction = tx;
            cmd2.CommandText = "DELETE FROM specimens WHERE COALESCE(test_time, receive_time) < @cutoff;";
            cmd2.Parameters.AddWithValue("@cutoff", cutoff);
            int rows = cmd2.ExecuteNonQuery();

            tx.Commit();
            return rows;
        }
        catch
        {
            tx.Rollback();
            throw;
        }
    }

    public long GetCount(SpecimenStatus? status = null)
    {
        using var conn = DbConnectionFactory.CreateConnection();
        string sql = "SELECT COUNT(*) FROM specimens";
        if (status.HasValue)
        {
            sql += " WHERE status = @status";
            return SqlHelpers.ExecuteScalar<long>(conn, sql, ("@status", (int)status.Value));
        }
        return SqlHelpers.ExecuteScalar<long>(conn, sql);
    }

    public int GetTodayCount()
    {
        using var conn = DbConnectionFactory.CreateConnection();
        var today = DateTime.Today;
        return (int)SqlHelpers.ExecuteScalar<long>(conn,
            "SELECT COUNT(*) FROM specimens WHERE test_time >= @today;",
            ("@today", today));
    }

    private long InsertSpecimen(SqliteConnection conn, Specimen s, SqliteTransaction tx)
    {
        using var cmd = conn.CreateCommand();
        cmd.Transaction = tx;
        cmd.CommandText = @"
            INSERT INTO specimens (
                specimen_id, patient_id, patient_name, specimen_no,
                sample_type, source, bed_no, department, gender,
                original_patient_id,
                test_time, receive_time, status, raw_message_path
            ) VALUES (
                @specimenId, @patientId, @patientName, @specimenNo,
                @sampleType, @source, @bedNo, @department, @gender,
                @originalPatientId,
                @testTime, @receiveTime, @status, @rawPath
            );";

        cmd.Parameters.AddWithValue("@specimenId", s.SpecimenId);
        cmd.Parameters.AddWithValue("@patientId", s.PatientId);
        cmd.Parameters.AddWithValue("@patientName", s.PatientName);
        cmd.Parameters.AddWithValue("@specimenNo", s.SpecimenNo);
        cmd.Parameters.AddWithValue("@sampleType", s.SampleType);
        cmd.Parameters.AddWithValue("@source", s.Source ?? string.Empty);
        cmd.Parameters.AddWithValue("@bedNo", s.BedNo);
        cmd.Parameters.AddWithValue("@department", s.Department);
        cmd.Parameters.AddWithValue("@gender", s.Gender);
        cmd.Parameters.AddWithValue("@originalPatientId", s.OriginalPatientId);
        cmd.Parameters.AddWithValue("@testTime", (object?)s.TestTime ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@receiveTime", s.ReceiveTime);
        cmd.Parameters.AddWithValue("@status", (int)s.Status);
        cmd.Parameters.AddWithValue("@rawPath", s.RawMessagePath);

        cmd.ExecuteNonQuery();
        using var idCmd = conn.CreateCommand();
        idCmd.Transaction = tx;
        idCmd.CommandText = "SELECT last_insert_rowid();";
        var idResult = idCmd.ExecuteScalar();
        return Convert.ToInt64(idResult ?? 0);
    }

    private long InsertResult(SqliteConnection conn, TestResult r, SqliteTransaction tx)
    {
        using var cmd = conn.CreateCommand();
        cmd.Transaction = tx;
        cmd.CommandText = @"
            INSERT INTO test_results (
                specimen_id, test_name, test_code, value, raw_value,
                unit, flag, min_range, max_range, result_type
            ) VALUES (
                @specimenId, @testName, @testCode, @value, @rawValue,
                @unit, @flag, @minRange, @maxRange, @resultType
            );";

        cmd.Parameters.AddWithValue("@specimenId", r.SpecimenId);
        cmd.Parameters.AddWithValue("@testName", r.TestName);
        cmd.Parameters.AddWithValue("@testCode", r.TestCode);
        cmd.Parameters.AddWithValue("@value", (object?)r.Value ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@rawValue", r.RawValue);
        cmd.Parameters.AddWithValue("@unit", r.Unit);
        cmd.Parameters.AddWithValue("@flag", (int)r.Flag);
        cmd.Parameters.AddWithValue("@minRange", (object?)r.MinRange ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@maxRange", (object?)r.MaxRange ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@resultType", r.ResultType);

        cmd.ExecuteNonQuery();
        using var idCmd = conn.CreateCommand();
        idCmd.Transaction = tx;
        idCmd.CommandText = "SELECT last_insert_rowid();";
        var idResult = idCmd.ExecuteScalar();
        return Convert.ToInt64(idResult ?? 0);
    }

    private List<TestResult> GetResultsForSpecimen(SqliteConnection conn, string specimenId)
    {
        return SqlHelpers.Query(conn,
            "SELECT * FROM test_results WHERE specimen_id = @id ORDER BY id;",
            MapResult,
            ("@id", specimenId));
    }

    private static Specimen MapSpecimen(SqliteDataReader reader)
    {
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
            TestTime = SqlHelpers.GetDateTimeNullable(reader, reader.GetOrdinal("test_time")),
            ReceiveTime = reader.GetDateTime(reader.GetOrdinal("receive_time")),
            Status = (SpecimenStatus)reader.GetInt32(reader.GetOrdinal("status")),
            AuditTime = SqlHelpers.GetDateTimeNullable(reader, reader.GetOrdinal("audit_time")),
            AuditUser = reader.GetString(reader.GetOrdinal("audit_user")),
            RawMessagePath = reader.GetString(reader.GetOrdinal("raw_message_path")),
            Remark = reader.GetString(reader.GetOrdinal("remark")),
            CreatedAt = reader.GetDateTime(reader.GetOrdinal("created_at")),
            UpdatedAt = SqlHelpers.GetDateTimeNullable(reader, reader.GetOrdinal("updated_at"))
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
                case "snapshot_remark":
                    spec.SnapshotRemark = reader.GetString(i);
                    break;
            }
        }

        return spec;
    }

    private static TestResult MapResult(SqliteDataReader reader)
    {
        return new TestResult
        {
            Id = reader.GetInt64(0),
            SpecimenId = reader.GetString(1),
            TestName = reader.GetString(2),
            TestCode = reader.GetString(3),
            Value = SqlHelpers.GetNullableValue<double>(reader, 4),
            RawValue = reader.GetString(5),
            Unit = reader.GetString(6),
            Flag = (ResultFlag)reader.GetInt32(7),
            MinRange = SqlHelpers.GetNullableValue<double>(reader, 8),
            MaxRange = SqlHelpers.GetNullableValue<double>(reader, 9),
            ResultType = reader.GetString(10),
            CreatedAt = reader.GetDateTime(11)
        };
    }
}
