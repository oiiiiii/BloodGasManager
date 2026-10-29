using Microsoft.Data.Sqlite;

namespace BGShared.Database;

public static class SchemaMigration
{
    private const int CurrentVersion = 5;

    public static void EnsureSchema(SqliteConnection conn)
    {
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            CREATE TABLE IF NOT EXISTS schema_version (
                version INTEGER PRIMARY KEY,
                applied_at DATETIME DEFAULT CURRENT_TIMESTAMP
            );";
        cmd.ExecuteNonQuery();

        int currentVersion = GetCurrentVersion(conn);
        if (currentVersion >= CurrentVersion) return;

        for (int v = currentVersion + 1; v <= CurrentVersion; v++)
        {
            ApplyMigration(conn, v);
        }
    }

    private static int GetCurrentVersion(SqliteConnection conn)
    {
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT COALESCE(MAX(version), 0) FROM schema_version;";
        var result = cmd.ExecuteScalar();
        return result == null || result == DBNull.Value ? 0 : Convert.ToInt32(result);
    }

    private static void ApplyMigration(SqliteConnection conn, int version)
    {
        using var tx = conn.BeginTransaction();
        try
        {
            var sql = GetMigrationSql(version);
            using var cmd = conn.CreateCommand();
            cmd.CommandText = sql;
            cmd.Transaction = tx;
            cmd.ExecuteNonQuery();

            using var verCmd = conn.CreateCommand();
            verCmd.CommandText = "INSERT INTO schema_version (version) VALUES (@v);";
            verCmd.Transaction = tx;
            verCmd.Parameters.AddWithValue("@v", version);
            verCmd.ExecuteNonQuery();

            tx.Commit();
        }
        catch
        {
            tx.Rollback();
            throw;
        }
    }

    private static string GetMigrationSql(int version)
    {
        return version switch
        {
            1 => MigrationV1(),
            2 => MigrationV2(),
            3 => MigrationV3(),
            4 => MigrationV4(),
            5 => MigrationV5(),
            _ => throw new NotSupportedException($"Unknown migration version: {version}")
        };
    }

    private static string MigrationV1()
    {
        return @"
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
    test_time DATETIME,
    receive_time DATETIME NOT NULL,
    status INTEGER DEFAULT 0,
    audit_time DATETIME,
    audit_user TEXT DEFAULT '',
    raw_message_path TEXT DEFAULT '',
    remark TEXT DEFAULT '',
    created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
    updated_at DATETIME
);

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

INSERT OR IGNORE INTO service_status (id) VALUES (1);

INSERT OR IGNORE INTO system_config (key, value, category, description) VALUES
('serial.port_name', 'COM1', 'serial', '串口号'),
('serial.baud_rate', '9600', 'serial', '波特率'),
('serial.data_bits', '8', 'serial', '数据位'),
('serial.parity', 'None', 'serial', '校验位'),
('serial.stop_bits', 'One', 'serial', '停止位'),
('serial.auto_reconnect', 'true', 'serial', '是否自动重连'),
('serial.reconnect_interval', '5000', 'serial', '重连间隔(毫秒)'),
('archive.message_path', 'archive', 'archive', '报文归档目录'),
('archive.save_hl7', 'true', 'archive', '是否保存原始报文'),
('archive.save_json', 'true', 'archive', '是否保存JSON解析结果'),
('app.audit_required', 'true', 'app', '是否需要审核'),
('api.port', '5000', 'api', 'API服务端口');
";
    }

    private static string MigrationV2()
    {
        return @"
ALTER TABLE specimens ADD COLUMN gender TEXT DEFAULT '';
";
    }

    private static string MigrationV3()
    {
        return @"
ALTER TABLE specimens ADD COLUMN original_patient_id TEXT DEFAULT '';
ALTER TABLE specimens ADD COLUMN snapshot_patient_name TEXT DEFAULT '';
ALTER TABLE specimens ADD COLUMN snapshot_bed_no TEXT DEFAULT '';
ALTER TABLE specimens ADD COLUMN snapshot_patient_id TEXT DEFAULT '';
ALTER TABLE specimens ADD COLUMN snapshot_name_abbreviation TEXT DEFAULT '';
ALTER TABLE specimens ADD COLUMN snapshot_department TEXT DEFAULT '';
ALTER TABLE specimens ADD COLUMN snapshot_gender TEXT DEFAULT '';
ALTER TABLE specimens ADD COLUMN snapshot_sample_type TEXT DEFAULT '';
";
    }

    private static string MigrationV4()
    {
        return @"
CREATE INDEX IF NOT EXISTS idx_specimens_test_time ON specimens(test_time DESC);
CREATE INDEX IF NOT EXISTS idx_specimens_patient_id ON specimens(patient_id);
CREATE INDEX IF NOT EXISTS idx_specimens_snapshot_patient_id ON specimens(snapshot_patient_id);
CREATE INDEX IF NOT EXISTS idx_specimens_status ON specimens(status);
CREATE INDEX IF NOT EXISTS idx_specimens_specimen_no ON specimens(specimen_no);
CREATE INDEX IF NOT EXISTS idx_specimens_bed_no ON specimens(bed_no);
CREATE INDEX IF NOT EXISTS idx_specimens_snapshot_bed_no ON specimens(snapshot_bed_no);
CREATE INDEX IF NOT EXISTS idx_specimens_patient_name ON specimens(patient_name);
CREATE INDEX IF NOT EXISTS idx_specimens_snapshot_patient_name ON specimens(snapshot_patient_name);
CREATE INDEX IF NOT EXISTS idx_specimens_department ON specimens(department);

CREATE UNIQUE INDEX IF NOT EXISTS idx_specimens_specimen_id_unique ON specimens(specimen_id);

CREATE INDEX IF NOT EXISTS idx_test_results_specimen_id ON test_results(specimen_id);
CREATE INDEX IF NOT EXISTS idx_test_results_test_code ON test_results(test_code);

CREATE INDEX IF NOT EXISTS idx_patients_name ON patients(patient_name);
CREATE INDEX IF NOT EXISTS idx_patients_bed_no ON patients(bed_no);
CREATE INDEX IF NOT EXISTS idx_patients_active ON patients(is_active);
CREATE INDEX IF NOT EXISTS idx_patients_name_abbreviation ON patients(name_abbreviation);

CREATE INDEX IF NOT EXISTS idx_system_logs_created ON system_logs(created_at DESC);
CREATE INDEX IF NOT EXISTS idx_system_logs_level ON system_logs(level);
";
    }

    private static string MigrationV5()
    {
        return @"
ALTER TABLE specimens ADD COLUMN snapshot_remark TEXT DEFAULT '';
";
    }
}