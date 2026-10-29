using Microsoft.Data.Sqlite;

namespace BGManager.Tools;

public class DbIndexChecker
{
    public static void EnsureIndexes(string dbPath)
    {
        using var conn = new SqliteConnection($"Data Source={dbPath};");
        conn.Open();

        var existingIndexes = GetExistingIndexes(conn);
        Console.WriteLine($"现有索引数量: {existingIndexes.Count}");

        int createdCount = 0;

        createdCount += CreateIndexIfNotExists(conn, existingIndexes,
            "idx_specimens_test_time",
            "CREATE INDEX idx_specimens_test_time ON specimens(test_time DESC);");

        createdCount += CreateIndexIfNotExists(conn, existingIndexes,
            "idx_specimens_patient_id",
            "CREATE INDEX idx_specimens_patient_id ON specimens(patient_id);");

        createdCount += CreateIndexIfNotExists(conn, existingIndexes,
            "idx_specimens_status",
            "CREATE INDEX idx_specimens_status ON specimens(status);");

        createdCount += CreateIndexIfNotExists(conn, existingIndexes,
            "idx_specimens_specimen_no",
            "CREATE INDEX idx_specimens_specimen_no ON specimens(specimen_no);");

        createdCount += CreateIndexIfNotExists(conn, existingIndexes,
            "idx_test_results_specimen_id",
            "CREATE INDEX idx_test_results_specimen_id ON test_results(specimen_id);");

        createdCount += CreateIndexIfNotExists(conn, existingIndexes,
            "idx_test_results_test_code",
            "CREATE INDEX idx_test_results_test_code ON test_results(test_code);");

        createdCount += CreateIndexIfNotExists(conn, existingIndexes,
            "idx_patients_name",
            "CREATE INDEX idx_patients_name ON patients(patient_name);");

        createdCount += CreateIndexIfNotExists(conn, existingIndexes,
            "idx_patients_bed_no",
            "CREATE INDEX idx_patients_bed_no ON patients(bed_no);");

        createdCount += CreateIndexIfNotExists(conn, existingIndexes,
            "idx_patients_active",
            "CREATE INDEX idx_patients_active ON patients(is_active);");

        createdCount += CreateIndexIfNotExists(conn, existingIndexes,
            "idx_system_logs_created",
            "CREATE INDEX idx_system_logs_created ON system_logs(created_at DESC);");

        createdCount += CreateIndexIfNotExists(conn, existingIndexes,
            "idx_system_logs_level",
            "CREATE INDEX idx_system_logs_level ON system_logs(level);");

        Console.WriteLine($"索引检查完成: 创建了 {createdCount} 个新索引");
    }

    public static void CheckAndOptimize(string dbPath)
    {
        Console.WriteLine($"检查数据库: {dbPath}");

        using var conn = new SqliteConnection($"Data Source={dbPath};");
        conn.Open();

        var existingIndexes = GetExistingIndexes(conn);
        Console.WriteLine($"当前索引: {existingIndexes.Count} 个");

        int createdCount = 0;

        createdCount += CreateIndexIfNotExists(conn, existingIndexes,
            "idx_specimens_test_time",
            "CREATE INDEX idx_specimens_test_time ON specimens(test_time DESC);");

        createdCount += CreateIndexIfNotExists(conn, existingIndexes,
            "idx_specimens_patient_id",
            "CREATE INDEX idx_specimens_patient_id ON specimens(patient_id);");

        createdCount += CreateIndexIfNotExists(conn, existingIndexes,
            "idx_specimens_status",
            "CREATE INDEX idx_specimens_status ON specimens(status);");

        createdCount += CreateIndexIfNotExists(conn, existingIndexes,
            "idx_specimens_specimen_no",
            "CREATE INDEX idx_specimens_specimen_no ON specimens(specimen_no);");

        createdCount += CreateIndexIfNotExists(conn, existingIndexes,
            "idx_test_results_specimen_id",
            "CREATE INDEX idx_test_results_specimen_id ON test_results(specimen_id);");

        createdCount += CreateIndexIfNotExists(conn, existingIndexes,
            "idx_test_results_test_code",
            "CREATE INDEX idx_test_results_test_code ON test_results(test_code);");

        createdCount += CreateIndexIfNotExists(conn, existingIndexes,
            "idx_patients_name",
            "CREATE INDEX idx_patients_name ON patients(patient_name);");

        createdCount += CreateIndexIfNotExists(conn, existingIndexes,
            "idx_patients_bed_no",
            "CREATE INDEX idx_patients_bed_no ON patients(bed_no);");

        createdCount += CreateIndexIfNotExists(conn, existingIndexes,
            "idx_patients_active",
            "CREATE INDEX idx_patients_active ON patients(is_active);");

        createdCount += CreateIndexIfNotExists(conn, existingIndexes,
            "idx_system_logs_created",
            "CREATE INDEX idx_system_logs_created ON system_logs(created_at DESC);");

        createdCount += CreateIndexIfNotExists(conn, existingIndexes,
            "idx_system_logs_level",
            "CREATE INDEX idx_system_logs_level ON system_logs(level);");

        if (createdCount > 0)
        {
            Console.WriteLine($"创建了 {createdCount} 个索引，执行 VACUUM 优化...");
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "VACUUM;";
            cmd.ExecuteNonQuery();
            Console.WriteLine("VACUUM 完成");
        }

        Console.WriteLine($"优化完成！");
    }

    private static HashSet<string> GetExistingIndexes(SqliteConnection conn)
    {
        var indexes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            SELECT name FROM sqlite_master 
            WHERE type='index' AND name NOT LIKE 'sqlite_%';";
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            indexes.Add(reader.GetString(0));
        }
        return indexes;
    }

    private static int CreateIndexIfNotExists(SqliteConnection conn, HashSet<string> existingIndexes, string indexName, string createSql)
    {
        if (existingIndexes.Contains(indexName))
        {
            Console.WriteLine($"索引 {indexName} 已存在，跳过");
            return 0;
        }

        try
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = createSql;
            cmd.ExecuteNonQuery();
            Console.WriteLine($"创建索引: {indexName}");
            return 1;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"创建索引 {indexName} 失败: {ex.Message}");
            return 0;
        }
    }

    public static void VerifyDatabase(string dbPath)
    {
        Console.WriteLine($"验证数据库: {dbPath}");

        using var conn = new SqliteConnection($"Data Source={dbPath};");
        conn.Open();

        using var cmd = conn.CreateCommand();

        cmd.CommandText = "SELECT COUNT(*) FROM specimens;";
        var specimenCount = Convert.ToInt32(cmd.ExecuteScalar());
        Console.WriteLine($"标本数量: {specimenCount}");

        cmd.CommandText = "SELECT COUNT(*) FROM test_results;";
        var resultCount = Convert.ToInt32(cmd.ExecuteScalar());
        Console.WriteLine($"测试结果数量: {resultCount}");

        cmd.CommandText = "SELECT COUNT(*) FROM patients;";
        var patientCount = Convert.ToInt32(cmd.ExecuteScalar());
        Console.WriteLine($"患者数量: {patientCount}");

        cmd.CommandText = "SELECT version FROM schema_version ORDER BY version DESC LIMIT 1;";
        var version = cmd.ExecuteScalar();
        Console.WriteLine($"数据库版本: {version}");

        var indexes = GetExistingIndexes(conn);
        Console.WriteLine($"索引数量: {indexes.Count}");
        foreach (var idx in indexes)
        {
            Console.WriteLine($"  - {idx}");
        }

        Console.WriteLine("数据库验证完成");
    }
}