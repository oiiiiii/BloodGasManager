using BGService.Config;
using BGService.Data;
using BGShared.Database;

namespace BGService.Services;

/// <summary>
/// 后台维护服务：定时自动备份数据库、清理超期标本数据。
/// 配置项存于 system_config 表，每小时读取一次，修改后无需重启。
/// </summary>
public class MaintenanceService : BackgroundService
{
    public const string KeyBackupEnabled = "backup.enabled";
    public const string KeyBackupIntervalHours = "backup.interval_hours";
    public const string KeyBackupKeepCount = "backup.keep_count";
    public const string KeyBackupLastRun = "backup.last_run";
    public const string KeyRetentionDays = "data.retention_days";

    private readonly ConfigRepository _config;
    private readonly ISpecimenRepository _specimens;
    private readonly ISystemLogRepository _sysLog;
    private readonly ILogger<MaintenanceService> _logger;

    public MaintenanceService(
        ConfigRepository config,
        ISpecimenRepository specimens,
        ISystemLogRepository sysLog,
        ILogger<MaintenanceService> logger)
    {
        _config = config;
        _specimens = specimens;
        _sysLog = sysLog;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // 首次延迟 5 分钟，避免影响服务启动
        try
        {
            await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                RunBackupIfDue();
                RunRetentionCleanup();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "自动维护任务执行失败");
                try { _sysLog.Add("ERROR", "维护", "自动维护任务执行失败", ex.Message); }
                catch (Exception logEx) { _logger.LogError(logEx, "写入维护日志失败"); }
            }

            try
            {
                await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    private void RunBackupIfDue()
    {
        if (!_config.GetValue(KeyBackupEnabled, true))
            return;

        int intervalHours = Math.Max(1, _config.GetValue(KeyBackupIntervalHours, 24));

        var lastRunText = _config.GetValue(KeyBackupLastRun);
        if (DateTime.TryParse(lastRunText, out var lastRun) &&
            DateTime.Now - lastRun < TimeSpan.FromHours(intervalHours))
        {
            return;
        }

        var backupDir = Path.Combine(SettingsManager.Current.ArchivePath, "backups");
        Directory.CreateDirectory(backupDir);

        var fileName = $"bloodgas_{DateTime.Now:yyyyMMdd_HHmmss}.db";
        var targetPath = Path.Combine(backupDir, fileName);

        // WAL 模式下用 VACUUM INTO 生成一致性快照，避免直接拷贝丢失 wal 内容
        using (var conn = DbConnectionFactory.CreateConnection())
        using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = $"VACUUM INTO '{targetPath.Replace("'", "''")}';";
            cmd.CommandTimeout = 300;
            cmd.ExecuteNonQuery();
        }

        _config.SetValue(KeyBackupLastRun, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), "backup", "上次自动备份时间");

        int keepCount = Math.Max(1, _config.GetValue(KeyBackupKeepCount, 10));
        int deleted = PruneOldBackups(backupDir, keepCount);

        var msg = $"自动备份完成: {fileName}" + (deleted > 0 ? $"，清理旧备份 {deleted} 份" : "");
        _logger.LogInformation(msg);
        try { _sysLog.Add("INFO", "维护", msg); }
        catch (Exception ex) { _logger.LogError(ex, "写入维护日志失败"); }
    }

    private static int PruneOldBackups(string backupDir, int keepCount)
    {
        var files = new DirectoryInfo(backupDir)
            .GetFiles("bloodgas_*.db")
            .OrderByDescending(f => f.Name)
            .Skip(keepCount)
            .ToList();

        int deleted = 0;
        foreach (var file in files)
        {
            try
            {
                file.Delete();
                deleted++;
            }
            catch
            {
                // 单个文件删除失败不影响其他文件
            }
        }
        return deleted;
    }

    private void RunRetentionCleanup()
    {
        int retentionDays = _config.GetValue(KeyRetentionDays, 0);
        if (retentionDays <= 0)
            return;

        var cutoff = DateTime.Today.AddDays(-retentionDays);
        int deleted = _specimens.DeleteOlderThan(cutoff);

        if (deleted > 0)
        {
            var msg = $"数据保留策略执行：清理 {cutoff:yyyy-MM-dd} 之前的标本 {deleted} 条（保留 {retentionDays} 天）";
            _logger.LogInformation(msg);
            try { _sysLog.Add("INFO", "维护", msg); }
            catch (Exception ex) { _logger.LogError(ex, "写入维护日志失败"); }
        }
    }
}
