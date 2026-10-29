using BGShared.Database;
using BGShared.Models;
using Microsoft.Data.Sqlite;

namespace BGService.Data;

public class PatientRepository : IPatientRepository
{
    public List<Patient> GetAll(bool onlyActive = false)
    {
        using var conn = DbConnectionFactory.CreateConnection();
        string sql = "SELECT * FROM patients";
        if (onlyActive)
            sql += " WHERE is_active = 1";
        sql += " ORDER BY patient_name;";
        return SqlHelpers.Query(conn, sql, MapPatient);
    }

    public List<Patient> Search(string keyword, int limit = 50)
    {
        using var conn = DbConnectionFactory.CreateConnection();
        return SqlHelpers.Query(conn,
            @"SELECT * FROM patients
              WHERE patient_id LIKE @kw OR patient_name LIKE @kw OR bed_no LIKE @kw
              ORDER BY patient_name LIMIT @limit;",
            MapPatient,
            ("@kw", $"%{keyword}%"),
            ("@limit", limit));
    }

    public Patient? GetById(string patientId)
    {
        using var conn = DbConnectionFactory.CreateConnection();
        var results = SqlHelpers.Query(conn,
            "SELECT * FROM patients WHERE patient_id = @id LIMIT 1;",
            MapPatient,
            ("@id", patientId));
        return results.Count > 0 ? results[0] : null;
    }

    public Patient? FindByIdOrBedNoOrName(string patientInfo)
    {
        if (string.IsNullOrWhiteSpace(patientInfo))
            return null;

        var allPatients = GetAll(true);
        string info = patientInfo.Trim();

        // 优先匹配床位号
        var patientByBedNo = allPatients.FirstOrDefault(p =>
            !string.IsNullOrEmpty(p.BedNo) && p.BedNo.Equals(info, StringComparison.OrdinalIgnoreCase));
        if (patientByBedNo != null)
            return patientByBedNo;

        // 其次匹配姓名缩写
        var patientByAbbreviation = allPatients.FirstOrDefault(p =>
            !string.IsNullOrEmpty(p.NameAbbreviation) &&
            p.NameAbbreviation.Equals(info, StringComparison.OrdinalIgnoreCase));
        if (patientByAbbreviation != null)
            return patientByAbbreviation;

        // 最后匹配病案号
        var patientById = allPatients.FirstOrDefault(p => p.PatientId.Equals(info, StringComparison.OrdinalIgnoreCase));
        if (patientById != null)
            return patientById;

        return null;
    }

    public Patient? FindByPatientId(string patientId)
    {
        if (string.IsNullOrWhiteSpace(patientId))
            return null;

        return GetById(patientId);
    }

    public bool Exists(string patientId)
    {
        using var conn = DbConnectionFactory.CreateConnection();
        var count = SqlHelpers.ExecuteScalar<long>(conn,
            "SELECT COUNT(*) FROM patients WHERE patient_id = @id;",
            ("@id", patientId));
        return count > 0;
    }

    public bool CheckPatientIdExists(string patientId, string? excludePatientId = null)
    {
        using var conn = DbConnectionFactory.CreateConnection();
        string sql = "SELECT COUNT(*) FROM patients WHERE patient_id = @id AND is_active = 1";
        if (!string.IsNullOrEmpty(excludePatientId))
            sql += " AND patient_id != @excludeId";
        var parameters = new List<(string, object)> { ("@id", patientId) };
        if (!string.IsNullOrEmpty(excludePatientId))
            parameters.Add(("@excludeId", excludePatientId));
        var count = SqlHelpers.ExecuteScalar<long>(conn, sql, parameters.ToArray());
        return count > 0;
    }

    public bool CheckBedNoExists(string bedNo, string? excludePatientId = null)
    {
        using var conn = DbConnectionFactory.CreateConnection();
        string sql = "SELECT COUNT(*) FROM patients WHERE bed_no = @bedNo AND is_active = 1";
        if (!string.IsNullOrEmpty(excludePatientId))
            sql += " AND patient_id != @excludeId";
        var parameters = new List<(string, object)> { ("@bedNo", bedNo) };
        if (!string.IsNullOrEmpty(excludePatientId))
            parameters.Add(("@excludeId", excludePatientId));
        var count = SqlHelpers.ExecuteScalar<long>(conn, sql, parameters.ToArray());
        return count > 0;
    }

    public void Insert(Patient patient)
    {
        using var conn = DbConnectionFactory.CreateConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            INSERT INTO patients (
                patient_id, patient_name, name_abbreviation, gender, age,
                bed_no, department, doctor, phone, diagnosis,
                is_active, remark, created_at, updated_at
            ) VALUES (
                @patientId, @patientName, @nameAbbr, @gender, @age,
                @bedNo, @dept, @doctor, @phone, @diagnosis,
                @isActive, @remark, @createdAt, @updatedAt
            );";

        AddPatientParams(cmd, patient);
        cmd.Parameters.AddWithValue("@createdAt", DateTime.Now);
        cmd.Parameters.AddWithValue("@updatedAt", DateTime.Now);
        cmd.ExecuteNonQuery();
    }

    public bool Update(Patient patient)
    {
        using var conn = DbConnectionFactory.CreateConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            UPDATE patients SET
                patient_name = @patientName,
                name_abbreviation = @nameAbbr,
                gender = @gender,
                age = @age,
                bed_no = @bedNo,
                department = @dept,
                doctor = @doctor,
                phone = @phone,
                diagnosis = @diagnosis,
                is_active = @isActive,
                remark = @remark,
                updated_at = @updatedAt
            WHERE patient_id = @patientId;";

        AddPatientParams(cmd, patient);
        cmd.Parameters.AddWithValue("@updatedAt", DateTime.Now);
        return cmd.ExecuteNonQuery() > 0;
    }

    public bool Delete(string patientId)
    {
        using var conn = DbConnectionFactory.CreateConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "DELETE FROM patients WHERE patient_id = @id;";
        cmd.Parameters.AddWithValue("@id", patientId);
        return cmd.ExecuteNonQuery() > 0;
    }

    private static void AddPatientParams(SqliteCommand cmd, Patient p)
    {
        cmd.Parameters.AddWithValue("@patientId", p.PatientId);
        cmd.Parameters.AddWithValue("@patientName", p.PatientName);
        cmd.Parameters.AddWithValue("@nameAbbr", p.NameAbbreviation);
        cmd.Parameters.AddWithValue("@gender", p.Gender);
        cmd.Parameters.AddWithValue("@age", (object?)p.Age ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@bedNo", p.BedNo);
        cmd.Parameters.AddWithValue("@dept", p.Department);
        cmd.Parameters.AddWithValue("@doctor", p.Doctor);
        cmd.Parameters.AddWithValue("@phone", p.Phone);
        cmd.Parameters.AddWithValue("@diagnosis", p.Diagnosis);
        cmd.Parameters.AddWithValue("@isActive", p.IsActive ? 1 : 0);
        cmd.Parameters.AddWithValue("@remark", p.Remark);
    }

    private static Patient MapPatient(SqliteDataReader reader)
    {
        return new Patient
        {
            PatientId = reader.GetString(0),
            PatientName = reader.GetString(1),
            NameAbbreviation = reader.GetString(2),
            Gender = reader.GetString(3),
            Age = SqlHelpers.GetNullableValue<int>(reader, 4),
            BedNo = reader.GetString(5),
            Department = reader.GetString(6),
            Doctor = reader.GetString(7),
            Phone = reader.GetString(8),
            Diagnosis = reader.GetString(9),
            IsActive = reader.GetInt32(10) == 1,
            Remark = reader.GetString(11),
            CreatedAt = SqlHelpers.GetDateTimeNullable(reader, 12),
            UpdatedAt = SqlHelpers.GetDateTimeNullable(reader, 13)
        };
    }
}
