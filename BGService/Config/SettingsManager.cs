using System.Text.Json;

namespace BGService.Config;

public class AppSettings
{
    public int ApiPort { get; set; } = 5000;
    public bool AutoStart { get; set; } = false;
    public bool StartMinimized { get; set; } = true;
    public string DatabasePath { get; set; } = "bloodgas.db";
    public string ArchivePath { get; set; } = "archive";
    public string SerialPort { get; set; } = "COM1";
    public int BaudRate { get; set; } = 9600;
    public string LogPath { get; set; } = "logs";
}

public static class SettingsManager
{
    private static readonly string BaseDir = AppDomain.CurrentDomain.BaseDirectory;
    private static readonly string SettingsPath = Path.Combine(BaseDir, "appsettings.json");
    private static AppSettings? _current;

    public static AppSettings Current
    {
        get
        {
            if (_current == null)
                _current = Load();
            return _current;
        }
    }

    private static string ResolvePath(string path)
    {
        if (string.IsNullOrEmpty(path)) return path;
        return Path.IsPathRooted(path) ? path : Path.GetFullPath(Path.Combine(BaseDir, path));
    }

    public static AppSettings Load()
    {
        AppSettings? settings = null;
        try
        {
            if (File.Exists(SettingsPath))
            {
                var json = File.ReadAllText(SettingsPath);
                settings = JsonSerializer.Deserialize<AppSettings>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }
        }
        catch (Exception ex)
        {
            SafeLog($"读取配置失败: {ex.Message}");
        }

        settings ??= new AppSettings();

        settings.DatabasePath = ResolvePath(settings.DatabasePath);
        settings.ArchivePath = ResolvePath(settings.ArchivePath);
        settings.LogPath = ResolvePath(settings.LogPath);

        _current = settings;
        return _current;
    }

    private static string ToRelativePath(string path)
    {
        if (string.IsNullOrEmpty(path) || !Path.IsPathRooted(path)) return path;
        var baseDir = BaseDir.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
        if (path.StartsWith(baseDir, StringComparison.OrdinalIgnoreCase))
        {
            return path.Substring(baseDir.Length);
        }
        return path;
    }

    public static void Save()
    {
        if (_current == null) return;
        try
        {
            var clone = new AppSettings
            {
                ApiPort = _current.ApiPort,
                AutoStart = _current.AutoStart,
                StartMinimized = _current.StartMinimized,
                DatabasePath = ToRelativePath(_current.DatabasePath),
                ArchivePath = ToRelativePath(_current.ArchivePath),
                SerialPort = _current.SerialPort,
                BaudRate = _current.BaudRate,
                LogPath = ToRelativePath(_current.LogPath)
            };
            var json = JsonSerializer.Serialize(clone, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(SettingsPath, json);
        }
        catch (Exception ex)
        {
            SafeLog($"保存配置失败: {ex.Message}");
        }
    }

    private static void SafeLog(string message)
    {
        try
        {
            var dir = Path.Combine(BaseDir, "logs");
            Directory.CreateDirectory(dir);
            File.AppendAllText(Path.Combine(dir, "settings.log"),
                $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}{Environment.NewLine}");
        }
        catch
        {
            // 日志写入失败不再抛出
        }
    }

    public static void Update(Action<AppSettings> updateAction)
    {
        updateAction(Current);
        Save();
    }
}
