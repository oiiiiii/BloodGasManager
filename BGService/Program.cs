using System.Windows.Forms;
using BGShared.Database;
using BGService;
using BGService.Api;
using BGService.Config;
using BGService.Data;
using BGService.SerialPort;
using BGService.Tray;

AppDomain.CurrentDomain.UnhandledException += (s, e) =>
{
    try
    {
        var ex = e.ExceptionObject as Exception;
        File.AppendAllText(
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "crash.log"),
            $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] FATAL: {ex?.Message}\n{ex?.StackTrace}\n\n");
    }
    catch { }
    if (e.IsTerminating)
        Environment.Exit(1);
};

string mutexName = "BGService_SingleInstance";
string eventName = "BGService_ShowNotificationEvent";
bool createdNew;

using var mutex = new Mutex(true, mutexName, out createdNew);

if (!createdNew)
{
    try
    {
        using (var existingEvent = EventWaitHandle.OpenExisting(eventName))
        {
            existingEvent.Set();
        }
    }
    catch
    {
        MessageBox.Show("血气分析服务正在后台运行中。\n\n请在任务栏托盘区域找到图标进行操作。", 
            "程序已在运行", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }
    return;
}

var settings = SettingsManager.Load();

var builder = WebApplication.CreateBuilder(args);
int apiPort = settings.ApiPort;
builder.WebHost.UseUrls($"http://0.0.0.0:{apiPort}");

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

builder.Services.AddSingleton<ISpecimenRepository, SpecimenRepository>();
builder.Services.AddSingleton<IPatientRepository, PatientRepository>();
builder.Services.AddSingleton<ISystemLogRepository, SystemLogRepository>();
builder.Services.AddSingleton<IServiceStatusRepository, ServiceStatusRepository>();
builder.Services.AddSingleton<ConfigRepository>();
builder.Services.AddHostedService<BGService.Services.MaintenanceService>();

string dbPath = settings.DatabasePath;
string archivePath = settings.ArchivePath;
string portName = settings.SerialPort;
int baudRate = settings.BaudRate;

DbConnectionFactory.Initialize(dbPath);

var serialSettings = new SerialPortSettings
{
    PortName = portName,
    BaudRate = baudRate,
    DataBits = 8,
    Parity = System.IO.Ports.Parity.None,
    StopBits = System.IO.Ports.StopBits.One,
    AutoReconnect = true,
    ReconnectIntervalMs = 5000
};

builder.Services.AddSingleton(sp =>
{
    return new BGProcessor(
        serialSettings,
        archivePath,
        sp.GetRequiredService<ISpecimenRepository>(),
        sp.GetRequiredService<ISystemLogRepository>(),
        sp.GetRequiredService<IServiceStatusRepository>(),
        sp.GetRequiredService<IPatientRepository>());
});

TrayApplicationContext? trayContext = null;

EventWaitHandle? showEvent = null;
try
{
    showEvent = EventWaitHandle.OpenExisting(eventName);
}
catch (Exception ex)
{
    Program.SafeLog($"打开通知事件失败: {ex.Message}");
    try
    {
        showEvent = new EventWaitHandle(false, EventResetMode.AutoReset, eventName);
    }
    catch (Exception ex2)
    {
        Program.SafeLog($"创建通知事件失败: {ex2.Message}");
    }
}

var trayThread = new Thread(() =>
{
    ApplicationConfiguration.Initialize();
    trayContext = new TrayApplicationContext(async () =>
    {
        try
        {
            var processor = Program.GetService<BGProcessor>();
            processor?.Stop();
        }
        catch (Exception ex)
        {
            Program.SafeLog($"停止处理器失败: {ex.Message}");
        }
        Environment.Exit(0);
    });

    var processor2 = Program.GetService<BGProcessor>();
    processor2?.OnLog += msg =>
    {
        try
        {
            trayContext?.Log(msg);
            trayContext?.TrayIcon.UpdateStatus(msg.Length > 50 ? msg.Substring(0, 50) : msg);
            if (msg.Contains("解析") || msg.Contains("新标本") || msg.Contains("串口") || msg.Contains("接收"))
            {
                trayContext?.TrayIcon.StartWorking();
            }
        }
        catch (Exception ex)
        {
            Program.SafeLog($"托盘日志回调失败: {ex.Message}");
        }
    };

    processor2?.OnNewSpecimen += specimen =>
    {
        try
        {
            trayContext?.TrayIcon.StopWorking();
            trayContext?.TrayIcon.ShowBalloon($"已解析 {specimen.SpecimenNo} 数据", ToolTipIcon.Info, 3000);
            trayContext?.TrayIcon.UpdateStatus($"新标本: {specimen.SpecimenNo}");
        }
        catch (Exception ex)
        {
            Program.SafeLog($"新标本通知失败: {ex.Message}");
        }
    };

    trayContext.TrayIcon.UpdateStatus("启动中...");
    trayContext.Log($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] BGService 正在启动...");
    trayContext.Log($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] 串口配置: {portName}@{baudRate}");
    trayContext.Log($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] 数据库: {dbPath}");
    trayContext.Log($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] 归档目录: {archivePath}");

    var ev = showEvent;
    if (ev != null)
    {
        var eventThread = new Thread(() =>
        {
            while (true)
            {
                try
                {
                    ev.WaitOne();
                    if (trayContext != null)
                    {
                        trayContext.TrayIcon.ShowBalloon("血气分析服务正在后台运行", ToolTipIcon.Info, 3000);
                    }
                }
                catch (Exception ex)
                {
                    Program.SafeLog($"通知事件等待线程退出: {ex.Message}");
                    break;
                }
            }
        });
        eventThread.IsBackground = true;
        eventThread.Start();
    }

    Application.Run(trayContext);
})
{
    IsBackground = false,
    Name = "TrayIconThread"
};
trayThread.SetApartmentState(ApartmentState.STA);
trayThread.Start();

while (trayContext == null)
    Thread.Sleep(50);

var app = builder.Build();
Program._serviceProvider = app.Services;

app.UseCors();
app.UseDefaultFiles();
app.UseStaticFiles();

app.UseMiddleware<ApiKeyMiddleware>();

var api = app.MapGroup("/api");

api.MapGroup("/specimens").MapSpecimenEndpoints();
api.MapGroup("/patients").MapPatientEndpoints();
api.MapGroup("/status").MapStatusEndpoints();
api.MapGroup("/config").MapConfigEndpoints();
api.MapGroup("/admin").MapAdminEndpoints();

app.MapGet("/api/health", () => new { status = "ok", time = DateTime.Now })
   .WithName("HealthCheck");

var processor3 = app.Services.GetRequiredService<BGProcessor>();
processor3.Start();
trayContext.Log($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] 串口服务已启动");
trayContext.Log($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] API服务运行在 http://0.0.0.0:{apiPort}");
trayContext.TrayIcon.UpdateStatus($"运行中 - 端口 {apiPort}");

await app.RunAsync();

internal static partial class Program
{
    public static IServiceProvider? _serviceProvider { get; set; }

    public static T? GetService<T>() where T : class
    {
        return _serviceProvider?.GetService<T>();
    }

    private static readonly object _safeLogLock = new();

    /// <summary>
    /// 兜底日志：写入 logs/service.log，任何失败都不再抛出
    /// </summary>
    public static void SafeLog(string message)
    {
        try
        {
            var dir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs");
            Directory.CreateDirectory(dir);
            lock (_safeLogLock)
            {
                File.AppendAllText(Path.Combine(dir, "service.log"),
                    $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}{Environment.NewLine}");
            }
        }
        catch
        {
            // 日志写入失败不再抛出，避免二次异常
        }
    }
}