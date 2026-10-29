using Microsoft.Data.Sqlite;

namespace BGManager.Tools;

public static class DbDiagnostic
{
    public static void Run(string dbPath)
    {
        Console.WriteLine($"===== 数据库诊断工具 =====");
        Console.WriteLine($"数据库路径: {dbPath}");
        Console.WriteLine($"文件存在: {File.Exists(dbPath)}");
        
        if (File.Exists(dbPath))
        {
            var fileInfo = new FileInfo(dbPath);
            Console.WriteLine($"文件大小: {fileInfo.Length} bytes ({(fileInfo.Length / 1024):N0} KB)");
        }
        
        Console.WriteLine();
        
        try
        {
            using var conn = new SqliteConnection($"Data Source={dbPath};");
            conn.Open();
            Console.WriteLine("✓ 数据库连接成功");
            
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "PRAGMA integrity_check;";
            var integrity = cmd.ExecuteScalar()?.ToString();
            Console.WriteLine($"完整性检查: {integrity ?? "未知"}");
            
            cmd.CommandText = "SELECT version FROM schema_version ORDER BY version DESC LIMIT 1;";
            var version = cmd.ExecuteScalar();
            Console.WriteLine($"数据库版本: {version ?? "无版本记录"}");
            
            cmd.CommandText = "SELECT COUNT(*) FROM specimens;";
            var specimenCount = cmd.ExecuteScalar();
            Console.WriteLine($"标本数量: {specimenCount ?? 0}");
            
            cmd.CommandText = "SELECT COUNT(*) FROM test_results;";
            var resultCount = cmd.ExecuteScalar();
            Console.WriteLine($"测试结果数量: {resultCount ?? 0}");
            
            cmd.CommandText = "SELECT COUNT(*) FROM patients;";
            var patientCount = cmd.ExecuteScalar();
            Console.WriteLine($"患者数量: {patientCount ?? 0}");
            
            Console.WriteLine();
            Console.WriteLine("索引列表:");
            cmd.CommandText = "SELECT name FROM sqlite_master WHERE type='index' AND name NOT LIKE 'sqlite_%';";
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                Console.WriteLine($"  - {reader.GetString(0)}");
            }
            
            Console.WriteLine();
            Console.WriteLine("表结构:");
            cmd.CommandText = "SELECT name FROM sqlite_master WHERE type='table';";
            using var tableReader = cmd.ExecuteReader();
            while (tableReader.Read())
            {
                Console.WriteLine($"  - {tableReader.GetString(0)}");
            }
            
            conn.Close();
            Console.WriteLine();
            Console.WriteLine("===== 诊断完成 =====");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"✗ 数据库连接失败: {ex.Message}");
            Console.WriteLine($"错误详情: {ex.GetType().Name}");
            if (!string.IsNullOrEmpty(ex.StackTrace))
            {
                Console.WriteLine($"堆栈跟踪: {ex.StackTrace.Substring(0, Math.Min(ex.StackTrace.Length, 500))}");
            }
        }
    }
    
    public static bool VerifySchema(string dbPath)
    {
        try
        {
            using var conn = new SqliteConnection($"Data Source={dbPath};");
            conn.Open();
            
            var requiredTables = new[] { "specimens", "test_results", "patients", "system_config", "system_logs", "service_status", "schema_version" };
            
            foreach (var table in requiredTables)
            {
                using var cmd = conn.CreateCommand();
                cmd.CommandText = $"SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name='{table}';";
                var count = Convert.ToInt32(cmd.ExecuteScalar());
                if (count == 0)
                {
                    Console.WriteLine($"✗ 缺少表: {table}");
                    return false;
                }
            }
            
            Console.WriteLine("✓ 所有必需表都存在");
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"✗ 验证失败: {ex.Message}");
            return false;
        }
    }
}