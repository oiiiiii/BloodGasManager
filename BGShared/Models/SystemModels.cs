namespace BGShared.Models;

public class ServiceStatus
{
    public bool IsRunning { get; set; }
    public string SerialPort { get; set; } = "";
    public int BaudRate { get; set; }
    public DateTime? LastHeartbeat { get; set; }
    public long TotalSpecimens { get; set; }
    public int TodaySpecimens { get; set; }
    public DateTime? LastMessageTime { get; set; }
    public string LastError { get; set; } = "";
    public string Version { get; set; } = "";
    public DateTime StartTime { get; set; }
}

public class SystemConfig
{
    public string Key { get; set; } = "";
    public string Value { get; set; } = "";
    public string Category { get; set; } = "";
    public string Description { get; set; } = "";
    public DateTime? UpdatedAt { get; set; }
}

public class SystemLog
{
    public long Id { get; set; }
    public string Level { get; set; } = "";
    public string Category { get; set; } = "";
    public string Message { get; set; } = "";
    public string Detail { get; set; } = "";
    public DateTime CreatedAt { get; set; }
}
