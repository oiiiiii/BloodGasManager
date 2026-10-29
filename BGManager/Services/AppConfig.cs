using System.Text.Json;

namespace BGManager.Services;

public class AppConfig
{
    public string ServerUrl { get; set; } = "http://localhost:5000";
    public string ApiKey { get; set; } = "";
    
    public ShortcutKeys ShortcutKeys { get; set; } = new();

    private static readonly string ConfigPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.json");

    public static AppConfig Load()
    {
        if (File.Exists(ConfigPath))
        {
            try
            {
                var json = File.ReadAllText(ConfigPath);
                return JsonSerializer.Deserialize<AppConfig>(json) ?? new AppConfig();
            }
            catch
            {
                return new AppConfig();
            }
        }
        return new AppConfig();
    }

    public void Save()
    {
        try
        {
            var json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(ConfigPath, json);
        }
        catch
        {
        }
    }
}

public class ShortcutKeys
{
    public string Refresh { get; set; } = "F5";
    public string Audit { get; set; } = "F6";
    public string Unarchive { get; set; } = "F7";
    public string Delete { get; set; } = "Del";
    public string Search { get; set; } = "Ctrl+F";
    public string Print { get; set; } = "Ctrl+P";
}