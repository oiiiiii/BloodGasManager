using Microsoft.Data.Sqlite;
using BGShared.Models;
using System.Globalization;

namespace BGManager.Tools;

public class LegacyDbMigrator
{
    public static void Migrate(string oldDbPath, string newDbPath)
    {
        if (File.Exists(newDbPath))
            File.Delete(newDbPath);

        using var oldConn = new SqliteConnection($"Data Source={oldDbPath};");
        oldConn.Open();

        using var newConn = new SqliteConnection($"Data Source={newDbPath};");
        newConn.Open();

        CreateNewSchema(newConn);

        MigratePatients(oldConn, newConn);

        MigrateSpecimens(oldConn, newConn, "test_results", SpecimenStatus.Audited);
        MigrateSpecimens(oldConn, newConn, "temp_results", SpecimenStatus.Pending);

        UpdateStatistics(newConn);
        EnsureIndexes(newConn);
        CheckpointWal(newConn);
    }

    private static void EnsureIndexes(SqliteConnection conn)
    {
        Console.WriteLine("检查并创建缺失的索引...");

        var existingIndexes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        using var idxCmd = conn.CreateCommand();
        idxCmd.CommandText = @"SELECT name FROM sqlite_master WHERE type='index' AND name NOT LIKE 'sqlite_%';";
        using var reader = idxCmd.ExecuteReader();
        while (reader.Read())
        {
            existingIndexes.Add(reader.GetString(0));
        }

        var indexesToCreate = new Dictionary<string, string>
        {
            { "idx_specimens_test_time", "CREATE INDEX idx_specimens_test_time ON specimens(test_time DESC);" },
            { "idx_specimens_patient_id", "CREATE INDEX idx_specimens_patient_id ON specimens(patient_id);" },
            { "idx_specimens_status", "CREATE INDEX idx_specimens_status ON specimens(status);" },
            { "idx_specimens_specimen_no", "CREATE INDEX idx_specimens_specimen_no ON specimens(specimen_no);" },
            { "idx_test_results_specimen_id", "CREATE INDEX idx_test_results_specimen_id ON test_results(specimen_id);" },
            { "idx_test_results_test_code", "CREATE INDEX idx_test_results_test_code ON test_results(test_code);" },
            { "idx_patients_name", "CREATE INDEX idx_patients_name ON patients(patient_name);" },
            { "idx_patients_bed_no", "CREATE INDEX idx_patients_bed_no ON patients(bed_no);" },
            { "idx_patients_active", "CREATE INDEX idx_patients_active ON patients(is_active);" },
            { "idx_system_logs_created", "CREATE INDEX idx_system_logs_created ON system_logs(created_at DESC);" },
            { "idx_system_logs_level", "CREATE INDEX idx_system_logs_level ON system_logs(level);" },
        };

        int createdCount = 0;
        foreach (var (name, sql) in indexesToCreate)
        {
            if (!existingIndexes.Contains(name))
            {
                try
                {
                    using var cmd = conn.CreateCommand();
                    cmd.CommandText = sql;
                    cmd.ExecuteNonQuery();
                    Console.WriteLine($"创建索引: {name}");
                    createdCount++;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"创建索引 {name} 失败: {ex.Message}");
                }
            }
        }

        Console.WriteLine($"索引创建完成: 创建了 {createdCount} 个新索引");
    }

    private static void CreateNewSchema(SqliteConnection conn)
    {
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
PRAGMA journal_mode=WAL;
PRAGMA synchronous=NORMAL;
PRAGMA foreign_keys=ON;

CREATE TABLE schema_version (
    version INTEGER PRIMARY KEY,
    applied_at DATETIME DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE specimens (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    specimen_id TEXT UNIQUE NOT NULL,
    patient_id TEXT DEFAULT '',
    patient_name TEXT DEFAULT '',
    specimen_no TEXT DEFAULT '',
    sample_type TEXT DEFAULT '',
    source INTEGER DEFAULT 0,
    bed_no TEXT DEFAULT '',
    department TEXT DEFAULT '',
    gender TEXT DEFAULT '',
    test_time DATETIME,
    receive_time DATETIME NOT NULL,
    status INTEGER DEFAULT 0,
    audit_time DATETIME,
    audit_user TEXT DEFAULT '',
    raw_message_path TEXT DEFAULT '',
    remark TEXT DEFAULT '',
    original_patient_id TEXT DEFAULT '',
    snapshot_patient_name TEXT DEFAULT '',
    snapshot_bed_no TEXT DEFAULT '',
    snapshot_patient_id TEXT DEFAULT '',
    snapshot_name_abbreviation TEXT DEFAULT '',
    snapshot_department TEXT DEFAULT '',
    snapshot_gender TEXT DEFAULT '',
    snapshot_sample_type TEXT DEFAULT '',
    created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
    updated_at DATETIME
);

CREATE INDEX idx_specimens_test_time ON specimens(test_time DESC);
CREATE INDEX idx_specimens_patient_id ON specimens(patient_id);
CREATE INDEX idx_specimens_status ON specimens(status);
CREATE INDEX idx_specimens_specimen_no ON specimens(specimen_no);

CREATE TABLE test_results (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    specimen_id TEXT NOT NULL,
    test_name TEXT NOT NULL DEFAULT '',
    test_code TEXT NOT NULL DEFAULT '',
    value REAL,
    raw_value TEXT DEFAULT '',
    unit TEXT DEFAULT '',
    flag INTEGER DEFAULT 0,
    min_range REAL,
    max_range REAL,
    result_type TEXT DEFAULT 'M',
    created_at DATETIME DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX idx_test_results_specimen_id ON test_results(specimen_id);
CREATE INDEX idx_test_results_test_code ON test_results(test_code);

CREATE TABLE patients (
    patient_id TEXT PRIMARY KEY,
    patient_name TEXT NOT NULL DEFAULT '',
    name_abbreviation TEXT DEFAULT '',
    gender TEXT DEFAULT '',
    age INTEGER,
    bed_no TEXT DEFAULT '',
    department TEXT DEFAULT '',
    doctor TEXT DEFAULT '',
    phone TEXT DEFAULT '',
    diagnosis TEXT DEFAULT '',
    is_active INTEGER DEFAULT 1,
    remark TEXT DEFAULT '',
    created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
    updated_at DATETIME
);

CREATE INDEX idx_patients_name ON patients(patient_name);
CREATE INDEX idx_patients_bed_no ON patients(bed_no);
CREATE INDEX idx_patients_active ON patients(is_active);

CREATE TABLE system_config (
    key TEXT PRIMARY KEY,
    value TEXT DEFAULT '',
    category TEXT DEFAULT '',
    description TEXT DEFAULT '',
    updated_at DATETIME
);

CREATE TABLE system_logs (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    level TEXT NOT NULL DEFAULT 'INFO',
    category TEXT DEFAULT '',
    message TEXT NOT NULL DEFAULT '',
    detail TEXT DEFAULT '',
    created_at DATETIME DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX idx_system_logs_created ON system_logs(created_at DESC);
CREATE INDEX idx_system_logs_level ON system_logs(level);

CREATE TABLE service_status (
    id INTEGER PRIMARY KEY CHECK(id=1),
    is_running INTEGER DEFAULT 0,
    serial_port TEXT DEFAULT '',
    baud_rate INTEGER DEFAULT 9600,
    last_heartbeat DATETIME,
    total_specimens INTEGER DEFAULT 0,
    today_specimens INTEGER DEFAULT 0,
    last_message_time DATETIME,
    last_error TEXT DEFAULT '',
    version TEXT DEFAULT ''
);

INSERT INTO schema_version (version) VALUES (3);
INSERT INTO service_status (id) VALUES (1);
";
        cmd.ExecuteNonQuery();
    }

    private static void MigratePatients(SqliteConnection oldConn, SqliteConnection newConn)
    {
        using var cmd = oldConn.CreateCommand();
        cmd.CommandText = "SELECT patient_id, patient_name, name_abbreviation, bed_no, department, gender, created_at, is_active FROM patients;";
        using var reader = cmd.ExecuteReader();

        using var tx = newConn.BeginTransaction();
        using var insertCmd = newConn.CreateCommand();
        insertCmd.CommandText = @"
INSERT OR REPLACE INTO patients (patient_id, patient_name, name_abbreviation, gender, bed_no, department, is_active, created_at)
VALUES (@pid, @name, @abbr, @gender, @bed, @dept, @active, @created);";
        insertCmd.Transaction = tx;

        var pPid = insertCmd.Parameters.Add("@pid", SqliteType.Text);
        var pName = insertCmd.Parameters.Add("@name", SqliteType.Text);
        var pAbbr = insertCmd.Parameters.Add("@abbr", SqliteType.Text);
        var pGender = insertCmd.Parameters.Add("@gender", SqliteType.Text);
        var pBed = insertCmd.Parameters.Add("@bed", SqliteType.Text);
        var pDept = insertCmd.Parameters.Add("@dept", SqliteType.Text);
        var pActive = insertCmd.Parameters.Add("@active", SqliteType.Integer);
        var pCreated = insertCmd.Parameters.Add("@created", SqliteType.Text);

        int count = 0;
        while (reader.Read())
        {
            pPid.Value = reader.GetString(0);
            pName.Value = reader.IsDBNull(1) ? "" : reader.GetString(1);
            pAbbr.Value = reader.IsDBNull(2) ? "" : reader.GetString(2);
            pGender.Value = reader.IsDBNull(5) ? "" : reader.GetString(5);
            pBed.Value = reader.IsDBNull(3) ? "" : reader.GetString(3);
            pDept.Value = reader.IsDBNull(4) ? "" : reader.GetString(4);
            pActive.Value = reader.IsDBNull(7) ? 1 : reader.GetInt32(7);
            pCreated.Value = reader.IsDBNull(6) ? DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") : reader.GetString(6);
            insertCmd.ExecuteNonQuery();
            count++;
        }
        tx.Commit();
        Console.WriteLine($"迁移患者: {count} 条");
    }

    private static void MigrateSpecimens(SqliteConnection oldConn, SqliteConnection newConn, string tableName, SpecimenStatus status)
    {
        var columns = GetTableColumns(oldConn, tableName);
        var specimenGroups = new Dictionary<string, List<OldResultRow>>();

        var selectCols = new List<string>
        {
            "patient_id", "specimen_id", "specimen_no", "source", "sample_type",
            "bed_no", "test_name", "value", "test_time"
        };
        var optionalCols = new[]
        {
            "snapshot_patient_name", "snapshot_bed_no", "snapshot_patient_id",
            "snapshot_name_abbreviation", "snapshot_department", "snapshot_gender",
            "original_patient_id", "snapshot_sample_type", "raw_value", "unit", "flag"
        };
        foreach (var col in optionalCols)
            if (columns.Contains(col))
                selectCols.Add(col);

        string sql = $"SELECT {string.Join(", ", selectCols)} FROM {tableName} ORDER BY specimen_id, test_name;";

        using var cmd = oldConn.CreateCommand();
        cmd.CommandText = sql;
        using var reader = cmd.ExecuteReader();

        var colIndex = new Dictionary<string, int>();
        for (int i = 0; i < reader.FieldCount; i++)
            colIndex[reader.GetName(i).ToLower()] = i;

        while (reader.Read())
        {
            string specimenId = GetString(reader, colIndex, "specimen_id");
            if (string.IsNullOrEmpty(specimenId)) continue;

            if (!specimenGroups.ContainsKey(specimenId))
                specimenGroups[specimenId] = new List<OldResultRow>();

            specimenGroups[specimenId].Add(new OldResultRow
            {
                PatientId = GetString(reader, colIndex, "patient_id"),
                SpecimenId = specimenId,
                SpecimenNo = GetString(reader, colIndex, "specimen_no"),
                Source = GetString(reader, colIndex, "source"),
                SampleType = GetString(reader, colIndex, "sample_type"),
                BedNo = GetString(reader, colIndex, "bed_no"),
                TestName = GetString(reader, colIndex, "test_name"),
                Value = GetDouble(reader, colIndex, "value"),
                TestTime = GetString(reader, colIndex, "test_time"),
                SnapshotPatientName = GetString(reader, colIndex, "snapshot_patient_name"),
                SnapshotBedNo = GetString(reader, colIndex, "snapshot_bed_no"),
                SnapshotPatientId = GetString(reader, colIndex, "snapshot_patient_id"),
                SnapshotNameAbbr = GetString(reader, colIndex, "snapshot_name_abbreviation"),
                SnapshotDepartment = GetString(reader, colIndex, "snapshot_department"),
                SnapshotGender = GetString(reader, colIndex, "snapshot_gender"),
                OriginalPatientId = GetString(reader, colIndex, "original_patient_id"),
                SnapshotSampleType = GetString(reader, colIndex, "snapshot_sample_type"),
                RawValue = GetString(reader, colIndex, "raw_value"),
                Unit = GetString(reader, colIndex, "unit"),
                Flag = GetString(reader, colIndex, "flag"),
            });
        }

        Console.WriteLine($"从 {tableName} 读取到 {specimenGroups.Count} 个标本");

        using var tx = newConn.BeginTransaction();

        using var insSpecimen = newConn.CreateCommand();
        insSpecimen.CommandText = @"
INSERT OR IGNORE INTO specimens (
    specimen_id, patient_id, patient_name, specimen_no, sample_type, source, bed_no,
    department, gender, test_time, receive_time, status, audit_time,
    original_patient_id, snapshot_patient_name, snapshot_bed_no, snapshot_patient_id,
    snapshot_name_abbreviation, snapshot_department, snapshot_gender, snapshot_sample_type
) VALUES (
    @sid, @pid, @pname, @sno, @stype, @src, @bed,
    @dept, @gender, @ttime, @rtime, @status, @atime,
    @opid, @spname, @sbed, @spid,
    @sabbr, @sdept, @sgender, @sstype
);";
        insSpecimen.Transaction = tx;

        var pSid = insSpecimen.Parameters.Add("@sid", SqliteType.Text);
        var pPid = insSpecimen.Parameters.Add("@pid", SqliteType.Text);
        var pPname = insSpecimen.Parameters.Add("@pname", SqliteType.Text);
        var pSno = insSpecimen.Parameters.Add("@sno", SqliteType.Text);
        var pStype = insSpecimen.Parameters.Add("@stype", SqliteType.Text);
        var pSrc = insSpecimen.Parameters.Add("@src", SqliteType.Integer);
        var pBed = insSpecimen.Parameters.Add("@bed", SqliteType.Text);
        var pDept = insSpecimen.Parameters.Add("@dept", SqliteType.Text);
        var pGender = insSpecimen.Parameters.Add("@gender", SqliteType.Text);
        var pTtime = insSpecimen.Parameters.Add("@ttime", SqliteType.Text);
        var pRtime = insSpecimen.Parameters.Add("@rtime", SqliteType.Text);
        var pStatus = insSpecimen.Parameters.Add("@status", SqliteType.Integer);
        var pAtime = insSpecimen.Parameters.Add("@atime", SqliteType.Text);
        var pOpid = insSpecimen.Parameters.Add("@opid", SqliteType.Text);
        var pSpname = insSpecimen.Parameters.Add("@spname", SqliteType.Text);
        var pSbed = insSpecimen.Parameters.Add("@sbed", SqliteType.Text);
        var pSpid = insSpecimen.Parameters.Add("@spid", SqliteType.Text);
        var pSabbr = insSpecimen.Parameters.Add("@sabbr", SqliteType.Text);
        var pSdept = insSpecimen.Parameters.Add("@sdept", SqliteType.Text);
        var pSgender = insSpecimen.Parameters.Add("@sgender", SqliteType.Text);
        var pSstype = insSpecimen.Parameters.Add("@sstype", SqliteType.Text);

        using var insResult = newConn.CreateCommand();
        insResult.CommandText = @"
INSERT INTO test_results (specimen_id, test_name, test_code, value, raw_value, unit, flag)
VALUES (@sid, @tname, @tcode, @val, @raw, @unit, @flag);";
        insResult.Transaction = tx;

        var rSid = insResult.Parameters.Add("@sid", SqliteType.Text);
        var rTname = insResult.Parameters.Add("@tname", SqliteType.Text);
        var rTcode = insResult.Parameters.Add("@tcode", SqliteType.Text);
        var rVal = insResult.Parameters.Add("@val", SqliteType.Real);
        var rRaw = insResult.Parameters.Add("@raw", SqliteType.Text);
        var rUnit = insResult.Parameters.Add("@unit", SqliteType.Text);
        var rFlag = insResult.Parameters.Add("@flag", SqliteType.Integer);

        int specimenCount = 0;
        int resultCount = 0;
        int skippedCount = 0;

        foreach (var group in specimenGroups)
        {
            var first = group.Value[0];
            DateTime? testTime = ParseDateTime(first.TestTime);

            pSid.Value = first.SpecimenId;
            pPid.Value = first.PatientId ?? "";
            pPname.Value = "";
            pSno.Value = first.SpecimenNo ?? "";
            pStype.Value = first.SampleType ?? "";
            pSrc.Value = ParseSource(first.Source);
            pBed.Value = first.BedNo ?? "";
            pDept.Value = "";
            pGender.Value = "";
            pTtime.Value = testTime.HasValue ? testTime.Value.ToString("yyyy-MM-dd HH:mm:ss") : DBNull.Value;
            pRtime.Value = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            pStatus.Value = (int)status;
            pAtime.Value = status == SpecimenStatus.Audited && testTime.HasValue
                ? testTime.Value.ToString("yyyy-MM-dd HH:mm:ss")
                : DBNull.Value;
            pOpid.Value = first.OriginalPatientId ?? first.PatientId ?? "";
            pSpname.Value = first.SnapshotPatientName ?? "";
            pSbed.Value = first.SnapshotBedNo ?? "";
            pSpid.Value = first.SnapshotPatientId ?? "";
            pSabbr.Value = first.SnapshotNameAbbr ?? "";
            pSdept.Value = first.SnapshotDepartment ?? "";
            pSgender.Value = first.SnapshotGender ?? "";
            pSstype.Value = first.SnapshotSampleType ?? "";

            int rowsAffected = insSpecimen.ExecuteNonQuery();
            if (rowsAffected == 0)
            {
                skippedCount++;
                continue;
            }
            specimenCount++;

            foreach (var row in group.Value)
            {
                rSid.Value = first.SpecimenId;
                rTname.Value = row.TestName ?? "";
                rTcode.Value = row.TestName ?? "";
                if (row.Value.HasValue)
                    rVal.Value = row.Value.Value;
                else
                    rVal.Value = DBNull.Value;
                rRaw.Value = row.RawValue ?? "";
                rUnit.Value = row.Unit ?? "";
                rFlag.Value = ParseFlag(row.Flag);
                insResult.ExecuteNonQuery();
                resultCount++;
            }
        }

        tx.Commit();
        Console.WriteLine($"迁移标本: {specimenCount} 个, 结果: {resultCount} 条, 跳过: {skippedCount} 个 (来自 {tableName})");
    }

    private static int ParseSource(string source)
    {
        if (string.IsNullOrEmpty(source)) return 0;
        return source.ToLower() switch
        {
            "arterial" or "动脉" => 1,
            "venous" or "静脉" => 2,
            "mixed" or "混合静脉" => 3,
            "capillary" or "毛细血管" => 4,
            "csf" or "脑脊液" => 5,
            _ => 0
        };
    }

    private static int ParseFlag(string flag)
    {
        if (string.IsNullOrEmpty(flag)) return 0;
        return flag.ToLower() switch
        {
            "high" or "h" or "高" => 1,
            "low" or "l" or "低" => 2,
            "critical_high" or "criticalhigh" => 3,
            "critical_low" or "cticallow" => 4,
            _ => 0
        };
    }

    private static DateTime? ParseDateTime(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        if (DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt))
            return dt;
        return null;
    }

    private static HashSet<string> GetTableColumns(SqliteConnection conn, string tableName)
    {
        var cols = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        using var cmd = conn.CreateCommand();
        cmd.CommandText = $"PRAGMA table_info({tableName});";
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            cols.Add(reader.GetString(1));
        }
        return cols;
    }

    private static string GetString(SqliteDataReader reader, Dictionary<string, int> colIndex, string colName)
    {
        if (!colIndex.TryGetValue(colName, out int idx))
            return "";
        return reader.IsDBNull(idx) ? "" : reader.GetString(idx);
    }

    private static double? GetDouble(SqliteDataReader reader, Dictionary<string, int> colIndex, string colName)
    {
        if (!colIndex.TryGetValue(colName, out int idx))
            return null;
        return reader.IsDBNull(idx) ? null : reader.GetDouble(idx);
    }

    private static void UpdateStatistics(SqliteConnection conn)
    {
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT COUNT(*) FROM specimens;";
        int total = Convert.ToInt32(cmd.ExecuteScalar());

        cmd.CommandText = "SELECT COUNT(*) FROM specimens WHERE date(test_time) = date('now');";
        int today = Convert.ToInt32(cmd.ExecuteScalar());

        cmd.CommandText = @"
UPDATE service_status SET total_specimens = @total, today_specimens = @today, last_heartbeat = datetime('now') WHERE id = 1;";
        cmd.Parameters.AddWithValue("@total", total);
        cmd.Parameters.AddWithValue("@today", today);
        cmd.ExecuteNonQuery();

        Console.WriteLine($"统计: 总共 {total} 个标本, 今日 {today} 个");
    }

    private static void CheckpointWal(SqliteConnection conn)
    {
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "PRAGMA wal_checkpoint(TRUNCATE); VACUUM;";
        cmd.ExecuteNonQuery();
        Console.WriteLine("WAL checkpoint + VACUUM 完成");
    }

    private class OldResultRow
    {
        public string PatientId { get; set; } = "";
        public string SpecimenId { get; set; } = "";
        public string SpecimenNo { get; set; } = "";
        public string Source { get; set; } = "";
        public string SampleType { get; set; } = "";
        public string BedNo { get; set; } = "";
        public string TestName { get; set; } = "";
        public double? Value { get; set; }
        public string? TestTime { get; set; }
        public string SnapshotPatientName { get; set; } = "";
        public string SnapshotBedNo { get; set; } = "";
        public string SnapshotPatientId { get; set; } = "";
        public string SnapshotNameAbbr { get; set; } = "";
        public string SnapshotDepartment { get; set; } = "";
        public string SnapshotGender { get; set; } = "";
        public string OriginalPatientId { get; set; } = "";
        public string SnapshotSampleType { get; set; } = "";
        public string RawValue { get; set; } = "";
        public string Unit { get; set; } = "";
        public string Flag { get; set; } = "";
    }
}
